using System;

public class Spectrum {
	public const int TONE_DIV = 4;
	public const int TONE_DIV_CENTER = TONE_DIV / 2;

	const int LOW_FREQ = 80;
	const int MID_FREQ = 350;
	const int OCT_DIV = TONE_DIV * 12;
	const int THRESHOLD_WIDE = TONE_DIV * 12 / 2;
	const int THRESHOLD_NARROW = TONE_DIV * 13 / 16;
	const double RMS_MIN = 1e-6;

	readonly int LOW_TONE;
	readonly int MID_TONE;
	readonly double AUTO_GAIN_ATTENUATION;

	double mMaxPeakL;
	double mMaxPeakR;
	double mMaxCurveL;
	double mMaxCurveR;

	public Bandpass[] Banks { get; private set; }

	delegate void DSetRms(IntPtr pInput, int sampleCount);
	DSetRms mSetRms;

	public static double Transpose { get; set; } = 0;
	public static bool AutoGain { get; set; } = true;
	public static bool NormGain { get; set; } = false;
	public static double ResponceSpeed { get; set; } = 16.0;

	public double GainL { get; private set; }
	public double GainR { get; private set; }
	public double[] PeakL { get; private set; }
	public double[] PeakR { get; private set; }

	public double[] Peak { get; private set; }
	public double[] Curve { get; private set; }
	public double[] Threshold { get; private set; }

