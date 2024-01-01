using System;

public class Spectrum {
	public const int TONE_DIV = 5;
	public const int TONE_DIV_CENTER = TONE_DIV / 2;
	public readonly int COUNT;
	public readonly int THRESHOLD_BEGIN;
	public readonly int PEAK_BEGIN;

	const int LOW_WIDTH = TONE_DIV * 10;
	const int HIGH_TONE = TONE_DIV * 120;
	const int MID_TONE = TONE_DIV * 30;
	const int OCT_DIV = TONE_DIV * 12;
	const double RMS_MIN = 1e-5;

	readonly double RMS_MAX_ATTENUATION;

	double mPeakMaxL;
	double mPeakMaxR;
	double mSlopeMaxL;
	double mSlopeMaxR;
	Bandpass[] mBanks;

	delegate void DSetRms(IntPtr pInput, int sampleCount);
	DSetRms mSetRms;

	public static double Transpose { get; set; } = 0;
	public static bool AutoGain { get; set; } = true;
	public static bool NormGain { get; set; } = false;
	public static double ResponceSpeed { get; set; } = 80.0;

	public double[] PeakL { get; private set; }
	public double[] PeakR { get; private set; }
	public double[] Slope { get; private set; }
	public double GainL { get; private set; }
	public double GainR { get; private set; }

