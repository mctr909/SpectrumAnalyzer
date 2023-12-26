using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

public abstract class WaveOut : WaveLib, IDisposable {
	enum WaveOutMessage {
		Close = 0x3BC,
		Done = 0x3BD,
		Open = 0x3BB
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	struct WAVEOUTCAPS {
		public ushort wMid;
		public ushort wPid;
		public uint vDriverVersion;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string szPname;
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
	static extern MMRESULT waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveOutReset(IntPtr hwo);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveOutWrite(IntPtr hwo, IntPtr pwh, int size);

	public static List<string> GetDeviceList() {
		var list = new List<string>();
		var deviceCount = waveOutGetNumDevs();
		for (uint i = 0; i < deviceCount; i++) {
			var caps = new WAVEOUTCAPS();
			var ret = waveOutGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
			if (MMRESULT.MMSYSERR_NOERROR == ret) {
				list.Add(caps.szPname);
			} else {
				list.Add(ret.ToString());
			}
		}
		return list;
	}

	public WaveOut(int sampleRate = 44100, int channels = 2, int bufferSize = 128, int bufferCount = 48) :
		base(sampleRate, channels, bufferSize, bufferCount) {
		mCallback = new DCallback(Callback);
	}

	public void Dispose() {
		Close();
	}

	public void Open() {
		Close();
		AllocHeader();
		var ret = waveOutOpen(ref mHandle, DeviceId, ref mWaveFormatEx, mCallback, IntPtr.Zero, 0x00030000);
		if (MMRESULT.MMSYSERR_NOERROR != ret) {
			return;
		}
		for (int i = 0; i < BufferCount; ++i) {
			waveOutPrepareHeader(mHandle, mpWaveHeader[i], Marshal.SizeOf(typeof(WAVEHDR)));
			waveOutWrite(mHandle, mpWaveHeader[i], Marshal.SizeOf(typeof(WAVEHDR)));
		}
	}

	public void Close() {
		if (IntPtr.Zero == mHandle) {
			return;
		}
		mDoStop = true;
		for (int i = 0; i < 20 && !mStopped; i++) {
			Thread.Sleep(100);
		}
		for (int i = 0; i < BufferCount; ++i) {
			waveOutUnprepareHeader(mHandle, mpWaveHeader[i], Marshal.SizeOf(typeof(WAVEHDR)));
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

	public void SetDevice(uint deviceId) {
		var enable = Enabled;
		Close();
		DeviceId = deviceId;
		if (enable) {
			Open();
		}
	}

	void Callback(IntPtr hdrvr, WaveOutMessage uMsg, int dwUser, IntPtr waveHdr, int dwParam2) {
		switch (uMsg) {
		case WaveOutMessage.Open:
			mStopped = false;
			Enabled = true;
			break;
		case WaveOutMessage.Close:
			mDoStop = false;
			Enabled = false;
			break;
		case WaveOutMessage.Done: {
			if (mDoStop) {
				mStopped = true;
				break;
			}
			waveOutWrite(mHandle, waveHdr, Marshal.SizeOf(typeof(WAVEHDR)));
			for (mBufferIndex = 0; mBufferIndex < BufferCount; ++mBufferIndex) {
				if (mpWaveHeader[mBufferIndex] == waveHdr) {
					SetData();
					var hdr = (WAVEHDR)Marshal.PtrToStructure(mpWaveHeader[mBufferIndex], typeof(WAVEHDR));
					Marshal.Copy(mBuffer, 0, hdr.lpData, BufferSize);
					Marshal.StructureToPtr(hdr, mpWaveHeader[mBufferIndex], true);
				}
			}
			break;
		}
		}
	}

	protected abstract void SetData();
}
