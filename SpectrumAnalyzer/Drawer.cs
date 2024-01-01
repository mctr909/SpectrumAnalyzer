using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Spectrum.Spectrum;

namespace SpectrumAnalyzer {
	class Drawer {
		private static readonly Font FONT = new Font("Meiryo UI", 11f);
		private static readonly Pen OCT_BORDER = new Pen(Color.FromArgb(95, 95, 71), 1.0f);
		private static readonly Pen KEY_BORDER = new Pen(Color.FromArgb(63, 63, 63), 1.0f);
		private static readonly Pen WHITE_KEY = new Pen(Color.FromArgb(31, 31, 31), 1.0f);
		private static readonly Pen BLACK_KEY = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
		private static readonly Brush SURFACE = new Pen(Color.FromArgb(111, 255, 255, 255)).Brush;
		private static readonly Pen THRESHOLD = new Pen(Color.FromArgb(0, 231, 0), 1.0f);
		private static readonly Pen PEAK = new Pen(Color.FromArgb(191, 191, 191), 1.0f);
		private static readonly Pen LEVEL_MAJOR = new Pen(Color.FromArgb(127, 127, 0), 1.0f);
		private static readonly Pen LEVEL_MINOR = new Pen(Color.FromArgb(47, 47, 47), 1.0f);
		private static readonly Pen FREQ_MAJOR = new Pen(Color.FromArgb(91, 91, 91), 1.0f);
		private static readonly Pen FREQ_MINOR = new Pen(Color.FromArgb(91, 91, 91), 1.0f)
		{
			DashStyle = DashStyle.Custom,
			DashPattern = new float[] { 1, 3 }
		};

		private const int KeyboardHeight = 24;

		public const int LabelWidth = 50;

		public static int OffsetDb = 12;
		public static int MinDb = -36;
		public static int KeyShift = 0;

		private readonly double[] mPeak = new double[BANK_COUNT];
		private readonly double[] mCurve = new double[BANK_COUNT];
		private readonly double[] mThreshold = new double[BANK_COUNT];
		private byte[] mScrollCanvas;
		private int mKeyboardBottom;
		private int mGraphWidth;
		private int mGraphHeight;
		private float mGraphLeft;
		private int mKeyboardTop;
		private int mScrollTop;
		private int mScrollBottom;
		private PictureBox mPictureBox;
		private Graphics mG;
		private Graphics mGb;

		public void Resize(PictureBox pictureBox, bool enableScroll) {
			mPictureBox = pictureBox;
			if (null != mG) {
				mG.Dispose();
				mG = null;
			}
			if (null != mGb) {
				mGb.Dispose();
				mGb = null;
			}
			if (null != pictureBox.Image) {
				pictureBox.Image.Dispose();
				pictureBox.Image = null;
			}
			if (null != pictureBox.BackgroundImage) {
				pictureBox.BackgroundImage.Dispose();
				pictureBox.BackgroundImage = null;
			}
			pictureBox.Image = new Bitmap(pictureBox.Width, pictureBox.Height, PixelFormat.Format32bppArgb);
			pictureBox.BackgroundImage = new Bitmap(pictureBox.Width, pictureBox.Height, PixelFormat.Format32bppArgb);
			mG = Graphics.FromImage(pictureBox.Image);
			mG.SmoothingMode = SmoothingMode.None;
			mGb = Graphics.FromImage(pictureBox.BackgroundImage);
			mGb.SmoothingMode = SmoothingMode.None;

			mScrollCanvas = new byte[4 * pictureBox.Width * pictureBox.Height];

			if (enableScroll) {
				mGraphHeight = pictureBox.Height / 2;
				mScrollBottom = pictureBox.Height - mGraphHeight - KeyboardHeight - 1;
			} else {
				mGraphHeight = pictureBox.Height - KeyboardHeight;
				mScrollBottom = -1;
			}
			mScrollTop = KeyboardHeight - 1;
			mKeyboardBottom = mGraphHeight + KeyboardHeight - 1;
			mGraphWidth = pictureBox.Width - LabelWidth;
			mGraphLeft = LabelWidth - (float)mGraphWidth / BANK_COUNT;
			mKeyboardTop = mGraphHeight + 1;
		}

