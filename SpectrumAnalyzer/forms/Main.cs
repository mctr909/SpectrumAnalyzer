using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SpectrumAnalyzer.Properties;

namespace SpectrumAnalyzer.Forms {
	public partial class Main : Form {
		public Playback Playback;
		public Record Record;

		const int SEEK_SEC_DIV = 10;

		Stopwatch Sw;
		long PreviousMilliSec = 0;

		bool NeedResize = true;
		bool GripSeekBar = false;

		readonly Drawer Drawer;

		public Main() {
			InitializeComponent();
			Playback = new Playback(44100, 1e-3, 6);
			Record = new Record(44100, 1e-3, 6);
			MinimumSize = new Size(Drawer.CanpasWidthMin + 16, 192);
			Size = MinimumSize;
			Drawer = new Drawer(pictureBox1);
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
			Settings.SetInstance(this);
			Playback.File.Speed = Settings.Speed;
			Playback.Load(Application.ExecutablePath);
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
			Playback.SetFileList(fileList);
			Playback.Save(Application.ExecutablePath);
		}

		private void TsbRec_Click(object sender, EventArgs e) {
			if (Record.IsPlaying) {
				Record.Stop();
				TsbRec.Text = "録音";
				TsbRec.Image = Resources.rec;
			} else {
				Playback.Stop();
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
			if (Playback.IsPlaying) {
				Playback.Stop();
				TsbPlay.Text = "再生";
				TsbPlay.Image = Resources.play;
			} else {
				Record.Stop();
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
			Playback.PreviousFile();
		}

		private void TsbNext_Click(object sender, EventArgs e) {
			Playback.NextFile();
		}

		private void TsbSetting_Click(object sender, EventArgs e) {
			Settings.Open();
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
					TrkSeek.Top = 0;
					TrkSeek.Left = TsbNext.Bounds.Right;
					TrkSeek.Width = Width - TrkSeek.Left - 16;
					pictureBox1.Top = TrkSeek.Bottom;
					pictureBox1.Left = 0;
					pictureBox1.Width = Width - 16;
					pictureBox1.Height = Height - TrkSeek.Bottom - 39;
					if (pictureBox1.Width < MinimumSize.Width - 16) {
						pictureBox1.Width = MinimumSize.Width - 16;
					}
					if (pictureBox1.Height < MinimumSize.Height - TrkSeek.Bottom - 39) {
						pictureBox1.Height = MinimumSize.Height - TrkSeek.Bottom - 39;
					}
					ResizeCanvas();
					NeedResize = false;
				}
				if (Record.IsPlaying) {
					Drawer.Update(Record.Spectrum);
				} else {
					Drawer.Update(Playback.Spectrum);
				}
				PreviousMilliSec = currentMilliSec;
			}
		}

		public void ResizeCanvas() {
			Drawer.Resize();
			Drawer.DrawBackground();
		}

		public void DrawBackground() {
			Drawer.DrawBackground();
		}
	}
}