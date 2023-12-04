using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SpectrumAnalyzer {
	static class Drawer {
		const int SCROLL_SPEED = 3;

		const int ALPHA_MAX = 167;
		const int BLUE_RANGE = ALPHA_MAX + 1;
		const int CYAN_RANGE = BLUE_RANGE + 256;
		const int GREEN_RANGE = CYAN_RANGE + 256;
		const int YELLOW_RANGE = GREEN_RANGE + 256;
		const int RED_MAX = YELLOW_RANGE + 255;

		static readonly Font FONT = new Font("Meiryo UI", 8.0f);
		static readonly Pen KEYBOARD_BORDER = new Pen(Color.FromArgb(95, 95, 95), 1.0f);
		static readonly Pen WHITE_KEY = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
		static readonly Pen BLACK_KEY = new Pen(Color.FromArgb(31, 31, 31), 1.0f);
		static readonly Pen BAR = new Pen(Color.FromArgb(111, 191, 191, 191), 1.0f);
		static readonly Pen GRID_MAJOR = new Pen(Color.FromArgb(95, 95, 0), 1.0f);
		static readonly Pen GRID_MINOR1 = new Pen(Color.FromArgb(63, 63, 0), 1.0f);
		static readonly Pen GRID_MINOR2 = new Pen(Color.FromArgb(47, 47, 47), 1.0f);

		public static byte[] ScrollCanvas;
		public static int MinLevel = -30;

		static string ToString(double value) {
			if (10000 <= value) {
				return (value / 1000).ToString("#.#k");
			} else if (1000 <= value) {
				return (value / 1000).ToString("#.##k");
			} else if (100 <= value) {
				return value.ToString("#");
			} else if (10 <= value) {
				return value.ToString("#.#");
			} else {
				return value.ToString("#.##");
			}
		}

		static int DbToY(double db, int height) {
			if (db < MinLevel) {
				db = MinLevel;
			}
			return (int)(db * height / MinLevel);
		}

		static int AmpToY(double amp, int height) {
			if (amp < 1 / 32768.0) {
				amp = 1 / 32768.0;
			}
			var db = 20 * Math.Log10(amp);
			if (db < MinLevel) {
				db = MinLevel;
			}
			return (int)(db * height / MinLevel);
		}

		static void SetHue(double amp, int pos, int width) {
			if (amp < 1 / 32768.0) {
				amp = 1 / 32768.0;
			}
			var db = 20 * Math.Log10(amp);
			if (db < MinLevel) {
				return;
			}
			var v = (int)((1.0 - db / MinLevel) * RED_MAX);
			byte a, r, g, b;
			if (v < BLUE_RANGE) {
				b = 255;
				g = 0;
				r = 0;
				a = (byte)v;
			} else if (v < CYAN_RANGE) {
				b = 255;
				g = (byte)(v - BLUE_RANGE);
				r = 0;
				a = ALPHA_MAX;
			} else if (v < GREEN_RANGE) {
				b = (byte)(255 - CYAN_RANGE - v);
				g = 255;
				r = 0;
				a = ALPHA_MAX;
			} else if (v < YELLOW_RANGE) {
				b = 0;
				g = 255;
				r = (byte)(v - GREEN_RANGE);
				a = ALPHA_MAX;
			} else {
				b = 0;
				g = (byte)(255 - YELLOW_RANGE - v);
				r = 255;
				a = ALPHA_MAX;
			}
			for (int x = 0, p = pos; x < width; x++, p += 4) {
				ScrollCanvas[p + 0] = b;
				ScrollCanvas[p + 1] = g;
				ScrollCanvas[p + 2] = r;
				ScrollCanvas[p + 3] = a;
			}
		}

		public static void Keyboard(
			Graphics g,
			int width, int height,
			int gaugeHeight, int keyboardHeight,
			int transepose, int noteCount, double baseFreq
		) {
			var barBottom = height - 1;
			for (int n = 0; n < noteCount; n++) {
				var note = n + transepose;
				var px = (n + 0.0f) * width / noteCount;
				var barWidth = (n + 1.0f) * width / noteCount - px + 1;
				switch ((note + 24) % 12) {
				case 0:
					g.FillRectangle(WHITE_KEY.Brush, px, 0, barWidth, height);
					g.DrawLine(KEYBOARD_BORDER, px, 0, px, barBottom);
					break;
				case 2:
				case 4:
				case 7:
				case 9:
				case 11:
					g.FillRectangle(WHITE_KEY.Brush, px, 0, barWidth, height);
					break;
				case 5:
					g.DrawLine(BLACK_KEY, px, 0, px, barBottom);
					break;
				default:
					g.FillRectangle(BLACK_KEY.Brush, px, 0, barWidth, height);
					break;
				}
			}
			var right = width - 1;
			var keyboardBottom = gaugeHeight + keyboardHeight - 1;
			var textBottom = keyboardBottom + 1;
			var textHeight = g.MeasureString("9", FONT).Height;
			var textOfsX = textHeight * 0.5f;
			var textArea = new RectangleF(0f, 0f, keyboardHeight, textHeight);
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Far,
				LineAlignment = StringAlignment.Center
			};
			for (int n = -3; n < noteCount + 12; n += 12) {
				var note = n - transepose;
				var px = width * (note + 0.5f) / noteCount - textOfsX;
				g.TranslateTransform(px, textBottom);
				g.RotateTransform(-90);
				g.DrawString(
					ToString(baseFreq * Math.Pow(2.0, n / 12.0)),
					FONT, Brushes.Gray, textArea, stringFormat
				);
				g.RotateTransform(90);
				g.TranslateTransform(-px, -textBottom);
			}
			g.DrawLine(KEYBOARD_BORDER, 0, keyboardBottom, right, keyboardBottom);
		}

		public static void Gauge(Graphics g, int width, int height) {
			var right = width - 1;
			for (double db = 0; MinLevel <= db; db -= 1.0) {
				var py = DbToY(db, height);
				if (db % 10 == 0) {
					g.DrawLine(GRID_MAJOR, 0, py, right, py);
				} else if (height >= -MinLevel && db % 5 == 0) {
					g.DrawLine(GRID_MINOR1, 0, py, right, py);
				} else if (height >= -4 * MinLevel) {
					g.DrawLine(GRID_MINOR2, 0, py, right, py);
				}
			}
			var textSize = g.MeasureString("9", FONT);
			var textArea = new RectangleF(0f, 0f, 24, textSize.Height);
			var textBottom = height - textArea.Height + 4;
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Near
			};
			for (double db = 0; MinLevel < db; db -= 10.0) {
				var py = DbToY(db, height) - 2;
				if (py < textBottom) {
					g.TranslateTransform(0, py);
					g.DrawString(db + "", FONT, Brushes.Gray, textArea, stringFormat);
					g.TranslateTransform(0, -py);
				} else {
					g.TranslateTransform(0, textBottom);
					g.DrawString(db + "", FONT, Brushes.Gray, textArea, stringFormat);
					g.TranslateTransform(0, -textBottom);
				}
			}
		}

		public static void Peak(Graphics g, double[] arr, int width, int height) {
			var count = arr.Length;
			for (int i = 0; i < count; i++) {
				var barY = AmpToY(arr[i], height);
				var barHeight = height - barY;
				if (0 < barHeight) {
					var barX = (i - 1) * width / count + 1;
					var barWidth = (i + 2) * width / count - barX + 1;
					g.FillRectangle(BAR.Brush, barX, barY, barWidth, barHeight);
				}
			}
		}

		public static void Slope(Graphics g, double[] arr, int width, int height, Pen color) {
			var idxA = 0;
			var preX = 0;
			var preY = AmpToY(arr[idxA], height);
			for (int x = 0; x < width; x++) {
				var idxB = x * arr.Length / width;
				int y;
				if (1 < idxB - idxA) {
					y = AmpToY(arr[idxA], height);
					g.DrawLine(color, preX, preY, x, y);
					var max = double.MinValue;
					var min = double.MaxValue;
					for (var i = idxA; i <= idxB; i++) {
						var v = arr[i];
						min = Math.Min(min, v);
						max = Math.Max(max, v);
					}
					var minY = AmpToY(min, height);
					var maxY = AmpToY(max, height);
					g.DrawLine(color, x, minY, x, maxY);
					y = AmpToY(arr[idxB], height);
				} else {
					y = AmpToY(arr[idxB], height);
					g.DrawLine(color, preX, preY, x, y);
				}
				preX = x;
				preY = y;
				idxA = idxB;
			}
		}

		public static void Spectrum(Bitmap bmp, double[] arr, int top, int keyboardHeight, int scrollHeight) {
			var width = bmp.Width;
			var count = arr.Length;
			var data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
			var offsetY0 = data.Stride * top;
			Array.Clear(ScrollCanvas, offsetY0, data.Stride);
			var idxA = 0;
			for (int x = 0; x < width; x++) {
				var idxB = x * count / width;
				double amp;
				if (1 < idxB - idxA) {
					amp = double.MinValue;
					for (var i = idxA; i <= idxB; i++) {
						amp = Math.Max(amp, arr[i]);
					}
				} else {
					amp = arr[idxB];
				}
				var barX = (idxB - 1) * width / count + 1;
				var barWidth = (idxB + 2) * width / count - barX + 1;
				SetHue(amp, offsetY0 + barX * 4, barWidth);
				idxA = idxB;
			}
			for (int y = 1; y < keyboardHeight; y++) {
				Buffer.BlockCopy(
					ScrollCanvas, offsetY0,
					ScrollCanvas, offsetY0 + data.Stride * y,
					data.Stride
				);
			}
			var offsetY1 = data.Stride * (top + keyboardHeight);
			Buffer.BlockCopy(
				ScrollCanvas, offsetY1,
				ScrollCanvas, offsetY1 + data.Stride * SCROLL_SPEED,
				data.Stride * (scrollHeight - SCROLL_SPEED)
			);
			Buffer.BlockCopy(
				ScrollCanvas, offsetY0,
				ScrollCanvas, offsetY1,
				data.Stride * SCROLL_SPEED
			);
			Marshal.Copy(
				ScrollCanvas, offsetY0,
				data.Scan0 + offsetY0,
				data.Stride * (keyboardHeight + scrollHeight)
			);
			bmp.UnlockBits(data);
		}
	}
}
