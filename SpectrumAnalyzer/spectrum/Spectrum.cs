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

		/// <summary>表示用ピーク</summary>
		public double[] Peak { get; private set; } = new double[BANK_COUNT];

		/// <summary>表示用曲線</summary>
		public double[] Curve { get; private set; } = new double[BANK_COUNT];

		/// <summary>表示用閾値</summary>
		public double[] Threshold { get; private set; } = new double[BANK_COUNT];

		/// <summary>波形合成用ピーク</summary>
		internal PeakBank[] PeakBanks { get; private set; } = new PeakBank[BANK_COUNT];

		private readonly int m_sample_rate;
		private unsafe BPF_BANK* mp_bpf_banks = null;

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
			m_sample_rate = sample_rate;
			mp_bpf_banks = (BPF_BANK*)Marshal.AllocHGlobal(BANK_COUNT * sizeof(BPF_BANK));
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var frequency = BASE_FREQ * Math.Pow(2.0, (double)ixB / OCT_DIV);
				SetBPFCoef(mp_bpf_banks + ixB, sample_rate, frequency);
				PeakBanks[ixB] = new PeakBank {
					DELTA = frequency / sample_rate
				};
			}
		}

		public unsafe void Dispose() {
			if (mp_bpf_banks != null) {
				Marshal.FreeHGlobal((IntPtr)mp_bpf_banks);
			}
		}

		/// <summary>
		/// スペクトルを更新
		/// </summary>
		/// <param name="p_input">入力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sample_count">入力バッファのサンプル数</param>
		public void Update(IntPtr p_input, int sample_count) {
			CalcMeanSquare(p_input, sample_count);
			UpdateAutoGain(sample_count);
			ExtractPeak();
		}

		private unsafe static void SetBPFCoef(BPF_BANK* p_bank, int sample_rate, double frequency) {
			var band_width = 1 + Math.Log(FREQ_AT_BANDWIDTH / frequency, 2.0);
			if (band_width < 0.666) {
				band_width = 0.666;
			}
			var omega = 2 * Math.PI * frequency / sample_rate;
			var c = Math.Cos(omega);
			var s = Math.Sin(omega);
			var x = Math.Log(2) / 2 * band_width / 12.0 * omega / s;
			var sh = s * Math.Sinh(x);
			var a0 = 1 + sh;
			Marshal.StructureToPtr(new BPF_BANK(), (IntPtr)p_bank, true);
			p_bank->k_b0 = (float)(sh / a0);
			p_bank->k_a1 = (float)(-2 * c / a0);
			p_bank->k_a2 = (float)((1 - sh) / a0);
			p_bank->delta = (float)(1.0 * frequency / sample_rate);
		}

		private unsafe void CalcMeanSquare(IntPtr p_input, int sample_count) {
			float l_b2, l_b1, l_a2, l_a1;
			float r_b2, r_b1, r_a2, r_a1;
			float ms_l, ms_r;
			float b0, a0;
			float k_b0, k_a2, k_a1, delta;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var p_bank = (float*)(mp_bpf_banks + ixB);
				k_b0 = *p_bank++;
				k_a2 = *p_bank++;
				k_a1 = *p_bank++;
				delta = *p_bank++;
				l_b2 = *p_bank++;
				l_b1 = *p_bank++;
				l_a2 = *p_bank++;
				l_a1 = *p_bank++;
				r_b2 = *p_bank++;
				r_b1 = *p_bank++;
				r_a2 = *p_bank++;
				r_a1 = *p_bank++;
				ms_l = *p_bank++;
				ms_r = *p_bank;
				var p_wave = (float*)p_input;
				for (int ixS = sample_count; ixS != 0; --ixS) {
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
				*p_bank-- = ms_r;
				*p_bank-- = ms_l;
				*p_bank-- = r_a1;
				*p_bank-- = r_a2;
				*p_bank-- = r_b1;
				*p_bank-- = r_b2;
				*p_bank-- = l_a1;
				*p_bank-- = l_a2;
				*p_bank-- = l_b1;
				*p_bank = l_b2;
			}
		}

		private unsafe void UpdateAutoGain(int sample_count) {
			/* 最大値を更新 */
			Max = AUTOGAIN_MIN;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var b = mp_bpf_banks[ixB];
				var amp = Math.Sqrt(Math.Max(b.ms_l, b.ms_r) * 2);
				Max = Math.Max(Max, amp);
			}

			/* 最大値に追随して自動ゲインを更新 */
			var diff = Max - AutoGain;
			var delta = (double)sample_count / m_sample_rate;
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
					/* 平均値を閾値の下限とする */
					var ms_l = 0.0;
					var ms_r = 0.0;
					for (int ixW = -width; ixW <= width; ++ixW) {
						var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, ixB + ixW));
						var b = mp_bpf_banks[bw];
						ms_l += b.ms_l;
						ms_r += b.ms_r;
						threshold_l = Math.Max(threshold_l, b.ms_l);
						threshold_r = Math.Max(threshold_r, b.ms_r);
					}
					width <<= 1;
					width++;
					double ms_scale;
					if (ixB < BEGIN_HIGH) {
						ms_scale = 1.047; //+0.2db
					} else {
						ms_scale = 1.188; //+0.75db
					}
					ms_scale /= width;
					ms_l *= ms_scale;
					ms_r *= ms_scale;
					threshold_l = Math.Max(threshold_l, ms_l);
					threshold_r = Math.Max(threshold_r, ms_r);
					/* 2乗平均を振幅に変換 */
					threshold_l = Math.Sqrt(threshold_l * 2);
					threshold_r = Math.Sqrt(threshold_r * 2);
				}
				/*** 波形合成用のピークを抽出 ***/
				var bank = mp_bpf_banks[ixB];
				var amp_l = Math.Sqrt(bank.ms_l * 2);
				var amp_r = Math.Sqrt(bank.ms_r * 2);
				var p_peak = PeakBanks[ixB];
				p_peak.L = amp_l < threshold_l ? 0.0 : amp_l;
				p_peak.R = amp_r < threshold_r ? 0.0 : amp_r;
				/*** 表示用のピークを抽出、曲線と閾値を設定 ***/
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
				Curve[ixB] = amp;
				Threshold[ixB] = threshold;
				Peak[ixB] = amp < threshold ? 0.0 : amp;
			}
		}
	}
}