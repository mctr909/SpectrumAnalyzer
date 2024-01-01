using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace WINMM {
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
			mCallback = (hwo, uMsg, dwUser, lpWaveHdr, dwParam2) => {
				switch (uMsg) {
				case MM_WOM.OPEN:
					mProcessedBufferCount = 0;
					mStartedBufferCount = 0;
					mStoppedBufferCount = 0;
					mStopBuffer = false;
					mCallbackStopped = false;
					break;
				case MM_WOM.CLOSE:
					break;
				case MM_WOM.DONE:
					lock (mLockBuffer) {
						if (mStopBuffer && mStartedBufferCount >= mBufferCount) {
							if (++mStoppedBufferCount == mBufferCount) {
								mCallbackStopped = true;
							}
							break;
						}
						waveOutWrite(hwo, lpWaveHdr, Marshal.SizeOf<WAVEHDR>());
						if (mProcessedBufferCount > 0) {
							mProcessedBufferCount--;
						}
						if (mStartedBufferCount < mBufferCount) {
							mStartedBufferCount++;
						}
					}
					break;
				}
			};
		}

		protected override void BufferTask() {
			Enabled = true;
			var ret = waveOutOpen(ref mHandle, DeviceId, ref mWaveFormatEx, mCallback, IntPtr.Zero, 0x00030000);
			if (MMRESULT.MMSYSERR_NOERROR != ret) {
				mHandle = IntPtr.Zero;
				Enabled = false;
				return;
			}
			AllocHeader();
			for (int i = 0; i < mBufferCount; ++i) {
				waveOutPrepareHeader(mHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			for (int i = 0; i < mBufferCount; ++i) {
				waveOutWrite(mHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			var writeIndex = 0;
			while (!mStopBuffer) {
				var enableWait = false;
				lock (mLockBuffer) {
					if (mBufferCount <= mProcessedBufferCount + 1) {
						enableWait = true;
					}
					else {
						var header = Marshal.PtrToStructure<WAVEHDR>(mpWaveHeader[writeIndex]);
						WriteBuffer(header.lpData);
						writeIndex = (writeIndex + 1) % mBufferCount;
						mProcessedBufferCount++;
					}
				}
				if (enableWait) {
					Thread.Sleep(1);
				}
			}
			for (int i = 0; i < 100 && !mCallbackStopped; i++) {
				Thread.Sleep(50);
			}
			waveOutReset(mHandle);
			for (int i = 0; i < mBufferCount; ++i) {
				waveOutUnprepareHeader(mHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			waveOutClose(mHandle);
			DisposeHeader();
			mHandle = IntPtr.Zero;
			Enabled = false;
		}

		protected abstract void WriteBuffer(IntPtr pBuffer);
	}
}
