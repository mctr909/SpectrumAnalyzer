using System;
using System.Runtime.InteropServices;

namespace WinMM {
    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEFORMATEX {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEHDR {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public WHDR dwFlags;
        public uint dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Auto)]
    public struct WAVEOUTCAPS {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint dwFormats;
        public ushort wChannels;
        public ushort wReserved1;
        public uint dwSupport;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Auto)]
    public struct WAVEINCAPS {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint dwFormats;
        public ushort wChannels;
        public ushort wReserved1;
    }

    public enum MMRESULT {
        MMSYSERR_NOERROR = 0,
        MMSYSERR_ERROR = 1,
        MMSYSERR_BADDEVICEID = 2,
        MMSYSERR_NOTENABLED = 3,
        MMSYSERR_ALLOCATED = 4,
        MMSYSERR_INVALHANDLE = 5,
        MMSYSERR_NODRIVER = 6,
        MMSYSERR_NOMEM = 7,
        MMSYSERR_NOTSUPPORTED = 8,
        MMSYSERR_BADERRNUM = 9,
        MMSYSERR_INVALFLAG = 10,
        MMSYSERR_INVALPARAM = 11,
        MMSYSERR_HANDLEBUSY = 12,
        MMSYSERR_INVALIDALIAS = 13,
        MMSYSERR_BADDB = 14,
        MMSYSERR_KEYNOTFOUND = 15,
        MMSYSERR_READERROR = 16,
        MMSYSERR_WRITEERROR = 17,
        MMSYSERR_DELETEERROR = 18,
        MMSYSERR_VALNOTFOUND = 19,
        MMSYSERR_NODRIVERCB = 20,
        MMSYSERR_MOREDATA = 21,
        MMSYSERR_LASTERROR = 21
    }

    public enum MM_WOM {
        OPEN  = 0x3BB,
        CLOSE = 0x3BC,
        DONE  = 0x3BD
    }

    public enum MM_WIM {
        OPEN  = 0x3BE,
        CLOSE = 0x3BF,
        DATA  = 0x3C0
    }

    public enum MM_MIM {
        OPEN      = 0x3C1,
        CLOSE     = 0x3C2,
        DATA      = 0x3C3,
        LONGDATA  = 0x3C4,
        ERROR     = 0x3C5,
        LONGERROR = 0x3C6
    }

    public enum MM_MOM {
        OPEN  = 0x3C7,
        CLOSE = 0x3C8,
        DONE  = 0x3C9
    }

    public enum WHDR {
        DONE      = 0x00000001, /* done bit */
        PREPARED  = 0x00000002, /* set if this header has been prepared */
        BEGINLOOP = 0x00000004, /* loop start block */
        ENDLOOP   = 0x00000008, /* loop end block */
        INQUEUE   = 0x00000010  /* reserved for driver */
    };

    public enum CALLBACK {
        TYPEMASK = 0x00070000,  /* callback type mask */
        NULL     = 0x00000000,  /* no callback */
        WINDOW   = 0x00010000,  /* dwCallback is a HWND */
        TASK     = 0x00020000,  /* dwCallback is a HTASK */
        FUNCTION = 0x00030000,  /* dwCallback is a FARPROC */
        THREAD   = TASK,        /* thread ID replaces 16 bit task */
        EVENT    = 0x00050000,  /* dwCallback is an EVENT Handle */
    };
}