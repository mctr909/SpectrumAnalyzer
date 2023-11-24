using System;
using System.Runtime.InteropServices;

public abstract class WaveLib {
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
		MONO_11kHz_8bit  = 0x1,
		MONO_11kHz_16bit = 0x4,
		MONO_22kHz_8bit  = 0x10,
		MONO_22kHz_16bit = 0x40,
		MONO_44kHz_8bit  = 0x100,
		MONO_44kHz_16bit = 0x400,
		MONO_48kHz_8bit  = 0x1000,
		MONO_48kHz_16bit = 0x4000,
		MONO_96kHz_8bit  = 0x10000,
		MONO_96kHz_16bit = 0x40000,
		STEREO_11kHz_8bit  = 0x2,
		STEREO_11kHz_16bit = 0x8,
		STEREO_22kHz_8bit  = 0x20,
		STEREO_22kHz_16bit = 0x80,
		STEREO_44kHz_8bit  = 0x200,
		STEREO_44kHz_16bit = 0x800,
		STEREO_48kHz_8bit  = 0x2000,
		STEREO_48kHz_16bit = 0x8000,
		STEREO_96kHz_8bit  = 0x20000,
		STEREO_96kHz_16bit = 0x80000,
	}

	protected const uint WAVE_MAPPER = unchecked((uint)-1);

	protected IntPtr mHandle;
	protected WAVEFORMATEX mWaveFormatEx;
	protected WAVEHDR[] mWaveHeader;
	protected IntPtr[] mWaveHeaderPtr;
	protected int mBufferIndex;
	protected short[] mBuffer;

	public int SampleRate { get; private set; }
	public int Channels { get; private set; }
	public int BufferSize { get; private set; }
	public int BufferCount { get; private set; }

	protected WaveLib(int sampleRate, int channels, int bufferSize, int bufferCount) {
		SampleRate = sampleRate;
		Channels = channels;
		BufferSize = bufferSize;
		BufferCount = bufferCount;
		mBufferIndex = 0;
		mBuffer = new short[bufferSize];
	}

	protected void AllocHeader() {
		mWaveFormatEx = new WAVEFORMATEX();
		mWaveFormatEx.wFormatTag = 1;
		mWaveFormatEx.nChannels = (ushort)Channels;
		mWaveFormatEx.nSamplesPerSec = (uint)SampleRate;
		mWaveFormatEx.nAvgBytesPerSec = (uint)(SampleRate * Channels * 16 >> 3);
		mWaveFormatEx.nBlockAlign = (ushort)(Channels * 16 >> 3);
		mWaveFormatEx.wBitsPerSample = 16;
		mWaveFormatEx.cbSize = 0;
		mWaveHeaderPtr = new IntPtr[BufferCount];
		mWaveHeader = new WAVEHDR[BufferCount];
		for (int i = 0; i < BufferCount; ++i) {
			mWaveHeaderPtr[i] = Marshal.AllocHGlobal(Marshal.SizeOf(mWaveHeader[i]));
			mWaveHeader[i].dwBufferLength = (uint)(BufferSize * 16 >> 3);
			mWaveHeader[i].lpData = Marshal.AllocHGlobal((int)mWaveHeader[i].dwBufferLength);
			mWaveHeader[i].dwFlags = 0;
			Marshal.Copy(mBuffer, 0, mWaveHeader[i].lpData, BufferSize);
			Marshal.StructureToPtr(mWaveHeader[i], mWaveHeaderPtr[i], true);
		}
	}

	protected void DisposeHeader() {
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
}
