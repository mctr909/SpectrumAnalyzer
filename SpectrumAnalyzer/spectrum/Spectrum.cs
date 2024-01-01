using System;
using System.Runtime.InteropServices;

namespace Spectrum {
	public partial class Spectrum : IDisposable {
		/// <summary>トランスポーズ[半音]</summary>
		public double Transpose { get; set; } = 0.0;

		/// <summary>変更ピッチ</summary>
		public double Pitch { get; set; } = 1.0;

		/// <summary> 最大値 </summary>
		public double Max { get; private set; } = AUTOGAIN_MIN;

		/// <summary> 自動ゲイン </summary>
		public double AutoGain { get; private set; } = AUTOGAIN_MIN;

		/// <summary>表示用データ</summary>
		public double[] DisplayData { get; private set; } = new double[BANK_COUNT * 3];

		/// <summary>波形合成用ピーク</summary>
		internal PeakBank[] PeakBanks { get; private set; } = new PeakBank[BANK_COUNT];

		private readonly int mSampleRate;
		private unsafe BpfBank* mpBpfBanks = null;
		private readonly double[] mPeak = new double[BANK_COUNT];

		private struct BpfBank {
			public float Kb0;
			public float Ka2;
			public float Ka1;
			public float Delta;
			public float Lb2;
			public float Lb1;
			public float La2;
			public float La1;
			public float Lms;
			public float Rb2;
			public float Rb1;
			public float Ra2;
			public float Ra1;
			public float Rms;
		}

		public unsafe Spectrum(int sample_rate) {
			mSampleRate = sample_rate;
			mpBpfBanks = (BpfBank*)Marshal.AllocHGlobal(BANK_COUNT * sizeof(BpfBank));
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var frequency = BASE_FREQ * Math.Pow(2.0, (double)ixB / OCT_DIV);
				SetBPFCoef(mpBpfBanks + ixB, sample_rate, frequency);
				PeakBanks[ixB] = new PeakBank {
					DELTA = frequency / sample_rate
				};
			}
		}

		public unsafe void Dispose() {
			if (mpBpfBanks != null) {
				Marshal.FreeHGlobal((IntPtr)mpBpfBanks);
			}
		}

		/// <summary>
		/// スペクトルを更新
		/// </summary>
		/// <param name="pInput">入力バッファ(float型ポインタ 2ch×サンプル数)</param>
		/// <param name="sampleCount">入力バッファのサンプル数</param>
		public void Update(IntPtr pInput, int sampleCount) {
			CalcMeanSquare(pInput, sampleCount);
			UpdateAutoGain(sampleCount);
			ExtractPeak();
		}

		private unsafe static void SetBPFCoef(BpfBank* pBank, int sampleRate, double frequency) {
			var bandWidth = 0.5 + Math.Log(HALFTONE_AT_FREQ / frequency, 2.0);
			if (bandWidth < 0.5) {
				bandWidth = 0.5;
			}
			var omega = 2 * Math.PI * frequency / sampleRate;
			var c = Math.Cos(omega);
			var s = Math.Sin(omega);
			var x = Math.Log(2) / 2 * bandWidth / 12.0 * omega / s;
			var sh = s * Math.Sinh(x);
			var a0 = 1 + sh;
			Marshal.StructureToPtr(new BpfBank(), (IntPtr)pBank, true);
			pBank->Kb0 = (float)(sh / a0);
			pBank->Ka1 = (float)(-2 * c / a0);
			pBank->Ka2 = (float)((1 - sh) / a0);
			pBank->Delta = (float)(2.0 * frequency / sampleRate);
		}

		private unsafe void CalcMeanSquare(IntPtr pInput, int sampleCount) {
			float lb2, lb1, la2, la1, lms;
			float rb2, rb1, ra2, ra1, rms;
			float kb0, ka2, ka1, delta;
			float b0, a0;
			int ixB, ixS;
			float *pWave;
			var pBanks = mpBpfBanks;
			for (ixB = BANK_COUNT; ixB != 0; --ixB) {
				kb0 = pBanks->Kb0;
				ka2 = pBanks->Ka2;
				ka1 = pBanks->Ka1;
				delta = pBanks->Delta;
				lb2 = pBanks->Lb2;
				lb1 = pBanks->Lb1;
				la2 = pBanks->La2;
				la1 = pBanks->La1;
				lms = pBanks->Lms;
				rb2 = pBanks->Rb2;
				rb1 = pBanks->Rb1;
				ra2 = pBanks->Ra2;
				ra1 = pBanks->Ra1;
				rms = pBanks->Rms;
				pWave = (float*)pInput;
				for (ixS = sampleCount; ixS != 0; --ixS) {
					/*** [帯域通過フィルタ 左チャンネル] ***/
					b0 = *pWave++;
					a0 = b0 - lb2;
					a0 *= kb0;
					a0 -= ka2 * la2;
					a0 -= ka1 * la1;
					lb2 = lb1;
					lb1 = b0;
					la2 = la1;
					la1 = a0;
					// 振幅の二乗平均
					a0 *= a0;
					a0 -= lms;
					lms += a0 * delta;
					/*** [帯域通過フィルタ 右チャンネル] ***/
					b0 = *pWave++;
					a0 = b0 - rb2;
					a0 *= kb0;
					a0 -= ka2 * ra2;
					a0 -= ka1 * ra1;
					rb2 = rb1;
					rb1 = b0;
					ra2 = ra1;
					ra1 = a0;
					// 振幅の二乗平均
					a0 *= a0;
					a0 -= rms;
					rms += a0 * delta;
				}
				pBanks->Rms = rms;
				pBanks->Ra1 = ra1;
				pBanks->Ra2 = ra2;
				pBanks->Rb1 = rb1;
				pBanks->Rb2 = rb2;
				pBanks->Lms = lms;
				pBanks->La1 = la1;
				pBanks->La2 = la2;
				pBanks->Lb1 = lb1;
				pBanks->Lb2 = lb2;
				pBanks++;
			}
		}

