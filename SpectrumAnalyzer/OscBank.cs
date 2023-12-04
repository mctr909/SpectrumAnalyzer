﻿using System;

public class OscBank {
	class BANK {
		public double ampL;
		public double ampR;
		public double declickSpeed;
		public double phase;
		public double[] delta;
	}

	const double AMP_MIN = 1.0 / 32768.0;
	const int TONE_DIV = 3;
	const int TONE_DIV_CENTER = 1;
	const int TABLE_LENGTH = 96;
	static readonly double[] TABLE;

	static OscBank() {
		TABLE = new double[TABLE_LENGTH + 1];
		for (int i = 0; i < TABLE_LENGTH + 1; i++) {
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
			var declickFreq = freq * 16;
			if (sampleRate / 8.0 < declickFreq) {
				declickFreq = sampleRate / 8.0;
			}
			var bank = new BANK() {
				declickSpeed = declickFreq / sampleRate,
				phase = random.NextDouble(),
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
		double[] peaksL, double[] peaksR,
		short[] output
	) {
		var loBankIndex = 0;
		var loBankAmp = 0.0;
		var loBankPhase = 0.0;
		for (int b = 0, p = 0; b < BANKS.Length; b++, p += TONE_DIV) {
			var bank = BANKS[b];
			var delta = bank.delta[TONE_DIV_CENTER];
			var peakL = 0.0;
			var peakR = 0.0;
			var peakC = 0.0;
			for (int d = 0; d < TONE_DIV; d++) {
				var divPeakL = peaksL[p + d];
				var divPeakR = peaksR[p + d];
				var divPeakC = Math.Max(divPeakL, divPeakR);
				if (peakL < divPeakL) {
					peakL = divPeakL;
				}
				if (peakR < divPeakR) {
					peakR = divPeakR;
				}
				if (peakC < divPeakC) {
					peakC = divPeakC;
					delta = bank.delta[d];
				}
			}
			if (bank.ampL < AMP_MIN && bank.ampR < AMP_MIN) {
				if (peakL < AMP_MIN && peakR < AMP_MIN) {
					continue;
				} else {
					var hiBankAmp = 0.0;
					var hiBankPhase = 0.0;
					var hiBankEnd = Math.Min(b + 6, BANKS.Length);
					for (int h = b + 1; h < hiBankEnd; h++) {
						var hiBank = BANKS[h];
						hiBankAmp = Math.Max(hiBank.ampL, hiBank.ampR);
						if (AMP_MIN <= hiBankAmp) {
							hiBankPhase = hiBank.phase;
							break;
						}
					}
					if (6 < b - loBankIndex) {
						loBankAmp = 0.0;
					}
					if (loBankAmp < hiBankAmp) {
						bank.phase = hiBankPhase;
					} else {
						bank.phase = loBankPhase;
					}
				}
			} else {
				loBankIndex = b;
				loBankAmp = Math.Max(bank.ampL, bank.ampR);
				loBankPhase = bank.phase;
			}
			peakL *= gainL;
			peakR *= gainR;
			delta *= Pitch;
			var declickSpeed = bank.declickSpeed * Pitch;
			for (int t = 0; t < BUFFER_LENGTH; t++) {
				var indexD = bank.phase * TABLE_LENGTH;
				var index = (int)indexD;
				var a2b = indexD - index;
				bank.phase += delta;
				bank.phase -= (int)bank.phase;
				bank.ampL += (peakL - bank.ampL) * declickSpeed;
				bank.ampR += (peakR - bank.ampR) * declickSpeed;
				var wave = TABLE[index] * (1.0 - a2b) + TABLE[index + 1] * a2b;
				mBufferL[t] += wave * bank.ampL;
				mBufferR[t] += wave * bank.ampR;
			}
		}
		for (int t = 0, i = 0; t < BUFFER_LENGTH; t++, i += 2) {
			var l = mBufferL[t];
			var r = mBufferR[t];
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
