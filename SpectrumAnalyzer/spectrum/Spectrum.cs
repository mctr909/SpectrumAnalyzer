using SpectrumAnalyzer.spectrum;
using System;

public class Spectrum {
	public const int TONE_DIV = 5;
	public const int TONE_DIV_CENTER = TONE_DIV / 2;

	const int WAVE_LENGTH = 96;
	const int LOW_FREQ = 80;
	const int MID_FREQ = 350;
	const int OCT_DIV = TONE_DIV * 12;
	const int THRESHOLD_WIDE = TONE_DIV * 12 / 2;
	const int THRESHOLD_NARROW = TONE_DIV * 5 / 4;
	const double RMS_MIN = 1e-6;

	readonly int SAMPLE_RATE;
	readonly int BANK_COUNT;
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

	public static double ResponceSpeed { get; set; } = 16;
	public static double Transpose { get; set; } = 0;
	public static bool AutoGain { get; set; } = true;
	public static bool NormGain { get; set; } = false;

	public double GainL { get; private set; }
	public double GainR { get; private set; }
	public double[] L { get; private set; }
	public double[] R { get; private set; }

	public double[] Peak { get; private set; }
	public double[] Curve { get; private set; }
	public double[] Threshold { get; private set; }

	public Spectrum(int sampleRate, double baseFrequency, int tones, int bufferSamples, bool stereo) {
		SAMPLE_RATE = sampleRate;
		BANK_COUNT = tones * TONE_DIV;
		LOW_TONE = (int)(12 * TONE_DIV * Math.Log(LOW_FREQ / baseFrequency, 2));
		MID_TONE = (int)(12 * TONE_DIV * Math.Log(MID_FREQ / baseFrequency, 2));
		AUTO_GAIN_ATTENUATION = 0.5 * bufferSamples / sampleRate;
		L = new double[BANK_COUNT];
		R = new double[BANK_COUNT];
		Peak = new double[BANK_COUNT];
		Curve = new double[BANK_COUNT];
		Threshold = new double[BANK_COUNT];
		mMaxL = RMS_MIN;
		mMaxR = RMS_MIN;
		mMaxDisplayL = RMS_MIN;
		mMaxDisplayR = RMS_MIN;
		Banks = new BPFBank[BANK_COUNT];
		for (int b = 0; b < BANK_COUNT; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				var frequency = baseFrequency * Math.Pow(2.0, (bd - 0.5 * TONE_DIV) / OCT_DIV);
				Banks[bd] = new BPFBank();
				SetBPF(Banks[bd], frequency);
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
		for (int b = 0; b < BANK_COUNT; ++b) {
			var bank = Banks[b];
			var frequency = bank.DELTA * SAMPLE_RATE;
			var responceSpeed = ResponceSpeed * 16;
			if (responceSpeed > frequency) {
				bank.DisplaySigma = GetAlpha(frequency);
			}
			else {
				bank.DisplaySigma = GetAlpha(responceSpeed);
			}
		}
	}

	void SetBPF(BPFBank bank, double frequency) {
		bank.DELTA = frequency / SAMPLE_RATE;
		bank.SIGMA = GetAlpha(8 * frequency);
		var alpha = GetAlpha(frequency);
		var a0 = 1.0 + alpha;
		bank.KB0 = alpha / a0;
		bank.KA1 = -2.0 * Math.Cos(2 * Math.PI * bank.DELTA) / a0;
		bank.KA2 = (1.0 - alpha) / a0;
	}

	double GetAlpha(double frequency) {
		const double MIN_WIDTH = 1.0;
		const double MIN_WIDTH_AT_FREQ = 440.0;
		var halfToneWidth = MIN_WIDTH + Math.Log(MIN_WIDTH_AT_FREQ / frequency, 2.0);
		if (halfToneWidth < MIN_WIDTH) {
			halfToneWidth = MIN_WIDTH;
		}
		var omega = 2 * Math.PI * frequency / SAMPLE_RATE;
		var s = Math.Sin(omega);
		var x = Math.Log(2) / 4 * halfToneWidth / 12.0 * omega / s;
		var a = s * Math.Sinh(x);
		if (a > 1) {
			return 1;
		}
		return a;
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
		for (int idxB = 0; idxB < BANK_COUNT; ++idxB) {
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
				var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, idxB + w));
				thL += Banks[bw].LPower;
				thR += Banks[bw].RPower;
				thDisplayL += Banks[bw].LDisplay;
				thDisplayR += Banks[bw].RDisplay;
			}
			thL /= width * 2 + 1;
			thR /= width * 2 + 1;

