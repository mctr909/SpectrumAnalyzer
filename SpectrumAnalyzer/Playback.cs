using System;
using WINMM;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		float[] mWaveL;
		float[] mWaveR;
		double mDelta;
		double mTime;
		OscBank mOscBank;

		public Spectrum FilterBank;

		public double Position {
			get { return mTime; }
			set { mTime = value; }
		}
		public int Length { get; private set; } = 1;
		public double Speed { get; set; } = 1.0;

		public Playback(int sampleRate)
			: base(sampleRate, 2, BUFFER_TYPE.F32, sampleRate / 800, 50) {
			mWaveL = new float[2];
			mWaveR = new float[2];
			mDelta = 0.0;
			mTime = 0.0;
			mOscBank = new OscBank(BufferSamples, Settings.NOTE_COUNT);
			FilterBank = new Spectrum(sampleRate, Settings.BASE_FREQ, Settings.NOTE_COUNT, BufferSamples, true);
		}

		public void LoadFile(string filePath) {
			var file = new WavReader(filePath);
			Length = (int)file.Samples;
			mWaveL = new float[Length + 1];
			mWaveR = new float[Length + 1];
			switch (file.Fmt.Channel) {
			case 1:
				for (var i = 0; i < Length; ++i) {
					file.Read();
					mWaveL[i] = (float)file.Values[0];
					mWaveR[i] = mWaveL[i];
				}
				break;
			case 2:
				for (var i = 0; i < Length; ++i) {
					file.Read();
					mWaveL[i] = (float)file.Values[0];
					mWaveR[i] = (float)file.Values[1];
				}
				break;
			default:
				Length = 1;
				mWaveL = new float[2];
				mWaveR = new float[2];
				break;
			}

			mDelta = (double)file.Fmt.SampleRate / SampleRate;
			mTime = 0.0;
		}

		protected unsafe override void WriteBuffer(IntPtr pBuffer) {
			var pWave = (float*)pBuffer;
			for (int t = 0, i = 0; t < BufferSamples; t++, i += 2) {
				var idxA = (int)mTime;
				var idxB = idxA + 1;
				var kb = (float)mTime - idxA;
				var ka = 1 - kb;
				mTime += mDelta * Speed;
				mTime -= (int)mTime / Length * Length;
				pWave[i] = mWaveL[idxA] * ka + mWaveL[idxB] * kb;
				pWave[i + 1] = mWaveR[idxA] * ka + mWaveR[idxB] * kb;
			}
			FilterBank.SetValue(pBuffer, BufferSamples);
			mOscBank.SetWave(FilterBank, pBuffer);
		}
	}
}
