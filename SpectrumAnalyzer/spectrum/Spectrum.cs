using System;

namespace Spectrum {
	public partial class Spectrum {
		/// <summary>トランスポーズ[半音]</summary>
		public double Transpose { get; set; } = 0.0;

		/// <summary> 最大値 </summary>
		public double Max { get; private set; } = AUTOGAIN_MIN;

		/// <summary> 自動ゲイン </summary>
		public double AutoGain { get; private set; } = AUTOGAIN_MIN;

		/// <summary>表示用ピーク</summary>
		public double[] Peak { get; private set; } = new double[BANK_COUNT];

		/// <summary>表示用曲線</summary>
		public double[] Curve { get; private set; } = new double[BANK_COUNT];

		/// <summary>波形合成用ピーク</summary>
		internal PeakBank[] PeakBanks { get; private set; } = new PeakBank[BANK_COUNT];

		private readonly FilterBank[] FilterBanks = new FilterBank[BANK_COUNT];
		private readonly int SampleRate;

		private class FilterBank {
			public float l_b2;
			public float l_b1;
			public float l_a2;
			public float l_a1;
			public float l_ms;
			public float r_b2;
			public float r_b1;
			public float r_a2;
			public float r_a1;
			public float r_ms;
			public float k_b0;
			public float k_a2;
			public float k_a1;
			public float sigma;

			public FilterBank(int sampleRate, double frequency) {
				var omega = 2 * Math.PI * frequency / sampleRate;
				var alpha = GetAlpha(sampleRate, frequency);
				var a0 = 1.0 + alpha;
				k_b0 = (float)(alpha / a0);
				k_a1 = (float)(-2.0 * Math.Cos(omega) / a0);
				k_a2 = (float)((1.0 - alpha) / a0);
				sigma = (float)GetAlpha(sampleRate / (2 * Math.PI), frequency);
			}

			private static double GetAlpha(double sampleRate, double frequency) {
				var halfToneWidth = 1.0 + Math.Log(FREQ_AT_HALFTONE_WIDTH / frequency, 2.0);
				if (halfToneWidth < 1.0) {
					halfToneWidth = 1.0;
				}
				var omega = 2 * Math.PI * frequency / sampleRate;
				var s = Math.Sin(omega);
				var x = Math.Log(2) / 4 * halfToneWidth / 12.0 * omega / s;
				var a = s * Math.Sinh(x);
				a = Math.Min(1, a);
				return a;
			}
		}

		public Spectrum(int sampleRate) {
			SampleRate = sampleRate;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var freq = BASE_FREQ * Math.Pow(2.0, ((double)ixB - HALFTONE_CENTER) / OCT_DIV);
				FilterBanks[ixB] = new FilterBank(sampleRate, freq);
				PeakBanks[ixB] = new PeakBank {
					DELTA = freq / sampleRate
				};
			}
		}

		/// <summary>
		/// スペクトルを更新
		/// </summary>
		/// <param name="pInput">入力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sampleCount">入力バッファのサンプル数</param>
		public void Update(IntPtr pInput, int sampleCount) {
			CalcMeanSquare(pInput, sampleCount);
			UpdateAutoGain(sampleCount);
			ExtractPeak();
		}

