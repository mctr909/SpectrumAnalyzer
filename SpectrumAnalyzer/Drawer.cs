using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Spectrum.Spectrum;

namespace SpectrumAnalyzer {
	static class Drawer {
		const int SCROLL_SPEED = 2;

		static readonly Font FONT = new Font("Meiryo UI", 10.5f);
		static readonly Pen OCT_BORDER = new Pen(Color.FromArgb(171, 171, 127), 1.0f);
		static readonly Pen KEY_BORDER = new Pen(Color.FromArgb(95, 95, 95), 1.0f);
		static readonly Pen WHITE_KEY = new Pen(Color.FromArgb(47, 47, 47), 1.0f);
		static readonly Pen BLACK_KEY = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
		static readonly Pen LEVEL_MAJOR = new Pen(Color.FromArgb(127, 127, 0), 1.0f);
		static readonly Pen LEVEL_MINOR = new Pen(Color.FromArgb(63, 63, 0), 1.0f);
		static readonly Pen FREQ_MAJOR = new Pen(Color.FromArgb(131, 131, 131), 1.0f);
		static readonly Pen FREQ_MINOR = new Pen(Color.FromArgb(111, 111, 111), 1.0f)
		{
			DashStyle = DashStyle.Custom,
			DashPattern = new float[] { 1, 3 }
		};

		public static readonly Pen CURVE = new Pen(Color.FromArgb(0, 255, 255), 1.0f);
		public static readonly Pen THRESHOLD = new Pen(Color.FromArgb(0, 211, 0), 1.0f);

		static readonly Brush PEAK = new Pen(Color.FromArgb(255, 255, 0)).Brush;
		static readonly Brush SURFACE = new Pen(Color.FromArgb(127, 95, 255, 95)).Brush;
		static readonly Brush SURFACE_H = new Pen(Color.FromArgb(95, 0, 255, 255)).Brush;

		static double mOffsetGain = 3.981;
		public static int OffsetDb {
			get { return (int)(20 * Math.Log10(mOffsetGain) + 0.5); }
			set { mOffsetGain = Math.Pow(10, value / 20.0); }
		}

		public const int DB_LABEL_WIDTH = 50;
		public const int KEYBOARD_HEIGHT = 24;
		public static byte[] ScrollCanvas;
		public static int MinDb = -30;
		public static int KeyboardShift = 0;

		public static bool DisplayPeak = true;
		public static bool DisplayCurve = false;
		public static bool DisplayThreshold = false;
		public static bool DisplayFreq = true;
		public static bool DisplayScroll = false;

		private static int DbToY(double db, int height) {
			if (db < MinDb) {
				db = MinDb;
			}
			return (int)(db * height / MinDb);
		}

		private static int LinearToY(double linear, int height) {
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

		private static void SetHue(double linear, int offset, int width) {
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
			var v = (int)((1.0 - db) * 1279);
			var a = v * 7 / 32;
			if (a > 255) {
				a = 255;
			}
			int r, g, b;
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
			for (int x = width, p = offset; x != 0; --x, p += 4) {
				ScrollCanvas[p    ] = (byte)b;
				ScrollCanvas[p + 1] = (byte)g;
				ScrollCanvas[p + 2] = (byte)r;
				ScrollCanvas[p + 3] = (byte)a;
			}
		}

		private static void DrawPianoRoll(Graphics g, int width, int height, int noteCount) {
			var bottom = height - 1;
			var keyDWidth = (double)width / noteCount;
			for (int n = 0; n < noteCount; n++) {
				var x0 = (int)(n * keyDWidth);
				var x1 = (int)((n + 1) * keyDWidth);
				var keyWidth = x1 - x0 + 1;
				var px = x0 + DB_LABEL_WIDTH;
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
		}

		private static void DrawOctLabel(Graphics g, int width, int offsetY, int noteCount) {
			var keyDWidth = (double)width / noteCount;
			var textWidth = g.MeasureString("10", FONT).Width;
			var textArea = new RectangleF(-1f, -1f, textWidth, KEYBOARD_HEIGHT);
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Near,
				LineAlignment = StringAlignment.Center
			};
			for (int n = -12; n < noteCount + 12; n += 12) {
				var note = n - KeyboardShift;
				if (note < 0) {
					continue;
				}
				var x = (float)(note * keyDWidth);
				var px = x + DB_LABEL_WIDTH;
				g.TranslateTransform(px, offsetY);
				g.DrawString($"{n / 12}", FONT, Brushes.LightGray, textArea, stringFormat);
				g.TranslateTransform(-px, -offsetY);
			}
		}

		private static void DrawLevelGauge(Graphics g, int width, int height) {
			var dbOfs = EnableAutoGain || EnableNormalize ? 0 : -OffsetDb;
			var dbMin = MinDb + dbOfs;
			var left = DB_LABEL_WIDTH;
			var right = left + width - 1;
			for (var db = dbOfs; dbMin <= db; --db) {
				var py = DbToY(db - dbOfs, height);
				switch (db % 6) {
				case 0:
					g.DrawLine(LEVEL_MAJOR, 0, py, right, py);
					break;
				default:
					g.DrawLine(LEVEL_MINOR, 0, py, right, py);
					break;
				}
			}
			var textSize = g.MeasureString("-12db", FONT);
			var textArea = new RectangleF(-2f, -FONT.Size, left, textSize.Height);
			var textTop = (int)(textSize.Height * 0.5);
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Center
			};
			for (var db = dbOfs; dbMin <= db; --db) {
				if (db % 6 == 0) {
					var py = DbToY(db - dbOfs, height);
					if (py < textTop) {
						py = textTop;
					}
					g.TranslateTransform(0, py);
					g.DrawString(db + "db", FONT, Brushes.Yellow, textArea, stringFormat);
					g.TranslateTransform(0, -py);
				}
			}
		}

