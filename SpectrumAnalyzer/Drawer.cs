using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SignalProcess;

namespace SpectrumAnalyzer {
	class Drawer : IDisposable {
		private const int LabelWidth = 48;
		private const int KeyboardHeight = 24;
		private const double LimitMinAmp = 1e-6;
		private static readonly Font FONT = new Font("Consolas", 11f);
		private static readonly RectangleF LevelGaugeOffset = new RectangleF(-2f, -9f, LabelWidth, 0f);

		private static readonly IntPtr HueLUT;
		private const int HueMax = 1279;

		#region 色設定定数
		private static readonly Pen POctBorder = new Pen(Color.FromArgb(95, 95, 71), 1.0f);
		private static readonly Pen PKeyBorder = new Pen(Color.FromArgb(63, 63, 63), 1.0f);
		private static readonly Pen PWhiteKey = new Pen(Color.FromArgb(31, 31, 31), 1.0f);
		private static readonly Pen PBlackKey = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
		private static readonly Pen PLevelMajor = new Pen(Color.FromArgb(81, 81, 0), 1.0f);
		private static readonly Pen PLevelMinor = new Pen(Color.FromArgb(47, 47, 47), 1.0f);
		private static readonly Pen PFreqMajor = new Pen(Color.FromArgb(91, 91, 91), 1.0f);
		private static readonly Pen PFreqMinor = new Pen(Color.FromArgb(91, 91, 91), 1.0f)
		{
			DashStyle = DashStyle.Custom,
			DashPattern = new float[] { 1, 3 }
		};
		private static readonly Pen PCurve = new Pen(Color.FromArgb(0, 231, 0), 1.0f);
		private static readonly Pen PThreshold = new Pen(Color.FromArgb(255, 0, 0), 1.0f);
		private static readonly Pen PPeak = new Pen(Color.FromArgb(0, 231, 231), 1.0f);
		private static readonly Brush BSurface = new Pen(Color.FromArgb(81, 191, 255, 191)).Brush;
		private static readonly Brush BAutogain = new Pen(Color.FromArgb(111, 0, 255, 255)).Brush;
		private static readonly Brush BMax = new Pen(Color.FromArgb(111, 255, 255, 255)).Brush;
		#endregion

		#region 公開設定値
		public static int CanpasWidthMin => LabelWidth + Spectrum.BANK_COUNT;
		public static bool EnablePeak { get; set; } = true;
		public static bool EnableAutoGain { get; set; } = true;
		public static bool EnableNormalize { get; set; } = false;
		public static bool DisplayCurve { get; set; } = true;
		public static bool DisplayPeak { get; set; } = true;
		public static bool DisplayThreshold { get; set; } = false;
		public static bool DisplayFreq { get; set; } = true;
		public static bool DisplayScroll { get; set; } = false;
		public static int ScrollSpeed { get; set; } = 2;
		public static int DisplayRangeDb { get; set; } = -36;
		public static int DisplayMaxDb { get; set; } = -12;
		public static int KeyShift { get; set; } = 0;
		#endregion

		private readonly double[] Data = new double[Spectrum.BANK_COUNT * 3];
		private readonly double[] Curve = new double[Spectrum.BANK_COUNT + 1];
		private readonly double[] Threshold = new double[Spectrum.BANK_COUNT + 1];
		private readonly double[] Peak = new double[Spectrum.BANK_COUNT + 1];
		private readonly double[] PeakWide = new double[Spectrum.BANK_COUNT + 1];
		private readonly uint[] HueValue = new uint[Spectrum.BANK_COUNT];
		private readonly PointF[] GpPoints = new PointF[Spectrum.BANK_COUNT + 2];
		private readonly GraphicsPath Gp = new GraphicsPath();
		private readonly PictureBox PictureBox;
		private IntPtr ScrollBufferPtr;
		private int ScrollBufferSize;
		private int KeyboardBottom;
		private int GraphWidth;
		private int GraphHeight;
		private float GraphLeft;
		private int KeyboardTop;
		private int ScrollTop;
		private int ScrollBottom;
		private Graphics Gf;
		private Graphics Gb;

