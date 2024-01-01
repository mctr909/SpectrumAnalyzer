using System;
using System.Runtime.InteropServices;

namespace SignalProcess {
	public class Spectrum : IDisposable {
		#region [定数]
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 126;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 3;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;
		/// <summary>フィルタバンク数</summary>
		public const int BANK_COUNT = HALFTONE_DIV * HALFTONE_COUNT;
		/// <summary>A4ピッチ</summary>
		public const double A4_PITCH = 440.0;
		/// <summary>基本周波数</summary>
		public static readonly double BASE_FREQ = A4_PITCH * Math.Pow(2, 3.0 / 12.0 + (1.0 / HALFTONE_DIV - 1) / 12.0 - 5);
		#endregion

		#region [設定値]
		/// <summary>トランスポーズ[半音]</summary>
		public double Transpose { get; set; }

		/// <summary>ゲイン自動調整 最小値</summary>
		public double AutoGainMin { get; set; } = 1e-3;
		/// <summary>ゲイン自動調整 減少時間[秒]</summary>
		public double AutoGainDecTime { get; set; } = 6.0;
		/// <summary>ゲイン自動調整 増加時間[秒]</summary>
		public double AutoGainIncTime { get; set; } = 1e-3;

		/// <summary>中音域閾値 開始位置[フィルタバンク数]</summary>
		public int MidTone { get; set; } = HALFTONE_DIV * 36;
		/// <summary>高音域閾値 開始位置[フィルタバンク数]</summary>
		public int HighTone { get; set; } = HALFTONE_DIV * 60;
		/// <summary>低音域閾値 半径[フィルタバンク数]</summary>
		public int LowToneRadius { get; set; } = 2;
		/// <summary>高音域閾値 半径[フィルタバンク数]</summary>
		public int HighToneRadius { get; set; } = 1;
		/// <summary>平均値 半径[フィルタバンク数]</summary>
		public int AvgRadius { get; set; } = 3;
		#endregion

		#region [公開メンバ]
		/// <summary>サンプリング周波数</summary>
		public int SampleRate { get; private set; }
		/// <summary>最大値</summary>
		public double Max { get; private set; }
		/// <summary>自動ゲイン</summary>
		public double AutoGain { get; private set; }
		/// <summary>表示用データ</summary>
		public readonly double[] DisplayData = new double[BANK_COUNT * 3];
		/// <summary>波形合成用データ</summary>
		internal unsafe BpfBank* mpBanks = null;
		#endregion

		/// <summary>
		/// スペクトラムを生成
		/// </summary>
		/// <param name="sampleRate">サンプリング周波数</param>
		public unsafe Spectrum(int sampleRate) {
			SampleRate = sampleRate;
			Max = AutoGainMin;
			AutoGain = AutoGainMin;
			mpBanks = (BpfBank*)Marshal.AllocHGlobal(sizeof(BpfBank) * BANK_COUNT);
			if (null != mpBanks) {
				SetupFilter(true);
			}
		}

		~Spectrum() {
			Free();
		}

		public void Dispose() {
			Free();
			GC.SuppressFinalize(this);
		}

		private unsafe void Free() {
			if (null != mpBanks) {
				Marshal.FreeHGlobal((IntPtr)mpBanks);
				mpBanks = null;
			}
		}

		/// <summary>
		/// フィルタを設定
		/// </summary>
		/// <param name="initialize">初期化を行うか</param>
		/// <param name="halftoneAtFreq">帯域幅が1半音に至る周波数</param>
		public unsafe void SetupFilter(bool initialize, double halftoneAtFreq = 300.0) {
			if (null == mpBanks) {
				return;
			}
			var pBank = mpBanks;
			for (int ix = 0; ix < BANK_COUNT; ++ix) {
				/* 中心周波数、ナイキスト周波数以上にならないように制限 */
				var f0 = BASE_FREQ * Math.Pow(2.0, (double)ix / OCT_DIV);
				f0 = Math.Min(f0, SampleRate * (0.5 - 1e-3));
				/* 中心周波数によってバンド幅を変える */
				var halftone = 1.0 + Math.Log(halftoneAtFreq / f0, 2.0) * 2.0;
				if (halftone < 0.5) {
					halftone = 0.5;
				}
				var bandWidth = halftone / 12.0;
				/* 正規化周波数 */
				var fn = f0 / SampleRate;
				/* バイクアッドフィルタ(BPF)の係数を設定 */
				var omega = 2.0 * Math.PI * fn;
				var c = Math.Cos(omega);
				var s = Math.Sin(omega);
				var x = Math.Log(2.0) / 2.0 * bandWidth * omega / s;
				var alpha = s * Math.Sinh(x);
				var a0 = 1.0 + alpha;
				BpfBank bank;
				if (initialize) {
					bank = default;
				} else {
					bank = Marshal.PtrToStructure<BpfBank>((IntPtr)pBank);
				}
				bank.Ka1 = (float)(2.0 * c / a0);
				bank.Ka2 = (float)(-(1.0 - alpha) / a0);
				bank.Kb0 = (float)(alpha / a0);
				/* RMSの応答速度を設定 */
				if (f0 < halftoneAtFreq) {
					bank.RmsSpeed = (float)fn;
				} else {
					bank.RmsSpeed = (float)(1.0 - Math.Exp(-2.0 * fn));
				}
				Marshal.StructureToPtr(bank, (IntPtr)pBank, false);
				pBank++;
			}
		}

