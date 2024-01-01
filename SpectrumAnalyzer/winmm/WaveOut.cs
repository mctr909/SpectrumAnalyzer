using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace WINMM {
	public abstract class WaveOut : WaveLib {
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

		#region dynamic variable
		Thread mBufferThread;
		object mLockBuffer = new object();
		int mWriteCount;
		int mWriteIndex;
		int mReadIndex;
		#endregion

		public static List<string> GetDeviceList() {
			var list = new List<string>();
			var deviceCount = waveOutGetNumDevs();
			for (uint i = 0; i < deviceCount; i++) {
				var caps = new WAVEOUTCAPS();
				var ret = waveOutGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
				if (MMRESULT.MMSYSERR_NOERROR == ret) {
					list.Add(caps.szPname);
				} else {
					list.Add(ret.ToString());
				}
			}
			return list;
		}

		public WaveOut(int sampleRate = 44100, int channels = 2, int bufferSamples = 128, int bufferCount = 64) :
			base(sampleRate, channels, bufferSamples, bufferCount) {
			mCallback = Callback;
		}

		public override void Open() {
			Close();
			AllocHeader();
			var ret = waveOutOpen(ref mHandle, DeviceId, ref mWaveFormatEx, mCallback, IntPtr.Zero, 0x00030000);
			if (MMRESULT.MMSYSERR_NOERROR != ret) {
				return;
			}
			for (int i = 0; i < mBufferCount; ++i) {
				waveOutPrepareHeader(mHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
				waveOutWrite(mHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			mBufferThread = new Thread(BufferTask) {
				Priority = ThreadPriority.Highest
			};
			mBufferThread.Start();
		}

		public override void Close() {
			if (IntPtr.Zero == mHandle) {
				return;
			}
			mDoStop = true;
			mBufferThread.Join();
			for (int i = 0; i < 20 && !mStopped; i++) {
				Thread.Sleep(100);
			}
			for (int i = 0; i < mBufferCount; ++i) {
				waveOutUnprepareHeader(mHandle, mpWaveHeader[i], Marshal.SizeOf<WAVEHDR>());
			}
			var ret = waveOutReset(mHandle);
			if (MMRESULT.MMSYSERR_NOERROR != ret) {
				throw new Exception(ret.ToString());
			}
			ret = waveOutClose(mHandle);
			if (MMRESULT.MMSYSERR_NOERROR != ret) {
				throw new Exception(ret.ToString());
			}
			mHandle = IntPtr.Zero;
			DisposeHeader();
		}

		void Callback(IntPtr hwo, MM_WOM uMsg, int dwUser, IntPtr lpWaveHdr, int dwParam2) {
			switch (uMsg) {
			case MM_WOM.OPEN:
				mStopped = false;
				Enabled = true;
				break;
			case MM_WOM.CLOSE:
				mDoStop = false;
				Enabled = false;
				break;
			case MM_WOM.DONE: {
				if (mDoStop) {
					mStopped = true;
					break;
				}
				lock (mLockBuffer) {
					waveOutWrite(mHandle, mpWaveHeader[mReadIndex], Marshal.SizeOf<WAVEHDR>());
					if (0 < mWriteCount) {
						mReadIndex = (mReadIndex + 1) % mBufferCount;
						mWriteCount--;
					}
				}
				break;
			}
			}
		}

		void BufferTask() {
			mWriteCount = 0;
			mWriteIndex = 0;
			mReadIndex = 0;
			while (!mDoStop) {
				var sleep = false;
				lock (mLockBuffer) {
					if (mBufferCount <= mWriteCount + 1) {
						sleep = true;
					} else {
						var pHdr = Marshal.PtrToStructure<WAVEHDR>(mpWaveHeader[mWriteIndex]);
						WriteBuffer(pHdr.lpData);
						mWriteIndex = (mWriteIndex + 1) % mBufferCount;
						mWriteCount++;
					}
				}
				if (sleep) {
					Thread.Sleep(1);
				}
			}
		}

		protected abstract void WriteBuffer(IntPtr pBuffer);
	}
}
