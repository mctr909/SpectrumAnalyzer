using System;
using System.Runtime.InteropServices;

namespace Spectrum {
	public class Spectrum : IDisposable {
		#region [定数]
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 126;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 4;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;
		/// <summary>フィルタバンク数</summary>
		public const int BANK_COUNT = HALFTONE_DIV * HALFTONE_COUNT;
		#endregion

		#region [設定値]
		/// <summary>基本周波数</summary>
		public static double BaseFreq = 442 * Math.Pow(2, 3.0 / 12.0 + (1.0 / HALFTONE_DIV - 1) / 12.0 - 5);
		/// <summary>帯域幅が1半音に至る周波数</summary>
		public static double HalftoneAtFreq = 300.0;

		/// <summary>ゲイン自動調整 最小値</summary>
		public static double AutoGainMin = 1.0 / 256;
		/// <summary>ゲイン自動調整 減少時間[秒]</summary>
		public static double AutoGainDecTime = 3.0;
		/// <summary>ゲイン自動調整 増加時間[秒]</summary>
		public static double AutoGainIncTime = 0.01;

		/// <summary>中音域閾値 開始位置[フィルタバンク数]</summary>
		public static int MidTone = HALFTONE_DIV * 24;
		/// <summary>高音域閾値 開始位置[フィルタバンク数]</summary>
		public static int HighTone = HALFTONE_DIV * 72;
		/// <summary>低音域閾値 半径[フィルタバンク数]</summary>
		public static int LowToneRadius = 3;
		/// <summary>高音域閾値 半径[フィルタバンク数]</summary>
		public static int HighToneRadius = 1;
		/// <summary>平均値 ゲイン[sqrt(10^(デシベル/20))]</summary>
		public static double AvgGain = Math.Sqrt(Math.Pow(10, 1.0/20.0));
		#endregion

		#region [公開メンバ]
		/// <summary>ゲイン自動調整有効フラグ</summary>
		public bool EnableAutoGain { get; set; } = true;
		/// <summary>正規化有効フラグ</summary>
		public bool EnableNormalize { get; set; } = false;
		/// <summary>トランスポーズ[半音]</summary>
		public double Transpose { get; set; } = 0.0;
		/// <summary>最大値</summary>
		public double Max { get; private set; } = AutoGainMin;
		/// <summary>自動ゲイン</summary>
		public double AutoGain { get; private set; } = AutoGainMin;
		/// <summary>波形合成用ピーク</summary>
		public PeakBank[] PeakBanks { get; private set; } = new PeakBank[BANK_COUNT];
		/// <summary>表示用データ</summary>
		public double[] DisplayData { get; private set; } = new double[BANK_COUNT * 4];
		#endregion

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct BpfBank {
			public float La1;
			public float La2;
			public float Lb1;
			public float Lb2;

			public float Ra1;
			public float Ra2;
			public float Rb1;
			public float Rb2;

			public float PowerL;
			public float PowerR;

			public float Ka1;
			public float Ka2;
			public float Kb0;
			public float RmsSpeed;

			public unsafe void SetCoefficients(int sampleRate, double halftoneAtFreq, double frequency) {
				/* フィルタリング周波数によってバンド幅を変える */
				var halftone = 0.5 + Math.Log(halftoneAtFreq / frequency, 2.0);
				if (halftone < 0.5) {
					halftone = 0.5;
				}
				var bandWidth = halftone / 12.0;
				/* バイクアッドフィルタ(BPF)の係数を設定 */
				var omega = 2.0 * Math.PI * frequency / sampleRate;
				var c = Math.Cos(omega);
				var s = Math.Sin(omega);
				var x = Math.Log(2.0) / 2.0 * bandWidth * omega / s;
				var alpha = s * Math.Sinh(x);
				var a0 = 1.0 + alpha;
				Ka1 = (float)(2.0 * c / a0);
				Ka2 = (float)(-(1.0 - alpha) / a0);
				Kb0 = (float)(alpha / a0);
				/* RMSの応答速度を設定 */
				if (frequency < 150) {
					frequency = 150;
				}
				RmsSpeed = (float)(0.5 * frequency / sampleRate);
			}
		}

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern int _controlfp_s(IntPtr currentControl, uint newControl, uint mask);
		private const uint _DN_FLUSH = 0x01000000; // FTZ (Flush To Zero) に相当
		private const uint _MCW_DN = 0x03000000;   // デノーマル制御マスク

		private readonly int mSampleRate;
		private readonly unsafe BpfBank* mpBanks = null;

		/// <summary>
		/// スペクトラムを生成
		/// </summary>
		/// <param name="sampleRate">サンプリング周波数</param>
		public unsafe Spectrum(int sampleRate) {
			mSampleRate = sampleRate;
			mpBanks = (BpfBank*)Marshal.AllocHGlobal(BANK_COUNT * sizeof(BpfBank));
			if (mpBanks != null) {
				SetFilter(true);
			}
			_controlfp_s(IntPtr.Zero, _DN_FLUSH, _MCW_DN);
		}

		public unsafe void Dispose() {
			if (mpBanks != null) {
				Marshal.FreeHGlobal((IntPtr)mpBanks);
			}
		}

