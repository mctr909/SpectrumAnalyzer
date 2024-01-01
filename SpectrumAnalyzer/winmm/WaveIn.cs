using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace WINMM {
	public abstract class WaveIn : WaveLib {
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
		DCallback mCallback;

		#region dll
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern uint waveInGetNumDevs();
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveInGetDevCaps(uint uDeviceID, ref WAVEINCAPS pwic, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveInOpen(ref IntPtr hwi, uint uDeviceID, ref WAVEFORMATEX lpFormat, DCallback dwCallback, IntPtr dwInstance, uint dwFlags = 0x00030000);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveInClose(IntPtr hwi);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveInPrepareHeader(IntPtr hwi, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMRESULT waveInUnprepareHeader(IntPtr hwi, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveInReset(IntPtr hwi);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveInAddBuffer(IntPtr hwi, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveInStart(IntPtr hwi);
		#endregion

		public static List<string> GetDeviceList() {
			var list = new List<string>();
			var deviceCount = waveInGetNumDevs();
			for (uint i = 0; i < deviceCount; i++) {
				var caps = new WAVEINCAPS();
				var ret = waveInGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
				if (MMRESULT.MMSYSERR_NOERROR == ret) {
					list.Add(caps.szPname);
				} else {
					list.Add(ret.ToString());
				}
			}
			return list;
		}

		public WaveIn(int sampleRate = 44100, int channels = 2, int bufferSize = 256, int bufferCount = 32) :
			base(sampleRate, channels, bufferSize, bufferCount) {
			mCallback = Callback;
		}

		public override void Open() {
			Close();
			AllocHeader();
			var mr = waveInOpen(ref mHandle, DeviceId, ref mWaveFormatEx, mCallback, IntPtr.Zero);
			if (MMRESULT.MMSYSERR_NOERROR != mr) {
				return;
			}
			for (int i = 0; i < mBufferCount; ++i) {
				waveInPrepareHeader(mHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
				waveInAddBuffer(mHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			waveInStart(mHandle);
		}

		public override void Close() {
			if (IntPtr.Zero == mHandle) {
				return;
			}
			mDoStop = true;
			for (int i = 0; i < 20 && !mStopped; i++) {
				Thread.Sleep(100);
			}
			for (int i = 0; i < mBufferCount; ++i) {
				waveInUnprepareHeader(mpWaveHeader[i], mHandle, Marshal.SizeOf<WAVEHDR>());
			}
			var mr = waveInReset(mHandle);
			if (MMRESULT.MMSYSERR_NOERROR != mr) {
				throw new Exception(mr.ToString());
			}
			mr = waveInClose(mHandle);
			if (MMRESULT.MMSYSERR_NOERROR != mr) {
				throw new Exception(mr.ToString());
			}
			mHandle = IntPtr.Zero;
			DisposeHeader();
		}

		void Callback(IntPtr hwi, MM_WIM uMsg, int dwUser, IntPtr lpWaveHdr, int dwParam2) {
			switch (uMsg) {
			case MM_WIM.OPEN:
				mStopped = false;
				Enabled = true;
				break;
			case MM_WIM.CLOSE:
				mDoStop = false;
				Enabled = false;
				break;
			case MM_WIM.DATA:
				if (mDoStop) {
					mStopped = true;
					break;
				}
				var hdr = Marshal.PtrToStructure<WAVEHDR>(lpWaveHdr);
				ReadBuffer(hdr.lpData);
				waveInAddBuffer(mHandle, lpWaveHdr, Marshal.SizeOf<WAVEHDR>());
				break;
			}
		}

		protected abstract void ReadBuffer(IntPtr pInput);
	}

}
