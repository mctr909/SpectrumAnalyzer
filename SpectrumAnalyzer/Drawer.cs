using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static Spectrum.Spectrum;

namespace SpectrumAnalyzer {
	static class Drawer {
		const int SCROLL_SPEED = 3;

		static readonly Font FONT_OCT = new Font("Meiryo UI", 10f);
		static readonly Font FONT_DB = new Font("Meiryo UI", 9f);
		static readonly Pen OCT_BORDER = new Pen(Color.FromArgb(171, 171, 147), 1.0f);
		static readonly Pen KEY_BORDER = new Pen(Color.FromArgb(95, 95, 95), 1.0f);
		static readonly Pen WHITE_KEY = new Pen(Color.FromArgb(71, 71, 71), 1.0f);
		static readonly Pen BLACK_KEY = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
		static readonly Pen GRID_MAJOR = new Pen(Color.FromArgb(167, 147, 0), 1.0f);
		static readonly Pen GRID_MINOR = new Pen(Color.FromArgb(131, 131, 131), 1.0f);

		static readonly Brush PEAK_TOP = new Pen(Color.FromArgb(63, 255, 63)).Brush;
		static readonly Brush SURFACE = new Pen(Color.FromArgb(127, 63, 255, 63)).Brush;
		static readonly Brush SURFACE_H = new Pen(Color.FromArgb(95, 255, 255, 255)).Brush;

		/// <summary>ゲイン自動調整 最大[10^-(db/10)]</summary>
		public const double AUTOGAIN_MAX = 3.981e-03;
		/// <summary>ゲイン自動調整 速度[秒]</summary>
		public const double AUTOGAIN_SPEED = 3.0;

		static double mOffsetGain = 3.981;
		public static int OffsetDb {
			get { return (int)(20 * Math.Log10(mOffsetGain) + 0.5); }
			set { mOffsetGain = Math.Pow(10, value / 20.0); }
		}

		public const int DB_LABEL_WIDTH = 40;
		public const int KEYBOARD_HEIGHT = 24;
		public static byte[] ScrollCanvas;
		public static int MinDb = -36;
		public static int KeyboardShift = 0;

		public static bool DisplayScroll = false;
		public static bool DisplayPeak = false;
		public static bool DisplayCurve = false;

		public static bool AutoGain = true;
		public static bool NormGain = false;

		static int DbToY(double db, int height) {
			if (db < MinDb) {
				db = MinDb;
			}
			return (int)(db * height / MinDb);
		}

		static int LinearToY(double linear, int height) {
			if (linear < 1e-8) {
				linear = 1e-8;
			}
			var db = 20 * Math.Log10(linear) / MinDb;
			if (db < 0) {
				db = 0;
			}
			if (db > 1) {
				db = 1;
			}
			return (int)(db * height);
		}