			double dthL;
			double dthR;
			double dthDisplayL;
			double dthDisplayR;
			if (idxB == 0) {
				dthL = 0;
				dthR = 0;
				dthDisplayL = 0;
				dthDisplayR = 0;
			} else {
				dthL = Math.Abs(Banks[idxB].LPower - Banks[idxB - 1].LPower);
				dthR = Math.Abs(Banks[idxB].RPower - Banks[idxB - 1].RPower);
				dthDisplayL = Math.Abs(Banks[idxB].LDisplay - Banks[idxB - 1].LDisplay);
				dthDisplayR = Math.Abs(Banks[idxB].RDisplay - Banks[idxB - 1].RDisplay);
			}
			const double A = 1.26;
			const double A1 = A - 1.0;
			const double G = 0.01;
			dthL = A - A1 * dthL / (dthL + G);
			dthR = A - A1 * dthR / (dthR + G);
			dthDisplayL = A - A1 * dthDisplayL / (dthDisplayL + G);
			dthDisplayR = A - A1 * dthDisplayR / (dthDisplayR + G);

			thL = Math.Sqrt(thL * dthL / mMaxL);
			thR = Math.Sqrt(thR * dthR / mMaxR);
			Threshold[idxB] = Math.Sqrt(Math.Max(
				thDisplayL * dthDisplayL / mMaxDisplayL,
				thDisplayR * dthDisplayR / mMaxDisplayR
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
		for (int b = 0; b < BANK_COUNT; ++b) {
			var pWave = (float*)pInput;
			var bank = Banks[b];
			for (int s = 0; s < sampleCount; ++s) {
				var input = *pWave++;
				var output
					= bank.KB0 * input
					- bank.KB0 * bank.Lb2
					- bank.KA1 * bank.La1
					- bank.KA2 * bank.La2
				;
				bank.La2 = bank.La1;
				bank.La1 = output;
				bank.Lb2 = bank.Lb1;
				bank.Lb1 = input;
				output *= output;
				bank.LPower += (output - bank.LPower) * bank.SIGMA;
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
		for (int b = 0; b < BANK_COUNT; ++b) {
			var pWave = (float*)pInput;
			var bank = Banks[b];
			for (int s = 0; s < sampleCount; ++s) {
				var input = *pWave++;
				var output
					= bank.KB0 * input
					- bank.KB0 * bank.Lb2
					- bank.KA1 * bank.La1
					- bank.KA2 * bank.La2
				;
				bank.La2 = bank.La1;
				bank.La1 = output;
				bank.Lb2 = bank.Lb1;
				bank.Lb1 = input;
				output *= output;
				bank.LPower += (output - bank.LPower) * bank.SIGMA;
				bank.LDisplay += (output - bank.LDisplay) * bank.DisplaySigma;
				input = *pWave++;
				output
					= bank.KB0 * input
					- bank.KB0 * bank.Rb2
					- bank.KA1 * bank.Ra1
					- bank.KA2 * bank.Ra2
				;
				bank.Ra2 = bank.Ra1;
				bank.Ra1 = output;
				bank.Rb2 = bank.Rb1;
				bank.Rb1 = input;
				output *= output;
				bank.RPower += (output - bank.RPower) * bank.SIGMA;
				bank.RDisplay += (output - bank.RDisplay) * bank.DisplaySigma;
			}
			mMaxL = Math.Max(mMaxL, bank.LPower);
			mMaxR = Math.Max(mMaxR, bank.RPower);
			mMaxDisplayL = Math.Max(mMaxDisplayL, bank.LDisplay);
			mMaxDisplayR = Math.Max(mMaxDisplayR, bank.RDisplay);
		}
	}
}
