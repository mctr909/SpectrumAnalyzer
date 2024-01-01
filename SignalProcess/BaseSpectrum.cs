using System.Runtime.InteropServices;

namespace SignalProcess;

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

	/// <summary>近傍平均の半径(最大)</summary>
	private const int NEAR_AVG_RADIUS_MAX = HALFTONE_DIV * 6;
	/// <summary>近傍平均の半径(最小)</summary>
	private const int NEAR_AVG_RADIUS_MIN = HALFTONE_DIV;
	/// <summary>近傍平均の半径の遷移開始位置</summary>
	private const float NEAR_AVG_RADIUS_TRANS_START = 2.5f;
	/// <summary>近傍平均の半径の遷移幅</summary>
	private const float NEAR_AVG_RADIUS_TRANS_WIDTH = 1.0f;
	/// <summary>近傍平均の中心からの距離に対するゲイン</summary>
	private const float NEAR_AVG_DIST_GAIN = 0.414f;
	/// <summary>近傍平均のゲイン</summary>
	private const float NEAR_AVG_GAIN = 1.0f + 4e-2f;
	/// <summary>近傍平均の減衰時間</summary>
	private const float NEAR_AVG_ATT_TIME = 1e-3f;

	/// <summary>ゲイン自動調整 減少時間[秒]</summary>
	public float AutoGainDecTime { get; set; } = 3.0f;
	/// <summary>ゲイン自動調整 増加時間[秒]</summary>
	public float AutoGainIncTime { get; set; } = 1e-2f;
	/// <summary>ゲイン自動調整 最小値</summary>
	public float AutoGainMin { get; set; } = (float)Math.Pow(10, -24 / 20.0);
	/// <summary>瞬間最大値</summary>
	public float Max { get; private set; }
	/// <summary>平滑化最大値</summary>
	public float SmoothedMax { get; private set; }
	/// <summary>波形合成用データ</summary>
	public readonly Bank[] Banks = new Bank[BankCount];
	/// <summary>表示用データ</summary>
	public readonly float[] DisplayData = new float[BankCount * 3];

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
	/// <param name="pInput">入力バッファ(2ch×サンプル数)</param>
	/// <param name="sampleCount">入力バッファのサンプル数</param>
	public unsafe void Update(IntPtr pInput, int sampleCount) {
		Calc((float*)pInput, sampleCount);
		ExtractPeak(sampleCount);
		UpdateAutoGain(sampleCount);
	}

	/// <summary>
	/// スペクトルを計算
	/// </summary>
	/// <param name="pInput">入力バッファ(2ch×サンプル数)</param>
	/// <param name="sampleCount">入力バッファのサンプル数</param>
	protected unsafe abstract void Calc(float* pInput, int sampleCount);

	private void ExtractPeak(int sampleCount) {
		var avgTimeDump = 1.0f - MathF.Exp(-sampleCount / (NEAR_AVG_ATT_TIME * SampleRate));
		var ixLastD = -1;
		var ixLastL = -1;
		var ixLastR = -1;
		var maxAmpD = 0.0f;
		var maxAmpL = 0.0f;
		var maxAmpR = 0.0f;
		for (int ixB = 0; ixB < BankCount; ixB++) {
			// 近傍平均(avg)
			float avgL, avgR;
			{
				// 周波数方向近傍平均(freqAvg)
				var freqAvgL = 0.0f;
				var freqAvgR = 0.0f;
				var totalGain = 0.0f;
				var k = ixB / (NEAR_AVG_RADIUS_TRANS_WIDTH * OCT_DIV) - NEAR_AVG_RADIUS_TRANS_START;
				k = Math.Clamp(k, 0.0f, 1.0f);
				var windowWidth = (int)((1.0f - k) * NEAR_AVG_RADIUS_MAX + k * NEAR_AVG_RADIUS_MIN);
				var windowStart = Math.Max(ixB - windowWidth, 0);
				var windowEnd = Math.Min(ixB + windowWidth, BankCount - 1);
				for (int ixW = windowStart; ixW <= windowEnd; ixW++) {
					var x1 = (ixB - ixW) * (MathF.E / OCT_DIV);
					var x2 = x1 * x1;
					var x4 = x2 * x2;
					var den = x4 + 6.0f * x2 + 12.0f;
					var notch = 12.0f * x2 / den;
					var gain = 1.0f + NEAR_AVG_DIST_GAIN * notch;
					ref var bw = ref Banks[ixW];
					freqAvgL += gain * bw.PowerL;
					freqAvgR += gain * bw.PowerR;
					totalGain += gain;
				}
				totalGain = NEAR_AVG_GAIN / totalGain;
				freqAvgL *= totalGain;
				freqAvgR *= totalGain;

				// 時間方向平均(timeAvg)
				ref var timeAvg = ref TimeAvg[ixB];
				var diffL = freqAvgL - timeAvg.L;
				var diffR = freqAvgR - timeAvg.R;
				diffL *= timeAvg.L < freqAvgL ? 1.0f : avgTimeDump;
				diffR *= timeAvg.R < freqAvgR ? 1.0f : avgTimeDump;
				timeAvg.L += diffL;
				timeAvg.R += diffR;
				avgL = timeAvg.L;
				avgR = timeAvg.R;
			}

			// 中心バンク
			ref var center = ref Banks[ixB];
			var powerL = center.PowerL;
			var powerR = center.PowerR;
			center.PeakL = 0.0f;
			center.PeakR = 0.0f;

			// リニア化
			var ampL = MathF.Sqrt(2.0f*powerL);
			var ampR = MathF.Sqrt(2.0f*powerR);
			var ampD = MathF.Max(ampL, ampR);
			var avgD = MathF.Sqrt(2.0f*MathF.Max(avgL, avgR));

			// 表示用曲線/閾値
			DisplayData[ixB] = ampD;
			DisplayData[ixB + BankCount] = avgD;
			DisplayData[ixB + 2*BankCount] = 0.0f;

			// 表示用ピーク
			if (ampD >= avgD) {
				if (ampD >= maxAmpD) {
					maxAmpD = ampD;
					ixLastD = ixB;
				}
			} else {
				if (ixLastD >= 0) {
					DisplayData[ixLastD + 2*BankCount] = maxAmpD;
					maxAmpD = 0.0f;
					ixLastD = -1;
				}
			}

			// 波形合成用ピーク(左)
			if (powerL >= avgL) {
				if (ampL >= maxAmpL) {
					maxAmpL = ampL;
					ixLastL = ixB;
				}
			} else {
				if (ixLastL >= 0) {
					ref var bank = ref Banks[ixLastL];
					bank.PeakL = maxAmpL;
					maxAmpL = 0.0f;
					ixLastL = -1;
				}
			}

			// 波形合成用ピーク(右)
			if (powerR >= avgR) {
				if (ampR >= maxAmpR) {
					maxAmpR = ampR;
					ixLastR = ixB;
				}
			} else {
				if (ixLastR >= 0) {
					ref var bank = ref Banks[ixLastR];
					bank.PeakR = maxAmpR;
					maxAmpR = 0.0f;
					ixLastR = -1;
				}
			}
		}
		if (ixLastD >= 0) {
			DisplayData[ixLastD + 2*BankCount] = maxAmpD;
		}
		if (ixLastL >= 0) {
			ref var bank = ref Banks[ixLastL];
			bank.PeakL = maxAmpL;
		}
		if (ixLastR >= 0) {
			ref var bank = ref Banks[ixLastR];
			bank.PeakR = maxAmpR;
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
		var delta = (float)sampleCount / SampleRate;
		delta = 1.0f - (float)Math.Exp(-delta / tau);
		smoothedMax += diff * delta;
		if (smoothedMax < AutoGainMin) {
			smoothedMax = AutoGainMin;
		}
		SmoothedMax = smoothedMax;
	}
}
