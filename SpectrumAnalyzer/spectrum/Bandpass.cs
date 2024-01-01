using System;

public class Bandpass {
	public readonly double Kb0;
	public readonly double Ka1;
	public readonly double Ka2;
	public readonly double Delta;
	public readonly double Alpha;

	public double DisplayAlpha { get; private set; }

	public double La1;
	public double La2;
	public double Lb1;
	public double Lb2;
	public double L;
	public double DisplayL;

	public double Ra1;
	public double Ra2;
	public double Rb1;
	public double Rb2;
	public double R;
	public double DisplayR;

	const double MIN_WIDTH = 1.125;
	const double MIN_WIDTH_AT_FREQ = 660.0;

	public Bandpass(int sampleRate, double frequency) {
		var halfToneWidth = MIN_WIDTH + Math.Log(MIN_WIDTH_AT_FREQ / frequency, 2.0);
		if (halfToneWidth < MIN_WIDTH) {
			halfToneWidth = MIN_WIDTH;
		}
		Delta = frequency / sampleRate;
		var omega = 2 * Math.PI * Delta;
		var s = Math.Sin(omega);
		var x = Math.Log(2) / 4 * halfToneWidth / 12.0 * omega / s;
		Alpha = s * Math.Sinh(x);
		DisplayAlpha = Alpha;
		var a0 = 1.0 + Alpha;
		Kb0 = Alpha / a0;
		Ka1 = -2.0 * Math.Cos(omega) / a0;
		Ka2 = (1.0 - Alpha) / a0;
	}

	public void SetResponceSpeed(int sampleRate, double responceSpeed) {
		responceSpeed *= 4;
		var frequency = 0.5 * Alpha * sampleRate;
		if (frequency > responceSpeed) {
			DisplayAlpha = responceSpeed / sampleRate;
		}
		else {
			DisplayAlpha = 0.5 * Alpha;
		}
	}
}