		public void DrawBackground(int noteCount = 0) {
			mGb.Clear(Color.Black);
			if (noteCount == 0) {
				LevelGauge();
				FreqGauge();
			} else {
				PianoRoll(noteCount);
				LevelGauge();
			}
			mGb.DrawLine(OCT_BORDER, LabelWidth, 0, LabelWidth, mPictureBox.Height);
			mGb.DrawLine(OCT_BORDER, LabelWidth, mGraphHeight, mPictureBox.Width, mGraphHeight);
			mGb.DrawLine(OCT_BORDER, LabelWidth, mKeyboardBottom, mPictureBox.Width, mKeyboardBottom);
			mPictureBox.BackgroundImage = mPictureBox.BackgroundImage;
		}

		public void Update(Spectrum.Spectrum spectrum) {
			Array.Copy(spectrum.DisplayData, mCurve, BANK_COUNT);
			Array.Copy(spectrum.DisplayData, BANK_COUNT, mThreshold, 0, BANK_COUNT);
			Array.Copy(spectrum.DisplayData, BANK_COUNT * 2, mPeak, 0, BANK_COUNT);

			var gain = EnableAutoGain || EnableNormalize ? 1 : Math.Pow(10, OffsetDb/20.0);

			mG.Clear(Color.Transparent);
			if (EnableAutoGain) {
				Level(spectrum.AutoGain);
			}
			if (EnableNormalize) {
				Level(spectrum.Max);
			}
			if (Forms.Settings.DisplayCurve) {
				Surface(mCurve, gain);
			}
			if (Forms.Settings.DisplayThreshold) {
				Curve(mThreshold, gain);
			}
			if (Forms.Settings.DisplayPeak) {
				Peak(mPeak, gain);
				HueScroll(mPeak, gain, Forms.Settings.ScrollSpeed);
			} else {
				HueScroll(mCurve, gain, Forms.Settings.ScrollSpeed);
			}
			mPictureBox.Image = mPictureBox.Image;
		}

		private void Curve(double[] arr, double gain) {
			var x0 = (float)LabelWidth;
			var y0 = LinearToY(arr[0] * gain, mGraphHeight);
			for (int x = 0; x < mGraphWidth; x++) {
				var ixD = (double)x * BANK_COUNT / mGraphWidth;
				var ixA = (int)ixD;
				var ixB = Math.Min(ixA + 1, BANK_COUNT - 1);
				var a2b = ixD - ixA;
				var val = (arr[ixA] * (1 - a2b) + arr[ixB] * a2b) * gain;
				var x1 = Math.Max(LabelWidth, x + mGraphLeft);
				var y1 = LinearToY(val, mGraphHeight);
				mG.DrawLine(THRESHOLD, x0, y0, x1, y1);
				x0 = x1;
				y0 = y1;
			}
		}

		private void Surface(double[] arr, double gain) {
			var minVal = Math.Pow(10, MinDb / 20.0);
			var dx = (float)mGraphWidth / BANK_COUNT;
			for (int i = 0; i < BANK_COUNT; i++) {
				var val = arr[i] * gain;
				if (val > minVal) {
					var x0 = (i - 0.5f) * dx;
					var x1 = (i + 0.5f) * dx;
					var y = LinearToY(val, mGraphHeight);
					x0 = Math.Max(LabelWidth - mGraphLeft, x0);
					var barWidth = x1 - x0;
					var barHeight = mGraphHeight - y;
					x0 += mGraphLeft;
					mG.FillRectangle(SURFACE, x0, y, barWidth, barHeight);
				}
			}
		}

