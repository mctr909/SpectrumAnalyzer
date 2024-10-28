using System;
using System.Windows.Forms;

using WinMM;
using Spectrum;

namespace SpectrumAnalyzer {
	public partial class SettingsForm : Form {
		static SettingsForm mInstance;

		MainForm mParent;

		public static double Speed { get; set; } = 1.0;

		public static readonly double BASE_FREQ = 440 * Math.Pow(2.0, 3 / 12.0 - 5);

		public const int OFS_DB = -12;
		public const double OFS_GAIN = 3.981;

		public static void Open(MainForm parent) {
			if (null == mInstance) {
				mInstance = new SettingsForm(parent);
				mInstance.Initialize();
				mInstance.SetKeySpeed();
			}
			if (mInstance.Visible) {
				return;
			}
			var key = Math.Log(Settings.Pitch * Speed, 2.0) * 12;
			mInstance.TrkKey.Value = (int)(key + 0.5 * Math.Sign(key));
			mInstance.GrbSpeed.Enabled = parent.Playback.Playing;
			mInstance.TrkSpeed.Value = (int)(Math.Log(Speed, 2.0) * Settings.OCT_DIV);
			mInstance.TrkMinLevel.Value = Drawer.MinLevel;
			mInstance.ChkCurve.Checked = Drawer.DisplayCurve;
			mInstance.ChkPeak.Checked = Drawer.DisplayPeak;
			mInstance.ChkThreshold.Checked = Drawer.DisplayThreshold;
			mInstance.ChkScroll.Checked = Drawer.DisplayScroll;
			mInstance.RbAutoGain.Checked = Settings.AutoGain;
			mInstance.RbNormGain.Checked = Settings.NormGain;
			mInstance.RbGainNone.Checked = !mInstance.RbAutoGain.Checked && !mInstance.RbNormGain.Checked;
			mInstance.RbGainNone.Text = "+" + -OFS_DB + "db";
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

		private void ChkThreshold_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayThreshold = ChkThreshold.Checked;
		}

		private void ChkScroll_CheckedChanged(object sender, EventArgs e) {
			Drawer.DisplayScroll = ChkScroll.Checked;
			mParent.SetLayout();
		}

		private void TrkMinLevel_Scroll(object sender, EventArgs e) {
			Drawer.MinLevel = TrkMinLevel.Value;
			SetKeySpeed();
			DispValue();
		}

		private void RbNormGain_CheckedChanged(object sender, EventArgs e) {
			Settings.NormGain = RbNormGain.Checked;
			mParent.DrawBackground();
		}

		private void RbAutoGain_CheckedChanged(object sender, EventArgs e) {
			Settings.AutoGain = RbAutoGain.Checked;
			mParent.DrawBackground();
		}

		private void RbGainNone_CheckedChanged(object sender, EventArgs e) {
			mParent.DrawBackground();
		}

		private void CmbOutput_SelectedIndexChanged(object sender, EventArgs e) {
			mParent.Playback.SetDevice((uint)(CmbOutput.SelectedIndex - 1));
		}

		private void CmbInput_SelectedIndexChanged(object sender, EventArgs e) {
			mParent.Record.SetDevice((uint)(CmbInput.SelectedIndex - 1));
		}

		void Initialize() {
			TrkSpeed.Minimum = -Settings.OCT_DIV;
			TrkSpeed.Maximum = Settings.OCT_DIV;
			TrkSpeed.TickFrequency = Settings.OCT_DIV;
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
			var transpose = (double)TrkSpeed.Value / Settings.HALFTONE_DIV;
			var key = TrkKey.Value;
			var pitchShift = key - transpose;
			Settings.Transpose = -transpose;
			Speed = Math.Pow(2.0, transpose / 12.0);
			mParent.Playback.File.Speed = Speed;
			Settings.Pitch = Math.Pow(2.0, pitchShift / 12.0);
			Drawer.KeyboardShift = (int)(pitchShift + 0.5 * Math.Sign(pitchShift));
			mParent.DrawBackground();
		}

		void DispValue() {
			GrbKey.Text = string.Format("キー:{0}半音", TrkKey.Value);
			GrbSpeed.Text = string.Format("速さ:{0}", Speed.ToString("0.0%"));
			GrbMinLevel.Text = string.Format("表示範囲:{0}db", -TrkMinLevel.Value);
		}
	}
}
