using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SpectrumAnalyzer {
	static class Drawer {
		const int SCROLL_SPEED = 2;

		const int ALPHA_MAX = 191;

		static readonly Font FONT = new Font("Meiryo UI", 8f);
		static readonly Pen OCT_BORDER = new Pen(Color.FromArgb(147, 147, 147), 1.0f);
		static readonly Pen KEY_BORDER = new Pen(Color.FromArgb(71, 71, 71), 1.0f);
		static readonly Pen WHITE_KEY = new Pen(Color.FromArgb(47, 47, 47), 1.0f);
		static readonly Pen BLACK_KEY = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
		static readonly Pen GRID_MAJOR = new Pen(Color.FromArgb(107, 107, 0), 1.0f);
		static readonly Pen GRID_MINOR1 = new Pen(Color.FromArgb(71, 71, 0), 1.0f);
		static readonly Pen GRID_MINOR2 = new Pen(Color.FromArgb(63, 63, 63), 1.0f);

		static readonly Brush PEAK = new Pen(Color.FromArgb(255, 0, 0)).Brush;
		static readonly Brush SURFACE = new Pen(Color.FromArgb(71, 191, 71)).Brush;

		public static readonly Pen SLOPE = new Pen(Color.FromArgb(0, 221, 0), 1.0f);
		public static readonly Pen THRESHOLD = Pens.Cyan;

		public const int KEYBOARD_HEIGHT = 20;
		public static byte[] ScrollCanvas;
		public static int MinLevel = -24;
		public static int KeyboardShift = 0;

		public static bool DisplayThreshold = false;
		public static bool DisplayScroll = true;
		public static bool DisplayPeak = false;
		public static bool DisplayCurve = true;

		static int DbToY(double db, int height) {
			if (db < MinLevel) {
				db = MinLevel;
			}
			return (int)(db * height / MinLevel);
		}

		static int AmpToY(double amp, int height) {
			if (amp < 1e-6) {
				amp = 1e-6;
			}
			var db = 20 * Math.Log10(amp) / MinLevel;
			if (db < 0) {
				db = 0;
			}
			if (db > 1) {
				db = 1;
			}
			return (int)(db * height);
		}

		static void SetHue(double amp, int offset, int width) {
			if (amp < 1e-6) {
				amp = 1e-6;
			}
			var db = 20 * Math.Log10(amp) / MinLevel;
			if (db < 0) {
				db = 0;
			}
			if (db > 1) {
				db = 1;
			}
			var v = (1.0 - db) * 1279;
			double a, r, g, b;
			if (v < 256) {
				b = 255;
				g = 0;
				r = 0;
				a = ALPHA_MAX * v / 255.0;
			}
			else if (v < 512) {
				b = 255;
				g = v - 256;
				r = 0;
				a = ALPHA_MAX;
			}
			else if (v < 768) {
				b = 255 - (v - 512);
				g = 255;
				r = 0;
				a = ALPHA_MAX;
			}
			else if (v < 1024) {
				b = 0;
				g = 255;
				r = v - 768;
				a = ALPHA_MAX;
			}
			else {
				b = 0;
				g = 255 - (v - 1024);
				r = 255;
				a = ALPHA_MAX;
			}
			for (int x = 0, p = offset; x < width; x++, p += 4) {
				ScrollCanvas[p + 0] = (byte)b;
				ScrollCanvas[p + 1] = (byte)g;
				ScrollCanvas[p + 2] = (byte)r;
				ScrollCanvas[p + 3] = (byte)a;
			}
		}

		public static void Keyboard(
			Graphics g,
			int width, int height, int gaugeHeight,
			int noteCount
		) {
			var barBottom = height - 1;
			for (int n = 0; n < noteCount; n++) {
				var note = n + KeyboardShift;
				var px = (n + 0.0f) * width / noteCount;
				var barWidth = (n + 1.0f) * width / noteCount - px + 1;
				switch ((note + 24) % 12) {
				case 0:
					g.FillRectangle(WHITE_KEY.Brush, px, 0, barWidth, height);
					g.DrawLine(OCT_BORDER, px, 0, px, barBottom);
					break;
				case 2:
				case 4:
				case 7:
				case 9:
				case 11:
					g.FillRectangle(WHITE_KEY.Brush, px, 0, barWidth, height);
					break;
				case 5:
					g.FillRectangle(WHITE_KEY.Brush, px, 0, barWidth, height);
					g.DrawLine(KEY_BORDER, px, 0, px, barBottom);
					break;
				default:
					g.FillRectangle(BLACK_KEY.Brush, px, 0, barWidth, height);
					break;
				}
			}
			var right = width - 1;
			var keyboardBottom = gaugeHeight + KEYBOARD_HEIGHT - 1;
			var textBottom = keyboardBottom + 1;
			var textHeight = g.MeasureString("9", FONT).Height;
			var textOfsX = textHeight * 0.5f;
			var textArea = new RectangleF(0f, 0f, KEYBOARD_HEIGHT, textHeight);
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Far,
				LineAlignment = StringAlignment.Center
			};
			for (int n = 0; n < noteCount + 12; n += 12) {
				var note = n - KeyboardShift;
				var px = 6 + width * note / noteCount - textOfsX;
				g.TranslateTransform(px, textBottom);
				g.RotateTransform(-90);
				g.DrawString("" + (n / 12), FONT, Brushes.Gray, textArea, stringFormat);
				g.RotateTransform(90);
				g.TranslateTransform(-px, -textBottom);
			}
			g.DrawLine(OCT_BORDER, 0, keyboardBottom, right, keyboardBottom);
		}

		public static void Gauge(Graphics g, int width, int height) {
			var right = width - 1;
			for (double db = 0; MinLevel <= db; db -= 1.0) {
				var py = DbToY(db, height);
				if (db % 12 == 0) {
					g.DrawLine(GRID_MAJOR, 0, py, right, py);
				}
				else if (height >= -MinLevel && db % 6 == 0) {
					g.DrawLine(GRID_MINOR1, 0, py, right, py);
				}
				else if (db % 3 == 0) {
					g.DrawLine(GRID_MINOR2, 0, py, right, py);
				}
			}
			var textSize = g.MeasureString("9", FONT);
			var textArea = new RectangleF(0f, 0f, 24, textSize.Height);
			var textBottom = height - textArea.Height + 4;
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Near
			};
			var dbOfs = Spectrum.AutoGain || Spectrum.NormGain ? 0 : -12;
			for (double db = 0; MinLevel < db; db -= 12.0) {
				var py = DbToY(db, height) - 2;
				if (py < textBottom) {
					g.TranslateTransform(0, py);
					g.DrawString(db + dbOfs + "", FONT, Brushes.Gray, textArea, stringFormat);
					g.TranslateTransform(0, -py);
				}
				else {
					g.TranslateTransform(0, textBottom);
					g.DrawString(db + dbOfs + "", FONT, Brushes.Gray, textArea, stringFormat);
					g.TranslateTransform(0, -textBottom);
				}
			}
		}

		public static void Surface(Graphics g, double[] arr, int count, int width, int height) {
			var scale = Spectrum.AutoGain || Spectrum.NormGain ? 1 : 4;
			var minValue = Math.Pow(10, MinLevel / 20.0);
			for (int x = 0, i = 0; x < count; x++, i++) {
				var val = arr[i] * scale;
				if (val > minValue) {
					var barX = (x - 0.5f) * width / count;
					var barWidth = (x + 0.5f) * width / count - barX + 1;
					var barY = AmpToY(val, height);
					var barHeight = height - barY;
					g.FillRectangle(SURFACE, barX, barY, barWidth, barHeight);
				}
			}
		}

		public static void Curve(Graphics g, double[] arr, int count, int width, int height, Pen color) {
			var scale = Spectrum.AutoGain || Spectrum.NormGain ? 1 : 4;
			var idxA = 0;
			var preX = 0;
			var preY = AmpToY(arr[idxA] * scale, height);
			for (int x = 0; x < width; x++) {
				var idxB = x * count / width;
				int y;
				if (1 < idxB - idxA) {
					y = AmpToY(arr[idxA] * scale, height);
					g.DrawLine(color, preX, preY, x, y);
					var max = double.MinValue;
					var min = double.MaxValue;
					for (var i = idxA; i <= idxB; i++) {
						var v = arr[i] * scale;
						min = Math.Min(min, v);
						max = Math.Max(max, v);
					}
					var minY = AmpToY(min, height);
					var maxY = AmpToY(max, height);
					g.DrawLine(color, x, minY, x, maxY);
					y = AmpToY(arr[idxB] * scale, height);
				}
				else {
					y = AmpToY(arr[idxB] * scale, height);
					g.DrawLine(color, preX, preY, x, y);
				}
				preX = x;
				preY = y;
				idxA = idxB;
			}
		}

		public static void Peak(Graphics g, double[] arr, int count, int width, int height) {
			var scale = Spectrum.AutoGain || Spectrum.NormGain ? 1 : 4;
			var minValue = Math.Pow(10, MinLevel / 20.0);
			var dx = (double)width / count;
			var ox = Spectrum.TONE_DIV * dx * 0.5;
			for (int x = 0, i = 0; x < count; x++, i++) {
				var val = arr[i] * scale;
				if (val > minValue) {
					var barA = (int)(x*dx - ox) + 1;
					var barB = (int)(x*dx + ox) + 1;
					var barY = AmpToY(val, height);
					var barHeight = height - barY;
					g.FillRectangle(PEAK, barA, barY, barB - barA, barHeight);
				}
			}
		}

		public static void Scroll(Bitmap bmp, double[] arr, int count, int top, int scrollHeight) {
			var scale = Spectrum.AutoGain || Spectrum.NormGain ? 1 : 4;
			var width = bmp.Width;
			var pix = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
			var offsetY0 = pix.Stride * top;
			var minValue = Math.Pow(10, MinLevel / 20.0);
			Array.Clear(ScrollCanvas, offsetY0, pix.Stride);
			if (DisplayPeak) {
				var dx = (double)width / count;
				var ox = Spectrum.TONE_DIV * dx * 0.5;
				for (int x = 0, i = 0; x < count; x++, i++) {
					if (arr[i] * scale > minValue) {
						var barA = (int)(x*dx - ox) + 1;
						var barB = (int)(x*dx + ox) + 1;
						SetHue(arr[i] * scale, offsetY0 + barA * 4, barB - barA);
					}
				}
			}
			else {
				for (int x = 0, i = 0; x < count; x++, i++) {
					if (arr[i] * scale > minValue) {
						var barA = x * width / count;
						var barB = (x + Spectrum.TONE_DIV) * width / count;
						SetHue(arr[i] * scale, offsetY0 + barA * 4, barB - barA);
					}
				}
			}
			for (int y = 1; y < KEYBOARD_HEIGHT; y++) {
				Buffer.BlockCopy(
					ScrollCanvas, offsetY0,
					ScrollCanvas, offsetY0 + pix.Stride * y,
					pix.Stride
				);
			}
			if (SCROLL_SPEED < scrollHeight) {
				var offsetY1 = pix.Stride * (top + KEYBOARD_HEIGHT);
				Buffer.BlockCopy(
					ScrollCanvas, offsetY1,
					ScrollCanvas, offsetY1 + pix.Stride * SCROLL_SPEED,
					pix.Stride * (scrollHeight - SCROLL_SPEED)
				);
				Buffer.BlockCopy(
					ScrollCanvas, offsetY0,
					ScrollCanvas, offsetY1,
					pix.Stride * SCROLL_SPEED
				);
			}
			Marshal.Copy(
				ScrollCanvas, offsetY0,
				pix.Scan0 + offsetY0,
				pix.Stride * (KEYBOARD_HEIGHT + scrollHeight)
			);
			bmp.UnlockBits(pix);
		}
	}
}
