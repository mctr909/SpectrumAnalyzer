using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WinMM {
	public abstract class WaveOut : Wave {
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
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

		private delegate void DWaveOutProc(IntPtr hwo, MM_WOM uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);
		private readonly DWaveOutProc Callback;

		public delegate void DOnEndOfFile();
		protected DOnEndOfFile OnEndOfFile = () => { };

		#region winmm.dll
		[DllImport("winmm.dll")]
		static extern uint waveOutGetNumDevs();
		[DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "waveOutGetDevCapsW")]
		static extern MMSysErr waveOutGetDevCaps(uint uDeviceID, ref WaveOutCaps pwoc, int size);
		[DllImport("winmm.dll")]
		static extern MMSysErr waveOutOpen(ref IntPtr hwo, uint uDeviceID, ref WAVEFORMATEX lpFormat, DWaveOutProc dwCallback, IntPtr dwInstance, MMCallback dwFlags);
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

		public WaveOut(int sampleRate, double unitTime, int unitCount) : base(sampleRate, unitTime, unitCount) {
			Callback = (hwo, uMsg, dwInstance, dwParam1, dwParam2) => {
				switch (uMsg) {
				case MM_WOM.OPEN:
					DeviceOpened.Set();
					break;
				case MM_WOM.CLOSE:
					DeviceClosed.Set();
					break;
				case MM_WOM.DONE:
					if (NotifyClose) {
						break;
					}
					CallbackEnabled.Set();
					BufferReady.Set();
					break;
				}
			};
		}

		~WaveOut() {
			Dispose();
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
			if (!DeviceOpened.WaitOne(DeviceTimeout)) {
				Console.WriteLine($"[WaveOut] Device Open Timeout:{res}");
				return false;
			}
			if (res == MMSysErr.NOERROR) {
				Console.WriteLine($"[WaveOut] Device Opened");
			} else {
				Console.WriteLine($"[WaveOut] Device Open error:{res}");
				return false;
			}

			foreach (var pHeader in WaveHeaderPtrs) {
				var header = Marshal.PtrToStructure<WAVEHDR>(pHeader);
				Marshal.Copy(MuteData, 0, header.lpData, MuteData.Length);
				Marshal.StructureToPtr(header, pHeader, false);
				waveOutPrepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveOut] Header Prepared");

			foreach (var pHeader in WaveHeaderPtrs) {
				waveOutWrite(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveOut] First write Completed");

			if (CallbackEnabled.WaitOne(DeviceTimeout)) {
				Console.WriteLine($"[WaveOut] Callback Enabled");
			} else {
				Console.WriteLine($"[WaveOut] Callback Start Timeout");
				return false;
			}

			return true;
		}

		protected override void FinalizeTask() {
			waveOutReset(DeviceHandle);
			Console.WriteLine($"[WaveOut] Reset Device");

			foreach (var pHeader in WaveHeaderPtrs) {
				waveOutUnprepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine($"[WaveOut] Header Unprepared");

			waveOutClose(DeviceHandle);
			if (DeviceClosed.WaitOne(DeviceTimeout)) {
				Console.WriteLine($"[WaveOut] Device Closed");
			} else {
				Console.WriteLine($"[WaveOut] Device Close Timeout");
			}
		}

		protected override void BufferTask() {
			int writeIndex = 0;
			while (!NotifyClose) {
				if (Muted && NotifyEndOfFile) {
					NotifyEndOfFile = false;
					Task.Run(() => OnEndOfFile.Invoke());
				}
				if (!BufferReady.WaitOne(BufferTaskTimeout)) {
					Console.WriteLine($"[WaveOut] Buffer task Timeout");
					NotifyClose = true;
					break;
				}
				while (!NotifyClose) {
					var pHeader = WaveHeaderPtrs[writeIndex];
					var header = Marshal.PtrToStructure<WAVEHDR>(pHeader);
					if (0 == (header.dwFlags & WHDR_FLAGS.DONE)) {
						break;
					} else {
						writeIndex = ++writeIndex % BufferCount;
					}
					if (NotifyMute || Muted || NotifyEndOfFile) {
						Marshal.Copy(MuteData, 0, header.lpData, MuteData.Length);
						Muted = true;
					} else {
						WriteBuffer(header.lpData);
					}
					waveOutWrite(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
				}
			}
		}

		protected abstract void WriteBuffer(IntPtr pBuffer);
	}
}