		/// <summary>
		/// スペクトルを更新
		/// </summary>
		/// <param name="pInput">入力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sampleCount">入力バッファのサンプル数</param>
		public unsafe void Update(IntPtr pInput, int sampleCount) {
			if (null == mpBanks) {
				return;
			}
			CalcPower(pInput, sampleCount);
			UpdateAutoGain(sampleCount);
			ExtractPeak();
		}

		private unsafe void CalcPower(IntPtr pInput, int sampleCount) {
			var pBank = mpBanks;
			var pBankTerm = mpBanks + BANK_COUNT;
			var pWave = (float*)pInput;
			var pWaveStart = pWave;
			var pWaveTerm = pWave + sampleCount * 2;
			/* フィルタバンクループ */
			while (pBank < pBankTerm) {
				float la1 = pBank->La1;
				float la2 = pBank->La2;
				float lb1 = pBank->Lb1;
				float lb2 = pBank->Lb2;
				float ra1 = pBank->Ra1;
				float ra2 = pBank->Ra2;
				float rb1 = pBank->Rb1;
				float rb2 = pBank->Rb2;
				float a0;
				float b0;
				float powerL = pBank->PowerL;
				float powerR = pBank->PowerR;
				float ka1 = pBank->Ka1;
				float ka2 = pBank->Ka2;
				float kb0 = pBank->Kb0;
				float rmsSpeed = pBank->RmsSpeed;
				/* 波形サンプルループ */
				while (pWave < pWaveTerm) {
					/* BPF(左) */
					b0 = *pWave++;
					a0 = b0 - lb2;
					a0 *= kb0;
					a0 += la1 * ka1;
					a0 += la2 * ka2;
					/* 状態変数を更新(左) */
					la2 = la1;
					la1 = a0;
					lb2 = lb1;
					lb1 = b0;
					/* RMS(左) */
					a0 *= a0;
					a0 -= powerL;
					powerL += a0 * rmsSpeed;
					/* BPF(右) */
					b0 = *pWave++;
					a0 = b0 - rb2;
					a0 *= kb0;
					a0 += ra1 * ka1;
					a0 += ra2 * ka2;
					/* 状態変数を更新(右) */
					ra2 = ra1;
					ra1 = a0;
					rb2 = rb1;
					rb1 = b0;
					/* RMS(右) */
					a0 *= a0;
					a0 -= powerR;
					powerR += a0 * rmsSpeed;
				}
				pBank->La1 = la1;
				pBank->La2 = la2;
				pBank->Lb1 = lb1;
				pBank->Lb2 = lb2;
				pBank->Ra1 = ra1;
				pBank->Ra2 = ra2;
				pBank->Rb1 = rb1;
				pBank->Rb2 = rb2;
				pBank->PowerL = powerL;
				pBank->PowerR = powerR;
				/* 次のバンクへ */
				pBank++;
				pWave = pWaveStart;
			}
		}

