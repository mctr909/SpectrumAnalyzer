using System;
using static Spectrum.Spectrum;

namespace Spectrum {
	public partial class WaveSynth {
		private readonly Spectrum mp_spectrum;
		private readonly OSC_BANK[] mp_osc_banks = new OSC_BANK[HALFTONE_COUNT];

		private class OSC_BANK {
			public double delta;
			public double phase;
			public double amp_l;
			public double amp_r;
			public double declicked_l;
			public double declicked_r;
		}

		public WaveSynth(Spectrum spectrum) {
			mp_spectrum = spectrum;
			var random = new Random();
			for (var ixT = 0; ixT < HALFTONE_COUNT; ixT++) {
				mp_osc_banks[ixT] = new OSC_BANK {
					phase = random.NextDouble()
				};
			}
		}

		/// <summary>
		/// 出力バッファへ書き込む
		/// </summary>
		/// <param name="p_output">出力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sample_count">出力バッファのサンプル数</param>
		public void WriteBuffer(IntPtr p_output, int sample_count) {
			/* パラメータを設定 */
			SetParameter();
			/* 波形合成を実行 */
			DoWaveSynth(p_output, sample_count);
		}

		private void SetParameter() {
			var threshold = TERGET_THRESHOLD * mp_spectrum.Max;
			for (int ixT = 0, ixS = 0; ixT < HALFTONE_COUNT; ++ixT, ixS += HALFTONE_DIV) {
				/* 1半音分のスペクトルから最大振幅のものを取得する */
				var p_osc = mp_osc_banks[ixT];
				p_osc.delta = Math.Sqrt(mp_spectrum.PeakBanks[ixS].DELTA * mp_spectrum.PeakBanks[ixS + HALFTONE_DIV - 1].DELTA);
				p_osc.amp_l = threshold;
				p_osc.amp_r = threshold;
				var amp_c = threshold;
				for (int i = HALFTONE_DIV, div = ixS; i != 0; --i, ++div) {
					var spec = mp_spectrum.PeakBanks[div];
					if (spec.L > p_osc.amp_l) {
						p_osc.amp_l = spec.L;
					}
					if (spec.R > p_osc.amp_r) {
						p_osc.amp_r = spec.R;
					}
					var spec_c = Math.Max(spec.L, spec.R);
					if (spec_c > amp_c) {
						amp_c = spec_c;
						p_osc.delta = spec.DELTA;
					}
				}
				p_osc.delta *= mp_spectrum.Pitch;
				/* 閾値以下の振幅を0クリア */
				if (p_osc.amp_l <= threshold) {
					p_osc.amp_l = 0;
				}
				if (p_osc.amp_r <= threshold) {
					p_osc.amp_r = 0;
				}
				if (p_osc.declicked_l <= threshold) {
					p_osc.declicked_l = 0;
				}
				if (p_osc.declicked_r <= threshold) {
					p_osc.declicked_r = 0;
				}
				/* 振幅が閾値以下の場合
				 * 最近低音側または最近高音側の振幅が大きい方の位相を取得して設定する */
				if (p_osc.declicked_l == 0 && p_osc.declicked_r == 0) {
					/* 最近低音側 */
					var low_end = Math.Max(ixT - 7, 0);
					var low_amp = threshold;
					for (int t = ixT - 1; t >= low_end; --t) {
						var low_osc = mp_osc_banks[t];
						var amp = Math.Max(low_osc.declicked_l, low_osc.declicked_r);
						if (amp > low_amp) {
							low_amp = amp;
							p_osc.phase = low_osc.phase;
							break;
						}
					}
					/* 最近高音側 */
					var high_end = Math.Min(ixT + 7, HALFTONE_COUNT - 1);
					for (int t = ixT + 1; t <= high_end; ++t) {
						var high_osc = mp_osc_banks[t];
						var amp = Math.Max(high_osc.declicked_l, high_osc.declicked_r);
						if (amp > low_amp) {
							p_osc.phase = high_osc.phase;
							break;
						}
					}
				}
			}
		}

		private unsafe void DoWaveSynth(IntPtr p_output, int sample_count) {
			foreach (var p_osc in mp_osc_banks) {
				/* 振幅が0なら波形合成を行わない */
				if (p_osc.amp_l == 0 && p_osc.declicked_l == 0 &&
					p_osc.amp_r == 0 && p_osc.declicked_r == 0) {
					// 位相を進める
					p_osc.phase += p_osc.delta * sample_count;
					p_osc.phase -= (int)p_osc.phase;
					continue;
				}
				/* 波形合成 */
				var phase = p_osc.phase;
				var declicked_l = p_osc.declicked_l;
				var declicked_r = p_osc.declicked_r;
				var DELTA = p_osc.delta;
				var AMP_L = p_osc.amp_l;
				var AMP_R = p_osc.amp_r;
				var p_out_wave = (float*)p_output;
				for (int ixS = sample_count; ixS != 0; --ixS) {
					// 正弦波テーブルの値を線形補間
					var ixD = phase * SIN_TABLE_LENGTH;
					var ixI = (int)ixD;
					var a2b = ixD - ixI;
					var sin = 1.0 - a2b;
					sin *= SIN_TABLE[ixI];
					sin += SIN_TABLE[ixI + 1] * a2b;
					// 位相を進める
					phase += DELTA;
					phase -= (int)phase;
					// 振幅を更新
					declicked_l += (AMP_L - declicked_l) * DECLICK_SPEED;
					declicked_r += (AMP_R - declicked_r) * DECLICK_SPEED;
					// 波形合成
					*p_out_wave++ += (float)(sin * declicked_l);
					*p_out_wave++ += (float)(sin * declicked_r);
				}
				p_osc.phase = phase;
				p_osc.declicked_l = declicked_l;
				p_osc.declicked_r = declicked_r;
			}
		}
	}
}