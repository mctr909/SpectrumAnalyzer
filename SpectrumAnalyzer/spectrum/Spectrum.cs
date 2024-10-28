using System;

public class Spectrum {
	public const int HALFTONE_DIV = 4;
	public const int HALFTONE_DIV_CENTER = HALFTONE_DIV / 2;
	public const int OCT_DIV = HALFTONE_DIV * 12;

	public readonly int TONE_COUNT;

	const int LOW_FREQ_MAX = 80;
	const int MID_FREQ_MIN = 220;
	const int THRESHOLD_WIDTH_LOW = HALFTONE_DIV * 5; // ±5半音
	const int THRESHOLD_WIDTH_MID = HALFTONE_DIV;     // ±1半音
	const double THRESHOLD_GAIN_LOW = 1.259; // +2.0db {10^(2.0/20)}
	const double THRESHOLD_GAIN_MID = 1.012; // +0.1db {10^(0.1/20)}
	const double POWER_MIN = 3.981e-03;      //  -24db {10^(-24/10)}
	const double NARROW_WIDTH = 1.0;            // 1半音
	const double NARROW_WIDTH_AT_FREQ = 1000.0; // 1半音@1kHz

	const double DISP_FREQ = 100;

	readonly int SAMPLE_RATE;
	readonly int BANK_COUNT;
	readonly int LOW_TONE;
	readonly int MID_TONE;

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
		public double PowerL;
		public double PowerR;
		public double DISP_SIGMA;
		public double DispPowerL;
		public double DispPowerR;

