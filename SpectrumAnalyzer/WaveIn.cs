using System;
using System.Runtime.InteropServices;

public abstract class WaveIn : WaveLib, IDisposable {
	DInCallback mCallback;

	public WaveIn(int sampleRate = 44100, int channels = 2, int bufferSize = 256, int bufferCount = 32) :
		base(sampleRate, channels, bufferSize, bufferCount) {
		mCallback = new DInCallback(Callback);
		mBuffer = new short[bufferSize];
		Open();
	}

	public void Dispose() {
		Close();
	}

	public void Open() {
		Close();
		SetHeader();
		var mr = waveInOpen(ref mHandle, WAVE_MAPPER, ref mWaveFormatEx, mCallback, IntPtr.Zero);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			//throw new Exception(mr.ToString());
		}
		for (int i = 0; i < BufferCount; ++i) {
			waveInPrepareHeader(mHandle, mWaveHeaderPtr[i], Marshal.SizeOf(typeof(WAVEHDR)));
			waveInAddBuffer(mHandle, mWaveHeaderPtr[i], (uint)Marshal.SizeOf(typeof(WAVEHDR)));
		}
		waveInStart(mHandle);
	}

	public void Close() {
		if (IntPtr.Zero == mHandle) {
			return;
		}
		var mr = waveInReset(mHandle);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			throw new Exception(mr.ToString());
		}
		for (int i = 0; i < BufferCount; ++i) {
			waveInUnprepareHeader(mWaveHeaderPtr[i], mHandle, Marshal.SizeOf<WAVEHDR>());
		}
		mr = waveInClose(mHandle);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			throw new Exception(mr.ToString());
		}
		mHandle = IntPtr.Zero;
		DisposeHeader();
	}

	void Callback(IntPtr hdrvr, WaveInMessage uMsg, int dwUser, IntPtr waveHdr, int dwParam2) {
		switch (uMsg) {
		case WaveInMessage.Open:
			break;
		case WaveInMessage.Close:
			break;
		case WaveInMessage.Data:
			var hdr = Marshal.PtrToStructure<WAVEHDR>(waveHdr);
			Marshal.Copy(hdr.lpData, mBuffer, 0, mBuffer.Length);
			SetData();
			waveInAddBuffer(mHandle, waveHdr, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
			break;
		}
	}

	protected abstract void SetData();
}
