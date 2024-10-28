using System;
using System.Windows.Forms;

using WinMM;
using static Spectrum.Spectrum;

namespace SpectrumAnalyzer.Forms {
	public partial class Settings : Form {
		public static double Speed { get; set; } = 1.0;

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
			}
			if (Instance.Visible) {
				return;
			}
			var key = Math.Log(parent.Playback.Osc.Pitch * Speed, 2.0) * 12;
			Instance.TrkKey.Value = (int)(key + 0.5 * Math.Sign(key));
			Instance.GrbSpeed.Enabled = parent.Playback.Playing;
			Instance.TrkSpeed.Value = (int)(Math.Log(Speed, 2.0) * OCT_DIV);
			Instance.TrkDispRange.Value = Drawer.MinDb;
			Instance.TrkDispMax.Value = Drawer.OffsetDb;
			Instance.ChkCurve.Checked = Drawer.DisplayCurve;
			Instance.ChkPeak.Checked = Drawer.DisplayPeak;
			Instance.ChkScroll.Checked = Drawer.DisplayScroll;
			Instance.ChkFreq.Checked = Drawer.DisplayFreq;
			Instance.RbAutoGain.Checked = Drawer.AutoGain;
			Instance.RbNormGain.Checked = Drawer.NormGain;
			Instance.RbGainNone.Checked = !(Drawer.AutoGain || Drawer.NormGain);
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

		private void ChkScroll_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayScroll = ChkScroll.Checked;
			ParentForm.DrawBackground();
		}

		private void ChkFreq_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayFreq = ChkFreq.Checked;
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

		private void RbNormGain_CheckedChanged(object sender, EventArgs e) {
			Drawer.NormGain = RbNormGain.Checked;
			TrkDispMax.Enabled = false;
			ParentForm.DrawBackground();
		}

		private void RbAutoGain_CheckedChanged(object sender, EventArgs e) {
			Drawer.AutoGain = RbAutoGain.Checked;
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
				foreach (var dev in outDevices) {
					CmbOutput.Items.Add(dev);
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
				foreach (var dev in inDevices) {
					CmbInput.Items.Add(dev);
				}
				CmbInput.SelectedIndex = (int)ParentForm.Record.DeviceId + 1;
			}
		}

		void ChangeKeySpeed() {
			var transpose = (double)TrkSpeed.Value / HALFTONE_DIV;
			var key = TrkKey.Value;
			var pitchShift = key - transpose;
			Speed = Math.Pow(2.0, transpose / 12.0);
			ParentForm.Playback.File.Speed = Speed;
			ParentForm.Playback.Spectrum.Transpose = -transpose;
			ParentForm.Playback.Osc.Pitch = Math.Pow(2.0, pitchShift / 12.0);
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
	}
}