		private void Peak(double[] arr, double gain) {
			var dx = (float)BANK_COUNT / mGraphWidth;
			for (int x = 0; x < mGraphWidth; x++) {
				var px = x + mGraphLeft;
				var ix = (int)(x * dx);
				var val = arr[ix] * gain;
				var py = LinearToY(val, mGraphHeight);
				mG.DrawLine(PEAK, px, mGraphHeight, px, py);
			}
		}

		private void HueScroll(double[] arr, double gain, int scrollSpeed) {
			var bmp = (Bitmap)mPictureBox.Image;
			var pix = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
			var stride = pix.Stride;
			var ofsKeyboardTop = stride * mKeyboardTop;
			var ofsScrollTop = ofsKeyboardTop + stride * mScrollTop;
			var left = (int)(mGraphLeft + 0.5);
			var dx = (double)BANK_COUNT / mGraphWidth;
			for (int x = 0; x < mGraphWidth; x++) {
				var ix = (int)(x * dx);
				var val = arr[ix] * gain;
				if (val < 1e-9) {
					val = 1e-9;
				}
				var db = 20 * Math.Log10(val);
				var v = (int)((1.0 - db / MinDb) * 1279);
				if (v < 0) {
					v = 0;
				}
				var a = (int)(v * 0.25);
				if (a > 255) {
					a = 255;
				}
				int r, g, b;
				switch (v / 256) {
				case 0:
					r = 0;
					g = 0;
					b = 255;
					break;
				case 1:
					r = 0;
					g = v - 256;
					b = 255;
					break;
				case 2:
					r = 0;
					g = 255;
					b = 255 - (v - 512);
					break;
				case 3:
					r = v - 768;
					g = 255;
					b = 0;
					break;
				case 4:
					r = 255;
					g = 255 - (v - 1024);
					b = 0;
					break;
				default:
					r = 255;
					g = 0;
					b = 0;
					break;
				}
				ix = ofsKeyboardTop + Math.Max(LabelWidth, x + left) * 4;
				mScrollCanvas[ix] = (byte)b;
				mScrollCanvas[ix + 1] = (byte)g;
				mScrollCanvas[ix + 2] = (byte)r;
				mScrollCanvas[ix + 3] = (byte)a;
			}
			for (int y = 1; y < mScrollTop; y++) {
				Buffer.BlockCopy(
					mScrollCanvas, ofsKeyboardTop,
					mScrollCanvas, ofsKeyboardTop + stride * y,
					stride
				);
			}
			if (scrollSpeed < mScrollBottom) {
				Buffer.BlockCopy(
					mScrollCanvas, ofsScrollTop,
					mScrollCanvas, ofsScrollTop + stride * scrollSpeed,
					stride * (mScrollBottom - scrollSpeed)
				);
				Buffer.BlockCopy(
					mScrollCanvas, ofsKeyboardTop,
					mScrollCanvas, ofsScrollTop,
					stride * scrollSpeed
				);
			}
			Marshal.Copy(
				mScrollCanvas, ofsKeyboardTop,
				pix.Scan0 + ofsKeyboardTop,
				stride * (KeyboardHeight + mScrollBottom)
			);
			bmp.UnlockBits(pix);
		}

		private void Level(double val) {
			var py = LinearToY(val, mGraphHeight);
			var barHeight = mGraphHeight - py;
			mG.FillRectangle(SURFACE, 0, py, LabelWidth, barHeight);
		}

		private void LevelGauge() {
			var dbOfs = EnableAutoGain || EnableNormalize ? 0 : -OffsetDb;
			var dbMin = MinDb + dbOfs;
			for (var db = dbOfs; dbMin <= db; --db) {
				var py = DbToY(db - dbOfs, mGraphHeight);
				switch (db % 6) {
				case 0:
					mGb.DrawLine(LEVEL_MAJOR, 0, py, mPictureBox.Width, py);
					break;
				default:
					mGb.DrawLine(LEVEL_MINOR, 0, py, mPictureBox.Width, py);
					break;
				}
			}
			var textSize = mGb.MeasureString("-12db", FONT);
			var textArea = new RectangleF(-2f, -FONT.Size, LabelWidth, textSize.Height);
			var textTop = (int)(textSize.Height * 0.5);
			var stringFormat = new StringFormat
			{
				Alignment = StringAlignment.Center
			};
			for (var db = dbOfs; dbMin <= db; --db) {
				if (db % 6 == 0) {
					var py = DbToY(db - dbOfs, mGraphHeight);
					if (py < textTop) {
						py = textTop;
					}
					mGb.TranslateTransform(0, py);
					mGb.DrawString($"{db}db", FONT, Brushes.Yellow, textArea, stringFormat);
					mGb.TranslateTransform(0, -py);
				}
			}
		}