	public Spectrum(int sampleRate, double baseFrequency, int tones, int bufferSamples, bool stereo) {
		RMS_MAX_ATTENUATION = 1.0 * bufferSamples / sampleRate;
		COUNT = tones * TONE_DIV;
		THRESHOLD_BEGIN = COUNT;
		PEAK_BEGIN = COUNT * 2;
		PeakL = new double[COUNT];
		PeakR = new double[COUNT];
		Slope = new double[COUNT * 3];
		mPeakMaxL = RMS_MIN;
		mPeakMaxR = RMS_MIN;
		mSlopeMaxL = RMS_MIN;
		mSlopeMaxR = RMS_MIN;
		mBanks = new Bandpass[COUNT];
		for (int b = 0; b < COUNT; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				var frequency = baseFrequency * Math.Pow(2.0, (bd - 0.5 * TONE_DIV) / OCT_DIV);
				mBanks[bd] = new Bandpass();
				mBanks[bd].SetParam(sampleRate, frequency);
			}
		}
		SetResponceSpeed(sampleRate);
		if (stereo) {
			mSetRms = SetRmsStereo;
		} else {
			mSetRms = SetRmsMono;
		}
	}

	public void SetResponceSpeed(int sampleRate) {
		for (int b = 0; b < COUNT; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				mBanks[bd].SetResponceSpeed(sampleRate, ResponceSpeed);
			}
		}
	}

	public void SetValue(IntPtr pInput, int sampleCount) {
		if (NormGain) {
			mPeakMaxL = RMS_MIN;
			mPeakMaxR = RMS_MIN;
			mSlopeMaxL = RMS_MIN;
			mSlopeMaxR = RMS_MIN;
		}
		if (AutoGain) {
			mPeakMaxL += (RMS_MIN - mPeakMaxL) * RMS_MAX_ATTENUATION;
			mPeakMaxR += (RMS_MIN - mPeakMaxR) * RMS_MAX_ATTENUATION;
			mSlopeMaxL += (RMS_MIN - mSlopeMaxL) * RMS_MAX_ATTENUATION;
			mSlopeMaxR += (RMS_MIN - mSlopeMaxR) * RMS_MAX_ATTENUATION;
		}
		mSetRms(pInput, sampleCount);
		if (AutoGain || NormGain) {
			GainL = Math.Sqrt(mPeakMaxL);
			GainR = Math.Sqrt(mPeakMaxR);
		} else {
			GainL = 1;
			GainR = 1;
			mPeakMaxL = 1;
			mPeakMaxR = 1;
			mSlopeMaxL = 1;
			mSlopeMaxR = 1;
		}

		var lastPeakL = 0.0;
		var lastPeakR = 0.0;
		var lastPeakIndexL = -1;
		var lastPeakIndexR = -1;
		var lastPeak = 0.0;
		var lastPeakIndex = -1;
		for (int b = 0; b < COUNT; ++b) {
			/* Calc threshold */
			int width;
			var t = b + Transpose * TONE_DIV;
			if (t < MID_TONE) {
				width = LOW_WIDTH;
			} else if (t < HIGH_TONE) {
				var a2b = (double)(t - MID_TONE) / (HIGH_TONE - MID_TONE);
				width = (int)(2 * a2b + LOW_WIDTH * (1 - a2b));
			} else {
				width = 2;
			}
			var peakThL = 0.0;
			var peakThR = 0.0;
			var slopeThL = 0.0;
			var slopeThR = 0.0;
			for (int w = -width; w <= width; ++w) {
				var bw = Math.Min(COUNT - 1, Math.Max(0, b + w));
				peakThL += mBanks[bw].PeakL;
				peakThR += mBanks[bw].PeakR;
				slopeThL += mBanks[bw].SlopeL;
				slopeThR += mBanks[bw].SlopeR;
			}
			peakThL /= width * 2 + 1;
			peakThR /= width * 2 + 1;
			peakThL = Math.Sqrt(peakThL / mPeakMaxL);
			peakThR = Math.Sqrt(peakThR / mPeakMaxR);
			/* Set peak */
			PeakL[b] = 0.0;
			PeakR[b] = 0.0;
			var peakL = Math.Sqrt(mBanks[b].PeakL / mPeakMaxL);
			var peakR = Math.Sqrt(mBanks[b].PeakR / mPeakMaxR);
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
				lastPeakIndexL = b;
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
				lastPeakIndexR = b;
			}
			/* Set slope */
			var curve = Math.Sqrt(Math.Max(
				mBanks[b].SlopeL / mSlopeMaxL,
				mBanks[b].SlopeR / mSlopeMaxR
			));
			Slope[b] = curve;
			Slope[THRESHOLD_BEGIN + b] = Math.Sqrt(Math.Max(
				slopeThL / mSlopeMaxL,
				slopeThR / mSlopeMaxR
			) / (width * 2 + 1));
			Slope[PEAK_BEGIN + b] = 0.0;
			if (curve < Slope[THRESHOLD_BEGIN + b]) {
				if (0 <= lastPeakIndex) {
					Slope[PEAK_BEGIN + lastPeakIndex] = lastPeak;
				}
				curve = 0.0;
				lastPeak = 0.0;
				lastPeakIndex = -1;
			}
			if (lastPeak < curve) {
				lastPeak = curve;
				lastPeakIndex = b;
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
		var inputWave = (short*)pInput;
		for (int b = 0; b < COUNT; ++b) {
			var bank = mBanks[b];
			for (int t = 0; t < sampleCount; ++t) {
				var input = inputWave[t] / 32768.0;
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
				bank.SlopeL += (output - bank.SlopeL) * bank.SlopeDelta;
			}
			mPeakMaxL = Math.Max(mPeakMaxL, bank.PeakL);
			mSlopeMaxL = Math.Max(mSlopeMaxL, bank.SlopeL);
			bank.PeakR = bank.PeakL;
			bank.SlopeR = bank.SlopeL;
		}
		mPeakMaxR = mPeakMaxL;
		mSlopeMaxR = mSlopeMaxL;
	}

	unsafe void SetRmsStereo(IntPtr pInput, int sampleCount) {
		var inputWave = (short*)pInput;
		for (int b = 0; b < COUNT; ++b) {
			var bank = mBanks[b];
			for (int t = 0, i = 0; t < sampleCount; ++t, i += 2) {
				var input = inputWave[i] / 32768.0;
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
				bank.SlopeL += (output - bank.SlopeL) * bank.SlopeDelta;
				input = inputWave[i + 1] / 32768.0;
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
				bank.SlopeR += (output - bank.SlopeR) * bank.SlopeDelta;
			}
			mPeakMaxL = Math.Max(mPeakMaxL, bank.PeakL);
			mPeakMaxR = Math.Max(mPeakMaxR, bank.PeakR);
			mSlopeMaxL = Math.Max(mSlopeMaxL, bank.SlopeL);
			mSlopeMaxR = Math.Max(mSlopeMaxR, bank.SlopeR);
		}
	}
}
