using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinMM {
	public abstract class Wave : IDisposable {
		public enum VALUE_TYPE {
			INTEGER = 0,
			I8 = INTEGER | 8,
			I16 = INTEGER | 16,
			I24 = INTEGER | 24,
			I32 = INTEGER | 32,
			BIT_MASK = 255,
			FLOAT = 256,
			F32 = FLOAT | 32,
		}

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

		protected const uint WAVE_MAPPER = unchecked((uint)-1);

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

		protected WAVEFORMATEX WaveFormatEx;
		protected IntPtr DeviceHandle;
		protected IntPtr[] mpWaveHeader;
		protected int BufferSize;
		protected int BufferCount;
		protected bool Closing = false;
		protected bool Pause = false;
		protected bool Paused = true;
		protected bool Terminate = false;
		protected object LockBuffer = new object();
		Thread BufferThread;

		public bool DeviceEnabled { get; protected set; }
		public bool Playing { get; protected set; }
		public uint DeviceId { get; private set; } = WAVE_MAPPER;
		public int SampleRate { get; private set; }
		public int Channels { get; private set; }
		public int BufferSamples { get; private set; }

		protected Wave(int sampleRate, int channels, VALUE_TYPE type, int bufferSamples, int bufferCount) {
			var bits = (ushort)(type & VALUE_TYPE.BIT_MASK);
			var bytesPerSample = channels * bits >> 3;
			SampleRate = sampleRate;
			Channels = channels;
			BufferSamples = bufferSamples;
			BufferSize = bufferSamples * bytesPerSample;
			BufferCount = bufferCount;
			WaveFormatEx = new WAVEFORMATEX() {
				wFormatTag = (ushort)((type & VALUE_TYPE.FLOAT) > 0 ? 3 : 1),
				nChannels = (ushort)channels,
				nSamplesPerSec = (uint)sampleRate,
				nAvgBytesPerSec = (uint)(sampleRate * bytesPerSample),
				nBlockAlign = (ushort)bytesPerSample,
				wBitsPerSample = bits,
				cbSize = 0
			};
		}

		protected void AllocHeader() {
			var defaultValue = new byte[BufferSize];
			mpWaveHeader = new IntPtr[BufferCount];
			for (int i = 0; i < BufferCount; ++i) {
				var header = new WAVEHDR() {
					dwFlags = WAVEHDR_FLAG.WHDR_BEGINLOOP | WAVEHDR_FLAG.WHDR_ENDLOOP,
					dwBufferLength = (uint)BufferSize,
					lpData = Marshal.AllocHGlobal(BufferSize),
				};
				Marshal.Copy(defaultValue, 0, header.lpData, BufferSize);
				mpWaveHeader[i] = Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>());
				Marshal.StructureToPtr(header, mpWaveHeader[i], true);
			}
			DeviceEnabled = true;
		}

		protected void DisposeHeader() {
			for (int i = 0; i < BufferCount; ++i) {
				if (mpWaveHeader[i] == IntPtr.Zero) {
					continue;
				}
				var header = Marshal.PtrToStructure<WAVEHDR>(mpWaveHeader[i]);
				if (header.lpData != IntPtr.Zero) {
					Marshal.FreeHGlobal(header.lpData);
				}
				Marshal.FreeHGlobal(mpWaveHeader[i]);
				mpWaveHeader[i] = IntPtr.Zero;
			}
			DeviceEnabled = false;
			DeviceHandle = IntPtr.Zero;
		}

		protected void OpenDevice() {
			CloseDevice();
			BufferThread = new Thread(BufferTask) {
				Priority = ThreadPriority.Highest
			};
			BufferThread.Start();
		}

		protected void CloseDevice() {
			if (IntPtr.Zero == DeviceHandle) {
				return;
			}
			Closing = true;
			BufferThread.Join();
		}

		public void Dispose() {
			CloseDevice();
		}

		public void SetDevice(uint deviceId) {
			var enable = DeviceEnabled;
			CloseDevice();
			DeviceId = deviceId;
			if (enable) {
				OpenDevice();
			}
		}

		public void Start() {
			Pause = false;
			Paused = false;
			Playing = DeviceEnabled;
		}

		public void Stop() {
			Pause = true;
			if (Playing) {
				for (int i = 0; i < 40 && !Paused; i++) {
					Thread.Sleep(50);
				}
				Playing = false;
			}
		}

		protected abstract void BufferTask();
	}
}
