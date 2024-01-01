using SpectrumAnalyzer.spectrum;
using System;

public class Spectrum {
	public const int TONE_DIV = 4;
	public const int TONE_DIV_CENTER = TONE_DIV / 2;

	const int WAVE_LENGTH = 96;
	const int LOW_FREQ = 80;
	const int MID_FREQ = 350;
	const int OCT_DIV = TONE_DIV * 12;
	const int THRESHOLD_WIDE = TONE_DIV * 12 / 2;
	const int THRESHOLD_NARROW = TONE_DIV * 5 / 4;
	const double RMS_MIN = 1e-6;

	readonly int LOW_TONE;
	readonly int MID_TONE;
	readonly double AUTO_GAIN_ATTENUATION;

	double mMaxL;
	double mMaxR;
	double mMaxDisplayL;
	double mMaxDisplayR;

	public BPFBank[] Banks { get; private set; }

	delegate void DSetRms(IntPtr pInput, int sampleCount);
	DSetRms mSetRms;

	public static double Transpose { get; set; } = 0;
	public static bool AutoGain { get; set; } = true;
	public static bool NormGain { get; set; } = false;
	public static double ResponceSpeed { get; set; } = 16.0;

	public double GainL { get; private set; }
	public double GainR { get; private set; }
	public double[] L { get; private set; }
	public double[] R { get; private set; }

	public double[] Peak { get; private set; }
	public double[] Curve { get; private set; }
	public double[] Threshold { get; private set; }

