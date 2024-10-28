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
			BaseFreq = 442 * Math.Pow(2, HALFTONE_CENTER / OCT_DIV + 3/12.0 - 5);
			for (int b = 0; b < BANK_COUNT; ++b) {
				var frequency = BaseFreq * Math.Pow(2.0, (b - 0.5 * HALFTONE_DIV) / OCT_DIV);
				PeakBanks[b] = new PeakBank() {
					DELTA = frequency / SampleRate
				};
				mpFilterBanks[b] = Marshal.AllocHGlobal(Marshal.SizeOf<FilterBank>());
				SetBPF(b, frequency);
			}
			SetResponceSpeed(DISP_SPEED);
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

		/// <summary>
		/// 表示応答速度を設定
		/// </summary>
		/// <param name="responceSpeed">応答速度[Hz]</param>
		public unsafe void SetResponceSpeed(double responceSpeed) {
			var sampleOmega = SampleRate / (2 * Math.PI);
			for (int b = 0; b < BANK_COUNT; ++b) {
				var pBank = (FilterBank*)mpFilterBanks[b];
				var bankFreq = PeakBanks[b].DELTA * SampleRate;
				pBank->SIGMA_DISP = GetAlpha(sampleOmega, (responceSpeed > bankFreq) ? bankFreq : responceSpeed);
			}
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
			if (a > 1) {
				return 1;
			}
			return a;
		}

		unsafe void SetBPF(int index, double frequency) {
			var sampleOmega = SampleRate / (2 * Math.PI);
			var omega = 2 * Math.PI * frequency / SampleRate;
			var alpha = GetAlpha(SampleRate, frequency);
			var a0 = 1.0 + alpha;
			var pBank = (FilterBank*)mpFilterBanks[index];
			Marshal.StructureToPtr(new FilterBank(), (IntPtr)pBank, true);
			pBank->KB0 = alpha / a0;
			pBank->KA1 = -2.0 * Math.Cos(omega) / a0;
			pBank->KA2 = (1.0 - alpha) / a0;
			pBank->SIGMA = GetAlpha(sampleOmega, frequency);
		}

		unsafe void CalcPower(float *pInput, int sampleCount) {
			Parallel.ForEach(mpFilterBanks, ptr => {
				var pBank = (FilterBank*)ptr;
				var KB0 = pBank->KB0;
				var KA2 = pBank->KA2;
				var KA1 = pBank->KA1;
				var SIGMA = pBank->SIGMA;
				var SIGMA_DISP = pBank->SIGMA_DISP;
				var pWave = pInput;
				for (int s = sampleCount; s != 0; --s) {
					/*** 左チャンネル ***/
					{
						/* 帯域通過フィルタに通す */
						var b0 = *pWave++;
						var a0 = KB0;
						a0 *= b0 - pBank->Lb2;
						pBank->Lb2 = pBank->Lb1;
						pBank->Lb1 = b0;
						a0 -= KA2 * pBank->La2;
						pBank->La2 = pBank->La1;
						a0 -= KA1 * pBank->La1;
						pBank->La1 = a0;
						/* パワースペクトルを得る */
						a0 *= a0;
						pBank->LPower += (a0 - pBank->LPower) * SIGMA;
						pBank->LPowerDisp += (a0 - pBank->LPowerDisp) * SIGMA_DISP;
					}
					/*** 右チャンネル ***/
					{
						/* 帯域通過フィルタに通す */
						var b0 = *pWave++;
						var a0 = KB0;
						a0 *= b0 - pBank->Rb2;
						pBank->Rb2 = pBank->Rb1;
						pBank->Rb1 = b0;
						a0 -= KA2 * pBank->Ra2;
						pBank->Ra2 = pBank->Ra1;
						a0 -= KA1 * pBank->Ra1;
						pBank->Ra1 = a0;
						/* パワースペクトルを得る */
						a0 *= a0;
						pBank->RPower += (a0 - pBank->RPower) * SIGMA;
						pBank->RPowerDisp += (a0 - pBank->RPowerDisp) * SIGMA_DISP;
					}
				}
			});
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
				var thresholdLDisp = 0.0;
				var thresholdRDisp = 0.0;
				{
					/* 音域によって閾値幅と閾値ゲインを選択 */
					int width;
					double gain;
					var transposedIdxB = idxB + Transpose * HALFTONE_DIV;
					if (transposedIdxB < END_LOW_BANK) {
						width = THRESHOLD_WIDTH_LOW;
						gain = THRESHOLD_GAIN_LOW;
					}
					else if (transposedIdxB < BEGIN_MID_BANK) {
						var a2b = (double)(transposedIdxB - END_LOW_BANK) / (BEGIN_MID_BANK - END_LOW_BANK);
						width = (int)(THRESHOLD_WIDTH_MID * a2b + THRESHOLD_WIDTH_LOW * (1 - a2b));
						gain = THRESHOLD_GAIN_MID * a2b + THRESHOLD_GAIN_LOW * (1 - a2b);
					}
					else {
						width = THRESHOLD_WIDTH_MID;
						gain = THRESHOLD_GAIN_MID;
					}
					/* 閾値幅で指定される範囲の平均値を閾値にする */
					for (int w = -width; w <= width; ++w) {
						var bw = Math.Min(BANK_COUNT - 1, Math.Max(0, idxB + w));
						var b = *(FilterBank*)mpFilterBanks[bw];
						thresholdL += b.LPower;
						thresholdR += b.RPower;
						thresholdLDisp += b.LPowerDisp;
						thresholdRDisp += b.RPowerDisp;
					}
					width = width * 2 + 1;
					/* パワー⇒リニア変換した値に閾値ゲインを掛ける */
					var scale = 2.0 / width;
					thresholdL = Math.Sqrt(thresholdL * scale) * gain;
					thresholdR = Math.Sqrt(thresholdR * scale) * gain;
					thresholdLDisp = Math.Sqrt(thresholdLDisp * scale) * gain;
					thresholdRDisp = Math.Sqrt(thresholdRDisp * scale) * gain;
				}
				var bank = *(FilterBank*)mpFilterBanks[idxB];
				/*** 波形合成用のピークを抽出 ***/
				{
					var peak = PeakBanks[idxB];
					peak.L = 0.0;
					peak.R = 0.0;
					var linearL = Math.Sqrt(bank.LPower * 2);
					var linearR = Math.Sqrt(bank.RPower * 2);
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
					var linear = Math.Sqrt(Math.Max(bank.LPowerDisp, bank.RPowerDisp) * 2);
					var threshold = Math.Max(thresholdLDisp, thresholdRDisp);
					Peak[idxB] = 0.0;
					Curve[idxB] = linear;
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
