using SignalProcess;
using static SignalProcess.BaseSpectrum;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace SpectrumAnalyzerNet.Forms;

public partial class Settings : Form {
	private readonly Main MainForm;

	public static bool EnablePeak { get; set; } = false;
	public static bool EnableAutoGain { get; set; } = true;
	public static bool EnableNormalize { get; set; } = false;
	public static bool DisplayCurve { get; set; } = true;
	public static bool DisplayPeak { get; set; } = true;
	public static bool DisplayThreshold { get; set; } = false;
	public static bool DisplayFreq { get; set; } = true;
	public static int ScrollSpeed { get; set; } = 2;
	public static int DisplayRangeDb { get; set; } = -30;
	public static int DisplayMaxDb { get; set; } = -12;
	public static int KeyShift { get; set; } = 0;

	public Settings(Main mainForm) {
		InitializeComponent();
		MainForm = mainForm;

		var key = Math.Log(mainForm.Playback.Osc.Pitch * mainForm.Playback.Speed, 2.0) * 12;
		trbKey.Value = (int)(key + 0.5 * Math.Sign(key));
		grpSpeed.Enabled = mainForm.Playback.IsPlaying;
		trbSpeed.Minimum = -OCT_DIV;
		trbSpeed.Maximum = OCT_DIV;
		trbSpeed.TickFrequency = OCT_DIV;
		trbSpeed.Value = (int)(Math.Log(mainForm.Playback.Speed, 2.0) * OCT_DIV);
		trbDispRange.Value = -DisplayRangeDb;
		trbGain.Value = -DisplayMaxDb;
		chkCurve.Checked = DisplayCurve;
		chkPeak.Checked = DisplayPeak;
		chkThreshold.Checked = DisplayThreshold;
		//chkScroll.Checked = DisplayScroll;
		rbAuto.Checked = EnableAutoGain;
		rbNorm.Checked = EnableNormalize;
		rbManual.Checked = !(EnableAutoGain || EnableNormalize);
		DispValue();
	}

	private void trbKey_Scroll(object sender, EventArgs e) {
		ChangeKeySpeed();
	}

	private void trbSpeed_Scroll(object sender, EventArgs e) {
		ChangeKeySpeed();
	}

	private void trbDispRange_Scroll(object sender, EventArgs e) {
		DisplayRangeDb = -trbDispRange.Value;
		DispValue();
		MainForm.DrawBackground();
	}

	private void trbGain_Scroll(object sender, EventArgs e) {
		DisplayMaxDb = -trbGain.Value;
		DispValue();
		MainForm.DrawBackground();
	}

	private void DisplayTarget_CheckedChanged(object sender, EventArgs e) {
		switch (((CheckBox)sender).Name) {
		case nameof(chkCurve):
			DisplayCurve = chkCurve.Checked;
			break;
		case nameof(chkPeak):
			DisplayPeak = chkPeak.Checked;
			break;
		case nameof(chkThreshold):
			DisplayThreshold = chkThreshold.Checked;
			break;
		case nameof(chkScroll):
			break;
		}
	}

	private void AutoGain_CheckedChanged(object sender, EventArgs e) {
		switch (((RadioButton)sender).Name) {
		case nameof(rbAuto):
			EnableAutoGain = rbAuto.Checked;
			trbGain.Enabled = false;
			MainForm.DrawBackground();
			break;
		case nameof(rbNorm):
			EnableNormalize = rbNorm.Checked;
			trbGain.Enabled = false;
			MainForm.DrawBackground();
			break;
		case nameof(rbManual):
			trbGain.Enabled = true;
			MainForm.DrawBackground();
			break;
		}
	}

	private void ChangeKeySpeed() {
		var transpose = (double)trbSpeed.Value / HALFTONE_DIV;
		var key = trbKey.Value;
		var pitchShift = key - transpose;
		var speed = Math.Pow(2.0, transpose / 12.0);
		MainForm.Playback.Speed = speed;
		MainForm.Playback.File.Speed = speed;
		MainForm.Playback.Osc.Pitch = Math.Pow(2.0, pitchShift / 12.0);
		KeyShift = (int)(pitchShift + 0.5 * Math.Sign(pitchShift));
		MainForm.DrawBackground();
		DispValue();
	}

	private void DispValue() {
		grpKey.Text = $"キー：{trbKey.Value}半音";
		grpSpeed.Text = $"スピード：{MainForm.Playback.Speed:0.0%}";
		grpDispRange.Text = $"表示範囲：{trbDispRange.Value}dB";
		rbManual.Text = $"手動：{-trbGain.Value}dB";
	}
}
