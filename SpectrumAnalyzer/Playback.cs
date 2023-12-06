public class Playback : WaveOut {
	short[] mWaveL;
	short[] mWaveR;
	short[] mDataL;
	short[] mDataR;
	uint mLoopBegin;
	uint mLoopEnd;
	double mDelta;
	double mTime;
	OscBank mOscBank;

	public Spectrum FilterBankL;
	public Spectrum FilterBankR;

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
		mDataL = new short[BufferSize / 2];
		mDataR = new short[BufferSize / 2];
		mOscBank = new OscBank(SampleRate, BufferSize / 2, notes, baseFreq);
		FilterBankL = new Spectrum(SampleRate, baseFreq, notes);
		FilterBankR = new Spectrum(SampleRate, baseFreq, notes);
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

	protected override void SetData() {
		for (int i = 0, j = 0; i < BufferSize; i += 2, j++) {
			var idxA = (int)mTime;
			var a2b = mTime - idxA;
			var idxB = idxA + 1;
			if (mWaveL.Length == idxB) {
				idxB = idxA;
			}
			mTime += mDelta * Speed;
			if (mLoopEnd <= mTime) {
				mTime = mLoopBegin + mTime - mLoopEnd;
			}
			var waveL = mWaveL[idxA] * (1.0 - a2b) + mWaveL[idxB] * a2b;
			var waveR = mWaveR[idxA] * (1.0 - a2b) + mWaveR[idxB] * a2b;
			mDataL[j] = (short)waveL;
			mDataR[j] = (short)waveR;
		}
		FilterBankL.SetLevel(mDataL);
		FilterBankR.SetLevel(mDataR);
		mOscBank.SetWave(
			FilterBankL.Gain, FilterBankR.Gain,
			FilterBankL.Peak, FilterBankR.Peak,
			mBuffer
		);
	}
}
