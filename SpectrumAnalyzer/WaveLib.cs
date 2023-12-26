using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

public abstract class WaveLib {
	protected enum MMRESULT {
		MMSYSERR_NOERROR = 0,
		MMSYSERR_ERROR = (MMSYSERR_NOERROR + 1),
		MMSYSERR_BADDEVICEID = (MMSYSERR_NOERROR + 2),
		MMSYSERR_NOTENABLED = (MMSYSERR_NOERROR + 3),
		MMSYSERR_ALLOCATED = (MMSYSERR_NOERROR + 4),
		MMSYSERR_INVALHANDLE = (MMSYSERR_NOERROR + 5),
		MMSYSERR_NODRIVER = (MMSYSERR_NOERROR + 6),
		MMSYSERR_NOMEM = (MMSYSERR_NOERROR + 7),
		MMSYSERR_NOTSUPPORTED = (MMSYSERR_NOERROR + 8),
		MMSYSERR_BADERRNUM = (MMSYSERR_NOERROR + 9),
		MMSYSERR_INVALFLAG = (MMSYSERR_NOERROR + 10),
		MMSYSERR_INVALPARAM = (MMSYSERR_NOERROR + 11),
		MMSYSERR_HANDLEBUSY = (MMSYSERR_NOERROR + 12),
		MMSYSERR_INVALIDALIAS = (MMSYSERR_NOERROR + 13),
		MMSYSERR_BADDB = (MMSYSERR_NOERROR + 14),
		MMSYSERR_KEYNOTFOUND = (MMSYSERR_NOERROR + 15),
		MMSYSERR_READERROR = (MMSYSERR_NOERROR + 16),
		MMSYSERR_WRITEERROR = (MMSYSERR_NOERROR + 17),
		MMSYSERR_DELETEERROR = (MMSYSERR_NOERROR + 18),
		MMSYSERR_VALNOTFOUND = (MMSYSERR_NOERROR + 19),
		MMSYSERR_NODRIVERCB = (MMSYSERR_NOERROR + 20),
		MMSYSERR_MOREDATA = (MMSYSERR_NOERROR + 21),
		MMSYSERR_LASTERROR = (MMSYSERR_NOERROR + 21)
	}
	protected enum WAVEHDR_FLAG {
		WHDR_DONE = 0x00000001,
		WHDR_PREPARED = 0x00000002,
		WHDR_BEGINLOOP = 0x00000004,
		WHDR_ENDLOOP = 0x00000008,
		WHDR_INQUEUE = 0x00000010
	}
	protected enum WAVE_FORMAT {
		MONO_8bit_11kHz    = 0x1,
		MONO_8bit_22kHz    = 0x10,
		MONO_8bit_44kHz    = 0x100,
		MONO_8bit_48kHz    = 0x1000,
		MONO_8bit_96kHz    = 0x10000,
		STEREO_8bit_11kHz  = 0x2,
		STEREO_8bit_22kHz  = 0x20,
		STEREO_8bit_44kHz  = 0x200,
		STEREO_8bit_48kHz  = 0x2000,
		STEREO_8bit_96kHz  = 0x20000,
		MONO_16bit_11kHz   = 0x4,
		MONO_16bit_22kHz   = 0x40,
		MONO_16bit_44kHz   = 0x400,
		MONO_16bit_48kHz   = 0x4000,
		MONO_16bit_96kHz   = 0x40000,
		STEREO_16bit_11kHz = 0x8,
		STEREO_16bit_22kHz = 0x80,
		STEREO_16bit_44kHz = 0x800,
		STEREO_16bit_48kHz = 0x8000,
		STEREO_16bit_96kHz = 0x80000,
	}
	protected enum WaveOutMessage {
		Close = 0x3BC,
		Done = 0x3BD,
		Open = 0x3BB
	}
	protected enum WaveInMessage {
		Open = 0x3BE,
		Close = 0x3BF,
		Data = 0x3C0
	}

