using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace WinMM {
	public abstract class WaveOut : Wave {
		enum MM_WOM {
			OPEN = 0x3BB,
			CLOSE = 0x3BC,
			DONE = 0x3BD
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		struct WAVEOUTCAPS {
			public ushort wMid;
			public ushort wPid;
			public uint vDriverVersion;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string szPname;
			private uint dwFormats;
			public ushort wChannels;
			public ushort wReserved1;
			public uint dwSupport;
			public List<WAVE_FORMAT> Formats {
				get {
					var list = new List<WAVE_FORMAT>();
					for (int i = 0, s = 1; i < 32; i++, s <<= 1) {
						if (0 < (dwFormats & s)) {
							list.Add((WAVE_FORMAT)s);
						}
					}
					return list;
				}
			}
		}

		delegate void DCallback(IntPtr hwo, MM_WOM uMsg, int dwUser, IntPtr lpWaveHdr, int dwParam2);
		DCallback Callback;

		byte[] MuteData;

		public delegate void DTerminated();
		protected DTerminated OnTerminated = () => { };

		#region dll
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern uint waveOutGetNumDevs();
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveOutGetDevCaps(uint uDeviceID, ref WAVEOUTCAPS pwoc, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveOutOpen(ref IntPtr hwo, uint uDeviceID, ref WAVEFORMATEX lpFormat, DCallback dwCallback, IntPtr dwInstance, uint dwFlags);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveOutClose(IntPtr hwo);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveOutPrepareHeader(IntPtr hwo, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMResult waveOutUnprepareHeader(IntPtr hwo, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveOutReset(IntPtr hwo);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveOutWrite(IntPtr hwo, IntPtr lpWaveHdr, int size);
		#endregion

		public static List<string> GetDeviceList() {
			var list = new List<string>();
			var deviceCount = waveOutGetNumDevs();
			for (uint i = 0; i < deviceCount; i++) {
				var caps = new WAVEOUTCAPS();
				var ret = waveOutGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
				if (MMResult.MMSYSERR_NOERROR == ret) {
					list.Add(caps.szPname);
				}
				else {
					list.Add(ret.ToString());
				}
			}
			return list;
		}

		public WaveOut(int sampleRate, int channels, VALUE_TYPE type, int bufferSamples, int bufferCount)
			: base(sampleRate, channels, type, bufferSamples, bufferCount) {
			MuteData = new byte[WaveFormatEx.nBlockAlign * bufferSamples];
			if (WaveFormatEx.wBitsPerSample == 8) {
				for (int i = 0; i < MuteData.Length; ++i) {
					MuteData[i] = 128;
				}
			}
			Callback = (hwo, uMsg, dwUser, lpWaveHdr, dwParam2) => {
				switch (uMsg) {
				case MM_WOM.OPEN:
					AllocHeader();
					Console.WriteLine("WaveOut Device Opened");
					break;
				case MM_WOM.CLOSE:
					DisposeHeader();
					Console.WriteLine("WaveOut Device Closed");
					break;
				case MM_WOM.DONE:
					if (Closing) {
						break;
					}
					lock (LockBuffer) {
						var header = Marshal.PtrToStructure<WAVEHDR>(lpWaveHdr);
						header.dwFlags &= ~WAVEHDR_FLAG.WHDR_INQUEUE;
						Marshal.StructureToPtr(header, lpWaveHdr, false);
						waveOutWrite(hwo, lpWaveHdr, Marshal.SizeOf<WAVEHDR>());
					}
					break;
				}
			};
		}

		protected override void BufferTask() {
			Closing = false;
			Terminate = false;
			Pause = false;
			Paused = false;
			DeviceEnabled = false;
			var ret = waveOutOpen(ref DeviceHandle, DeviceId, ref WaveFormatEx, Callback, IntPtr.Zero, 0x00030000);
			if (MMResult.MMSYSERR_NOERROR != ret) {
				return;
			}
			for (int i = 0; i < 40 && !DeviceEnabled; ++i) {
				Thread.Sleep(50);
			}
			foreach (var pHeader in mpWaveHeader) {
				waveOutPrepareHeader(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
				waveOutWrite(DeviceHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine("WaveOut Header Prepared");
			var bufferIndex = 0;
			var sleepThreshold = BufferCount * 3 / 4;
			while (!Closing) {
				for (var inqueueCount = 0; inqueueCount < sleepThreshold;) {
					lock (LockBuffer) {
						var pHeader = mpWaveHeader[bufferIndex];
						bufferIndex = ++bufferIndex % BufferCount;
						var header = Marshal.PtrToStructure<WAVEHDR>(pHeader);
						if (0 != (header.dwFlags & WAVEHDR_FLAG.WHDR_INQUEUE)) {
							++inqueueCount;
							continue;
						}
						header.dwFlags |= WAVEHDR_FLAG.WHDR_INQUEUE;
						Marshal.StructureToPtr(header, pHeader, false);
						if (Terminate || Pause) {
							Marshal.Copy(MuteData, 0, header.lpData, MuteData.Length);
							Paused = true;
						}
						else {
							WriteBuffer(header.lpData);
						}
					}
				}
				if (Terminate && Paused) {
					Terminate = false;
					Pause = true;
					new Task(() => { OnTerminated(); }).Start();
				}
				Thread.Sleep(1);
			}
			waveOutReset(DeviceHandle);
			for (int i = 0; i < BufferCount; ++i) {
				waveOutUnprepareHeader(DeviceHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine("WaveOut Header Unprepared");
			waveOutClose(DeviceHandle);
			for (int i = 0; i < 40 && DeviceEnabled; ++i) {
				Thread.Sleep(50);
			}
		}

		protected abstract void WriteBuffer(IntPtr pBuffer);
	}
}
