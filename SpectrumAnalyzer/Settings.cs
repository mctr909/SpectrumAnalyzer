using System;
using System.Windows.Forms;

namespace SpectrumAnalyzer {
	public partial class Settings : Form {
		public static Settings Instance;

		MainForm mMain;

		public Settings(MainForm fm) {
			InitializeComponent();
			mMain = fm;
		}

		private void Settings_FormClosing(object sender, FormClosingEventArgs e) {
			e.Cancel = true;
			Visible = false;
		}

		private void Settings_Load(object sender, EventArgs e) {
			TrkKey.Value = (int)(Math.Log(OscBank.Pitch * mMain.Playback.Speed, 2.0) * 120);
			TrkSpeed.Value = (int)(Math.Log(mMain.Playback.Speed, 2.0) * 12);
			GrbSpeed.Enabled = mMain.Playback.Enabled;
			TrkThresholdHigh.Value = Spectrum.ThresholdHigh / 3;
			TrkThresholdOffset.Value = (int)(200 * Math.Log10(Spectrum.ThresholdOffset));
			ChkThreshold.Checked = Drawer.DisplayThreshold;
			TrkMinLevel.Value = Drawer.MinLevel;
			RbAutoGain.Checked = Spectrum.AutoGain;
			RbGain15.Checked = Drawer.ShiftGain != 0;
			RbGainNone.Checked = !RbAutoGain.Checked && !RbGain15.Checked;
			var outDevices = WaveOut.GetDeviceList();
			if (0 == outDevices.Count) {
				CmbOutput.Enabled = false;
			} else {
				CmbOutput.Enabled = true;
				CmbOutput.Items.Add("既定のデバイス");
				foreach (var dev in outDevices) {
					CmbOutput.Items.Add(dev);
				}
				CmbOutput.SelectedIndex = (int)mMain.Playback.DeviceId + 1;
			}
			var inDevices = WaveIn.GetDeviceList();
			if (0 == inDevices.Count) {
				CmbInput.Enabled = false;
			} else {
				CmbInput.Enabled = true;
				CmbInput.Items.Add("既定のデバイス");
				foreach (var dev in inDevices) {
					CmbInput.Items.Add(dev);
				}
				CmbInput.SelectedIndex = (int)mMain.Record.DeviceId + 1;
			}
			setting();
		}

		private void TrkKey_Scroll(object sender, EventArgs e) {
			setting();
		}

		private void TrkSpeed_Scroll(object sender, EventArgs e) {
			Spectrum.Transpose = -TrkSpeed.Value;
			mMain.Playback.Speed = Math.Pow(2.0, TrkSpeed.Value / 12.0);
			setting();
		}

		private void TrkThresholdHigh_Scroll(object sender, EventArgs e) {
			Spectrum.ThresholdHigh = TrkThresholdHigh.Value * 3;
			setting();
		}

		private void TrkThresholdOffset_Scroll(object sender, EventArgs e) {
			Spectrum.ThresholdOffset = Math.Pow(10, TrkThresholdOffset.Value / 200.0);
			setting();
		}

		private void ChkThreshold_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayThreshold = ChkThreshold.Checked;
		}

		private void TrkMinLevel_Scroll(object sender, EventArgs e) {
			Drawer.MinLevel = TrkMinLevel.Value;
			setting();
		}

		private void RbGain15_CheckedChanged(object sender, EventArgs e) {
			Drawer.ShiftGain = RbGain15.Checked ? 15 : 0;
		}

		private void RbNormGain_CheckedChanged(object sender, EventArgs e) {
			Spectrum.NormGain = RbNormGain.Checked;
		}

		private void RbAutoGain_CheckedChanged(object sender, EventArgs e) {
			Spectrum.AutoGain = RbAutoGain.Checked;
		}

		private void CmbOutput_SelectedIndexChanged(object sender, EventArgs e) {
			mMain.Playback.SetDevice((uint)(CmbOutput.SelectedIndex - 1));
		}

		private void CmbInput_SelectedIndexChanged(object sender, EventArgs e) {
			mMain.Record.SetDevice((uint)(CmbInput.SelectedIndex - 1));
		}

		void setting() {
			var shift = TrkKey.Value / 10.0 - TrkSpeed.Value;
			Drawer.KeyboardShift = (int)(shift + 0.5 * Math.Sign(shift));
			OscBank.Pitch = Math.Pow(2.0, TrkKey.Value / 120.0) / mMain.Playback.Speed;
			mMain.DrawBackground();
			GrbKey.Text = string.Format("キー:{0}半音", (TrkKey.Value * 0.1).ToString("0.0"));
			GrbSpeed.Text = string.Format("速さ:{0}", mMain.Playback.Speed.ToString("0%"));
			GrbThresholdHigh.Text = string.Format("閾値　幅:{0}半音　オフセット:{1}db",
				TrkThresholdHigh.Value * 2 + 1,
				(TrkThresholdOffset.Value * 0.1).ToString("0.0")
			);
			GrbMinLevel.Text = string.Format("表示範囲:{0}db", TrkMinLevel.Value);
		}
	}
}
