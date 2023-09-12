using System;
class BiQuadFilter {
	private double mInput1 = 0.0;
	private double mInput2 = 0.0;
	private double mInput3 = 0.0;
	private double mInput4 = 0.0;
	private double mOutput1 = 0.0;
	private double mOutput2 = 0.0;
	private double mOutput3 = 0.0;
	private double mOutput4 = 0.0;
	private double mA1 = 0.0;
	private double mA2 = 0.0;
	private double mB0 = 0.0;
	private double mB1 = 0.0;
	private double mB2 = 0.0;

	public int SampleRate { get; private set; }

	public double Output { get; private set; }

	public BiQuadFilter(int samplerate) {
		SampleRate = samplerate;
	}

	public void HighPass(double freq, double resonance) {
		var omega = 2.0 * Math.PI * freq / SampleRate;
		var alpha = Math.Sin(omega) / (2.0 * resonance);
		var a0 = 1.0 + alpha;
		var p1cos_a0 = (1.0 + Math.Cos(omega)) / a0;
		mA1 = -2.0 * Math.Cos(omega) / a0;
		mA2 = (1.0 - alpha) / a0;
		mB0 = p1cos_a0 / 2.0f;
		mB1 = -p1cos_a0;
		mB2 = p1cos_a0 / 2.0f;
	}

	public void LowPass(double freq, double resonance) {
		var omega = 2.0 * Math.PI * freq / SampleRate;
		var alpha = Math.Sin(omega) / (2.0 * resonance);
		var a0 = 1.0 + alpha;
		var n1cos_a0 = (1.0 - Math.Cos(omega)) / a0;
		mA1 = -2.0 * Math.Cos(omega) / a0;
		mA2 = (1.0 - alpha) / a0;
		mB0 = n1cos_a0 / 2.0f;
		mB1 = n1cos_a0;
		mB2 = n1cos_a0 / 2.0f;
	}

	public void BandPass(double freq, double width) {
		var omega = 2.0 * Math.PI * freq / SampleRate;
		var alpha = Math.Sin(omega) * Math.Sinh(Math.Log(2.0) / 2.0 * width * omega / Math.Sin(omega));
		var a0 = 1.0 + alpha;
		mA1 = -2.0 * Math.Cos(omega) / a0;
		mA2 = (1.0 - alpha) / a0;
		mB0 = alpha / a0;
		mB1 = 0.0;
		mB2 = -alpha / a0;
	}

	public void Peaking(double freq, double width, double gain) {
		var omega = 2.0 * Math.PI * freq / SampleRate;
		var alpha = Math.Sin(omega) * Math.Sinh(Math.Log(2.0) / 2.0 * width * omega / Math.Sin(omega));
		var A = Math.Pow(10.0, gain / 40.0);
		var a0 = 1.0 + alpha / A;
		mA1 = -2.0 * Math.Cos(omega) / a0;
		mA2 = (1.0 - alpha / A) / a0;
		mB0 = (1.0 + alpha * A) / a0;
		mB1 = -2.0 * Math.Cos(omega) / a0;
		mB2 = (1.0 - alpha * A) / a0;
	}

	public void HighShelf(double freq, double resonance, double gain) {
		var omega = 2.0 * Math.PI * freq / SampleRate;
		var a = Math.Pow(10.0, gain / 40.0);
		var beta_sin = Math.Sqrt(a) / resonance * Math.Sin(omega);
		var an1 = a - 1.0;
		var ap1 = a + 1.0;
		var an1_cos = an1 * Math.Cos(omega);
		var ap1_cos = ap1 * Math.Cos(omega);
		var a0 = ap1 - an1_cos + beta_sin;
		mA1 = (an1 - ap1_cos) * 2.0 / a0;
		mA2 = (ap1 - an1_cos - beta_sin) / a0;
		mB0 = (ap1 + an1_cos + beta_sin) * a / a0;
		mB1 = (an1 + ap1_cos) * -2.0 * a / a0;
		mB2 = (ap1 + an1_cos - beta_sin) * a / a0;
	}

	public void LowShelf(double freq, double resonance, double gain) {
		var omega = 2.0 * Math.PI * freq / SampleRate;
		var a = Math.Pow(10.0, gain / 40.0);
		var beta_sin = Math.Sqrt(a) / resonance * Math.Sin(omega);
		var an1 = a - 1.0;
		var ap1 = a + 1.0;
		var an1_cos = an1 * Math.Cos(omega);
		var ap1_cos = ap1 * Math.Cos(omega);
		var a0 = ap1 + an1_cos + beta_sin;
		mA1 = (an1 + ap1_cos) * -2.0 / a0;
		mA2 = (ap1 + an1_cos - beta_sin) / a0;
		mB0 = (ap1 - an1_cos + beta_sin) * a / a0;
		mB1 = (an1 - ap1_cos) * 2.0 * a / a0;
		mB2 = (ap1 - an1_cos - beta_sin) * a / a0;
	}

	public void Exec(double input) {
		Output
			= mB0 * input
			+ mB1 * mInput1
			+ mB2 * mInput2
			- mA1 * mOutput1
			- mA2 * mOutput2;
		mInput2 = mInput1;
		mInput1 = input;
		mOutput2 = mOutput1;
		mOutput1 = Output;

		input = Output;
		Output
			= mB0 * input
			+ mB1 * mInput3
			+ mB2 * mInput4
			- mA1 * mOutput3
			- mA2 * mOutput4;
		mInput4 = mInput3;
		mInput3 = input;
		mOutput4 = mOutput3;
		mOutput3 = Output;
	}
}
