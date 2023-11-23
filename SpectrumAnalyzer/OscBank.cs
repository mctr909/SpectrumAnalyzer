using System;

public class OscBank {
	class BANK {
		public double amp;
		public double time;
		public double delta;
		public double amp_transition;
	}

	const double AMP_MIN = 1.0 / 32768.0;
	const int TABLE_LENGTH = 192;
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

	public double Pitch = 1.0;

	public OscBank(int sampleRate, double baseFreq, int octDiv, int banks, int bufferLength) {
		BANKS = new BANK[banks];
		var rnd = new Random();
		for (var b = 0; b < banks; ++b) {
			var freq = baseFreq * Math.Pow(2.0, (double)b / octDiv);
			var bank = new BANK();
			bank.time = rnd.NextDouble();
			bank.delta = freq / sampleRate;
			bank.amp = 0.0;
			if (freq < 220) {
				bank.amp_transition = 1000.0 / sampleRate;
			} else if (freq < 800) {
				bank.amp_transition = 200.0 / sampleRate;
			} else if (freq < 1600) {
				bank.amp_transition = 500.0 / sampleRate;
			} else {
				bank.amp_transition = 2.0 * freq / sampleRate;
			}
			BANKS[b] = bank;
		}
		BUFFER_LENGTH = bufferLength;
		mBuffer = new double[BUFFER_LENGTH];
	}

	public void SetData(double gain, double[] levels, short[] data) {
		for (int b = 0; b < levels.Length; b++) {
			var bank = BANKS[b];
			var level = levels[b];
			if (level < AMP_MIN) {
				if (bank.amp < AMP_MIN) {
					continue;
				} else {
					level = 0.0;
				}
			}
			for (int i = 0; i < BUFFER_LENGTH; i++) {
				var idxD = bank.time * TABLE_LENGTH;
				bank.time += bank.delta * Pitch;
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
				bank.amp += (level - bank.amp) * bank.amp_transition;
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
			data[i] = (short)(v * 32767);
		}
	}
}