	public Spectrum(int sampleRate, double baseFrequency, int tones, int bufferSamples, bool stereo) {
		var bankCount = tones * TONE_DIV;
		LOW_TONE = (int)(12 * TONE_DIV * Math.Log(LOW_FREQ / baseFrequency, 2));
		MID_TONE = (int)(12 * TONE_DIV * Math.Log(MID_FREQ / baseFrequency, 2));
		AUTO_GAIN_ATTENUATION = 0.5 * bufferSamples / sampleRate;
		L = new double[bankCount];
		R = new double[bankCount];
		Peak = new double[bankCount];
		Curve = new double[bankCount];
		Threshold = new double[bankCount];
		mMaxL = RMS_MIN;
		mMaxR = RMS_MIN;
		mMaxDisplayL = RMS_MIN;
		mMaxDisplayR = RMS_MIN;
		Banks = new BPFBank[bankCount];
		for (int b = 0; b < bankCount; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				var frequency = baseFrequency * Math.Pow(2.0, (bd - 0.5 * TONE_DIV) / OCT_DIV);
				Banks[bd] = new BPFBank(sampleRate, frequency);
			}
		}
		SetResponceSpeed();
		if (stereo) {
			mSetRms = SetRmsStereo;
		}
		else {
			mSetRms = SetRmsMono;
		}
	}

	public void SetResponceSpeed() {
		for (int b = 0; b < Banks.Length; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				Banks[bd].SetResponceSpeed(ResponceSpeed);
			}
		}
	}

	public void SetSpeed(double speed) {
		for (int b = 0; b < Banks.Length; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				Banks[bd].SetSpeed(speed);
			}
		}
	}

	public void SetValue(IntPtr pInput, int sampleCount) {
		if (NormGain) {
			mMaxDisplayL = RMS_MIN;
			mMaxDisplayR = RMS_MIN;
		}
		if (AutoGain) {
			mMaxDisplayL += (RMS_MIN - mMaxDisplayL) * AUTO_GAIN_ATTENUATION;
			mMaxDisplayR += (RMS_MIN - mMaxDisplayR) * AUTO_GAIN_ATTENUATION;
		}
		mMaxL += (RMS_MIN - mMaxL) * AUTO_GAIN_ATTENUATION;
		mMaxR += (RMS_MIN - mMaxR) * AUTO_GAIN_ATTENUATION;
		mSetRms(pInput, sampleCount);
		GainL = Math.Sqrt(mMaxL);
		GainR = Math.Sqrt(mMaxR);
		if (!(AutoGain || NormGain)) {
			mMaxDisplayL = 1;
			mMaxDisplayR = 1;
		}

		var lastL = 0.0;
		var lastR = 0.0;
		var lastIndexL = -1;
		var lastIndexR = -1;
		var lastDisplay = 0.0;
		var lastDisplayIndex = -1;
		var bankCount = Banks.Length;
		for (int idxB = 0; idxB < bankCount; ++idxB) {
			/* Calc threshold */
			int width;
			var transposeB = idxB + Transpose * TONE_DIV;
			if (transposeB < LOW_TONE) {
				width = THRESHOLD_WIDE;
			}
			else if (transposeB < MID_TONE) {
				var a2b = (double)(transposeB - LOW_TONE) / (MID_TONE - LOW_TONE);
				width = (int)(THRESHOLD_NARROW * a2b + THRESHOLD_WIDE * (1 - a2b));
			}
			else {
				width = THRESHOLD_NARROW;
			}
			var thL = 0.0;
			var thR = 0.0;
			var thDisplayL = 0.0;
			var thDisplayR = 0.0;
			for (int w = -width; w <= width; ++w) {
				var bw = Math.Min(bankCount - 1, Math.Max(0, idxB + w));
				thL += Banks[bw].LPower;
				thR += Banks[bw].RPower;
				thDisplayL += Banks[bw].LDisplay;
				thDisplayR += Banks[bw].RDisplay;
			}
			thL /= width * 2 + 1;
			thR /= width * 2 + 1;
			thL = Math.Sqrt(thL / mMaxL);
			thR = Math.Sqrt(thR / mMaxR);
			Threshold[idxB] = Math.Sqrt(Math.Max(
				thDisplayL / mMaxDisplayL,
				thDisplayR / mMaxDisplayR
			) / (width * 2 + 1));
			/* Set peak */
			L[idxB] = 0.0;
			R[idxB] = 0.0;
			var l = Math.Sqrt(Banks[idxB].LPower / mMaxL);
			var r = Math.Sqrt(Banks[idxB].RPower / mMaxR);
			if (l < thL) {
				if (0 <= lastIndexL) {
					L[lastIndexL] = lastL;
				}
				l = 0.0;
				lastL = 0.0;
				lastIndexL = -1;
			}
			if (lastL < l) {
				lastL = l;
				lastIndexL = idxB;
			}
			if (r < thR) {
				if (0 <= lastIndexR) {
					R[lastIndexR] = lastR;
				}
				r = 0.0;
				lastR = 0.0;
				lastIndexR = -1;
			}
			if (lastR < r) {
				lastR = r;
				lastIndexR = idxB;
			}
			/* Set display value */
			var display = Math.Sqrt(Math.Max(
				Banks[idxB].LDisplay / mMaxDisplayL,
				Banks[idxB].RDisplay / mMaxDisplayR
			));
			Curve[idxB] = display;
			Peak[idxB] = 0.0;
			if (display < Threshold[idxB]) {
				if (0 <= lastDisplayIndex) {
					Peak[lastDisplayIndex] = lastDisplay;
				}
				display = 0.0;
				lastDisplay = 0.0;
				lastDisplayIndex = -1;
			}
			if (lastDisplay < display) {
				lastDisplay = display;
				lastDisplayIndex = idxB;
			}
		}
		if (0 <= lastIndexL) {
			L[lastIndexL] = lastL;
		}
		if (0 <= lastIndexR) {
			R[lastIndexR] = lastR;
		}
	}

	unsafe void SetRmsMono(IntPtr pInput, int sampleCount) {
		var inputWave = (float*)pInput;
		for (int b = 0; b < Banks.Length; ++b) {
			var bank = Banks[b];
			for (int t = 0; t < sampleCount; ++t) {
				var input = inputWave[t];
				var output
					= bank.Kb0 * input
					- bank.Kb0 * bank.Lb2
					- bank.Ka1 * bank.La1
					- bank.Ka2 * bank.La2
				;
				bank.La2 = bank.La1;
				bank.La1 = output;
				bank.Lb2 = bank.Lb1;
				bank.Lb1 = input;
				output *= output;
				bank.LPower += (output - bank.LPower) * bank.Sigma;
				bank.LDisplay += (output - bank.LDisplay) * bank.DisplaySigma;
			}
			mMaxL = Math.Max(mMaxL, bank.LPower);
			mMaxDisplayL = Math.Max(mMaxDisplayL, bank.LDisplay);
			bank.RPower = bank.LPower;
			bank.RDisplay = bank.LDisplay;
		}
		mMaxR = mMaxL;
		mMaxDisplayR = mMaxDisplayL;
	}

	unsafe void SetRmsStereo(IntPtr pInput, int sampleCount) {
		var inputWave = (float*)pInput;
		for (int b = 0; b < Banks.Length; ++b) {
			var bank = Banks[b];
			for (int t = 0, i = 0; t < sampleCount; ++t, i += 2) {
				var input = inputWave[i];
				var output
					= bank.Kb0 * input
					- bank.Kb0 * bank.Lb2
					- bank.Ka1 * bank.La1
					- bank.Ka2 * bank.La2
				;
				bank.La2 = bank.La1;
				bank.La1 = output;
				bank.Lb2 = bank.Lb1;
				bank.Lb1 = input;
				output *= output;
				bank.LPower += (output - bank.LPower) * bank.Sigma;
				bank.LDisplay += (output - bank.LDisplay) * bank.DisplaySigma;
				input = inputWave[i + 1];
				output
					= bank.Kb0 * input
					- bank.Kb0 * bank.Rb2
					- bank.Ka1 * bank.Ra1
					- bank.Ka2 * bank.Ra2
				;
				bank.Ra2 = bank.Ra1;
				bank.Ra1 = output;
				bank.Rb2 = bank.Rb1;
				bank.Rb1 = input;
				output *= output;
				bank.RPower += (output - bank.RPower) * bank.Sigma;
				bank.RDisplay += (output - bank.RDisplay) * bank.DisplaySigma;
			}
			mMaxL = Math.Max(mMaxL, bank.LPower);
			mMaxR = Math.Max(mMaxR, bank.RPower);
			mMaxDisplayL = Math.Max(mMaxDisplayL, bank.LDisplay);
			mMaxDisplayR = Math.Max(mMaxDisplayR, bank.RDisplay);
		}
	}
}
