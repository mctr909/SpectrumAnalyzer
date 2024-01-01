using System;
using System.Runtime.InteropServices;

namespace SignalProcess {
	public class Spectrum : IDisposable {
		#region [定数]
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 125;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 4;
		/// <summary>フィルタバンクにおける半音の中心インデックス</summary>
		public const int HALFTONE_CENTER = HALFTONE_DIV >> 1;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;
		/// <summary>フィルタバンク数</summary>
		public const int BANK_COUNT = HALFTONE_DIV * HALFTONE_COUNT;
		/// <summary>A4ピッチ</summary>
		public const double A4_PITCH = 440.0;
		/// <summary>基本周波数</summary>
		public static readonly double BASE_FREQ = A4_PITCH * Math.Pow(2, 3.0 / 12.0 + (1.0 / HALFTONE_DIV - 1) / 12.0 - 5);
		/// <summary>近傍平均の半径</summary>
		private const int NEAR_AVG_RADIUS = HALFTONE_DIV * 4;
		/// <summary>近傍平均の中心からの距離に対するゲイン</summary>
		private const float NEAR_AVG_GAIN = 0.01f;
		/// <summary>近傍平均の減衰時間</summary>
		private const float NEAR_AVG_ATT_TIME = 4e-3f;
		#endregion

		#region [設定値]
		/// <summary>ゲイン自動調整 最小値</summary>
		public double AutoGainMin { get; set; } = Math.Pow(10, -24 / 20.0);
		/// <summary>ゲイン自動調整 減少時間[秒]</summary>
		public double AutoGainDecTime { get; set; } = 3.0;
		/// <summary>ゲイン自動調整 増加時間[秒]</summary>
		public double AutoGainIncTime { get; set; } = 1e-2;
		/// <summary>バンド幅の上限[半音]</summary>
		public double BandWidthMax { get; set; } = 7.0;
		/// <summary>バンド幅の下限[半音]</summary>
		public double BandWidthFloor { get; set; } = 0.25;
		/// <summary>バンド幅の変化の急峻さ[オクターブ]</summary>
		public double BandWidthTransition { get; set; } = 6.8;
		#endregion

		#region [公開メンバ]
		/// <summary>サンプリング周波数</summary>
		public int SampleRate { get; private set; }
		/// <summary>瞬間最大値</summary>
		public double Max { get; private set; }
		/// <summary>平滑化最大値</summary>
		public double SmoothedMax { get; private set; }
		/// <summary>表示用データ</summary>
		public readonly double[] DisplayData = new double[BANK_COUNT * 3];
		/// <summary>波形合成用データ</summary>
		internal unsafe BpfBank* mpBanks = null;
		#endregion

		private struct Avg {
			public float L;
			public float R;
		}
		private readonly Avg[] mAvg = new Avg[BANK_COUNT];

		[StructLayout(LayoutKind.Sequential)]
		internal struct BpfBank {
			public float Ka1;
			public float Ka2;
			public float Kb0;
			public float MsDelta;

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
			public float PeakL;
			public float PeakR;
		}

		private readonly Fft.Vec[] mFft = new Fft.Vec[1024];

