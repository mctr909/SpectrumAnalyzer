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

		public double sum;
		public double attenuation;
		public double gain;
		public double power;
	}

	const double GAIN_MIN = 1.0 / 10000.0;
	const int TONE_DIV = 3;
	const int TONE_DIV_CENTER = 1;
	const int AVG_WIDTH_WIDE = TONE_DIV * 6;
	const int AVG_WIDTH_NARROW = TONE_DIV;

	readonly double FREQ_TO_OMEGA;
	readonly double GAIN_ATTENUATION;
	readonly double RESPONSE_SPEED_MAX;
	readonly int SAMPLERATE;
	readonly int MID_BEGIN;
	readonly BANK[] mBanks;
	double mMax;

	public readonly int Count;

	public double[] Slope { get; private set; }
	public double[] Peak { get; private set; }
	public double[] Average { get; private set; }
	public double Gain { get { return Math.Sqrt(mMax); } }
	public int Transpose { get; set; }
	public bool AutoGain { get; set; } = true;

	public Spectrum(int sampleRate, double baseFreq, int notes) {
		FREQ_TO_OMEGA = 8.0 * Math.Atan(1.0) / sampleRate;
		GAIN_ATTENUATION = 1.0 - 50 * FREQ_TO_OMEGA;
		RESPONSE_SPEED_MAX = sampleRate / 2.0;
		SAMPLERATE = sampleRate;
		int octDiv = TONE_DIV * 12;
		MID_BEGIN = (int)(octDiv * 4.0);
		Count = TONE_DIV * notes;
		Slope = new double[Count];
		Peak = new double[Count];
		Average = new double[Count];
		mMax = GAIN_MIN;
		mBanks = new BANK[Count];
		for (int b = 0, n = 0; b < Count; b += TONE_DIV, n++) {
			var freqN = baseFreq * Math.Pow(2.0, n / 12.0);
			for (var d = 0; d < TONE_DIV; ++d) {
				var freqW = freqN * Math.Pow(2.0, (double)(d - TONE_DIV_CENTER) / octDiv);
				var width = Math.Log(2000.0 / freqW, 2.0);
				if (width < 1.0) {
					width = 1.0;
				}
				mBanks[b + d] = new BANK();
				Bandpass(mBanks[b + d], freqW, width / 12.0);
			}
		}
	}

	void Bandpass(BANK bank, double freq, double width) {
		var omega = freq * FREQ_TO_OMEGA;
		var x = Math.Log(2.0) / 4.0 * width * omega / Math.Sin(omega);
		var alpha = Math.Sin(omega) * (Math.Exp(x) - Math.Exp(-x)) / 2.0;
		var a0 = 1.0 + alpha;
		bank.a1 = -2.0 * Math.Cos(omega) / a0;
		bank.a2 = (1.0 - alpha) / a0;
		bank.b0 = alpha / a0;
		bank.b1 = 0.0;
		bank.b2 = -alpha / a0;
		var responseSpeed = freq;
		if (responseSpeed < 5.0) {
			responseSpeed = 5.0;
		}
		if (RESPONSE_SPEED_MAX < responseSpeed) {
			responseSpeed = RESPONSE_SPEED_MAX;
		}
		bank.attenuation = 1.0 - responseSpeed / SAMPLERATE;
		bank.gain = 1.0 / bank.attenuation - 1.0;
	}

	public void SetLevel(short[] inputWave) {
		mMax = Math.Max(mMax * GAIN_ATTENUATION, GAIN_MIN);
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
			mMax = Math.Max(mMax, bank.power);
		}
		if (!AutoGain) {
			mMax = 1.0;
		}
		var lastPeak = 0.0;
		var lastPeakIndex = -1;
		for (int b = 0; b < Count; ++b) {
			int avgWidth;
			if (b + Transpose < MID_BEGIN) {
				avgWidth = AVG_WIDTH_WIDE;
			} else {
				avgWidth = AVG_WIDTH_NARROW;
			}
			var sum = 0.0;
			for (int w = -avgWidth; w <= avgWidth; ++w) {
				var bw = Math.Min(Count - 1, Math.Max(0, b + w));
				sum += mBanks[bw].power;
			}
			sum /= avgWidth * 2 + 1;
			var threshold = Math.Sqrt(sum / mMax) * 1.01;
			sum = 0.0;
			for (int w = -AVG_WIDTH_WIDE; w <= AVG_WIDTH_WIDE; ++w) {
				var bw = Math.Min(Count - 1, Math.Max(0, b + w));
				sum += Math.Sqrt(mBanks[bw].power / mMax);
			}
			Average[b] = sum / (AVG_WIDTH_WIDE * 2 + 1);
			var slope = Math.Sqrt(mBanks[b].power / mMax);
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
