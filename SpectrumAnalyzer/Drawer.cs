using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SpectrumAnalyzer.Forms;
using static Spectrum.Spectrum;

namespace SpectrumAnalyzer {
	class Drawer : IDisposable {
		private static readonly Font FONT = new Font("Meiryo UI", 11f);
		private static readonly Pen OCT_BORDER = new Pen(Color.FromArgb(95, 95, 71), 1.0f);
		private static readonly Pen KEY_BORDER = new Pen(Color.FromArgb(63, 63, 63), 1.0f);
		private static readonly Pen WHITE_KEY = new Pen(Color.FromArgb(31, 31, 31), 1.0f);
		private static readonly Pen BLACK_KEY = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
		private static readonly Brush SURFACE = new Pen(Color.FromArgb(111, 255, 255, 255)).Brush;
		private static readonly Brush AUTOGAIN = new Pen(Color.FromArgb(111, 0, 255, 255)).Brush;
		private static readonly Brush MAX = new Pen(Color.FromArgb(111, 255, 255, 255)).Brush;
		private static readonly Pen CURVE = new Pen(Color.FromArgb(0, 231, 0), 1.0f);
		private static readonly Pen THRESHOLD = new Pen(Color.FromArgb(255, 0, 0), 1.0f);
		private static readonly Pen PEAK = new Pen(Color.FromArgb(0, 221, 221), 1.0f);
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

		private readonly double[] mData = new double[BANK_COUNT * 4];
		private readonly double[] mPeakN = new double[BANK_COUNT];
		private readonly double[] mPeakW = new double[BANK_COUNT];
		private readonly double[] mCurve = new double[BANK_COUNT];
		private readonly double[] mThreshold = new double[BANK_COUNT];
		private readonly uint[] mHueValue = new uint[BANK_COUNT];
		private IntPtr mpScrollBuffer;
		private int mScrollBufferSize;
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

		private static IntPtr mpHueLUT = IntPtr.Zero;
		private const int HueMax = 1279;

		static unsafe Drawer() {
			if (IntPtr.Zero != mpHueLUT) {
				return;
			}
			mpHueLUT = Marshal.AllocHGlobal((HueMax + 1) * 4);
			var pHueLUT = (byte*)mpHueLUT;
			for (int v = 0; v <= HueMax; v++) {
				int r, g, b, a;
				a = (int)(v * 0.25);
				if (a > 255) {
					a = 255;
				}
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
				*pHueLUT++ = (byte)b;
				*pHueLUT++ = (byte)g;
				*pHueLUT++ = (byte)r;
				*pHueLUT++ = (byte)a;
			}
		}

		public void Dispose() {
			if (null != mG) {
				mG.Dispose();
				mG = null;
			}
			if (null != mGb) {
				mGb.Dispose();
				mGb = null;
			}
			if (IntPtr.Zero != mpScrollBuffer) {
				Marshal.FreeHGlobal(mpScrollBuffer);
				mpScrollBuffer = IntPtr.Zero;
			}
		}

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

			if (IntPtr.Zero != mpScrollBuffer) {
				Marshal.FreeHGlobal(mpScrollBuffer);
				mpScrollBuffer = IntPtr.Zero;
			}
			mScrollBufferSize = 4 * pictureBox.Width * pictureBox.Height;
			mpScrollBuffer = Marshal.AllocHGlobal(mScrollBufferSize);

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
			var gain = Settings.EnableAutoGain || Settings.EnableNormalize ? 1 : Math.Pow(10, OffsetDb/20.0);
			Array.Copy(spectrum.DisplayData, mData, mData.Length);
			for (int ix = 0; ix < BANK_COUNT; ++ix) {
				mCurve[ix] = 20 * Math.Log10(Math.Max(mData[ix] * gain, 1e-9));
				mThreshold[ix] = 20 * Math.Log10(Math.Max(mData[ix + BANK_COUNT] * gain, 1e-9));
				mPeakN[ix] = 20 * Math.Log10(Math.Max(mData[ix + BANK_COUNT * 2] * gain, 1e-9));
				mPeakW[ix] = 20 * Math.Log10(Math.Max(mData[ix + BANK_COUNT * 3] * gain, 1e-9));
			}
			mG.Clear(Color.Transparent);
			if (Settings.EnableAutoGain) {
				Level(spectrum.AutoGain, AUTOGAIN);
			}
			if (Settings.EnableNormalize) {
				Level(spectrum.Max, MAX);
			}
			if (Settings.DisplayPeak) {
				Peak(mPeakN, PEAK);
			}
			if (Settings.DisplayCurve) {
				if (Settings.DisplayThreshold) {
					Surface(mCurve, SURFACE);
				} else {
					Curve(mCurve, CURVE);
				}
			}
			if (Settings.DisplayThreshold) {
				Curve(mThreshold, THRESHOLD);
			}
			if (Settings.DisplayPeak) {
				HueScroll(mPeakW, Settings.ScrollSpeed);
			} else {
				HueScroll(mCurve, Settings.ScrollSpeed);
			}
			mPictureBox.Image = mPictureBox.Image;
		}

