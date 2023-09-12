using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WinMM {
    class WaveOut : IDisposable {
        public const uint MAPPER = 0xFFFFFFFF;

        private delegate void DCallback(IntPtr hwo, MM_WOM uMsg, int dwUser, IntPtr dwParam1, int dwParam2);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveOutGetNumDevs")]
        private static extern uint GetNumDevs();

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveOutGetDevCaps")]
        private static extern MMRESULT GetDevCaps(int uDeviceID, IntPtr pwoc, int cbwoc);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveOutOpen")]
        private static extern MMRESULT Open(
            ref IntPtr phwo,
            uint uDeviceID,
            ref WAVEFORMATEX pwfx,
            DCallback dwCallback,
            IntPtr dwInstance,
            CALLBACK dwFlags
        );

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveOutClose")]
        private static extern MMRESULT Close(IntPtr hwo);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveOutPrepareHeader")]
        private static extern MMRESULT PrepareHeader(IntPtr hwo, IntPtr pwh, int cbwh);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveOutUnprepareHeader")]
        private static extern MMRESULT UnprepareHeader(IntPtr hwo, IntPtr pwh, int cbwh);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveOutReset")]
        private static extern MMRESULT Reset(IntPtr hwo);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveOutWrite")]
        private static extern MMRESULT Write(IntPtr hwo, IntPtr pwh, uint cbwh);

        private IntPtr mWaveOutHandle;
        private WAVEFORMATEX mWaveFormatEx;
        private readonly WAVEHDR[] mWaveHeader;
        private readonly IntPtr[] mWaveHeaderPtr;
        private DCallback mCallback;

        protected short[] WaveBuffer;
        private int mBufferIndex;
        private bool mIsPlay;

        public int SampleRate { get; }
        public int Channels { get; }
        public int BufferSize { get; }

        public WaveOut(int sampleRate = 44100, int channels = 2, int bufferSize = 4096, int bufferCount = 4) {
            SampleRate = sampleRate;
            Channels = channels;
            BufferSize = bufferSize;
            mBufferIndex = 0;

            mWaveOutHandle = IntPtr.Zero;
            mWaveHeaderPtr = new IntPtr[bufferCount];
            mWaveHeader = new WAVEHDR[bufferCount];
            WaveBuffer = new short[BufferSize];

            Open();
        }

        public void Dispose() {
            Close();
        }

        public static List<Tuple<string, uint>> GetList() {
            var device_count = GetNumDevs();
            var waveOutCapsList = new List<Tuple<string, uint>>() {
                new Tuple<string, uint>("既定のデバイス", MAPPER)
            };
            var waveOutCaps = new WAVEOUTCAPS();
            var lpWaveOutCaps = Marshal.AllocHGlobal(Marshal.SizeOf(waveOutCaps));
            for (int i = 0; i < device_count; i++) {
                GetDevCaps(i, lpWaveOutCaps, Marshal.SizeOf(waveOutCaps));
                waveOutCaps = Marshal.PtrToStructure<WAVEOUTCAPS>(lpWaveOutCaps);
                waveOutCapsList.Add(new Tuple<string, uint>(waveOutCaps.szPname, (uint)i));
            }
            Marshal.FreeHGlobal(lpWaveOutCaps);
            return waveOutCapsList;
        }

        public void Open(uint deviceNumber = MAPPER) {
            if (IntPtr.Zero != mWaveOutHandle) {
                Close();
            }

            mWaveFormatEx = new WAVEFORMATEX() {
                wFormatTag = 1,
                nChannels = (ushort)Channels,
                nSamplesPerSec = (uint)SampleRate,
                nAvgBytesPerSec = (uint)(SampleRate * Channels * 16 >> 3),
                nBlockAlign = (ushort)(Channels * 16 >> 3),
                wBitsPerSample = 16,
                cbSize = 0
            };

            mCallback = new DCallback(Callback);
            Open(ref mWaveOutHandle, deviceNumber, ref mWaveFormatEx, mCallback, IntPtr.Zero, CALLBACK.FUNCTION);

            WaveBuffer = new short[BufferSize];

            for (int i = 0; i < mWaveHeader.Length; ++i) {
                mWaveHeaderPtr[i] = Marshal.AllocHGlobal(Marshal.SizeOf(mWaveHeader[i]));
                mWaveHeader[i].dwBufferLength = (uint)(WaveBuffer.Length * 16 >> 3);
                mWaveHeader[i].lpData = Marshal.AllocHGlobal((int)mWaveHeader[i].dwBufferLength);
                mWaveHeader[i].dwFlags = 0;
                Marshal.Copy(WaveBuffer, 0, mWaveHeader[i].lpData, WaveBuffer.Length);
                Marshal.StructureToPtr(mWaveHeader[i], mWaveHeaderPtr[i], true);

                PrepareHeader(mWaveOutHandle, mWaveHeaderPtr[i], Marshal.SizeOf(typeof(WAVEHDR)));
                Write(mWaveOutHandle, mWaveHeaderPtr[i], (uint)Marshal.SizeOf(typeof(WAVEHDR)));
            }
        }

        public void Close() {
            if (IntPtr.Zero == mWaveOutHandle) {
                return;
            }

            mIsPlay = false;

            Reset(mWaveOutHandle);
            for (int i = 0; i < mWaveHeader.Length; ++i) {
                UnprepareHeader(mWaveHeaderPtr[i], mWaveOutHandle, Marshal.SizeOf<WAVEHDR>());
                Marshal.FreeHGlobal(mWaveHeader[i].lpData);
                Marshal.FreeHGlobal(mWaveHeaderPtr[i]);
                mWaveHeader[i].lpData = IntPtr.Zero;
                mWaveHeaderPtr[i] = IntPtr.Zero;
            }
            Close(mWaveOutHandle);
            mWaveOutHandle = IntPtr.Zero;
        }

        private void Callback(IntPtr hwo, MM_WOM uMsg, int dwUser, IntPtr dwParam1, int dwParam2) {
            switch (uMsg) {
            case MM_WOM.OPEN:
                mIsPlay = true;
                break;
            case MM_WOM.CLOSE:
                break;
            case MM_WOM.DONE:
                if (!mIsPlay) {
                    break;
                }

                Write(mWaveOutHandle, dwParam1, (uint)Marshal.SizeOf(typeof(WAVEHDR)));

                for (mBufferIndex = 0; mBufferIndex < mWaveHeader.Length; ++mBufferIndex) {
                    if (mWaveHeaderPtr[mBufferIndex] == dwParam1) {
                        SetData();
                        mWaveHeader[mBufferIndex] = (WAVEHDR)Marshal.PtrToStructure(mWaveHeaderPtr[mBufferIndex], typeof(WAVEHDR));
                        Marshal.Copy(WaveBuffer, 0, mWaveHeader[mBufferIndex].lpData, WaveBuffer.Length);
                        Marshal.StructureToPtr(mWaveHeader[mBufferIndex], mWaveHeaderPtr[mBufferIndex], true);
                    }
                }
                break;
            }
        }

        protected virtual void SetData() { }
    }
}
