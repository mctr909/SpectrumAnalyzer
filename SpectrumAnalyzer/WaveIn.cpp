#pragma once
#include "WaveIn.h"

WaveIn::WaveIn(HWND hWnd) {
    for (UINT32 i = 0; i < BUFFERS; ++i) {
        mWhdr[i] = NULL;
    }

    // 開始
    if (!IsWaveOpen) {
        IsWaveOpen = OpenDevice(hWnd);
        if (!IsWaveOpen) return;
        StartRecording();
    }
}

WaveIn::~WaveIn() {
    // WAVEデバイスを閉じる
    CloseDevice();
}

//*******************************************************************
// 録音デバイスを開く - 成功したらTRUEを返す
//*******************************************************************
bool
WaveIn::OpenDevice(HWND hWnd) {
    WAVEFORMATEX wfe;
    wfe.wFormatTag = WAVE_FORMAT_PCM;			// PCM without any compression
    wfe.nChannels = CHANNELS;
    wfe.nSamplesPerSec = SAMPLE_RATE;			// (Hz)
    wfe.wBitsPerSample = sizeof(INT16) * 8;		// (bits/channel)
    wfe.nBlockAlign = (WORD)(wfe.nChannels * wfe.wBitsPerSample / 8);
    wfe.nAvgBytesPerSec = wfe.nSamplesPerSec * wfe.nBlockAlign;
    wfe.cbSize = 0;

    mhWaveIn = NULL;
    return (waveInOpen(
        &mhWaveIn,
        WAVE_MAPPER,
        &wfe,
        (DWORD)(void*)WaveInProc,
        (DWORD)hWnd,
        CALLBACK_FUNCTION
    ) == MMSYSERR_NOERROR);
}

//*******************************************************************
// 録音デバイスを閉じる
//*******************************************************************
void
WaveIn::CloseDevice() {
    // 録音を停止する
    waveInStop(mhWaveIn);
    waveInReset(mhWaveIn);

    // ヘッダを非準備状態にする
    for (UINT32 i = 0; i < BUFFERS; ++i) {
        waveInUnprepareHeader(mhWaveIn, mWhdr[i], sizeof(WAVEHDR));
    }

    // 録音デバイスを閉じる
    waveInClose(mhWaveIn);

    //*******************************************************************
    // 割り当てた録音バッファを解放
    //*******************************************************************
    if (mhgRecBuffer != NULL) {
        GlobalFree(mhgRecBuffer);
    }

    for (UINT32 i = 0; i < BUFFERS; i++) {
        mWhdr[i] = NULL;
    }

    IsWaveOpen = false;
}

//*******************************************************************
// 録音開始
//*******************************************************************
void
WaveIn::StartRecording() {
    //*******************************************************************
    // 録音バッファを確保する
    //*******************************************************************
    if ((mhgRecBuffer = GlobalAlloc(GPTR, (sizeof(WAVEHDR) + BUFFER_SIZE) * BUFFERS)) == NULL) {
        waveInClose(mhWaveIn);
        IsWaveOpen = false;
        return;
    }

    //
    {
        LPSTR h = (LPSTR)mhgRecBuffer;
        for (UINT32 i = 0; i < BUFFERS; ++i) {
            mWhdr[i] = (LPWAVEHDR)h;
            h += sizeof(WAVEHDR);
            mWhdr[i]->lpData = h;
            mWhdr[i]->dwBufferLength = BUFFER_SIZE;
            h += mWhdr[i]->dwBufferLength;
        }
    }

    //
    {
        MMRESULT rc;

        // バッファブロックを準備して、入力キューに追加する
        for (UINT32 i = 0; i < BUFFERS; ++i) {
            rc = waveInPrepareHeader(mhWaveIn, mWhdr[i], sizeof(WAVEHDR));

            // 入力キューにバッファを追加する
            if (MMSYSERR_NOERROR == rc) {
                rc = waveInAddBuffer(mhWaveIn, mWhdr[i], sizeof(WAVEHDR));
            }
        }

        // 録音を開始する
        if (MMSYSERR_NOERROR == rc) {
            rc = waveInStart(mhWaveIn);
        }

        if (MMSYSERR_NOERROR != rc) {
            CloseDevice();	 // 割り当てられたメモリを解放する
            return;
        }
    }
}

//*******************************************************************
// データバッファブロックを再利用する
//*******************************************************************
MMRESULT
WaveIn::Reuse(LPARAM lParam) {
    MMRESULT rc = waveInPrepareHeader(mhWaveIn, (LPWAVEHDR)lParam, sizeof(WAVEHDR));
    if (MMSYSERR_NOERROR == rc) {
        rc = waveInAddBuffer(mhWaveIn, (LPWAVEHDR)lParam, sizeof(WAVEHDR));
    }

    if (MMSYSERR_NOERROR != rc) {
        CloseDevice();
    }

    return rc;
}

//*******************************************************************
// LPWAVEHDRのlpDataの値をバッファにセット
//*******************************************************************
void
WaveIn::SetBuffer(LPARAM lParam) {
    for (UINT32 i = 0; i < BUFFERS; ++i) {
        if (((LPWAVEHDR)lParam)->lpData == mWhdr[i]->lpData) {
            mpBuffer = mWhdr[i & (BUFFERS - 1)]->lpData;
            break;
        }
    }
}

//*******************************************************************
// WaveInコールバック関数
// (注) この関数内では限られたAPIしか呼べず早急に復帰する必要がある
//*******************************************************************
void CALLBACK
WaveIn::WaveInProc(HWAVEIN hwi, UINT uMsg, DWORD dwInstance, DWORD dwParam1, DWORD dwParam2) {
    switch (uMsg) {
    case WIM_DATA:
        // メッセージをポストして、受信した入力ブロックを処理する
        PostMessage((HWND)dwInstance, WM_USER, 0, dwParam1);
        break;

    default:
        break;
    }
}
