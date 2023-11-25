using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace SpectrumAnalyzer {
	public partial class Form1 : Form {
		const int NOTE_COUNT = 124;
		const int KEYBOARD_HEIGHT = 34;
		readonly double BASE_FREQ = 13.75 * Math.Pow(2.0, 3 / 12.0);

		Playback mWaveOut;
		Record mWaveIn;
		
		bool mIsScrol;
		bool mSetLayout = true;

		public Form1() {
			InitializeComponent();
			mWaveOut = new Playback(NOTE_COUNT, BASE_FREQ);
			mWaveOut.Open();
			mWaveIn = new Record(44100, 256, NOTE_COUNT, BASE_FREQ);
			mWaveIn.Open();
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
				TrkSeek.Enabled = true;
				TrkSpeed.Enabled = true;
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
				TrkSeek.Enabled = false;
				TrkSpeed.Enabled = false;
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
			DrawBackground();
		}

		private void TrkSpeed_Scroll(object sender, EventArgs e) {
			mWaveOut.Speed = Math.Pow(2.0, TrkSpeed.Value / 12.0);
			mWaveOut.Pitch = Math.Pow(2.0, TrkKey.Value / 12.0) / mWaveOut.Speed;
			DrawBackground();
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
				mSetLayout = false;
			}
			Draw();
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
			if (mWaveOut.Enabled) {
				Drawer.Peak(g, mWaveOut.FilterBankL.Peak, width, gaugeHeight);
				Drawer.Slope(g, mWaveOut.FilterBankL.Slope, width, gaugeHeight, Pens.OrangeRed);
				Drawer.Spectrum(bmp, mWaveOut.FilterBankL.Peak, gaugeHeight, KEYBOARD_HEIGHT, scrollHeight);
			}
			if (mWaveIn.Enabled) {
				Drawer.Peak(g, mWaveIn.FilterBank.Peak, width, gaugeHeight);
				Drawer.Slope(g, mWaveIn.FilterBank.Slope, width, gaugeHeight, Pens.OrangeRed);
				Drawer.Spectrum(bmp, mWaveIn.FilterBank.Peak, gaugeHeight, KEYBOARD_HEIGHT, scrollHeight);
			}
			pictureBox1.Image = pictureBox1.Image;
			g.Dispose();
		}

		void DrawBackground() {
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
				TrkKey.Value - TrkSpeed.Value, NOTE_COUNT, BASE_FREQ
			);
			Drawer.Gauge(g, pictureBox1.Width, gaugeHeight);
			pictureBox1.BackgroundImage = pictureBox1.BackgroundImage;
			g.Dispose();
		}
	}
}