		private unsafe void CalcMeanSquare(IntPtr pInput, int sampleCount) {
			float l_b2, l_b1, l_a2, l_a1, l_ms;
			float r_b2, r_b1, r_a2, r_a1, r_ms;
			float k_b0, k_a2, k_a1, sigma;
			float b0, a0;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var bank = FilterBanks[ixB];
				l_b2 = bank.l_b2;
				l_b1 = bank.l_b1;
				l_a2 = bank.l_a2;
				l_a1 = bank.l_a1;
				l_ms = bank.l_ms;
				r_b2 = bank.r_b2;
				r_b1 = bank.r_b1;
				r_a2 = bank.r_a2;
				r_a1 = bank.r_a1;
				r_ms = bank.r_ms;
				k_b0 = bank.k_b0;
				k_a2 = bank.k_a2;
				k_a1 = bank.k_a1;
				sigma = bank.sigma;
				var pWave = (float*)pInput;
				for (int ixS = sampleCount; ixS != 0; --ixS) {
					/*** [左チャンネル] ***/
					/* 帯域通過フィルタ */
					b0 = *pWave++;
					a0 = b0 - l_b2;
					a0 *= k_b0;
					a0 -= k_a2 * l_a2;
					a0 -= k_a1 * l_a1;
					l_b2 = l_b1;
					l_b1 = b0;
					l_a2 = l_a1;
					l_a1 = a0;
					/* 二乗平均 */
					a0 *= a0;
					a0 -= l_ms;
					l_ms += a0 * sigma;
					/*** [右チャンネル] ***/
					/* 帯域通過フィルタ */
					b0 = *pWave++;
					a0 = b0 - r_b2;
					a0 *= k_b0;
					a0 -= k_a2 * r_a2;
					a0 -= k_a1 * r_a1;
					r_b2 = r_b1;
					r_b1 = b0;
					r_a2 = r_a1;
					r_a1 = a0;
					/* 二乗平均 */
					a0 *= a0;
					a0 -= r_ms;
					r_ms += a0 * sigma;
				}
				bank.l_b2 = l_b2;
				bank.l_b1 = l_b1;
				bank.l_a2 = l_a2;
				bank.l_a1 = l_a1;
				bank.l_ms = l_ms;
				bank.r_b2 = r_b2;
				bank.r_b1 = r_b1;
				bank.r_a2 = r_a2;
				bank.r_a1 = r_a1;
				bank.r_ms = r_ms;
			}
		}

		private void UpdateAutoGain(int sampleCount) {
			/* 最大値を更新 */
			Max = AUTOGAIN_MIN;
			foreach (var bank in FilterBanks) {
				var linear = Math.Sqrt(Math.Max(bank.l_ms, bank.r_ms) * 2);
				Max = Math.Max(Max, linear);
			}

			/* 最大値に追随して自動ゲインを更新 */
			var diff = Max - AutoGain;
			var autoGainDelta = (double)sampleCount / SampleRate;
			autoGainDelta /= diff < 0 ? AUTOGAIN_TIME_DOWN : AUTOGAIN_TIME_UP;
			AutoGain += diff * autoGainDelta;
			if (AutoGain < AUTOGAIN_MIN) {
				AutoGain = AUTOGAIN_MIN;
			}
		}

		private void ExtractPeak() {
			var lastL = 0.0;
			var lastR = 0.0;
			var lastLIndex = -1;
			var lastRIndex = -1;
			var lastDisp = 0.0;
			var lastDispIndex = -1;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				/*** ピーク抽出用の閾値を算出 ***/
				var thresholdL = 0.0;
				var thresholdR = 0.0;
				{
					/* 音域によって閾値幅を選択 */
					int width;
					var transposedIxB = ixB + Transpose * HALFTONE_DIV;
					if (transposedIxB < BEGIN_MID) {
						width = THRESHOLD_WIDTH_LOW;
					} else if (transposedIxB < BEGIN_HIGH) {
						var a2b = (double)(transposedIxB - BEGIN_MID) / (BEGIN_HIGH - BEGIN_MID);
						width = (int)(THRESHOLD_WIDTH_HIGH * a2b + THRESHOLD_WIDTH_LOW * (1 - a2b));
					} else {
						width = THRESHOLD_WIDTH_HIGH;
					}
					/* 閾値幅で指定される範囲の平均値を閾値にする */
					for (int ixW = -width; ixW <= width; ++ixW) {
						var ixBW = Math.Min(BANK_COUNT - 1, Math.Max(0, ixB + ixW));
						var b = FilterBanks[ixBW];
						thresholdL += b.l_ms;
						thresholdR += b.r_ms;
					}
					width = width * 2 + 1;
					var scale = 2.0 / width;
					thresholdL = Math.Sqrt(thresholdL * scale) * THRESHOLD_GAIN;
					thresholdR = Math.Sqrt(thresholdR * scale) * THRESHOLD_GAIN;
				}
				var bank = FilterBanks[ixB];
				/*** 波形合成用のピークを抽出 ***/
				{
					var peak = PeakBanks[ixB];
					peak.L = 0.0;
					peak.R = 0.0;
					var linearL = Math.Sqrt(bank.l_ms * 2);
					var linearR = Math.Sqrt(bank.r_ms * 2);
					if (linearL < thresholdL) {
						if (0 <= lastLIndex) {
							PeakBanks[lastLIndex].L = lastL;
						}
						linearL = 0.0;
						lastL = 0.0;
						lastLIndex = -1;
					}
					if (lastL < linearL) {
						lastL = linearL;
						lastLIndex = ixB;
					}
					if (linearR < thresholdR) {
						if (0 <= lastRIndex) {
							PeakBanks[lastRIndex].R = lastR;
						}
						linearR = 0.0;
						lastR = 0.0;
						lastRIndex = -1;
					}
					if (lastR < linearR) {
						lastR = linearR;
						lastRIndex = ixB;
					}
				}
				/*** 表示用のピークを抽出、曲線を設定 ***/
				{
					var linear = Math.Sqrt(Math.Max(bank.l_ms, bank.r_ms) * 2);
					var threshold = Math.Max(thresholdL, thresholdR);
					if (EnableNormalize) {
						linear /= Max;
						threshold /= Max;
					}
					if (EnableAutoGain) {
						linear /= AutoGain;
						threshold /= AutoGain;
					}
					Curve[ixB] = linear;
					Peak[ixB] = 0.0;
					if (linear < threshold) {
						if (0 <= lastDispIndex) {
							Peak[lastDispIndex] = lastDisp;
						}
						linear = 0.0;
						lastDisp = 0.0;
						lastDispIndex = -1;
					}
					if (lastDisp < linear) {
						lastDisp = linear;
						lastDispIndex = ixB;
					}
				}
			}
			if (0 <= lastLIndex) {
				PeakBanks[lastLIndex].L = lastL;
			}
			if (0 <= lastRIndex) {
				PeakBanks[lastRIndex].R = lastR;
			}
			if (0 <= lastDispIndex) {
				Peak[lastDispIndex] = lastDisp;
			}
		}
	}
}
