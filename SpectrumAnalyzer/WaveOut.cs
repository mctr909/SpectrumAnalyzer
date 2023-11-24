using System;
using System.Runtime.InteropServices;

public abstract class WaveOut : WaveLib, IDisposable {
	DOutCallback mCallback;

	public WaveOut(int sampleRate = 44100, int channels = 2, int bufferSize = 256, int bufferCount = 32) :
		base(sampleRate, channels, bufferSize, bufferCount) {
		mCallback = new DOutCallback(Callback);
		mBuffer = new short[bufferSize];
		Open();
	}

	public void Dispose() {
		Close();
	}

	public void Open() {
		Close();
		SetHeader();
		var mr = waveOutOpen(ref mHandle, WAVE_MAPPER, ref mWaveFormatEx, mCallback, IntPtr.Zero, 0x00030000);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			//throw new Exception(mr.ToString());
		}
		for (int i = 0; i < BufferCount; ++i) {
			waveOutPrepareHeader(mHandle, mWaveHeaderPtr[i], Marshal.SizeOf(typeof(WAVEHDR)));
			waveOutWrite(mHandle, mWaveHeaderPtr[i], (uint)Marshal.SizeOf(typeof(WAVEHDR)));
		}
	}

	public void Close() {
		if (IntPtr.Zero == mHandle) {
			return;
		}
		for (int i = 0; i < BufferCount; ++i) {
			waveOutUnprepareHeader(mHandle, mWaveHeaderPtr[i], Marshal.SizeOf(typeof(WAVEHDR)));
		}
		var mr = waveOutReset(mHandle);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			throw new Exception(mr.ToString());
		}
		mr = waveOutClose(mHandle);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			throw new Exception(mr.ToString());
		}
		mHandle = IntPtr.Zero;
		DisposeHeader();
	}

	void Callback(IntPtr hdrvr, WaveOutMessage uMsg, int dwUser, IntPtr waveHdr, int dwParam2) {
		switch (uMsg) {
		case WaveOutMessage.Open:
			break;
		case WaveOutMessage.Close:
			break;
		case WaveOutMessage.Done: {
			waveOutWrite(mHandle, waveHdr, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
			for (mBufferIndex = 0; mBufferIndex < BufferCount; ++mBufferIndex) {
				if (mWaveHeaderPtr[mBufferIndex] == waveHdr) {
					SetData();
					mWaveHeader[mBufferIndex] = (WAVEHDR)Marshal.PtrToStructure(mWaveHeaderPtr[mBufferIndex], typeof(WAVEHDR));
					Marshal.Copy(mBuffer, 0, mWaveHeader[mBufferIndex].lpData, mBuffer.Length);
					Marshal.StructureToPtr(mWaveHeader[mBufferIndex], mWaveHeaderPtr[mBufferIndex], true);
				}
			}
			break;
		}
		}
	}

	protected abstract void SetData();
}
