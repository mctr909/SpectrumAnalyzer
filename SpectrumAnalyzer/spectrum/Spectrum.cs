using System;
using System.Runtime.InteropServices;

namespace Spectrum {
	public partial class Spectrum : IDisposable {
		/// <summary>トランスポーズ[半音]</summary>
		public double Transpose { get; set; } = 0.0;

		/// <summary>変更ピッチ</summary>
		public double Pitch { get; set; } = 1.0;

		/// <summary> 最大値 </summary>
		public double Max { get; private set; } = AUTOGAIN_MIN;

		/// <summary> 自動ゲイン </summary>
		public double AutoGain { get; private set; } = AUTOGAIN_MIN;

		/// <summary>表示用データ</summary>
		public double[] DisplayData { get; private set; } = new double[BANK_COUNT * 3];

		/// <summary>波形合成用ピーク</summary>
		internal PeakBank[] PeakBanks { get; private set; } = new PeakBank[BANK_COUNT];

		private readonly int mSampleRate;
		private unsafe BPF_BANK* mpBpfBanks = null;
		private readonly double[] mPeak = new double[BANK_COUNT];

		private struct BPF_BANK {
			public float k_b0;
			public float k_a2;
			public float k_a1;
			public float delta;
			public float l_b2;
			public float l_b1;
			public float l_a2;
			public float l_a1;
			public float r_b2;
			public float r_b1;
			public float r_a2;
			public float r_a1;
			public float ms_l;
			public float ms_r;
		}

		public unsafe Spectrum(int sample_rate) {
			mSampleRate = sample_rate;
			mpBpfBanks = (BPF_BANK*)Marshal.AllocHGlobal(BANK_COUNT * sizeof(BPF_BANK));
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var frequency = BASE_FREQ * Math.Pow(2.0, (double)ixB / OCT_DIV);
				SetBPFCoef(mpBpfBanks + ixB, sample_rate, frequency);
				PeakBanks[ixB] = new PeakBank {
					DELTA = frequency / sample_rate
				};
			}
		}

		public unsafe void Dispose() {
			if (mpBpfBanks != null) {
				Marshal.FreeHGlobal((IntPtr)mpBpfBanks);
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

		private unsafe static void SetBPFCoef(BPF_BANK* pBank, int sampleRate, double frequency) {
			var bandWidth = 0.5 + Math.Log(HALFTONE_AT_FREQ / frequency, 2.0);
			if (bandWidth < 0.5) {
				bandWidth = 0.5;
			}
			var omega = 2 * Math.PI * frequency / sampleRate;
			var c = Math.Cos(omega);
			var s = Math.Sin(omega);
			var x = Math.Log(2) / 2 * bandWidth / 12.0 * omega / s;
			var sh = s * Math.Sinh(x);
			var a0 = 1 + sh;
			Marshal.StructureToPtr(new BPF_BANK(), (IntPtr)pBank, true);
			pBank->k_b0 = (float)(sh / a0);
			pBank->k_a1 = (float)(-2 * c / a0);
			pBank->k_a2 = (float)((1 - sh) / a0);
			pBank->delta = (float)(1.0 * frequency / sampleRate);
		}

		private unsafe void CalcMeanSquare(IntPtr pInput, int sampleCount) {
			float l_b2, l_b1, l_a2, l_a1;
			float r_b2, r_b1, r_a2, r_a1;
			float ms_l, ms_r;
			float b0, a0;
			float k_b0, k_a2, k_a1, delta;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var p_bank = mpBpfBanks + ixB;
				k_b0 = p_bank->k_b0;
				k_a2 = p_bank->k_a2;
				k_a1 = p_bank->k_a1;
				delta = p_bank->delta;
				l_b2 = p_bank->l_b2;
				l_b1 = p_bank->l_b1;
				l_a2 = p_bank->l_a2;
				l_a1 = p_bank->l_a1;
				r_b2 = p_bank->r_b2;
				r_b1 = p_bank->r_b1;
				r_a2 = p_bank->r_a2;
				r_a1 = p_bank->r_a1;
				ms_l = p_bank->ms_l;
				ms_r = p_bank->ms_r;
				var p_wave = (float*)pInput;
				for (int ixS = sampleCount; ixS != 0; --ixS) {
					/*** [左チャンネル] ***/
					/* 帯域通過フィルタ */
					b0 = *p_wave++;
					a0 = b0 - l_b2;
					a0 *= k_b0;
					a0 -= k_a2 * l_a2;
					a0 -= k_a1 * l_a1;
					l_b2 = l_b1;
					l_b1 = b0;
					l_a2 = l_a1;
					l_a1 = a0;
					/* 振幅の二乗平均 */
					a0 *= a0;
					a0 -= ms_l;
					ms_l += a0 * delta;
					/*** [右チャンネル] ***/
					/* 帯域通過フィルタ */
					b0 = *p_wave++;
					a0 = b0 - r_b2;
					a0 *= k_b0;
					a0 -= k_a2 * r_a2;
					a0 -= k_a1 * r_a1;
					r_b2 = r_b1;
					r_b1 = b0;
					r_a2 = r_a1;
					r_a1 = a0;
					/* 振幅の二乗平均 */
					a0 *= a0;
					a0 -= ms_r;
					ms_r += a0 * delta;
				}
				p_bank->ms_r = ms_r;
				p_bank->ms_l = ms_l;
				p_bank->r_a1 = r_a1;
				p_bank->r_a2 = r_a2;
				p_bank->r_b1 = r_b1;
				p_bank->r_b2 = r_b2;
				p_bank->l_a1 = l_a1;
				p_bank->l_a2 = l_a2;
				p_bank->l_b1 = l_b1;
				p_bank->l_b2 = l_b2;
			}
		}

		private unsafe void UpdateAutoGain(int sampleCount) {
			/* 最大値を更新 */
			Max = AUTOGAIN_MIN;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var b = mpBpfBanks[ixB];
				var amp = Math.Sqrt(Math.Max(b.ms_l, b.ms_r) * 2);
				Max = Math.Max(Max, amp);
			}

			/* 最大値に追随して自動ゲインを更新 */
			var diff = Max - AutoGain;
			var delta = (double)sampleCount / mSampleRate;
			delta /= diff < 0 ? AUTOGAIN_TIME_DOWN : AUTOGAIN_TIME_UP;
			AutoGain += diff * delta;
			if (AutoGain < AUTOGAIN_MIN) {
				AutoGain = AUTOGAIN_MIN;
			}
		}

