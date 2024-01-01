using SoundApi;
using SpectrumAnalyzerNet.Properties;
using System.Diagnostics;
using System.Xml;

namespace SpectrumAnalyzerNet.Forms {
	public partial class Main : Form {
		public enum EState {
			Stop,
			Play,
			Rec
		}
		public EState State { get; private set; }
		public Playback Playback { get; private set; }
		public Record Record { get; private set; }

		private readonly Stopwatch Sw = new();
		private readonly Drawer Drawer;
		private bool NeedResize = true;
		private bool GripSeekBar = false;
		private double Progress = 0;
		private long PreviousMilliSec = 0;

		public Main() {
			InitializeComponent();
			Playback = new Playback(44100, 1e-3, 6, (type) => {
				switch (type) {
				case Playback.ENotify.Closed:
					State = EState.Stop;
					tsbPlay.Text = "再生";
					tsbPlay.Image = Resources.play;
					break;
				}
			});
			Playback.Open();
			Record = new Record(44100, 1e-3, 6);
			Record.Open();
			LoadSettings();
			Playback.File.Speed = Playback.Speed;
			Playback.Load(Application.ExecutablePath);
			Drawer = new Drawer(picChart, picScroll);
			Sw.Start();
			timer1.Interval = 1;
			timer1.Enabled = true;
			timer1.Start();
		}

		private void Main_FormClosing(object sender, FormClosingEventArgs e) {
			Playback?.Dispose();
			Record?.Dispose();
			SaveSettings();
		}

		private void Panel_SizeChanged(object sender, EventArgs e) {
			NeedResize = true;
		}

		private void ToolTipClicked(object sender, EventArgs e) {
			if (sender is ToolStripButton tsb) {
				switch (tsb.Name) {
				case nameof(tsbOpen):
					EditFileList();
					break;
				case nameof(tsbSettings):
					break;
				case nameof(tsbPrev):
					Playback.PreviousFile();
					break;
				case nameof(tsbRew):
					Playback.File.Position = 0;
					break;
				case nameof(tsbNext):
					Playback.NextFile();
					break;
				case nameof(tsbPlay):
					AltPlay();
					break;
				case nameof(tsbRec):
					AltRec();
					break;
				}
			}
		}

		private void progressBar_MouseDown(object sender, MouseEventArgs e) {
			Progress = (double)e.Location.X / progressBar.Width;
			GripSeekBar = true;
		}

		private void progressBar_MouseUp(object sender, MouseEventArgs e) {
			Playback.File.Position = Progress * Playback.File.SampleCount;
			Progress = 0;
			GripSeekBar = false;
		}

		private void progressBar_MouseMove(object sender, MouseEventArgs e) {
			if (!GripSeekBar) {
				return;
			}
			Progress = (double)e.Location.X / progressBar.Width;
			progressBar.Value = (int)(Progress * Playback.File.SampleCount / Playback.File.Format.SampleRate);
		}

		private void EditFileList() {
			openFileDialog1.FileName = "";
			openFileDialog1.Filter = "WAVファイル(*.wav)|*.wav";
			openFileDialog1.Multiselect = true;
			openFileDialog1.ShowDialog();
			var fileList = new List<string>();
			foreach (var filePath in openFileDialog1.FileNames) {
				if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) {
					continue;
				}
				var file = new RiffWavReader(filePath);
				if (file.CheckFormat()) {
					fileList.Add(filePath);
				}
			}
			Playback.SetFileList(fileList);
			Playback.Save(Application.ExecutablePath);
		}

		private void AltPlay() {
			if (EState.Play == State) {
				State = EState.Stop;
				tsbPlay.ToolTipText = "再生";
				tsbPlay.Image = Resources.play;
				var dc = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;
				Playback.Stop();
				Cursor.Current = dc;
			} else {
				State = EState.Play;
				tsbPrev.Enabled = true;
				tsbRew.Enabled = true;
				tsbNext.Enabled = true;
				progressBar.Enabled = true;
				tsbPlay.ToolTipText = "停止";
				tsbPlay.Image = Resources.play_stop;
				tsbRec.ToolTipText = "録音";
				tsbRec.Image = Resources.rec;
				var dc = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;
				Record.Stop();
				Playback.Start();
				Cursor.Current = dc;
			}
		}

		private void AltRec() {
			if (EState.Rec == State) {
				State = EState.Stop;
				tsbRec.ToolTipText = "録音";
				tsbRec.Image = Resources.rec;
				var dc = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;
				Record.Stop();
				Cursor.Current = dc;
			} else {
				State = EState.Rec;
				tsbPrev.Enabled = false;
				tsbRew.Enabled = false;
				tsbNext.Enabled = false;
				progressBar.Enabled = false;
				tsbPlay.ToolTipText = "再生";
				tsbPlay.Image = Resources.play;
				tsbRec.ToolTipText = "停止";
				tsbRec.Image = Resources.rec_stop;
				var dc = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;
				Playback.Stop();
				Record.Start();
				Cursor.Current = dc;
			}
		}

		private void timer1_Tick(object sender, EventArgs e) {
			var currentMilliSec = Sw.ElapsedMilliseconds;
			var deltaTime = currentMilliSec - PreviousMilliSec;
			if (deltaTime >= 1000 / 120.0) {
				PreviousMilliSec = currentMilliSec;
				if (NeedResize) {
					ResizeCanvas();
					NeedResize = false;
				}
				switch (State) {
				case EState.Rec:
					Drawer.Draw(Record.Spectrum);
					break;
				case EState.Play:
					Drawer.Draw(Playback.Spectrum);
					break;
				default:
					Drawer.Draw();
					break;
				}
				var maxSec = (double)Playback.File.SampleCount / Playback.File.Format.SampleRate;
				var max = (int)(maxSec);
				if (progressBar.Maximum != max) {
					progressBar.Value = 0;
					progressBar.Maximum = max;
				}
				double posSec;
				if (GripSeekBar) {
					posSec = progressBar.Value;
				} else {
					posSec = (double)Playback.File.Position / Playback.File.Format.SampleRate;
					progressBar.Value = (int)posSec;
				}
			}
		}

		private void ResizeCanvas() {
			picChart.Left = 0;
			picChart.Top = 0;
			picChart.Width = splitContainer1.Panel1.Width;
			picChart.Height = splitContainer1.Panel1.Height;
			picScroll.Left = 0;
			picScroll.Top = 0;
			picScroll.Width = splitContainer1.Panel2.Width;
			picScroll.Height = splitContainer1.Panel2.Height;
			progressBar.Width = toolStrip.Width - toolStripSeparator4.Bounds.Right - 8;
			Drawer.Resize();
			Drawer.DrawBackground();
		}

		public void DrawBackground() {
			Drawer.DrawBackground();
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
						//Drawer.DisplayScroll = dispScroll;
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
			Playback.Speed = speed;
			Drawer.KeyShift = (int)(key + 0.5 * Math.Sign(key));
		}

		private void SaveSettings() {
			var xml = new XmlDocument();
			var root = xml.CreateElement("settings");

			var elmKey = xml.CreateElement("key");
			var key = Math.Log(Playback.Osc.Pitch * Playback.Speed, 2.0) * 12;
			elmKey.InnerText = $"{(int)(key + 0.5 * Math.Sign(key))}";
			root.AppendChild(elmKey);

			var elmSpeed = xml.CreateElement("speed");
			var speed = Math.Truncate((decimal)(Playback.Speed * 1000)) / 1000.0m;
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
			//elmDispScroll.InnerText = $"{Drawer.DisplayScroll}";
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
	}
}
