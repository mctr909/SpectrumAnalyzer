using System;
using System.Runtime.InteropServices;

namespace WINMM {
	public abstract class WaveLib : IDisposable {
		protected enum WAVEHDR_FLAG : uint {
			WHDR_NONE = 0,
			WHDR_DONE = 0x00000001,
			WHDR_PREPARED = 0x00000002,
			WHDR_BEGINLOOP = 0x00000004,
			WHDR_ENDLOOP = 0x00000008,
			WHDR_INQUEUE = 0x00000010
		}
		protected enum WAVE_FORMAT : uint {
			MONO_8bit_11kHz = 0x1,
			MONO_8bit_22kHz = 0x10,
			MONO_8bit_44kHz = 0x100,
			MONO_8bit_48kHz = 0x1000,
			MONO_8bit_96kHz = 0x10000,
			STEREO_8bit_11kHz = 0x2,
			STEREO_8bit_22kHz = 0x20,
			STEREO_8bit_44kHz = 0x200,
			STEREO_8bit_48kHz = 0x2000,
			STEREO_8bit_96kHz = 0x20000,
			MONO_16bit_11kHz = 0x4,
			MONO_16bit_22kHz = 0x40,
			MONO_16bit_44kHz = 0x400,
			MONO_16bit_48kHz = 0x4000,
			MONO_16bit_96kHz = 0x40000,
			STEREO_16bit_11kHz = 0x8,
			STEREO_16bit_22kHz = 0x80,
			STEREO_16bit_44kHz = 0x800,
			STEREO_16bit_48kHz = 0x8000,
			STEREO_16bit_96kHz = 0x80000
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
			public WAVEHDR_FLAG dwFlags;
			public uint dwLoops;
			public IntPtr lpNext;
			public uint reserved;
		}

		protected const uint WAVE_MAPPER = unchecked((uint)-1);

		#region dynamic variable
		protected IntPtr mHandle;
		protected WAVEFORMATEX mWaveFormatEx;
		protected IntPtr[] mpWaveHeader;
		protected int mBufferCount;
		protected bool mDoStop = false;
		protected bool mStopped = true;
		#endregion

		#region property
		public bool Enabled { get; protected set; }
		public uint DeviceId { get; private set; } = WAVE_MAPPER;
		public int SampleRate { get; private set; }
		public int Channels { get; private set; }
		public int BufferSamples { get; private set; }
		public int BufferSize { get; private set; }
		#endregion

		protected WaveLib(int sampleRate, int channels, int bufferSamples, int bufferCount) {
			var bytesPerSample = channels * 16 >> 3;
			SampleRate = sampleRate;
			Channels = channels;
			BufferSamples = bufferSamples;
			BufferSize = bufferSamples * bytesPerSample;
			mBufferCount = bufferCount;
			mWaveFormatEx = new WAVEFORMATEX() {
				wFormatTag = 1,
				nChannels = (ushort)channels,
				nSamplesPerSec = (uint)sampleRate,
				nAvgBytesPerSec = (uint)(sampleRate * bytesPerSample),
				nBlockAlign = (ushort)bytesPerSample,
				wBitsPerSample = 16,
				cbSize = 0
			};
		}

		protected void AllocHeader() {
			var defaultValue = new byte[BufferSize];
			mpWaveHeader = new IntPtr[mBufferCount];
			for (int i = 0; i < mBufferCount; ++i) {
				var hdr = new WAVEHDR()
				{
					dwFlags = WAVEHDR_FLAG.WHDR_NONE,
					dwBufferLength = (uint)BufferSize
				};
				hdr.lpData = Marshal.AllocHGlobal((int)hdr.dwBufferLength);
				Marshal.Copy(defaultValue, 0, hdr.lpData, BufferSize);
				mpWaveHeader[i] = Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>());
				Marshal.StructureToPtr(hdr, mpWaveHeader[i], true);
			}
		}

		protected void DisposeHeader() {
			for (int i = 0; i < mBufferCount; ++i) {
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

		public void Dispose() {
			Close();
		}

		public void SetDevice(uint deviceId) {
			var enable = Enabled;
			Close();
			DeviceId = deviceId;
			if (enable) {
				Open();
			}
		}

		public abstract void Open();

		public abstract void Close();
	}
}
