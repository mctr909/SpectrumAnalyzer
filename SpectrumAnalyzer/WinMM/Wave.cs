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

		[Flags]
		protected enum WHDR_FLAGS : uint {
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
			public UIntPtr dwUser;
			public WHDR_FLAGS dwFlags;
			public uint dwLoops;
			public IntPtr lpNext;
			public UIntPtr reserved;
		}

		protected const uint DefaultDevice = unchecked((uint)-1);
		protected const int DeviceTimeout = 2000;
		protected const int BufferTaskTimeout = 500;

		protected IntPtr DeviceHandle;
		protected WAVEFORMATEX WaveFormatEx;
		protected readonly IntPtr[] WaveHeaderPtrs;
		protected readonly int BufferFrames;
		protected readonly int BufferSize;
		protected readonly int BufferCount;
		protected readonly byte[] MuteData;
		protected readonly AutoResetEvent DeviceOpened;
		protected readonly AutoResetEvent DeviceClosed;
		protected readonly AutoResetEvent CallbackEnabled;
		protected readonly AutoResetEvent BufferReady;
		protected volatile bool NotifyClose;
		protected volatile bool NotifyEndOfFile;
		protected volatile bool NotifyMute;
		protected volatile bool Muted;
		private IntPtr TotalWaveHeader;
		private IntPtr TotalBuffer;
		private Thread BufferThread;

		public const ushort FormatTag = 3;
		public const ushort Bits = 32;
		public const ushort Channels = 2;
		public readonly int SampleRate;

		public uint DeviceId { get; private set; }

		public bool IsPlaying => !Muted;

		protected Wave(int sampleRate, double unitTime, int unitCount) {
			var bytesPerFrame = Bits * Channels >> 3;
			SampleRate = sampleRate;
			DeviceId = DefaultDevice;
			BufferFrames = (int)(sampleRate * unitTime) * unitCount;
			BufferSize = bytesPerFrame * BufferFrames;
			BufferCount = unitCount * 4;
			MuteData = new byte[BufferSize];
			WaveFormatEx = new WAVEFORMATEX()
			{
				wFormatTag = FormatTag,
				nChannels = Channels,
				nSamplesPerSec = (uint)sampleRate,
				nAvgBytesPerSec = (uint)(bytesPerFrame * sampleRate),
				nBlockAlign = (ushort)bytesPerFrame,
				wBitsPerSample = Bits,
				cbSize = 0
			};
			WaveHeaderPtrs = new IntPtr[BufferCount];
			DeviceOpened = new AutoResetEvent(false);
			DeviceClosed = new AutoResetEvent(false);
			CallbackEnabled = new AutoResetEvent(false);
			BufferReady = new AutoResetEvent(false);
			TotalWaveHeader = Marshal.AllocHGlobal(Marshal.SizeOf<WAVEHDR>() * BufferCount);
			TotalBuffer = Marshal.AllocHGlobal(BufferSize * BufferCount);
		}

		~Wave() {
			Free();
		}

		private void Free() {
			DeviceOpened?.Dispose();
			DeviceClosed?.Dispose();
			CallbackEnabled?.Dispose();
			BufferReady?.Dispose();
			if (IntPtr.Zero != TotalWaveHeader) {
				Marshal.FreeHGlobal(TotalWaveHeader);
				TotalWaveHeader = IntPtr.Zero;
			}
			if (IntPtr.Zero != TotalBuffer) {
				Marshal.FreeHGlobal(TotalBuffer);
				TotalBuffer = IntPtr.Zero;
			}
		}

		private void ResetHeader() {
			for (int i = 0; i < BufferCount; ++i) {
				var header = new WAVEHDR()
				{
					dwBufferLength = (uint)BufferSize,
					lpData = IntPtr.Add(TotalBuffer, BufferSize * i),
				};
				WaveHeaderPtrs[i] = IntPtr.Add(TotalWaveHeader, Marshal.SizeOf<WAVEHDR>() * i);
				Marshal.Copy(MuteData, 0, header.lpData, BufferSize);
				Marshal.StructureToPtr(header, WaveHeaderPtrs[i], false);
			}
		}

		private void ClearStatus() {
			DeviceOpened.Reset();
			DeviceClosed.Reset();
			CallbackEnabled.Reset();
			BufferReady.Reset();
			NotifyClose = false;
			NotifyEndOfFile = false;
			NotifyMute = false;
			Muted = true;
		}

		protected void OpenDevice() {
			CloseDevice();
			BufferThread = new Thread(() => {
				ResetHeader();
				ClearStatus();
				if (InitializeTask()) {
					BufferTask();
					FinalizeTask();
				}
				ClearStatus();
				DeviceHandle = IntPtr.Zero;
			})
			{
				Priority = ThreadPriority.Highest,
				IsBackground = true
			};
			BufferThread.Start();
		}

		protected void CloseDevice() {
			if (IntPtr.Zero == DeviceHandle) {
				return;
			}
			NotifyClose = true;
			BufferThread?.Join();
		}

		protected abstract bool InitializeTask();

		protected abstract void FinalizeTask();

		protected abstract void BufferTask();

		public void Dispose() {
			CloseDevice();
			Free();
			GC.SuppressFinalize(this);
		}

		public void SetDevice(uint deviceId) {
			CloseDevice();
			DeviceId = deviceId;
			OpenDevice();
		}

		public void Start() {
			NotifyMute = false;
			Muted = false;
		}

		public void Stop() {
			NotifyMute = true;
			for (int i = 0; i < 100 && !Muted; i++) {
				Thread.Sleep(10);
			}
			NotifyMute = false;
		}
	}
}