		public double PeakL;
		public double PeakR;
		public double DELTA;
	}

	double DispMaxL;
	double DispMaxR;

	public static bool AutoGain { get; set; } = true;
	public static bool NormGain { get; set; } = false;

	public BPFBank[] Banks { get; private set; }
	public double[] Peak { get; private set; }
	public double[] Curve { get; private set; }
	public double[] Threshold { get; private set; }
	public double Transpose { get; set; } = 0;
	public double Pitch { get; set; } = 1.0;

	delegate void DCalcPower(IntPtr pInput, int sampleCount);
	DCalcPower CalcPower;

	public Spectrum(int sampleRate, double baseFrequency, int tones, bool stereo) {
		SAMPLE_RATE = sampleRate;
		TONE_COUNT = tones;
		BANK_COUNT = tones * HALFTONE_DIV;
		LOW_TONE = (int)(OCT_DIV * Math.Log(LOW_FREQ_MAX / baseFrequency, 2));
		MID_TONE = (int)(OCT_DIV * Math.Log(MID_FREQ_MIN / baseFrequency, 2));
		Peak = new double[BANK_COUNT];
		Curve = new double[BANK_COUNT];
		Threshold = new double[BANK_COUNT];
		DispMaxL = POWER_MIN;
		DispMaxR = POWER_MIN;
		Banks = new BPFBank[BANK_COUNT];
		for (int b = 0; b < BANK_COUNT; ++b) {
			var frequency = baseFrequency * Math.Pow(2.0, (b - 0.5 * HALFTONE_DIV) / OCT_DIV);
			Banks[b] = new BPFBank();
			SetBPF(Banks[b], frequency);
			SetSigma(Banks[b], frequency);
		}
		if (stereo) {
			CalcPower = CalcPowerStereo;
		}
		else {
			CalcPower = CalcPowerMono;
		}
	}

	double GetAlpha(double sampleRate, double frequency) {
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

	/// <summary>
	/// 帯域通過フィルタの係数を設定
	/// </summary>
	/// <param name="bank">フィルタバンク</param>
	/// <param name="frequency">通過周波数</param>
	void SetBPF(BPFBank bank, double frequency) {
		var omega = 2 * Math.PI * frequency / SAMPLE_RATE;
		var alpha = GetAlpha(SAMPLE_RATE, frequency);
		var a0 = 1.0 + alpha;
		bank.KB0 = alpha / a0;
		bank.KA1 = -2.0 * Math.Cos(omega) / a0;
		bank.KA2 = (1.0 - alpha) / a0;
	}

	/// <summary>
	/// スペクトルの応答速度を設定
	/// </summary>
	/// <param name="bank">フィルタバンク</param>
	/// <param name="frequency">応答周波数</param>
	void SetSigma(BPFBank bank, double frequency) {
		var sampleOmega = SAMPLE_RATE / (2 * Math.PI);
		var limitFreq = frequency / 2;
		bank.DELTA = frequency / SAMPLE_RATE;
		bank.SIGMA = GetAlpha(sampleOmega, frequency);
		bank.DISP_SIGMA = GetAlpha(sampleOmega, (DISP_FREQ > limitFreq) ? limitFreq : DISP_FREQ);
	}

	/// <summary>
	/// パワースペクトルを算出
	/// </summary>
	/// <param name="pInput">入力バッファ(モノラル)</param>
	/// <param name="sampleCount">入力バッファのサンプル数</param>
	unsafe void CalcPowerMono(IntPtr pInput, int sampleCount) {
		for (int b = 0; b < BANK_COUNT; ++b) {
			var pWave = (float*)pInput;
			var bank = Banks[b];
			for (int s = 0; s < sampleCount; ++s) {
				/* 帯域通過フィルタに通す */
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
				/* パワースペクトルを得る */
				la0 *= la0;
				bank.PowerL += (la0 - bank.PowerL) * bank.SIGMA;
				bank.DispPowerL += (la0 - bank.DispPowerL) * bank.DISP_SIGMA;
			}
			DispMaxL = Math.Max(DispMaxL, bank.DispPowerL);
			bank.PowerR = bank.PowerL;
			bank.DispPowerR = bank.DispPowerL;
		}
		DispMaxR = DispMaxL;
	}

	/// <summary>
	/// パワースペクトルを算出
	/// </summary>
	/// <param name="pInput">入力バッファ(ステレオ)</param>
	/// <param name="sampleCount">入力バッファのサンプル数</param>
	unsafe void CalcPowerStereo(IntPtr pInput, int sampleCount) {
		for (int b = 0; b < BANK_COUNT; ++b) {
			var pWave = (float*)pInput;
			var bank = Banks[b];
			for (int s = 0; s < sampleCount; ++s) {
				/* 帯域通過フィルタに通す */
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
				/* パワースペクトルを得る */
				la0 *= la0;
				ra0 *= ra0;
				bank.PowerL += (la0 - bank.PowerL) * bank.SIGMA;
				bank.PowerR += (ra0 - bank.PowerR) * bank.SIGMA;
				bank.DispPowerL += (la0 - bank.DispPowerL) * bank.DISP_SIGMA;
				bank.DispPowerR += (ra0 - bank.DispPowerR) * bank.DISP_SIGMA;
			}
			DispMaxL = Math.Max(DispMaxL, bank.DispPowerL);
			DispMaxR = Math.Max(DispMaxR, bank.DispPowerR);
		}
	}

	/// <summary>
	/// スペクトルを更新
	/// </summary>
	/// <param name="pInput">入力バッファ</param>
	/// <param name="sampleCount">入力バッファのサンプル数</param>
	public void Update(IntPtr pInput, int sampleCount) {
		/* 表示値を正規化する場合、最大値をクリア */
		if (NormGain) {
			DispMaxL = POWER_MIN;
			DispMaxR = POWER_MIN;
		}
		/* 表示値を自動調整する場合、最大値を減衰 */
		if (AutoGain) {
			var autoGainAttenuation = (double)sampleCount / SAMPLE_RATE;
			DispMaxL += (POWER_MIN - DispMaxL) * autoGainAttenuation;
			DispMaxR += (POWER_MIN - DispMaxR) * autoGainAttenuation;
		}
		/* パワースペクトルを算出 */
		CalcPower(pInput, sampleCount);
		/* 表示値の正規化/自動調整をしない場合、等倍とする */
		if (!(AutoGain || NormGain)) {
			DispMaxL = 1;
			DispMaxR = 1;
		}
		var lastL = 0.0;
		var lastR = 0.0;
		var lastIndexL = -1;
		var lastIndexR = -1;
		var lastDisplay = 0.0;
		var lastDisplayIndex = -1;
		for (int idxB = 0; idxB < BANK_COUNT; ++idxB) {
			/* ピーク抽出用の閾値を算出 */
			var thL = 0.0;
			var thR = 0.0;
			{
				/* 閾値幅と閾値ゲインを設定 */
				int width;
				double gain;
				var transposedB = idxB + Transpose * HALFTONE_DIV;
				if (transposedB < LOW_TONE) {
					width = THRESHOLD_WIDTH_LOW;
					gain = THRESHOLD_GAIN_LOW;
				}
				else if (transposedB < MID_TONE) {
					var a2b = (double)(transposedB - LOW_TONE) / (MID_TONE - LOW_TONE);
					width = (int)(THRESHOLD_WIDTH_MID * a2b + THRESHOLD_WIDTH_LOW * (1 - a2b));
					gain = THRESHOLD_GAIN_MID * a2b + THRESHOLD_GAIN_LOW * (1 - a2b);
				}
				else {
					width = THRESHOLD_WIDTH_MID;
					gain = THRESHOLD_GAIN_MID;
				}
				/* 閾値幅で指定される範囲の平均値を閾値にする */
				var thDisplayL = 0.0;
				var thDisplayR = 0.0;
				for (int w = -width; w <= width; ++w) {
					var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, idxB + w));
					var b = Banks[bw];
					thL += b.PowerL;
					thR += b.PowerR;
					thDisplayL += b.DispPowerL;
					thDisplayR += b.DispPowerR;
				}
				width = width * 2 + 1;
				thL /= width;
				thR /= width;
				thDisplayL /= width * DispMaxL;
				thDisplayR /= width * DispMaxR;
				/* パワー⇒リニア変換後、閾値ゲインを掛ける */
				thL = Math.Sqrt(thL) * gain;
				thR = Math.Sqrt(thR) * gain;
				Threshold[idxB] = Math.Sqrt(Math.Max(thDisplayL, thDisplayR)) * gain;
			}
			/* 波形合成用のピークを抽出 */
			var bank = Banks[idxB];
			bank.PeakL = 0.0;
			bank.PeakR = 0.0;
			var linearL = Math.Sqrt(bank.PowerL);
			var linearR = Math.Sqrt(bank.PowerR);
			if (linearL < thL) {
				if (0 <= lastIndexL) {
					Banks[lastIndexL].PeakL = lastL;
				}
				linearL = 0.0;
				lastL = 0.0;
				lastIndexL = -1;
			}
			if (lastL < linearL) {
				lastL = linearL;
				lastIndexL = idxB;
			}
			if (linearR < thR) {
				if (0 <= lastIndexR) {
					Banks[lastIndexR].PeakR = lastR;
				}
				linearR = 0.0;
				lastR = 0.0;
				lastIndexR = -1;
			}
			if (lastR < linearR) {
				lastR = linearR;
				lastIndexR = idxB;
			}
			/* 表示用のピーク/曲線を設定 */
			var linearDisplay = Math.Sqrt(Math.Max(
				bank.DispPowerL / DispMaxL,
				bank.DispPowerR / DispMaxR
			));
			Peak[idxB] = 0.0;
			Curve[idxB] = linearDisplay;
			if (linearDisplay < Threshold[idxB]) {
				if (0 <= lastDisplayIndex) {
					Peak[lastDisplayIndex] = lastDisplay;
				}
				linearDisplay = 0.0;
				lastDisplay = 0.0;
				lastDisplayIndex = -1;
			}
			if (lastDisplay < linearDisplay) {
				lastDisplay = linearDisplay;
				lastDisplayIndex = idxB;
			}
		}
		if (0 <= lastIndexL) {
			Banks[lastIndexL].PeakL = lastL;
		}
		if (0 <= lastIndexR) {
			Banks[lastIndexR].PeakR = lastR;
		}
		if (0 <= lastDisplayIndex) {
			Peak[lastDisplayIndex] = lastDisplay;
		}
	}
}
