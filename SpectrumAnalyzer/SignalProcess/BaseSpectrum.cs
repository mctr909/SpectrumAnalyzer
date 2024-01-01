using System;
using System.Runtime.InteropServices;

namespace SignalProcess {
	public abstract class BaseSpectrum {
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 125;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 4;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;
		/// <summary>A4ピッチ</summary>
		public const double A4_PITCH = 440.0;

		/// <summary>最低周波数</summary>
		public static readonly double MinFreq = A4_PITCH * Math.Pow(2, 3.0 / 12.0 + (1.0 / HALFTONE_DIV - 1) / 12.0 - 5);
		/// <summary>最高周波数</summary>
		public static readonly double MaxFreq = A4_PITCH * Math.Pow(2, 3.0 / 12.0 + (HALFTONE_COUNT - 1.0 / HALFTONE_DIV) / 12.0 - 5);
		/// <summary>フィルタバンク数</summary>
		public static readonly int BankCount = (int)(OCT_DIV * Math.Log(MaxFreq / MinFreq, 2.0));

		/// <summary>近傍平均の半径</summary>
		private const int NEAR_AVG_RADIUS = HALFTONE_DIV * 6;
		/// <summary>近傍平均のゲイン</summary>
		private const float NEAR_AVG_GAIN = 1.01f;
		/// <summary>近傍平均の中心からの距離に対するゲイン</summary>
		private const float NEAR_AVG_DIST_GAIN = 0.01f;
		/// <summary>近傍平均の減衰時間</summary>
		private const float NEAR_AVG_ATT_TIME = 5e-3f;

		/// <summary>ゲイン自動調整 最小値</summary>
		public double AutoGainMin { get; set; } = Math.Pow(10, -24 / 20.0);
		/// <summary>ゲイン自動調整 減少時間[秒]</summary>
		public double AutoGainDecTime { get; set; } = 3.0;
		/// <summary>ゲイン自動調整 増加時間[秒]</summary>
		public double AutoGainIncTime { get; set; } = 1e-2;
		/// <summary>瞬間最大値</summary>
		public double Max { get; private set; }
		/// <summary>平滑化最大値</summary>
		public double SmoothedMax { get; private set; }
		/// <summary>波形合成用データ</summary>
		public readonly Bank[] Banks = new Bank[BankCount];
		/// <summary>表示用データ</summary>
		public readonly double[] DisplayData = new double[BankCount * 3];

		[StructLayout(LayoutKind.Sequential)]
		public struct Bank {
			public float PowerL;
			public float PowerR;
			public float PeakL;
			public float PeakR;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Stereo {
			public float L;
			public float R;
		}

		private readonly Stereo[] TimeAvg = new Stereo[BankCount];

		private readonly int SampleRate;

		protected BaseSpectrum(int sampleRate) {
			SampleRate = sampleRate;
			Max = AutoGainMin;
			SmoothedMax = AutoGainMin;
		}

		/// <summary>
		/// スペクトルを更新
		/// </summary>
		/// <param name="pInput">入力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sampleCount">入力バッファのサンプル数</param>
		public void Update(IntPtr pInput, int sampleCount) {
			Calc(pInput, sampleCount);
			ExtractPeak(sampleCount);
			UpdateAutoGain(sampleCount);
		}

		/// <summary>
		/// スペクトルを計算
		/// </summary>
		/// <param name="pInput">入力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sampleCount">入力バッファのサンプル数</param>
		protected abstract void Calc(IntPtr pInput, int sampleCount);

		private void ExtractPeak(int sampleCount) {
			float avgTimeDump = 1 - (float)Math.Exp(-sampleCount / (NEAR_AVG_ATT_TIME * SampleRate));
			for (int ib = 0; ib < BankCount; ib++) {
				/* 近傍平均 */
				double avgL, avgR;
				{
					/* 周波数方向近傍平均 */
					var avgFreqL = 0f;
					var avgFreqR = 0f;
					var iwStart = Math.Max(ib - NEAR_AVG_RADIUS, 0);
					var iwEnd = Math.Min(ib + NEAR_AVG_RADIUS, BankCount - 1);
					for (int iw = iwStart; iw <= iwEnd; iw++) {
						var t = (iw - ib) * (float)Math.E / HALFTONE_DIV;
						var gain = 1 + NEAR_AVG_DIST_GAIN - NEAR_AVG_DIST_GAIN * (float)Math.Exp(-t * t);
						ref var bw = ref Banks[iw];
						avgFreqL += gain * bw.PowerL;
						avgFreqR += gain * bw.PowerR;
					}
					iwEnd++;
					iwEnd -= iwStart;
					avgFreqL *= NEAR_AVG_GAIN / iwEnd;
					avgFreqR *= NEAR_AVG_GAIN / iwEnd;

					/* 時間方向平均 */
					ref var avgTime = ref TimeAvg[ib];
					var diffL = avgFreqL - avgTime.L;
					var diffR = avgFreqR - avgTime.R;
					diffL *= avgTime.L < avgFreqL ? 1 : avgTimeDump;
					diffR *= avgTime.R < avgFreqR ? 1 : avgTimeDump;
					avgTime.L += diffL;
					avgTime.R += diffR;
					avgL = avgTime.L;
					avgR = avgTime.R;
				}

				// 中心バンク
				ref var center = ref Banks[ib];
				var centerL = center.PowerL;
				var centerR = center.PowerR;

				/* 局所最大であるか(isPeak)を取得 */
				// 中心バンク[-1]と比較
				ref var prev = ref Banks[Math.Max(ib - 1, 0)];
				var isPeakL = centerL > prev.PowerL;
				var isPeakR = centerR > prev.PowerR;
				var peakL = Math.Max(centerL, prev.PowerL);
				var peakR = Math.Max(centerR, prev.PowerR);
				// 中心バンク[+1]と比較
				ref var next = ref Banks[Math.Min(ib + 1, BankCount - 1)];
				isPeakL &= centerL > next.PowerL;
				isPeakR &= centerR > next.PowerR;
				peakL = Math.Max(peakL, next.PowerL);
				peakR = Math.Max(peakR, next.PowerR);

				/* 近傍平均と局所最大を基に閾値を取得 */
				var thresholdL = Math.Max(avgL, peakL);
				var thresholdR = Math.Max(avgR, peakR);
				isPeakL &= centerL >= thresholdL;
				isPeakR &= centerR >= thresholdR;

				/* リニア化 */
				var threshold = (float)Math.Sqrt(Math.Max(thresholdL, thresholdR) * 2);
				var ampL = (float)Math.Sqrt(centerL * 2);
				var ampR = (float)Math.Sqrt(centerR * 2);

				/* 波形合成用のピークを設定 */
				center.PeakL = isPeakL ? ampL : 0;
				center.PeakR = isPeakR ? ampR : 0;

				/* 表示用曲線/閾値/ピークを設定 */
				ampL = Math.Max(ampL, ampR);
				isPeakL |= isPeakR;
				isPeakL &= ampL >= threshold;
				var peak = isPeakL ? ampL : 0;
				DisplayData[ib] = ampL;
				DisplayData[ib + BankCount] = threshold;
				DisplayData[ib + BankCount * 2] = peak;
			}
		}

		private void UpdateAutoGain(int sampleCount) {
			/* 瞬間最大値を更新 */
			var max = AutoGainMin;
			for (int ix = 0; ix < BankCount; ix++) {
				ref var b = ref Banks[ix];
				var amp = Math.Max(b.PeakL, b.PeakR);
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
