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
		DCallback mCallback;

		byte[] mMuteData;

		public delegate void DTerminated();
		protected DTerminated mOnTerminated = () => { };

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

		public WaveOut(int sampleRate, int channels, BUFFER_TYPE type, int bufferSamples, int bufferCount)
			: base(sampleRate, channels, type, bufferSamples, bufferCount) {
			mMuteData = new byte[WaveFormatEx.nBlockAlign * bufferSamples];
			if (WaveFormatEx.wBitsPerSample == 8) {
				for (int i = 0; i < mMuteData.Length; ++i) {
					mMuteData[i] = 128;
				}
			}
			mCallback = (hwo, uMsg, dwUser, lpWaveHdr, dwParam2) => {
				switch (uMsg) {
				case MM_WOM.OPEN:
					AllocHeader();
					mEnableCallback = true;
					Enabled = true;
					Console.WriteLine("waveOutOpen");
					break;
				case MM_WOM.CLOSE:
					DisposeHeader();
					mHandle = IntPtr.Zero;
					mEnableCallback = false;
					Enabled = false;
					Console.WriteLine("waveOutClose");
					break;
				case MM_WOM.DONE:
					if (mStop) {
						mEnableCallback = false;
						break;
					}
					lock (mLockBuffer) {
						var waveHdr = Marshal.PtrToStructure<WAVEHDR>(lpWaveHdr);
						waveHdr.dwFlags &= ~WAVEHDR_FLAG.WHDR_INQUEUE;
						Marshal.StructureToPtr(waveHdr, lpWaveHdr, false);
						waveOutWrite(hwo, lpWaveHdr, Marshal.SizeOf<WAVEHDR>());
						if (mProcessedBufferCount > 0) {
							--mProcessedBufferCount;
						}
					}
					break;
				}
			};
		}

		protected override void BufferTask() {
			mStop = false;
			mPause = false;
			mTerminate = false;
			mBufferPaused = false;
			mProcessedBufferCount = 0;
			var ret = waveOutOpen(ref mHandle, DeviceId, ref WaveFormatEx, mCallback, IntPtr.Zero, 0x00030000);
			if (MMResult.MMSYSERR_NOERROR != ret) {
				return;
			}
			foreach (var pHeader in mpWaveHeader) {
				waveOutPrepareHeader(mHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			foreach (var pHeader in mpWaveHeader) {
				waveOutWrite(mHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine("waveOutWrite");
			int writeIndex = 0;
			while (!mStop) {
				var enableWait = false;
				lock (mLockBuffer) {
					if (mProcessedBufferCount < mBufferCount) {
						var lpWaveHdr = mpWaveHeader[writeIndex];
						var waveHdr = Marshal.PtrToStructure<WAVEHDR>(lpWaveHdr);
						writeIndex = ++writeIndex % mBufferCount;
						if ((waveHdr.dwFlags & WAVEHDR_FLAG.WHDR_INQUEUE) == WAVEHDR_FLAG.WHDR_INQUEUE) {
							continue;
						}
						waveHdr.dwFlags |= WAVEHDR_FLAG.WHDR_INQUEUE;
						Marshal.StructureToPtr(waveHdr, lpWaveHdr, false);
						if (mPause || mTerminate) {
							Marshal.Copy(mMuteData, 0, waveHdr.lpData, mMuteData.Length);
							mBufferPaused = true;
							if (mTerminate) {
								mPause = true;
								mTerminate = false;
								new Task(() => { mOnTerminated(); }).Start();
							}
						}
						else {
							WriteBuffer(waveHdr.lpData);
						}
						++mProcessedBufferCount;
					} else {
						enableWait = true;
					}
				}
				if (enableWait) {
					Thread.Sleep(1);
				}
			}
			for (int i = 0; i < 40 && mEnableCallback; ++i) {
				Thread.Sleep(50);
			}
			waveOutReset(mHandle);
			for (int i = 0; i < mBufferCount; ++i) {
				waveOutUnprepareHeader(mHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			waveOutClose(mHandle);
			for (int i = 0; i < 40 && Enabled; ++i) {
				Thread.Sleep(50);
			}
		}

		protected abstract void WriteBuffer(IntPtr pBuffer);
	}
}
