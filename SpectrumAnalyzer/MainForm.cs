using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using SpectrumAnalyzer.Properties;

namespace SpectrumAnalyzer {
	public partial class MainForm : Form {
		const int NOTE_COUNT = 124;
		const int KEYBOARD_HEIGHT = 34;
		readonly double BASE_FREQ = 442 * Math.Pow(2.0, 3 / 12.0 - 5);

		public Playback Playback;
		public Record Record;

		bool mGripSeekBar = false;
		bool mSetLayout = true;

		public MainForm() {
			InitializeComponent();
			Settings.Instance = new Settings(this);
			Playback = new Playback(NOTE_COUNT, BASE_FREQ);
			Record = new Record(44100, 128, NOTE_COUNT, BASE_FREQ);
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
			openFileDialog1.FileName = "";
			openFileDialog1.Filter = "WAVファイル(*.wav)|*.wav";
			openFileDialog1.ShowDialog();
			var filePath = openFileDialog1.FileName;
			if (!File.Exists(filePath)) {
				return;
			}
			var enable = Playback.Enabled;
			Playback.Close();
			Playback.Position = 0;
			Playback.LoadFile(filePath);
			TrkSeek.Minimum = 0;
			TrkSeek.Maximum = Playback.Length / Playback.SampleRate;
			TrkSeek.Value = 0;
			if (enable) {
				Playback.Open();
			}
		}

		private void TsbPlay_Click(object sender, EventArgs e) {
			if (Playback.Enabled) {
				Playback.Close();
				TsbPlay.Text = "再生";
				TsbPlay.Image = Resources.play;
			} else {
				Record.Close();
				Playback.Open();
				TsbRec.Text = "録音";
				TsbRec.Image = Resources.rec;
				TsbPlay.Text = "停止";
				TsbPlay.Image = Resources.play_stop;
				TrkSeek.Enabled = true;
			}
		}

		private void TsbRec_Click(object sender, EventArgs e) {
			if (Record.Enabled) {
				Record.Close();
				TsbRec.Text = "録音";
				TsbRec.Image = Resources.rec;
			} else {
				Playback.Close();
				Record.Open();
				TsbPlay.Text = "再生";
				TsbPlay.Image = Resources.play;
				TsbRec.Text = "停止";
				TsbRec.Image = Resources.rec_stop;
				TrkSeek.Enabled = false;
			}
		}

		private void TsbSetting_Click(object sender, EventArgs e) {
			if (!Settings.Instance.Visible) {
				Settings.Instance.Visible = true;
				Settings.Instance.Location = Location;
			}
		}

		private void TrkSeek_MouseDown(object sender, MouseEventArgs e) {
			mGripSeekBar = true;
		}

		private void TrkSeek_MouseUp(object sender, EventArgs e) {
			Playback.Position = TrkSeek.Value * Playback.SampleRate;
			mGripSeekBar = false;
		}

		private void TrkSeek_KeyDown(object sender, KeyEventArgs e) {
			mGripSeekBar = true;
		}

		private void TrkSeek_KeyUp(object sender, KeyEventArgs e) {
			Playback.Position = TrkSeek.Value * Playback.SampleRate;
			mGripSeekBar = false;
		}

		private void timer1_Tick(object sender, EventArgs e) {
			if (!mGripSeekBar) {
				var temp = Playback.Position / Playback.SampleRate;
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
			if (Playback.Enabled) {
				Drawer.Peak(g, Playback.FilterBankL.Peak, width, gaugeHeight);
				if (Drawer.DisplayThreshold) {
					Drawer.Slope(g, Playback.FilterBankL.Threshold, width, gaugeHeight, Pens.Cyan);
				}
				Drawer.Slope(g, Playback.FilterBankL.Slope, width, gaugeHeight, Pens.Red);
				Drawer.Scroll(bmp, Playback.FilterBankL.Peak, gaugeHeight, KEYBOARD_HEIGHT, scrollHeight);
				pictureBox1.Image = pictureBox1.Image;
			}
			if (Record.Enabled) {
				Drawer.Peak(g, Record.FilterBank.Peak, width, gaugeHeight);
				if (Drawer.DisplayThreshold) {
					Drawer.Slope(g, Record.FilterBank.Threshold, width, gaugeHeight, Pens.Cyan);
				}
				Drawer.Slope(g, Record.FilterBank.Slope, width, gaugeHeight, Pens.Red);
				Drawer.Scroll(bmp, Record.FilterBank.Peak, gaugeHeight, KEYBOARD_HEIGHT, scrollHeight);
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
				NOTE_COUNT, BASE_FREQ
			);
			Drawer.Gauge(g, pictureBox1.Width, gaugeHeight);
			pictureBox1.BackgroundImage = pictureBox1.BackgroundImage;
			g.Dispose();
		}
	}
}