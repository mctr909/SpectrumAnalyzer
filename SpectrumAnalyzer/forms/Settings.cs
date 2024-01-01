using System;
using System.Windows.Forms;

using WINMM;

namespace SpectrumAnalyzer {
	public partial class Settings : Form {
		static Settings mInstance;

		MainForm mParent;

		public const int NOTE_COUNT = 126;
		public static readonly double BASE_FREQ = 440 * Math.Pow(2.0, 3 / 12.0 - 5);

		public static void Open(MainForm parent) {
			if (null == mInstance) {
				mInstance = new Settings(parent);
				mInstance.Initialize();
				mInstance.SetKeySpeed();
			}
			if (mInstance.Visible) {
				return;
			}
			var key = Math.Log(OscBank.Pitch * parent.Playback.Speed, 2.0) * 12;
			mInstance.TrkKey.Value = (int)(key + 0.5 * Math.Sign(key));
			mInstance.GrbSpeed.Enabled = parent.Playback.Enabled;
			mInstance.TrkSpeed.Value = (int)(10 * Math.Log(parent.Playback.Speed, 2.0) * 12 * Spectrum.TONE_DIV);
			mInstance.TrkMinLevel.Value = Drawer.MinLevel;
			mInstance.TrkResponce.Value = (int)(0.5 + 10 * Math.Log(Spectrum.ResponceSpeed, 2));
			mInstance.ChkCurve.Checked = Drawer.DisplayCurve;
			mInstance.ChkPeak.Checked = Drawer.DisplayPeak;
			mInstance.ChkThreshold.Checked = Drawer.DisplayThreshold;
			mInstance.ChkScroll.Checked = Drawer.DisplayScroll;
			mInstance.RbAutoGain.Checked = Spectrum.AutoGain;
			mInstance.RbNormGain.Checked = Spectrum.NormGain;
			mInstance.RbGainNone.Checked = !mInstance.RbAutoGain.Checked && !mInstance.RbNormGain.Checked;
			mInstance.Visible = true;
			mInstance.Location = parent.Location;
			mInstance.DispValue();
		}

		Settings(MainForm fm) {
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

		private void TrkResponce_Scroll(object sender, EventArgs e) {
			Spectrum spectrum = null;
			if (mParent.Playback.Enabled) {
				spectrum = mParent.Playback.FilterBank;
			}
			if (mParent.Record.Enabled) {
				spectrum = mParent.Record.FilterBank;
			}
			if (null == spectrum) {
				return;
			}
			Spectrum.ResponceSpeed = Math.Pow(2, 0.1 * TrkResponce.Value);
			spectrum.SetResponceSpeed(44100);
			DispValue();
		}

		private void RbNormGain_CheckedChanged(object sender, EventArgs e) {
			Spectrum.NormGain = RbNormGain.Checked;
		}

		private void RbAutoGain_CheckedChanged(object sender, EventArgs e) {
			Spectrum.AutoGain = RbAutoGain.Checked;
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
			TrkSpeed.Minimum = -240 * Spectrum.TONE_DIV;
			TrkSpeed.Maximum = 120 * Spectrum.TONE_DIV;
			TrkSpeed.TickFrequency = 10 * Spectrum.TONE_DIV;
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
			var transpose = 0.1 * TrkSpeed.Value / Spectrum.TONE_DIV;
			var key = TrkKey.Value;
			var pitchShift = key - transpose;
			Spectrum.Transpose = -transpose;
			mParent.Playback.Speed = Math.Pow(2.0, transpose / 12.0);
			OscBank.Pitch = Math.Pow(2.0, pitchShift / 12.0);
			Drawer.KeyboardShift = (int)(pitchShift + 0.5 * Math.Sign(pitchShift));
			mParent.DrawBackground();
		}

		void DispValue() {
			GrbKey.Text = string.Format("キー:{0}半音", TrkKey.Value);
			GrbSpeed.Text = string.Format("速さ:{0}", mParent.Playback.Speed.ToString("0.0%"));
			GrbResponce.Text = string.Format("応答速度:{0}Hz", Spectrum.ResponceSpeed.ToString("g3"));
			GrbMinLevel.Text = string.Format("表示範囲:{0}db", TrkMinLevel.Value);
		}
	}
}
