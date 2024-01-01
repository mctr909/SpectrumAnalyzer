using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using SignalProcess;
using static SpectrumAnalyzerNet.Forms.Settings;
using static SignalProcess.BaseSpectrum;

namespace SpectrumAnalyzerNet;

public class Drawer : IDisposable {
	private const int LabelWidth = 48;
	private const int KeyboardHeight = 20;
	private const double LimitMinAmp = 1e-6;
	private static readonly Font FONT = new("Consolas", 11f);
	private static readonly RectangleF LevelGaugeOffset = new(-2f, -9f, LabelWidth, 0f);

	public static int CanvasWidthMin => LabelWidth + BankCount;

	#region 色設定定数
	private const int HueMax = 1279;
	private static readonly uint[] HueLUT = new uint[HueMax + 1];
	private static readonly Pen POctBorder = new(Color.FromArgb(95, 95, 71), 1.0f);
	private static readonly Pen PKeyBorder = new(Color.FromArgb(63, 63, 63), 1.0f);
	private static readonly Pen PWhiteKey = new(Color.FromArgb(31, 31, 31), 1.0f);
	private static readonly Pen PBlackKey = new(Color.FromArgb(0, 0, 0), 1.0f);
	private static readonly Pen PLevelMajor = new(Color.FromArgb(81, 81, 0), 1.0f);
	private static readonly Pen PLevelMinor = new(Color.FromArgb(47, 47, 47), 1.0f);
	private static readonly Pen PFreqMajor = new(Color.FromArgb(91, 91, 91), 1.0f);
	private static readonly Pen PFreqMinor = new(Color.FromArgb(91, 91, 91), 1.0f)
	{
		DashStyle = DashStyle.Custom,
		DashPattern = [1, 3]
	};
	private static readonly Pen PCurve = new(Color.FromArgb(0, 241, 0), 1.0f);
	private static readonly Pen PThreshold = new(Color.FromArgb(255, 0, 0), 1.0f);
	private static readonly Pen PPeak = new(Color.FromArgb(0, 221, 221), 1.0f);
	private static readonly Brush BSurface = new Pen(Color.FromArgb(81, 167, 255, 167)).Brush;
	private static readonly Brush BAutogain = new Pen(Color.FromArgb(111, 0, 255, 255)).Brush;
	private static readonly Brush BMax = new Pen(Color.FromArgb(111, 255, 255, 255)).Brush;
	#endregion

	#region 変数
	private readonly double[] Data = new double[BankCount * 4];
	private readonly double[] Curve = new double[BankCount + 1];
	private readonly double[] Threshold = new double[BankCount + 1];
	private readonly double[] Peak = new double[BankCount + 1];
	private readonly double[] SmoothedPeak = new double[BankCount + 1];
	private readonly PointF[] GpPoints = new PointF[BankCount + 2];
	private readonly GraphicsPath Gp = new();
	private readonly PictureBox PicChart;
	private readonly PictureBox PicScroll;
	private uint[] HueMap;
	private int KeyboardBottom;
	private int GraphWidth;
	private int ChartHeight;
	private float GraphLeft;
	private double Max;
	private double SmoothedMax;
	private Graphics GChartF;
	private Graphics GChartB;
	private Graphics GScrollF;
	private Graphics GScrollB;
	#endregion

