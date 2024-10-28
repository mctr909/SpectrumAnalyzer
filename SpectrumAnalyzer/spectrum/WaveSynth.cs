using System;
using static Spectrum.Settings;

namespace Spectrum {
	public class WaveSynth {
		const double THRESHOLD = 0.0001; /* -80db */
		const int TABLE_LENGTH = 96;
		const int HALFTONE_DIV_CENTER = HALFTONE_DIV / 2;

		static readonly double[] TABLE = {
			0.0000, 0.0654, 0.1305, 0.1951, 0.2588, 0.3214, 0.3827, 0.4423,
			0.5000, 0.5556, 0.6088, 0.6593, 0.7071, 0.7518, 0.7934, 0.8315,
			0.8660, 0.8969, 0.9239, 0.9469, 0.9659, 0.9808, 0.9914, 0.9979,
			1.0000, 0.9979, 0.9914, 0.9808, 0.9659, 0.9469, 0.9239, 0.8969,
			0.8660, 0.8315, 0.7934, 0.7518, 0.7071, 0.6593, 0.6088, 0.5556,
			0.5000, 0.4423, 0.3827, 0.3214, 0.2588, 0.1951, 0.1305, 0.0654,
			0.0000, -0.0654, -0.1305, -0.1951, -0.2588, -0.3214, -0.3827, -0.4423,
			-0.5000, -0.5556, -0.6088, -0.6593, -0.7071, -0.7518, -0.7934, -0.8315,
			-0.8660, -0.8969, -0.9239, -0.9469, -0.9659, -0.9808, -0.9914, -0.9979,
			-1.0000, -0.9979, -0.9914, -0.9808, -0.9659, -0.9469, -0.9239, -0.8969,
			-0.8660, -0.8315, -0.7934, -0.7518, -0.7071, -0.6593, -0.6088, -0.5556,
			-0.5000, -0.4423, -0.3827, -0.3214, -0.2588, -0.1951, -0.1305, -0.0654,
			0.0000
		};

		class OSCBank {
			public double Phase;
			public double L;
			public double R;
		}
		OSCBank[] OSCBanks;
		PeakBank[] PeakBanks;

		public WaveSynth(Spectrum spectrum) {
			PeakBanks = spectrum.PeakBanks;
			OSCBanks = new OSCBank[NOTE_COUNT];
			var random = new Random();
			for (var idxT = 0; idxT < NOTE_COUNT; idxT++) {
				OSCBanks[idxT] = new OSCBank() {
					Phase = random.NextDouble(),
				};
			}
		}

		/// <summary>
		/// 出力バッファへ書き込む
		/// </summary>
		/// <param name="pOutput">出力バッファ</param>
		/// <param name="sampleCount">出力バッファのサンプル数</param>
		public unsafe void WriteBuffer(IntPtr pOutput, int sampleCount) {
			var lowToneIdx = 0;
			var lowToneAmp = 0.0;
			var lowTonePhase = 0.0;
			for (int idxT = 0, idxB = 0; idxT < OSCBanks.Length; ++idxT, idxB += HALFTONE_DIV) {
				/* スペクトル中の半音に対応する範囲で最大振幅のバンクを採用する */
				var specL = 0.0;
				var specR = 0.0;
				var specC = 0.0;
				var delta = PeakBanks[idxB + HALFTONE_DIV_CENTER].DELTA;
				for (int div = 0, divB = idxB; div < HALFTONE_DIV; ++div, ++divB) {
					var spec = PeakBanks[divB];
					if (specL < spec.L) {
						specL = spec.L;
					}
					if (specR < spec.R) {
						specR = spec.R;
					}
					var peakC = Math.Max(spec.L, spec.R);
					if (specC < peakC) {
						specC = peakC;
						delta = spec.DELTA;
					}
				}
				delta *= Pitch;
				/* 直前の振幅が閾値未満の場合、位相を設定する */
				var oscBank = OSCBanks[idxT];
				if (oscBank.L < THRESHOLD) {
					oscBank.L = 0;
				}
				if (oscBank.R < THRESHOLD) {
					oscBank.R = 0;
				}
				if (oscBank.L == 0 && oscBank.R == 0) {
					/* 低音側の振幅を確認して位相を設定 */
					if (lowToneAmp < THRESHOLD || (idxT - lowToneIdx > 4)) {
						lowToneAmp = THRESHOLD;
					}
					else {
						oscBank.Phase = lowTonePhase;
					}
					/* 高音側の振幅を確認して位相を設定 */
					var highToneEnd = Math.Min(idxT + 4, OSCBanks.Length);
					for (int t = idxT + 1; t < highToneEnd; ++t) {
						var highTone = OSCBanks[t];
						var highToneAmp = Math.Max(highTone.L, highTone.R);
						if (highToneAmp > lowToneAmp) {
							oscBank.Phase = highTone.Phase;
							break;
						}
					}
					/* スペクトルの振幅が閾値未満の場合、
					 * 波形合成を行わずに位相を進めて次の半音へ移る */
					if (specL < THRESHOLD && specR < THRESHOLD) {
						oscBank.Phase += delta * sampleCount;
						oscBank.Phase -= (int)oscBank.Phase;
						continue;
					}
				}
				else {
					lowToneIdx = idxT;
					lowToneAmp = Math.Max(oscBank.L, oscBank.R);
					lowTonePhase = oscBank.Phase;
				}
				/* 波形合成 */
				var declickSpeed = Pitch * (idxT < DECLICK_MID_TONE ? DECLICK_LOW_SPEED : DECLICK_MID_SPEED);
				var pWave = (float*)pOutput;
				for (int s = 0; s < sampleCount; ++s) {
					var indexD = oscBank.Phase * TABLE_LENGTH;
					var indexI = (int)indexD;
					var a2b = indexD - indexI;
					oscBank.Phase += delta;
					oscBank.Phase -= (int)oscBank.Phase;
					oscBank.L += (specL - oscBank.L) * declickSpeed;
					oscBank.R += (specR - oscBank.R) * declickSpeed;
					var wave = TABLE[indexI] * (1.0 - a2b) + TABLE[indexI + 1] * a2b;
					*pWave++ += (float)(wave * oscBank.L);
					*pWave++ += (float)(wave * oscBank.R);
				}
			}
		}
	}
}
