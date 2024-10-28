#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <vector>
#include <thread>

#include "winmm_wave.h"

#include <mmsystem.h>
#pragma comment (lib, "winmm.lib")

Wave::Wave(BUFFER_TYPE type,
	int32_t sampleRate,
	int32_t channels,
	int32_t bufferSamples,
	int32_t bufferCount
) {
	auto bits = (uint16_t)type & (uint16_t)BUFFER_TYPE::BIT_MASK;
	auto bytesPerSample = channels * bits >> 3;
	BufferSamples = bufferSamples;
	BufferSize = bufferSamples * bytesPerSample;
	BufferCount = bufferCount;
	WaveFormatEx.wFormatTag = ((uint16_t)type & (uint16_t)BUFFER_TYPE::FLOAT) > 0 ? 3 : 1;
	WaveFormatEx.nChannels = (uint16_t)channels,
	WaveFormatEx.nSamplesPerSec = (uint32_t)sampleRate,
	WaveFormatEx.nAvgBytesPerSec = (uint32_t)(sampleRate * bytesPerSample),
	WaveFormatEx.nBlockAlign = (uint16_t)bytesPerSample,
	WaveFormatEx.wBitsPerSample = bits,
	WaveFormatEx.cbSize = 0;
}
Wave::~Wave() {
	CloseDevice();
}
void Wave::Allocate() {
	ppWaveHdr = (LPWAVEHDR)calloc(BufferCount, sizeof(WAVEHDR));
	for (int32_t i = 0; i < BufferCount; ++i) {
		auto pWaveHdr = ppWaveHdr + i;
		pWaveHdr->dwFlags = 0;
		pWaveHdr->dwLoops = 0;
		pWaveHdr->dwBufferLength = (uint32_t)BufferSize;
		pWaveHdr->lpData = (LPSTR)calloc(BufferSize, 1);
		pWaveHdr->dwUser = (DWORD_PTR)this;
	}
}
void Wave::Dispose() {
	if (ppWaveHdr == nullptr) {
		return;
	}
	for (int32_t i = 0; i < BufferCount; ++i) {
		auto pWaveHdr = ppWaveHdr + i;
		if (pWaveHdr != nullptr && pWaveHdr->lpData != nullptr) {
			free(pWaveHdr->lpData);
		}
	}
	free(ppWaveHdr);
	ppWaveHdr = nullptr;
	hHandle = nullptr;
}
void Wave::CloseDevice() {
	if (hHandle == nullptr) {
		return;
	}
	Closing = true;
	WaitForSingleObject(hThread, 5000);
	CloseHandle(hThread);
	hThread = nullptr;
}
void Wave::SetDevice(uint32_t deviceId) {
	auto enable = EnableDevice;
	CloseDevice();
	DeviceId = deviceId;
	if (enable) {
		OpenDevice();
	}
}
void Wave::Pause() {
	Pausing = true;
	if (Playing) {
		for (int32_t i = 0; i < 40 && !Paused; i++) {
			Sleep(50);
		}
	}
	Playing = false;
}
void Wave::Start() {
	Pausing = false;
	Paused = false;
	Playing = EnableDevice;
}

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
	fpBufferTask = BufferTask;
}
void WaveOut::OpenDevice() {
	CloseDevice();
	hThread = CreateThread(
		nullptr,
		0,
		(LPTHREAD_START_ROUTINE)fpBufferTask,
		this,
		CREATE_SUSPENDED,
		&ThreadId
	);
	if (hThread == nullptr) {
		return;
	}
	SetThreadPriority(hThread, THREAD_PRIORITY_HIGHEST);
	ResumeThread(hThread);
}
void WaveOut::Callback(HWAVEOUT hwo, WORD uMsg, DWORD_PTR dwUser, DWORD_PTR dwParam1, DWORD_PTR dwParam2) {
	auto pThis = (WaveOut*)dwUser;
	switch (uMsg) {
	case MM_WOM_OPEN:
		pThis->EnableCallback = true;
		pThis->EnableDevice = true;
		break;
	case MM_WOM_CLOSE:
		pThis->Dispose();
		pThis->EnableCallback = false;
		pThis->EnableDevice = false;
		break;
	case MM_WOM_DONE: {
		if (pThis->Closing) {
			pThis->EnableCallback = false;
			break;
		}
		EnterCriticalSection((LPCRITICAL_SECTION)&pThis->BufferLock);
		waveOutWrite(hwo, (LPWAVEHDR)dwParam1, sizeof(WAVEHDR));
		if (pThis->ProcessedBufferCount > 0) {
			--pThis->ProcessedBufferCount;
		}
		LeaveCriticalSection((LPCRITICAL_SECTION)&pThis->BufferLock);
		break;
	}
	default:
		break;
	}
}
DWORD WaveOut::BufferTask(LPVOID lpInstance) {
	auto pThis = (WaveOut*)lpInstance;
	pThis->Allocate();
	auto ret = waveOutOpen(
		(LPHWAVEOUT)&pThis->hHandle,
		(UINT)pThis->DeviceId,
		&pThis->WaveFormatEx,
		(DWORD_PTR)Callback,
		(DWORD_PTR)pThis,
		CALLBACK_FUNCTION
	);
	if (MMSYSERR_NOERROR != ret) {
		pThis->Dispose();
		return -1;
	}
	InitializeCriticalSection(&pThis->BufferLock);
	pThis->Closing = false;
	pThis->Pausing = false;
	pThis->Paused = false;
	pThis->Terminating = false;
	pThis->ProcessedBufferCount = 0;
	auto handle = (HWAVEOUT)pThis->hHandle;
	for (int32_t i = 0; i < pThis->BufferCount; ++i) {
		waveOutPrepareHeader(handle, pThis->ppWaveHdr + i, sizeof(WAVEHDR));
		waveOutWrite(handle, pThis->ppWaveHdr + i, sizeof(WAVEHDR));
	}
	int32_t writeIndex = 0;
	while (!pThis->Closing) {
		bool enableSleep;
		EnterCriticalSection((LPCRITICAL_SECTION)&pThis->BufferLock);
		if (pThis->ProcessedBufferCount < pThis->BufferCount) {
			auto pWaveHdr = pThis->ppWaveHdr + writeIndex;
			writeIndex = ++writeIndex % pThis->BufferCount;
			if (!(pWaveHdr->dwFlags & WHDR_DONE)) {
				LeaveCriticalSection((LPCRITICAL_SECTION)&pThis->BufferLock);
				continue;
			}
			enableSleep = false;
			pThis->ProcessedBufferCount++;
			if (pThis->Pausing || pThis->Terminating) {
				/*** Pause ***/
				memset(pWaveHdr->lpData, 0, pThis->BufferSize);
				pThis->Paused = true;
				if (pThis->Terminating) {
					pThis->Pausing = true;
					pThis->Terminating = false;
					pThis->fpOnTerminated();
				}
			}
			else {
				/*** Write Buffer ***/
				pThis->WriteBuffer(pWaveHdr->lpData);
			}
		}
		else {
			/*** Buffer full ***/
			enableSleep = true;
		}
		LeaveCriticalSection((LPCRITICAL_SECTION)&pThis->BufferLock);
		if (enableSleep) {
			Sleep(1);
		}
	}
	for (int32_t i = 0; i < 40 && pThis->EnableCallback; ++i) {
		Sleep(50);
	}
	waveOutReset(handle);
	for (int32_t i = 0; i < pThis->BufferCount; ++i) {
		waveOutUnprepareHeader(handle, pThis->ppWaveHdr + i, sizeof(WAVEHDR));
	}
	waveOutClose(handle);
	for (int32_t i = 0; i < 40 && pThis->EnableDevice; ++i) {
		Sleep(50);
	}
	return 0;
}
