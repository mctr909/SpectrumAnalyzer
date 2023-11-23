public class WavePlayback : WaveOutLib {
    private short[] mWaveL;
    private short[] mWaveR;
    private uint mLoopBegin;
    private uint mLoopEnd;
    private double mDelta;
    private double mTime;
    private short[] mData;
    private OscBank mOsc;

    public bool IsPlay;
    public Spectrum FilterBank;

    public int Position {
        get { return (int)mTime; }
        set { mTime = value; }
    }
    public int Size {
        get { return mWaveL.Length; }
    }
    public double Pitch {
        get { return mOsc.Pitch; }
        set { mOsc.Pitch = value; }
    }
    public double Speed { get; set; } = 1.0;

    public WavePlayback(int notes, double baseFreq) {
        mWaveL = new short[1];
        mWaveR = new short[1];
        mLoopBegin = 0;
        mLoopEnd = 1;
        mDelta = 0.0;
        mTime = 0.0;
        mData = new short[BufferSize / 2];
        mOsc = new OscBank(SampleRate, baseFreq, 12 * 3, notes * 3, BufferSize / 2);
        FilterBank = new Spectrum(SampleRate, baseFreq, notes);
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
            if (8 == file.fmt.BitPerSample) {
                for (var i = 0; i < mWaveL.Length; ++i) {
                    file.read8(ref mWaveL[i]);
                    mWaveL[i] = (short)(256 * mWaveL[i]);
                    mWaveR[i] = (short)(256 * mWaveL[i]);
                }
            } else {
                for (var i = 0; i < mWaveL.Length; ++i) {
                    file.read16(ref mWaveL[i]);
                    mWaveR[i] = mWaveL[i];
                }
            }
            break;

        case 2:
            mWaveL = new short[file.data.DataSize / 4];
            mWaveR = new short[file.data.DataSize / 4];
            if (8 == file.fmt.BitPerSample) {
                for (var i = 0; i < mWaveL.Length; ++i) {
                    file.read8(ref mWaveL[i], ref mWaveR[i]);
                    mWaveL[i] = (short)(256 * mWaveL[i]);
                    mWaveR[i] = (short)(256 * mWaveR[i]);
                }
            } else {
                for (var i = 0; i < mWaveL.Length; ++i) {
                    file.read16(ref mWaveL[i], ref mWaveR[i]);
                }
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
            if (IsPlay && mTime < mWaveL.Length) {
                waveL = mWaveL[idxA] * (1.0 - a2b) + mWaveL[idxB] * a2b;
                waveR = mWaveR[idxA] * (1.0 - a2b) + mWaveR[idxB] * a2b;
            }
            mData[j] = (short)((waveL + waveR) / 2);
            WaveBuffer[i] = 0;
            WaveBuffer[i + 1] = 0;
            mTime += IsPlay ? (mDelta * Speed) : 0.0;
            if (mLoopEnd <= mTime) {
                mTime = mLoopBegin + mTime - mLoopEnd;
            }
        }
        FilterBank.SetLevel(mData);
        mOsc.SetData(FilterBank.Gain, FilterBank.Peak, mData);
        for (int i = 0, j = 0; i < BufferSize; i += 2, j++) {
            WaveBuffer[i] = mData[j];
            WaveBuffer[i + 1] = mData[j];
        }
    }
}