using System;

namespace SignalProcess {
	public class Fft {
		public struct Complex {
			public double re;
			public double im;
		}

		public static void Forward(Complex[] z) => CoreTransform(z, false);

		public static void Inverse(Complex[] z) => CoreTransform(z, true);

		public static void Mirror(Complex[] z) {
			for (int r = (z.Length >> 1) - 1, f = r + 1; r >= 0; r--, f++) {
				ref var a = ref z[r];
				ref var b = ref z[f];
				b.re = a.re;
				b.im = -a.im;
			}
		}

		public static void Lpf(Complex[] z, double normFreq, double roundness = 0.25) {
			var fftLen = z.Length;
			var k = 2 * Math.PI / roundness;
			var u = 1 - normFreq;
			for (int i = 0; i < fftLen; i++) {
				var t = 2.0 * i / fftLen - 1;
				var ta = k * (t + u);
				var tb = k * (t - u);
				var ga = 1 / (1 + Math.Exp(ta));
				var gb = 1 / (1 + Math.Exp(-tb));
				var g = ga + gb;
				z[i].re *= g;
				z[i].im *= g;
			}
		}

		private static void CoreTransform(Complex[] z, bool inverse) {
			uint i, j, m;
			uint hn, wn;
			var n = (uint)z.Length;
			for (i = 1, j = 0; i<n; i++) {
				for (m = n>>1; (j&m)!=0; j^=m, m>>=1);
				j^=m;
				if (i < j) {
					(z[j], z[i]) = (z[i], z[j]);
				}
			}
			var th = inverse ? Math.PI : -Math.PI;
			for (hn = 1, wn = 2; wn <= n; hn=wn, wn<<=1) {
				var w_re = Math.Cos(th);
				var w_im = Math.Sin(th);
				for (j = 0; j<n; j+=wn) {
					var t_re = 1.0;
					var t_im = 0.0;
					for (i = 0; i<hn; i++) {
						ref var a = ref z[i+j];
						ref var b = ref z[i+j+hn];
						var re = b.re*t_re - b.im*t_im;
						var im = b.re*t_im + b.im*t_re;
						b.re = a.re - re;
						b.im = a.im - im;
						a.re += re;
						a.im += im;
						var t = t_re*w_re - t_im*w_im;
						t_im = t_re*w_im + t_im*w_re;
						t_re = t;
					}
				}
				th *= 0.5;
			}
			if (inverse) {
				for (i = 0; i<n; i++) {
					z[i].re /= n;
					z[i].im /= n;
				}
			}
		}
	}
}
