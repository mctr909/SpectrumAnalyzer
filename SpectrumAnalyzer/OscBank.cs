using System;

public class OscBank {
	class BANK {
		public double ampL;
		public double ampR;
		public double time;
		public double delta;
		public double declickSpeed;
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
	double[] mBufferL;
	double[] mBufferR;

	public double Pitch { get; set; } = 1.0;

	public OscBank(int sampleRate, double baseFreq, int octDiv, int banks, int bufferLength) {
		BANKS = new BANK[banks];
		var random = new Random();
		for (var b = 0; b < banks; ++b) {
			var freq = baseFreq * Math.Pow(2.0, (double)b / octDiv);
			double declickSpeed;
			if (freq < sampleRate * 0.25) {
				declickSpeed = freq;
			} else {
				declickSpeed = sampleRate * 0.25;
			}
			BANKS[b] = new BANK() {
				time = random.NextDouble(),
				delta = freq / sampleRate,
				declickSpeed = declickSpeed / sampleRate
			};
		}
		BUFFER_LENGTH = bufferLength;
		mBufferL = new double[BUFFER_LENGTH];
		mBufferR = new double[BUFFER_LENGTH];
	}

	public void SetWave(
		double gainL, double gainR,
		double[] levelL, double[] levelR,
		short[] output
	) {
		for (int b = 0; b < levelL.Length; b += 3) {
			var bank = BANKS[b + 1];
			var delta = bank.delta * Pitch;
			var peakL = 0.0;
			var peakR = 0.0;
			var peakC = 0.0;
			for (int w = 0; w < 3; w++) {
				var l = levelL[b + w];
				var r = levelR[b + w];
				var c = l + r;
				if (peakL < l) {
					peakL = l;
				}
				if (peakR < r) {
					peakR = r;
				}
				if (peakC < c) {
					peakC = c;
					delta = BANKS[b + w].delta * Pitch;
				}
			}
			if (peakL < AMP_MIN && peakR < AMP_MIN &&
				bank.ampL < AMP_MIN && bank.ampR < AMP_MIN) {
				continue;
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
				bank.ampL += (peakL - bank.ampL) * bank.declickSpeed;
				bank.ampR += (peakR - bank.ampR) * bank.declickSpeed;
				var wave = TABLE[idxA] * (1.0 - a2b) + TABLE[idxB] * a2b;
				mBufferL[i] += wave * bank.ampL;
				mBufferR[i] += wave * bank.ampR;
			}
		}
		for (int i = 0, j = 0; i < BUFFER_LENGTH; i++, j += 2) {
			var l = mBufferL[i] * gainL;
			var r = mBufferR[i] * gainR;
			mBufferL[i] = 0.0;
			mBufferR[i] = 0.0;
			if (1.0 < l) {
				l = 1.0;
			}
			if (l < -1.0) {
				l = -1.0;
			}
			if (1.0 < r) {
				r = 1.0;
			}
			if (r < -1.0) {
				r = -1.0;
			}
			output[j] = (short)(l * 32767);
			output[j + 1] = (short)(r * 32767);
		}
	}
}