	static Drawer() {
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
			var hue = (uint)a;
			hue <<= 8;
			hue |= (uint)r;
			hue <<= 8;
			hue |= (uint)g;
			hue <<= 8;
			hue |= (uint)b;
			HueLUT[v] = hue;
		}
	}

	public Drawer(PictureBox picChart, PictureBox picScroll) {
		PicChart = picChart;
		PicScroll = picScroll;
		HueMap = new uint[PicScroll.Height * (PicScroll.Width + (4 - PicScroll.Width) % 4)];
		var dbMin = 20 * Math.Log10(LimitMinAmp);
		for (int i = 0; i<BankCount; i++) {
			Curve[i] = dbMin;
			Threshold[i] = dbMin;
			Peak[i] = dbMin;
			SmoothedPeak[i] = dbMin;
		}
	}

	public void Dispose() {
		GChartF?.Dispose();
		GChartB?.Dispose();
		GScrollF?.Dispose();
		GScrollB?.Dispose();
		PicChart.Image?.Dispose();
		PicChart.Image = null;
		PicChart.BackgroundImage?.Dispose();
		PicChart.BackgroundImage = null;
		PicScroll.Image?.Dispose();
		PicScroll.Image = null;
		PicScroll.BackgroundImage?.Dispose();
		PicScroll.BackgroundImage = null;
	}

	public void Resize() {
		Dispose();
		PicChart.Image = new Bitmap(PicChart.Width, PicChart.Height, PixelFormat.Format32bppArgb);
		PicChart.BackgroundImage = new Bitmap(PicChart.Width, PicChart.Height, PixelFormat.Format32bppArgb);
		PicScroll.Image = new Bitmap(PicScroll.Width, PicScroll.Height, PixelFormat.Format32bppArgb);
		PicScroll.BackgroundImage = new Bitmap(PicScroll.Width, PicScroll.Height, PixelFormat.Format32bppArgb);
		GChartF = Graphics.FromImage(PicChart.Image);
		GChartB = Graphics.FromImage(PicChart.BackgroundImage);
		GScrollF = Graphics.FromImage(PicScroll.Image);
		GScrollB = Graphics.FromImage(PicScroll.BackgroundImage);
		GChartF.SmoothingMode = SmoothingMode.None;
		GChartB.SmoothingMode = SmoothingMode.None;
		GScrollF.SmoothingMode = SmoothingMode.None;
		GScrollB.SmoothingMode = SmoothingMode.None;
		ChartHeight = PicChart.Height - KeyboardHeight;
		KeyboardBottom = ChartHeight + KeyboardHeight - 1;
		GraphWidth = PicChart.Width - LabelWidth;
		GraphLeft = LabelWidth - (float)GraphWidth / BankCount;
		HueMap = new uint[PicScroll.Height * (PicScroll.Width + (4 - PicScroll.Width) % 4)];
	}

	public void DrawBackground() {
		GChartB.Clear(Color.Black);
		GScrollB.Clear(Color.Black);
		if (DisplayFreq) {
			LevelGauge();
			FreqGauge();
		} else {
			PianoRoll(HALFTONE_COUNT);
			LevelGauge();
		}
		GChartB.DrawLine(POctBorder, LabelWidth, 0, LabelWidth, PicChart.Height);
		GChartB.DrawLine(POctBorder, LabelWidth, ChartHeight, PicChart.Width, ChartHeight);
		GChartB.DrawLine(POctBorder, LabelWidth, KeyboardBottom, PicChart.Width, KeyboardBottom);
		GScrollB.DrawLine(POctBorder, LabelWidth, 0, LabelWidth, PicScroll.Height);
		PicChart.BackgroundImage = PicChart.BackgroundImage;
		PicScroll.BackgroundImage = PicScroll.BackgroundImage;
	}

	public void Draw(BaseSpectrum spectrum) {
		Array.Copy(spectrum.DisplayData, Data, BankCount * 3);
		Max = spectrum.Max;
		SmoothedMax = spectrum.SmoothedMax;
		double gain;
		if (EnableNormalize) {
			gain = 1.0 / Max;
		} else if (EnableAutoGain) {
			gain = 1.0 / SmoothedMax;
		} else {
			gain = Math.Pow(10, -DisplayMaxDb / 20.0);
		}
		for (int i = 0; i < BankCount; ++i) {
			Curve[i] = 20 * Math.Log10(Math.Max(Data[i] * gain, LimitMinAmp));
			Threshold[i] = 20 * Math.Log10(Math.Max(Data[i + BankCount] * gain, LimitMinAmp));
			Peak[i] = 20 * Math.Log10(Math.Max(Data[i + BankCount * 2] * gain, LimitMinAmp));
		}
		Array.Copy(Peak, SmoothedPeak, BankCount);
		Interp(SmoothedPeak, BankCount);
		Curve[BankCount] = Curve[BankCount - 1];
		Threshold[BankCount] = Threshold[BankCount - 1];
		Peak[BankCount] = Peak[BankCount - 1];
		SmoothedPeak[BankCount] = SmoothedPeak[BankCount - 1];
		Draw();
	}

	public void Draw() {
		GChartF.Clear(Color.Transparent);
		GScrollF.Clear(Color.Transparent);
		if (EnableAutoGain) {
			DrawLevel(SmoothedMax, BAutogain);
		}
		if (EnableNormalize) {
			DrawLevel(Max, BMax);
		}
		if (DisplayPeak) {
			DrawPeak(Peak, PPeak);
		}
		if (DisplayCurve) {
			DrawCurve(Curve, PCurve);
		} else {
			DrawSurface(Curve, BSurface);
		}
		if (DisplayThreshold) {
			DrawCurve(Threshold, PThreshold);
		}
		if (DisplayPeak) {
			ScrollHue(SmoothedPeak);
		} else {
			ScrollHue(Curve);
		}
		PicChart.Image = PicChart.Image;
		PicScroll.Image = PicScroll.Image;
	}

	private void DrawLevel(double linear, Brush color) {
		var db = 20 * Math.Log10(Math.Max(linear, LimitMinAmp));
		var normal = Math.Min(Math.Max(db / DisplayRangeDb, 0.0), 1.0);
		var py = (float)(normal * ChartHeight);
		var barHeight = ChartHeight - py;
		GChartF.FillRectangle(color, 0, py, LabelWidth, barHeight);
	}

	private void DrawPeak(double[] values, Pen color) {
		var dx = (float)GraphWidth / BankCount;
		for (int ix = 0; ix < BankCount; ix++) {
			var val = values[ix];
			if (val > DisplayRangeDb) {
				var px = ix * dx + GraphLeft;
				var py = DbToY(val);
				GChartF.DrawLine(color, px, ChartHeight, px, py);
			}
		}
	}

	private void DrawCurve(double[] values, Pen color) {
		Gp.Reset();
		var x0 = (float)LabelWidth;
		var y0 = DbToY(values[0]);
		var dx = (double)BankCount / GraphWidth;
		for (int x = 0; x < GraphWidth; x++) {
			var ixD = x * dx;
			var ixI = (int)ixD;
			var a2b = ixD - ixI;
			var val = values[ixI] * (1.0 - a2b) + values[ixI+1] * a2b;
			var x1 = Math.Max(x + GraphLeft, LabelWidth);
			var y1 = DbToY(val);
			Gp.AddLine(x0, y0, x1, y1);
			x0 = x1;
			y0 = y1;
		}
		GChartF.DrawPath(color, Gp);
	}

	private void DrawSurface(double[] values, Brush color) {
		var dx = (float)GraphWidth / BankCount;
		for (int ix = 0; ix < BankCount; ix++) {
			GpPoints[ix].X = GraphLeft + ix * dx;
			GpPoints[ix].Y = DbToY(values[ix]);
		}
		GpPoints[BankCount].X = GraphLeft + GraphWidth;
		GpPoints[BankCount].Y = ChartHeight;
		GpPoints[BankCount + 1].X = GraphLeft;
		GpPoints[BankCount + 1].Y = ChartHeight;
		Gp.Reset();
		Gp.AddLines(GpPoints);
		GChartF.FillPath(color, Gp);
	}

	private void ScrollHue(double[] values) {
	}

	private void LevelGauge() {
		var dbMax = EnableAutoGain || EnableNormalize ? 0 : DisplayMaxDb;
		var dbMin = dbMax + DisplayRangeDb;
		var dbRange = dbMax - dbMin;
		if (ChartHeight >= dbRange * 4) {
			draw(6, 1);
		} else if(ChartHeight >= dbRange * 2) {
			draw(6, 2);
		} else if (ChartHeight >= dbRange) {
			draw(6, 4);
		} else {
			draw(6, 8);
		}
		void draw(int unit, int steps) {
			for (var db = dbMax; db >= dbMin; --db) {
				if (db % (unit*steps) == 0) {
					var py = DbToY(db - dbMax);
					GChartB.DrawLine(PLevelMajor, 0, py, PicChart.Width, py);
				} else if (db % steps == 0) {
					var py = DbToY(db - dbMax);
					GChartB.DrawLine(PLevelMinor, 0, py, PicChart.Width, py);
				}
			}
			var textSize = GChartB.MeasureString("-12dB", FONT);
			var textArea = LevelGaugeOffset;
			textArea.Height = textSize.Height;
			var textTop = (int)(textSize.Height * 0.5);
			var stringFormat = new StringFormat
			{
				Alignment = StringAlignment.Center
			};
			for (var db = dbMax; db >= dbMin; --db) {
				if (db % (unit*steps) == 0) {
					var py = DbToY(db - dbMax);
					if (py < textTop) {
						py = textTop;
					}
					GChartB.TranslateTransform(0, py);
					GChartB.DrawString($"{db}dB", FONT, Brushes.Yellow, textArea, stringFormat);
					GChartB.TranslateTransform(0, -py);
				}
			}
		}
	}

	private void FreqGauge() {
		var shift = -1 - KeyShift * HALFTONE_DIV;
		var textWidth = GChartB.MeasureString("100", FONT).Width;
		var textArea = new RectangleF(-textWidth * 0.5f, 2f, textWidth, KeyboardHeight);
		var stringFormat = new StringFormat
		{
			Alignment = StringAlignment.Center,
			LineAlignment = StringAlignment.Center
		};
		for (var unit = 1; unit <= 10000; unit *= 10) {
			for (var i = 1; i < 10; i++) {
				var hz = i * unit;
				var bank = shift + Math.Log(hz / MinFreq, 2.0) * OCT_DIV;
				if (bank < 0) {
					continue;
				}
				if (bank >= BankCount) {
					break;
				}
				var px = LabelWidth + (float)(GraphWidth * bank / BankCount);
				if (i == 1) {
					GChartB.DrawLine(PFreqMajor, px, 0, px, PicChart.Height);
					GScrollB.DrawLine(PFreqMajor, px, 0, px, PicScroll.Height);
				} else {
					GChartB.DrawLine(PFreqMinor, px, 0, px, PicChart.Height);
					GScrollB.DrawLine(PFreqMinor, px, 0, px, PicScroll.Height);
				}
				if (i == 1 || i == 5) {
					GChartB.TranslateTransform(px, ChartHeight);
					var label = hz < 1000 ? $"{hz}" : $"{hz * 0.001}k";
					GChartB.DrawString(label, FONT, Brushes.LightGray, textArea, stringFormat);
					GChartB.TranslateTransform(-px, -ChartHeight);
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
				GChartB.FillRectangle(PWhiteKey.Brush, px, 0, keyWidth, PicChart.Height);
				GChartB.DrawLine(POctBorder, px, 0, px, PicChart.Height);
				break;
			case 2:
			case 4:
			case 7:
			case 9:
			case 11:
				GChartB.FillRectangle(PWhiteKey.Brush, px, 0, keyWidth, PicChart.Height);
				break;
			case 5:
				GChartB.FillRectangle(PWhiteKey.Brush, px, 0, keyWidth, PicChart.Height);
				GChartB.DrawLine(PKeyBorder, px, 0, px, PicChart.Height);
				break;
			default:
				GChartB.FillRectangle(PBlackKey.Brush, px, 0, keyWidth, PicChart.Height);
				break;
			}
		}
		var textWidth = GChartB.MeasureString("10", FONT).Width;
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
			GChartB.TranslateTransform(px, ChartHeight);
			GChartB.DrawString($"{n / 12}", FONT, Brushes.LightGray, textArea, stringFormat);
			GChartB.TranslateTransform(-px, -ChartHeight);
		}
	}

	private float DbToY(double db) {
		return (float)(Math.Max(db, DisplayRangeDb) * ChartHeight / DisplayRangeDb);
	}

	private static void Interp(double[] dbArray, int dataLen, double dbMin = -100, double dbRange = 100, double smooth = 0.5) {
		double curent = 0;
		double delta;
		var slope = (int)(smooth * dataLen);
		int prevI = 0;
		for (int i = 0; i < dataLen; i++) {
			var db = dbArray[i];
			if (db < dbMin) {
				dbArray[i] = dbMin;
				continue;
			}
			if (prevI == 0) {
				prevI = Math.Max(0, i - slope / 2);
			}
			var len = i - prevI + 1;
			var halfLen = len / 2 + 1;
			var weight = 0.5 - Math.Min(0.5, (double)len / slope);
			var target = (db - dbMin) / dbRange;
			var middle = weight * (target + curent);
			var middleI = prevI + halfLen;
			delta = (middle - curent) / halfLen;
			for (int j = prevI; j < middleI; j++) {
				dbArray[j] = curent * dbRange + dbMin;
				curent += delta;
			}
			delta = (target - curent) / halfLen;
			for (int j = middleI; j < i; j++) {
				dbArray[j] = curent * dbRange + dbMin;
				curent += delta;
			}
			prevI = i;
		}
		delta = -4 * curent / slope;
		for (int i = prevI; i < dbArray.Length; i++) {
			dbArray[i] = curent * dbRange + dbMin;
			curent += delta;
			curent = Math.Max(0, curent);
		}
	}
}
