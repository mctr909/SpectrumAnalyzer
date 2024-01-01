using System;
using WINMM;

public class Playback : WaveOut {
	short[] mWaveL;
	short[] mWaveR;
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

	public Playback(int sampleRate, int notes, double baseFreq) : base(sampleRate, 2, sampleRate / 200, 32) {
		mWaveL = new short[2];
		mWaveR = new short[2];
		mDelta = 0.0;
		mTime = 0.0;
		mOscBank = new OscBank(SampleRate, BufferSamples, notes, baseFreq);
		FilterBank = new Spectrum(SampleRate, baseFreq, notes, BufferSamples, true);
	}

	public void LoadFile(string filePath) {
		var file = new WavReader(filePath);
		Length = (int)file.Samples;
		mWaveL = new short[Length + 1];
		mWaveR = new short[Length + 1];
		switch (file.Fmt.Channel) {
		case 1:
			for (var i = 0; i < Length; ++i) {
				file.Read();
				mWaveL[i] = (short)(file.Values[0] * 32767);
				mWaveR[i] = mWaveL[i];
			}
			break;
		case 2:
			for (var i = 0; i < Length; ++i) {
				file.Read();
				mWaveL[i] = (short)(file.Values[0] * 32767);
				mWaveR[i] = (short)(file.Values[1] * 32767);
			}
			break;
		default:
			Length = 1;
			mWaveL = new short[2];
			mWaveR = new short[2];
			break;
		}

		mDelta = (double)file.Fmt.SampleRate / SampleRate;
		mTime = 0.0;
	}

	protected unsafe override void WriteBuffer(IntPtr pBuffer) {
		var pWave = (short*)pBuffer;
		for (int t = 0, i = 0; t < BufferSamples; t++, i += 2) {
			var waveL = 0.0;
			var waveR = 0.0;
			for (int o = 0; o < 8; o++) {
				var idxA = (int)mTime;
				var idxB = idxA + 1;
				var kb = mTime - idxA;
				var ka = 1 - kb;
				waveL += mWaveL[idxA] * ka + mWaveL[idxB] * kb;
				waveR += mWaveR[idxA] * ka + mWaveR[idxB] * kb;
				mTime += mDelta * Speed * 0.125;
				mTime -= (int)mTime / Length * Length;
			}
			pWave[i] = (short)(waveL * 0.125);
			pWave[i + 1] = (short)(waveR * 0.125);
		}
		FilterBank.SetValue(pBuffer, BufferSamples);
		mOscBank.SetWave(FilterBank, pBuffer);
	}
}
