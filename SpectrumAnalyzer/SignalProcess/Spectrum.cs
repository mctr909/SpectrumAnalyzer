using System;
using System.Runtime.InteropServices;

namespace SignalProcess {
	public class Spectrum : IDisposable {
		#region [定数]
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 125;
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
		/// <summary>帯域幅が1半音に至る周波数</summary>
		public double BandWidthHalftoneAtFreq { get; set; } = 300.0;
		/// <summary>1オクターブあたりの帯域幅変化量[半音]</summary>
		public double BandWidthHalftonesPerOctave { get; set; } = 1.5;

		/// <summary>ゲイン自動調整 最小値</summary>
		public double AutoGainMin { get; set; } = 1e-3;
		/// <summary>ゲイン自動調整 減少時間[秒]</summary>
		public double AutoGainDecTime { get; set; } = 10.0;
		/// <summary>ゲイン自動調整 増加時間[秒]</summary>
		public double AutoGainIncTime { get; set; } = 1e-2;
		/// <summary>平均値 半径[フィルタバンク数]</summary>
		public int AvgRadius { get; set; } = 4;
		/// <summary>平均値 ゲイン[10^(db/10)]</summary>
		public double AvgGain { get; set; } = Math.Pow(10, 0.1 / 10.0);
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
		/// <param name="halftoneAtFreq"></param>
		/// <param name="halftonesPerOctave"></param>
		public unsafe void SetupFilter(bool initialize) {
			if (null == mpBanks) {
				return;
			}
			var pBank = mpBanks;
			for (int ix = 0; ix < BANK_COUNT; ++ix) {
				/* 中心周波数、ナイキスト周波数以上にならないように制限 */
				var f0 = BASE_FREQ * Math.Pow(2.0, (double)ix / OCT_DIV);
				f0 = Math.Min(f0, SampleRate * (0.5 - 1e-2));
				/* 中心周波数によってバンド幅を変える */
				var halftone = 1.0 + Math.Log(BandWidthHalftoneAtFreq / f0, 2.0) * BandWidthHalftonesPerOctave;
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
				if (f0 < BandWidthHalftoneAtFreq) {
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
			const float AntiDenormal = 1e-20f;
			var pBank = mpBanks;
			var pBankTerm = mpBanks + BANK_COUNT;
			var pWave = (float*)pInput;
			var pWaveStart = pWave;
			var pWaveTerm = pWave + sampleCount * 2;
			/* フィルタバンクループ */
			while (pBank < pBankTerm) {
				float la1 = pBank->La1 + AntiDenormal;
				float la2 = pBank->La2 - AntiDenormal;
				float lb1 = pBank->Lb1 + AntiDenormal;
				float lb2 = pBank->Lb2 - AntiDenormal;
				float ra1 = pBank->Ra1 + AntiDenormal;
				float ra2 = pBank->Ra2 - AntiDenormal;
				float rb1 = pBank->Rb1 + AntiDenormal;
				float rb2 = pBank->Rb2 - AntiDenormal;
				float a0;
				float b0;
				float powerL = pBank->PowerL + AntiDenormal;
				float powerR = pBank->PowerR + AntiDenormal;
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
			BpfBank* pBank;
			for (int ix = 0; ix < BANK_COUNT; ++ix) {
				/* 局所ピークと最大値を取得 */
				pBank = mpBanks + ix;
				var powerL = pBank->PowerL;
				var powerR = pBank->PowerR;
				pBank = mpBanks + Math.Max(ix - 1, 0);
				var isPeakL = powerL > pBank->PowerL;
				var isPeakR = powerR > pBank->PowerR;
				var maxL = Math.Max(powerL, pBank->PowerL);
				var maxR = Math.Max(powerR, pBank->PowerR);
				pBank = mpBanks + Math.Min(ix + 1, BANK_COUNT - 1);
				isPeakL &= powerL > pBank->PowerL;
				isPeakR &= powerR > pBank->PowerR;
				maxL = Math.Max(maxL, pBank->PowerL);
				maxR = Math.Max(maxR, pBank->PowerR);
				/* 平均値を取得 */
				var avgL = 0.0;
				var avgR = 0.0;
				var ixStart = Math.Max(ix - AvgRadius, 0);
				var ixEnd = Math.Min(ix + AvgRadius, BANK_COUNT - 1);
				for (int i = ixStart; i <= ixEnd; ++i) {
					pBank = mpBanks + i;
					avgL += pBank->PowerL;
					avgR += pBank->PowerR;
				}
				var width = AvgRadius * 2 + 1;
				avgL *= AvgGain / width;
				avgR *= AvgGain / width;
				/* 平均値と最大値をもとに閾値を取得 */
				var thresholdL = Math.Max(avgL, maxL);
				var thresholdR = Math.Max(avgR, maxR);
				/* 波形合成用のピークを設定 */
				pBank = mpBanks + ix;
				avgL = pBank->PowerL;
				avgR = pBank->PowerR;
				isPeakL &= avgL >= thresholdL;
				isPeakR &= avgR >= thresholdR;
				avgL = Math.Sqrt(avgL * 2.0);
				avgR = Math.Sqrt(avgR * 2.0);
				pBank->PeakL = isPeakL ? (float)avgL : 0;
				pBank->PeakR = isPeakR ? (float)avgR : 0;
				/* 表示用の曲線/閾値/ピークを設定 */
				avgL = Math.Max(avgL, avgR);
				thresholdL = Math.Max(thresholdL, thresholdR);
				thresholdL = Math.Sqrt(thresholdL * 2.0);
				isPeakL |= isPeakR;
				isPeakL &= avgL >= thresholdL;
				DisplayData[ix] = avgL;
				DisplayData[ix + BANK_COUNT] = thresholdL;
				DisplayData[ix + BANK_COUNT * 2] = isPeakL ? avgL : 0;
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
