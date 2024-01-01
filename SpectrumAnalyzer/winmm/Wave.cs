using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinMM {
	public abstract class Wave : IDisposable {
		public enum BUFFER_TYPE {
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

		[StructLayout(LayoutKind.Sequential)]
		public struct WAVEFORMATEX {
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
		public WAVEFORMATEX WaveFormatEx;
		protected IntPtr mHandle;
		protected IntPtr[] mpWaveHeader;
		protected int mBufferSize;
		protected int mBufferCount;
		protected int mProcessedBufferCount = 0;
		protected bool mStop = false;
		protected bool mPause = false;
		protected bool mTerminate = false;
		protected bool mBufferPaused = true;
		protected bool mEnableCallback = true;
		protected object mLockBuffer = new object();
		Thread mBufferThread;
		#endregion

		#region property
		public bool Enabled { get; protected set; }
		public bool Playing { get; protected set; }
		public uint DeviceId { get; private set; } = WAVE_MAPPER;
		public int SampleRate { get; private set; }
		public int Channels { get; private set; }
		public int BufferSamples { get; private set; }
		#endregion

		protected Wave(int sampleRate, int channels, BUFFER_TYPE type, int bufferSamples, int bufferCount) {
			var bits = (ushort)(type & BUFFER_TYPE.BIT_MASK);
			var bytesPerSample = channels * bits >> 3;
			SampleRate = sampleRate;
			Channels = channels;
			BufferSamples = bufferSamples;
			mBufferSize = bufferSamples * bytesPerSample;
			mBufferCount = bufferCount;
			WaveFormatEx = new WAVEFORMATEX() {
				wFormatTag = (ushort)((type & BUFFER_TYPE.FLOAT) > 0 ? 3 : 1),
				nChannels = (ushort)channels,
				nSamplesPerSec = (uint)sampleRate,
				nAvgBytesPerSec = (uint)(sampleRate * bytesPerSample),
				nBlockAlign = (ushort)bytesPerSample,
				wBitsPerSample = bits,
				cbSize = 0
			};
		}

		protected void AllocHeader() {
			var defaultValue = new byte[mBufferSize];
			mpWaveHeader = new IntPtr[mBufferCount];
			for (int i = 0; i < mBufferCount; ++i) {
				var header = new WAVEHDR() {
					dwFlags = WAVEHDR_FLAG.WHDR_NONE,
					dwBufferLength = (uint)mBufferSize,
					lpData = Marshal.AllocHGlobal(mBufferSize)
				};
				Marshal.Copy(defaultValue, 0, header.lpData, mBufferSize);
				mpWaveHeader[i] = Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>());
				Marshal.StructureToPtr(header, mpWaveHeader[i], true);
			}
		}

		protected void DisposeHeader() {
			for (int i = 0; i < mBufferCount; ++i) {
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
		}

		protected void OpenDevice() {
			CloseDevice();
			mBufferThread = new Thread(BufferTask) {
				Priority = ThreadPriority.Highest
			};
			mBufferThread.Start();
		}

		protected void CloseDevice() {
			if (IntPtr.Zero == mHandle) {
				return;
			}
			mStop = true;
			mBufferThread.Join();
		}

		public void Dispose() {
			CloseDevice();
		}

		public void SetDevice(uint deviceId) {
			var enable = Enabled;
			CloseDevice();
			DeviceId = deviceId;
			if (enable) {
				OpenDevice();
			}
		}

		public void Pause() {
			mPause = true;
			if (Playing) {
				for (int i = 0; i < 40 && !mBufferPaused; i++) {
					Thread.Sleep(50);
				}
			}
			Playing = false;
		}

		public void Start() {
			mPause = false;
			mBufferPaused = false;
			Playing = Enabled;
		}

		protected abstract void BufferTask();
	}
}
