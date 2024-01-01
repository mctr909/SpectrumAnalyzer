using System;
using System.Runtime.InteropServices;

namespace SignalProcess {
	public class IQSpectrum : BaseSpectrum {
		public const double Sigma = 0.66;
		public const double SigmaTransitionDelta = 1.0 / (5.0 * OCT_DIV);

		[StructLayout(LayoutKind.Sequential)]
		private struct IQ {
			public float li, lq;
			public float ri, rq;
			public float oi, oq;
			public float di, dq;
			public float dump;
		}
		private readonly IQ[] IQs;

		public IQSpectrum(int sampleRate) : base(sampleRate) {
			IQs = new IQ[BankCount];
			for (int ixb = 0; ixb < BankCount; ixb++) {
				var t = ixb * SigmaTransitionDelta;
				var sigma = Sigma * Math.Exp(-t * t);
				var fc = MinFreq * Math.Pow(2.0, (double)ixb / OCT_DIV);
				var fn = fc / sampleRate;
				var nOmega = 2.0 * Math.PI * fn;
				ref var bank = ref IQs[ixb];
				bank.oi = 1.0f;
				bank.oq = 0.0f;
				bank.di = (float)Math.Cos(nOmega);
				bank.dq = (float)Math.Sin(nOmega);
				bank.dump = 1.0f - (float)Math.Exp(-sigma*fn);
			}
		}

		/// <inheritdoc/>
		protected override unsafe void Calc(IntPtr pInput, int sampleCount) {
			var pWave = (float*)pInput;
			var pWaveStart = pWave;
			var pWaveTerm = pWave + sampleCount * 2;
			var bankCount = Banks.Length;
			for (int ib = 0; ib < bankCount; ib++) {
				ref var b = ref IQs[ib];
				float li = b.li, lq = b.lq;
				float ri = b.ri, rq = b.rq;
				float oi = b.oi, oq = b.oq;
				float di = b.di, dq = b.dq;
				float dump = b.dump;
				float l, r, ti;
				for (pWave = pWaveStart; pWave < pWaveTerm; pWave += 2) {
					l = *pWave;
					r = *(pWave + 1);
					li += dump * (l * oi - li);
					lq += dump * (l * oq - lq);
					ri += dump * (r * oi - ri);
					rq += dump * (r * oq - rq);
					ti = oi * di - oq * dq;
					oq = oi * dq + oq * di;
					oi = ti;
				}
				b.li = li; b.lq = lq;
				b.ri = ri; b.rq = rq;
				b.oi = oi; b.oq = oq;
			}
			for (int ib = 0; ib < bankCount; ib++) {
				ref var b = ref IQs[ib];
				ref var p = ref Banks[ib];
				p.PowerL = b.li * b.li + b.lq * b.lq;
				p.PowerR = b.ri * b.ri + b.rq * b.rq;
				var or = (float)Math.Sqrt(b.oi * b.oi + b.oq * b.oq);
				b.oi /= or;
				b.oq /= or;
			}
		}
	}
}
