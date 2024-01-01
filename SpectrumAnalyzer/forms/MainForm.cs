using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using SpectrumAnalyzer.Properties;

namespace SpectrumAnalyzer {
	public partial class MainForm : Form {
		public Playback Playback;
		public Record Record;

		public bool DoSetLayout { get; set; } = true;

		const int SEEK_DIV = 100;

		bool mGripSeekBar = false;
		int mGaugeHeight;
		int mScrollHeight;
		int mPlayFileIndex = 0;
		List<string> mFileList = new List<string>();

		public MainForm() {
			InitializeComponent();
			Playback = new Playback(48000, () => {
				mPlayFileIndex = ++mPlayFileIndex % mFileList.Count;
				Playback.OpenFile(mFileList[mPlayFileIndex]);
				Playback.File.Speed = Settings.Speed;
				Playback.Start();
			});
			Record = new Record(48000);
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
			if(null != Playback) {
				Playback.Dispose();
			}
			if (null != Record) {
				Record.Dispose();
			}
		}

		private void Form1_Load(object sender, EventArgs e) {
			timer1.Interval = 1;
			timer1.Enabled = true;
			timer1.Start();
			Playback.Open();
			Record.Open();
		}

		private void Form1_Resize(object sender, EventArgs e) {
			DoSetLayout = true;
		}

		private void TsbOpen_Click(object sender, EventArgs e) {
			openFileDialog1.FileName = "";
			openFileDialog1.Filter = "WAVファイル(*.wav)|*.wav";
			openFileDialog1.Multiselect = true;
			openFileDialog1.ShowDialog();
			var selectedFiles = openFileDialog1.FileNames;
			if (selectedFiles.Length == 0) {
				return;
			}

			mPlayFileIndex = 0;
			mFileList.Clear();
			foreach (var filePath in selectedFiles) {
				var file = new WavReader(filePath);
				if (file.CheckFormat()) {
					mFileList.Add(filePath);
				}
			}
			if (mFileList.Count == 0) {
				return;
			}

			var playing = Playback.Playing;
			try {
				Playback.OpenFile(mFileList[mPlayFileIndex]);
			} catch (Exception ex) {
				MessageBox.Show(ex.ToString());
			}
			Playback.File.Speed = Settings.Speed;
			if (playing) {
				Playback.Start();
			}
		}

		private void TsbPlay_Click(object sender, EventArgs e) {
			if (Playback.Playing) {
				Playback.Pause();
				TsbPlay.Text = "再生";
				TsbPlay.Image = Resources.play;
			} else {
				Record.Pause();
				Playback.Start();
				TsbRec.Text = "録音";
				TsbRec.Image = Resources.rec;
				TsbPlay.Text = "停止";
				TsbPlay.Image = Resources.play_stop;
				TrkSeek.Enabled = true;
			}
		}

		private void TsbRec_Click(object sender, EventArgs e) {
			if (Record.Playing) {
				Record.Pause();
				TsbRec.Text = "録音";
				TsbRec.Image = Resources.rec;
			} else {
				Playback.Pause();
				Record.Start();
				TsbPlay.Text = "再生";
				TsbPlay.Image = Resources.play;
				TsbRec.Text = "停止";
				TsbRec.Image = Resources.rec_stop;
				TrkSeek.Enabled = false;
			}
		}

		private void TsbSetting_Click(object sender, EventArgs e) {
			Settings.Open(this);
		}

		private void TrkSeek_MouseDown(object sender, MouseEventArgs e) {
			mGripSeekBar = true;
		}

		private void TrkSeek_MouseUp(object sender, EventArgs e) {
			Playback.File.Position = TrkSeek.Value * Playback.File.Format.SampleRate / SEEK_DIV;
			mGripSeekBar = false;
		}

		private void TrkSeek_KeyDown(object sender, KeyEventArgs e) {
			mGripSeekBar = true;
		}

