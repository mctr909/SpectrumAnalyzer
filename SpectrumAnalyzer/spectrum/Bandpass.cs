using System;

public class Bandpass {
	public double Kb0 { get; private set; }
	public double Ka1 { get; private set; }
	public double Ka2 { get; private set; }
	public double PeakDelta { get; private set; }
	public double SlopeDelta { get; private set; }

	public double La1;
	public double La2;
	public double Lb1;
	public double Lb2;
	public double PeakL;
	public double SlopeL;

	public double Ra1;
	public double Ra2;
	public double Rb1;
	public double Rb2;
	public double PeakR;
	public double SlopeR;

	const double PI2 = 6.283;    // 2π
	const double LN2_4 = 0.173;  // Ln(2)/4
	const double MIN_WIDTH_AT_FREQ = 330.0;

	public void SetParam(int sampleRate, double frequency) {
		var halfToneWidth = 1 + Math.Log(MIN_WIDTH_AT_FREQ / frequency, 2.0);
		if (halfToneWidth < 1) {
			halfToneWidth = 1;
		}
		var omega = PI2 * frequency / sampleRate;
		var s = Math.Sin(omega);
		var x = LN2_4 * halfToneWidth / 12.0 * omega / s;
		var alpha = s * Math.Sinh(x);
		var a0 = 1.0 + alpha;
		Kb0 = alpha / a0;
		Ka1 = -2.0 * Math.Cos(omega) / a0;
		Ka2 = (1.0 - alpha) / a0;
		PeakDelta = frequency / sampleRate;
		SlopeDelta = PeakDelta;
	}

	public void SetResponceSpeed(int sampleRate, double responceSpeed) {
		var frequency = PeakDelta * sampleRate;
		if (frequency > responceSpeed) {
			SlopeDelta = responceSpeed / sampleRate;
		} else {
			SlopeDelta = PeakDelta;
		}
	}
}
