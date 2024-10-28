using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Spectrum.Spectrum;

namespace SpectrumAnalyzer {
	static class Drawer {
		static readonly Font FONT = new Font("Meiryo UI", 11f);
		static readonly Pen OCT_BORDER = new Pen(Color.FromArgb(95, 95, 71), 1.0f);
		static readonly Pen KEY_BORDER = new Pen(Color.FromArgb(63, 63, 63), 1.0f);
		static readonly Pen WHITE_KEY = new Pen(Color.FromArgb(31, 31, 31), 1.0f);
		static readonly Pen BLACK_KEY = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
		static readonly Pen LEVEL_MAJOR = new Pen(Color.FromArgb(127, 127, 0), 1.0f);
		static readonly Pen LEVEL_MINOR = new Pen(Color.FromArgb(63, 63, 0), 1.0f);
		static readonly Pen FREQ_MAJOR = new Pen(Color.FromArgb(131, 131, 131), 1.0f);
		static readonly Pen FREQ_MINOR = new Pen(Color.FromArgb(127, 127, 127), 1.0f)
		{
			DashStyle = DashStyle.Custom,
			DashPattern = new float[] { 1, 3 }
		};

		static double mOffsetGain = 3.981;
		public static int OffsetDb {
			get { return (int)(20 * Math.Log10(mOffsetGain) + 0.5); }
			set { mOffsetGain = Math.Pow(10, value / 20.0); }
		}

		public static byte[] ScrollCanvas;
		public static int MinDb = -30;
		public static int KeyboardShift = 0;

		public static void Background(PictureBox pictureBox, int ofsX, int keyboardTop, int keyboardHeight, int noteCount = 0) {
			var width = pictureBox.Width - ofsX;
			var right = ofsX + width;
			var bottom = pictureBox.Height - 1;
			var keyboardBottom = keyboardTop + keyboardHeight - 1;
			var g = Graphics.FromImage(pictureBox.BackgroundImage);
			g.SmoothingMode = SmoothingMode.None;
			g.Clear(Color.Black);
			if (noteCount == 0) {
				levelGauge();
				freqGauge();
			} else {
				pianoRoll();
				levelGauge();
			}
			g.DrawLine(OCT_BORDER, ofsX, 0, ofsX, pictureBox.Height);
			g.DrawLine(OCT_BORDER, ofsX, keyboardTop, right, keyboardTop);
			g.DrawLine(OCT_BORDER, ofsX, keyboardBottom, right, keyboardBottom);
			pictureBox.BackgroundImage = pictureBox.BackgroundImage;

			void levelGauge() {
				var dbOfs = EnableAutoGain || EnableNormalize ? 0 : -OffsetDb;
				var dbMin = MinDb + dbOfs;
				for (var db = dbOfs; dbMin <= db; --db) {
					var py = DbToY(db - dbOfs, keyboardTop);
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
				var textArea = new RectangleF(-2f, -FONT.Size, ofsX, textSize.Height);
				var textTop = (int)(textSize.Height * 0.5);
				var stringFormat = new StringFormat() {
					Alignment = StringAlignment.Center
				};
				for (var db = dbOfs; dbMin <= db; --db) {
					if (db % 6 == 0) {
						var py = DbToY(db - dbOfs, keyboardTop);
						if (py < textTop) {
							py = textTop;
						}
						g.TranslateTransform(0, py);
						g.DrawString($"{db}db", FONT, Brushes.Yellow, textArea, stringFormat);
						g.TranslateTransform(0, -py);
					}
				}
			}

			void freqGauge() {
				var shift = -1 - KeyboardShift * HALFTONE_DIV;
				var textWidth = g.MeasureString("100", FONT).Width;
				var textArea = new RectangleF(-textWidth * 0.5f, -1f, textWidth, keyboardHeight);
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
						var px = ofsX + (float)(width * bank / BANK_COUNT);
						if (i == 1) {
							g.DrawLine(FREQ_MAJOR, px, 0, px, bottom);
						} else {
							g.DrawLine(FREQ_MINOR, px, 0, px, bottom);
						}
						if (i == 1 || i == 5) {
							g.TranslateTransform(px, keyboardTop);
							var label = freq < 1000 ? $"{freq}" : $"{freq * 0.001}k";
							g.DrawString(label, FONT, Brushes.LightGray, textArea, stringFormat);
							g.TranslateTransform(-px, -keyboardTop);
						}
					}
				}
			}

