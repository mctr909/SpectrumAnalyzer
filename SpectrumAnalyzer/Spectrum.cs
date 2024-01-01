using System;

public class Spectrum {
	class BANK {
		public double Kb0;
		public double Ka1;
		public double Ka2;

		public double RmsDelta;

		public double La1;
		public double La2;
		public double Lb1;
		public double Lb2;
		public double RmsL;

		public double Ra1;
		public double Ra2;
		public double Rb1;
		public double Rb2;
		public double RmsR;

		public static BANK Bandpass(int sampleRate, double frequency, double width) {
			var omega = 8.0 * Math.Atan(1.0) * frequency / sampleRate;
			var x = Math.Log(2.0) / 4.0 * width * omega / Math.Sin(omega);
			var alpha = Math.Sin(omega) * Math.Sinh(x);
			var a0 = 1.0 + alpha;
			var bank = new BANK() {
				Kb0 = alpha / a0,
				Ka1 = -2.0 * Math.Cos(omega) / a0,
				Ka2 = (1.0 - alpha) / a0,
			};
			if (2000 < frequency) {
				bank.RmsDelta = 6000.0 / sampleRate;
			} else {
				bank.RmsDelta = 3 * frequency / sampleRate;
			}
			return bank;
		}
	}

	public const int TONE_DIV = 3;
	public const int TONE_DIV_CENTER = 1;

	const int WIDTH_LOW = TONE_DIV * 23;
	const int OCT_DIV = TONE_DIV * 12;
	const int HIGH_TONE = TONE_DIV * 90;
	const double GAIN_MIN = 1e-5;

	readonly double ATTENUATION_DELTA;
	readonly BANK[] mBanks;

	public readonly int Count;

	delegate void DSetRms(IntPtr pInput, int sampleCount);
	DSetRms mfSetRms;

	double mMaxRmsL;
	double mMaxRmsR;

	public static int ThresholdWidth { get; set; } = TONE_DIV * 2;
	public static int Transpose { get; set; } = 0;
	public static bool AutoGain { get; set; } = true;
	public static bool NormGain { get; set; } = false;

	public double[] SlopeL { get; private set; }
	public double[] SlopeR { get; private set; }
	public double[] PeakL { get; private set; }
	public double[] PeakR { get; private set; }
	public double[] ThresholdL { get; private set; }
	public double[] ThresholdR { get; private set; }
	public double GainL { get { return Math.Sqrt(mMaxRmsL); } }
	public double GainR { get { return Math.Sqrt(mMaxRmsR); } }

	public Spectrum(int sampleRate, double baseFrequency, int tones, int bufferSamples, bool stereo) {
		ATTENUATION_DELTA = 4.0 * bufferSamples / sampleRate;
		Count = tones * TONE_DIV;
		SlopeL = new double[Count];
		SlopeR = new double[Count];
		PeakL = new double[Count];
		PeakR = new double[Count];
		ThresholdL = new double[Count];
		ThresholdR = new double[Count];
		mMaxRmsL = GAIN_MIN;
		mMaxRmsR = GAIN_MIN;
		mBanks = new BANK[Count];
		for (int b = 0; b < Count; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				var frequency = baseFrequency * Math.Pow(2.0, (double)(bd - TONE_DIV_CENTER) / OCT_DIV);
				var width = Math.Log(1000.0 / frequency, 2.0);
				if (width < 1.0) {
					width = 1.0;
				}
				mBanks[bd] = BANK.Bandpass(sampleRate, frequency, width / 12.0);
			}
		}
		if (stereo) {
			mfSetRms = SetRmsStereo;
		} else {
			mfSetRms = SetRmsMono;
		}
	}

	public void SetLevel(IntPtr pInput, int sampleCount) {
		if (NormGain) {
			mMaxRmsL = GAIN_MIN;
			mMaxRmsR = GAIN_MIN;
		}
		if (AutoGain) {
			mMaxRmsL += (GAIN_MIN - mMaxRmsL) * ATTENUATION_DELTA;
			mMaxRmsR += (GAIN_MIN - mMaxRmsR) * ATTENUATION_DELTA;
		}
		mfSetRms(pInput, sampleCount);
		if (!AutoGain && !NormGain) {
			mMaxRmsL = 1.0;
			mMaxRmsR = 1.0;
		}
		var lastPeakL = 0.0;
		var lastPeakR = 0.0;
		var lastPeakIndexL = -1;
		var lastPeakIndexR = -1;
		var highTone = HIGH_TONE + Transpose;
		for (int b = 0; b < Count; ++b) {
			/* Calc threshold */
			int width;
			if (b < highTone) {
				var a2b = (double)b / highTone;
				width = (int)(ThresholdWidth * a2b + WIDTH_LOW * (1 - a2b));
			} else {
				width = ThresholdWidth;
			}
			var thresholdL = 0.0;
			var thresholdR = 0.0;
			for (int w = -width; w <= width; ++w) {
				var bw = Math.Min(Count - 1, Math.Max(0, b + w));
				thresholdL += mBanks[bw].RmsL;
				thresholdR += mBanks[bw].RmsR;
			}
			thresholdL /= width * 2 + 1;
			thresholdR /= width * 2 + 1;
			thresholdL = Math.Sqrt(thresholdL / mMaxRmsL);
			thresholdR = Math.Sqrt(thresholdR / mMaxRmsR);
			ThresholdL[b] = thresholdL;
			ThresholdR[b] = thresholdR;
			/* Set slope */
			var slopeL = Math.Sqrt(mBanks[b].RmsL / mMaxRmsL);
			var slopeR = Math.Sqrt(mBanks[b].RmsR / mMaxRmsR);
			SlopeL[b] = slopeL;
			SlopeR[b] = slopeR;
			/* Set peak */
			PeakL[b] = 0.0;
			PeakR[b] = 0.0;
			if (slopeL < thresholdL) {
				if (0 <= lastPeakIndexL) {
					PeakL[lastPeakIndexL] = lastPeakL;
				}
				slopeL = 0.0;
				lastPeakL = 0.0;
				lastPeakIndexL = -1;
			}
			if (lastPeakL < slopeL) {
				lastPeakL = slopeL;
				lastPeakIndexL = b;
			}
			if (slopeR < thresholdR) {
				if (0 <= lastPeakIndexR) {
					PeakR[lastPeakIndexR] = lastPeakR;
				}
				slopeR = 0.0;
				lastPeakR = 0.0;
				lastPeakIndexR = -1;
			}
			if (lastPeakR < slopeR) {
				lastPeakR = slopeR;
				lastPeakIndexR = b;
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
		for (int b = 0; b < Count; ++b) {
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
				bank.RmsL += (output * output - bank.RmsL) * bank.RmsDelta;
			}
			mMaxRmsL = Math.Max(mMaxRmsL, bank.RmsL);
			bank.RmsR = bank.RmsL;
		}
		mMaxRmsR = mMaxRmsL;
	}

	unsafe void SetRmsStereo(IntPtr pInput, int sampleCount) {
		var inputWave = (short*)pInput;
		for (int b = 0; b < Count; ++b) {
			var bank = mBanks[b];
			for (int t = 0, i=0; t < sampleCount; ++t, i+=2) {
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
				bank.RmsL += (output * output - bank.RmsL) * bank.RmsDelta;
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
				bank.RmsR += (output * output - bank.RmsR) * bank.RmsDelta;
			}
			mMaxRmsL = Math.Max(mMaxRmsL, bank.RmsL);
			mMaxRmsR = Math.Max(mMaxRmsR, bank.RmsR);
		}
	}
}
