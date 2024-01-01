using System;
using System.Runtime.InteropServices;

public class OscBank  {
	class Tone {
		public double AmpL;
		public double AmpR;
		public double Phase;
	}

	const double DECLICK_SPEED = 0.1;
	const double THRESHOLD = 0.001; /* -60db */
	const int TABLE_LENGTH = 192;
	readonly int BUFFER_SAMPLES;

	static readonly double[] TABLE;
	static OscBank() {
		TABLE = new double[TABLE_LENGTH + 1];
		for (int i = 0; i < TABLE_LENGTH + 1; i++) {
			TABLE[i] = Math.Sin(2 * Math.PI * i / TABLE_LENGTH);
		}
	}

	Tone[] Tones;
	float[] mMuteData;

	public static double Pitch { get; set; } = 1.0;

	public OscBank(int bufferSamples, int toneCount) {
		BUFFER_SAMPLES = bufferSamples;
		Tones = new Tone[toneCount];
		var random = new Random();
		for (var idxT = 0; idxT < toneCount; idxT++) {
			Tones[idxT] = new Tone() {
				Phase = random.NextDouble(),
			};
		}
		mMuteData = new float[BUFFER_SAMPLES * 2];
	}

	public unsafe void SetWave(Spectrum spectrum, IntPtr output) {
		Marshal.Copy(mMuteData, 0, output, mMuteData.Length);
		var lowToneIndex = 0;
		var lowTonePhase = 0.0;
		var lowToneAmp = 0.0;
		for (int idxT = 0, idxB = 0; idxT < Tones.Length; idxT++, idxB += Spectrum.TONE_DIV) {
			var specAmpL = 0.0;
			var specAmpR = 0.0;
			var specAmpC = 0.0;
			var delta = spectrum.Banks[idxB + Spectrum.TONE_DIV_CENTER].DELTA;
			for (int div = 0, divB = idxB; div < Spectrum.TONE_DIV; div++, divB++) {
				var peakL = spectrum.L[divB];
				var peakR = spectrum.R[divB];
				var peakC = Math.Max(peakL, peakR);
				if (specAmpL < peakL) {
					specAmpL = peakL;
				}
				if (specAmpR < peakR) {
					specAmpR = peakR;
				}
				if (specAmpC < peakC) {
					specAmpC = peakC;
					delta = spectrum.Banks[divB].DELTA;
				}
			}
			specAmpL *= spectrum.GainL;
			specAmpR *= spectrum.GainR;
			var tone = Tones[idxT];
			if (tone.AmpL >= THRESHOLD || tone.AmpR >= THRESHOLD) {
				lowToneIndex = idxT;
				lowTonePhase = tone.Phase;
				lowToneAmp = Math.Max(tone.AmpL, tone.AmpR);
			}
			else {
				if (specAmpL >= THRESHOLD || specAmpR >= THRESHOLD) {
					var highToneEnd = Math.Min(idxT + 12, Tones.Length);
					var highTonePhase = 0.0;
					var highToneAmp = 0.0;
					for (int h = idxT + 1; h < highToneEnd; h++) {
						var hiTone = Tones[h];
						highToneAmp = Math.Max(hiTone.AmpL, hiTone.AmpR);
						if (highToneAmp >= THRESHOLD) {
							highTonePhase = hiTone.Phase;
							break;
						}
					}
					if (12 < idxT - lowToneIndex) {
						lowToneAmp = 0.0;
					}
					if (lowToneAmp < highToneAmp) {
						tone.Phase = highTonePhase;
					}
					else {
						tone.Phase = lowTonePhase;
					}
				}
			}
			delta *= Pitch;
			var pOutput = (float*)output;
			for (int s = 0; s < BUFFER_SAMPLES; s++) {
				var indexF = tone.Phase * TABLE_LENGTH;
				var indexI = (int)indexF;
				var a2b = indexF - indexI;
				tone.Phase += delta;
				tone.Phase -= (int)tone.Phase;
				tone.AmpL += (specAmpL - tone.AmpL) * DECLICK_SPEED;
				tone.AmpR += (specAmpR - tone.AmpR) * DECLICK_SPEED;
				var wave = TABLE[indexI] * (1.0 - a2b) + TABLE[indexI + 1] * a2b;
				*pOutput++ += (float)(wave * tone.AmpL);
				*pOutput++ += (float)(wave * tone.AmpR);
			}
		}
	}
}