		static void SetHue(double linear, int offset, int width) {
			if (linear < 1e-8) {
				linear = 1e-8;
			}
			var db = 20 * Math.Log10(linear) / MinDb;
			if (db < 0) {
				db = 0;
			}
			if (db > 1) {
				db = 1;
			}
			var v = (1.0 - db) * 1279;
			var a = v / 4.5;
			if (a > 255) {
				a = 255;
			}
			double r, g, b;
			if (v < 256) {
				b = 255;
				g = 0;
				r = 0;
			}
			else if (v < 512) {
				b = 255;
				g = v - 256;
				r = 0;
			}
			else if (v < 768) {
				b = 255 - (v - 512);
				g = 255;
				r = 0;
			}
			else if (v < 1024) {
				b = 0;
				g = 255;
				r = v - 768;
			}
			else {
				b = 0;
				g = 255 - (v - 1024);
				r = 255;
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
			width -= DB_LABEL_WIDTH;
			var right = width + DB_LABEL_WIDTH - 1;
			var bottom = height - 1;
			var keyDWidth = (double)width / noteCount;
			for (int n = 0; n < noteCount; n++) {
				var px = (int)(n * keyDWidth);
				var keyWidth = (int)((n + 1) * keyDWidth) - px + 1;
				px += DB_LABEL_WIDTH;
				var note = (n + KeyboardShift + 24) % 12;
				switch (note) {
				case 0:
					g.FillRectangle(WHITE_KEY.Brush, px, 0, keyWidth, height);
					g.DrawLine(OCT_BORDER, px, 0, px, bottom);
					break;
				case 2:
				case 4:
				case 7:
				case 9:
				case 11:
					g.FillRectangle(WHITE_KEY.Brush, px, 0, keyWidth, height);
					break;
				case 5:
					g.FillRectangle(WHITE_KEY.Brush, px, 0, keyWidth, height);
					g.DrawLine(KEY_BORDER, px, 0, px, bottom);
					break;
				default:
					g.FillRectangle(BLACK_KEY.Brush, px, 0, keyWidth, height);
					break;
				}
			}
			var keyboardBottom = gaugeHeight + KEYBOARD_HEIGHT - 1;
			var textBottom = keyboardBottom + 1;
			var textHeight = g.MeasureString("9", FONT_OCT).Height;
			var textOfsX = textHeight * 0.5f;
			var textArea = new RectangleF(0f, 0f, KEYBOARD_HEIGHT, textHeight);
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Far,
				LineAlignment = StringAlignment.Center
			};
			for (int n = -12; n < noteCount + 12; n += 12) {
				var note = n - KeyboardShift;
				var px = (float)(note * keyDWidth) + 6;
				if (px < 0) {
					continue;
				}
				px += DB_LABEL_WIDTH - textOfsX;
				g.TranslateTransform(px, textBottom);
				g.RotateTransform(-90);
				g.DrawString("" + (n / 12), FONT_OCT, Brushes.LightGray, textArea, stringFormat);
				g.RotateTransform(90);
				g.TranslateTransform(-px, -textBottom);
			}
			g.DrawLine(OCT_BORDER, DB_LABEL_WIDTH, keyboardBottom, right, keyboardBottom);
		}

		public static void Gauge(Graphics g, int width, int height) {
			var dbOfs = AutoGain || NormGain ? 0 : -OffsetDb;
			var dbMin = MinDb + dbOfs;
			var right = width - 1;
			g.DrawLine(GRID_MINOR, DB_LABEL_WIDTH, height, width, height);
			g.DrawLine(GRID_MINOR, DB_LABEL_WIDTH, 0, DB_LABEL_WIDTH, height + KEYBOARD_HEIGHT);
			for (var db = dbOfs; dbMin <= db; --db) {
				var py = DbToY(db - dbOfs, height);
				if (db % 6 == 0) {
					g.DrawLine(GRID_MAJOR, DB_LABEL_WIDTH - 3, py, right, py);
				}
			}
			var textSize = g.MeasureString("-12db", FONT_DB);
			var textArea = new RectangleF(0f, 0f, DB_LABEL_WIDTH, textSize.Height);
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Center
			};
			for (var db = dbOfs; dbMin <= db; --db) {
				if (db % 6 == 0) {
					var py = DbToY(db - dbOfs, height) - 9;
					if (py < -2) {
						py = -2;
					}
					g.TranslateTransform(0, py);
					g.DrawString(db + "db", FONT_DB, Brushes.Yellow, textArea, stringFormat);
					g.TranslateTransform(0, -py);
				}
			}
		}

		public static void Curve(Graphics g, double[] arr, int width, int height, Pen color) {
			width -= DB_LABEL_WIDTH;
			var scale = AutoGain || NormGain ? 1 : mOffsetGain;
			var px0 = DB_LABEL_WIDTH;
			var py0 = LinearToY(arr[0] * scale, height);
			if (BANK_COUNT > width) {
				var idxA = 0;
				for (int x = 0; x < width; x++) {
					var idxB = x * BANK_COUNT / width;
					var px1 = x + DB_LABEL_WIDTH;
					var py1 = LinearToY(arr[idxA] * scale, height);
					g.DrawLine(color, px0, py0, px1, py1);
					var max = double.MinValue;
					var min = double.MaxValue;
					for (var i = idxA; i <= idxB; i++) {
						var v = arr[i] * scale;
						min = Math.Min(min, v);
						max = Math.Max(max, v);
					}
					var minY = LinearToY(min, height);
					var maxY = LinearToY(max, height);
					g.DrawLine(color, px1, minY, px1, maxY);
					py1 = LinearToY(arr[idxB] * scale, height);
					px0 = px1;
					py0 = py1;
					idxA = idxB;
				}
			}
			else {
				for (int x = 0; x < width; x++) {
					var idxD = (double)x * BANK_COUNT / width;
					var idxA = (int)idxD;
					var idxB = Math.Min(idxA + 1, BANK_COUNT - 1);
					var a2b = idxD - idxA;
					var px1 = x + DB_LABEL_WIDTH;
					var py1 = LinearToY((arr[idxA] * (1 - a2b) + arr[idxB] * a2b) * scale, height);
					g.DrawLine(color, px0, py0, px1, py1);
					px0 = px1;
					py0 = py1;
				}
			}
		}

