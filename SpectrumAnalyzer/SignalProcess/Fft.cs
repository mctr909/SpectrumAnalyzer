using System;

namespace SignalProcess {
	public class Fft {
		public struct Vec {
			public double x;
			public double y;
		}

		public static void Forward(Vec[] z) {
			var n = z.Length;
			for (uint i = 1, j = 0; i<n; i++) {
				uint m;
				for (m = (uint)n>>1; (j&m)!=0; j^=m, m>>=1);
				j^=m;
				if (i < j) {
					(z[j].x, z[i].x)=(z[i].x, z[j].x);
					(z[j].y, z[i].y)=(z[i].y, z[j].y);
				}
			}
			var th = -Math.PI;
			for (uint hn = 1, wn = 2; wn <= n; hn=wn, wn<<=1) {
				var wx = Math.Cos(th);
				var wy = Math.Sin(th);
				for (uint j = 0; j<n; j+=wn) {
					var rx = 1.0;
					var ry = 0.0;
					for (uint i = 0; i<hn; i++) {
						ref var a = ref z[i+j];
						ref var b = ref z[i+j+hn];
						var x = b.x*rx - b.y*ry;
						var y = b.x*ry + b.y*rx;
						b.x = a.x - x;
						b.y = a.y - y;
						a.x += x;
						a.y += y;
						var t = rx*wx - ry*wy;
						ry = rx*wy + ry*wx;
						rx = t;
					}
				}
				th *= 0.5;
			}
		}

		public static void Inverse(Vec[] z) {
			var n = z.Length;
			for (uint i = 1, j = 0; i<n; i++) {
				uint m;
				for (m = (uint)n>>1; (j&m)!=0; j^=m, m>>=1);
				j^=m;
				if (i < j) {
					(z[j].x, z[i].x)=(z[i].x, z[j].x);
					(z[j].y, z[i].y)=(z[i].y, z[j].y);
				}
			}
			var th = Math.PI;
			for (uint hn = 1, wn = 2; wn <= n; hn=wn, wn<<=1) {
				var wx = Math.Cos(th);
				var wy = Math.Sin(th);
				for (uint j = 0; j<n; j+=wn) {
					var rx = 1.0;
					var ry = 0.0;
					for (uint i = 0; i<hn; i++) {
						ref var a = ref z[i+j];
						ref var b = ref z[i+j+hn];
						var x = b.x*rx - b.y*ry;
						var y = b.x*ry + b.y*rx;
						b.x = a.x - x;
						b.y = a.y - y;
						a.x += x;
						a.y += y;
						var t = rx*wx - ry*wy;
						ry = rx*wy + ry*wx;
						rx = t;
					}
				}
				th *= 0.5;
			}
			for (uint i = 0; i<n; i++) {
				z[i].x /= n;
				z[i].y /= n;
			}
		}

		public static void Interp(Vec[] z, int dataLen) {
			const double EPS2 = 1e-5;
			double curV = 0;
			double deltaV;
			int slope = (int)(0.75 * dataLen);
			int prevI = 0;
			for (int i = 0; i < dataLen; i++) {
				var inV = z[i].x;
				if (inV * inV < EPS2) {
					continue;
				}
				if (prevI == 0) {
					prevI = Math.Max(0, i - slope);
				}
				var width = i + 1 - prevI;
				var weight = 0.5 - Math.Min(0.5, (double)width / slope);
				var halfWidth = width / 2;
				var centerI = prevI + halfWidth;
				var centerV = weight * (inV + curV);
				deltaV = (centerV - curV) / halfWidth;
				for (int j = prevI; j < centerI; j++) {
					z[j].x = curV;
					curV += deltaV;
				}
				deltaV = (inV - curV) / halfWidth;
				for (int j = centerI; j < i; j++) {
					z[j].x = curV;
					curV += deltaV;
				}
				prevI = i;
			}

			var fftLen = z.Length;
			deltaV = -curV / slope;
			for (int j = prevI; j < fftLen; j++) {
				z[j].x = curV;
				curV += deltaV;
			}

			for (int i = (fftLen >> 1) - 1, j = i + 1; i >= 0; i--, j++) {
				ref var a = ref z[i];
				ref var b = ref z[j];
				b.x = a.x;
				a.y = 0;
				b.y = 0;
			}
		}

		public static void Lpf(Vec[] z, double width = 0.5, double sharp = 16) {
			var k = 2 * Math.PI * sharp;
			var w = 1 - width / 2;
			var fftLen = z.Length;
			for (int i = 0; i < fftLen; i++) {
				var t = 2.0 * i / fftLen - 1;
				var a = k * (t + w);
				var b = k * (t - w);
				a = 1 / (1 + Math.Exp(a));
				b = 1 / (1 + Math.Exp(-b));
				var g = a + b;
				z[i].x *= g;
				z[i].y *= g;
			}
		}

		public static void Cepstrum(Vec[] spectrum, int dataLen, double minDb=-100, double maxDb=0) {
			var width = maxDb - minDb;
			var lowLimit = Math.Pow(10, minDb/20);
			for (int i = 0; i<dataLen; i++) {
				var x = spectrum[i].x;
				x = Math.Max(x, lowLimit);
				x = 20*Math.Log10(x);
				x = Math.Min(x, maxDb);
				x -= minDb;
				x /= width;
				spectrum[i].x = x;
				spectrum[i].y = 0;
			}
			Interp(spectrum, dataLen);
			Forward(spectrum);
			Lpf(spectrum);
			Inverse(spectrum);
			for (int i = 0; i<dataLen; i++) {
				var x = spectrum[i].x;
				x *= width;
				x += minDb;
				spectrum[i].x = Math.Pow(10, x/20);
				spectrum[i].y = 0;
			}
		}
	}
}
