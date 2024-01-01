#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <vector>
#include <thread>

#include <Windows.h>
#include <mmsystem.h>
#pragma comment (lib, "winmm.lib")

#include "wave.h"
#include "wave_out.h"

WaveOut::WaveOut(BUFFER_TYPE type,
	int32_t sampleRate,
	int32_t channels,
	int32_t bufferSamples,
	int32_t bufferCount
) : Wave(type,
	sampleRate,
	channels,
	bufferSamples,
	bufferCount
) {
	mfpBufferTask = BufferTask;
}

void
WaveOut::Callback(HWAVEOUT hwo, UINT uMsg, DWORD_PTR dwInstance, DWORD dwParam1, DWORD dwParam2) {
	auto pHdr = (LPWAVEHDR)dwInstance;
	auto pThis = (WaveOut*)pHdr->dwUser;
	switch (uMsg) {
	case MM_WOM_OPEN:
		pThis->AllocHeader();
		pThis->mEnableCallback = true;
		pThis->Enabled = true;
		break;
	case MM_WOM_CLOSE:
		pThis->DisposeHeader();
		pThis->mHandle = nullptr;
		pThis->mEnableCallback = false;
		pThis->Enabled = false;
		break;
	case MM_WOM_DONE:
		if (pThis->mStop) {
			pThis->mEnableCallback = false;
			break;
		}
		EnterCriticalSection((LPCRITICAL_SECTION)&pThis->mBufferLock);
		pHdr->dwFlags &= ~WHDR_INQUEUE;
		if (pThis->mProcessedBufferCount > 0) {
			--pThis->mProcessedBufferCount;
		}
		waveOutWrite(hwo, pHdr, sizeof(WAVEHDR));
		LeaveCriticalSection((LPCRITICAL_SECTION)&pThis->mBufferLock);
		break;
	default:
		break;
	}
}

DWORD
WaveOut::BufferTask(LPVOID param) {
	auto pHdr = (WAVEHDR*)param;
	auto pThis = (WaveOut*)pHdr->dwUser;
	auto handle = *(LPHWAVEOUT)pThis->mHandle;
	pThis->mStop = false;
	pThis->mThreadStopped = false;
	pThis->mPause = false;
	pThis->mTerminate = false;
	pThis->mBufferPaused = false;
	pThis->mProcessedBufferCount = 0;
	auto ret = waveOutOpen(
		(LPHWAVEOUT)pThis->mHandle,
		(UINT)pThis->DeviceId,
		&pThis->WaveFormatEx,
		(DWORD_PTR)Callback,
		NULL,
		0x00030000
	);
	if (MMSYSERR_NOERROR != ret) {
		return -1;
	}
	for (int i = 0; i < pThis->mBufferCount; ++i) {
		waveOutPrepareHeader(handle, &pThis->mpWaveHdr[i], sizeof(WAVEHDR));
	}
	for (int i = 0; i < pThis->mBufferCount; ++i) {
		waveOutWrite(handle, &pThis->mpWaveHdr[i], sizeof(WAVEHDR));
	}
	int32_t writeIndex = 0;
	while (!pThis->mStop) {
		bool enableSleep;
		EnterCriticalSection((LPCRITICAL_SECTION)&pThis->mBufferLock);
		if (pThis->mProcessedBufferCount < pThis->mBufferCount) {
			auto lpWaveHdr = &pThis->mpWaveHdr[writeIndex];
			writeIndex = ++writeIndex % pThis->mBufferCount;
			if (lpWaveHdr->dwFlags & WHDR_INQUEUE) {
				LeaveCriticalSection((LPCRITICAL_SECTION)&pThis->mBufferLock);
				continue;
			}
			/*** Write Buffer ***/
			pThis->mProcessedBufferCount++;
			lpWaveHdr->dwFlags |= WHDR_INQUEUE;
			enableSleep = false;
			if (pThis->mPause || pThis->mTerminate) {
				memset(pHdr->lpData, 0, pThis->mBufferSize);
				pThis->mBufferPaused = true;
				if (pThis->mTerminate) {
					pThis->mPause = true;
					pThis->mTerminate = false;
					std::thread th(pThis->mfpOnTerminated);
				}
			}
			else {
				pThis->WriteBuffer(pHdr->lpData);
			}
		}
		else {
			/*** Buffer full ***/
			enableSleep = true;
		}
		LeaveCriticalSection((LPCRITICAL_SECTION)&pThis->mBufferLock);
		if (enableSleep) {
			Sleep(1);
		}
	}
	for (int i = 0; i < 40 && pThis->mEnableCallback; ++i) {
		Sleep(50);
	}
	waveOutReset(handle);
	for (int i = 0; i < pThis->mBufferCount; ++i) {
		waveOutUnprepareHeader(handle, &pThis->mpWaveHdr[i], sizeof(WAVEHDR));
	}
	waveOutClose(handle);
	for (int i = 0; i < 40 && pThis->Enabled; ++i) {
		Sleep(50);
	}
	pThis->mThreadStopped = true;
	return 0;
}
