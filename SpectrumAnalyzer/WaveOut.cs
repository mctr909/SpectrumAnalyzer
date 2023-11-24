using System;
using System.Runtime.InteropServices;

public abstract class WaveOut : WaveLib, IDisposable {
	const uint WAVE_MAPPER = unchecked((uint)(-1));

	DOutCallback mCallback;

	protected short[] WaveBuffer;

	public WaveOut(int sampleRate = 44100, int channels = 2, int bufferSize = 256, int bufferCount = 32) :
		base(sampleRate, channels, bufferSize, bufferCount) {
		mCallback = new DOutCallback(Callback);
		WaveBuffer = new short[BufferSize];
		Open();
		PrepareHeader();
	}

	public void Dispose() {
		Close();
	}

	void Open() {
		Close();
		SetHeader();
		var mr = waveOutOpen(ref mHandle, WAVE_MAPPER, ref mWaveFormatEx, mCallback, IntPtr.Zero, 0x00030000);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			//throw new Exception(mr.ToString());
		}
	}

	void Close() {
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
		for (int i = 0; i < BufferCount; ++i) {
			if (mWaveHeader[i].lpData != IntPtr.Zero) {
				Marshal.FreeHGlobal(mWaveHeader[i].lpData);
				mWaveHeader[i].lpData = IntPtr.Zero;
			}
			if (mWaveHeaderPtr[i] != IntPtr.Zero) {
				Marshal.FreeHGlobal(mWaveHeaderPtr[i]);
				mWaveHeaderPtr[i] = IntPtr.Zero;
			}
		}
	}

	void PrepareHeader() {
		for (int i = 0; i < BufferCount; ++i) {
			mWaveHeaderPtr[i] = Marshal.AllocHGlobal(Marshal.SizeOf(mWaveHeader[i]));
			mWaveHeader[i].dwBufferLength = (uint)(WaveBuffer.Length * 16 >> 3);
			mWaveHeader[i].lpData = Marshal.AllocHGlobal((int)mWaveHeader[i].dwBufferLength);
			mWaveHeader[i].dwFlags = 0;
			Marshal.Copy(WaveBuffer, 0, mWaveHeader[i].lpData, WaveBuffer.Length);
			Marshal.StructureToPtr(mWaveHeader[i], mWaveHeaderPtr[i], true);
			MMRESULT mmrc = waveOutPrepareHeader(mHandle, mWaveHeaderPtr[i], Marshal.SizeOf(typeof(WAVEHDR)));
			waveOutWrite(mHandle, mWaveHeaderPtr[i], (uint)Marshal.SizeOf(typeof(WAVEHDR)));
		}
	}

	void Callback(IntPtr hdrvr, WaveOutMessage uMsg, int dwUser, IntPtr waveHdr, int dwParam2) {
		switch (uMsg) {
		case WaveOutMessage.Close:
			break;

		case WaveOutMessage.Done: {
			waveOutWrite(mHandle, waveHdr, (uint)Marshal.SizeOf(typeof(WAVEHDR)));

			for (mBufferIndex = 0; mBufferIndex < BufferCount; ++mBufferIndex) {
				if (mWaveHeaderPtr[mBufferIndex] == waveHdr) {
					SetData();
					mWaveHeader[mBufferIndex] = (WAVEHDR)Marshal.PtrToStructure(mWaveHeaderPtr[mBufferIndex], typeof(WAVEHDR));
					Marshal.Copy(WaveBuffer, 0, mWaveHeader[mBufferIndex].lpData, WaveBuffer.Length);
					Marshal.StructureToPtr(mWaveHeader[mBufferIndex], mWaveHeaderPtr[mBufferIndex], true);
				}
			}
		}
		break;

		case WaveOutMessage.Open:
			break;
		}
	}

	protected virtual void SetData() { }
}
