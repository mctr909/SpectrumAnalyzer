using System;

public class OscBank {
	class BANK {
		public double amp;
		public double time;
		public double delta;
		public double declick_speed;
	}

	const double AMP_MIN = 1.0 / 32768.0;
	const int TABLE_LENGTH = 48;
	static readonly double[] TABLE;

	static OscBank() {
		TABLE = new double[TABLE_LENGTH];
		for (int i = 0; i < TABLE_LENGTH; i++) {
			TABLE[i] = Math.Sin(2 * Math.PI * i / TABLE_LENGTH);
		}
	}

	readonly BANK[] BANKS;
	readonly int BUFFER_LENGTH;
	double[] mBuffer;

	public double Pitch { get; set; } = 1.0;

	public OscBank(int sampleRate, double baseFreq, int octDiv, int banks, int bufferLength) {
		BANKS = new BANK[banks];
		var random = new Random();
		for (var b = 0; b < banks; ++b) {
			var freq = baseFreq * Math.Pow(2.0, (double)b / octDiv);
			double declickSpeed;
			if (freq < 440) {
				declickSpeed = 1000;
			} else if (freq < sampleRate * 0.2) {
				declickSpeed = freq;
			} else {
				declickSpeed = sampleRate * 0.2;
			}
			BANKS[b] = new BANK() {
				amp = 0.0,
				time = random.NextDouble(),
				delta = freq / sampleRate,
				declick_speed = declickSpeed / sampleRate
			};
		}
		BUFFER_LENGTH = bufferLength;
		mBuffer = new double[BUFFER_LENGTH];
	}

	public void SetWave(double gain, double[] levels, short[] output) {
		for (int b = 0; b < levels.Length; b += 3) {
			var bank = BANKS[b + 1];
			var peak = 0.0;
			var delta = bank.delta * Pitch;
			for (int w = 0; w < 3; w++) {
				var level = levels[b + w];
				if (peak < level) {
					peak = level;
					delta = BANKS[w + b].delta * Pitch;
				}
			}
			if (peak < AMP_MIN) {
				if (bank.amp < AMP_MIN) {
					continue;
				} else {
					peak = 0.0;
				}
			}
			for (int i = 0; i < BUFFER_LENGTH; i++) {
				var idxD = bank.time * TABLE_LENGTH;
				bank.time += delta;
				if (1.0 <= bank.time) {
					bank.time -= 1.0;
				}
				if (1.0 <= bank.time) {
					bank.time -= 1.0;
				}
				var idxA = (int)idxD;
				var a2b = idxD - idxA;
				var idxB = idxA + 1;
				if (idxB == TABLE_LENGTH) {
					idxB = 0;
				}
				bank.amp += (peak - bank.amp) * bank.declick_speed;
				mBuffer[i] += (TABLE[idxA] * (1.0 - a2b) + TABLE[idxB] * a2b) * bank.amp;
			}
		}
		for (int i = 0; i < BUFFER_LENGTH; i++) {
			var v = mBuffer[i] * gain;
			mBuffer[i] = 0.0;
			if (1.0 < v) {
				v = 1.0;
			}
			if (v < -1.0) {
				v = -1.0;
			}
			output[i] = (short)(v * 32767);
		}
	}
}
