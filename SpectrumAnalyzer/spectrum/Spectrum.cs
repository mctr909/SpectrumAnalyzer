using System;

namespace Spectrum {
	public partial class Spectrum {
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

		private readonly BPF_BANK[] mp_bpf_banks = new BPF_BANK[BANK_COUNT];
		private readonly int m_sample_rate;

		private class BPF_BANK {
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

			public BPF_BANK(int sample_rate, double frequency) {
				var omega = 2 * Math.PI * frequency / sample_rate;
				var alpha = GetAlpha(sample_rate, frequency);
				var a0 = 1.0 + alpha;
				k_b0 = (float)(alpha / a0);
				k_a1 = (float)(-2.0 * Math.Cos(omega) / a0);
				k_a2 = (float)((1.0 - alpha) / a0);
				sigma = (float)GetAlpha(sample_rate / (2 * Math.PI), frequency);
			}

			private static double GetAlpha(double sample_rate, double frequency) {
				var band_width = 1 + Math.Log(FREQ_AT_BANDWIDTH / frequency, 2.0);
				if (band_width < 1.0) {
					band_width = 1.0;
				}
				var omega = 2 * Math.PI * frequency / sample_rate;
				var s = Math.Sin(omega);
				var x = Math.Log(2) / 4 * band_width / 12.0 * omega / s;
				var a = s * Math.Sinh(x);
				a = Math.Min(1, a);
				return a;
			}
		}

		public Spectrum(int sample_rate) {
			m_sample_rate = sample_rate;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var frequency = BASE_FREQ * Math.Pow(2.0, ((double)ixB - HALFTONE_CENTER) / OCT_DIV);
				mp_bpf_banks[ixB] = new BPF_BANK(sample_rate, frequency);
				PeakBanks[ixB] = new PeakBank {
					DELTA = frequency / sample_rate
				};
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

		private unsafe void CalcMeanSquare(IntPtr p_input, int sample_count) {
			float l_b2, l_b1, l_a2, l_a1, l_ms;
			float r_b2, r_b1, r_a2, r_a1, r_ms;
			float k_b0, k_a2, k_a1, sigma;
			float b0, a0;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var p_bank = mp_bpf_banks[ixB];
				l_b2 = p_bank.l_b2;
				l_b1 = p_bank.l_b1;
				l_a2 = p_bank.l_a2;
				l_a1 = p_bank.l_a1;
				l_ms = p_bank.l_ms;
				r_b2 = p_bank.r_b2;
				r_b1 = p_bank.r_b1;
				r_a2 = p_bank.r_a2;
				r_a1 = p_bank.r_a1;
				r_ms = p_bank.r_ms;
				k_b0 = p_bank.k_b0;
				k_a2 = p_bank.k_a2;
				k_a1 = p_bank.k_a1;
				sigma = p_bank.sigma;
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
					/* 二乗平均 */
					a0 *= a0;
					a0 -= l_ms;
					l_ms += a0 * sigma;
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
					/* 二乗平均 */
					a0 *= a0;
					a0 -= r_ms;
					r_ms += a0 * sigma;
				}
				p_bank.l_b2 = l_b2;
				p_bank.l_b1 = l_b1;
				p_bank.l_a2 = l_a2;
				p_bank.l_a1 = l_a1;
				p_bank.l_ms = l_ms;
				p_bank.r_b2 = r_b2;
				p_bank.r_b1 = r_b1;
				p_bank.r_a2 = r_a2;
				p_bank.r_a1 = r_a1;
				p_bank.r_ms = r_ms;
			}
		}

		private void UpdateAutoGain(int sample_count) {
			/* 最大値を更新 */
			Max = AUTOGAIN_MIN;
			foreach (var bank in mp_bpf_banks) {
				var amp = Math.Sqrt(Math.Max(bank.l_ms, bank.r_ms) * 2);
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

		private void ExtractPeak() {
			var last_amp_l = 0.0;
			var last_amp_r = 0.0;
			var last_amp_disp = 0.0;
			var last_index_l = -1;
			var last_index_r = -1;
			var last_index_disp = -1;
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
					/* 閾値幅で指定される範囲のスペクトルの平均値を閾値にする */
					for (int ixW = -width; ixW <= width; ++ixW) {
						var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, ixB + ixW));
						var b = mp_bpf_banks[bw];
						threshold_l += b.l_ms * b.l_ms;
						threshold_r += b.r_ms * b.r_ms;
					}
					width = width * 2 + 1;
					var scale = 4.0 / width;
					threshold_l = Math.Pow(threshold_l * scale, 0.25) * THRESHOLD_GAIN;
					threshold_r = Math.Pow(threshold_r * scale, 0.25) * THRESHOLD_GAIN;
				}
				var bank = mp_bpf_banks[ixB];
				/*** 波形合成用のピークを抽出 ***/
				{
					var p_peak = PeakBanks[ixB];
					p_peak.L = 0.0;
					p_peak.R = 0.0;
					var amp_l = Math.Sqrt(bank.l_ms * 2);
					var amp_r = Math.Sqrt(bank.r_ms * 2);
					if (amp_l < threshold_l) {
						if (0 <= last_index_l) {
							PeakBanks[last_index_l].L = last_amp_l;
						}
						amp_l = 0.0;
						last_amp_l = 0.0;
						last_index_l = -1;
					}
					if (last_amp_l < amp_l) {
						last_amp_l = amp_l;
						last_index_l = ixB;
					}
					if (amp_r < threshold_r) {
						if (0 <= last_index_r) {
							PeakBanks[last_index_r].R = last_amp_r;
						}
						amp_r = 0.0;
						last_amp_r = 0.0;
						last_index_r = -1;
					}
					if (last_amp_r < amp_r) {
						last_amp_r = amp_r;
						last_index_r = ixB;
					}
				}
				/*** 表示用のピークを抽出、曲線を設定 ***/
				{
					var amp = Math.Sqrt(Math.Max(bank.l_ms, bank.r_ms) * 2);
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
					Peak[ixB] = 0.0;
					Threshold[ixB] = threshold;
					if (amp < threshold) {
						if (0 <= last_index_disp) {
							Peak[last_index_disp] = last_amp_disp;
						}
						amp = 0.0;
						last_amp_disp = 0.0;
						last_index_disp = -1;
					}
					if (last_amp_disp < amp) {
						last_amp_disp = amp;
						last_index_disp = ixB;
					}
				}
			}
			if (0 <= last_index_l) {
				PeakBanks[last_index_l].L = last_amp_l;
			}
			if (0 <= last_index_r) {
				PeakBanks[last_index_r].R = last_amp_r;
			}
			if (0 <= last_index_disp) {
				Peak[last_index_disp] = last_amp_disp;
			}
		}
	}
}
