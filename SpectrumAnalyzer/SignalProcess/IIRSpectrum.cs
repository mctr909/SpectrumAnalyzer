using System;
using System.Runtime.InteropServices;

namespace SignalProcess {
	public class IIRSpectrum : BaseSpectrum {
		/// <summary>バンド幅の上限[半音]</summary>
		public double BandWidthMax { get; set; } = 7.0;
		/// <summary>バンド幅の下限[半音]</summary>
		public double BandWidthFloor { get; set; } = 0.25;
		/// <summary>バンド幅の変化の急峻さ[オクターブ]</summary>
		public double BandWidthTransition { get; set; } = 6.8;

		[StructLayout(LayoutKind.Sequential)]
		private struct BPF {
			public float La1;
			public float La2;
			public float Lb1;
			public float Lb2;

			public float Ra1;
			public float Ra2;
			public float Rb1;
			public float Rb2;

			public float Ka1;
			public float Ka2;
			public float Kb0;
			public float EmaDelta;
		}
		private readonly BPF[] Bpf;

		public IIRSpectrum(int sampleRate) : base(sampleRate) {
			Bpf = new BPF[BankCount];
			var bandWidthTransitionScale = 2.0 / (BandWidthTransition * OCT_DIV);
			var bandWidthScale = BandWidthMax - BandWidthFloor;
			for (int ix = 0; ix < BankCount; ++ix) {
				/* バンクによってバンド幅を変える */
				var bw = bandWidthTransitionScale * ix;
				bw = bandWidthScale * Math.Exp(-bw * bw);
				var bandWidth = (BandWidthFloor + bw) / 12.0;
				/* 中心周波数 */
				var f0 = MinFreq * Math.Pow(2.0, (double)ix / OCT_DIV);
				/* 正規化周波数 */
				var fn = f0 / sampleRate;
				/* バイクアッドフィルタ(BPF)の係数を設定 */
				var omega = 2.0 * Math.PI * fn;
				var c = Math.Cos(omega);
				var s = Math.Sin(omega);
				var x = Math.Log(2.0) / 2.0 * bandWidth * omega / s;
				var alpha = s * Math.Sinh(x);
				var a0 = 1.0 + alpha;
				ref var bpf = ref Bpf[ix];
				bpf.Ka1 = (float)(2.0 * c / a0);
				bpf.Ka2 = (float)(-(1.0 - alpha) / a0);
				bpf.Kb0 = (float)(alpha / a0);
				/* 指数移動平均の応答速度を設定 */
				bpf.EmaDelta = (float)(1.0 - Math.Exp(-fn));
			}
		}

		/// <inheritdoc/>
		protected override unsafe void Calc(IntPtr pInput, int sampleCount) {
			var pWave = (float*)pInput;
			var pWaveStart = pWave;
			var pWaveTerm = pWave + sampleCount * 2;
			var banks = Banks;
			/* デノーマル対策 */
			const float AntiDenormal = 1e-9f;
			/* フィルタバンクループ */
			for (int i = 0; i < BankCount; i++) {
				ref var bpf = ref Bpf[i];
				float la1 = bpf.La1 + AntiDenormal;
				float la2 = bpf.La2 - AntiDenormal;
				float lb1 = bpf.Lb1 + AntiDenormal;
				float lb2 = bpf.Lb2 - AntiDenormal;
				float ra1 = bpf.Ra1 + AntiDenormal;
				float ra2 = bpf.Ra2 - AntiDenormal;
				float rb1 = bpf.Rb1 + AntiDenormal;
				float rb2 = bpf.Rb2 - AntiDenormal;
				float ka1 = bpf.Ka1;
				float ka2 = bpf.Ka2;
				float kb0 = bpf.Kb0;
				float emaDelta = bpf.EmaDelta;
				ref var bank = ref banks[i];
				float powerL = bank.PowerL;
				float powerR = bank.PowerR;
				float a0, b0;
				for (pWave = pWaveStart; pWave < pWaveTerm; pWave += 2) {
					/* IIR BPF(左) */
					b0 = *pWave;
					a0 = b0 * kb0;
					a0 -= lb2 * kb0;
					a0 += la1 * ka1;
					a0 += la2 * ka2;
					la2 = la1; la1 = a0;
					lb2 = lb1; lb1 = b0;
					/* 2乗振幅の指数移動平均(左) */
					a0 *= a0;
					a0 -= powerL;
					powerL += a0 * emaDelta;
					/* IIR BPF(右) */
					b0 = *(pWave + 1);
					a0 = b0 * kb0;
					a0 -= rb2 * kb0;
					a0 += ra1 * ka1;
					a0 += ra2 * ka2;
					ra2 = ra1; ra1 = a0;
					rb2 = rb1; rb1 = b0;
					/* 2乗振幅の指数移動平均(右) */
					a0 *= a0;
					a0 -= powerR;
					powerR += a0 * emaDelta;
				}
				/* 状態を更新 */
				bpf.La1 = la1; bpf.La2 = la2;
				bpf.Lb1 = lb1; bpf.Lb2 = lb2;
				bpf.Ra1 = ra1; bpf.Ra2 = ra2;
				bpf.Rb1 = rb1; bpf.Rb2 = rb2;
				bank.PowerL = powerL;
				bank.PowerR = powerR;
			}
		}
	}
}
