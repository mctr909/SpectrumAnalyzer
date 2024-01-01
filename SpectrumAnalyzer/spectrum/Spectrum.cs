using System;

public class Spectrum {
	public const int TONE_DIV = 4;
	public const int TONE_DIV_CENTER = TONE_DIV / 2;
	public const int OCT_DIV = TONE_DIV * 12;

	const int LOW_FREQ = 55;
	const int MID_FREQ = 180;
	const int THRESHOLD_WIDE = TONE_DIV * 6;
	const int THRESHOLD_NARROW = TONE_DIV;
	const double THRESHOLD_GAIN_LOW = 1.059;
	const double THRESHOLD_GAIN_MID = 1.122;
	const double RMS_MIN = 1e-6;
	const double NARROW_WIDTH = 1.0;
	const double NARROW_WIDTH_AT_FREQ = 660.0;

	readonly int SAMPLE_RATE;
	readonly int BANK_COUNT;
	readonly int LOW_TONE;
	readonly int MID_TONE;

	double mMaxL;
	double mMaxR;
	double mResponceSpeed;

	public class BPFBank {
		public double KB0;
		public double KA2;
		public double KA1;
		public double Lb2;
		public double Lb1;
		public double La2;
		public double La1;
		public double Rb2;
		public double Rb1;
		public double Ra2;
		public double Ra1;

		public double SIGMA;
		public double LPower;
		public double RPower;
		public double SIGMA_DISP;
		public double LPowerDisp;
		public double RPowerDisp;

		public double LPeak;
		public double RPeak;
		public double DELTA;
	}
	public BPFBank[] Banks { get; private set; }

	unsafe delegate void DSetRms(float *pInput, int sampleCount);
	DSetRms mCalc;

	public double Transpose { get; set; } = 0;
	public double Pitch { get; set; } = 1.0;
	public static bool AutoGain { get; set; } = true;
	public static bool NormGain { get; set; } = false;

	public double[] Peak { get; private set; }
	public double[] Curve { get; private set; }
	public double[] Threshold { get; private set; }

	public Spectrum(int sampleRate, double baseFrequency, int tones, bool stereo) {
		SAMPLE_RATE = sampleRate;
		BANK_COUNT = tones * TONE_DIV;
		LOW_TONE = (int)(OCT_DIV * Math.Log(LOW_FREQ / baseFrequency, 2));
		MID_TONE = (int)(OCT_DIV * Math.Log(MID_FREQ / baseFrequency, 2));
		Peak = new double[BANK_COUNT];
		Curve = new double[BANK_COUNT];
		Threshold = new double[BANK_COUNT];
		mMaxL = RMS_MIN;
		mMaxR = RMS_MIN;
		Banks = new BPFBank[BANK_COUNT];
		for (int b = 0; b < BANK_COUNT; ++b) {
			var frequency = baseFrequency * Math.Pow(2.0, (b - 0.5 * TONE_DIV) / OCT_DIV);
			Banks[b] = new BPFBank();
			SetBPF(Banks[b], frequency);
		}
		SetResponceSpeed(16);
		if (stereo) {
			unsafe { mCalc = CalcStereo; }
		}
		else {
			unsafe { mCalc = CalcMono; }
		}
	}

	public double GetResponceSpeed() {
		return mResponceSpeed;
	}

	public void SetResponceSpeed(double frequency) {
		mResponceSpeed = frequency;
		for (int b = 0; b < BANK_COUNT; ++b) {
			var bank = Banks[b];
			var limitFreq = bank.DELTA * SAMPLE_RATE / 2;
			if (frequency > limitFreq) {
				bank.SIGMA_DISP = GetAlpha(SAMPLE_RATE / 4, limitFreq);
			}
			else {
				bank.SIGMA_DISP = GetAlpha(SAMPLE_RATE / 4, frequency);
			}
		}
	}

	void SetBPF(BPFBank bank, double frequency) {
		bank.DELTA = frequency / SAMPLE_RATE;
		bank.SIGMA = GetAlpha(SAMPLE_RATE / 2, frequency);
		var alpha = GetAlpha(SAMPLE_RATE, frequency);
		var a0 = 1.0 + alpha;
		bank.KB0 = alpha / a0;
		bank.KA1 = -2.0 * Math.Cos(2 * Math.PI * bank.DELTA) / a0;
		bank.KA2 = (1.0 - alpha) / a0;
	}

	double GetAlpha(int sampleRate, double frequency) {
		var halfToneWidth = NARROW_WIDTH + Math.Log(NARROW_WIDTH_AT_FREQ / frequency, 2.0);
		if (halfToneWidth < NARROW_WIDTH) {
			halfToneWidth = NARROW_WIDTH;
		}
		var omega = 2 * Math.PI * frequency / sampleRate;
		var s = Math.Sin(omega);
		var x = Math.Log(2) / 4 * halfToneWidth / 12.0 * omega / s;
		var a = s * Math.Sinh(x);
		if (a > 1) {
			return 1;
		}
		return a;
	}

