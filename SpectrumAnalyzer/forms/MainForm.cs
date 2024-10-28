using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using SpectrumAnalyzer.Properties;

namespace SpectrumAnalyzer {
	public partial class MainForm : Form {
		public Playback Playback;
		public Record Record;
		public SpectrumDll Spectrum;

		public bool DoSetLayout { get; set; } = true;

		const int SEEK_SEC_DIV = 10;

		bool mGripSeekBar = false;
		int mGaugeHeight;
		int mScrollHeight;
		int mPlayFileIndex = 0;
		string mPlayingName = "";
		List<string> mFileList = new List<string>();

		public MainForm() {
			InitializeComponent();
			//Spectrum = new SpectrumDll();
			Playback = new Playback(48000, (isOpened) => {
				mPlayingName = Path.GetFileNameWithoutExtension(mFileList[mPlayFileIndex]);
				Playback.File.Speed = Settings.Speed;
			}, () => {
				if (mFileList.Count > 0) {
					mPlayFileIndex = ++mPlayFileIndex % mFileList.Count;
					Playback.OpenFile(mFileList[mPlayFileIndex]);
					Playback.Start();
				}
			});
			Record = new Record(48000);
			MinimumSize = new Size(Settings.NOTE_COUNT * 3 + 16, 200);
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
			if(null != Playback) {
				Playback.Dispose();
			}
			//if (null != Spectrum) {
			//	Spectrum.Dispose();
			//}
			if (null != Record) {
				Record.Dispose();
			}
		}

		private void Form1_Load(object sender, EventArgs e) {
			mPlayingName = Text;
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
			DoSetLayout = true;
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
			if (fileList.Count == 0) {
				return;
			}

			mPlayFileIndex = 0;
			mFileList.Clear();
			mFileList.AddRange(fileList);
			//Spectrum.SetFiles(fileList);

			var playing = Playback.Playing;
			//var playing = Spectrum.Playing;
			Playback.OpenFile(mFileList[mPlayFileIndex]);
			if (playing) {
				Playback.Start();
				//Spectrum.Start();
			}
		}

		private void TsbRec_Click(object sender, EventArgs e) {
			if (Record.Playing) {
				Record.Pause();
				TsbRec.Text = "録音";
				TsbRec.Image = Resources.rec;
			}
			else {
				Playback.Pause();
				//Spectrum.Pause();
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
			//if (Spectrum.Playing) {
				Playback.Pause();
				//Spectrum.Pause();
				TsbPlay.Text = "再生";
				TsbPlay.Image = Resources.play;
			} else {
				Record.Pause();
				Playback.Start();
				//Spectrum.Start();
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
			//Spectrum.Position = 0;
		}

		private void TsbPrevious_Click(object sender, EventArgs e) {
			//Spectrum.Previous();
			if (mFileList.Count == 0) {
				return;
			}
			var play = Playback.Playing;
			if (play) {
				Playback.Pause();
			}
			mPlayFileIndex = (mFileList.Count + mPlayFileIndex - 1) % mFileList.Count;
			Playback.OpenFile(mFileList[mPlayFileIndex]);
			if (play) {
				Playback.Start();
			}
		}

		private void TsbNext_Click(object sender, EventArgs e) {
			//Spectrum.Next();
			if (mFileList.Count == 0) {
				return;
			}
			var play = Playback.Playing;
			if (play) {
				Playback.Pause();
			}
			mPlayFileIndex = ++mPlayFileIndex % mFileList.Count;
			Playback.OpenFile(mFileList[mPlayFileIndex]);
			if (play) {
				Playback.Start();
			}
		}

		private void TsbSetting_Click(object sender, EventArgs e) {
			Settings.Open(this);
		}

		private void TrkSeek_MouseDown(object sender, MouseEventArgs e) {
			mGripSeekBar = true;
		}

		private void TrkSeek_MouseUp(object sender, EventArgs e) {
			Playback.File.Position = TrkSeek.Value * Playback.File.Format.SampleRate / SEEK_SEC_DIV;
			//Spectrum.Position = TrkSeek.Value * Playback.File.Format.SampleRate / SEEK_SEC_DIV;
			mGripSeekBar = false;
		}

		private void TrkSeek_KeyDown(object sender, KeyEventArgs e) {
			mGripSeekBar = true;
		}

		private void TrkSeek_KeyUp(object sender, KeyEventArgs e) {
			Playback.File.Position = TrkSeek.Value * Playback.File.Format.SampleRate / SEEK_SEC_DIV;
			//Spectrum.Position = TrkSeek.Value * Playback.File.Format.SampleRate / SEEK_SEC_DIV;
			mGripSeekBar = false;
		}

		private void TimerSeek_Tick(object sender, EventArgs e) {
			var maxSec = (double)Playback.File.Length / Playback.File.Format.SampleRate;
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
			if (mGripSeekBar) {
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
			Text = $"[{min}:{sec}.{csec}] {mPlayingName}";
		}

		private void TimerDisplay_Tick(object sender, EventArgs e) {
			if (DoSetLayout) {
				SetLayout();
				DoSetLayout = false;
			}
			Draw();
		}

		public void SetLayout() {
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

		void Draw() {
			Spectrum spectrum = null;
			if (Record.Playing) {
				spectrum = Record.Spectrum;
			}
			if (null == spectrum) {
				spectrum = Playback.Spectrum;
			}
			var bmp = (Bitmap)pictureBox1.Image;
			var g = Graphics.FromImage(bmp);
			g.Clear(Color.Transparent);
			var width = pictureBox1.Width;
			var count = spectrum.Banks.Length;
			if (Drawer.DisplayCurve) {
				Drawer.Surface(g, spectrum.Curve, count, width, mGaugeHeight);
			}
			if (Drawer.DisplayPeak) {
				Drawer.Peak(g, spectrum.Peak, count, width, mGaugeHeight);
				Drawer.Scroll(bmp, spectrum.Peak, count, mGaugeHeight + 1, mScrollHeight - 1, Drawer.KEYBOARD_HEIGHT - 1);
			}
			else {
				Drawer.Scroll(bmp, spectrum.Curve, count, mGaugeHeight + 1, mScrollHeight - 1, Drawer.KEYBOARD_HEIGHT - 1);
			}
			if (Drawer.DisplayThreshold) {
				Drawer.Curve(g, spectrum.Threshold, count, width, mGaugeHeight, Drawer.THRESHOLD);
			}
			pictureBox1.Image = pictureBox1.Image;
			g.Dispose();
		}
	}
}