	public Spectrum(int sampleRate, double baseFrequency, int tones, int bufferSamples, bool stereo) {
		var bankCount = tones * TONE_DIV;
		LOW_TONE = (int)(12 * TONE_DIV * Math.Log(LOW_FREQ / baseFrequency, 2));
		MID_TONE = (int)(12 * TONE_DIV * Math.Log(MID_FREQ / baseFrequency, 2));
		AUTO_GAIN_ATTENUATION = 0.5 * bufferSamples / sampleRate;
		PeakL = new double[bankCount];
		PeakR = new double[bankCount];
		Peak = new double[bankCount];
		Curve = new double[bankCount];
		Threshold = new double[bankCount];
		mMaxPeakL = RMS_MIN;
		mMaxPeakR = RMS_MIN;
		mMaxCurveL = RMS_MIN;
		mMaxCurveR = RMS_MIN;
		Banks = new Bandpass[bankCount];
		for (int b = 0; b < bankCount; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				var frequency = baseFrequency * Math.Pow(2.0, (bd - 0.5 * TONE_DIV) / OCT_DIV);
				Banks[bd] = new Bandpass();
				Banks[bd].SetParam(sampleRate, frequency);
			}
		}
		SetResponceSpeed(sampleRate);
		if (stereo) {
			mSetRms = SetRmsStereo;
		}
		else {
			mSetRms = SetRmsMono;
		}
	}

	public void SetResponceSpeed(int sampleRate) {
		for (int b = 0; b < Banks.Length; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				Banks[bd].SetResponceSpeed(sampleRate, ResponceSpeed);
			}
		}
	}

	public void SetValue(IntPtr pInput, int sampleCount) {
		if (NormGain) {
			mMaxCurveL = RMS_MIN;
			mMaxCurveR = RMS_MIN;
		}
		if (AutoGain) {
			mMaxCurveL += (RMS_MIN - mMaxCurveL) * AUTO_GAIN_ATTENUATION;
			mMaxCurveR += (RMS_MIN - mMaxCurveR) * AUTO_GAIN_ATTENUATION;
		}
		mMaxPeakL += (RMS_MIN - mMaxPeakL) * AUTO_GAIN_ATTENUATION;
		mMaxPeakR += (RMS_MIN - mMaxPeakR) * AUTO_GAIN_ATTENUATION;
		mSetRms(pInput, sampleCount);
		GainL = Math.Sqrt(mMaxPeakL);
		GainR = Math.Sqrt(mMaxPeakR);
		if (!(AutoGain || NormGain)) {
			mMaxCurveL = 1;
			mMaxCurveR = 1;
		}

		var lastPeakL = 0.0;
		var lastPeakR = 0.0;
		var lastPeakIndexL = -1;
		var lastPeakIndexR = -1;
		var lastPeak = 0.0;
		var lastPeakIndex = -1;
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
			var peakThL = 0.0;
			var peakThR = 0.0;
			var curveThL = 0.0;
			var curveThR = 0.0;
			for (int w = -width; w <= width; ++w) {
				var bw = Math.Min(bankCount - 1, Math.Max(0, idxB + w));
				peakThL += Banks[bw].PeakL;
				peakThR += Banks[bw].PeakR;
				curveThL += Banks[bw].CurveL;
				curveThR += Banks[bw].CurveR;
			}
			peakThL /= width * 2 + 1;
			peakThR /= width * 2 + 1;
			peakThL = Math.Sqrt(peakThL / mMaxPeakL);
			peakThR = Math.Sqrt(peakThR / mMaxPeakR);
			Threshold[idxB] = Math.Sqrt(Math.Max(
				curveThL / mMaxCurveL,
				curveThR / mMaxCurveR
			) / (width * 2 + 1));
			/* Set peak */
			PeakL[idxB] = 0.0;
			PeakR[idxB] = 0.0;
			var peakL = Math.Sqrt(Banks[idxB].PeakL / mMaxPeakL);
			var peakR = Math.Sqrt(Banks[idxB].PeakR / mMaxPeakR);
			if (peakL < peakThL) {
				if (0 <= lastPeakIndexL) {
					PeakL[lastPeakIndexL] = lastPeakL;
				}
				peakL = 0.0;
				lastPeakL = 0.0;
				lastPeakIndexL = -1;
			}
			if (lastPeakL < peakL) {
				lastPeakL = peakL;
				lastPeakIndexL = idxB;
			}
			if (peakR < peakThR) {
				if (0 <= lastPeakIndexR) {
					PeakR[lastPeakIndexR] = lastPeakR;
				}
				peakR = 0.0;
				lastPeakR = 0.0;
				lastPeakIndexR = -1;
			}
			if (lastPeakR < peakR) {
				lastPeakR = peakR;
				lastPeakIndexR = idxB;
			}
			/* Set curve */
			var curve = Math.Sqrt(Math.Max(
				Banks[idxB].CurveL / mMaxCurveL,
				Banks[idxB].CurveR / mMaxCurveR
			));
			Curve[idxB] = curve;
			Peak[idxB] = 0.0;
			if (curve < Threshold[idxB]) {
				if (0 <= lastPeakIndex) {
					Peak[lastPeakIndex] = lastPeak;
				}
				curve = 0.0;
				lastPeak = 0.0;
				lastPeakIndex = -1;
			}
			if (lastPeak < curve) {
				lastPeak = curve;
				lastPeakIndex = idxB;
			}
		}
		if (0 <= lastPeakIndexL) {
			PeakL[lastPeakIndexL] = lastPeakL;
		}
		if (0 <= lastPeakIndexR) {
			PeakR[lastPeakIndexR] = lastPeakR;
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
				bank.PeakL += (output - bank.PeakL) * bank.PeakDelta;
				bank.CurveL += (output - bank.CurveL) * bank.CurveDelta;
			}
			mMaxPeakL = Math.Max(mMaxPeakL, bank.PeakL);
			mMaxCurveL = Math.Max(mMaxCurveL, bank.CurveL);
			bank.PeakR = bank.PeakL;
			bank.CurveR = bank.CurveL;
		}
		mMaxPeakR = mMaxPeakL;
		mMaxCurveR = mMaxCurveL;
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
				bank.PeakL += (output - bank.PeakL) * bank.PeakDelta;
				bank.CurveL += (output - bank.CurveL) * bank.CurveDelta;
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
				bank.PeakR += (output - bank.PeakR) * bank.PeakDelta;
				bank.CurveR += (output - bank.CurveR) * bank.CurveDelta;
			}
			mMaxPeakL = Math.Max(mMaxPeakL, bank.PeakL);
			mMaxPeakR = Math.Max(mMaxPeakR, bank.PeakR);
			mMaxCurveL = Math.Max(mMaxCurveL, bank.CurveL);
			mMaxCurveR = Math.Max(mMaxCurveR, bank.CurveR);
		}
	}
}