		private unsafe void UpdateAutoGain(int sampleCount) {
			/* 最大値を更新 */
			Max = AUTOGAIN_MIN;
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				var b = mpBpfBanks[ixB];
				var amp = Math.Sqrt(Math.Max(b.Lms, b.Rms) * 2);
				Max = Math.Max(Max, amp);
			}

			/* 最大値に追随して自動ゲインを更新 */
			var diff = Max - AutoGain;
			var delta = (double)sampleCount / mSampleRate;
			delta /= diff < 0 ? AUTOGAIN_TIME_DOWN : AUTOGAIN_TIME_UP;
			AutoGain += diff * delta;
			if (AutoGain < AUTOGAIN_MIN) {
				AutoGain = AUTOGAIN_MIN;
			}
		}

		private unsafe void ExtractPeak() {
			for (int ixB = 0; ixB < BANK_COUNT; ++ixB) {
				/*** ピーク抽出用の閾値を算出 ***/
				var threshold_l = 0.0;
				var threshold_r = 0.0;
				{
					/* 音域によって閾値幅を選択 */
					int width;
					var transposed = ixB + Transpose * HALFTONE_DIV;
					if (transposed < BEGIN_MID) {
						width = THRESHOLD_WIDTH_LOW;
					} else if (transposed < BEGIN_HIGH) {
						var a2b = (double)(transposed - BEGIN_MID) / (BEGIN_HIGH - BEGIN_MID);
						width = (int)(THRESHOLD_WIDTH_HIGH * a2b + THRESHOLD_WIDTH_LOW * (1 - a2b));
					} else {
						width = THRESHOLD_WIDTH_HIGH;
					}
					/* 閾値幅で指定される範囲の最大値を閾値とする */
					for (int ixW = -width; ixW <= width; ++ixW) {
						var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, ixB + ixW));
						var b = mpBpfBanks[bw];
						threshold_l = Math.Max(threshold_l, b.Lms);
						threshold_r = Math.Max(threshold_r, b.Rms);
					}
					/* 平均値を閾値の下限とする */
					width = HALFTONE_DIV;
					var avg_l = 0.0;
					var avg_r = 0.0;
					for (int ixW = -width; ixW <= width; ++ixW) {
						var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, ixB + ixW));
						var b = mpBpfBanks[bw];
						avg_l += b.Lms;
						avg_r += b.Rms;
					}
					width *= 2;
					width++;
					avg_l *= AVG_GAIN / width;
					avg_r *= AVG_GAIN / width;
					threshold_l = Math.Max(threshold_l, avg_l);
					threshold_r = Math.Max(threshold_r, avg_r);
					/* 2乗平均を振幅に変換 */
					threshold_l = Math.Sqrt(threshold_l * 2);
					threshold_r = Math.Sqrt(threshold_r * 2);
				}
				/*** 波形合成用のピークを抽出 ***/
				var bank = mpBpfBanks[ixB];
				var amp_l = Math.Sqrt(bank.Lms * 2);
				var amp_r = Math.Sqrt(bank.Rms * 2);
				var p_peak = PeakBanks[ixB];
				p_peak.L = amp_l < threshold_l ? 0.0 : amp_l;
				p_peak.R = amp_r < threshold_r ? 0.0 : amp_r;
				/*** 表示用の曲線と閾値を設定 ***/
				var amp = Math.Max(amp_l, amp_r);
				var threshold = Math.Max(threshold_l, threshold_r);
				if (EnableNormalize) {
					amp /= Max;
					threshold /= Max;
				}
				if (EnableAutoGain) {
					amp /= AutoGain;
					threshold /= AutoGain;
				}
				DisplayData[ixB] = amp;
				DisplayData[ixB + BANK_COUNT] = threshold;
				mPeak[ixB] = amp < threshold ? 0.0 : amp;
			}
			for (int ixB = 0, ixP = BANK_COUNT * 2; ixB < BANK_COUNT; ++ixB, ++ixP) {
				var max = 0.0;
				for (int ixW = -1; ixW <= 1; ++ixW) {
					var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, ixB + ixW));
					var val = mPeak[bw];
					if (ixW != 0) {
						val *= 0.5;
					}
					max = Math.Max(val, max);
				}
				DisplayData[ixP] = max;
			}
		}
	}
}