		/// <summary>
		/// スペクトラムを生成
		/// </summary>
		public unsafe Spectrum() {
			Max = AutoGainMin;
			SmoothedMax = AutoGainMin;
			try {
				mpBanks = (BpfBank*)Marshal.AllocHGlobal(sizeof(BpfBank) * BANK_COUNT);
			} catch (OutOfMemoryException) {
				mpBanks = null;
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
		/// <param name="sampleRate">サンプリング周波数</param>
		/// <param name="initialize">初期化を行うか</param>
		public unsafe void SetupFilter(int sampleRate = 44100, bool initialize = true) {
			if (null == mpBanks) {
				return;
			}
			SampleRate = sampleRate;
			var nyquistLimit = sampleRate * (0.5 - 1e-2);
			var bandWidthTransitionScale = 2.0 / (BandWidthTransition * OCT_DIV);
			var bandWidthScale = BandWidthMax - BandWidthFloor;
			var pBank = mpBanks;
			for (int ix = 0; ix < BANK_COUNT; ++ix) {
				/* バンクによってバンド幅を変える */
				var bw = bandWidthTransitionScale * ix;
				bw = bandWidthScale * Math.Exp(-bw * bw);
				var bandWidth = (BandWidthFloor + bw) / 12.0;
				/* 中心周波数、ナイキスト周波数以上にならないように制限 */
				var f0 = Math.Min(BASE_FREQ * Math.Pow(2.0, (double)ix / OCT_DIV), nyquistLimit);
				/* 正規化周波数 */
				var fn = f0 / sampleRate;
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
					bank = *pBank;
				}
				bank.Ka1 = (float)(2.0 * c / a0);
				bank.Ka2 = (float)(-(1.0 - alpha) / a0);
				bank.Kb0 = (float)(alpha / a0);
				/* MSの応答速度を設定 */
				bank.MsDelta = (float)(1.0 - Math.Exp(-fn));
				*pBank = bank;
				pBank++;
			}
			for (int i = 0; i<mFft.Length; i++) {
				mFft[i] = new Fft.Vec();
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
			ExtractPeak(sampleCount);
		}

		private unsafe void CalcPower(IntPtr pInput, int sampleCount) {
			var pBank = mpBanks;
			var pBankTerm = mpBanks + BANK_COUNT;
			/* デノーマル対策 */
			const float AntiDenormal = 1e-9f;
			while (pBank < pBankTerm) {
				pBank->La1 += AntiDenormal;
				pBank->La2 -= AntiDenormal;
				pBank->Lb1 += AntiDenormal;
				pBank->Lb2 -= AntiDenormal;
				pBank->Ra1 += AntiDenormal;
				pBank->Ra2 -= AntiDenormal;
				pBank->Rb1 += AntiDenormal;
				pBank->Rb2 -= AntiDenormal;
				pBank++;
			}
			/* 波形サンプルループ */
			var pWave = (float*)pInput;
			var pWaveStart = pWave;
			var pWaveTerm = pWave + sampleCount * 2;
			while (pWave < pWaveTerm) {
				/* フィルタバンクループ */
				pBank = mpBanks;
				while (pBank < pBankTerm) {
					float ka1 = pBank->Ka1;
					float ka2 = pBank->Ka2;
					float kb0 = pBank->Kb0;
					float msDelta = pBank->MsDelta;
					float la1 = pBank->La1;
					float la2 = pBank->La2;
					float lb1 = pBank->Lb1;
					float lb2 = pBank->Lb2;
					float ra1 = pBank->Ra1;
					float ra2 = pBank->Ra2;
					float rb1 = pBank->Rb1;
					float rb2 = pBank->Rb2;
					float a0, b0;
					float powerL = pBank->PowerL;
					float powerR = pBank->PowerR;
					/* BPF(左) */
					b0 = *pWave;
					a0 = b0 - lb2;
					a0 *= kb0;
					a0 += la1 * ka1;
					a0 += la2 * ka2;
					la2 = la1;
					la1 = a0;
					lb2 = lb1;
					lb1 = b0;
					/* MS(左) */
					a0 *= a0;
					a0 -= powerL;
					powerL += a0 * msDelta;
					/* BPF(右) */
					b0 = *(pWave + 1);
					a0 = b0 - rb2;
					a0 *= kb0;
					a0 += ra1 * ka1;
					a0 += ra2 * ka2;
					ra2 = ra1;
					ra1 = a0;
					rb2 = rb1;
					rb1 = b0;
					/* MS(右) */
					a0 *= a0;
					a0 -= powerR;
					powerR += a0 * msDelta;
					/* 状態を更新 */
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
				}
				/* 次の波形サンプルへ */
				pWave += 2;
			}
		}

		private unsafe void ExtractPeak(int sampleCount) {
			float avgK = 1 - (float)Math.Exp(-sampleCount / (NEAR_AVG_ATT_TIME * SampleRate));
			float avgL, avgR;
			float centerL, centerR;
			float peakL, peakR;
			bool isPeakL, isPeakR;
			int ib, iw;
			int iwStart, iwEnd;
			BpfBank* pBank;
			for (ib = 0; ib < BANK_COUNT; ++ib) {
				/* 近傍平均(avg)を取得 */
				avgL = 0;
				avgR = 0;
				iwStart = Math.Max(ib - NEAR_AVG_RADIUS, 0);
				iwEnd = Math.Min(ib + NEAR_AVG_RADIUS, BANK_COUNT - 1);
				for (iw = iwStart; iw <= iwEnd; ++iw) {
					var t = (iw - ib) * 2.718f / HALFTONE_DIV;
					var gain = 1 + NEAR_AVG_GAIN - NEAR_AVG_GAIN * (float)Math.Exp(-t * t);
					pBank = mpBanks + iw;
					avgL += gain * pBank->PowerL;
					avgR += gain * pBank->PowerR;
				}
				iwEnd++;
				iwEnd -= iwStart;
				avgL /= iwEnd;
				avgR /= iwEnd;
				var diffL = avgL - mAvg[ib].L;
				var diffR = avgR - mAvg[ib].R;
				diffL *= mAvg[ib].L < avgL ? 1 : avgK;
				diffR *= mAvg[ib].R < avgR ? 1 : avgK;
				mAvg[ib].L += diffL;
				mAvg[ib].R += diffR;
				avgL = mAvg[ib].L;
				avgR = mAvg[ib].R;

				// 中心バンク
				pBank = mpBanks + ib;
				centerL = pBank->PowerL;
				centerR = pBank->PowerR;

				/* 局所最大であるか(isPeak)を取得 */
				// 中心バンク[-1]と比較
				pBank = mpBanks + Math.Max(ib - 1, 0);
				isPeakL = centerL > pBank->PowerL;
				isPeakR = centerR > pBank->PowerR;
				peakL = Math.Max(centerL, pBank->PowerL);
				peakR = Math.Max(centerR, pBank->PowerR);
				// 中心バンク[+1]と比較
				pBank = mpBanks + Math.Min(ib + 1, BANK_COUNT - 1);
				isPeakL &= centerL > pBank->PowerL;
				isPeakR &= centerR > pBank->PowerR;
				peakL = Math.Max(peakL, pBank->PowerL);
				peakR = Math.Max(peakR, pBank->PowerR);

				/* 局所最大と近傍平均をもとに閾値を取得 */
				peakL = Math.Max(peakL, avgL);
				peakR = Math.Max(peakR, avgR);
				isPeakL &= centerL == peakL;
				isPeakR &= centerR == peakR;

				/* 波形合成用のピークを設定 */
				centerL = (float)Math.Sqrt(centerL * 2);
				centerR = (float)Math.Sqrt(centerR * 2);
				pBank = mpBanks + ib;
				pBank->PeakL = isPeakL ? centerL : 0;
				pBank->PeakR = isPeakR ? centerR : 0;

				/* 表示用の曲線/閾値/ピークを設定 */
				centerL = Math.Max(centerL, centerR);
				peakL = Math.Max(peakL, peakR);
				peakL = (float)Math.Sqrt(peakL * 2);
				isPeakL |= isPeakR;
				isPeakL &= centerL >= peakL;
				//DisplayData[ib] = centerL;
				DisplayData[ib + BANK_COUNT] = peakL;
				mFft[ib].x = isPeakL ? centerL : 0;
				DisplayData[ib + BANK_COUNT * 2] = isPeakL ? centerL : 0;
			}
			Fft.Cepstrum(mFft, BANK_COUNT);
			for (ib = 0; ib < BANK_COUNT; ++ib) {
				DisplayData[ib] = mFft[ib].x;
			}
		}

		private unsafe void UpdateAutoGain(int sampleCount) {
			/* 瞬間最大値を更新 */
			var max = AutoGainMin;
			for (int ix = 0; ix < BANK_COUNT; ++ix) {
				var b = mpBanks + ix;
				var amp = Math.Sqrt(Math.Max(b->PowerL, b->PowerR) * 2);
				max = Math.Max(max, amp);
			}
			Max = max;
			/* 瞬間最大値に追随して平滑化最大値を更新 */
			var smoothedMax = SmoothedMax;
			var diff = max - smoothedMax;
			var tau = diff < 0 ? AutoGainDecTime : AutoGainIncTime;
			var delta = (double)sampleCount / SampleRate;
			delta = 1.0 - Math.Exp(-delta / tau);
			smoothedMax += diff * delta;
			if (smoothedMax < AutoGainMin) {
				smoothedMax = AutoGainMin;
			}
			SmoothedMax = smoothedMax;
		}
	}
}
