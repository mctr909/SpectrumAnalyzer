using System;
using System.Runtime.InteropServices;

using static Spectrum.Settings;

namespace Spectrum {
	public class Spectrum : IDisposable {
		readonly int SAMPLE_RATE;
		readonly int LOW_TONE;
		readonly int MID_TONE;

		double mAutoGainMaxL;
		double mAutoGainMaxR;
		IntPtr mpFilterBanks;

		internal PeakBank[] PeakBanks { get; private set; }

		public double Transpose { get; set; } = 0.0;
		public double[] Peak { get; private set; }
		public double[] Curve { get; private set; }
		public double[] Threshold { get; private set; }

		public Spectrum(int sampleRate, double baseFrequency) {
			SAMPLE_RATE = sampleRate;
			LOW_TONE = (int)(OCT_DIV * Math.Log(END_LOW_FREQ / baseFrequency, 2));
			MID_TONE = (int)(OCT_DIV * Math.Log(BEGIN_MID_FREQ / baseFrequency, 2));
			Peak = new double[BANK_COUNT];
			Curve = new double[BANK_COUNT];
			Threshold = new double[BANK_COUNT];
			PeakBanks = new PeakBank[BANK_COUNT];
			mAutoGainMaxL = AUTOGAIN_MAX;
			mAutoGainMaxR = AUTOGAIN_MAX;
			mpFilterBanks = Marshal.AllocHGlobal(Marshal.SizeOf<FilterBank>() * BANK_COUNT);
			for (int b = 0; b < BANK_COUNT; ++b) {
				var frequency = baseFrequency * Math.Pow(2.0, (b - 0.5 * HALFTONE_DIV) / OCT_DIV);
				PeakBanks[b] = new PeakBank(frequency / SAMPLE_RATE);
				SetBPF(b, frequency);
				SetSigma(b, frequency);
			}
		}

		public void Dispose() {
			Marshal.FreeHGlobal(mpFilterBanks);
		}

		/// <summary>
		/// スペクトルを更新
		/// </summary>
		/// <param name="pInput">入力バッファ</param>
		/// <param name="sampleCount">入力バッファのサンプル数</param>
		public void Update(IntPtr pInput, int sampleCount) {
			/* 表示値を正規化する場合、最大値をクリア */
			if (NormGain) {
				mAutoGainMaxL = AUTOGAIN_MAX;
				mAutoGainMaxR = AUTOGAIN_MAX;
			}
			/* 表示値を自動調整する場合、最大値を減衰 */
			if (AutoGain) {
				var autoGainAttenuation = 4.0 * AUTOGAIN_SPEED / SAMPLE_RATE * sampleCount;
				mAutoGainMaxL += (AUTOGAIN_MAX - mAutoGainMaxL) * autoGainAttenuation;
				mAutoGainMaxR += (AUTOGAIN_MAX - mAutoGainMaxR) * autoGainAttenuation;
			}
			/* パワースペクトルを算出 */
			CalcPower(pInput, sampleCount);
			/* 表示値の正規化/自動調整をしない場合、等倍とする */
			if (!(AutoGain || NormGain)) {
				mAutoGainMaxL = 1;
				mAutoGainMaxR = 1;
			}
			/* ピークを抽出 */
			ExtractPeak();
		}

