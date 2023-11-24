using System;
using System.Runtime.InteropServices;

public abstract class WaveOut : WaveLib, IDisposable {
	enum WaveOutMessage {
		Close = 0x3BC,
		Done = 0x3BD,
		Open = 0x3BB
	}
	[StructLayout(LayoutKind.Sequential)]
	struct WAVEOUTCAPS {
		public ushort wMid;
		public ushort wPid;
		public uint vDriverVersion;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public char[] szPname;
		public uint dwFormats;
		public ushort wChannels;
		public ushort wReserved1;
		public uint dwSupport;
	}

	delegate void DCallback(IntPtr hdrvr, WaveOutMessage uMsg, int dwUser, IntPtr wavhdr, int dwParam2);
	DCallback mCallback;

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern uint waveOutGetNumDevs();
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveOutGetDevCaps(uint uDeviceID, ref WAVEOUTCAPS pwoc, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveOutOpen(ref IntPtr hWaveOut, uint uDeviceID, ref WAVEFORMATEX lpFormat, DCallback dwCallback, IntPtr dwInstance, uint dwFlags);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveOutClose(IntPtr hwo);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int size);
	[DllImport("winmm.dll")]
	static extern MMRESULT waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int cbwh);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveOutReset(IntPtr hwo);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveOutWrite(IntPtr hwo, IntPtr pwh, uint cbwh);

	public static void GetDeviceList() {
		var deviceCount = waveOutGetNumDevs();
		for (uint i = 0; i < deviceCount; i++) {
			var caps = new WAVEOUTCAPS();
			var ret = waveOutGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
			//if (MMRESULT.MMSYSERR_NOERROR != ret) {
			//	throw new Exception(ret.ToString());
			//}
		}
	}

	public WaveOut(int sampleRate = 44100, int channels = 2, int bufferSize = 256, int bufferCount = 32) :
		base(sampleRate, channels, bufferSize, bufferCount) {
		mCallback = new DCallback(Callback);
		Open();
	}

	public void Dispose() {
		Close();
	}

	public void Open() {
		Close();
		AllocHeader();
		var ret = waveOutOpen(ref mHandle, WAVE_MAPPER, ref mWaveFormatEx, mCallback, IntPtr.Zero, 0x00030000);
		if (MMRESULT.MMSYSERR_NOERROR != ret) {
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
		var ret = waveOutReset(mHandle);
		if (MMRESULT.MMSYSERR_NOERROR != ret) {
			throw new Exception(ret.ToString());
		}
		ret = waveOutClose(mHandle);
		if (MMRESULT.MMSYSERR_NOERROR != ret) {
			throw new Exception(ret.ToString());
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
					Marshal.Copy(mBuffer, 0, mWaveHeader[mBufferIndex].lpData, BufferSize);
					Marshal.StructureToPtr(mWaveHeader[mBufferIndex], mWaveHeaderPtr[mBufferIndex], true);
				}
			}
			break;
		}
		}
	}

	protected abstract void SetData();
}
