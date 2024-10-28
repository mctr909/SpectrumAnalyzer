using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;

using WinMM;
using static Spectrum.Spectrum;

namespace SpectrumAnalyzer.Forms {
	public partial class Settings : Form {
		public static int KeyCent { get; private set; } = 0;
		public static int SpeedCent { get; private set; } = 0;
		public static double Speed { get { return Math.Pow(2.0, SpeedCent / 1200.0); } }
		public static double Transpose { get { return -SpeedCent / 100.0; } }

		public static bool DisplayPeak { get; private set; } = true;
		public static bool DisplayCurve { get; private set; } = true;
		public static bool DisplayThreshold { get; private set; } = false;
		public static bool DisplayFreq { get; private set; } = true;
		public static bool DisplayScroll { get; private set; } = false;
		public static int ScrollSpeed { get; private set; } = 2;

		static Settings Instance;

		Main ParentForm;

		Settings(Main form) {
			InitializeComponent();
			ParentForm = form;
		}

		public static void Open(Main parent) {
			if (null == Instance) {
				Instance = new Settings(parent);
				Instance.Initialize();
				Instance.GrbSpeed.Enabled = parent.Playback.Playing;
				Instance.TrkKey.Value = KeyCent / 100;
				Instance.TrkSpeed.Value = (int)(SpeedCent / 1200.0 * OCT_DIV);
				Instance.TrkDispRange.Value = Drawer.MinDb;
				Instance.TrkDispMax.Value = Drawer.OffsetDb;
				Instance.TrkScrollSpeed.Enabled = DisplayScroll;
				Instance.TrkScrollSpeed.Value = ScrollSpeed;
				Instance.ChkCurve.Checked = DisplayCurve;
				Instance.ChkPeak.Checked = DisplayPeak;
				Instance.ChkThreshold.Checked = DisplayThreshold;
				Instance.ChkFreq.Checked = DisplayFreq;
				Instance.ChkScroll.Checked = DisplayScroll;
				Instance.RbAutoGain.Checked = EnableAutoGain;
				Instance.RbNormGain.Checked = EnableNormalize;
				Instance.RbGainNone.Checked = !(EnableAutoGain || EnableNormalize);
				Instance.Visible = true;
				Instance.Location = parent.Location;
				Instance.DispValue();
			}
			if (!Instance.Visible) {
				Instance.Visible = true;
			}
		}

		private void Settings_FormClosing(object sender, FormClosingEventArgs e) {
			e.Cancel = true;
			Visible = false;
		}

		private void TrkKey_Scroll(object sender, EventArgs e) {
			ChangeKeySpeed();
		}

		private void TrkSpeed_Scroll(object sender, EventArgs e) {
			ChangeKeySpeed();
		}

		private void ChkCurve_CheckedChanged(object sender, EventArgs e) {
			DisplayCurve = ChkCurve.Checked;
		}

		private void ChkPeak_CheckedChanged(object sender, EventArgs e) {
			DisplayPeak = ChkPeak.Checked;
		}

		private void ChkThreshold_CheckedChanged(object sender, EventArgs e) {
			DisplayThreshold = ChkThreshold.Checked;
		}

		private void ChkFreq_CheckedChanged(object sender, EventArgs e) {
			DisplayFreq = ChkFreq.Checked;
			ParentForm.DrawBackground();
		}

		private void ChkScroll_CheckedChanged(object sender, EventArgs e) {
			DisplayScroll = ChkScroll.Checked;
			TrkScrollSpeed.Enabled = DisplayScroll;
			ParentForm.DrawBackground();
		}

		private void TrkDispRange_Scroll(object sender, EventArgs e) {
			Drawer.MinDb = TrkDispRange.Value;
			DispValue();
			ParentForm.DrawBackground();
		}

		private void TrkDispMax_Scroll(object sender, EventArgs e) {
			Drawer.OffsetDb = TrkDispMax.Value;
			DispValue();
			ParentForm.DrawBackground();
		}

		private void TrkScrollSpeed_Scroll(object sender, EventArgs e) {
			ScrollSpeed = TrkScrollSpeed.Value;
		}

