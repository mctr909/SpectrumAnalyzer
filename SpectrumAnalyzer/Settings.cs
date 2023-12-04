using System;
using System.Windows.Forms;

namespace SpectrumAnalyzer {
	public partial class Settings : Form {
		MainForm mMain;

		public Settings(MainForm fm) {
			InitializeComponent();
			mMain = fm;
		}

		private void Settings_Load(object sender, EventArgs e) {
			TrkSpeed.Value = (int)(Math.Log(mMain.WaveOut.Speed, 2.0) * 12);
			TrkKey.Value = (int)(Math.Log(mMain.WaveOut.Pitch * mMain.WaveOut.Speed, 2.0) * 120);
			GrbSpeed.Enabled = mMain.WaveOut.Enabled;
			TrkMinLevel.Value = Drawer.MinLevel;
			ChkAutoGain.Checked = mMain.WaveIn.FilterBank.AutoGain;
			setting();
		}

		private void TrkKey_Scroll(object sender, EventArgs e) {
			setting();
		}

		private void TrkSpeed_Scroll(object sender, EventArgs e) {
			setting();
			mMain.WaveOut.FilterBankL.Transpose = -TrkSpeed.Value;
			mMain.WaveOut.FilterBankR.Transpose = -TrkSpeed.Value;
		}

		private void TrkMinLevel_Scroll(object sender, EventArgs e) {
			Drawer.MinLevel = TrkMinLevel.Value;
			setting();
		}

		private void ChkAutoGain_CheckedChanged(object sender, EventArgs e) {
			mMain.WaveOut.FilterBankL.AutoGain = ChkAutoGain.Checked;
			mMain.WaveOut.FilterBankR.AutoGain = ChkAutoGain.Checked;
			mMain.WaveIn.FilterBank.AutoGain = ChkAutoGain.Checked;
		}

		void setting() {
			mMain.WaveOut.Speed = Math.Pow(2.0, TrkSpeed.Value / 12.0);
			mMain.WaveOut.Pitch = Math.Pow(2.0, TrkKey.Value / 120.0) / mMain.WaveOut.Speed;
			var shift = TrkKey.Value / 10.0 - TrkSpeed.Value;
			mMain.KeyboardShift = (int)(shift + 0.5 * Math.Sign(shift));
			mMain.DrawBackground();
			GrbKey.Text = "キー:" + (TrkKey.Value * 0.1).ToString("0.0");
			GrbSpeed.Text = "速さ:" + mMain.WaveOut.Speed.ToString("0%");
			GrbMinLevel.Text = "表示範囲:" + TrkMinLevel.Value + "db";
		}
	}
}
