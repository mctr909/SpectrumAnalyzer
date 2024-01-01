using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WinMM {
	public abstract class WaveIn : Wave {
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
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

		private enum MM_WIM {
			OPEN = 0x3BE,
			CLOSE = 0x3BF,
			DATA = 0x3C0
		}

		private delegate void DWaveInProc(IntPtr hwi, MM_WIM uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);
		private readonly DWaveInProc MMCallback;

		#region winmm.dll
		[DllImport("winmm.dll")]
		static extern uint waveInGetNumDevs();
		[DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "waveInGetDevCapsW")]
		static extern MMSysErr waveInGetDevCaps(uint uDeviceID, ref WaveInCaps pwic, int size);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveInOpen(ref IntPtr hwi, uint uDeviceID, ref WAVEFORMATEX lpFormat, DWaveInProc dwCallback, IntPtr dwInstance, MMCallback dwFlags);
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

		public WaveIn(int sampleRate, double unitTime, int unitCount, DNotify notify) : base(sampleRate, unitTime, unitCount, notify) {
			MMCallback = (hwi, uMsg, dwInstance, dwParam1, dwParam2) => {
				switch (uMsg) {
				case MM_WIM.OPEN:
					DeviceOpened.Set();
					break;
				case MM_WIM.CLOSE:
					DeviceClosed.Set();
					break;
				case MM_WIM.DATA:
					if (NotifyClose) {
						break;
					}
					CallbackEnabled.Set();
					BufferReady.Set();
					break;
				}
			};
		}

		~WaveIn() {
			Dispose();
		}

		public static List<WaveInCaps> GetDeviceList() {
			var list = new List<WaveInCaps>();
			var deviceCount = waveInGetNumDevs();
			for (int i = -1; i < deviceCount; i++) {
				var caps = new WaveInCaps();
				waveInGetDevCaps((uint)i, ref caps, Marshal.SizeOf(caps));
				list.Add(caps);
			}
			return list;
		}

		public string GetDeviceName() {
			var caps = new WaveInCaps();
			var res = waveInGetDevCaps(DeviceId, ref caps, Marshal.SizeOf(caps));
			if (res == MMSysErr.NOERROR) {
				return caps.szPname;
			} else {
				Console.WriteLine($"[WaveIn] Get Device Name error:{res}");
				return string.Empty;
			}
		}

		public void SetDeviceByName(string deviceName) {
		}

		protected override bool InitializeTask() {
			var res = waveInOpen(ref DeviceHandle, DeviceId, ref WaveFormatEx, MMCallback, IntPtr.Zero, WinMM.MMCallback.FUNCTION);
			if (!DeviceOpened.WaitOne(DeviceTimeout)) {
				Console.WriteLine($"[WaveIn] Device Open Timeout:{res}");
				return false;
			}
			if (res == MMSysErr.NOERROR) {
				Console.WriteLine($"[WaveIn] Device Opened");
			} else {
				Console.WriteLine($"[WaveIn] Device Open error:{res}");
				return false;
			}

			foreach (var pHeader in WaveHeaderPtrs) {
				waveInPrepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveIn] Header Prepared");

			foreach (var pHeader in WaveHeaderPtrs) {
				waveInAddBuffer(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveIn] First add buffer Completed");

			waveInStart(DeviceHandle);
			if (CallbackEnabled.WaitOne(DeviceTimeout)) {
				Console.WriteLine($"[WaveIn] Callback Enabled");
			} else {
				Console.WriteLine($"[WaveIn] Callback Start Timeout");
				return false;
			}

			return true;
		}

		protected override void FinalizeTask() {
			waveInReset(DeviceHandle);
			Console.WriteLine($"[WaveIn] Reset Device");

			foreach (var pHeader in WaveHeaderPtrs) {
				waveInUnprepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveIn] Header Unprepared");

			waveInClose(DeviceHandle);
			if (DeviceClosed.WaitOne(DeviceTimeout)) {
				Console.WriteLine($"[WaveIn] Device Closed");
			} else {
				Console.WriteLine($"[WaveIn] Device Close Timeout");
			}
		}

		protected override void BufferTask() {
			int readIndex = 0;
			while (!NotifyClose) {
				if (!BufferReady.WaitOne(BufferTaskTimeout)) {
					Console.WriteLine($"[WaveIn] Buffer task Timeout");
					NotifyClose = true;
					break;
				}
				while (!NotifyClose) {
					var pHeader = WaveHeaderPtrs[readIndex];
					var header = Marshal.PtrToStructure<WAVEHDR>(pHeader);
					if (0 == (header.dwFlags & WHDR_FLAGS.DONE)) {
						break;
					} else {
						readIndex = ++readIndex % BufferCount;
					}
					if (NotifyMute) {
						Muted = true;
					} else {
						ReadBuffer(header.lpData);
					}
					waveInAddBuffer(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
				}
			}
		}

		protected abstract void ReadBuffer(IntPtr pInput);
	}
}