		private static void DrawFreqGauge(Graphics g, int width, int height, int labelY) {
			var bottom = height - 1;
			var shift = 1.5 - KeyboardShift * HALFTONE_DIV;
			var textWidth = g.MeasureString("100", FONT).Width;
			var textArea = new RectangleF(-textWidth*0.5f, -1f, textWidth, KEYBOARD_HEIGHT);
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			for (var unit = 1; unit <= 10000; unit *= 10) {
				for (var i = 1; i < 10; i++) {
					var freq = i * unit;
					var bank = shift + Math.Log(freq / BASE_FREQ, 2.0) * OCT_DIV;
					if (bank < 0) {
						continue;
					}
					if (bank >= BANK_COUNT) {
						break;
					}
					var px = DB_LABEL_WIDTH + (float)(width * bank / BANK_COUNT);
					if (i == 1) {
						g.DrawLine(FREQ_MAJOR, px, 0, px, bottom);
					}
					else {
						g.DrawLine(FREQ_MINOR, px, 0, px, bottom);
					}
					if (i == 1 || i == 5) {
						g.TranslateTransform(px, labelY);
						var label = freq < 1000 ? $"{freq}" : $"{freq * 0.001}k";
						g.DrawString(label, FONT, Brushes.LightGray, textArea, stringFormat);
						g.TranslateTransform(-px, -labelY);
					}
				}
			}
		}

		public static void Background(PictureBox pictureBox, int keyboardTop, int noteCount) {
			var width  = pictureBox.Width - DB_LABEL_WIDTH;
			var left = DB_LABEL_WIDTH;
			var right = left + width;
			var keyboardBottom = keyboardTop + KEYBOARD_HEIGHT - 1;
			using (var g = Graphics.FromImage(pictureBox.BackgroundImage)) {
				g.SmoothingMode = SmoothingMode.None;
				g.Clear(Color.Black);
				if (DisplayFreq) {
					DrawLevelGauge(g, width, keyboardTop);
					DrawFreqGauge(g, width, pictureBox.Height, keyboardTop);
				}
				else {
					DrawPianoRoll(g, width, pictureBox.Height, noteCount);
					DrawOctLabel(g, width, keyboardTop, noteCount);
					DrawLevelGauge(g, width, keyboardTop);
				}
				g.DrawLine(OCT_BORDER, left, 0, left, pictureBox.Height);
				g.DrawLine(OCT_BORDER, left, keyboardTop, right, keyboardTop);
				g.DrawLine(OCT_BORDER, left, keyboardBottom, right, keyboardBottom);
				pictureBox.BackgroundImage = pictureBox.BackgroundImage;
			}
		}

		public static void Level(Graphics g, double val, int height) {
			var py = LinearToY(val, height);
			var barHeight = height - py;
			g.FillRectangle(SURFACE_H, 10, py, DB_LABEL_WIDTH - 20, barHeight);
		}