		private void Level(double linear, Brush color) {
			var db = 20 * Math.Log10(Math.Max(linear, 1e-9));
			var normal = Math.Min(Math.Max(db / MinDb, 0.0), 1.0);
			var py = (float)(normal * mGraphHeight);
			var barHeight = mGraphHeight - py;
			mG.FillRectangle(color, 0, py, LabelWidth, barHeight);
		}

		private void Curve(double[] arr, Pen color) {
			var x0 = (float)LabelWidth;
			var y0 = DbToY(arr[0], mGraphHeight);
			for (int x = 0; x < mGraphWidth; x++) {
				var ixD = (double)x * BANK_COUNT / mGraphWidth;
				var ixA = (int)ixD;
				var ixB = Math.Min(ixA + 1, BANK_COUNT - 1);
				var a2b = ixD - ixA;
				var val = arr[ixA] * (1 - a2b) + arr[ixB] * a2b;
				var x1 = Math.Max(LabelWidth, x + mGraphLeft);
				var y1 = DbToY(val, mGraphHeight);
				mG.DrawLine(color, x0, y0, x1, y1);
				x0 = x1;
				y0 = y1;
			}
		}

		private void Surface(double[] arr, Brush color) {
			var dx = (float)mGraphWidth / BANK_COUNT;
			for (int i = 0; i < BANK_COUNT; i++) {
				var val = arr[i];
				if (val > MinDb) {
					var x0 = (i - 0.5f) * dx;
					var x1 = (i + 0.5f) * dx;
					var y = DbToY(val, mGraphHeight);
					x0 = Math.Max(LabelWidth - mGraphLeft, x0);
					var barWidth = x1 - x0;
					var barHeight = mGraphHeight - y;
					x0 += mGraphLeft;
					mG.FillRectangle(color, x0, y, barWidth, barHeight);
				}
			}
		}

		private void Peak(double[] arr, Pen color) {
			var dx = (float)BANK_COUNT / mGraphWidth;
			for (int x = 0; x < mGraphWidth; x++) {
				var px = x + mGraphLeft;
				var ix = (int)(x * dx);
				var val = arr[ix];
				var py = DbToY(val, mGraphHeight);
				mG.DrawLine(color, px, mGraphHeight, px, py);
			}
		}

		private unsafe void HueScroll(double[] arr, int scrollSpeed) {
			var bmp = (Bitmap)mPictureBox.Image;
			var stride = bmp.Width*sizeof(uint);
			var ofsKeyboardTop = stride * mKeyboardTop;
			for (int ix = 0; ix < mHueValue.Length; ++ix) {
				var normal = Math.Min(Math.Max(arr[ix] / MinDb, 0.0), 1.0);
				var ixH = (int)((1.0 - normal) * HueMax);
				mHueValue[ix] = ((uint*)mpHueLUT)[ixH];
			}
			var left = (int)(mGraphLeft + 0.5);
			var dx = (double)BANK_COUNT / mGraphWidth;
			var pHueLine = (uint*)(mpScrollBuffer + ofsKeyboardTop);
			for (int x = 0; x < mGraphWidth; ++x) {
				var ix = (int)(x * dx);
				var ixH = Math.Max(LabelWidth, x + left);
				pHueLine[ixH] = mHueValue[ix];
			}
			for (int y = 1, ofsA = ofsKeyboardTop; y < mScrollTop; ++y, ofsA += stride) {
				var ofsB = ofsA + stride;
				Buffer.MemoryCopy(
					(byte*)mpScrollBuffer + ofsA,
					(byte*)mpScrollBuffer + ofsB,
					mScrollBufferSize - ofsB,
					stride
				);
			}
			if (scrollSpeed < mScrollBottom) {
				var scrollBytes = stride * scrollSpeed;
				var remainBytes = stride * (mScrollBottom - scrollSpeed);
				var ofsA = ofsKeyboardTop;
				var ofsB = ofsA + stride * mScrollTop;
				var ofsC = ofsB + scrollBytes;
				Buffer.MemoryCopy(
					(byte*)mpScrollBuffer + ofsB,
					(byte*)mpScrollBuffer + ofsC,
					mScrollBufferSize - ofsC,
					remainBytes
				);
				Buffer.MemoryCopy(
					(byte*)mpScrollBuffer + ofsA,
					(byte*)mpScrollBuffer + ofsB,
					mScrollBufferSize - ofsB,
					scrollBytes
				);
			}
			var pix = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
			Buffer.MemoryCopy(
				(byte*)mpScrollBuffer + ofsKeyboardTop,
				(byte*)pix.Scan0 + ofsKeyboardTop,
				mScrollBufferSize - ofsKeyboardTop,
				stride * (KeyboardHeight + mScrollBottom)
			);
			bmp.UnlockBits(pix);
		}

		private void LevelGauge() {
			var dbOfs = Settings.EnableAutoGain || Settings.EnableNormalize ? 0 : -OffsetDb;
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
					var bank = shift + Math.Log(hz / BaseFreq, 2.0) * OCT_DIV;
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
	}
}
