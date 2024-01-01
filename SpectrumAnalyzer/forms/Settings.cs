using System;
using System.Windows.Forms;

using WinMM;
using SignalProcess;
using static SignalProcess.Spectrum;

namespace SpectrumAnalyzer.Forms {
	public partial class Settings : Form {
		public static double Speed { get; set; } = 1.0;

		private static Settings Instance;

		private Main MainForm;
		private WaveSynth WaveSynth;

		private Settings(Main form) {
			InitializeComponent();
			MainForm = form;
		}

		public static void SetInstance(Main form) {
			if (Instance == null) {
				Instance = new Settings(form);
				Instance.Initialize();
			}
		}

		public static void Open() {
			if (Instance.Visible) {
				return;
			}

			var parent = Instance.MainForm;
			var waveSynth = parent.Playback.Osc;
			Instance.WaveSynth = waveSynth;

			var key = Math.Log(waveSynth.Pitch * Speed, 2.0) * 12;
			Instance.TrkKey.Value = (int)(key + 0.5 * Math.Sign(key));
			Instance.GrbSpeed.Enabled = parent.Playback.IsPlaying;
			Instance.TrkSpeed.Value = (int)(Math.Log(Speed, 2.0) * OCT_DIV);
			Instance.TrkDispRange.Value = Drawer.DisplayRangeDb;
			Instance.TrkDispMax.Value = -Drawer.DisplayMaxDb;
			Instance.TrkScrollSpeed.Enabled = Drawer.DisplayScroll;
			Instance.TrkScrollSpeed.Value = Drawer.ScrollSpeed;
			Instance.ChkCurve.Checked = Drawer.DisplayCurve;
			Instance.ChkPeak.Checked = Drawer.DisplayPeak;
			Instance.ChkThreshold.Checked = Drawer.DisplayThreshold;
			Instance.ChkFreq.Checked = Drawer.DisplayFreq;
			Instance.ChkScroll.Checked = Drawer.DisplayScroll;
			Instance.RbAutoGain.Checked = Drawer.EnableAutoGain;
			Instance.RbNormGain.Checked = Drawer.EnableNormalize;
			Instance.RbGainNone.Checked = !(Drawer.EnableAutoGain || Drawer.EnableNormalize);
			Instance.Visible = true;
			Instance.Location = parent.Location;
			Instance.DispValue();
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
			Drawer.DisplayCurve = ChkCurve.Checked;
		}

		private void ChkPeak_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayPeak = ChkPeak.Checked;
		}

		private void ChkThreshold_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayThreshold = ChkThreshold.Checked;
		}

		private void ChkFreq_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayFreq = ChkFreq.Checked;
			MainForm.DrawBackground();
		}

		private void ChkScroll_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayScroll = ChkScroll.Checked;
			TrkScrollSpeed.Enabled = Drawer.DisplayScroll;
			MainForm.ResizeCanvas();
		}

		private void TrkDispRange_Scroll(object sender, EventArgs e) {
			Drawer.DisplayRangeDb = TrkDispRange.Value;
			DispValue();
			MainForm.DrawBackground();
		}

		private void TrkDispMax_Scroll(object sender, EventArgs e) {
			Drawer.DisplayMaxDb = -TrkDispMax.Value;
			DispValue();
			MainForm.DrawBackground();
		}

		private void TrkScrollSpeed_Scroll(object sender, EventArgs e) {
			Drawer.ScrollSpeed = TrkScrollSpeed.Value;
		}

		private void RbNormGain_CheckedChanged(object sender, EventArgs e) {
			Drawer.EnableNormalize = RbNormGain.Checked;
			TrkDispMax.Enabled = false;
			MainForm.DrawBackground();
		}

		private void RbAutoGain_CheckedChanged(object sender, EventArgs e) {
			Drawer.EnableAutoGain = RbAutoGain.Checked;
			TrkDispMax.Enabled = false;
			MainForm.DrawBackground();
		}

		private void RbGainNone_CheckedChanged(object sender, EventArgs e) {
			TrkDispMax.Enabled = true;
			MainForm.DrawBackground();
		}

		private void CmbOutput_SelectedIndexChanged(object sender, EventArgs e) {
			MainForm.Playback.SetDevice((uint)(CmbOutput.SelectedIndex - 1));
		}

		private void CmbInput_SelectedIndexChanged(object sender, EventArgs e) {
			MainForm.Record.SetDevice((uint)(CmbInput.SelectedIndex - 1));
		}

		private void Initialize() {
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
				CmbOutput.SelectedIndex = (int)MainForm.Playback.DeviceId + 1;
			}
			CmbOutput.SelectedIndexChanged += new EventHandler(CmbOutput_SelectedIndexChanged);

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
				CmbInput.SelectedIndex = (int)MainForm.Record.DeviceId + 1;
			}
			CmbInput.SelectedIndexChanged += new EventHandler(CmbInput_SelectedIndexChanged);
		}

		private void ChangeKeySpeed() {
			var transpose = (double)TrkSpeed.Value / HALFTONE_DIV;
			var key = TrkKey.Value;
			var pitchShift = key - transpose;
			Speed = Math.Pow(2.0, transpose / 12.0);
			MainForm.Playback.File.Speed = Speed;
			WaveSynth.Pitch = Math.Pow(2.0, pitchShift / 12.0);
			Drawer.KeyShift = (int)(pitchShift + 0.5 * Math.Sign(pitchShift));
			MainForm.DrawBackground();
			DispValue();
		}

		private void DispValue() {
			GrbKey.Text = $"キー:{TrkKey.Value}半音";
			GrbSpeed.Text = $"速さ:{Speed:0.0%}";
			GrbDisplaySettings.Text = $"表示範囲:{-TrkDispRange.Value}db";
			RbGainNone.Text = $"最大値指定({-TrkDispMax.Value}db)";
		}
	}
}
