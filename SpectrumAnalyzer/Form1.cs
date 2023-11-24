using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SpectrumAnalyzer {
	public partial class Form1 : Form {
		const int RANGE_DB = -30;
		const int NOTE_COUNT = 120;
		const int KEYBOARD_HEIGHT = 34;
		const int SCROLL_SPEED = 2;

		readonly double BASE_FREQ = 13.75 * Math.Pow(2.0, (3 - 1 / 3.0) / 12.0);

		readonly Font FONT = new Font("Meiryo UI", 8.0f);
		readonly Pen KEYBOARD_BORDER = new Pen(Color.FromArgb(95, 95, 95), 1.0f);
		readonly Pen WHITE_KEY = new Pen(Color.FromArgb(0, 0, 0), 1.0f);
		readonly Pen BLACK_KEY = new Pen(Color.FromArgb(31, 31, 31), 1.0f);
		readonly Pen BAR = new Pen(Color.FromArgb(0, 95, 0), 1.0f);
		readonly Pen GRID_MAJOR = new Pen(Color.FromArgb(95, 95, 0), 1.0f);
		readonly Pen GRID_MINOR1 = new Pen(Color.FromArgb(63, 63, 0), 1.0f);
		readonly Pen GRID_MINOR2 = new Pen(Color.FromArgb(47, 47, 47), 1.0f);

		Playback mWaveOut;
		Record mWaveIn;
		byte[] mPix;
		bool mIsScrol;
		bool mSetLayout = true;

		public Form1() {
			InitializeComponent();
			mWaveOut = new Playback(NOTE_COUNT, BASE_FREQ);
			mWaveIn = new Record(44100, 256, NOTE_COUNT, BASE_FREQ);
		}

		private void Form1_Load(object sender, EventArgs e) {
			timer1.Interval = 1;
			timer1.Enabled = true;
			timer1.Start();
		}

		private void Form1_Resize(object sender, EventArgs e) {
			mSetLayout = true;
		}

		private void BtnFileOpen_Click(object sender, EventArgs e) {
			mWaveOut.Enabled = false;
			mWaveOut.Position = 0;
			BtnPlayStop.Text = "再生";

			openFileDialog1.FileName = "";
			openFileDialog1.Filter = "PCMファイル(*.wav)|*.wav";
			openFileDialog1.ShowDialog();
			var filePath = openFileDialog1.FileName;
			if (!File.Exists(filePath)) {
				return;
			}
			mWaveOut.SetValue(filePath);
			TrkSeek.Minimum = 0;
			TrkSeek.Maximum = mWaveOut.Length / mWaveOut.SampleRate;
			TrkSeek.Value = 0;
		}

		private void BtnPlayStop_Click(object sender, EventArgs e) {
			if ("再生" == BtnPlayStop.Text) {
				mWaveIn.Enabled = false;
				BtnRec.Text = "録音";
				mWaveOut.Enabled = true;
				BtnPlayStop.Text = "停止";
			} else {
				mWaveOut.Enabled = false;
				BtnPlayStop.Text = "再生";
			}
		}

		private void BtnRec_Click(object sender, EventArgs e) {
			if ("録音" == BtnRec.Text) {
				mWaveOut.Enabled = false;
				BtnPlayStop.Text = "再生";
				mWaveIn.Enabled = true;
				BtnRec.Text = "停止";
			} else {
				mWaveIn.Enabled = false;
				BtnRec.Text = "録音";
			}
		}

		private void TrkSeek_MouseDown(object sender, MouseEventArgs e) {
			mIsScrol = true;
		}

		private void TrkSeek_MouseUp(object sender, EventArgs e) {
			mWaveOut.Position = TrkSeek.Value * mWaveOut.SampleRate;
			mIsScrol = false;
		}

		private void TrkKey_Scroll(object sender, EventArgs e) {
			mWaveOut.Speed = Math.Pow(2.0, TrkSpeed.Value / 12.0);
			mWaveOut.Pitch = Math.Pow(2.0, TrkKey.Value / 12.0) / mWaveOut.Speed;
		}

		private void TrkSpeed_Scroll(object sender, EventArgs e) {
			mWaveOut.Speed = Math.Pow(2.0, TrkSpeed.Value / 12.0);
			mWaveOut.Pitch = Math.Pow(2.0, TrkKey.Value / 12.0) / mWaveOut.Speed;
		}

		private void timer1_Tick(object sender, EventArgs e) {
			if (!mIsScrol) {
				var temp = mWaveOut.Position / mWaveOut.SampleRate;
				if (temp <= TrkSeek.Maximum) {
					TrkSeek.Value = temp;
				}
			}
			if (mSetLayout) {
				SetLayout();
			}
			var g = Graphics.FromImage(pictureBox1.Image);
			g.Clear(Color.Transparent);
			var width = pictureBox1.Width;
			var gaugeHeight = pictureBox1.Height / 3;
			var scrollHeight = pictureBox1.Height - gaugeHeight - KEYBOARD_HEIGHT;
			if (mWaveOut.Enabled) {
				DrawPeak(g, mWaveOut.FilterBank.Peak, width, gaugeHeight);
				DrawSlope(g, mWaveOut.FilterBank.Slope, width, gaugeHeight, Pens.Gray);
				DrawSpectrum(mWaveOut.FilterBank.Spec, gaugeHeight, scrollHeight);
			}
			if (mWaveIn.Enabled) {
				DrawPeak(g, mWaveIn.FilterBank.Peak, width, gaugeHeight);
				DrawSlope(g, mWaveIn.FilterBank.Slope, width, gaugeHeight, Pens.Gray);
				DrawSpectrum(mWaveIn.FilterBank.Spec, gaugeHeight, scrollHeight);
			}
			pictureBox1.Image = pictureBox1.Image;
			g.Dispose();
			mSetLayout = false;
		}

		void SetLayout() {
			TrkSeek.Top = 0;
			TrkSeek.Width = Width - TrkSeek.Left - 16;
			TrkKey.Top = TrkSeek.Bottom;
			TrkKey.Width = TrkSeek.Width / 2;
			TrkSpeed.Top = TrkKey.Top;
			TrkSpeed.Left = TrkKey.Right;
			TrkSpeed.Width = TrkSeek.Width / 2;
			pictureBox1.Top = TrkKey.Bottom;
			pictureBox1.Left = 0;
			pictureBox1.Width = Width - 16;
			pictureBox1.Height = Height - TrkKey.Bottom - 39;
			if (null != pictureBox1.Image) {
				pictureBox1.Image.Dispose();
				pictureBox1.Image = null;
			}
			pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height, PixelFormat.Format32bppArgb);
			mPix = new byte[4 * pictureBox1.Width * pictureBox1.Height];
			DrawBackground();
		}

		void DrawBackground() {
			if (null != pictureBox1.BackgroundImage) {
				pictureBox1.BackgroundImage.Dispose();
				pictureBox1.BackgroundImage = null;
			}
			pictureBox1.BackgroundImage = new Bitmap(pictureBox1.Width, pictureBox1.Height, PixelFormat.Format32bppArgb);
			var g = Graphics.FromImage(pictureBox1.BackgroundImage);
			g.Clear(Color.Black);
			var gaugeHeight = pictureBox1.Height / 3;
			DrawKeyboard(g, pictureBox1.Width, pictureBox1.Height, gaugeHeight);
			DrawGauge(g, pictureBox1.Width, gaugeHeight);
			pictureBox1.BackgroundImage = pictureBox1.BackgroundImage;
			g.Dispose();
		}

		void DrawKeyboard(Graphics g, int width, int height, int gaugeHeight) {
			var barBottom = height - 1;
			for (int note = 0; note < NOTE_COUNT; note++) {
				var px = (note + 0.0f) * width / NOTE_COUNT;
				var barWidth = (note + 1.0f) * width / NOTE_COUNT - px + 1;
				switch (note % 12) {
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
			var keyboardBottom = gaugeHeight + KEYBOARD_HEIGHT - 1;
			var textBottom = keyboardBottom + 1;
			var textHeight = g.MeasureString("9", FONT).Height;
			var textOfsX = textHeight * 0.5f;
			var textArea = new RectangleF(0f, 0f, KEYBOARD_HEIGHT, textHeight);
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Far,
				LineAlignment = StringAlignment.Center
			};
			for (int note = 9; note < NOTE_COUNT; note += 12) {
				var px = width * (note + 0.5f) / NOTE_COUNT - textOfsX;
				g.TranslateTransform(px, textBottom);
				g.RotateTransform(-90);
				g.DrawString(
					ToString(BASE_FREQ * Math.Pow(2.0, (note + 1 / 3.0) / 12.0)),
					FONT, Brushes.Gray, textArea, stringFormat
				);
				g.RotateTransform(90);
				g.TranslateTransform(-px, -textBottom);
			}
			g.DrawLine(KEYBOARD_BORDER, 0, keyboardBottom, right, keyboardBottom);
		}

		void DrawGauge(Graphics g, int width, int height) {
			var right = width - 1;
			for (double db = 0; RANGE_DB <= db; db -= 1.0) {
				var py = DbToY(db, height, 0);
				if (db % 10 == 0) {
					g.DrawLine(GRID_MAJOR, 0, py, right, py);
				} else if (height >= -RANGE_DB && db % 5 == 0) {
					g.DrawLine(GRID_MINOR1, 0, py, right, py);
				} else if (height >= -4 * RANGE_DB) {
					g.DrawLine(GRID_MINOR2, 0, py, right, py);
				}
			}
			var textSize = g.MeasureString("9", FONT);
			var textArea = new RectangleF(0f, 0f, 24, textSize.Height);
			var textBottom = height - textArea.Height + 4;
			var stringFormat = new StringFormat() {
				Alignment = StringAlignment.Near
			};
			for (double db = 0; RANGE_DB < db; db -= 10.0) {
				var py = DbToY(db, height, 0) - 2;
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

		void DrawPeak(Graphics g, double[] arr, int width, int height) {
			var count = arr.Length;
			for (int i = 0; i < count; i++) {
				var barX = (i + 0.0f) * width / count;
				var barWidth = (i + 1.0f) * width / count - barX;
				var barY = AmpToY(arr[i], height, 0);
				var barHeight = height - barY;
				g.FillRectangle(BAR.Brush, barX, barY, barWidth, barHeight);
			}
		}

		void DrawSlope(Graphics g, double[] arr, int width, int height, Pen color) {
			var idxA = 0;
			var preX = 0;
			var preY = AmpToY(arr[idxA], height, 0);
			for (int x = 0; x < width; x++) {
				var idxB = x * arr.Length / width;
				int y;
				if (1 < idxB - idxA) {
					y = AmpToY(arr[idxA], height, 0);
					g.DrawLine(color, preX, preY, x, y);
					var max = double.MinValue;
					var min = double.MaxValue;
					for (var i = idxA; i <= idxB; i++) {
						var v = arr[i];
						min = Math.Min(min, v);
						max = Math.Max(max, v);
					}
					var minY = AmpToY(min, height, 0);
					var maxY = AmpToY(max, height, 0);
					g.DrawLine(color, x, minY, x, maxY);
					y = AmpToY(arr[idxB], height, 0);
				} else {
					y = AmpToY(arr[idxB], height, 0);
					g.DrawLine(color, preX, preY, x, y);
				}
				preX = x;
				preY = y;
				idxA = idxB;
			}
		}

		void DrawSpectrum(double[] arr, int top, int height) {
			var bmp = (Bitmap)pictureBox1.Image;
			var data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
			var offsetY0 = data.Stride * top;
			var idxA = 0;
			for (int x = 0, pos = offsetY0; x < bmp.Width; x++, pos += 4) {
				var idxB = x * arr.Length / bmp.Width;
				if (1 < idxB - idxA) {
					var max = double.MinValue;
					for (var i = idxA; i <= idxB; i++) {
						max = Math.Max(max, arr[i]);
					}
					SetHue(mPix, pos, max);
				} else {
					SetHue(mPix, pos, arr[idxB]);
				}
				idxA = idxB;
			}
			for (int y = 1; y < KEYBOARD_HEIGHT; y++) {
				Array.Copy(
					mPix, offsetY0,
					mPix, offsetY0 + data.Stride * y,
					data.Stride
				);
			}
			var offsetY1 = data.Stride * (top + KEYBOARD_HEIGHT);
			Array.Copy(
				mPix, offsetY1,
				mPix, offsetY1 + data.Stride * SCROLL_SPEED,
				data.Stride * (height - SCROLL_SPEED)
			);
			Array.Copy(
				mPix, offsetY0,
				mPix, offsetY1,
				data.Stride * SCROLL_SPEED
			);
			Marshal.Copy(
				mPix, offsetY0,
				data.Scan0 + offsetY0,
				data.Stride * (KEYBOARD_HEIGHT + height)
			);
			bmp.UnlockBits(data);
		}

		string ToString(double value) {
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

		int DbToY(double db, int height, int offset) {
			if (db < RANGE_DB) {
				db = RANGE_DB;
			}
			return (int)(offset + db * height / RANGE_DB);
		}

		int AmpToY(double amp, int height, int offset) {
			if (amp < 1 / 32768.0) {
				amp = 1 / 32768.0;
			}
			var db = 20 * Math.Log10(amp);
			if (db < RANGE_DB) {
				db = RANGE_DB;
			}
			return (int)(offset + db * height / RANGE_DB);
		}

		void SetHue(byte[] pix, int pos, double amp) {
			if (amp < 1 / 32768.0) {
				amp = 1 / 32768.0;
			}
			var db = 20 * Math.Log10(amp);
			if (db < RANGE_DB) {
				pix[pos + 0] = 0;
				pix[pos + 1] = 0;
				pix[pos + 2] = 0;
				pix[pos + 3] = 0;
				return;
			}
			var g = (int)((1.0 - db / RANGE_DB) * 1279);
			if (g < 256) {
				pix[pos + 0] = 255;
				pix[pos + 1] = 0;
				pix[pos + 2] = 0;
				pix[pos + 3] = (byte)g;
			} else if (g < 512) {
				g -= 256;
				pix[pos + 0] = 255;
				pix[pos + 1] = (byte)g;
				pix[pos + 2] = 0;
				pix[pos + 3] = 191;
			} else if (g < 768) {
				g -= 512;
				pix[pos + 0] = (byte)(255 - g);
				pix[pos + 1] = 255;
				pix[pos + 2] = 0;
				pix[pos + 3] = 191;
			} else if (g < 1024) {
				g -= 768;
				pix[pos + 0] = 0;
				pix[pos + 1] = 255;
				pix[pos + 2] = (byte)g;
				pix[pos + 3] = 167;
			} else {
				g -= 1024;
				pix[pos + 0] = 0;
				pix[pos + 1] = (byte)(255 - g);
				pix[pos + 2] = 255;
				pix[pos + 3] = 191;
			}
		}
	}
}