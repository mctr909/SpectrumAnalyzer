using System;

public class OscBank {
	class BANK {
		public double ampL;
		public double ampR;
		public double declickSpeed;
		public double time;
		public double[] delta;
	}

	const double AMP_MIN = 1.0 / 32768.0;
	const int TONE_DIV = 3;
	const int TONE_DIV_CENTER = 1;
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
	double[] mBufferL;
	double[] mBufferR;

	public double Pitch { get; set; } = 1.0;

	public OscBank(int sampleRate, int bufferLength, int banks, double baseFreq) {
		BANKS = new BANK[banks];
		var random = new Random();
		for (var b = 0; b < banks; b++) {
			var freq = baseFreq * Math.Pow(2.0, b / 12.0);
			double declickSpeed;
			if (freq < sampleRate * 0.25) {
				declickSpeed = freq * 2;
			} else {
				declickSpeed = sampleRate * 0.5;
			}
			var bank = new BANK() {
				declickSpeed = declickSpeed / sampleRate,
				time = random.NextDouble(),
				delta = new double[TONE_DIV],
			};
			for (int d = 0; d < TONE_DIV; d++) {
				bank.delta[d] = freq * Math.Pow(2.0, (d - TONE_DIV_CENTER) / (TONE_DIV * 12.0)) / sampleRate;
			}
			BANKS[b] = bank;
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
		for (int b = 0, il = 0; b < BANKS.Length; b++, il += TONE_DIV) {
			var bank = BANKS[b];
			var delta = bank.delta[TONE_DIV_CENTER];
			var peakL = 0.0;
			var peakR = 0.0;
			var peakC = 0.0;
			for (int d = 0; d < TONE_DIV; d++) {
				var l = levelL[il + d];
				var r = levelR[il + d];
				var c = l + r;
				if (peakL < l) {
					peakL = l;
				}
				if (peakR < r) {
					peakR = r;
				}
				if (peakC < c) {
					peakC = c;
					delta = bank.delta[d];
				}
			}
			if (peakL < AMP_MIN && peakR < AMP_MIN &&
				bank.ampL < AMP_MIN && bank.ampR < AMP_MIN) {
				continue;
			}
			delta *= Pitch;
			for (int t = 0; t < BUFFER_LENGTH; t++) {
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
				mBufferL[t] += wave * bank.ampL;
				mBufferR[t] += wave * bank.ampR;
			}
		}
		for (int t = 0, i = 0; t < BUFFER_LENGTH; t++, i += 2) {
			var l = mBufferL[t] * gainL;
			var r = mBufferR[t] * gainR;
			mBufferL[t] = 0.0;
			mBufferR[t] = 0.0;
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
			output[i] = (short)(l * 32767);
			output[i + 1] = (short)(r * 32767);
		}
	}
}
