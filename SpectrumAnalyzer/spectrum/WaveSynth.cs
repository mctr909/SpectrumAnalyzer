using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using static Spectrum.Spectrum;

namespace Spectrum {
	public partial class WaveSynth : IDisposable {
		IntPtr[] mpOscillatorBanks;
		PeakBank[] PeakBanks;

		/// <summary>
		/// 変更ピッチ
		/// </summary>
		public double Pitch { get; set; } = 1.0;

		public WaveSynth(Spectrum spectrum) {
			PeakBanks = spectrum.PeakBanks;
			mpOscillatorBanks = new IntPtr[HALFTONE_COUNT];
			var random = new Random();
			for (var idxT = 0; idxT < HALFTONE_COUNT; idxT++) {
				mpOscillatorBanks[idxT] = Marshal.AllocHGlobal(Marshal.SizeOf<OscillatorBank>());
				var osc = new OscillatorBank() {
					Phase = random.NextDouble()
				};
				Marshal.StructureToPtr(osc, mpOscillatorBanks[idxT], true);
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
			for (int idxTone = 0, idxSpec = 0; idxTone < HALFTONE_COUNT; ++idxTone, idxSpec += HALFTONE_DIV) {
				/* 1半音分のスペクトルから最大振幅のものを取得する */
				var max = 0.0;
				var pOsc = (OscillatorBank*)mpOscillatorBanks[idxTone];
				pOsc->Delta = PeakBanks[idxSpec + HALFTONE_CENTER].DELTA;
				pOsc->LTarget = 0.0;
				pOsc->RTarget = 0.0;
				for (int i = HALFTONE_DIV, divSpec = idxSpec; i != 0; --i, ++divSpec) {
					var spec = PeakBanks[divSpec];
					var specL = spec.L;
					var specR = spec.R;
					var specC = Math.Max(specL, specR);
					if (specC > max) {
						max = specC;
						pOsc->Delta = spec.DELTA;
					}
					if (specL > pOsc->LTarget) {
						pOsc->LTarget = specL;
					}
					if (specR > pOsc->RTarget) {
						pOsc->RTarget = specR;
					}
				}
				pOsc->Delta *= Pitch;
				/* 対象閾値未満の振幅を0クリア */
				if (pOsc->LTarget < TERGET_THRESHOLD) {
					pOsc->LTarget = 0;
				}
				if (pOsc->RTarget < TERGET_THRESHOLD) {
					pOsc->RTarget = 0;
				}
				/* 破棄閾値未満の振幅を0クリア */
				if (pOsc->LCurrent < PURGE_THRESHOLD) {
					pOsc->LCurrent = 0;
				}
				if (pOsc->RCurrent < PURGE_THRESHOLD) {
					pOsc->RCurrent = 0;
				}
				/* 現在の振幅が破棄閾値未満の場合
				 * 直近低音側または直近高音側の位相を取得して設定する */
				if (pOsc->LCurrent == 0 && pOsc->RCurrent == 0) {
					/* 直近低音側の振幅を確認して位相を設定 */
					var lowToneAmp = PURGE_THRESHOLD;
					var lowToneEnd = Math.Max(idxTone - 7, 0);
					for (int t = idxTone - 1; t >= lowToneEnd; --t) {
						var lowOsc = *(OscillatorBank*)mpOscillatorBanks[t];
						var amp = Math.Max(lowOsc.LCurrent, lowOsc.RCurrent);
						if (amp > lowToneAmp) {
							lowToneAmp = amp;
							pOsc->Phase = lowOsc.Phase;
							break;
						}
					}
					/* 直近高音側の振幅を確認して位相を設定 */
					var highToneEnd = Math.Min(idxTone + 7, HALFTONE_COUNT - 1);
					for (int t = idxTone + 1; t <= highToneEnd; ++t) {
						var highOsc = *(OscillatorBank*)mpOscillatorBanks[t];
						var amp = Math.Max(highOsc.LCurrent, highOsc.RCurrent);
						if (amp > lowToneAmp) {
							pOsc->Phase = highOsc.Phase;
							break;
						}
					}
				}
			}
		}

		unsafe void DoWaveSynth(float *pOutput, int sampleCount) {
			Parallel.ForEach(mpOscillatorBanks, pOscillatorBank => {
				var pOsc = (OscillatorBank*)pOscillatorBank;
				/* 振幅が0なら位相だけ進めて波形合成を行わない */
				if (pOsc->LTarget == 0 && pOsc->LCurrent == 0 &&
					pOsc->RTarget == 0 && pOsc->RCurrent == 0) {
					pOsc->Phase += pOsc->Delta * sampleCount;
					pOsc->Phase -= (int)pOsc->Phase;
					return;
				}
				/* 波形合成 */
				var pWave = pOutput;
				for (int i = sampleCount; i != 0; --i) {
					var indexD = pOsc->Phase * SIN_TABLE_LENGTH;
					var indexI = (int)indexD;
					var a2b = indexD - indexI;
					pOsc->Phase += pOsc->Delta;
					pOsc->Phase -= (int)pOsc->Phase;
					pOsc->LCurrent += (pOsc->LTarget - pOsc->LCurrent) * DECLICK_SPEED;
					pOsc->RCurrent += (pOsc->RTarget - pOsc->RCurrent) * DECLICK_SPEED;
					var wave = SIN_TABLE[indexI] * (1.0 - a2b) + SIN_TABLE[indexI + 1] * a2b;
					*pWave++ += (float)(wave * pOsc->LCurrent);
					*pWave++ += (float)(wave * pOsc->RCurrent);
				}
			});
		}
	}
}
