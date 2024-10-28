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

		private enum MM_WOM {
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

		protected DStateChanged OnEndOfFile = () => { };
		protected bool EndOfFile = false;
		private readonly DCallback Callback;
		private int ProcessInterval = 0;
		private const int PROCESS_TIMEOUT = 100;

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
					CallbackEnabled = true;
					ProcessInterval = 0;
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
			WaitEnable(ref DeviceEnabled);
			if (DeviceEnabled) {
				Console.WriteLine($"[WaveOut] Device Enabled");
			} else {
				Console.WriteLine($"[WaveOut] Device Open error:{res}");
				return false;
			}
			foreach (var pHeader in WaveHeaders) {
				waveOutPrepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
				waveOutWrite(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveOut] Header Prepared");
			WaitEnable(ref CallbackEnabled);
			if (CallbackEnabled) {
				Console.WriteLine($"[WaveOut] Callback Enabled");
			} else {
				Console.WriteLine($"[WaveOut] Callback error");
				return false;
			}
			return true;
		}

		protected override void FinalizeTask() {
			if (ProcessInterval < PROCESS_TIMEOUT) {
				waveOutReset(DeviceHandle);
				Console.WriteLine($"[WaveOut] Reset Device");
				foreach (var pHeader in WaveHeaders) {
					waveOutUnprepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
				}
				Console.WriteLine($"[WaveOut] Header Unprepared");
				waveOutClose(DeviceHandle);
				WaitDisable(ref DeviceEnabled);
				Console.WriteLine($"[WaveOut] Device Closed");
			} else {
				Console.WriteLine($"[WaveOut] Device Locked");
			}
		}

		protected override void Task() {
			while (!Closing) {
				for (var nonInqueues = BufferCount; nonInqueues != 0;) {
					lock (LockBuffer) {
						var pHeader = WaveHeaders[BufferIndex];
						BufferIndex = ++BufferIndex % BufferCount;
						var header = Marshal.PtrToStructure<WAVEHDR>(pHeader);
						if (0 != (header.dwFlags & WHDR_FLAG.INQUEUE)) {
							nonInqueues--;
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
				if (++ProcessInterval >= PROCESS_TIMEOUT) {
					Closing = true;
					break;
				}
				if (Paused && EndOfFile) {
					EndOfFile = false;
					Pause = true;
					new Task(() => { OnEndOfFile(); }).Start();
				}
				Thread.Sleep(1);
			}
		}

		protected abstract void WriteBuffer(IntPtr pBuffer);
	}
}
