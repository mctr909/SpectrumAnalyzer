using System;
using static SignalProcess.Spectrum;

namespace SignalProcess {
	public class WaveSynth {
		/// <summary>ピッチ</summary>
		public double Pitch { get; set; } = 1.0;

		private readonly Spectrum Spectrum;
		private readonly OscBank[] Banks = new OscBank[HALFTONE_COUNT];
		private readonly double[] Delta = new double[BANK_COUNT];

		private const double TARGET_THRESHOLD = 1e-3;
		private const double DECLICK_SPEED = 0.1;
		private const int SIN_TABLE_LENGTH = 48;
		private static readonly double[] SIN_TABLE = {
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

		private class OscBank {
			public double Delta;
			public double Phase;
			public double AmpL;
			public double AmpR;
			public double DeclickedL;
			public double DeclickedR;
		}

		public WaveSynth(Spectrum spectrum) {
			Spectrum = spectrum;
			var random = new Random();
			for (var ixT = 0; ixT < HALFTONE_COUNT; ixT++) {
				Banks[ixT] = new OscBank {
					Phase = random.NextDouble()
				};
			}
			for (int i = 0; i < BANK_COUNT; i++) {
				var frequency = BASE_FREQ * Math.Pow(2.0, (double)i / OCT_DIV);
				Delta[i] = frequency / spectrum.SampleRate;
			}
		}

		/// <summary>
		/// 出力バッファへ書き込む
		/// </summary>
		/// <param name="pOutput">出力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sampleCount">出力バッファのサンプル数</param>
		public void WriteBuffer(IntPtr pOutput, int sampleCount) {
			/* パラメータを設定 */
			SetParameter();
			/* 波形合成を実行 */
			DoWaveSynth(pOutput, sampleCount);
		}

		private unsafe void SetParameter() {
			var autoGain = Spectrum.Max;
			var threshold = TARGET_THRESHOLD * autoGain;
			for (int ixN = 0, ixS = 0, ixE = HALFTONE_DIV; ixN < HALFTONE_COUNT; ++ixN, ixS += HALFTONE_DIV, ixE += HALFTONE_DIV) {
				/* フィルタバンクから半音間隔で振幅が最大のバンクを取得する */
				var osc = Banks[ixN];
				osc.Delta = Delta[ixS + HALFTONE_CENTER];
				osc.AmpL = threshold;
				osc.AmpR = threshold;
				var ampC = threshold;
				for (int ixB = ixS; ixB < ixE; ++ixB) {
					var spec = Spectrum.mpBanks[ixB];
					if (spec.PeakL > osc.AmpL) {
						osc.AmpL = spec.PeakL;
					}
					if (spec.PeakR > osc.AmpR) {
						osc.AmpR = spec.PeakR;
					}
					var peakC = Math.Max(spec.PeakL, spec.PeakR);
					if (peakC > ampC) {
						ampC = peakC;
						osc.Delta = Delta[ixB];
					}
				}
				osc.Delta *= Pitch;
				/* 閾値以下の振幅を0クリア */
				if (osc.AmpL <= threshold) {
					osc.AmpL = 0;
				}
				if (osc.AmpR <= threshold) {
					osc.AmpR = 0;
				}
				if (osc.DeclickedL <= threshold) {
					osc.DeclickedL = 0;
				}
				if (osc.DeclickedR <= threshold) {
					osc.DeclickedR = 0;
				}
				/* 振幅が閾値以下の場合
				 * 低音側または高音側の振幅が大きい方の位相を取得して設定する */
				if (osc.DeclickedL == 0 && osc.DeclickedR == 0) {
					/* 低音側 */
					var ixL = Math.Max(ixN - 1, 0);
					var oscL = Banks[ixL];
					var ampL = threshold;
					var amp = Math.Max(oscL.DeclickedL, oscL.DeclickedR);
					if (amp > ampL) {
						ampL = amp;
						osc.Phase = oscL.Phase;
					}
					/* 高音側 */
					var ixH = Math.Min(ixN + 1, HALFTONE_COUNT - 1);
					var oscH = Banks[ixH];
					amp = Math.Max(oscH.DeclickedL, oscH.DeclickedR);
					if (amp > ampL) {
						osc.Phase = oscH.Phase;
					}
				}
			}
		}

		private unsafe void DoWaveSynth(IntPtr pOutput, int sampleCount) {
			var pWaveStart = (float*)pOutput;
			var pWaveTerm = pWaveStart + sampleCount * 2;
			float* pWave = pWaveStart;
			foreach (var osc in Banks) {
				/* 振幅が0なら波形合成を行わない */
				if (osc.AmpL == 0 && osc.DeclickedL == 0 &&
					osc.AmpR == 0 && osc.DeclickedR == 0) {
					// 位相を進める
					osc.Phase += osc.Delta * sampleCount;
					osc.Phase -= (int)osc.Phase;
					continue;
				}
				/* 波形合成 */
				var phase = osc.Phase;
				var declickedL = osc.DeclickedL;
				var declickedR = osc.DeclickedR;
				var delta = osc.Delta;
				var ampL = osc.AmpL;
				var ampR = osc.AmpR;
				while (pWave < pWaveTerm) {
					// 正弦波テーブルの値を線形補間
					var ixD = phase * SIN_TABLE_LENGTH;
					var ixI = (int)ixD;
					var a2b = ixD - ixI;
					var sin = 1.0 - a2b;
					sin *= SIN_TABLE[ixI];
					sin += SIN_TABLE[ixI + 1] * a2b;
					// 位相を進める
					phase += delta;
					phase -= (int)phase;
					// 振幅を更新
					declickedL += (ampL - declickedL) * DECLICK_SPEED;
					declickedR += (ampR - declickedR) * DECLICK_SPEED;
					// 波形合成
					*pWave++ += (float)(sin * declickedL);
					*pWave++ += (float)(sin * declickedR);
				}
				osc.Phase = phase;
				osc.DeclickedL = declickedL;
				osc.DeclickedR = declickedR;
				pWave = pWaveStart;
			}
		}
	}
}