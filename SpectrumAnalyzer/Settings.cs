﻿using System;
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
			TrkKey.Value = (int)(Math.Log(mMain.Playback.Pitch * mMain.Playback.Speed, 2.0) * 120);
			TrkSpeed.Value = (int)(Math.Log(mMain.Playback.Speed, 2.0) * 12);
			GrbSpeed.Enabled = mMain.Playback.Enabled;
			TrkThresholdHigh.Value = Spectrum.ThresholdHigh / 3;
			TrkMinLevel.Value = Drawer.MinLevel;
			RbAutoGain.Checked = Spectrum.AutoGain;
			RbGain15.Checked = Drawer.ShiftGain != 0;
			RbGainNone.Checked = !RbAutoGain.Checked && !RbGain15.Checked;
			var outDevices = WaveOut.GetDeviceList();
			if (0 < outDevices.Count) {
				CmbOutput.Items.Add("既定のデバイス");
			}
			foreach(var dev in outDevices) {
				CmbOutput.Items.Add(dev);
			}
			if (0 < outDevices.Count) {
				CmbOutput.SelectedIndex = (int)mMain.Playback.DeviceId + 1;
			}
			var inDevices = WaveIn.GetDeviceList();
			if (0 < inDevices.Count) {
				CmbInput.Items.Add("既定のデバイス");
			}
			foreach (var dev in inDevices) {
				CmbInput.Items.Add(dev);
			}
			if (0 < inDevices.Count) {
				CmbInput.SelectedIndex = (int)mMain.Record.DeviceId + 1;
			}
			setting();
		}

		private void TrkKey_Scroll(object sender, EventArgs e) {
			setting();
		}

		private void TrkSpeed_Scroll(object sender, EventArgs e) {
			setting();
			Spectrum.Transpose = -TrkSpeed.Value;
		}

		private void TrkThresholdHigh_Scroll(object sender, EventArgs e) {
			Spectrum.ThresholdHigh = TrkThresholdHigh.Value * 3;
			setting();
		}

		private void TrkMinLevel_Scroll(object sender, EventArgs e) {
			Drawer.MinLevel = TrkMinLevel.Value;
			setting();
		}

		private void RbAutoGain_CheckedChanged(object sender, EventArgs e) {
			Spectrum.AutoGain = RbAutoGain.Checked;
		}

		private void RbGain15_CheckedChanged(object sender, EventArgs e) {
			Drawer.ShiftGain = RbGain15.Checked ? 15 : 0;
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
			mMain.Playback.Speed = Math.Pow(2.0, TrkSpeed.Value / 12.0);
			mMain.Playback.Pitch = Math.Pow(2.0, TrkKey.Value / 120.0) / mMain.Playback.Speed;
			mMain.DrawBackground();
			GrbKey.Text = "キー:" + (TrkKey.Value * 0.1).ToString("0.0半音");
			GrbSpeed.Text = "速さ:" + mMain.Playback.Speed.ToString("0%");
			GrbThresholdHigh.Text = "閾値平均幅:" + (TrkThresholdHigh.Value * 2 + 1) + "半音";
			GrbMinLevel.Text = "表示範囲:" + TrkMinLevel.Value + "db";
		}
	}
}