		static unsafe Drawer() {
			if (IntPtr.Zero != HueLUT) {
				return;
			}
			HueLUT = Marshal.AllocHGlobal((HueMax + 1) * 4);
			var pHueLUT = (byte*)HueLUT;
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

		public Drawer(PictureBox pictureBox) {
			PictureBox = pictureBox;
		}

		public void Dispose() {
			Gf?.Dispose();
			Gf = null;
			Gb?.Dispose();
			Gb = null;
			PictureBox.Image?.Dispose();
			PictureBox.Image = null;
			PictureBox.BackgroundImage?.Dispose();
			PictureBox.BackgroundImage = null;
			if (IntPtr.Zero != ScrollBufferPtr) {
				Marshal.FreeHGlobal(ScrollBufferPtr);
				ScrollBufferPtr = IntPtr.Zero;
			}
		}

		public void Resize() {
			Dispose();
			PictureBox.Image = new Bitmap(PictureBox.Width, PictureBox.Height, PixelFormat.Format32bppArgb);
			PictureBox.BackgroundImage = new Bitmap(PictureBox.Width, PictureBox.Height, PixelFormat.Format32bppArgb);
			Gf = Graphics.FromImage(PictureBox.Image);
			Gf.SmoothingMode = SmoothingMode.None;
			Gb = Graphics.FromImage(PictureBox.BackgroundImage);
			Gb.SmoothingMode = SmoothingMode.None;
			ScrollBufferSize = 4 * PictureBox.Width * PictureBox.Height;
			ScrollBufferPtr = Marshal.AllocHGlobal(ScrollBufferSize);
			if (DisplayScroll) {
				GraphHeight = PictureBox.Height / 2;
				ScrollBottom = PictureBox.Height - GraphHeight - KeyboardHeight - 1;
			} else {
				GraphHeight = PictureBox.Height - KeyboardHeight;
				ScrollBottom = -1;
			}
			ScrollTop = KeyboardHeight - 1;
			KeyboardBottom = GraphHeight + KeyboardHeight - 1;
			GraphWidth = PictureBox.Width - LabelWidth;
			GraphLeft = LabelWidth - (float)GraphWidth / Spectrum.BANK_COUNT;
			KeyboardTop = GraphHeight + 1;
		}

		public void DrawBackground() {
			Gb.Clear(Color.Black);
			if (DisplayFreq) {
				LevelGauge();
				FreqGauge();
			} else {
				PianoRoll(Spectrum.HALFTONE_COUNT);
				LevelGauge();
			}
			Gb.DrawLine(POctBorder, LabelWidth, 0, LabelWidth, PictureBox.Height);
			Gb.DrawLine(POctBorder, LabelWidth, GraphHeight, PictureBox.Width, GraphHeight);
			Gb.DrawLine(POctBorder, LabelWidth, KeyboardBottom, PictureBox.Width, KeyboardBottom);
			PictureBox.BackgroundImage = PictureBox.BackgroundImage;
		}

		public void Update(Spectrum spectrum) {
			Array.Copy(spectrum.DisplayData, Data, Spectrum.BANK_COUNT * 3);
			double gain;
			if (EnableNormalize) {
				gain = 1.0 / spectrum.Max;
			} else if (EnableAutoGain) {
				gain = 1.0 / spectrum.AutoGain;
			} else {
				gain = Math.Pow(10, -DisplayMaxDb / 20.0);
			}
			for (int ix = 0; ix < Spectrum.BANK_COUNT; ++ix) {
				Curve[ix] = 20 * Math.Log10(Math.Max(Data[ix] * gain, LimitMinAmp));
				Threshold[ix] = 20 * Math.Log10(Math.Max(Data[ix + Spectrum.BANK_COUNT] * gain, LimitMinAmp));
				Peak[ix] = 20 * Math.Log10(Math.Max(Data[ix + Spectrum.BANK_COUNT * 2] * gain, LimitMinAmp));
			}
			Curve[Spectrum.BANK_COUNT] = Curve[Spectrum.BANK_COUNT - 1];
			Threshold[Spectrum.BANK_COUNT] = Threshold[Spectrum.BANK_COUNT - 1];
			Peak[Spectrum.BANK_COUNT] = Peak[Spectrum.BANK_COUNT - 1];
			for (int ix = 0; ix < Spectrum.BANK_COUNT; ++ix) {
				var ixStart = Math.Max(ix - 1, 0);
				var ixEnd = Math.Min(ix + 1, Spectrum.BANK_COUNT - 1);
				var amp = Peak[ixStart];
				amp = Math.Max(Peak[ix], amp);
				amp = Math.Max(Peak[ixEnd], amp);
				PeakWide[ix] = amp;
			}
			PeakWide[Spectrum.BANK_COUNT] = PeakWide[Spectrum.BANK_COUNT - 1];
			Gf.Clear(Color.Transparent);
			if (EnableAutoGain) {
				DrawLevel(spectrum.AutoGain, BAutogain);
			}
			if (EnableNormalize) {
				DrawLevel(spectrum.Max, BMax);
			}
			if (DisplayPeak) {
				DrawPeak(Peak, PPeak);
			}
			if (DisplayCurve) {
				if (DisplayThreshold) {
					DrawSurface(Curve, BSurface);
				} else {
					DrawCurve(Curve, PCurve);
				}
			}
			if (DisplayThreshold) {
				DrawCurve(Threshold, PThreshold);
			}
			if (EnablePeak) {
				ScrollHue(PeakWide);
			} else {
				ScrollHue(Curve);
			}
			PictureBox.Image = PictureBox.Image;
		}

		private void DrawLevel(double linear, Brush color) {
			var db = 20 * Math.Log10(Math.Max(linear, LimitMinAmp));
			var normal = Math.Min(Math.Max(db / DisplayRangeDb, 0.0), 1.0);
			var py = (float)(normal * GraphHeight);
			var barHeight = GraphHeight - py;
			Gf.FillRectangle(color, 0, py, LabelWidth, barHeight);
		}

		private void DrawPeak(double[] arr, Pen color) {
			var dx = (float)GraphWidth / Spectrum.BANK_COUNT;
			for (int ix = 0; ix < Spectrum.BANK_COUNT; ix++) {
				var val = arr[ix];
				if (val > DisplayRangeDb) {
					var px = ix * dx + GraphLeft;
					var py = DbToY(val);
					Gf.DrawLine(color, px, GraphHeight, px, py);
				}
			}
		}

		private void DrawCurve(double[] arr, Pen color) {
			Gp.Reset();
			var x0 = (float)LabelWidth;
			var y0 = DbToY(arr[0]);
			var dx = (double)Spectrum.BANK_COUNT / GraphWidth;
			for (int x = 0; x < GraphWidth; x++) {
				var ixD = x * dx;
				var ixI = (int)ixD;
				var a2b = ixD - ixI;
				var val = arr[ixI] * (1.0 - a2b) + arr[ixI+1] * a2b;
				var x1 = Math.Max(x + GraphLeft, LabelWidth);
				var y1 = DbToY(val);
				Gp.AddLine(x0, y0, x1, y1);
				x0 = x1;
				y0 = y1;
			}
			Gf.DrawPath(color, Gp);
		}

		private void DrawSurface(double[] arr, Brush color) {
			var dx = (float)GraphWidth / Spectrum.BANK_COUNT;
			for (int ix = 0; ix < Spectrum.BANK_COUNT; ix++) {
				GpPoints[ix].X = GraphLeft + ix * dx;
				GpPoints[ix].Y = DbToY(arr[ix]);
			}
			GpPoints[Spectrum.BANK_COUNT].X = GraphLeft + GraphWidth;
			GpPoints[Spectrum.BANK_COUNT].Y = GraphHeight;
			GpPoints[Spectrum.BANK_COUNT + 1].X = GraphLeft;
			GpPoints[Spectrum.BANK_COUNT + 1].Y = GraphHeight;
			Gp.Reset();
			Gp.AddLines(GpPoints);
			Gf.FillPath(color, Gp);
		}

		private unsafe void ScrollHue(double[] arr) {
			var bmp = (Bitmap)PictureBox.Image;
			var stride = bmp.Width * sizeof(uint);
			var ofsKeyboardTop = stride * KeyboardTop;
			for (int ix = 0; ix < HueValue.Length; ++ix) {
				var normal = Math.Min(Math.Max(arr[ix] / DisplayRangeDb, 0.0), 1.0);
				var ixH = (int)((1.0 - normal) * HueMax);
				HueValue[ix] = ((uint*)HueLUT)[ixH];
			}
			var left = (int)GraphLeft;
			var dx = (double)Spectrum.BANK_COUNT / GraphWidth;
			var pHueLine = (uint*)(ScrollBufferPtr + ofsKeyboardTop);
			for (int px = 0; px < GraphWidth; ++px) {
				var ix = (int)(px * dx);
				var ixH = Math.Max(px + left, LabelWidth);
				pHueLine[ixH] = HueValue[ix];
			}
			for (int y = 1, ofsA = ofsKeyboardTop; y < ScrollTop; ++y, ofsA += stride) {
				var ofsB = ofsA + stride;
				Buffer.MemoryCopy(
					(byte*)ScrollBufferPtr + ofsA,
					(byte*)ScrollBufferPtr + ofsB,
					ScrollBufferSize - ofsB,
					stride
				);
			}
			if (ScrollSpeed < ScrollBottom) {
				var scrollBytes = stride * ScrollSpeed;
				var remainBytes = stride * (ScrollBottom - ScrollSpeed);
				var ofsA = ofsKeyboardTop;
				var ofsB = ofsA + stride * ScrollTop;
				var ofsC = ofsB + scrollBytes;
				Buffer.MemoryCopy(
					(byte*)ScrollBufferPtr + ofsB,
					(byte*)ScrollBufferPtr + ofsC,
					ScrollBufferSize - ofsC,
					remainBytes
				);
				Buffer.MemoryCopy(
					(byte*)ScrollBufferPtr + ofsA,
					(byte*)ScrollBufferPtr + ofsB,
					ScrollBufferSize - ofsB,
					scrollBytes
				);
			}
			var pix = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
			Buffer.MemoryCopy(
				(byte*)ScrollBufferPtr + ofsKeyboardTop,
				(byte*)pix.Scan0 + ofsKeyboardTop,
				ScrollBufferSize - ofsKeyboardTop,
				stride * (KeyboardHeight + ScrollBottom)
			);
			bmp.UnlockBits(pix);
		}

		private void LevelGauge() {
			var dbMax = EnableAutoGain || EnableNormalize ? 0 : DisplayMaxDb;
			var dbMin = dbMax + DisplayRangeDb;
			for (var db = dbMax; db >= dbMin; --db) {
				var py = DbToY(db - dbMax);
				switch (db % 6) {
				case 0:
					Gb.DrawLine(PLevelMajor, 0, py, PictureBox.Width, py);
					break;
				default:
					Gb.DrawLine(PLevelMinor, 0, py, PictureBox.Width, py);
					break;
				}
			}
			var textSize = Gb.MeasureString("-12db", FONT);
			var textArea = LevelGaugeOffset;
			textArea.Height = textSize.Height;
			var textTop = (int)(textSize.Height * 0.5);
			var stringFormat = new StringFormat
			{
				Alignment = StringAlignment.Center
			};
			for (var db = dbMax; db >= dbMin; --db) {
				if (db % 6 == 0) {
					var py = DbToY(db - dbMax);
					if (py < textTop) {
						py = textTop;
					}
					Gb.TranslateTransform(0, py);
					Gb.DrawString($"{db}db", FONT, Brushes.Yellow, textArea, stringFormat);
					Gb.TranslateTransform(0, -py);
				}
			}
		}

		private void FreqGauge() {
			var shift = -1 - KeyShift * Spectrum.HALFTONE_DIV;
			var textWidth = Gb.MeasureString("100", FONT).Width;
			var textArea = new RectangleF(-textWidth * 0.5f, 2f, textWidth, KeyboardHeight);
			var stringFormat = new StringFormat
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			for (var unit = 1; unit <= 10000; unit *= 10) {
				for (var i = 1; i < 10; i++) {
					var hz = i * unit;
					var bank = shift + Math.Log(hz / Spectrum.BASE_FREQ, 2.0) * Spectrum.OCT_DIV;
					if (bank < 0) {
						continue;
					}
					if (bank >= Spectrum.BANK_COUNT) {
						break;
					}
					var px = LabelWidth + (float)(GraphWidth * bank / Spectrum.BANK_COUNT);
					if (i == 1) {
						Gb.DrawLine(PFreqMajor, px, 0, px, PictureBox.Height);
					} else {
						Gb.DrawLine(PFreqMinor, px, 0, px, PictureBox.Height);
					}
					if (i == 1 || i == 5) {
						Gb.TranslateTransform(px, GraphHeight);
						var label = hz < 1000 ? $"{hz}" : $"{hz * 0.001}k";
						Gb.DrawString(label, FONT, Brushes.LightGray, textArea, stringFormat);
						Gb.TranslateTransform(-px, -GraphHeight);
					}
				}
			}
		}

		private void PianoRoll(int noteCount) {
			var keyDWidth = (double)GraphWidth / noteCount;
			for (int n = 0; n < noteCount; n++) {
				var x0 = (float)(n * keyDWidth);
				var x1 = (float)((n + 1) * keyDWidth);
				var keyWidth = x1 - x0 + 1;
				var px = x0 + LabelWidth;
				var note = (n + KeyShift + 24) % 12;
				switch (note) {
				case 0:
					Gb.FillRectangle(PWhiteKey.Brush, px, 0, keyWidth, PictureBox.Height);
					Gb.DrawLine(POctBorder, px, 0, px, PictureBox.Height);
					break;
				case 2:
				case 4:
				case 7:
				case 9:
				case 11:
					Gb.FillRectangle(PWhiteKey.Brush, px, 0, keyWidth, PictureBox.Height);
					break;
				case 5:
					Gb.FillRectangle(PWhiteKey.Brush, px, 0, keyWidth, PictureBox.Height);
					Gb.DrawLine(PKeyBorder, px, 0, px, PictureBox.Height);
					break;
				default:
					Gb.FillRectangle(PBlackKey.Brush, px, 0, keyWidth, PictureBox.Height);
					break;
				}
			}
			var textWidth = Gb.MeasureString("10", FONT).Width;
			var textArea = new RectangleF(-2f, 2f, textWidth, KeyboardHeight);
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
				Gb.TranslateTransform(px, GraphHeight);
				Gb.DrawString($"{n / 12}", FONT, Brushes.LightGray, textArea, stringFormat);
				Gb.TranslateTransform(-px, -GraphHeight);
			}
		}

		private float DbToY(double db) {
			return (float)(Math.Max(db, DisplayRangeDb) * GraphHeight / DisplayRangeDb);
		}
	}
}
