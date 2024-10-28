using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using static Spectrum.Spectrum;

namespace Spectrum {
	public class WaveSynth : IDisposable {
		/// <summary>正弦波テーブル</summary>
		static readonly double[] SIN_TABLE = {
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

		/// <summary>正弦波テーブルの長さ</summary>
		const int SIN_TABLE_LENGTH = 48;
		/// <summary>閾値[10^(db/20)]</summary>
		const double TERGET_THRESHOLD = 1.0 / 1500;
		/// <summary>閾値[10^(db/20)]</summary>
		const double PURGE_THRESHOLD = 1.0 / 32768.0;
		/// <summary>低音域デクリック速度</summary>
		const double DECLICK_LOW_SPEED = 0.125;
		/// <summary>中音域デクリック速度</summary>
		const double DECLICK_MID_SPEED = 0.25;
		/// <summary>高音域デクリック速度</summary>
		const double DECLICK_HIGH_SPEED = 0.5;
		/// <summary>中音域開始[半音]</summary>
		const int BEGIN_MID_TONE = 36;
		/// <summary>高音域開始[半音]</summary>
		const int BEGIN_HIGH_TONE = 108;

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
					var specC = Math.Max(spec.L, spec.R);
					if (specC > max) {
						max = specC;
						pOsc->Delta = spec.DELTA;
					}
					if (spec.L > pOsc->LTarget) {
						pOsc->LTarget = spec.L;
					}
					if (spec.R > pOsc->RTarget) {
						pOsc->RTarget = spec.R;
					}
				}
				pOsc->Delta *= Pitch;
				/* 音域によってデクリック速度を選択 */
				var transeposedTone = idxTone + (12 * Math.Log(Pitch, 2));
				if (transeposedTone < BEGIN_MID_TONE) {
					pOsc->Declick = DECLICK_LOW_SPEED;
				} else if (transeposedTone < BEGIN_HIGH_TONE) {
					pOsc->Declick = DECLICK_MID_SPEED;
				} else {
					pOsc->Declick = DECLICK_HIGH_SPEED;
				}
				/* 閾値未満の振幅を0クリア */
				if (pOsc->LTarget < TERGET_THRESHOLD) {
					pOsc->LTarget = 0;
				}
				if (pOsc->RTarget < TERGET_THRESHOLD) {
					pOsc->RTarget = 0;
				}
				if (pOsc->LCurrent < PURGE_THRESHOLD) {
					pOsc->LCurrent = 0;
				}
				if (pOsc->RCurrent < PURGE_THRESHOLD) {
					pOsc->RCurrent = 0;
				}
				/* 現在の振幅が閾値未満の場合
				 * 低音側または高音側の位相を取得して設定する */
				if (pOsc->LCurrent == 0 && pOsc->RCurrent == 0) {
					/* 低音側の振幅を確認して位相を設定 */
					var lowToneAmp = PURGE_THRESHOLD;
					var lowToneEnd = Math.Max(idxTone - 5, 0);
					for (int t = idxTone - 1; t >= lowToneEnd; --t) {
						var lowOsc = *(OscillatorBank*)mpOscillatorBanks[t];
						var amp = Math.Max(lowOsc.LCurrent, lowOsc.RCurrent);
						if (amp > lowToneAmp) {
							lowToneAmp = amp;
							pOsc->Phase = lowOsc.Phase;
							break;
						}
					}
					/* 高音側の振幅を確認して位相を設定 */
					var highToneEnd = Math.Min(idxTone + 5, HALFTONE_COUNT - 1);
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
					pOsc->LCurrent += (pOsc->LTarget - pOsc->LCurrent) * pOsc->Declick;
					pOsc->RCurrent += (pOsc->RTarget - pOsc->RCurrent) * pOsc->Declick;
					var wave = SIN_TABLE[indexI] * (1.0 - a2b) + SIN_TABLE[indexI + 1] * a2b;
					*pWave++ += (float)(wave * pOsc->LCurrent);
					*pWave++ += (float)(wave * pOsc->RCurrent);
				}
			});
		}
	}
}