			void pianoRoll() {
				var keyDWidth = (double)width / noteCount;
				for (int n = 0; n < noteCount; n++) {
					var x0 = (float)(n * keyDWidth);
					var x1 = (float)((n + 1) * keyDWidth);
					var keyWidth = x1 - x0 + 1;
					var px = x0 + ofsX;
					var note = (n + KeyboardShift + 24) % 12;
					switch (note) {
					case 0:
						g.FillRectangle(WHITE_KEY.Brush, px, 0, keyWidth, pictureBox.Height);
						g.DrawLine(OCT_BORDER, px, 0, px, bottom);
						break;
					case 2:
					case 4:
					case 7:
					case 9:
					case 11:
						g.FillRectangle(WHITE_KEY.Brush, px, 0, keyWidth, pictureBox.Height);
						break;
					case 5:
						g.FillRectangle(WHITE_KEY.Brush, px, 0, keyWidth, pictureBox.Height);
						g.DrawLine(KEY_BORDER, px, 0, px, bottom);
						break;
					default:
						g.FillRectangle(BLACK_KEY.Brush, px, 0, keyWidth, pictureBox.Height);
						break;
					}
				}
				var textWidth = g.MeasureString("10", FONT).Width;
				var textArea = new RectangleF(-1f, -1f, textWidth, keyboardHeight);
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
					var px = x + ofsX;
					g.TranslateTransform(px, keyboardTop);
					g.DrawString($"{n / 12}", FONT, Brushes.LightGray, textArea, stringFormat);
					g.TranslateTransform(-px, -keyboardTop);
				}
			}
		}

		public static void Curve(Graphics g, double[] arr, int ofsX, int width, int height, Pen color) {
			var scale = EnableAutoGain || EnableNormalize ? 1 : mOffsetGain;
			var left = ofsX - (float)width / BANK_COUNT;
			var x0 = (float)ofsX;
			var y0 = LinearToY(arr[0] * scale, height);
			if (width < BANK_COUNT) {
				var ixA = 0;
				for (int x = 0; x < width; x++) {
					var ixB = x * BANK_COUNT / width;
					var x1 = x + left;
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
			} else {
				for (int x = 0; x < width; x++) {
					var ixD = (double)x * BANK_COUNT / width;
					var ixA = (int)ixD;
					var ixB = Math.Min(ixA + 1, BANK_COUNT - 1);
					var a2b = ixD - ixA;
					var x1 = Math.Max(ofsX, x + left);
					var y1 = LinearToY((arr[ixA] * (1 - a2b) + arr[ixB] * a2b) * scale, height);
					g.DrawLine(color, x0, y0, x1, y1);
					x0 = x1;
					y0 = y1;
				}
			}
		}

		public static void Surface(Graphics g, double[] arr, int ofsX, int width, int height, Brush color) {
			var scale = EnableAutoGain || EnableNormalize ? 1 : mOffsetGain;
			var left = ofsX - (float)width / BANK_COUNT;
			var minValue = Math.Pow(10, MinDb / 20.0);
			var dx = (float)width / BANK_COUNT;
			for (int i = 0; i < BANK_COUNT; i++) {
				var value = arr[i] * scale;
				if (value > minValue) {
					var x0 = (i - 0.5f) * dx;
					var x1 = (i + 0.5f) * dx;
					var y = LinearToY(value, height);
					x0 = Math.Max(ofsX - left, x0);
					var barWidth = x1 - x0;
					var barHeight = height - y;
					x0 += left;
					g.FillRectangle(color, x0, y, barWidth, barHeight);
				}
			}
		}

		public static void Peak(Graphics g, double[] arr, int ofsX, int width, int height, Pen color) {
			var scale = EnableAutoGain || EnableNormalize ? 1 : mOffsetGain;
			var left = ofsX - (float)width / BANK_COUNT;
			var minValue = Math.Pow(10, MinDb / 20.0);
			var dx = (float)width / BANK_COUNT;
			var r = 0.5f * width / HALFTONE_COUNT - 1;
			for (int i = 0; i < BANK_COUNT; i++) {
				var value = arr[i] * scale;
				if (value > minValue) {
					var x0 = (i - 0.5f) * dx - r;
					var x1 = (i + 0.5f) * dx + r;
					var y = LinearToY(value, height);
					x0 += Math.Max(2, x1 - x0) / 2;
					x0 += left;
					g.DrawLine(color, x0, y, x0, height);
				}
			}
		}

		public static void Level(Graphics g, double val, int ofsX, int width, int height, Brush color) {
			var py = LinearToY(val, height);
			var barHeight = height - py;
			g.FillRectangle(color, ofsX, py, width, barHeight);
		}

		public static void Scroll(Bitmap bmp, double[] arr, int ofsX, int ofsY, int height, int keyboardHeight, int scrollSpeed) {
			var scrollTop = keyboardHeight - 1;
			var pix = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
			var stride = pix.Stride;
			var ofsKeyboardTop = stride * ofsY;
			var ofsScrollTop = ofsKeyboardTop + stride * scrollTop;
			var scale = EnableAutoGain || EnableNormalize ? 1 : mOffsetGain;
			var width = bmp.Width - ofsX;
			var left = ofsX - width / BANK_COUNT;
			var dx = (double)BANK_COUNT / width;
			for (int x = 0; x < width; x++) {
				var ixD = x * dx;
				var ixA = (int)ixD;
				var ixB = Math.Min(ixA + 1, BANK_COUNT - 1);
				var a2b = ixD - ixA;
				var pos = ofsKeyboardTop + Math.Max(ofsX, x + left) * 4;
				SetHue((arr[ixA] * (1 - a2b) + arr[ixB] * a2b) * scale, pos);
			}
			for (int y = 1; y < scrollTop; y++) {
				Buffer.BlockCopy(
					ScrollCanvas, ofsKeyboardTop,
					ScrollCanvas, ofsKeyboardTop + stride * y,
					stride
				);
			}
			if (scrollSpeed < height) {
				Buffer.BlockCopy(
					ScrollCanvas, ofsScrollTop,
					ScrollCanvas, ofsScrollTop + stride * scrollSpeed,
					stride * (height - scrollSpeed)
				);
				Buffer.BlockCopy(
					ScrollCanvas, ofsKeyboardTop,
					ScrollCanvas, ofsScrollTop,
					stride * scrollSpeed
				);
			}
			Marshal.Copy(
				ScrollCanvas, ofsKeyboardTop,
				pix.Scan0 + ofsKeyboardTop,
				stride * (keyboardHeight + height)
			);
			bmp.UnlockBits(pix);
		}

		private static int DbToY(double db, int height) {
			if (db < MinDb) {
				db = MinDb;
			}
			return (int)(db * height / MinDb);
		}

		private static float LinearToY(double linear, int height) {
			if (linear < 1e-9) {
				linear = 1e-9;
			}
			var db = 20 * Math.Log10(linear) / MinDb;
			if (db < 0) {
				db = 0;
			}
			if (db > 1) {
				db = 1;
			}
			return (float)(db * height);
		}

		private static void SetHue(double value, int pos) {
			if (value < 1e-9) {
				value = 1e-9;
			}
			var db = 20 * Math.Log10(value) / MinDb;
			if (db < 0) {
				db = 0;
			}
			if (db > 1) {
				db = 1;
			}
			var v = (int)((1.0 - db) * 1279);
			var a = v * 9 / 32;
			if (a > 255) {
				a = 255;
			}
			int r, g, b;
			if (v < 256) {
				b = 255;
				g = 0;
				r = 0;
			} else if (v < 512) {
				b = 255;
				g = v - 256;
				r = 0;
			} else if (v < 768) {
				b = 255 - (v - 512);
				g = 255;
				r = 0;
			} else if (v < 1024) {
				b = 0;
				g = 255;
				r = v - 768;
			} else {
				b = 0;
				g = 255 - (v - 1024);
				r = 255;
			}
			ScrollCanvas[pos] = (byte)b;
			ScrollCanvas[pos + 1] = (byte)g;
			ScrollCanvas[pos + 2] = (byte)r;
			ScrollCanvas[pos + 3] = (byte)a;
		}
	}
}
