using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinMM {
	public abstract class WaveIn : Wave {
		enum MM_WIM {
			OPEN = 0x3BE,
			CLOSE = 0x3BF,
			DATA = 0x3C0
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		struct WAVEINCAPS {
			public ushort wMid;
			public ushort wPid;
			public uint vDriverVersion;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string szPname;
			private uint dwFormats;
			public ushort wChannels;
			public ushort wReserved1;
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

		delegate void DCallback(IntPtr hwi, MM_WIM uMsg, int dwUser, IntPtr lpWaveHdr, int dwParam2);
		DCallback Callback;
		int ProcessedBufferCount = 0;

		#region dll
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern uint waveInGetNumDevs();
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveInGetDevCaps(uint uDeviceID, ref WAVEINCAPS pwic, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveInOpen(ref IntPtr hwi, uint uDeviceID, ref WAVEFORMATEX lpFormat, DCallback dwCallback, IntPtr dwInstance, uint dwFlags = 0x00030000);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveInClose(IntPtr hwi);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveInPrepareHeader(IntPtr hwi, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMResult waveInUnprepareHeader(IntPtr hwi, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveInReset(IntPtr hwi);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveInAddBuffer(IntPtr hwi, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMResult waveInStart(IntPtr hwi);
		#endregion

		public static List<string> GetDeviceList() {
			var list = new List<string>();
			var deviceCount = waveInGetNumDevs();
			for (uint i = 0; i < deviceCount; i++) {
				var caps = new WAVEINCAPS();
				var ret = waveInGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
				if (MMResult.MMSYSERR_NOERROR == ret) {
					list.Add(caps.szPname);
				}
				else {
					list.Add(ret.ToString());
				}
			}
			return list;
		}

		public WaveIn(int sampleRate, int channels, VALUE_TYPE type, int bufferSamples, int bufferCount)
			: base(sampleRate, channels, type, bufferSamples, bufferCount) {
			Callback = (hwi, uMsg, dwUser, lpWaveHdr, dwParam2) => {
				switch (uMsg) {
				case MM_WIM.OPEN:
					AllocHeader();
					break;
				case MM_WIM.CLOSE:
					DisposeHeader();
					break;
				case MM_WIM.DATA:
					lock (LockBuffer) {
						if (Closing) {
							break;
						}
						waveInAddBuffer(hwi, lpWaveHdr, Marshal.SizeOf<WAVEHDR>());
						if (ProcessedBufferCount > 0) {
							ProcessedBufferCount--;
						}
					}
					break;
				}
			};
		}

		protected override void BufferTask() {
			Closing = false;
			Pause = false;
			Paused = false;
			ProcessedBufferCount = 0;
			var mr = waveInOpen(ref DeviceHandle, DeviceId, ref WaveFormatEx, Callback, IntPtr.Zero);
			if (MMResult.MMSYSERR_NOERROR != mr) {
				return;
			}
			for (int i = 0; i < BufferCount; ++i) {
				waveInPrepareHeader(DeviceHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
				waveInAddBuffer(DeviceHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			waveInStart(DeviceHandle);
			var readIndex = 0;
			while (!Closing) {
				var enableWait = false;
				lock (LockBuffer) {
					if (BufferCount <= ProcessedBufferCount + 1) {
						enableWait = true;
					}
					else {
						if (Pause) {
							Paused = true;
						}
						else {
							var header = Marshal.PtrToStructure<WAVEHDR>(mpWaveHeader[readIndex]);
							ReadBuffer(header.lpData);
							readIndex = (readIndex + 1) % BufferCount;
							ProcessedBufferCount++;
						}
					}
				}
				if (enableWait) {
					Thread.Sleep(1);
				}
			}
			waveInReset(DeviceHandle);
			for (int i = 0; i < BufferCount; ++i) {
				waveInUnprepareHeader(DeviceHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			waveInClose(DeviceHandle);
			for (int i = 0; i < 40 && DeviceEnabled; ++i) {
				Thread.Sleep(50);
			}
		}

		protected abstract void ReadBuffer(IntPtr pInput);
	}
}
