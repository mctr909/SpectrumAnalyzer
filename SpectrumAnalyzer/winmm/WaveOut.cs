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
		static extern MMRESULT waveOutGetDevCaps(uint uDeviceID, ref WAVEOUTCAPS pwoc, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveOutOpen(ref IntPtr hwo, uint uDeviceID, ref WAVEFORMATEX lpFormat, DCallback dwCallback, IntPtr dwInstance, uint dwFlags);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveOutClose(IntPtr hwo);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveOutPrepareHeader(IntPtr hwo, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll")]
		static extern MMRESULT waveOutUnprepareHeader(IntPtr hwo, IntPtr lpWaveHdr, int size);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveOutReset(IntPtr hwo);
		[DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern MMRESULT waveOutWrite(IntPtr hwo, IntPtr lpWaveHdr, int size);
		#endregion

		public static List<string> GetDeviceList() {
			var list = new List<string>();
			var deviceCount = waveOutGetNumDevs();
			for (uint i = 0; i < deviceCount; i++) {
				var caps = new WAVEOUTCAPS();
				var ret = waveOutGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
				if (MMRESULT.MMSYSERR_NOERROR == ret) {
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
					Enabled = true;
					Console.WriteLine("waveOutOpen");
					break;
				case MM_WOM.CLOSE:
					DisposeHeader();
					mHandle = IntPtr.Zero;
					Enabled = false;
					Console.WriteLine("waveOutClose");
					break;
				case MM_WOM.DONE:
					lock (mLockBuffer) {
						if (mStop) {
							break;
						}
						waveOutWrite(hwo, lpWaveHdr, Marshal.SizeOf<WAVEHDR>());
						var header = Marshal.PtrToStructure<WAVEHDR>(lpWaveHdr);
						header.dwFlags |= WAVEHDR_FLAG.WHDR_DONE;
						Marshal.StructureToPtr(header, lpWaveHdr, true);
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
			var ret = waveOutOpen(ref mHandle, DeviceId, ref WaveFormatEx, mCallback, IntPtr.Zero, 0x00030000);
			if (MMRESULT.MMSYSERR_NOERROR != ret) {
				return;
			}
			foreach (var pHeader in mpWaveHeader) {
				waveOutPrepareHeader(mHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			foreach (var pHeader in mpWaveHeader) {
				waveOutWrite(mHandle, pHeader, Marshal.SizeOf<WAVEHDR>());
			}
			Console.WriteLine("waveOutWrite");
			while (!mStop) {
				var enableWait = false;
				lock (mLockBuffer) {
					int writeCount = 0;
					foreach (var pHeader in mpWaveHeader) {
						var header = Marshal.PtrToStructure<WAVEHDR>(pHeader);
						if ((header.dwFlags & WAVEHDR_FLAG.WHDR_DONE) == WAVEHDR_FLAG.WHDR_NONE) {
							continue;
						}
						if (mPause || mTerminate) {
							Marshal.Copy(mMuteData, 0, header.lpData, mMuteData.Length);
							mBufferPaused = true;
							if (mTerminate) {
								mPause = true;
								mTerminate = false;
								new Task(() => { mOnTerminated(); }).Start();
							}
						}
						else {
							WriteBuffer(header.lpData);
						}
						header.dwFlags &= ~WAVEHDR_FLAG.WHDR_DONE;
						Marshal.StructureToPtr(header, pHeader, true);
						++writeCount;
					}
					enableWait = writeCount < mBufferCount / 4;
				}
				if (enableWait) {
					Thread.Sleep(10);
				}
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
