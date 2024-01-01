using System;

namespace SpectrumAnalyzer.spectrum {
	public class BPFBank {
		public readonly int SAMPLE_RATE;
		public readonly double DELTA;
		public readonly double ALPHA;

		public readonly double Kb0;
		public readonly double Ka1;
		public readonly double Ka2;

		public double Sigma { get; private set; }
		public double DisplaySigma { get; private set; }

		public double La1;
		public double La2;
		public double Lb1;
		public double Lb2;
		public double LPower;
		public double LDisplay;

		public double Ra1;
		public double Ra2;
		public double Rb1;
		public double Rb2;
		public double RPower;
		public double RDisplay;

		public BPFBank(int sampleRate, double frequency) {
			SAMPLE_RATE = sampleRate;
			DELTA = frequency / SAMPLE_RATE;
			ALPHA = GetAlpha(frequency);
			var a0 = 1.0 + ALPHA;
			Kb0 = ALPHA / a0;
			Ka1 = -2.0 * Math.Cos(2 * Math.PI * DELTA) / a0;
			Ka2 = (1.0 - ALPHA) / a0;
			SetSpeed(1);
		}

		public void SetResponceSpeed(double speed) {
			var frequency = DELTA * SAMPLE_RATE;
			if (speed > frequency) {
				DisplaySigma = GetAlpha(8 * frequency);
			} else {
				DisplaySigma = GetAlpha(8 * speed);
			}
		}

		public void SetSpeed(double speed) {
			Sigma = GetAlpha(8 * DELTA * SAMPLE_RATE) * speed;
		}

		double GetAlpha(double frequency) {
			const double MIN_WIDTH = 1.0;
			const double MIN_WIDTH_AT_FREQ = 1000.0;
			var halfToneWidth = MIN_WIDTH + Math.Log(MIN_WIDTH_AT_FREQ / frequency, 2.0);
			if (halfToneWidth < MIN_WIDTH) {
				halfToneWidth = MIN_WIDTH;
			}
			var omega = 2 * Math.PI * frequency / SAMPLE_RATE;
			var s = Math.Sin(omega);
			var x = Math.Log(2) / 4 * halfToneWidth / 12.0 * omega / s;
			var a = s * Math.Sinh(x);
			if (a > 0.5) {
				return 0.5;
			}
			return a;
		}
	}
}
