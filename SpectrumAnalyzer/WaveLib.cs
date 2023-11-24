using System;
using System.Runtime.InteropServices;

unsafe public abstract class WaveLib {
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

	[StructLayout(LayoutKind.Sequential)]
	protected struct WAVEOUTCAPS {
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

	protected delegate void DOutCallback(IntPtr hdrvr, WaveOutMessage uMsg, int dwUser, IntPtr wavhdr, int dwParam2);
	protected delegate void DInCallback(IntPtr hdrvr, WaveInMessage uMsg, int dwUser, IntPtr wavhdr, int dwParam2);

	protected IntPtr mHandle;
	protected WAVEFORMATEX mWaveFormatEx;
	protected WAVEHDR[] mWaveHeader;
	protected IntPtr[] mWaveHeaderPtr;
	protected int mBufferIndex;

	public int SampleRate { get; protected set; }
	public int Channels { get; protected set; }
	public int BufferSize { get; protected set; }
	public int BufferCount { get; protected set; }

	WaveLib() { }
	protected WaveLib(int sampleRate, int channels, int bufferSize, int bufferCount) {
		SampleRate = sampleRate;
		Channels = channels;
		BufferSize = bufferSize;
		BufferCount = bufferCount;
		mBufferIndex = 0;
		mWaveHeaderPtr = new IntPtr[BufferCount];
		mWaveHeader = new WAVEHDR[BufferCount];
	}

	protected void SetHeader() {
		mWaveFormatEx = new WAVEFORMATEX();
		mWaveFormatEx.wFormatTag = 1;
		mWaveFormatEx.nChannels = (ushort)Channels;
		mWaveFormatEx.nSamplesPerSec = (uint)SampleRate;
		mWaveFormatEx.nAvgBytesPerSec = (uint)(SampleRate * Channels * 16 >> 3);
		mWaveFormatEx.nBlockAlign = (ushort)(Channels * 16 >> 3);
		mWaveFormatEx.wBitsPerSample = 16;
		mWaveFormatEx.cbSize = 0;
	}

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutOpen(ref IntPtr hWaveOut, uint uDeviceID, ref WAVEFORMATEX lpFormat, DOutCallback dwCallback, IntPtr dwInstance, uint dwFlags);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInOpen(ref IntPtr hwi, uint uDeviceID, ref WAVEFORMATEX lpFormat, DInCallback dwCallback, IntPtr dwInstance, uint dwFlags = 0x00030000);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutClose(IntPtr hwo);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInClose(IntPtr hwi);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInPrepareHeader(IntPtr hwi, IntPtr lpWaveInHdr, int uSize);

	[DllImport("winmm.dll")]
	protected static extern MMRESULT waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int cbwh);

	[DllImport("winmm.dll")]
	protected static extern MMRESULT waveInUnprepareHeader(IntPtr hwi, IntPtr lpWaveInHdr, int uSize);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutReset(IntPtr hwo);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInReset(IntPtr hwi);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveOutWrite(IntPtr hwo, IntPtr pwh, uint cbwh);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInAddBuffer(IntPtr hwi, IntPtr pwh, uint cbwh);

	[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
	protected static extern MMRESULT waveInStart(IntPtr hwi);
}
