using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinMM {
	public abstract class WaveIn : Wave {
		[StructLayout(LayoutKind.Sequential)]
		public struct WaveInCaps {
			public ushort wMid;
			public ushort wPid;
			public uint vDriverVersion;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string szPname;
			public EAvailableFormats dwFormats;
			public ushort wChannels;
			public ushort wReserved1;
		}

		enum MM_WIM {
			OPEN = 0x3BE,
			CLOSE = 0x3BF,
			DATA = 0x3C0
		}

		delegate void DCallback(IntPtr hwi, MM_WIM uMsg, int dwUser, IntPtr lpWaveHdr, int dwParam2);
		DCallback Callback;

		#region winmm.dll
		[DllImport("winmm.dll")]
		static extern uint waveInGetNumDevs();
		[DllImport("winmm.dll", CharSet = CharSet.Ansi)]
		static extern MMSysErr waveInGetDevCaps(uint uDeviceID, ref WaveInCaps pwic, int size);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveInOpen(ref IntPtr hwi, uint uDeviceID, ref WAVEFORMATEX lpFormat, DCallback dwCallback, IntPtr dwInstance, MMCallback dwFlags);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveInClose(IntPtr hwi);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveInPrepareHeader(IntPtr hwi, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveInUnprepareHeader(IntPtr hwi, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveInReset(IntPtr hwi);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveInAddBuffer(IntPtr hwi, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveInStart(IntPtr hwi);
		#endregion

		public WaveIn(int sampleRate, int channels, EBufferType bufferType, int bufferSamples, int bufferCount)
			: base(sampleRate, channels, bufferType, bufferSamples, bufferCount) {
			Callback = (hwi, uMsg, dwUser, lpWaveHdr, dwParam2) => {
				switch (uMsg) {
				case MM_WIM.OPEN:
					DeviceEnabled = true;
					break;
				case MM_WIM.CLOSE:
					DeviceEnabled = false;
					break;
				case MM_WIM.DATA:
					CallbackEnabled = true;
					if (Closing) {
						break;
					}
					lock (LockBuffer) {
						var header = Marshal.PtrToStructure<WAVEHDR>(lpWaveHdr);
						header.dwFlags |= WHDR_FLAG.INQUEUE;
						Marshal.StructureToPtr(header, lpWaveHdr, false);
						waveInAddBuffer(hwi, lpWaveHdr, Marshal.SizeOf<WAVEHDR>());
					}
					break;
				}
			};
		}

		public static List<WaveInCaps> GetDeviceList() {
			var list = new List<WaveInCaps>();
			var deviceCount = waveInGetNumDevs();
			for (uint i = 0; i < deviceCount; i++) {
				var caps = new WaveInCaps();
				waveInGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
				list.Add(caps);
			}
			return list;
		}

		protected override bool InitializeTask() {
			var res = waveInOpen(ref DeviceHandle, DeviceId, ref WaveFormatEx, Callback, IntPtr.Zero, MMCallback.FUNCTION);
			WaitEnable(ref DeviceEnabled);
			if (DeviceEnabled) {
				Console.WriteLine($"[WaveIn] Device Enabled");
			} else {
				Console.WriteLine($"[WaveIn] Device Open error:{res}");
				return false;
			}
			foreach (var pHeader in WaveHeaders) {
				waveInPrepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
				waveInAddBuffer(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveIn] Header Prepared");
			waveInStart(DeviceHandle);
			WaitEnable(ref CallbackEnabled);
			if (CallbackEnabled) {
				Console.WriteLine($"[WaveIn] Callback Enabled");
			} else {
				Console.WriteLine($"[WaveIn] Callback error");
				return false;
			}
			return true;
		}

		protected override void FinalizeTask() {
			waveInReset(DeviceHandle);
			Console.WriteLine($"[WaveIn] Reset Device");
			foreach (var pHeader in WaveHeaders) {
				waveInUnprepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveIn] Header Unprepared");
			waveInClose(DeviceHandle);
			WaitDisable(ref DeviceEnabled);
			Console.WriteLine($"[WaveIn] Device Closed");
		}

		protected override void Task() {
			while (!Closing) {
				for (var nonQueueCount = 0; nonQueueCount < BufferCount;) {
					lock (LockBuffer) {
						var pHeader = WaveHeaders[BufferIndex];
						BufferIndex = ++BufferIndex % BufferCount;
						var header = Marshal.PtrToStructure<WAVEHDR>(pHeader);
						if (0 == (header.dwFlags & WHDR_FLAG.INQUEUE)) {
							++nonQueueCount;
							continue;
						}
						header.dwFlags &= ~WHDR_FLAG.INQUEUE;
						Marshal.StructureToPtr(header, pHeader, false);
						if (Pause) {
							Paused = true;
						}
						else {
							ReadBuffer(header.lpData);
						}
					}
				}
				Thread.Sleep(1);
			}
		}

		protected abstract void ReadBuffer(IntPtr pInput);
	}
}