		private unsafe void ExtractPeak() {
			var ixMaxL = int.MinValue;
			var ixMaxR = int.MinValue;
			int ix, ixWindow, ixStart, ixEnd;
			int radius;
			var maxL = double.MinValue;
			var maxR = double.MinValue;
			double ampL, ampR;
			double thresholdL, thresholdR;
			double transposed;
			double a2b;
			BpfBank* pBank;
			for (ix = 0; ix < BANK_COUNT; ++ix) {
				/* バンクに対応した最大値の探索半径を設定 */
				transposed = ix + Transpose * HALFTONE_DIV;
				if (transposed < MidTone) {
					radius = LowToneRadius;
				} else if (transposed < HighTone) {
					a2b = transposed - MidTone;
					a2b /= HighTone - MidTone;
					transposed = 1.0 - a2b;
					transposed *= LowToneRadius;
					transposed += HighToneRadius * a2b;
					radius = (int)transposed;
				} else {
					radius = HighToneRadius;
				}
				/* 探索半径内の最大値を取得 */
				ixStart = Math.Max(ix - radius, 0);
				ixEnd = Math.Min(ix + radius, BANK_COUNT - 1);
				if (ixMaxL < ixStart) {
					maxL = double.MinValue;
					for (ixWindow = ixStart; ixWindow <= ixEnd; ++ixWindow) {
						pBank = mpBanks + ixWindow;
						if (pBank->PowerL > maxL) {
							maxL = pBank->PowerL;
							ixMaxL = ixWindow;
						}
					}
				} else {
					pBank = mpBanks + ixEnd;
					if (pBank->PowerL >= maxL) {
						maxL = pBank->PowerL;
						ixMaxL = ixEnd;
					}
				}
				if (ixMaxR < ixStart) {
					maxR = double.MinValue;
					for (ixWindow = ixStart; ixWindow <= ixEnd; ++ixWindow) {
						pBank = mpBanks + ixWindow;
						if (pBank->PowerR > maxR) {
							maxR = pBank->PowerR;
							ixMaxR = ixWindow;
						}
					}
				} else {
					pBank = mpBanks + ixEnd;
					if (pBank->PowerR >= maxR) {
						maxR = pBank->PowerR;
						ixMaxR = ixEnd;
					}
				}
				/* 平均値を取得 */
				ampL = 0;
				ampR = 0;
				radius = AvgRadius;
				ixStart = Math.Max(ix - radius, 0);
				ixEnd = Math.Min(ix + radius, BANK_COUNT - 1);
				for (ixWindow = ixStart; ixWindow <= ixEnd; ++ixWindow) {
					pBank = mpBanks + ixWindow;
					ampL += pBank->PowerL;
					ampR += pBank->PowerR;
				}
				radius = radius * 2 + 1;
				ampL /= radius;
				ampR /= radius;
				/* ピーク抽出用の閾値を取得 */
				thresholdL = Math.Max(ampL, maxL);
				thresholdR = Math.Max(ampR, maxR);
				thresholdL = Math.Sqrt(thresholdL * 2.0);
				thresholdR = Math.Sqrt(thresholdR * 2.0);
				/* 波形合成用のピークを抽出 */
				pBank = mpBanks + ix;
				ampL = Math.Sqrt(pBank->PowerL * 2.0);
				ampR = Math.Sqrt(pBank->PowerR * 2.0);
				pBank->PeakL = ampL < thresholdL ? 0 : (float)ampL;
				pBank->PeakR = ampR < thresholdR ? 0 : (float)ampR;
				/* 表示用の曲線/閾値/ピークを設定 */
				ampL = Math.Max(ampL, ampR);
				thresholdL = Math.Max(thresholdL, thresholdR);
				DisplayData[ix] = ampL;
				DisplayData[ix + BANK_COUNT] = thresholdL;
				DisplayData[ix + BANK_COUNT * 2] = ampL < thresholdL ? 0 : ampL;
			}
		}

		private unsafe void UpdateAutoGain(int sampleCount) {
			/* 最大値を更新 */
			var max = AutoGainMin;
			for (int ix = 0; ix < BANK_COUNT; ++ix) {
				var b = mpBanks + ix;
				var amp = Math.Sqrt(Math.Max(b->PowerL, b->PowerR) * 2);
				max = Math.Max(max, amp);
			}
			Max = max;
			/* 最大値に追随して自動ゲインを更新 */
			var autoGain = AutoGain;
			var diff = max - autoGain;
			var tau = diff < 0 ? AutoGainDecTime : AutoGainIncTime;
			var delta = (double)sampleCount / SampleRate;
			delta = 1.0 - Math.Exp(-delta / tau);
			autoGain += diff * delta;
			if (autoGain < AutoGainMin) {
				autoGain = AutoGainMin;
			}
			AutoGain = autoGain;
		}
	}
}