		private void FreqGauge() {
			var shift = -1 - KeyShift * HALFTONE_DIV;
			var textWidth = mGb.MeasureString("100", FONT).Width;
			var textArea = new RectangleF(-textWidth * 0.5f, -1f, textWidth, KeyboardHeight);
			var stringFormat = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			for (var unit = 1; unit <= 10000; unit *= 10) {
				for (var i = 1; i < 10; i++) {
					var hz = i * unit;
					var bank = shift + Math.Log(hz / BASE_FREQ, 2.0) * OCT_DIV;
					if (bank < 0) {
						continue;
					}
					if (bank >= BANK_COUNT) {
						break;
					}
					var px = LabelWidth + (float)(mGraphWidth * bank / BANK_COUNT);
					if (i == 1) {
						mGb.DrawLine(FREQ_MAJOR, px, 0, px, mPictureBox.Height);
					} else {
						mGb.DrawLine(FREQ_MINOR, px, 0, px, mPictureBox.Height);
					}
					if (i == 1 || i == 5) {
						mGb.TranslateTransform(px, mGraphHeight);
						var label = hz < 1000 ? $"{hz}" : $"{hz * 0.001}k";
						mGb.DrawString(label, FONT, Brushes.LightGray, textArea, stringFormat);
						mGb.TranslateTransform(-px, -mGraphHeight);
					}
				}
			}
		}

		private void PianoRoll(int noteCount) {
			var keyDWidth = (double)mGraphWidth / noteCount;
			for (int n = 0; n < noteCount; n++) {
				var x0 = (float)(n * keyDWidth);
				var x1 = (float)((n + 1) * keyDWidth);
				var keyWidth = x1 - x0 + 1;
				var px = x0 + LabelWidth;
				var note = (n + KeyShift + 24) % 12;
				switch (note) {
				case 0:
					mGb.FillRectangle(WHITE_KEY.Brush, px, 0, keyWidth, mPictureBox.Height);
					mGb.DrawLine(OCT_BORDER, px, 0, px, mPictureBox.Height);
					break;
				case 2:
				case 4:
				case 7:
				case 9:
				case 11:
					mGb.FillRectangle(WHITE_KEY.Brush, px, 0, keyWidth, mPictureBox.Height);
					break;
				case 5:
					mGb.FillRectangle(WHITE_KEY.Brush, px, 0, keyWidth, mPictureBox.Height);
					mGb.DrawLine(KEY_BORDER, px, 0, px, mPictureBox.Height);
					break;
				default:
					mGb.FillRectangle(BLACK_KEY.Brush, px, 0, keyWidth, mPictureBox.Height);
					break;
				}
			}
			var textWidth = mGb.MeasureString("10", FONT).Width;
			var textArea = new RectangleF(-1f, -1f, textWidth, KeyboardHeight);
			var stringFormat = new StringFormat
			{
				Alignment = StringAlignment.Near,
				LineAlignment = StringAlignment.Center
			};
			for (int n = -12; n < noteCount + 12; n += 12) {
				var note = n - KeyShift;
				if (note < 0) {
					continue;
				}
				var x = (float)(note * keyDWidth);
				var px = x + LabelWidth;
				mGb.TranslateTransform(px, mGraphHeight);
				mGb.DrawString($"{n / 12}", FONT, Brushes.LightGray, textArea, stringFormat);
				mGb.TranslateTransform(-px, -mGraphHeight);
			}
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
	}
}