		public static void Curve(Graphics g, double[] arr, int width, int height, Pen color) {
			width -= DB_LABEL_WIDTH;
			var scale = EnableAutoGain || EnableNormalize ? 1 : mOffsetGain;
			var ofsX = DB_LABEL_WIDTH - (float)width / BANK_COUNT;
			var x0 = (float)DB_LABEL_WIDTH;
			var y0 = LinearToY(arr[0] * scale, height);
			if (BANK_COUNT > width) {
				var ixA = 0;
				for (int x = 0; x < width; x++) {
					var ixB = x * BANK_COUNT / width;
					var x1 = x + ofsX;
					var y1 = LinearToY(arr[ixA] * scale, height);
					g.DrawLine(color, x0, y0, x1, y1);
					var max = double.MinValue;
					var min = double.MaxValue;
					for (var i = ixA; i <= ixB; i++) {
						var v = arr[i] * scale;
						min = Math.Min(min, v);
						max = Math.Max(max, v);
					}
					var minY = LinearToY(min, height);
					var maxY = LinearToY(max, height);
					g.DrawLine(color, x1, minY, x1, maxY);
					y1 = LinearToY(arr[ixB] * scale, height);
					x0 = x1;
					y0 = y1;
					ixA = ixB;
				}
			}
			else {
				for (int x = 0; x < width; x++) {
					var ixD = (double)x * BANK_COUNT / width;
					var ixA = (int)ixD;
					var ixB = Math.Min(ixA + 1, BANK_COUNT - 1);
					var a2b = ixD - ixA;
					var x1 = Math.Max(DB_LABEL_WIDTH, x + ofsX);
					var y1 = LinearToY((arr[ixA] * (1 - a2b) + arr[ixB] * a2b) * scale, height);
					g.DrawLine(color, x0, y0, x1, y1);
					x0 = x1;
					y0 = y1;
				}
			}
		}

		public static void Surface(Graphics g, double[] arr, int width, int height) {
			width -= DB_LABEL_WIDTH;
			var color = DisplayPeak ? SURFACE_H : SURFACE;
			var scale = EnableAutoGain || EnableNormalize ? 1 : mOffsetGain;
			var ofsX = DB_LABEL_WIDTH - (float)width / BANK_COUNT;
			var minValue = Math.Pow(10, MinDb / 20.0);
			var dx = (float)width / BANK_COUNT;
			for (int i = 0; i < BANK_COUNT; i++) {
				var value = arr[i] * scale;
				if (value > minValue) {
					var x0 = (i - 0.5f) * dx;
					var x1 = (i + 0.5f) * dx;
					var y = LinearToY(value, height);
					var barWidth = x1 - x0;
					var barHeight = height - y;
					x0 += ofsX;
					g.FillRectangle(color, x0, y, barWidth, barHeight);
				}
			}
		}

		public static void Peak(Graphics g, double[] arr, int width, int height) {
			width -= DB_LABEL_WIDTH;
			var scale = EnableAutoGain || EnableNormalize ? 1 : mOffsetGain;
			var ofsX = DB_LABEL_WIDTH - (float)width / BANK_COUNT;
			var minValue = Math.Pow(10, MinDb / 20.0);
			var dx = (float)width / BANK_COUNT;
			var r = 0.5f * width / HALFTONE_COUNT - 1;
			for (int i = 0; i < BANK_COUNT; i++) {
				var value = arr[i] * scale;
				if (value > minValue) {
					var x0 = (i - 0.5f) * dx - r;
					var x1 = (i + 0.5f) * dx + r;
					var y = LinearToY(value, height);
					var barWidth = Math.Max(2, x1 - x0);
					x0 += ofsX;
					g.FillEllipse(PEAK, x0, y, barWidth, barWidth);
				}
			}
		}

		public static void Scroll(Bitmap bmp, double[] arr, int top, int scrollHeight, int keyboardHeight) {
			var scale = EnableAutoGain || EnableNormalize ? 1 : mOffsetGain;
			var width = bmp.Width - DB_LABEL_WIDTH;
			var pix = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
			var offsetY0 = pix.Stride * top;
			Array.Clear(ScrollCanvas, offsetY0, pix.Stride);
			if (DisplayPeak) {
				var dx = (double)width / BANK_COUNT;
				var ox = HALFTONE_CENTER * dx - 0.5;
				ox = Math.Max(1, ox);
				var minValue = Math.Pow(10, MinDb / 20.0);
				for (int i = 0; i < BANK_COUNT; i++) {
					var value = arr[i] * scale;
					if (value > minValue) {
						var x0 = (int)(i * dx - ox);
						var x1 = (int)(i * dx + ox);
						var hueWidth = x1 - x0;
						x0 += DB_LABEL_WIDTH;
						SetHue(value, offsetY0 + x0 * 4, hueWidth);
					}
				}
			}
			else {
				var dx = (double)BANK_COUNT / width;
				var ofsX = DB_LABEL_WIDTH - width / BANK_COUNT;
				for (int ix = 0; ix < width; ix++) {
					var ixD = ix * dx;
					var ixA = (int)ixD;
					var ixB = Math.Min(ixA + 1, BANK_COUNT - 1);
					var a2b = ixD - ixA;
					var x = Math.Max(DB_LABEL_WIDTH, ix + ofsX);
					SetHue((arr[ixA] * (1 - a2b) + arr[ixB] * a2b) * scale, offsetY0 + x * 4, 1);
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
