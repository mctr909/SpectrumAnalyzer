using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

using SpectrumAnalyzer.Properties;
using static Spectrum.Spectrum;

namespace SpectrumAnalyzer.Forms {
	public partial class Main : Form {
		public Playback Playback;
		public Record Record;

		const int SEEK_SEC_DIV = 10;

		Stopwatch Sw;
		long PreviousMilliSec = 0;

		bool NeedResize = true;
		bool GripSeekBar = false;
		int GaugeHeight;
		int ScrollHeight;

		Graphics G;
		double AutoGainMax = Drawer.AUTOGAIN_MAX;
		double[] Curve = new double[BANK_COUNT];
		double[] Peak = new double[BANK_COUNT];

		public Main() {
			InitializeComponent();
			Playback = new Playback(48000);
			Record = new Record(48000);
			MinimumSize = new Size(Drawer.DB_LABEL_WIDTH + HALFTONE_COUNT * 3 + 16, 200);
			Size = MinimumSize;
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
			Playback?.Dispose();
			Record?.Dispose();
		}

		private void Form1_Load(object sender, EventArgs e) {
			Sw = new Stopwatch();
			Sw.Start();
			TimerSeek.Interval = 1;
			TimerSeek.Enabled = true;
			TimerSeek.Start();
			TimerDisplay.Interval = 1;
			TimerDisplay.Enabled = true;
			TimerDisplay.Start();
			Playback.Open();
			Record.Open();
		}

		private void Form1_Resize(object sender, EventArgs e) {
			NeedResize = true;
		}

		private void TsbOpen_Click(object sender, EventArgs e) {
			openFileDialog1.FileName = "";
			openFileDialog1.Filter = "WAVファイル(*.wav)|*.wav";
			openFileDialog1.Multiselect = true;
			openFileDialog1.ShowDialog();

			var fileList = new List<string>();
			foreach (var filePath in openFileDialog1.FileNames) {
				if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) {
					continue;
				}
				var file = new WavReader(filePath);
				if (file.CheckFormat()) {
					fileList.Add(filePath);
				}
			}
			Playback.SetFiles(fileList);
		}

		private void TsbRec_Click(object sender, EventArgs e) {
			if (Record.Playing) {
				Record.Pause();
				TsbRec.Text = "録音";
				TsbRec.Image = Resources.rec;
			}
			else {
				Playback.Pause();
				Record.Start();
				TsbPlay.Text = "再生";
				TsbPlay.Image = Resources.play;
				TsbRec.Text = "停止";
				TsbRec.Image = Resources.rec_stop;
				TrkSeek.Enabled = false;
				TsbNext.Enabled = false;
				TsbRestart.Enabled = false;
				TsbPrevious.Enabled = false;
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
				TsbNext.Enabled = true;
				TsbRestart.Enabled = true;
				TsbPrevious.Enabled = true;
			}
		}

		private void TsbRestart_Click(object sender, EventArgs e) {
			Playback.File.Position = 0;
		}

		private void TsbPrevious_Click(object sender, EventArgs e) {
			Playback.Previous();
		}

		private void TsbNext_Click(object sender, EventArgs e) {
			Playback.Next();
		}

		private void TsbSetting_Click(object sender, EventArgs e) {
			Settings.Open(this);
		}

		private void TrkSeek_MouseDown(object sender, MouseEventArgs e) {
			GripSeekBar = true;
		}

		private void TrkSeek_MouseUp(object sender, EventArgs e) {
			Playback.File.Position = TrkSeek.Value * Playback.File.Format.SampleRate / SEEK_SEC_DIV;
			GripSeekBar = false;
		}

		private void TrkSeek_KeyDown(object sender, KeyEventArgs e) {
			GripSeekBar = true;
		}

		private void TrkSeek_KeyUp(object sender, KeyEventArgs e) {
			Playback.File.Position = TrkSeek.Value * Playback.File.Format.SampleRate / SEEK_SEC_DIV;
			GripSeekBar = false;
		}

		private void TimerSeek_Tick(object sender, EventArgs e) {
			var maxSec = (double)Playback.File.SampleCount / Playback.File.Format.SampleRate;
			var max = (int)(SEEK_SEC_DIV * maxSec);
			if (TrkSeek.Maximum != max) {
				TrkSeek.Value = 0;
				TrkSeek.Maximum = max;
				if (maxSec >= 90) {
					TrkSeek.TickFrequency = (int)(60 * max / maxSec + 0.99);
				} else if (maxSec >= 15) {
					TrkSeek.TickFrequency = (int)(10 * max / maxSec + 0.99);
				} else {
					TrkSeek.TickFrequency = (int)(max / maxSec + 0.99);
				}
			}

			double posSec;
			if (GripSeekBar) {
				posSec = (double)TrkSeek.Value / SEEK_SEC_DIV;
			} else {
				posSec = (double)Playback.File.Position / Playback.File.Format.SampleRate;
				TrkSeek.Value = (int)(SEEK_SEC_DIV * posSec);
			}

			var fsec = posSec % 60;
			var isec = (int)fsec;
			var min = ((int)(posSec / 60)).ToString("00");
			var sec = isec.ToString("00");
			var csec = ((int)((fsec - isec) * 100)).ToString("00");
			Text = $"[{min}:{sec}.{csec}] {Playback.PlayingName}";
		}

