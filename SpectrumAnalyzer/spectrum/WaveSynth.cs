using System;

public class WaveSynth  {
	const double DECLICK_SPEED = 0.1;
	const double THRESHOLD = 0.0001; /* -80db */
	const int TABLE_LENGTH = 192;

	static readonly double[] TABLE;
	static WaveSynth() {
		TABLE = new double[TABLE_LENGTH + 1];
		for (int i = 0; i < TABLE_LENGTH + 1; i++) {
			TABLE[i] = Math.Sin(2 * Math.PI * i / TABLE_LENGTH);
		}
	}

	class Halftone {
		public double AmpL;
		public double AmpR;
		public double Phase;
	}
	Halftone[] Halftones;

	Spectrum Spectrum;

	public WaveSynth(Spectrum spectrum) {
		Spectrum = spectrum;
		Halftones = new Halftone[spectrum.TONE_COUNT];
		var random = new Random();
		for (var idxT = 0; idxT < spectrum.TONE_COUNT; idxT++) {
			Halftones[idxT] = new Halftone() {
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
		for (int idxT = 0, idxB = 0; idxT < Halftones.Length; ++idxT, idxB += Spectrum.HALFTONE_DIV) {
			/* スペクトル中の半音に対応する範囲で最大振幅のバンクを採用する */
			var specAmpL = 0.0;
			var specAmpR = 0.0;
			var specAmpC = 0.0;
			var delta = Spectrum.Banks[idxB + Spectrum.HALFTONE_DIV_CENTER].DELTA;
			for (int div = 0, divB = idxB; div < Spectrum.HALFTONE_DIV; ++div, ++divB) {
				var bank = Spectrum.Banks[divB];
				if (specAmpL < bank.PeakL) {
					specAmpL = bank.PeakL;
				}
				if (specAmpR < bank.PeakR) {
					specAmpR = bank.PeakR;
				}
				var peakC = Math.Max(bank.PeakL, bank.PeakR);
				if (specAmpC < peakC) {
					specAmpC = peakC;
					delta = bank.DELTA;
				}
			}
			delta *= Spectrum.Pitch;
			/* 直前の振幅が閾値未満の場合、位相を設定する */
			var halftone = Halftones[idxT];
			if (halftone.AmpL < THRESHOLD && halftone.AmpR < THRESHOLD) {
				/* 低音側の振幅を確認して位相を設定 */
				if (lowToneAmp < THRESHOLD || (idxT - lowToneIdx > 5)) {
					lowToneAmp = THRESHOLD;
				}
				else {
					halftone.Phase = lowTonePhase;
				}
				/* 高音側の振幅を確認して位相を設定 */
				var highToneEnd = Math.Min(idxT + 5, Halftones.Length);
				for (int t = idxT + 1; t < highToneEnd; ++t) {
					var highTone = Halftones[t];
					var highToneAmp = Math.Max(highTone.AmpL, highTone.AmpR);
					if (highToneAmp > lowToneAmp) {
						halftone.Phase = highTone.Phase;
						break;
					}
				}
				/* スペクトルの振幅が閾値未満の場合、
				 * 波形合成を行わずに位相を進めて次の半音へ移る */
				if (specAmpL < THRESHOLD && specAmpR < THRESHOLD) {
					halftone.Phase += delta * sampleCount;
					halftone.Phase -= (int)halftone.Phase;
					continue;
				}
			}
			else {
				lowToneIdx = idxT;
				lowToneAmp = Math.Max(halftone.AmpL, halftone.AmpR);
				lowTonePhase = halftone.Phase;
			}
			/* 波形合成 */
			var pWave = (float*)pOutput;
			for (int s = 0; s < sampleCount; ++s) {
				var indexD = halftone.Phase * TABLE_LENGTH;
				var indexI = (int)indexD;
				var a2b = indexD - indexI;
				halftone.Phase += delta;
				halftone.Phase -= (int)halftone.Phase;
				halftone.AmpL += (specAmpL - halftone.AmpL) * DECLICK_SPEED;
				halftone.AmpR += (specAmpR - halftone.AmpR) * DECLICK_SPEED;
				var wave = TABLE[indexI] * (1.0 - a2b) + TABLE[indexI + 1] * a2b;
				*pWave++ += (float)(wave * halftone.AmpL);
				*pWave++ += (float)(wave * halftone.AmpR);
			}
		}
	}
}
