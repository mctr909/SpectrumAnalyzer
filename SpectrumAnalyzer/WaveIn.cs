using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

public abstract class WaveIn : WaveLib, IDisposable {
	enum WaveInMessage {
		Open = 0x3BE,
		Close = 0x3BF,
		Data = 0x3C0
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	struct WAVEINCAPS {
		public ushort wMid;
		public ushort wPid;
		public uint vDriverVersion;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string szPname;
		public uint dwFormats;
		public ushort wChannels;
		public ushort wReserved1;
	}

	delegate void DCallback(IntPtr hdrvr, WaveInMessage uMsg, int dwUser, IntPtr wavhdr, int dwParam2);
	DCallback mCallback;

	uint mDeviceId = WAVE_MAPPER;

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern uint waveInGetNumDevs();
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveInGetDevCaps(uint uDeviceID, ref WAVEINCAPS pwic, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveInOpen(ref IntPtr hwi, uint uDeviceID, ref WAVEFORMATEX lpFormat, DCallback dwCallback, IntPtr dwInstance, uint dwFlags = 0x00030000);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveInClose(IntPtr hwi);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveInPrepareHeader(IntPtr hwi, IntPtr lpWaveInHdr, int size);
	[DllImport("winmm.dll")]
	static extern MMRESULT waveInUnprepareHeader(IntPtr hwi, IntPtr lpWaveInHdr, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveInReset(IntPtr hwi);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveInAddBuffer(IntPtr hwi, IntPtr pwh, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	static extern MMRESULT waveInStart(IntPtr hwi);

	public static List<string> GetDeviceList() {
		var list = new List<string>();
		var deviceCount = waveInGetNumDevs();
		for (uint i = 0; i < deviceCount; i++) {
			var caps = new WAVEINCAPS();
			var ret = waveInGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
			if (MMRESULT.MMSYSERR_NOERROR == ret) {
				list.Add(caps.szPname);
			} else {
				list.Add(ret.ToString());
			}
		}
		return list;
	}

	public WaveIn(int sampleRate = 44100, int channels = 2, int bufferSize = 256, int bufferCount = 32) :
		base(sampleRate, channels, bufferSize, bufferCount) {
		mCallback = new DCallback(Callback);
	}

	public void Dispose() {
		Close();
	}

	public void Open() {
		Close();
		AllocHeader();
		var mr = waveInOpen(ref mHandle, mDeviceId, ref mWaveFormatEx, mCallback, IntPtr.Zero);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			//throw new Exception(mr.ToString());
		}
		for (int i = 0; i < BufferCount; ++i) {
			waveInPrepareHeader(mHandle, mpWaveHeader[i], Marshal.SizeOf(typeof(WAVEHDR)));
			waveInAddBuffer(mHandle, mpWaveHeader[i], Marshal.SizeOf(typeof(WAVEHDR)));
		}
		waveInStart(mHandle);
	}

	public void Close() {
		if (IntPtr.Zero == mHandle) {
			return;
		}
		mDoStop = true;
		while (!mStopped) {
			Thread.Sleep(100);
		}
		for (int i = 0; i < BufferCount; ++i) {
			waveInUnprepareHeader(mpWaveHeader[i], mHandle, Marshal.SizeOf<WAVEHDR>());
		}
		var mr = waveInReset(mHandle);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			throw new Exception(mr.ToString());
		}
		mr = waveInClose(mHandle);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			throw new Exception(mr.ToString());
		}
		mHandle = IntPtr.Zero;
		DisposeHeader();
	}

	public void SetDevice(uint deviceId) {
		var enable = Enabled;
		Close();
		mDeviceId = deviceId;
		if (enable) {
			Open();
		}
	}

	void Callback(IntPtr hdrvr, WaveInMessage uMsg, int dwUser, IntPtr waveHdr, int dwParam2) {
		switch (uMsg) {
		case WaveInMessage.Open:
			mStopped = false;
			Enabled = true;
			break;
		case WaveInMessage.Close:
			mDoStop = false;
			Enabled = false;
			break;
		case WaveInMessage.Data:
			if (mDoStop) {
				mStopped = true;
				break;
			}
			var hdr = (WAVEHDR)Marshal.PtrToStructure(waveHdr, typeof(WAVEHDR));
			Marshal.Copy(hdr.lpData, mBuffer, 0, BufferSize);
			SetData();
			waveInAddBuffer(mHandle, waveHdr, Marshal.SizeOf(typeof(WAVEHDR)));
			break;
		}
	}

	protected abstract void SetData();
}
