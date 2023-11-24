using System;
using System.Runtime.InteropServices;

public abstract class WaveIn : WaveLib, IDisposable {
	const uint WAVE_MAPPER = unchecked((uint)(-1));

	DInCallback mCallback;

	protected short[] WaveBuffer;

	public WaveIn(int sampleRate = 44100, int channels = 2, int bufferSize = 256, int bufferCount = 32) :
		base(sampleRate, channels, bufferSize, bufferCount) {
		mCallback = new DInCallback(Callback);
		WaveBuffer = new short[BufferSize];
		Open();
		PrepareHeader();
		waveInStart(mHandle);
	}

	public void Dispose() {
		Close();
	}

	void Open() {
		Close();
		SetHeader();
		var mr = waveInOpen(ref mHandle, WAVE_MAPPER, ref mWaveFormatEx, mCallback, IntPtr.Zero);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			//throw new Exception(mr.ToString());
		}
	}

	void Close() {
		if (IntPtr.Zero == mHandle) {
			return;
		}
		waveInReset(mHandle);
		for (int i = 0; i < BufferCount; ++i) {
			waveInUnprepareHeader(mWaveHeaderPtr[i], mHandle, Marshal.SizeOf<WAVEHDR>());
		}
		waveInClose(mHandle);
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
			waveInPrepareHeader(mHandle, mWaveHeaderPtr[i], Marshal.SizeOf(typeof(WAVEHDR)));
			waveInAddBuffer(mHandle, mWaveHeaderPtr[i], (uint)Marshal.SizeOf(typeof(WAVEHDR)));
		}
	}

	void Callback(IntPtr hdrvr, WaveInMessage uMsg, int dwUser, IntPtr waveHdr, int dwParam2) {
		switch (uMsg) {
		case WaveInMessage.Open:
			break;
		case WaveInMessage.Close:
			break;
		case WaveInMessage.Data:
			var hdr = Marshal.PtrToStructure<WAVEHDR>(waveHdr);
			Marshal.Copy(hdr.lpData, WaveBuffer, 0, WaveBuffer.Length);
			SetData();
			waveInAddBuffer(mHandle, waveHdr, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
			break;
		}
	}

	protected virtual void SetData() { }
}