		private void RbNormGain_CheckedChanged(object sender, EventArgs e) {
			EnableNormalize = RbNormGain.Checked;
			TrkDispMax.Enabled = false;
			ParentForm.DrawBackground();
		}

		private void RbAutoGain_CheckedChanged(object sender, EventArgs e) {
			EnableAutoGain = RbAutoGain.Checked;
			TrkDispMax.Enabled = false;
			ParentForm.DrawBackground();
		}

		private void RbGainNone_CheckedChanged(object sender, EventArgs e) {
			TrkDispMax.Enabled = true;
			ParentForm.DrawBackground();
		}

		private void CmbOutput_SelectedIndexChanged(object sender, EventArgs e) {
			ParentForm.Playback.SetDevice((uint)(CmbOutput.SelectedIndex - 1));
		}

		private void CmbInput_SelectedIndexChanged(object sender, EventArgs e) {
			ParentForm.Record.SetDevice((uint)(CmbInput.SelectedIndex - 1));
		}

		void Initialize() {
			TrkSpeed.Minimum = -OCT_DIV;
			TrkSpeed.Maximum = OCT_DIV;
			TrkSpeed.TickFrequency = OCT_DIV;
			CmbOutput.Items.Clear();
			var outDevices = WaveOut.GetDeviceList();
			if (0 == outDevices.Count) {
				CmbOutput.Enabled = false;
			}
			else {
				CmbOutput.Enabled = true;
				CmbOutput.Items.Add("既定のデバイス");
				foreach (var caps in outDevices) {
					CmbOutput.Items.Add(caps.szPname);
				}
				CmbOutput.SelectedIndex = (int)ParentForm.Playback.DeviceId + 1;
			}
			CmbInput.Items.Clear();
			var inDevices = WaveIn.GetDeviceList();
			if (0 == inDevices.Count) {
				CmbInput.Enabled = false;
			}
			else {
				CmbInput.Enabled = true;
				CmbInput.Items.Add("既定のデバイス");
				foreach (var caps in inDevices) {
					CmbInput.Items.Add(caps.szPname);
				}
				CmbInput.SelectedIndex = (int)ParentForm.Record.DeviceId + 1;
			}
		}

		void ChangeKeySpeed() {
			SpeedCent = 1200 * TrkSpeed.Value / OCT_DIV;
			KeyCent = TrkKey.Value * 100;
			ParentForm.Playback.File.Speed = Speed;
			ParentForm.Playback.Spectrum.Transpose = Transpose;
			var pitchShift = Transpose + TrkKey.Value;
			ParentForm.Playback.Spectrum.Pitch = Math.Pow(2.0, pitchShift / 12.0);
			Drawer.KeyboardShift = (int)(pitchShift + 0.5 * Math.Sign(pitchShift));
			ParentForm.DrawBackground();
			DispValue();
		}

		void DispValue() {
			GrbKey.Text = $"キー:{TrkKey.Value}半音";
			GrbSpeed.Text = $"速さ:{Speed:0.0%}";
			GrbDisplaySettings.Text = $"表示幅:{-TrkDispRange.Value}db";
			RbGainNone.Text = $"最大値指定({-TrkDispMax.Value}db)";
		}

		public static void Save() {
			var xml = new XmlDocument();
			var root = xml.CreateElement("settings");
			var player = xml.CreateElement("player");
			SavePlayer(xml, player);
			root.AppendChild(player);
			var display = xml.CreateElement("display");
			SaveDisplay(xml, display);
			root.AppendChild(display);
			xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
			xml.AppendChild(root);
			xml.Save(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "settings.xml"));
		}