		/// <summary>
		/// フィルタを設定
		/// </summary>
		/// <param name="initialize">初期化を行うか</param>
		public unsafe void SetFilter(bool initialize = false) {
			if (null == mpBanks) {
				return;
			}
			var pBanks = mpBanks;
			for (int i = 0; i < BANK_COUNT; ++i) {
				BpfBank bank;
				if (initialize) {
					bank = new BpfBank();
				} else {
					bank = Marshal.PtrToStructure<BpfBank>((IntPtr)pBanks);
				}
				var frequency = BaseFreq * Math.Pow(2.0, (double)i / OCT_DIV);
				bank.SetCoefficients(mSampleRate, HalftoneAtFreq, frequency);
				Marshal.StructureToPtr(bank, (IntPtr)pBanks, false);
				PeakBanks[i] = new PeakBank
				{
					DELTA = frequency / mSampleRate
				};
				pBanks++;
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
			CalcMeanSquare(pInput, sampleCount);
			UpdateAutoGain(sampleCount);
			ExtractPeak();
		}

		private unsafe void CalcMeanSquare(IntPtr pInput, int sampleCount) {
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

		private unsafe void UpdateAutoGain(int sampleCount) {
			/* 最大値を更新 */
			Max = AutoGainMin;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var b = mpBanks + ixB;
				var amp = Math.Sqrt(Math.Max(b->PowerL, b->PowerR) * 2);
				Max = Math.Max(Max, amp);
			}

			/* 最大値に追随して自動ゲインを更新 */
			var diff = Max - AutoGain;
			var delta = (double)sampleCount / mSampleRate;
			delta /= diff < 0 ? AutoGainDecTime : AutoGainIncTime;
			AutoGain += diff * delta;
			if (AutoGain < AutoGainMin) {
				AutoGain = AutoGainMin;
			}
		}

		private unsafe void ExtractPeak() {
			int ixMaxL = -1, ixMaxR = -1;
			int ix, ixWindow, ixStart, ixEnd;
			int radius;
			double maxL = double.MinValue, maxR = double.MinValue;
			double ampL, ampR;
			double thresholdL, thresholdR;
			double transposed;
			double a2b;
			BpfBank* pBank;
			for (ix = 0; ix < BANK_COUNT; ++ix) {
				/*** バンクに対応した最大値の探索半径を設定 ***/
				{
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
				}
				/*** 最大値を取得 ***/
				{
					ixStart = Math.Max(0, ix - radius);
					ixEnd = Math.Min(BANK_COUNT - 1, ix + radius);
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
				}
				/*** 平均値を取得 ***/
				{
					ampL = 0.0;
					ampR = 0.0;
					radius *= 4;
					ixStart = Math.Max(0, ix - radius);
					ixEnd = Math.Min(BANK_COUNT - 1, ix + radius);
					for (ixWindow = ixStart; ixWindow <= ixEnd; ++ixWindow) {
						pBank = mpBanks + ixWindow;
						ampL += pBank->PowerL;
						ampR += pBank->PowerR;
					}
					radius = radius * 2 + 1;
					ampL *= AvgGain / radius;
					ampR *= AvgGain / radius;
				}
				/*** ピーク抽出用の閾値を取得 ***/
				thresholdL = Math.Max(ampL, maxL);
				thresholdR = Math.Max(ampR, maxR);
				thresholdL = Math.Sqrt(thresholdL * 2);
				thresholdR = Math.Sqrt(thresholdR * 2);
				/*** 波形合成用のピークを抽出 ***/
				pBank = mpBanks + ix;
				ampL = Math.Sqrt(pBank->PowerL * 2);
				ampR = Math.Sqrt(pBank->PowerR * 2);
				var peak = PeakBanks[ix];
				peak.L = ampL < thresholdL ? 0.0 : ampL;
				peak.R = ampR < thresholdR ? 0.0 : ampR;
				/*** 表示用の曲線/閾値/ピークを設定 ***/
				ampL = Math.Max(ampL, ampR);
				thresholdL = Math.Max(thresholdL, thresholdR);
				if (EnableNormalize) {
					ampL /= Max;
					thresholdL /= Max;
				}
				if (EnableAutoGain) {
					ampL /= AutoGain;
					thresholdL /= AutoGain;
				}
				DisplayData[ix] = ampL;
				DisplayData[ix + BANK_COUNT] = thresholdL;
				DisplayData[ix + BANK_COUNT * 2] = ampL < thresholdL ? 0.0 : ampL;
			}
			for (ix = 0; ix < BANK_COUNT; ++ix) {
				ixStart = BANK_COUNT * 2;
				ixStart += Math.Max(0, ix - 1);
				ixWindow = BANK_COUNT * 2;
				ixWindow += ix;
				ixEnd = BANK_COUNT * 2;
				ixEnd += Math.Min(BANK_COUNT - 1, ix + 1);
				ampL = DisplayData[ixStart];
				ampL = Math.Max(DisplayData[ixWindow], ampL);
				ampL = Math.Max(DisplayData[ixEnd], ampL);
				DisplayData[ix + BANK_COUNT * 3] = ampL;
			}
		}
	}
}
