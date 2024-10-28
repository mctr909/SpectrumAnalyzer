using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Spectrum {
	public partial class Spectrum : IDisposable {
		/// <summary>サンプリング周波数[Hz]</summary>
		readonly int SampleRate;

		/// <summary>基本周波数(C0)[Hz]</summary>
		public static double BaseFreq;

		/// <summary>トランスポーズ[半音]</summary>
		public double Transpose { get; set; } = 0.0;

		/// <summary> 最大値 </summary>
		public double Max { get; private set; } = 1.0;

		/// <summary> 自動ゲイン </summary>
		public double AutoGain { get; private set; } = 1.0;

		/// <summary>表示用ピーク</summary>
		public double[] Peak { get; private set; }

		/// <summary>表示用曲線</summary>
		public double[] Curve { get; private set; }

		/// <summary>波形合成用ピーク</summary>
		internal PeakBank[] PeakBanks { get; private set; }

		/// <summary>帯域通過フィルタバンク</summary>
		IntPtr[] mpFilterBanks;

		public Spectrum(int sampleRate) {
			SampleRate = sampleRate;
			Peak = new double[BANK_COUNT];
			Curve = new double[BANK_COUNT];
			PeakBanks = new PeakBank[BANK_COUNT];
			mpFilterBanks = new IntPtr[BANK_COUNT];
			BaseFreq = 442 * Math.Pow(2, HALFTONE_CENTER / OCT_DIV + 3 / 12.0 - 5);
			for (int b = 0; b < BANK_COUNT; ++b) {
				var frequency = BaseFreq * Math.Pow(2.0, (b - 0.5 * HALFTONE_DIV) / OCT_DIV);
				PeakBanks[b] = new PeakBank() {
					DELTA = frequency / SampleRate
				};
				mpFilterBanks[b] = Marshal.AllocHGlobal(Marshal.SizeOf<FilterBank>());
				SetBPF(b, frequency);
			}
		}

		public void Dispose() {
			foreach (var pFilterBank in mpFilterBanks) {
				Marshal.FreeHGlobal(pFilterBank);
			}
		}

		/// <summary>
		/// スペクトルを更新
		/// </summary>
		/// <param name="pInput">入力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sampleCount">入力バッファのサンプル数</param>
		public unsafe void Update(IntPtr pInput, int sampleCount) {
			/* パワースペクトルを算出 */
			CalcPower((float*)pInput, sampleCount);
			/* ピークを抽出 */
			ExtractPeak();
		}

		static double GetAlpha(double sampleRate, double frequency) {
			var halfToneWidth = 1.0 + Math.Log(FREQ_AT_HALFTONE_WIDTH / frequency, 2.0);
			if (halfToneWidth < 1.0) {
				halfToneWidth = 1.0;
			}
			var omega = 2 * Math.PI * frequency / sampleRate;
			var s = Math.Sin(omega);
			var x = Math.Log(2) / 4 * halfToneWidth / 12.0 * omega / s;
			var a = s * Math.Sinh(x);
			return a;
		}

		unsafe void SetBPF(int index, double frequency) {
			var omega = 2 * Math.PI * frequency / SampleRate;
			var alpha = GetAlpha(SampleRate, frequency);
			var a0 = 1.0 + alpha;
			var pBank = (FilterBank*)mpFilterBanks[index];
			Marshal.StructureToPtr(new FilterBank(), (IntPtr)pBank, true);
			pBank->KB0 = alpha / a0;
			pBank->KA1 = -2.0 * Math.Cos(omega) / a0;
			pBank->KA2 = (1.0 - alpha) / a0;
			pBank->SIGMA = GetAlpha(SampleRate / Math.PI * 0.5, frequency);
		}

		unsafe void CalcPower(float* pInput, int sampleCount) {
			Parallel.ForEach(mpFilterBanks, ptr => {
				var pWave = pInput;
				var pBank = (FilterBank*)ptr;
				var KB0 = pBank->KB0;
				var KA2 = pBank->KA2;
				var KA1 = pBank->KA1;
				var SIGMA = pBank->SIGMA;
				for (int s = sampleCount; s != 0; --s) {
					/*** 左チャンネル ***/
					{
						/* 帯域通過フィルタに通す */
						var b = *pWave++;
						var a = b - pBank->lb2;
						a *= KB0;
						pBank->lb2 = pBank->lb1;
						pBank->lb1 = b;
						a -= KA2 * pBank->la2;
						a -= KA1 * pBank->la1;
						pBank->la2 = pBank->la1;
						pBank->la1 = a;
						/* パワースペクトルを得る */
						a *= a;
						pBank->l += (a - pBank->l) * SIGMA;
					}
					/*** 右チャンネル ***/
					{
						/* 帯域通過フィルタに通す */
						var b = *pWave++;
						var a = b - pBank->rb2;
						a *= KB0;
						pBank->rb2 = pBank->rb1;
						pBank->rb1 = b;
						a -= KA2 * pBank->ra2;
						a -= KA1 * pBank->ra1;
						pBank->ra2 = pBank->ra1;
						pBank->ra1 = a;
						/* パワースペクトルを得る */
						a *= a;
						pBank->r += (a - pBank->r) * SIGMA;
					}
				}
			});
			/* 最大値を更新 */
			Max = AUTOGAIN_MIN;
			foreach (FilterBank* pBank in mpFilterBanks) {
				var linear = Math.Sqrt(Math.Max(pBank->l, pBank->r) * 2);
				Max = Math.Max(Max, linear);
			}
			/* 最大値に追随して自動ゲインを更新 */
			var autoGainDelta = (double)sampleCount / SampleRate;
			var diff = Max - AutoGain;
			if (diff < 0) {
				autoGainDelta /= AUTOGAIN_DOWN_SPEED;
			} else {
				autoGainDelta /= AUTOGAIN_UP_SPEED;
			}
			AutoGain += diff * autoGainDelta;
		}

		unsafe void ExtractPeak() {
			var lastL = 0.0;
			var lastR = 0.0;
			var lastLIndex = -1;
			var lastRIndex = -1;
			var lastDisp = 0.0;
			var lastDispIndex = -1;
			for (int idxB = 0; idxB < BANK_COUNT; ++idxB) {
				/*** ピーク抽出用の閾値を算出 ***/
				var thresholdL = 0.0;
				var thresholdR = 0.0;
				{
					/* 音域によって閾値幅と閾値ゲインを選択 */
					int width;
					double gain;
					var transposedIdxB = idxB + Transpose * HALFTONE_DIV;
					if (transposedIdxB < END_LOW_BANK) {
						width = THRESHOLD_WIDTH_LOW;
						gain = THRESHOLD_GAIN_LOW;
					} else if (transposedIdxB < BEGIN_MID_BANK) {
						var a2b = (double)(transposedIdxB - END_LOW_BANK) / (BEGIN_MID_BANK - END_LOW_BANK);
						width = (int)(THRESHOLD_WIDTH_MID * a2b + THRESHOLD_WIDTH_LOW * (1 - a2b));
						gain = THRESHOLD_GAIN_MID * a2b + THRESHOLD_GAIN_LOW * (1 - a2b);
					} else {
						width = THRESHOLD_WIDTH_MID;
						gain = THRESHOLD_GAIN_MID;
					}
					/* 閾値幅で指定される範囲の平均値を閾値にする */
					for (int w = -width; w <= width; ++w) {
						var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, idxB + w));
						var b = *(FilterBank*)mpFilterBanks[bw];
						thresholdL += b.l;
						thresholdR += b.r;
					}
					width = width * 2 + 1;
					/* パワー⇒リニア変換した値に閾値ゲインを掛ける */
					var scale = 2.0 / width;
					thresholdL = Math.Sqrt(thresholdL * scale) * gain;
					thresholdR = Math.Sqrt(thresholdR * scale) * gain;
				}
				var bank = *(FilterBank*)mpFilterBanks[idxB];
				/*** 波形合成用のピークを抽出 ***/
				{
					var peak = PeakBanks[idxB];
					peak.L = 0.0;
					peak.R = 0.0;
					var linearL = Math.Sqrt(bank.l * 2);
					var linearR = Math.Sqrt(bank.r * 2);
					if (linearL < thresholdL) {
						if (0 <= lastLIndex) {
							PeakBanks[lastLIndex].L = lastL;
						}
						linearL = 0.0;
						lastL = 0.0;
						lastLIndex = -1;
					}
					if (lastL < linearL) {
						lastL = linearL;
						lastLIndex = idxB;
					}
					if (linearR < thresholdR) {
						if (0 <= lastRIndex) {
							PeakBanks[lastRIndex].R = lastR;
						}
						linearR = 0.0;
						lastR = 0.0;
						lastRIndex = -1;
					}
					if (lastR < linearR) {
						lastR = linearR;
						lastRIndex = idxB;
					}
				}
				/*** 表示用のピークを抽出、曲線を設定 ***/
				{
					var linear = Math.Sqrt(Math.Max(bank.l, bank.r) * 2);
					var threshold = Math.Max(thresholdL, thresholdR);
					if (EnableNormalize) {
						linear /= Max;
						threshold /= Max;
					}
					if (EnableAutoGain) {
						linear /= AutoGain;
						threshold /= AutoGain;
					}
					Curve[idxB] = linear;
					Peak[idxB] = 0.0;
					if (linear < threshold) {
						if (0 <= lastDispIndex) {
							Peak[lastDispIndex] = lastDisp;
						}
						linear = 0.0;
						lastDisp = 0.0;
						lastDispIndex = -1;
					}
					if (lastDisp < linear) {
						lastDisp = linear;
						lastDispIndex = idxB;
					}
				}
			}
			if (0 <= lastLIndex) {
				PeakBanks[lastLIndex].L = lastL;
			}
			if (0 <= lastRIndex) {
				PeakBanks[lastRIndex].R = lastR;
			}
			if (0 <= lastDispIndex) {
				Peak[lastDispIndex] = lastDisp;
			}
		}
	}
}