		public static new void Load() {
			var path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "settings.xml");
			if (!File.Exists(path)) {
				return;
			}
			var xml = new XmlDocument();
			xml.Load(path);
			foreach (var root in xml.ChildNodes) {
				if (root is XmlElement settings && settings.Name == "settings") {
					foreach (var child in settings.ChildNodes) {
						if (child is XmlElement elm) {
							if (elm.Name == "player") {
								LoadPlayer(elm);
							}
							if (elm.Name == "display") {
								LoadDisplay(elm);
							}
						}
					}
				}
			}
		}

		static void SavePlayer(XmlDocument xml, XmlElement elm) {
			{
				var key = xml.CreateElement("key");
				var value = xml.CreateAttribute("value");
				value.Value = $"{KeyCent}";
				key.Attributes.Append(value);
				elm.AppendChild(key);
			}
			{
				var speed = xml.CreateElement("speed");
				var value = xml.CreateAttribute("value");
				value.Value = $"{SpeedCent}";
				speed.Attributes.Append(value);
				elm.AppendChild(speed);
			}
		}

		static void LoadPlayer(XmlElement elm) {
			foreach (var child in elm.ChildNodes) {
				if (child is XmlElement col) {
					switch (col.Name) {
					case "key":
						KeyCent = int.Parse(col.GetAttribute("value"));
						break;
					case "speed":
						SpeedCent = int.Parse(col.GetAttribute("value"));
						break;
					}
				}
			}
			var pitchShift = Transpose + KeyCent / 100;
			Drawer.KeyboardShift = (int)(pitchShift + 0.5 * Math.Sign(pitchShift));
		}

		static void SaveDisplay(XmlDocument xml, XmlElement elm) {
			{
				var range = xml.CreateElement("range");
				var value = xml.CreateAttribute("value");
				value.Value = $"{Drawer.MinDb}";
				range.Attributes.Append(value);
				elm.AppendChild(range);
			}
			{
				var gain = xml.CreateElement("gain");
				var type = xml.CreateAttribute("type");
				gain.Attributes.Append(type);
				if (EnableAutoGain) {
					type.Value = "auto";
				} else if (EnableNormalize) {
					type.Value = "normalize";
				} else {
					type.Value = "offset";
					var value = xml.CreateAttribute("value");
					value.Value = $"{Drawer.OffsetDb}";
					gain.Attributes.Append(value);
				}
				elm.AppendChild(gain);
			}
			{
				if (DisplayCurve) {
					elm.AppendChild(xml.CreateElement("curve"));
				}
				if (DisplayPeak) {
					elm.AppendChild(xml.CreateElement("peak"));
				}
				if (DisplayThreshold) {
					elm.AppendChild(xml.CreateElement("threshold"));
				}
				if (DisplayFreq) {
					elm.AppendChild(xml.CreateElement("freq"));
				} else {
					elm.AppendChild(xml.CreateElement("note"));
				}
			}
			{
				var scroll = xml.CreateElement("scroll");
				var value = xml.CreateAttribute("value");
				if (DisplayScroll) {
					value.Value = $"{ScrollSpeed}";
				} else {
					value.Value = "0";
				}
				scroll.Attributes.Append(value);
				elm.AppendChild(scroll);
			}
		}

		static void LoadDisplay(XmlElement elm) {
			DisplayCurve = false;
			DisplayPeak = false;
			DisplayThreshold = false;
			foreach (var child in elm.ChildNodes) {
				if (child is XmlElement col) {
					switch (col.Name) {
					case "range":
						Drawer.MinDb = int.Parse(col.GetAttribute("value"));
						break;
					case "gain":
						switch (col.GetAttribute("type")) {
						case "auto":
							EnableAutoGain = true;
							EnableNormalize = false;
							break;
						case "normalize":
							EnableAutoGain = false;
							EnableNormalize = true;
							break;
						case "offset":
							EnableAutoGain = false;
							EnableNormalize = false;
							Drawer.OffsetDb = int.Parse(col.GetAttribute("value"));
							break;
						}
						break;
					case "curve":
						DisplayCurve = true;
						break;
					case "peak":
						DisplayPeak = true;
						break;
					case "threshold":
						DisplayThreshold = true;
						break;
					case "freq":
						DisplayFreq = true;
						break;
					case "note":
						DisplayFreq = false;
						break;
					case "scroll":
						ScrollSpeed = int.Parse(col.GetAttribute("value"));
						DisplayScroll = ScrollSpeed > 0;
						if (!DisplayScroll) {
							ScrollSpeed = 1;
						}
						break;
					}
				}
			}
		}
	}
}