		private void TrkSeek_KeyUp(object sender, KeyEventArgs e) {
			Playback.File.Position = TrkSeek.Value * Playback.File.Format.SampleRate / SEEK_DIV;
			mGripSeekBar = false;
		}

		private void timer1_Tick(object sender, EventArgs e) {
			var maximum = SEEK_DIV * Playback.File.Length / (int)Playback.File.Format.SampleRate;
			if (TrkSeek.Maximum != maximum) {
				TrkSeek.Value = 0;
				TrkSeek.Maximum = maximum;
				var sec = (double)Playback.File.Length / Playback.File.Format.SampleRate;
				if (sec >= 90) {
					TrkSeek.TickFrequency = (int)(60 * maximum / sec + 0.99);
				} else if (sec >= 15) {
					TrkSeek.TickFrequency = (int)(10 * maximum / sec + 0.99);
				} else {
					TrkSeek.TickFrequency = (int)(maximum / sec + 0.99);
				}
			}
			if (!mGripSeekBar) {
				var temp = (int)(SEEK_DIV * Playback.File.Position / Playback.File.Format.SampleRate);
				if (temp <= maximum) {
					TrkSeek.Value = temp;
				}
			}
			if (DoSetLayout) {
				SetLayout();
				DoSetLayout = false;
			}
			Draw();
		}

		public void SetLayout() {
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
			if (pictureBox1.Width < MinimumSize.Width - 16) {
				pictureBox1.Width = MinimumSize.Width - 16;
			}
			if (pictureBox1.Height < MinimumSize.Height - TrkSeek.Bottom - 39) {
				pictureBox1.Height = MinimumSize.Height - TrkSeek.Bottom - 39;
			}
			pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height, PixelFormat.Format32bppArgb);
			Drawer.ScrollCanvas = new byte[4 * pictureBox1.Width * pictureBox1.Height];
			if (Drawer.DisplayScroll) {
				mGaugeHeight = pictureBox1.Height / 2;
				mScrollHeight = pictureBox1.Height - mGaugeHeight - Drawer.KEYBOARD_HEIGHT;
			} else {
				mGaugeHeight = pictureBox1.Height - Drawer.KEYBOARD_HEIGHT;
				mScrollHeight = 0;
			}
			DrawBackground();
		}

		void Draw() {
			Spectrum spectrum = null;
			if (Record.Playing) {
				spectrum = Record.FilterBank;
			}
			if (null == spectrum) {
				spectrum = Playback.FilterBank;
			}
			var bmp = (Bitmap)pictureBox1.Image;
			var g = Graphics.FromImage(bmp);
			g.Clear(Color.Transparent);
			var width = pictureBox1.Width;
			var count = spectrum.L.Length;
			if (Drawer.DisplayCurve) {
				Drawer.Curve(g, spectrum.Curve, count, width, mGaugeHeight, Drawer.SLOPE);
			} else {
				Drawer.Surface(g, spectrum.Curve, count, width, mGaugeHeight);
			}
			if (Drawer.DisplayPeak) {
				Drawer.Peak(g, spectrum.Peak, count, width, mGaugeHeight);
				Drawer.Scroll(bmp, spectrum.Peak, count, mGaugeHeight + 1, mScrollHeight - 1, Drawer.KEYBOARD_HEIGHT - 1);
			} else {
				Drawer.Scroll(bmp, spectrum.Curve, count, mGaugeHeight + 1, mScrollHeight - 1, Drawer.KEYBOARD_HEIGHT - 1);
			}
			if (Drawer.DisplayThreshold) {
				Drawer.Curve(g, spectrum.Threshold, count, width, mGaugeHeight, Drawer.THRESHOLD);
			}
			pictureBox1.Image = pictureBox1.Image;
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
			Drawer.Keyboard(g, pictureBox1.Width, pictureBox1.Height, mGaugeHeight, Settings.NOTE_COUNT);
			Drawer.Gauge(g, pictureBox1.Width, mGaugeHeight);
			pictureBox1.BackgroundImage = pictureBox1.BackgroundImage;
			g.Dispose();
		}
	}
}