using System;

public class Spectrum {
	class BANK {
		public double a1;
		public double a2;
		public double b0;
		public double b1;
		public double b2;

		public double aDelay1;
		public double aDelay2;
		public double bDelay1;
		public double bDelay2;

		public double attenuation;
		public double gain;
		public double sum;
		public double power;

		public static BANK Bandpass(int sampleRate, double frequency, double width) {
			var omega = 8.0 * Math.Atan(1.0) * frequency / sampleRate;
			var x = Math.Log(2.0) / 4.0 * width * omega / Math.Sin(omega);
			var alpha = Math.Sin(omega) * Math.Sinh(x);
			var a0 = 1.0 + alpha;
			var bank = new BANK() {
				a1 = -2.0 * Math.Cos(omega) / a0,
				a2 = (1.0 - alpha) / a0,
				b0 = alpha / a0,
				b1 = 0.0,
				b2 = -alpha / a0
			};
			var responseSpeed = frequency;
			if (responseSpeed < 5.0) {
				responseSpeed = 5.0;
			}
			if (sampleRate * 0.5 < responseSpeed) {
				responseSpeed = sampleRate * 0.5;
			}
			bank.attenuation = 1.0 - responseSpeed / sampleRate;
			bank.gain = 1.0 / bank.attenuation - 1.0;
			return bank;
		}
	}

	const double GAIN_MIN = 1e-5;
	const int TONE_DIV = 3;
	const int TONE_DIV_CENTER = 1;
	const int OCT_DIV = TONE_DIV * 12;

	readonly double GAIN_ATTENUATION;
	readonly int MID_BEGIN;
	readonly BANK[] mBanks;

	double mMaxPower;

	public readonly int Count;

	public static int ThresholdHigh { get; set; } = TONE_DIV * 1;
	public static int ThresholdLow { get; set; } = TONE_DIV * 7;
	public static double ThresholdOffset { get; set; } = 1.0;
	public static int Transpose { get; set; } = 0;
	public static bool AutoGain { get; set; } = true;
	public static bool NormGain { get; set; } = false;

	public double[] Slope { get; private set; }
	public double[] Peak { get; private set; }
	public double[] Threshold { get; private set; }
	public double Gain {
		get { return Math.Sqrt(mMaxPower); }
		set { mMaxPower = value * value; }
	}

	public Spectrum(int sampleRate, double baseFrequency, int tones) {
		GAIN_ATTENUATION = 1.0 - 8.0 * Math.Atan(1.0) * 100 / sampleRate;
		Count = tones * TONE_DIV;
		Slope = new double[Count];
		Peak = new double[Count];
		Threshold = new double[Count];
		mMaxPower = GAIN_MIN;
		mBanks = new BANK[Count];
		for (int b = 0; b < Count; b += TONE_DIV) {
			for (int d = 0, bd = b; d < TONE_DIV; ++d, ++bd) {
				var frequency = baseFrequency * Math.Pow(2.0, (double)(bd - TONE_DIV_CENTER) / OCT_DIV);
				if (frequency < 220) {
					MID_BEGIN = bd;
				}
				var width = Math.Log(2000.0 / frequency, 2.0);
				if (width < 1.0) {
					width = 1.0;
				}
				mBanks[bd] = BANK.Bandpass(sampleRate, frequency, width / 12.0);
			}
		}
	}

	public void SetLevel(short[] inputWave) {
		if (NormGain) {
			mMaxPower = GAIN_MIN;
		} else {
			mMaxPower = Math.Max(mMaxPower * GAIN_ATTENUATION, GAIN_MIN);
		}
		for (int b = 0; b < Count; ++b) {
			var bank = mBanks[b];
			for (int t = 0; t < inputWave.Length; ++t) {
				var input = inputWave[t] / 32768.0;
				var output
					= bank.b0 * input
					+ bank.b1 * bank.bDelay1
					+ bank.b2 * bank.bDelay2
					- bank.a1 * bank.aDelay1
					- bank.a2 * bank.aDelay2
				;
				bank.aDelay2 = bank.aDelay1;
				bank.aDelay1 = output;
				bank.bDelay2 = bank.bDelay1;
				bank.bDelay1 = input;
				bank.sum += output * output;
				bank.sum *= bank.attenuation;
			}
			bank.power = bank.sum * bank.gain;
			mMaxPower = Math.Max(mMaxPower, bank.power);
		}
		if (!AutoGain && !NormGain) {
			mMaxPower = 1.0;
		}
		var lastPeak = 0.0;
		var lastPeakIndex = -1;
		for (int b = 0; b < Count; ++b) {
			int thresholdWidth;
			double thresholdOffset;
			if (b + Transpose < MID_BEGIN) {
				thresholdWidth = ThresholdLow;
				thresholdOffset = 1.0;
			} else {
				thresholdWidth = ThresholdHigh;
				thresholdOffset = ThresholdOffset;
			}
			var threshold = 0.0;
			for (int w = -thresholdWidth; w <= thresholdWidth; ++w) {
				var bw = Math.Min(Count - 1, Math.Max(0, b + w));
				threshold += mBanks[bw].power;
			}
			threshold /= thresholdWidth * 2;
			threshold = Math.Sqrt(threshold / mMaxPower) * thresholdOffset;
			Threshold[b] = threshold;
			var slope = Math.Sqrt(mBanks[b].power / mMaxPower);
			Slope[b] = slope;
			Peak[b] = 0.0;
			if (slope < threshold) {
				if (0 <= lastPeakIndex) {
					Peak[lastPeakIndex] = lastPeak;
				}
				slope = 0.0;
				lastPeak = 0.0;
				lastPeakIndex = -1;
			}
			if (lastPeak < slope) {
				lastPeak = slope;
				lastPeakIndex = b;
			}
		}
		if (0 <= lastPeakIndex) {
			Peak[lastPeakIndex] = lastPeak;
		}
	}
}
