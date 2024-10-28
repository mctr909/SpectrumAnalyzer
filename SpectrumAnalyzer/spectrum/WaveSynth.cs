using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using static Spectrum.Settings;

namespace Spectrum {
	public class WaveSynth : IDisposable {
		const int TABLE_LENGTH = 48;
		static readonly double[] TABLE = {
			 0.0000, 0.1305, 0.2588, 0.3827, 0.5000, 0.6088,
			 0.7071, 0.7934, 0.8660, 0.9239, 0.9659, 0.9914,
			 1.0000, 0.9914, 0.9659, 0.9239, 0.8660, 0.7934,
			 0.7071, 0.6088, 0.5000, 0.3827, 0.2588, 0.1305,
			 0.0000,-0.1305,-0.2588,-0.3827,-0.5000,-0.6088,
			-0.7071,-0.7934,-0.8660,-0.9239,-0.9659,-0.9914,
			-1.0000,-0.9914,-0.9659,-0.9239,-0.8660,-0.7934,
			-0.7071,-0.6088,-0.5000,-0.3827,-0.2588,-0.1305,
			 0.0000
		};

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
		/// <param name="pOutput">出力バッファ</param>
		/// <param name="sampleCount">出力バッファのサンプル数</param>
		public unsafe void WriteBuffer(IntPtr pOutput, int sampleCount) {
			SetParameter();
			/* 波形合成 */
			Parallel.ForEach(mpOscillatorBanks, pOscillatorBank => {
				var pOsc = (OscillatorBank*)pOscillatorBank;
				if (pOsc->LTarget == 0 && pOsc->LCurrent == 0 &&
					pOsc->RTarget == 0 && pOsc->RCurrent == 0) {
					pOsc->Phase += pOsc->Delta * sampleCount;
					pOsc->Phase -= (int)pOsc->Phase;
					return;
				}
				var pWave = (float*)pOutput;
				for (int s = 0; s < sampleCount; ++s) {
					var indexD = pOsc->Phase * TABLE_LENGTH;
					var indexI = (int)indexD;
					var a2b = indexD - indexI;
					pOsc->Phase += pOsc->Delta;
					pOsc->Phase -= (int)pOsc->Phase;
					pOsc->LCurrent += (pOsc->LTarget - pOsc->LCurrent) * pOsc->Declick;
					pOsc->RCurrent += (pOsc->RTarget - pOsc->RCurrent) * pOsc->Declick;
					var wave = TABLE[indexI] * (1.0 - a2b) + TABLE[indexI + 1] * a2b;
					*pWave++ += (float)(wave * pOsc->LCurrent);
					*pWave++ += (float)(wave * pOsc->RCurrent);
				}
			});
		}

		unsafe void SetParameter() {
			for (int idxT = 0, idxS = 0; idxT < HALFTONE_COUNT; ++idxT, idxS += HALFTONE_DIV) {
				/* 1半音分のスペクトルから最大振幅のものを取得する */
				var pOsc = (OscillatorBank*)mpOscillatorBanks[idxT];
				pOsc->Delta = PeakBanks[idxS + HALFTONE_CENTER].DELTA;
				pOsc->LTarget = 0.0;
				pOsc->RTarget = 0.0;
				var max = 0.0;
				for (int div = 0, divS = idxS; div < HALFTONE_DIV; ++div, ++divS) {
					var spec = PeakBanks[divS];
					if (spec.L > pOsc->LTarget) {
						pOsc->LTarget = spec.L;
					}
					if (spec.R > pOsc->RTarget) {
						pOsc->RTarget = spec.R;
					}
					var specC = Math.Max(spec.L, spec.R);
					if (specC > max) {
						max = specC;
						pOsc->Delta = spec.DELTA;
					}
				}
				pOsc->Delta *= Pitch;
				pOsc->Declick = Pitch * (idxT < SYNTH_BEGIN_MID_TONE ? SYNTH_DECLICK_LOW_SPEED : SYNTH_DECLICK_MID_SPEED);
				if (pOsc->LTarget < SYNTH_THRESHOLD) {
					pOsc->LTarget = 0;
				}
				if (pOsc->LCurrent < SYNTH_THRESHOLD) {
					pOsc->LCurrent = 0;
				}
				if (pOsc->RTarget < SYNTH_THRESHOLD) {
					pOsc->RTarget = 0;
				}
				if (pOsc->RCurrent < SYNTH_THRESHOLD) {
					pOsc->RCurrent = 0;
				}
				/* 現在の振幅が閾値未満の場合
				 * 低音側または高音側の位相を取得して設定する */
				if (pOsc->LCurrent == 0 && pOsc->RCurrent == 0) {
					/* 低音側の振幅を確認して位相を設定 */
					var lowToneAmp = SYNTH_THRESHOLD;
					var lowToneEnd = Math.Max(idxT - 4, 0);
					for (int t = idxT - 1; t >= lowToneEnd; --t) {
						var pLowTone = (OscillatorBank*)mpOscillatorBanks[t];
						var amp = Math.Max(pLowTone->LCurrent, pLowTone->RCurrent);
						if (amp > lowToneAmp) {
							lowToneAmp = amp;
							pOsc->Phase = pLowTone->Phase;
							break;
						}
					}
					/* 高音側の振幅を確認して位相を設定 */
					var highToneEnd = Math.Min(idxT + 4, HALFTONE_COUNT - 1);
					for (int t = idxT + 1; t <= highToneEnd; ++t) {
						var pHighTone = (OscillatorBank*)mpOscillatorBanks[t];
						var amp = Math.Max(pHighTone->LCurrent, pHighTone->RCurrent);
						if (amp > lowToneAmp) {
							pOsc->Phase = pHighTone->Phase;
							break;
						}
					}
				}
			}
		}
	}
}
