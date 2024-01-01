using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
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
			Playback = new Playback(44100, 1e-3, 6, (type) => {
				switch (type) {
				case Playback.ENotify.Closed:
					TsbPlay.Text = "再生";
					TsbPlay.Image = Resources.play;
					break;
				}
			});
			Record = new Record(44100, 1e-3, 6);
			MinimumSize = new Size(Drawer.CanpasWidthMin + 16, 192);
			Size = MinimumSize;
			Drawer = new Drawer(pictureBox1);
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
			Playback?.Dispose();
			Record?.Dispose();
			SaveSettings();
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
			LoadSettings();
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

		private void SaveSettings() {
			var xml = new XmlDocument();
			var root = xml.CreateElement("settings");

			var elmKey = xml.CreateElement("key");
			var key = Math.Log(Playback.Osc.Pitch * Settings.Speed, 2.0) * 12;
			elmKey.InnerText = $"{(int)(key + 0.5 * Math.Sign(key))}";
			root.AppendChild(elmKey);

			var elmSpeed = xml.CreateElement("speed");
			var speed = Math.Truncate((decimal)(Settings.Speed * 1000)) / 1000.0m;
			elmSpeed.InnerText = $"{speed}";
			root.AppendChild(elmSpeed);

			var elmDbRange = xml.CreateElement("db_range");
			elmDbRange.InnerText = $"{Drawer.DisplayRangeDb}";
			root.AppendChild(elmDbRange);

			var elmDbMax = xml.CreateElement("db_max");
			elmDbMax.InnerText = $"{Drawer.DisplayMaxDb}";
			root.AppendChild(elmDbMax);

			var elmAutoGain = xml.CreateElement("auto_gain");
			elmAutoGain.InnerText = $"{Drawer.EnableAutoGain}";
			root.AppendChild(elmAutoGain);

			var elmNormGain = xml.CreateElement("norm_gain");
			elmNormGain.InnerText = $"{Drawer.EnableNormalize}";
			root.AppendChild(elmNormGain);

			var elmDispCurve = xml.CreateElement("disp_curve");
			elmDispCurve.InnerText = $"{Drawer.DisplayCurve}";
			root.AppendChild(elmDispCurve);

			var elmDispPeak = xml.CreateElement("disp_peak");
			elmDispPeak.InnerText = $"{Drawer.DisplayPeak}";
			root.AppendChild(elmDispPeak);

			var elmDispThreshold = xml.CreateElement("disp_threshold");
			elmDispThreshold.InnerText = $"{Drawer.DisplayThreshold}";
			root.AppendChild(elmDispThreshold);

			var elmDispFreq = xml.CreateElement("disp_freq");
			elmDispFreq.InnerText = $"{Drawer.DisplayFreq}";
			root.AppendChild(elmDispFreq);

			var elmDispScroll = xml.CreateElement("disp_scroll");
			elmDispScroll.InnerText = $"{Drawer.DisplayScroll}";
			root.AppendChild(elmDispScroll);

			var elmScrollSpeed = xml.CreateElement("scroll_speed");
			elmScrollSpeed.InnerText = $"{Drawer.ScrollSpeed}";
			root.AppendChild(elmScrollSpeed);

			var elmWidth = xml.CreateElement("width");
			elmWidth.InnerText = $"{Width}";
			root.AppendChild(elmWidth);

			var elmHeight = xml.CreateElement("height");
			elmHeight.InnerText = $"{Height}";
			root.AppendChild(elmHeight);

			var elmLeft = xml.CreateElement("left");
			elmLeft.InnerText = $"{Left}";
			root.AppendChild(elmLeft);

			var elmTop = xml.CreateElement("top");
			elmTop.InnerText = $"{Top}";
			root.AppendChild(elmTop);

			var elmOutDeviceName = xml.CreateElement("out_device_name");
			elmOutDeviceName.InnerText = Playback.GetDeviceName();
			root.AppendChild(elmOutDeviceName);

			var elmInDeviceName = xml.CreateElement("in_device_name");
			elmInDeviceName.InnerText = Record.GetDeviceName();
			root.AppendChild(elmInDeviceName);

			xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
			xml.AppendChild(root);
			xml.Save(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "settings.xml"));
		}

		private void LoadSettings() {
			var path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "settings.xml");
			if (!File.Exists(path)) {
				return;
			}
			var xml = new XmlDocument();
			xml.Load(path);
			var root = xml.SelectSingleNode("settings");
			if (root == null) {
				return;
			}
			var key = 0.0;
			var speed = 1.0;
			foreach (XmlNode node in root.ChildNodes) {
				switch (node.Name) {
				case "key":
					double.TryParse(node.InnerText, out key);
					break;
				case "speed":
					double.TryParse(node.InnerText, out speed);
					break;

				case "db_range":
					if (int.TryParse(node.InnerText, out var dbRange)) {
						Drawer.DisplayRangeDb = dbRange;
					}
					break;
				case "db_max":
					if (int.TryParse(node.InnerText, out var dbMax)) {
						Drawer.DisplayMaxDb = dbMax;
					}
					break;
				case "auto_gain":
					if (bool.TryParse(node.InnerText, out var autoGain)) {
						Drawer.EnableAutoGain = autoGain;
					}
					break;
				case "norm_gain":
					if (bool.TryParse(node.InnerText, out var normGain)) {
						Drawer.EnableNormalize = normGain;
					}
					break;

				case "disp_curve":
					if (bool.TryParse(node.InnerText, out var dispCurve)) {
						Drawer.DisplayCurve = dispCurve;
					}
					break;
				case "disp_peak":
					if (bool.TryParse(node.InnerText, out var dispPeak)) {
						Drawer.DisplayPeak = dispPeak;
					}
					break;
				case "disp_threshold":
					if (bool.TryParse(node.InnerText, out var dispThreshold)) {
						Drawer.DisplayThreshold = dispThreshold;
					}
					break;
				case "disp_freq":
					if (bool.TryParse(node.InnerText, out var dispFreq)) {
						Drawer.DisplayFreq = dispFreq;
					}
					break;
				case "disp_scroll":
					if (bool.TryParse(node.InnerText, out var dispScroll)) {
						Drawer.DisplayScroll = dispScroll;
					}
					break;
				case "scroll_speed":
					if (int.TryParse(node.InnerText, out var scrollSpeed)) {
						Drawer.ScrollSpeed = scrollSpeed;
					}
					break;

				case "width":
					if (int.TryParse(node.InnerText, out var width)) {
						Width = width;
					}
					break;
				case "height":
					if (int.TryParse(node.InnerText, out var height)) {
						Height = height;
					}
					break;
				case "left":
					if (int.TryParse(node.InnerText, out var left)) {
						Left = left;
					}
					break;
				case "top":
					if (int.TryParse(node.InnerText, out var top)) {
						Top = top;
					}
					break;

				case "out_device_name":
					Playback.SetDeviceByName(node.InnerText);
					break;
				case "in_device_name":
					Record.SetDeviceByName(node.InnerText);
					break;
				}
			}
			Playback.Osc.Pitch = Math.Pow(2.0, key / 12.0) / speed;
			Settings.Speed = speed;
			Drawer.KeyShift = (int)(key + 0.5 * Math.Sign(key));
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