		private unsafe void ExtractPeak() {
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				/*** ピーク抽出用の閾値を算出 ***/
				var threshold_l = 0.0;
				var threshold_r = 0.0;
				{
					/* 音域によって閾値幅を選択 */
					int width;
					var transposed = ixB + Transpose * HALFTONE_DIV;
					if (transposed < BEGIN_MID) {
						width = THRESHOLD_WIDTH_LOW;
					} else if (transposed < BEGIN_HIGH) {
						var a2b = (double)(transposed - BEGIN_MID) / (BEGIN_HIGH - BEGIN_MID);
						width = (int)(THRESHOLD_WIDTH_HIGH * a2b + THRESHOLD_WIDTH_LOW * (1 - a2b));
					} else {
						width = THRESHOLD_WIDTH_HIGH;
					}
					/* 閾値幅で指定される範囲の最大値を閾値とする */
					for (int ixW = -width; ixW <= width; ++ixW) {
						var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, ixB + ixW));
						var b = mpBpfBanks[bw];
						threshold_l = Math.Max(threshold_l, b.ms_l);
						threshold_r = Math.Max(threshold_r, b.ms_r);
					}
					/* 平均値を閾値の下限とする */
					width = HALFTONE_DIV;
					var avg_l = 0.0;
					var avg_r = 0.0;
					for (int ixW = -width; ixW <= width; ++ixW) {
						var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, ixB + ixW));
						var b = mpBpfBanks[bw];
						avg_l += b.ms_l;
						avg_r += b.ms_r;
					}
					width *= 2;
					width++;
					avg_l /= width;
					avg_r /= width;
					threshold_l = Math.Max(threshold_l, avg_l);
					threshold_r = Math.Max(threshold_r, avg_r);
					/* 2乗平均を振幅に変換 */
					threshold_l = Math.Sqrt(threshold_l * 2);
					threshold_r = Math.Sqrt(threshold_r * 2);
				}
				/*** 波形合成用のピークを抽出 ***/
				var bank = mpBpfBanks[ixB];
				var amp_l = Math.Sqrt(bank.ms_l * 2);
				var amp_r = Math.Sqrt(bank.ms_r * 2);
				var p_peak = PeakBanks[ixB];
				p_peak.L = amp_l < threshold_l ? 0.0 : amp_l;
				p_peak.R = amp_r < threshold_r ? 0.0 : amp_r;
				/*** 表示用の曲線と閾値を設定 ***/
				var amp = Math.Max(amp_l, amp_r);
				var threshold = Math.Max(threshold_l, threshold_r);
				if (EnableNormalize) {
					amp /= Max;
					threshold /= Max;
				}
				if (EnableAutoGain) {
					amp /= AutoGain;
					threshold /= AutoGain;
				}
				DisplayData[ixB] = amp;
				DisplayData[ixB + BANK_COUNT] = threshold;
				mPeak[ixB] = amp < threshold ? 0.0 : amp;
			}
			for (int ixB = 0, ixP = BANK_COUNT * 2; ixB < BANK_COUNT; ++ixB, ++ixP) {
				var max = 0.0;
				for (int ixW = -1; ixW <= 1; ++ixW) {
					var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, ixB + ixW));
					var val = mPeak[bw];
					if (ixW != 0) {
						val *= 0.5;
					}
					max = Math.Max(val, max);
				}
				DisplayData[ixP] = max;
			}
		}
	}
}