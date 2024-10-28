using System;
using System.Windows.Forms;

using WinMM;
using static Spectrum.Spectrum;

namespace SpectrumAnalyzer {
	public partial class SettingsForm : Form {
		static SettingsForm mInstance;

		MainForm mParent;

		public static double Speed { get; set; } = 1.0;

		public static void Open(MainForm parent) {
			if (null == mInstance) {
				mInstance = new SettingsForm(parent);
				mInstance.Initialize();
				mInstance.SetKeySpeed();
			}
			if (mInstance.Visible) {
				return;
			}
			var key = Math.Log(parent.Playback.Osc.Pitch * Speed, 2.0) * 12;
			mInstance.TrkKey.Value = (int)(key + 0.5 * Math.Sign(key));
			mInstance.GrbSpeed.Enabled = parent.Playback.Playing;
			mInstance.TrkSpeed.Value = (int)(Math.Log(Speed, 2.0) * OCT_DIV);
			mInstance.TrkDispRange.Value = Drawer.MinLevel;
			mInstance.TrkDispMax.Value = Drawer.OffsetDb;
			mInstance.ChkCurve.Checked = Drawer.DisplayCurve;
			mInstance.ChkPeak.Checked = Drawer.DisplayPeak;
			mInstance.ChkScroll.Checked = Drawer.DisplayScroll;
			mInstance.RbAutoGain.Checked = Drawer.AutoGain;
			mInstance.RbNormGain.Checked = Drawer.NormGain;
			mInstance.RbGainNone.Checked = !mInstance.RbAutoGain.Checked && !mInstance.RbNormGain.Checked;
			mInstance.RbGainNone.Text = "最大値指定(" + (-Drawer.OffsetDb) + "db)";
			mInstance.Visible = true;
			mInstance.Location = parent.Location;
			mInstance.DispValue();
		}

		SettingsForm(MainForm fm) {
			InitializeComponent();
			mParent = fm;
		}

		private void Settings_FormClosing(object sender, FormClosingEventArgs e) {
			e.Cancel = true;
			Visible = false;
		}

		private void TrkKey_Scroll(object sender, EventArgs e) {
			SetKeySpeed();
			DispValue();
		}

		private void TrkSpeed_Scroll(object sender, EventArgs e) {
			SetKeySpeed();
			DispValue();
		}

		private void ChkCurve_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayCurve = ChkCurve.Checked;
		}

		private void ChkPeak_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayPeak = ChkPeak.Checked;
		}

		private void ChkScroll_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayScroll = ChkScroll.Checked;
			mParent.SetLayout();
		}

		private void TrkDispRange_Scroll(object sender, EventArgs e) {
			Drawer.MinLevel = TrkDispRange.Value;
			SetKeySpeed();
			DispValue();
		}

		private void TrkDispMax_Scroll(object sender, EventArgs e) {
			Drawer.OffsetDb = TrkDispMax.Value;
			RbGainNone.Text = "最大値指定(" + (-TrkDispMax.Value) + "db)";
			SetKeySpeed();
			DispValue();
		}

		private void RbNormGain_CheckedChanged(object sender, EventArgs e) {
			Drawer.NormGain = RbNormGain.Checked;
			TrkDispMax.Enabled = false;
			mParent.DrawBackground();
		}

		private void RbAutoGain_CheckedChanged(object sender, EventArgs e) {
			Drawer.AutoGain = RbAutoGain.Checked;
			TrkDispMax.Enabled = false;
			mParent.DrawBackground();
		}

		private void RbGainNone_CheckedChanged(object sender, EventArgs e) {
			TrkDispMax.Enabled = true;
			mParent.DrawBackground();
		}

		private void CmbOutput_SelectedIndexChanged(object sender, EventArgs e) {
			mParent.Playback.SetDevice((uint)(CmbOutput.SelectedIndex - 1));
		}

		private void CmbInput_SelectedIndexChanged(object sender, EventArgs e) {
			mParent.Record.SetDevice((uint)(CmbInput.SelectedIndex - 1));
		}

		void Initialize() {
			TrkSpeed.Minimum = -OCT_DIV;
			TrkSpeed.Maximum = OCT_DIV;
			TrkSpeed.TickFrequency = OCT_DIV;
			CmbOutput.Items.Clear();
			var outDevices = WaveOut.GetDeviceList();
			if (0 == outDevices.Count) {
				CmbOutput.Enabled = false;
			} else {
				CmbOutput.Enabled = true;
				CmbOutput.Items.Add("既定のデバイス");
				foreach (var dev in outDevices) {
					CmbOutput.Items.Add(dev);
				}
				CmbOutput.SelectedIndex = (int)mParent.Playback.DeviceId + 1;
			}
			CmbInput.Items.Clear();
			var inDevices = WaveIn.GetDeviceList();
			if (0 == inDevices.Count) {
				CmbInput.Enabled = false;
			} else {
				CmbInput.Enabled = true;
				CmbInput.Items.Add("既定のデバイス");
				foreach (var dev in inDevices) {
					CmbInput.Items.Add(dev);
				}
				CmbInput.SelectedIndex = (int)mParent.Record.DeviceId + 1;
			}
		}

		void SetKeySpeed() {
			var transpose = (double)TrkSpeed.Value / HALFTONE_DIV;
			var key = TrkKey.Value;
			var pitchShift = key - transpose;
			Speed = Math.Pow(2.0, transpose / 12.0);
			mParent.Playback.File.Speed = Speed;
			mParent.Playback.Spectrum.Transpose = -transpose;
			mParent.Playback.Osc.Pitch = Math.Pow(2.0, pitchShift / 12.0);
			Drawer.KeyboardShift = (int)(pitchShift + 0.5 * Math.Sign(pitchShift));
			mParent.DrawBackground();
		}

		void DispValue() {
			GrbKey.Text = string.Format("キー:{0}半音", TrkKey.Value);
			GrbSpeed.Text = string.Format("速さ:{0}", Speed.ToString("0.0%"));
			GrbDisplaySettings.Text = string.Format("表示幅:{0}db", -TrkDispRange.Value);
		}
	}
}