		public static void Surface(Graphics g, double[] arr, int width, int height) {
			var color = DisplayPeak ? SURFACE_H : SURFACE;
			var scale = AutoGain || NormGain ? 1 : mOffsetGain;
			var minValue = Math.Pow(10, MinDb / 20.0);
			var dx = (float)(width - DB_LABEL_WIDTH) / BANK_COUNT;
			for (int x = 0, i = 0; x < BANK_COUNT; x++, i++) {
				var val = arr[i] * scale;
				if (val > minValue) {
					var px0 = (x - 0.5f) * dx;
					var px1 = (x + 0.5f) * dx;
					var py = LinearToY(val, height);
					var barWidth = px1 - px0;
					var barHeight = height - py;
					px0 += DB_LABEL_WIDTH;
					g.FillRectangle(color, px0, py, barWidth, barHeight);
				}
			}
		}

		public static void Peak(Graphics g, double[] arr, int width, int height) {
			width -= DB_LABEL_WIDTH;
			var scale = AutoGain || NormGain ? 1 : mOffsetGain;
			var minValue = Math.Pow(10, MinDb / 20.0);
			var dx = (float)width / BANK_COUNT;
			var ox = HALFTONE_DIV * dx * 0.5f;
			for (int x = 0, i = 0; x < BANK_COUNT; x++, i++) {
				var val = arr[i] * scale;
				if (val > minValue) {
					var px0 = (int)(x*dx - ox);
					var px1 = (int)(x*dx + ox);
					var py = LinearToY(val, height);
					var barWidth = px1 - px0;
					px0 += DB_LABEL_WIDTH;
					g.FillRectangle(PEAK_TOP, px0, py, barWidth, barWidth);
				}
			}
		}

		public static void Scroll(Bitmap bmp, double[] arr, int top, int scrollHeight, int keyboardHeight) {
			var scale = AutoGain || NormGain ? 1 : mOffsetGain;
			var width = bmp.Width - DB_LABEL_WIDTH;
			var pix = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
			var offsetY0 = pix.Stride * top;
			Array.Clear(ScrollCanvas, offsetY0, pix.Stride);
			if (DisplayPeak) {
				var dx = (float)width / BANK_COUNT;
				var ox = HALFTONE_DIV * dx * 0.5f;
				var minValue = Math.Pow(10, MinDb / 20.0);
				for (int i = 0; i < BANK_COUNT; i++) {
					var value = arr[i] * scale;
					if (value > minValue) {
						var px0 = (int)(i*dx - ox);
						var px1 = (int)(i*dx + ox);
						var hueWidth = px1 - px0;
						px0 += DB_LABEL_WIDTH;
						SetHue(value, offsetY0 + px0 * 4, hueWidth);
					}
				}
			}
			else {
				for (int x = 0; x < width; x++) {
					var idxD = (double)x * BANK_COUNT / width;
					var idxA = (int)idxD;
					var idxB = Math.Min(idxA + 1, BANK_COUNT - 1);
					var a2b = idxD - idxA;
					var px = x + DB_LABEL_WIDTH;
					SetHue((arr[idxA] * (1 - a2b) + arr[idxB] * a2b) * scale, offsetY0 + px * 4, 1);
				}
			}
			for (int y = 1; y < keyboardHeight; y++) {
				Buffer.BlockCopy(
					ScrollCanvas, offsetY0,
					ScrollCanvas, offsetY0 + pix.Stride * y,
					pix.Stride
				);
			}
			if (SCROLL_SPEED < scrollHeight) {
				var offsetY1 = pix.Stride * (top + keyboardHeight);
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
				pix.Stride * (keyboardHeight + scrollHeight)
			);
			bmp.UnlockBits(pix);
		}
	}
}