		static double GetAlpha(double sampleRate, double frequency) {
			var halfToneWidth = 1.0 + Math.Log(FREQ_AT_HALFTONE_WIDTH / frequency, 2.0);
			if (halfToneWidth < 1.0) {
				halfToneWidth = 1.0;
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

		unsafe void SetBPF(int index, double frequency) {
			var omega = 2 * Math.PI * frequency / SAMPLE_RATE;
			var alpha = GetAlpha(SAMPLE_RATE, frequency);
			var a0 = 1.0 + alpha;
			var pBank = (FilterBank*)mpFilterBanks + index;
			Marshal.StructureToPtr(new FilterBank(), (IntPtr)pBank, true);
			pBank->KB0 = alpha / a0;
			pBank->KA1 = -2.0 * Math.Cos(omega) / a0;
			pBank->KA2 = (1.0 - alpha) / a0;
		}

		unsafe void SetSigma(int index, double frequency) {
			var sampleOmega = SAMPLE_RATE / (2 * Math.PI);
			var limitFreq = frequency / 2;
			var pBank = (FilterBank*)mpFilterBanks + index;
			pBank->SIGMA = GetAlpha(sampleOmega, frequency);
			pBank->SIGMA_DISP = GetAlpha(sampleOmega, (DISP_SPEED > limitFreq) ? limitFreq : DISP_SPEED);
		}

		unsafe void CalcPower(IntPtr pInput, int sampleCount) {
			var pBank = (FilterBank*)mpFilterBanks;
			for (int b = 0; b < BANK_COUNT; ++b) {
				var pWave = (float*)pInput;
				for (int s = 0; s < sampleCount; ++s) {
					{
						/* 帯域通過フィルタに通す */
						var b0 = *pWave++;
						var a0 = pBank->KB0;
						a0 *= b0 - pBank->Lb2;
						pBank->Lb2 = pBank->Lb1;
						pBank->Lb1 = b0;
						a0 -= pBank->KA2 * pBank->La2;
						pBank->La2 = pBank->La1;
						a0 -= pBank->KA1 * pBank->La1;
						pBank->La1 = a0;
						/* パワースペクトルを得る */
						a0 *= a0;
						pBank->LPower += (a0 - pBank->LPower) * pBank->SIGMA;
						pBank->LPowerDisp += (a0 - pBank->LPowerDisp) * pBank->SIGMA_DISP;
					}
					{
						/* 帯域通過フィルタに通す */
						var b0 = *pWave++;
						var a0 = pBank->KB0;
						a0 *= b0 - pBank->Rb2;
						pBank->Rb2 = pBank->Rb1;
						pBank->Rb1 = b0;
						a0 -= pBank->KA2 * pBank->Ra2;
						pBank->Ra2 = pBank->Ra1;
						a0 -= pBank->KA1 * pBank->Ra1;
						pBank->Ra1 = a0;
						/* パワースペクトルを得る */
						a0 *= a0;
						pBank->RPower += (a0 - pBank->RPower) * pBank->SIGMA;
						pBank->RPowerDisp += (a0 - pBank->RPowerDisp) * pBank->SIGMA_DISP;
					}
				}
				mAutoGainMaxL = Math.Max(mAutoGainMaxL, pBank->LPowerDisp);
				mAutoGainMaxR = Math.Max(mAutoGainMaxR, pBank->RPowerDisp);
				pBank++;
			}
		}

		unsafe void ExtractPeak() {
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
						var b = *((FilterBank*)mpFilterBanks + bw);
						thL += b.LPower;
						thR += b.RPower;
						thDisplayL += b.LPowerDisp;
						thDisplayR += b.RPowerDisp;
					}
					width = width * 2 + 1;
					thL /= width;
					thR /= width;
					thDisplayL /= width * mAutoGainMaxL;
					thDisplayR /= width * mAutoGainMaxR;
					/* パワー⇒リニア変換後、閾値ゲインを掛ける */
					thL = Math.Sqrt(thL) * gain;
					thR = Math.Sqrt(thR) * gain;
					Threshold[idxB] = Math.Sqrt(Math.Max(thDisplayL, thDisplayR)) * gain;
				}
				var bank = *((FilterBank*)mpFilterBanks + idxB);
				/* 波形合成用のピークを抽出 */
				{
					var spec = PeakBanks[idxB];
					spec.L = 0.0;
					spec.R = 0.0;
					var linearL = Math.Sqrt(bank.LPower);
					var linearR = Math.Sqrt(bank.RPower);
					if (linearL < thL) {
						if (0 <= lastIndexL) {
							PeakBanks[lastIndexL].L = lastL;
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
							PeakBanks[lastIndexR].R = lastR;
						}
						linearR = 0.0;
						lastR = 0.0;
						lastIndexR = -1;
					}
					if (lastR < linearR) {
						lastR = linearR;
						lastIndexR = idxB;
					}
				}
				/* 表示用のピーク/曲線を設定 */
				{
					var linearDisplay = Math.Sqrt(Math.Max(
						bank.LPowerDisp / mAutoGainMaxL,
						bank.RPowerDisp / mAutoGainMaxR
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
			}
			if (0 <= lastIndexL) {
				PeakBanks[lastIndexL].L = lastL;
			}
			if (0 <= lastIndexR) {
				PeakBanks[lastIndexR].R = lastR;
			}
			if (0 <= lastDisplayIndex) {
				Peak[lastDisplayIndex] = lastDisplay;
			}
		}
	}
}
