using System;
using System.Runtime.InteropServices;
using WINMM;

public class Playback : WaveOut {
	short[] mWaveL;
	short[] mWaveR;
	IntPtr mData;
	uint mLoopBegin;
	uint mLoopEnd;
	double mDelta;
	double mTime;
	OscBank mOscBank;

	public Spectrum FilterBank;

	public int Position {
		get { return (int)mTime; }
		set { mTime = value; }
	}
	public int Length {
		get { return mWaveL.Length; }
	}
	public double Speed { get; set; } = 1.0;

	public Playback(int notes, double baseFreq) {
		mWaveL = new short[1];
		mWaveR = new short[1];
		mLoopBegin = 0;
		mLoopEnd = 1;
		mDelta = 0.0;
		mTime = 0.0;
		mData = Marshal.AllocHGlobal(BufferSamples * 4);
		mOscBank = new OscBank(SampleRate, BufferSamples, notes, baseFreq);
		FilterBank = new Spectrum(SampleRate, baseFreq, notes, BufferSamples, true);
	}

	public void LoadFile(string filePath) {
		var file = new WavReader(filePath);
		mWaveL = new short[file.Data.Size / file.Fmt.BlockSize];
		mWaveR = new short[file.Data.Size / file.Fmt.BlockSize];
		switch (file.Fmt.Channel) {
		case 1:
			for (var i = 0; i < mWaveL.Length; ++i) {
				file.ReadMono(ref mWaveL[i]);
				mWaveR[i] = mWaveL[i];
			}
			break;
		case 2:
			for (var i = 0; i < mWaveL.Length; ++i) {
				file.Read(ref mWaveL[i], ref mWaveR[i]);
			}
			break;
		default:
			mWaveL = new short[1];
			mWaveR = new short[1];
			break;
		}

		mLoopBegin = 0;
		mLoopEnd = (uint)mWaveL.Length;
		mDelta = (double)file.Fmt.SamplingFrequency / SampleRate;
		mTime = 0.0;
	}

	protected unsafe override void WriteBuffer(IntPtr pBuffer) {
		var pData = (short*)mData;
		for (int t = 0, i = 0; t < BufferSamples; t++, i += 2) {
			var waveL = 0.0;
			var waveR = 0.0;
			for (int o = 0; o < 4; o++) {
				var idxA = (int)mTime;
				var a2b = mTime - idxA;
				var idxB = idxA + 1;
				if (mWaveL.Length == idxB) {
					idxB = idxA;
				}
				mTime += mDelta * Speed * 0.25;
				if (mLoopEnd <= mTime) {
					mTime = mLoopBegin + mTime - mLoopEnd;
				}
				waveL += mWaveL[idxA] * (1.0 - a2b) + mWaveL[idxB] * a2b;
				waveR += mWaveR[idxA] * (1.0 - a2b) + mWaveR[idxB] * a2b;
			}
			pData[i] = (short)(waveL * 0.25);
			pData[i + 1] = (short)(waveR * 0.25);
		}
		FilterBank.SetLevel(mData, BufferSamples);
		mOscBank.SetWave(
			FilterBank.GainL, FilterBank.GainR,
			FilterBank.PeakL, FilterBank.PeakR,
			pBuffer
		);
	}
}
