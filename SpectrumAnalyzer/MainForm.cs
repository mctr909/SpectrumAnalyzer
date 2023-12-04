using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace SpectrumAnalyzer {
	public partial class MainForm : Form {
		const int NOTE_COUNT = 124;
		const int KEYBOARD_HEIGHT = 34;
		readonly double BASE_FREQ = 442 * Math.Pow(2.0, 3 / 12.0 - 5);

		public Playback WaveOut;
		public Record WaveIn;

		bool mIsScrol;
		bool mSetLayout = true;

		public int KeyboardShift = 0;

		public MainForm() {
			InitializeComponent();
			WaveOut = new Playback(NOTE_COUNT, BASE_FREQ);
			WaveIn = new Record(44100, 256, NOTE_COUNT, BASE_FREQ);
		}

		private void Form1_Load(object sender, EventArgs e) {
			timer1.Interval = 1;
			timer1.Enabled = true;
			timer1.Start();
		}

		private void Form1_Resize(object sender, EventArgs e) {
			mSetLayout = true;
		}

		private void TsbOpen_Click(object sender, EventArgs e) {
			WaveOut.Close();
			WaveOut.Position = 0;

			openFileDialog1.FileName = "";
			openFileDialog1.Filter = "PCMファイル(*.wav)|*.wav";
			openFileDialog1.ShowDialog();
			var filePath = openFileDialog1.FileName;
			if (!File.Exists(filePath)) {
				return;
			}
			WaveOut.SetValue(filePath);
			TrkSeek.Minimum = 0;
			TrkSeek.Maximum = WaveOut.Length / WaveOut.SampleRate;
			TrkSeek.Value = 0;
		}

		private void TsbPlay_Click(object sender, EventArgs e) {
			if (WaveOut.Enabled) {
				WaveOut.Close();
				TsbPlay.Text = "再生";
			} else {
				WaveIn.Close();
				WaveOut.Open();
				TsbRec.Text = "録音";
				TsbPlay.Text = "停止";
				TrkSeek.Enabled = true;
			}
		}

		private void TsbRec_Click(object sender, EventArgs e) {
			if (WaveIn.Enabled) {
				WaveIn.Close();
				TsbRec.Text = "録音";
			} else {
				WaveOut.Close();
				WaveIn.Open();
				TsbPlay.Text = "再生";
				TsbRec.Text = "停止";
				TrkSeek.Enabled = false;
			}
		}

		private void TsbSetting_Click(object sender, EventArgs e) {
			var fm = new Settings(this);
			fm.StartPosition = FormStartPosition.CenterParent;
			fm.ShowDialog();
		}

		private void TrkSeek_MouseDown(object sender, MouseEventArgs e) {
			mIsScrol = true;
		}

		private void TrkSeek_MouseUp(object sender, EventArgs e) {
			WaveOut.Position = TrkSeek.Value * WaveOut.SampleRate;
			mIsScrol = false;
		}

		private void timer1_Tick(object sender, EventArgs e) {
			if (!mIsScrol) {
				var temp = WaveOut.Position / WaveOut.SampleRate;
				if (temp <= TrkSeek.Maximum) {
					TrkSeek.Value = temp;
				}
			}
			if (mSetLayout) {
				SetLayout();
				mSetLayout = false;
			}
			Draw();
		}

		void SetLayout() {
			TrkSeek.Top = 0;
			TrkSeek.Left = TsbPlay.Bounds.Right;
			TrkSeek.Width = Width - TrkSeek.Left - 16;
			pictureBox1.Top = TrkSeek.Bottom;
			pictureBox1.Left = 0;
			pictureBox1.Width = Width - 16;
			pictureBox1.Height = Height - TrkSeek.Bottom - 39;
			if (null != pictureBox1.Image) {
				pictureBox1.Image.Dispose();
				pictureBox1.Image = null;
			}
			if (pictureBox1.Height < MinimumSize.Height) {
				pictureBox1.Height = MinimumSize.Height;
			}
			pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height, PixelFormat.Format32bppArgb);
			Drawer.ScrollCanvas = new byte[4 * pictureBox1.Width * pictureBox1.Height];
			DrawBackground();
		}

		void Draw() {
			var bmp = (Bitmap)pictureBox1.Image;
			var g = Graphics.FromImage(bmp);
			g.Clear(Color.Transparent);
			var width = pictureBox1.Width;
			var gaugeHeight = pictureBox1.Height / 2;
			var scrollHeight = pictureBox1.Height - gaugeHeight - KEYBOARD_HEIGHT;
			if (WaveOut.Enabled) {
				Drawer.Peak(g, WaveOut.FilterBankL.Peak, width, gaugeHeight);
				Drawer.Slope(g, WaveOut.FilterBankL.Average, width, gaugeHeight, Pens.Cyan);
				Drawer.Slope(g, WaveOut.FilterBankL.Slope, width, gaugeHeight, Pens.Red);
				Drawer.Spectrum(bmp, WaveOut.FilterBankL.Peak, gaugeHeight, KEYBOARD_HEIGHT, scrollHeight);
				pictureBox1.Image = pictureBox1.Image;
			}
			if (WaveIn.Enabled) {
				Drawer.Peak(g, WaveIn.FilterBank.Peak, width, gaugeHeight);
				Drawer.Slope(g, WaveIn.FilterBank.Average, width, gaugeHeight, Pens.Cyan);
				Drawer.Slope(g, WaveIn.FilterBank.Slope, width, gaugeHeight, Pens.Red);
				Drawer.Spectrum(bmp, WaveIn.FilterBank.Peak, gaugeHeight, KEYBOARD_HEIGHT, scrollHeight);
				pictureBox1.Image = pictureBox1.Image;
			}
			g.Dispose();
		}

		public void DrawBackground() {
			if (null != pictureBox1.BackgroundImage) {
				pictureBox1.BackgroundImage.Dispose();
				pictureBox1.BackgroundImage = null;
			}
			pictureBox1.BackgroundImage = new Bitmap(pictureBox1.Width, pictureBox1.Height, PixelFormat.Format32bppArgb);
			var g = Graphics.FromImage(pictureBox1.BackgroundImage);
			g.Clear(Color.Black);
			var gaugeHeight = pictureBox1.Height / 2;
			Drawer.Keyboard(g,
				pictureBox1.Width, pictureBox1.Height,
				gaugeHeight, KEYBOARD_HEIGHT,
				KeyboardShift, NOTE_COUNT, BASE_FREQ
			);
			Drawer.Gauge(g, pictureBox1.Width, gaugeHeight);
			pictureBox1.BackgroundImage = pictureBox1.BackgroundImage;
			g.Dispose();
		}
	}
}