		private void TimerDisplay_Tick(object sender, EventArgs e) {
			var currentMilliSec = Sw.ElapsedMilliseconds;
			var deltaTime = currentMilliSec - PreviousMilliSec;
			if (deltaTime >= 1000 / 120.0) {
				if (NeedResize) {
					DoResize();
					NeedResize = false;
				}
				Draw();
				PreviousMilliSec = currentMilliSec;
			}
		}

		void DoResize() {
			TrkSeek.Top = 0;
			TrkSeek.Left = TsbNext.Bounds.Right;
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
			G = Graphics.FromImage(pictureBox1.Image);
			Drawer.ScrollCanvas = new byte[4 * pictureBox1.Width * pictureBox1.Height];
			DrawBackground();
		}

		void Draw() {
			Spectrum.Spectrum spectrum = null;
			if (Record.Playing) {
				spectrum = Record.Spectrum;
			}
			if (null == spectrum) {
				spectrum = Playback.Spectrum;
			}
			/* 表示値を正規化する場合、最大値をクリア */
			if (Drawer.NormGain) {
				AutoGainMax = Drawer.AUTOGAIN_MAX;
			}
			/* 表示値を自動調整する場合、最大値を減衰 */
			if (Drawer.AutoGain) {
				var autoGainAttenuation = 4.0 / Drawer.AUTOGAIN_SPEED / 120.0;
				AutoGainMax += (Drawer.AUTOGAIN_MAX - AutoGainMax) * autoGainAttenuation;
			}
			for (int i = 0; i < BANK_COUNT; i++) {
				Curve[i] = spectrum.Curve[i];
				Peak[i] = spectrum.Peak[i];
				AutoGainMax = Math.Max(AutoGainMax, Curve[i]);
			}
			if (!(Drawer.AutoGain || Drawer.NormGain)) {
				AutoGainMax = 1;
			}
			for (int i = 0; i < BANK_COUNT; i++) {
				Curve[i] /= AutoGainMax;
				Peak[i] /= AutoGainMax;
			}

			var bmp = (Bitmap)pictureBox1.Image;
			G.Clear(Color.Transparent);
			var width = pictureBox1.Width;
			if (Drawer.DisplayCurve) {
				Drawer.Curve(G, Curve, width, GaugeHeight, Pens.Cyan);
			} else {
				Drawer.Surface(G, Curve, width, GaugeHeight);
			}
			if (Drawer.DisplayPeak) {
				Drawer.Peak(G, Peak, width, GaugeHeight);
				Drawer.Scroll(bmp, Peak, GaugeHeight + 1, ScrollHeight - 1, Drawer.KEYBOARD_HEIGHT - 1);
			}
			else {
				Drawer.Scroll(bmp, Curve, GaugeHeight + 1, ScrollHeight - 1, Drawer.KEYBOARD_HEIGHT - 1);
			}
			pictureBox1.Image = pictureBox1.Image;
		}

		public void DrawBackground() {
			if (Drawer.DisplayScroll) {
				GaugeHeight = pictureBox1.Height / 2;
				ScrollHeight = pictureBox1.Height - GaugeHeight - Drawer.KEYBOARD_HEIGHT;
			}
			else {
				GaugeHeight = pictureBox1.Height - Drawer.KEYBOARD_HEIGHT;
				ScrollHeight = 0;
			}
			if (null != pictureBox1.BackgroundImage) {
				pictureBox1.BackgroundImage.Dispose();
				pictureBox1.BackgroundImage = null;
			}
			pictureBox1.BackgroundImage = new Bitmap(pictureBox1.Width, pictureBox1.Height, PixelFormat.Format32bppArgb);
			var g = Graphics.FromImage(pictureBox1.BackgroundImage);
			g.Clear(Color.Black);
			Drawer.Keyboard(g, pictureBox1.Width, pictureBox1.Height, GaugeHeight, HALFTONE_COUNT);
			Drawer.Gauge(g, pictureBox1.Width, GaugeHeight);
			pictureBox1.BackgroundImage = pictureBox1.BackgroundImage;
			g.Dispose();
		}
	}
}