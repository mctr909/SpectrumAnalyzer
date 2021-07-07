using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinMM {
    class WaveIn : IDisposable {
        public const uint MAPPER = 0xFFFFFFFF;

        private delegate void DCallback(IntPtr hwi, MM_WIM uMsg, int dwInstance, IntPtr dwParam1, int dwParam2);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInGetNumDevs")]
        private static extern int GetNumDevs();

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInGetDevCaps")]
        private static extern MMRESULT GetDevCaps(int uDeviceID, IntPtr pwic, int cbwic);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInOpen")]
        private static extern MMRESULT Open(
            ref IntPtr phwi,
            uint uDeviceID,
            ref WAVEFORMATEX pwfx,
            DCallback dwCallback,
            uint dwCallbackInstance,
            CALLBACK fdwOpen
        );

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInClose")]
        private static extern MMRESULT Close(IntPtr hwi);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInStart")]
        private static extern MMRESULT Start(IntPtr hwi);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInStop")]
        private static extern MMRESULT Stop(IntPtr hwi);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInReset")]
        private static extern MMRESULT Reset(IntPtr hwi);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInPrepareHeader")]
        private static extern MMRESULT PrepareHeader(IntPtr hwi, IntPtr pwh, int cbwh);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInUnprepareHeader")]
        private static extern MMRESULT UnprepareHeader(IntPtr hwi, IntPtr pwh, int cbwh);

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "waveInAddBuffer")]
        private static extern MMRESULT AddBuffer(IntPtr hwi, IntPtr pwh, int cbwh);

        private IntPtr mDeviceHandle = IntPtr.Zero;
        private WAVEFORMATEX mWaveFormatEx;
        private readonly WAVEHDR[] mWaveHeader;
        private readonly IntPtr[] mWaveHeaderPtr;
        private DCallback mCallback;

        protected short[] WaveBuffer;
        private bool mIsRec = false;

        public int SampleRate { get; }
        public int Channels { get; }
        public int BufferSize { get; }

        public static List<Tuple<string, uint>> GetList() {
            var device_count = GetNumDevs();
            var waveInCapsList = new List<Tuple<string, uint>>() {
                new Tuple<string, uint>("既定のデバイス", MAPPER)
            };
            var waveInCaps = new WAVEINCAPS();
            var lpWaveInCaps = Marshal.AllocHGlobal(Marshal.SizeOf(waveInCaps));
            for (int i = 0; i < device_count; i++) {
                GetDevCaps(i, lpWaveInCaps, Marshal.SizeOf(waveInCaps));
                waveInCaps = Marshal.PtrToStructure<WAVEINCAPS>(lpWaveInCaps);
                waveInCapsList.Add(new Tuple<string, uint>(waveInCaps.szPname, (uint)i));
            }
            Marshal.FreeHGlobal(lpWaveInCaps);
            return waveInCapsList;
        }

        public WaveIn(int sampleRate = 44100, int channels = 2, int bufferSize = 1024, int bufferCount = 8) {
            SampleRate = sampleRate;
            Channels = channels;
            BufferSize = bufferSize;

            mDeviceHandle = IntPtr.Zero;
            mWaveHeaderPtr = new IntPtr[bufferCount];
            mWaveHeader = new WAVEHDR[bufferCount];
            WaveBuffer = new short[BufferSize];

            Open();
        }

        public void Dispose() {
            Close();
        }

        public void Open(uint deviceNumber = MAPPER) {
            if (IntPtr.Zero != mDeviceHandle) {
                Close();
            }

            mIsRec = true;

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
            var res = Open(ref mDeviceHandle, deviceNumber, ref mWaveFormatEx, mCallback, 0, CALLBACK.FUNCTION);
            if (MMRESULT.MMSYSERR_NOERROR != res) {
                return;
            }

            Stop(mDeviceHandle);

            WaveBuffer = new short[BufferSize];

            for (int i = 0; i < mWaveHeader.Length; ++i) {
                mWaveHeaderPtr[i] = Marshal.AllocHGlobal(Marshal.SizeOf(mWaveHeader[i]));
                mWaveHeader[i].dwBufferLength = (uint)(WaveBuffer.Length * 16 >> 3);
                mWaveHeader[i].lpData = Marshal.AllocHGlobal((int)mWaveHeader[i].dwBufferLength);
                mWaveHeader[i].dwFlags = 0;
                mWaveHeader[i].dwLoops = 0;
                Marshal.StructureToPtr(mWaveHeader[i], mWaveHeaderPtr[i], true);
                PrepareHeader(mDeviceHandle, mWaveHeaderPtr[i], Marshal.SizeOf<WAVEHDR>());
                res = AddBuffer(mDeviceHandle, mWaveHeaderPtr[i], Marshal.SizeOf<WAVEHDR>());
                if (MMRESULT.MMSYSERR_NOERROR != res) {
                    Unprepare();
                    return;
                }
            }

            res = Start(mDeviceHandle);
            if (MMRESULT.MMSYSERR_NOERROR != res) {
                Unprepare();
                return;
            }
        }

        public void Close() {
            if (IntPtr.Zero == mDeviceHandle) {
                return;
            }
            mIsRec = false;
            Stop(mDeviceHandle);
            Reset(mDeviceHandle);
            Unprepare();
            Close(mDeviceHandle);
            mDeviceHandle = IntPtr.Zero;
        }

        private void Unprepare() {
            for (int i = 0; i < mWaveHeader.Length; ++i) {
                UnprepareHeader(mDeviceHandle, mWaveHeaderPtr[i], Marshal.SizeOf<WAVEHDR>());
                Marshal.FreeHGlobal(mWaveHeader[i].lpData);
                Marshal.FreeHGlobal(mWaveHeaderPtr[i]);
                mWaveHeader[i].lpData = IntPtr.Zero;
                mWaveHeaderPtr[i] = IntPtr.Zero;
            }
        }

        private void Callback(IntPtr hwi, MM_WIM uMsg, int dwInstance, IntPtr dwParam1, int dwParam2) {
            switch (uMsg) {
            case MM_WIM.OPEN:
                break;
            case MM_WIM.CLOSE:
                break;
            case MM_WIM.DATA:
                if (!mIsRec) {
                    return;
                }
                var wavehdr = (WAVEHDR)Marshal.PtrToStructure(dwParam1, typeof(WAVEHDR));
                if (WHDR.DONE == (wavehdr.dwFlags & WHDR.DONE)) {
                    Marshal.Copy(wavehdr.lpData, WaveBuffer, 0, WaveBuffer.Length);
                    GetData();
                    Marshal.StructureToPtr(wavehdr, dwParam1, true);
                    UnprepareHeader(hwi, dwParam1, Marshal.SizeOf<WAVEHDR>());
                    PrepareHeader(hwi, dwParam1, Marshal.SizeOf<WAVEHDR>());
                    AddBuffer(hwi, dwParam1, Marshal.SizeOf<WAVEHDR>());
                }
                break;
            }
        }

        protected virtual void GetData() { }
    }
}