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

	public bool Enabled { get; set; }
	public int Position {
		get { return (int)mTime; }
		set { mTime = value; }
	}
	public int Length {
		get { return mWaveL.Length; }
	}
	public double Pitch {
		get { return mOscBank.Pitch; }
		set { mOscBank.Pitch = value; }
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

	public void SetValue(string filePath) {
		var file = new RiffWAV(filePath, false);
		if (8 != file.fmt.BitPerSample && 16 != file.fmt.BitPerSample) {
			mWaveL = new short[1];
			mWaveR = new short[1];
		}

		switch (file.fmt.Channel) {
		case 1:
			mWaveL = new short[file.data.DataSize / 2];
			mWaveR = new short[file.data.DataSize / 2];
			switch (file.fmt.BitPerSample) {
			case 8:
				for (var i = 0; i < mWaveL.Length; ++i) {
					file.read8(ref mWaveL[i]);
					mWaveR[i] = mWaveL[i];
				}
				break;
			case 16:
				for (var i = 0; i < mWaveL.Length; ++i) {
					file.read16(ref mWaveL[i]);
					mWaveR[i] = mWaveL[i];
				}
				break;
			case 24:
				for (var i = 0; i < mWaveL.Length; ++i) {
					file.read24(ref mWaveL[i]);
					mWaveR[i] = mWaveL[i];
				}
				break;
			case 32:
				for (var i = 0; i < mWaveL.Length; ++i) {
					file.read32(ref mWaveL[i]);
					mWaveR[i] = mWaveL[i];
				}
				break;
			}
			break;

		case 2:
			mWaveL = new short[file.data.DataSize / 4];
			mWaveR = new short[file.data.DataSize / 4];
			switch (file.fmt.BitPerSample) {
			case 8:
				for (var i = 0; i < mWaveL.Length; ++i) {
					file.read8(ref mWaveL[i], ref mWaveR[i]);
				}
				break;
			case 16:
				for (var i = 0; i < mWaveL.Length; ++i) {
					file.read16(ref mWaveL[i], ref mWaveR[i]);
				}
				break;
			case 24:
				for (var i = 0; i < mWaveL.Length; ++i) {
					file.read24(ref mWaveL[i], ref mWaveR[i]);
				}
				break;
			case 32:
				for (var i = 0; i < mWaveL.Length; ++i) {
					file.read32(ref mWaveL[i], ref mWaveR[i]);
				}
				break;
			}
			break;

		default:
			mWaveL = new short[1];
			mWaveR = new short[1];
			break;
		}

		mLoopBegin = 0;
		mLoopEnd = (uint)mWaveL.Length;
		mDelta = (double)file.fmt.SamplingFrequency / SampleRate;
		mTime = 0.0;

		file.close();
	}

	protected override void SetData() {
		for (int i = 0, j = 0; i < BufferSize; i += 2, j++) {
			var idxA = (int)mTime;
			var a2b = mTime - idxA;
			var idxB = idxA + 1;
			if (mWaveL.Length == idxB) {
				idxB = idxA;
			}
			var waveL = 0.0;
			var waveR = 0.0;
			if (Enabled && mTime < mWaveL.Length) {
				waveL = mWaveL[idxA] * (1.0 - a2b) + mWaveL[idxB] * a2b;
				waveR = mWaveR[idxA] * (1.0 - a2b) + mWaveR[idxB] * a2b;
			}
			mDataL[j] = (short)waveL;
			mDataR[j] = (short)waveR;
			mTime += Enabled ? (mDelta * Speed) : 0.0;
			if (mLoopEnd <= mTime) {
				mTime = mLoopBegin + mTime - mLoopEnd;
			}
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
