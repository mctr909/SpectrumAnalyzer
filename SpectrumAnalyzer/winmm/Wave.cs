using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinMM {
	public abstract class Wave : IDisposable {
		public enum EAvailableFormats : uint {
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

		protected enum WHDR_FLAG : uint {
			NONE = 0,
			DONE = 0x00000001,
			PREPARED = 0x00000002,
			BEGINLOOP = 0x00000004,
			ENDLOOP = 0x00000008,
			INQUEUE = 0x00000010
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
			public WHDR_FLAG dwUser;
			public WHDR_FLAG dwFlags;
			public uint dwLoops;
			public IntPtr lpNext;
			public uint reserved;
		}

		protected const uint WAVE_MAPPER = unchecked((uint)-1);

		protected WAVEFORMATEX WaveFormatEx;
		protected IntPtr DeviceHandle;
		protected IntPtr[] WaveHeaders;
		protected int BufferSamples;
		protected int BufferSize;
		protected int BufferCount;
		protected int BufferIndex;
		protected bool DeviceEnabled;
		protected bool CallbackEnabled;
		protected bool Closing;
		protected bool Pause;
		protected bool Paused;
		protected object LockBuffer;
		protected byte[] MuteData;
		Thread BufferThread;

		public int SampleRate { get; private set; }
		public int Channels { get; private set; }
		public uint DeviceId { get; private set; }
		public bool Playing { get; private set; }

		protected Wave(int sampleRate, int channels, int bufferSamples, int bufferCount) {
			var bits = (ushort)32;
			var bytesPerSample = channels * bits >> 3;
			SampleRate = sampleRate;
			Channels = channels;
			DeviceId = WAVE_MAPPER;
			BufferSamples = bufferSamples;
			BufferSize = bufferSamples * bytesPerSample;
			BufferCount = bufferCount;
			LockBuffer = new object();
			MuteData = new byte[BufferSize];
			WaveFormatEx = new WAVEFORMATEX() {
				wFormatTag = 3,
				nChannels = (ushort)channels,
				nSamplesPerSec = (uint)sampleRate,
				nAvgBytesPerSec = (uint)(sampleRate * bytesPerSample),
				nBlockAlign = (ushort)bytesPerSample,
				wBitsPerSample = bits,
				cbSize = 0
			};
		}

		void AllocateHeader() {
			WaveHeaders = new IntPtr[BufferCount];
			for (int i = 0; i < BufferCount; ++i) {
				var header = new WAVEHDR() {
					dwFlags = WHDR_FLAG.BEGINLOOP | WHDR_FLAG.ENDLOOP,
					dwBufferLength = (uint)BufferSize,
					lpData = Marshal.AllocHGlobal(BufferSize),
				};
				Marshal.Copy(MuteData, 0, header.lpData, BufferSize);
				WaveHeaders[i] = Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>());
				Marshal.StructureToPtr(header, WaveHeaders[i], true);
			}
			BufferIndex = 0;
		}

		void DisposeHeader() {
			for (int i = 0; i < BufferCount; ++i) {
				if (IntPtr.Zero == WaveHeaders[i]) {
					continue;
				}
				var header = Marshal.PtrToStructure<WAVEHDR>(WaveHeaders[i]);
				if (IntPtr.Zero != header.lpData) {
					Marshal.FreeHGlobal(header.lpData);
				}
				Marshal.FreeHGlobal(WaveHeaders[i]);
				WaveHeaders[i] = IntPtr.Zero;
			}
		}

		void ClearFlags() {
			Closing = false;
			Paused = false;
			DeviceEnabled = false;
			CallbackEnabled = false;
		}

		protected void OpenDevice() {
			CloseDevice();
			BufferThread = new Thread(() => {
				AllocateHeader();
				ClearFlags();
				if (InitializeTask()) {
					Task();
					FinalizeTask();
				}
				DisposeHeader();
				ClearFlags();
				DeviceHandle = IntPtr.Zero;
			}) {
				Priority = ThreadPriority.Highest,
				IsBackground = false
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
				WaitEnable(ref Paused);
				Playing = false;
			}
		}

		protected static void WaitEnable(ref bool flag) {
			for (int i = 0; i < 100 && !flag; i++) {
				Thread.Sleep(10);
			}
		}
		protected static void WaitDisable(ref bool flag) {
			for (int i = 0; i < 100 && flag; i++) {
				Thread.Sleep(10);
			}
		}

		protected abstract bool InitializeTask();
		protected abstract void FinalizeTask();
		protected abstract void Task();
	}
}