	public unsafe void Calc(float* pInput, int sampleCount) {
		if (NormGain) {
			mMaxL = RMS_MIN;
			mMaxR = RMS_MIN;
		}
		if (AutoGain) {
			var autoGainAttenuation = (double)sampleCount / SAMPLE_RATE;
			mMaxL += (RMS_MIN - mMaxL) * autoGainAttenuation;
			mMaxR += (RMS_MIN - mMaxR) * autoGainAttenuation;
		}
		mCalc(pInput, sampleCount);
		if (!(AutoGain || NormGain)) {
			mMaxL = 1;
			mMaxR = 1;
		}

		var lastL = 0.0;
		var lastR = 0.0;
		var lastIndexL = -1;
		var lastIndexR = -1;
		var lastDisplay = 0.0;
		var lastDisplayIndex = -1;
		for (int idxB = 0; idxB < BANK_COUNT; ++idxB) {
			/* Calc threshold */
			var thL = 0.0;
			var thR = 0.0;
			var thDisplayL = 0.0;
			var thDisplayR = 0.0;
			{
				int width;
				double gain;
				var transposeB = idxB + Transpose * TONE_DIV;
				if (transposeB < LOW_TONE) {
					width = THRESHOLD_WIDE;
					gain = THRESHOLD_GAIN_LOW;
				}
				else if (transposeB < MID_TONE) {
					var a2b = (double)(transposeB - LOW_TONE) / (MID_TONE - LOW_TONE);
					width = (int)(THRESHOLD_NARROW * a2b + THRESHOLD_WIDE * (1 - a2b));
					gain = THRESHOLD_GAIN_MID * a2b + THRESHOLD_GAIN_LOW * (1 - a2b);
				}
				else {
					width = THRESHOLD_NARROW;
					gain = THRESHOLD_GAIN_MID;
				}
				for (int w = -width; w <= width; ++w) {
					var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, idxB + w));
					var b = Banks[bw];
					thL += b.LPower;
					thR += b.RPower;
					thDisplayL += b.LPowerDisp;
					thDisplayR += b.RPowerDisp;
				}
				width = 1 + width << 1;
				thL /= width;
				thR /= width;
				thDisplayL /= width * mMaxL;
				thDisplayR /= width * mMaxR;
				thL = gain * Math.Sqrt(thL);
				thR = gain * Math.Sqrt(thR);
				thDisplayL = gain * Math.Sqrt(Math.Max(thDisplayL, thDisplayR));
			}
			/* Set peak */
			var bank = Banks[idxB];
			bank.LPeak = 0.0;
			bank.RPeak = 0.0;
			var l = Math.Sqrt(bank.LPower);
			var r = Math.Sqrt(bank.RPower);
			if (l < thL) {
				if (0 <= lastIndexL) {
					Banks[lastIndexL].LPeak = lastL;
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
					Banks[lastIndexR].RPeak = lastR;
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
				bank.LPowerDisp / mMaxL,
				bank.RPowerDisp / mMaxR
			));
			Peak[idxB] = 0.0;
			Curve[idxB] = display;
			Threshold[idxB] = thDisplayL;
			if (display < thDisplayL) {
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
			Banks[lastIndexL].LPeak = lastL;
		}
		if (0 <= lastIndexR) {
			Banks[lastIndexR].RPeak = lastR;
		}
	}

	unsafe void CalcMono(float *pInput, int sampleCount) {
		for (int b = 0; b < BANK_COUNT; ++b) {
			var pWave = pInput;
			var bank = Banks[b];
			for (int s = 0; s < sampleCount; ++s) {
				var lb0 = *pWave++;
				var la0
					= bank.KB0 * (lb0 - bank.Lb2)
					- bank.KA2 * bank.La2
					- bank.KA1 * bank.La1
				;
				bank.Lb2 = bank.Lb1;
				bank.Lb1 = lb0;
				bank.La2 = bank.La1;
				bank.La1 = la0;
				la0 *= la0;
				bank.LPower += (la0 - bank.LPower) * bank.SIGMA;
				bank.LPowerDisp += (la0 - bank.LPowerDisp) * bank.SIGMA_DISP;
			}
			mMaxL = Math.Max(mMaxL, bank.LPowerDisp);
			bank.RPower = bank.LPower;
			bank.RPowerDisp = bank.LPowerDisp;
		}
		mMaxR = mMaxL;
	}

	unsafe void CalcStereo(float* pInput, int sampleCount) {
		for (int b = 0; b < BANK_COUNT; ++b) {
			var pWave = pInput;
			var bank = Banks[b];
			for (int s = 0; s < sampleCount; ++s) {
				var lb0 = *pWave++;
				var rb0 = *pWave++;
				var la0
					= bank.KB0 * (lb0 - bank.Lb2)
					- bank.KA2 * bank.La2
					- bank.KA1 * bank.La1
				;
				var ra0
					= bank.KB0 * (rb0 - bank.Rb2)
					- bank.KA2 * bank.Ra2
					- bank.KA1 * bank.Ra1
				;
				bank.Lb2 = bank.Lb1;
				bank.Lb1 = lb0;
				bank.La2 = bank.La1;
				bank.La1 = la0;
				bank.Rb2 = bank.Rb1;
				bank.Rb1 = rb0;
				bank.Ra2 = bank.Ra1;
				bank.Ra1 = ra0;
				la0 *= la0;
				ra0 *= ra0;
				bank.LPower += (la0 - bank.LPower) * bank.SIGMA;
				bank.RPower += (ra0 - bank.RPower) * bank.SIGMA;
				bank.LPowerDisp += (la0 - bank.LPowerDisp) * bank.SIGMA_DISP;
				bank.RPowerDisp += (ra0 - bank.RPowerDisp) * bank.SIGMA_DISP;
			}
			mMaxL = Math.Max(mMaxL, bank.LPowerDisp);
			mMaxR = Math.Max(mMaxR, bank.RPowerDisp);
		}
	}
}
