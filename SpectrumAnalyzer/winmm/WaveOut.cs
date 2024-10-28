using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace WinMM {
	public abstract class WaveOut : Wave {
		[StructLayout(LayoutKind.Sequential)]
		public struct WaveOutCaps {
			public ushort wMid;
			public ushort wPid;
			public uint vDriverVersion;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string szPname;
			public EAvailableFormats dwFormats;
			public ushort wChannels;
			public ushort wReserved1;
			public uint dwSupport;
		}

		enum MM_WOM {
			OPEN = 0x3BB,
			CLOSE = 0x3BC,
			DONE = 0x3BD
		}

		delegate void DCallback(IntPtr hwo, MM_WOM uMsg, int dwUser, IntPtr lpWaveHdr, int dwParam2);
		public delegate void DStateChanged();

		#region winmm.dll
		[DllImport("winmm.dll")]
		static extern uint waveOutGetNumDevs();
		[DllImport("winmm.dll", CharSet = CharSet.Ansi)]
		static extern MMSysErr waveOutGetDevCaps(uint uDeviceID, ref WaveOutCaps pwoc, int size);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveOutOpen(ref IntPtr hwo, uint uDeviceID, ref WAVEFORMATEX lpFormat, DCallback dwCallback, IntPtr dwInstance, MMCallback dwFlags);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveOutClose(IntPtr hwo);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveOutPrepareHeader(IntPtr hwo, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveOutUnprepareHeader(IntPtr hwo, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveOutReset(IntPtr hwo);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveOutWrite(IntPtr hwo, IntPtr lpWaveHdr, int size);
		#endregion

		protected DStateChanged OnTerminated = () => { };
		protected bool EndOfFile = false;
		DCallback Callback;
		int ProcessCount = 0;
		const int PROCESS_LIMIT = 10;

		public WaveOut(int sampleRate, int channels, EBufferType bufferType, int bufferSamples, int bufferCount)
			: base(sampleRate, channels, bufferType, bufferSamples, bufferCount) {
			Callback = (hwo, uMsg, dwUser, lpWaveHdr, dwParam2) => {
				switch (uMsg) {
				case MM_WOM.OPEN:
					DeviceEnabled = true;
					break;
				case MM_WOM.CLOSE:
					DeviceEnabled = false;
					break;
				case MM_WOM.DONE:
					ProcessCount = 0;
					if (Closing) {
						break;
					}
					lock (LockBuffer) {
						var header = Marshal.PtrToStructure<WAVEHDR>(lpWaveHdr);
						header.dwFlags &= ~WHDR_FLAG.INQUEUE;
						Marshal.StructureToPtr(header, lpWaveHdr, false);
						waveOutWrite(hwo, lpWaveHdr, Marshal.SizeOf<WAVEHDR>());
					}
					break;
				}
			};
		}

		public static List<WaveOutCaps> GetDeviceList() {
			var list = new List<WaveOutCaps>();
			var deviceCount = waveOutGetNumDevs();
			for (uint i = 0; i < deviceCount; i++) {
				var caps = new WaveOutCaps();
				waveOutGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
				list.Add(caps);
			}
			return list;
		}

		protected override bool InitializeTask() {
			var res = waveOutOpen(ref DeviceHandle, DeviceId, ref WaveFormatEx, Callback, IntPtr.Zero, MMCallback.FUNCTION);
			for (int i = 0; i < 40 && !DeviceEnabled; ++i) {
				Thread.Sleep(50);
			}
			if (!DeviceEnabled) {
				Console.WriteLine($"[WaveOut:{Thread.CurrentThread.ManagedThreadId}] Device Open error:{res}");
				return false;
			}
			Console.WriteLine($"[WaveOut:{Thread.CurrentThread.ManagedThreadId}] Device Opened");
			foreach (var pHeader in WaveHeaders) {
				waveOutPrepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
				waveOutWrite(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveOut:{Thread.CurrentThread.ManagedThreadId}] Header Prepared");
			return true;
		}

		protected override void FinalizeTask() {
			if (ProcessCount < PROCESS_LIMIT) {
				waveOutReset(DeviceHandle);
				Console.WriteLine($"[WaveOut:{Thread.CurrentThread.ManagedThreadId}] Reset Device");
				foreach (var pHeader in WaveHeaders) {
					waveOutUnprepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
				}
				Console.WriteLine($"[WaveOut:{Thread.CurrentThread.ManagedThreadId}] Header Unprepared");
				waveOutClose(DeviceHandle);
				for (int i = 0; i < 40 && DeviceEnabled; ++i) {
					Thread.Sleep(50);
				}
				Console.WriteLine($"[WaveOut:{Thread.CurrentThread.ManagedThreadId}] Device Closed");
			} else {
				Console.WriteLine($"[WaveOut:{Thread.CurrentThread.ManagedThreadId}] Device Locked");
			}
		}

		protected override void Task() {
			while (!Closing) {
				for (var inQueueCount = 0; inQueueCount < BufferCount;) {
					lock (LockBuffer) {
						var pHeader = WaveHeaders[BufferIndex];
						BufferIndex = ++BufferIndex % BufferCount;
						var header = Marshal.PtrToStructure<WAVEHDR>(pHeader);
						if (0 != (header.dwFlags & WHDR_FLAG.INQUEUE)) {
							++inQueueCount;
							continue;
						}
						header.dwFlags |= WHDR_FLAG.INQUEUE;
						Marshal.StructureToPtr(header, pHeader, false);
						if (Pause || EndOfFile) {
							Marshal.Copy(MuteData, 0, header.lpData, MuteData.Length);
							Paused = true;
						} else {
							WriteBuffer(header.lpData);
						}
					}
				}
				if (++ProcessCount >= PROCESS_LIMIT) {
					Closing = true;
					break;
				}
				if (Paused && EndOfFile) {
					EndOfFile = false;
					Pause = true;
					new Task(() => { OnTerminated(); }).Start();
				}
				Thread.Sleep(1);
			}
		}

		protected abstract void WriteBuffer(IntPtr pBuffer);
	}
}