	[StructLayout(LayoutKind.Sequential)]
	protected struct WAVEFORMATEX {
		public ushort wFormatTag;
		public ushort nChannels;
		public uint nSamplesPerSec;
		public uint nAvgBytesPerSec;
		public ushort nBlockAlign;
		public ushort wBitsPerSample;
		public ushort cbSize;
	}
	[StructLayout(LayoutKind.Sequential)]
	protected struct WAVEHDR {
		public IntPtr lpData;
		public uint dwBufferLength;
		public uint dwBytesRecorded;
		public uint dwUser;
		public uint dwFlags;
		public uint dwLoops;
		public IntPtr lpNext;
		public uint reserved;
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	protected struct WAVEOUTCAPS {
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
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	protected struct WAVEINCAPS {
		public ushort wMid;
		public ushort wPid;
		public uint vDriverVersion;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string szPname;
		public uint dwFormats;
		public ushort wChannels;
		public ushort wReserved1;
	}

	protected const uint WAVE_MAPPER = unchecked((uint)-1);

	protected delegate void DOutCallback(IntPtr hdrvr, WaveOutMessage uMsg, int dwUser, IntPtr wavhdr, int dwParam2);
	protected delegate void DInCallback(IntPtr hdrvr, WaveInMessage uMsg, int dwUser, IntPtr wavhdr, int dwParam2);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern uint waveOutGetNumDevs();
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutGetDevCaps(uint uDeviceID, ref WAVEOUTCAPS pwoc, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutOpen(ref IntPtr hWaveOut, uint uDeviceID, ref WAVEFORMATEX lpFormat, DOutCallback dwCallback, IntPtr dwInstance, uint dwFlags);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutClose(IntPtr hwo);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int size);
	[DllImport("winmm.dll")]
	protected static extern MMRESULT waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutReset(IntPtr hwo);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutWrite(IntPtr hwo, IntPtr pwh, int size);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern uint waveInGetNumDevs();
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInGetDevCaps(uint uDeviceID, ref WAVEINCAPS pwic, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInOpen(ref IntPtr hwi, uint uDeviceID, ref WAVEFORMATEX lpFormat, DInCallback dwCallback, IntPtr dwInstance, uint dwFlags = 0x00030000);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInClose(IntPtr hwi);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInPrepareHeader(IntPtr hwi, IntPtr lpWaveInHdr, int size);
	[DllImport("winmm.dll")]
	protected static extern MMRESULT waveInUnprepareHeader(IntPtr hwi, IntPtr lpWaveInHdr, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInReset(IntPtr hwi);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInAddBuffer(IntPtr hwi, IntPtr pwh, int size);
	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInStart(IntPtr hwi);

	protected IntPtr mHandle;
	protected WAVEFORMATEX mWaveFormatEx;
	protected IntPtr[] mpWaveHeader;
	protected int mWriteCount;
	protected int mWriteIndex;
	protected int mReadIndex;
	protected short[] mBuffer;
	protected bool mDoStop = false;
	protected bool mStopped = true;
	protected object mLockBuffer = new object();

	public uint DeviceId { get; protected set; } = WAVE_MAPPER;
	public bool Enabled { get; protected set; }
	public int SampleRate { get; private set; }
	public int Channels { get; private set; }
	public int BufferSize { get; private set; }
	public int BufferCount { get; private set; }

	protected WaveLib(int sampleRate, int channels, int bufferSize, int bufferCount) {
		SampleRate = sampleRate;
		Channels = channels;
		BufferSize = bufferSize;
		BufferCount = bufferCount;
		mWriteIndex = 0;
		mBuffer = new short[bufferSize];
		mWaveFormatEx = new WAVEFORMATEX();
		mWaveFormatEx.wFormatTag = 1;
		mWaveFormatEx.nChannels = (ushort)channels;
		mWaveFormatEx.nSamplesPerSec = (uint)sampleRate;
		mWaveFormatEx.nAvgBytesPerSec = (uint)(sampleRate * channels * 16 >> 3);
		mWaveFormatEx.nBlockAlign = (ushort)(channels * 16 >> 3);
		mWaveFormatEx.wBitsPerSample = 16;
		mWaveFormatEx.cbSize = 0;
	}

	protected void AllocHeader() {
		var defaultValue = new short[BufferSize];
		mpWaveHeader = new IntPtr[BufferCount];
		for (int i = 0; i < BufferCount; ++i) {
			var hdr = new WAVEHDR();
			hdr.dwFlags = 0;
			hdr.dwBufferLength = (uint)(BufferSize * 16 >> 3);
			hdr.lpData = Marshal.AllocHGlobal((int)hdr.dwBufferLength);
			Marshal.Copy(defaultValue, 0, hdr.lpData, BufferSize);
			mpWaveHeader[i] = Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>());
			Marshal.StructureToPtr(hdr, mpWaveHeader[i], true);
		}
	}

	protected void DisposeHeader() {
		for (int i = 0; i < BufferCount; ++i) {
			if (mpWaveHeader[i] == IntPtr.Zero) {
				continue;
			}
			var hdr = Marshal.PtrToStructure<WAVEHDR>(mpWaveHeader[i]);
			if (hdr.lpData != IntPtr.Zero) {
				Marshal.FreeHGlobal(hdr.lpData);
			}
			Marshal.FreeHGlobal(mpWaveHeader[i]);
			mpWaveHeader[i] = IntPtr.Zero;
		}
	}
}

public abstract class WaveIn : WaveLib, IDisposable {
	DInCallback mCallback;

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
		mCallback = new DInCallback(Callback);
	}

	public void Dispose() {
		Close();
	}

	public void Open() {
		Close();
		AllocHeader();
		var mr = waveInOpen(ref mHandle, DeviceId, ref mWaveFormatEx, mCallback, IntPtr.Zero);
		if (MMRESULT.MMSYSERR_NOERROR != mr) {
			return;
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
		for (int i = 0; i < 20 && !mStopped; i++) {
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
		DeviceId = deviceId;
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

public abstract class WaveOut : WaveLib, IDisposable {
	DOutCallback mCallback;
	Thread mBufferThread;

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

	public WaveOut(int sampleRate = 44100, int channels = 2, int bufferSize = 128, int bufferCount = 128) :
		base(sampleRate, channels, bufferSize, bufferCount) {
		mCallback = new DOutCallback(Callback);
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
		mBufferThread = new Thread(() => {
			mWriteCount = 0;
			mWriteIndex = 0;
			mReadIndex = 0;
			while (!mDoStop) {
				var sleep = false;
				lock (mLockBuffer) {
					if (BufferCount <= mWriteCount + 1) {
						/*** Buffer full ***/
						sleep = true;
					} else {
						/*** Write Buffer ***/
						SetData();
						var pHdr = Marshal.PtrToStructure<WAVEHDR>(mpWaveHeader[mWriteIndex]);
						Marshal.Copy(mBuffer, 0, pHdr.lpData, BufferSize);
						mWriteIndex = (mWriteIndex + 1) % BufferCount;
						mWriteCount++;
					}
				}
				if (sleep) {
					Thread.Sleep(1);
				}
			}
		});
		mBufferThread.Priority = ThreadPriority.Highest;
		mBufferThread.Start();
	}

	public void Close() {
		if (IntPtr.Zero == mHandle) {
			return;
		}
		mDoStop = true;
		if (null != mBufferThread) {
			mBufferThread.Join();
		}
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
			lock (mLockBuffer) {
				waveOutWrite(mHandle, mpWaveHeader[mReadIndex], Marshal.SizeOf<WAVEHDR>());
				if (0 < mWriteCount) {
					mReadIndex = (mReadIndex + 1) % BufferCount;
					mWriteCount--;
				}
			}
			break;
		}
		}
	}

	protected abstract void SetData();
}
