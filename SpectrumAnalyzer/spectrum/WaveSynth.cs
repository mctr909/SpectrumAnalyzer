using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using static Spectrum.Spectrum;

namespace Spectrum {
	public partial class WaveSynth : IDisposable {
		/// <summary>変更ピッチ</summary>
		public double Pitch { get; set; } = 1.0;

		readonly Spectrum Spectrum;
		readonly IntPtr[] mpOscillatorBanks = new IntPtr[HALFTONE_COUNT];

		public WaveSynth(Spectrum spectrum) {
			Spectrum = spectrum;
			var random = new Random();
			for (var ixT = 0; ixT < HALFTONE_COUNT; ixT++) {
				mpOscillatorBanks[ixT] = Marshal.AllocHGlobal(Marshal.SizeOf<OscillatorBank>());
				Marshal.StructureToPtr(new OscillatorBank {
					phase = random.NextDouble()
				}, mpOscillatorBanks[ixT], true);
			}
		}

		public void Dispose() {
			foreach (var pOscillatorBank in mpOscillatorBanks) {
				Marshal.FreeHGlobal(pOscillatorBank);
			}
		}

		/// <summary>
		/// 出力バッファへ書き込む
		/// </summary>
		/// <param name="pOutput">出力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sampleCount">出力バッファのサンプル数</param>
		public unsafe void WriteBuffer(IntPtr pOutput, int sampleCount) {
			/* パラメータを設定 */
			SetParameter();
			/* 波形合成を実行 */
			DoWaveSynth((float*)pOutput, sampleCount);
		}

		unsafe void SetParameter() {
			var targetThreshold = TERGET_THRESHOLD * Spectrum.Max;
			for (int idxTone = 0, idxSpec = 0; idxTone < HALFTONE_COUNT; ++idxTone, idxSpec += HALFTONE_DIV) {
				/* 1半音分のスペクトルから最大振幅のものを取得する */
				var pOsc = (OscillatorBank*)mpOscillatorBanks[idxTone];
				pOsc->delta = Math.Sqrt(Spectrum.PeakBanks[idxSpec].DELTA * Spectrum.PeakBanks[idxSpec + HALFTONE_DIV - 1].DELTA);
				pOsc->amp_l = targetThreshold;
				pOsc->amp_r = targetThreshold;
				var ampC = targetThreshold;
				for (int i = HALFTONE_DIV, divSpec = idxSpec; i != 0; --i, ++divSpec) {
					var spec = Spectrum.PeakBanks[divSpec];
					if (spec.L > pOsc->amp_l) {
						pOsc->amp_l = spec.L;
					}
					if (spec.R > pOsc->amp_r) {
						pOsc->amp_r = spec.R;
					}
					var specC = Math.Max(spec.L, spec.R);
					if (specC > ampC) {
						ampC = specC;
						pOsc->delta = spec.DELTA;
					}
				}
				pOsc->delta *= Pitch;
				/* 対象閾値以下の振幅を0クリア */
				if (pOsc->amp_l <= targetThreshold) {
					pOsc->amp_l = 0;
				}
				if (pOsc->amp_r <= targetThreshold) {
					pOsc->amp_r = 0;
				}
				if (pOsc->declicked_l <= targetThreshold) {
					pOsc->declicked_l = 0;
				}
				if (pOsc->declicked_r <= targetThreshold) {
					pOsc->declicked_r = 0;
				}
				/* 振幅が対象閾値以下の場合
				 * 最近低音側または最近高音側の振幅が大きい方の位相を取得して設定する */
				if (pOsc->declicked_l == 0 && pOsc->declicked_r == 0) {
					/* 最近低音側 */
					var lowToneEnd = Math.Max(idxTone - 7, 0);
					var lowToneAmp = targetThreshold;
					for (int t = idxTone - 1; t >= lowToneEnd; --t) {
						var lowOsc = *(OscillatorBank*)mpOscillatorBanks[t];
						var amp = Math.Max(lowOsc.declicked_l, lowOsc.declicked_r);
						if (amp > lowToneAmp) {
							lowToneAmp = amp;
							pOsc->phase = lowOsc.phase;
							break;
						}
					}
					/* 最近高音側 */
					var highToneEnd = Math.Min(idxTone + 7, HALFTONE_COUNT - 1);
					for (int t = idxTone + 1; t <= highToneEnd; ++t) {
						var highOsc = *(OscillatorBank*)mpOscillatorBanks[t];
						var amp = Math.Max(highOsc.declicked_l, highOsc.declicked_r);
						if (amp > lowToneAmp) {
							pOsc->phase = highOsc.phase;
							break;
						}
					}
				}
			}
		}

		unsafe void DoWaveSynth(float* pOutput, int sampleCount) {
			foreach (OscillatorBank* pOsc in mpOscillatorBanks) {
				/* 振幅が0なら波形合成を行わない */
				if (pOsc->amp_l == 0 && pOsc->declicked_l == 0 &&
					pOsc->amp_r == 0 && pOsc->declicked_r == 0) {
					// 位相を進める
					pOsc->phase += pOsc->delta * sampleCount;
					pOsc->phase -= (int)pOsc->phase;
					continue;
				}
				/* 波形合成 */
				var phase = pOsc->phase;
				var declicked_l = pOsc->declicked_l;
				var declicked_r = pOsc->declicked_r;
				var DELTA = pOsc->delta;
				var AMP_L = pOsc->amp_l;
				var AMP_R = pOsc->amp_r;
				var pOutWave = pOutput;
				for (int ixS = sampleCount; ixS != 0; --ixS) {
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
					*pOutWave++ += (float)(sin * declicked_l);
					*pOutWave++ += (float)(sin * declicked_r);
				}
				pOsc->phase = phase;
				pOsc->declicked_l = declicked_l;
				pOsc->declicked_r = declicked_r;
			}
		}
	}
}