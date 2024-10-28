#include <windows.h>
#include <mmsystem.h>
#include <iostream>
#include "WaveOut.h"

constexpr auto PROCESS_TIMEOUT = 100;

WaveOut::WaveOut(int32_t sampleRate, int32_t channels, EBufferType bufferType, int32_t bufferSamples, int32_t bufferCount, void (*fpWriteBuffer)(LPSTR lpData))
    : Wave(sampleRate, channels, bufferType, bufferSamples, bufferCount) {
	this->fpWriteBuffer = fpWriteBuffer;
}

WaveOut::~WaveOut() {
	Wave::~Wave();
}

bool WaveOut::InitializeTask() {
	auto res = waveOutOpen(
		(LPHWAVEOUT)&DeviceHandle,
		DeviceId,
		&WaveFormatEx,
		(DWORD_PTR)Callback,
		(DWORD_PTR)this,
		CALLBACK_FUNCTION
	);
	WaitEnable(&DeviceEnabled);
	if (DeviceEnabled) {
		std::wcout << "[WaveOut] Device Enabled\r\n";
	} else {
		std::wcout << "[WaveOut] Device Open error:" << res << "\r\n";
		return false;
	}
	for (int i = 0; i < BufferCount; i++) {
		auto pHeader = WaveHeaders + i;
		waveOutPrepareHeader((HWAVEOUT)DeviceHandle, pHeader, sizeof(WAVEHDR));
		waveOutWrite((HWAVEOUT)DeviceHandle, pHeader, sizeof(WAVEHDR));
	}
	std::wcout << "[WaveOut] Header Prepared\r\n";
	WaitEnable(&CallbackEnabled);
	if (CallbackEnabled) {
		std::wcout << "[WaveOut] Callback Enabled\r\n";
	} else {
		std::wcout << "[WaveOut] Callback error\r\n";
		return false;
	}
	return true;
}

void WaveOut::FinalizeTask() {
	if (ProcessInterval < PROCESS_TIMEOUT) {
		waveOutReset((HWAVEOUT)DeviceHandle);
		std::wcout << "[WaveOut] Reset Device\r\n";
		for (int i = 0; i < BufferCount; i++) {
			auto pHeader = WaveHeaders + i;
			waveOutUnprepareHeader((HWAVEOUT)DeviceHandle, pHeader, sizeof(WAVEHDR));
		}
		std::wcout << "[WaveOut] Header Unprepared\r\n";
		waveOutClose((HWAVEOUT)DeviceHandle);
		WaitDisable(&DeviceEnabled);
		std::wcout << "[WaveOut] Device Closed\r\n";
	} else {
		std::wcout << "[WaveOut] Device Locked\r\n";
	}
}

void WaveOut::BufferTask() {
	while (!Closing) {
		for (int nonInqueues = BufferCount; nonInqueues != 0;) {
			EnterCriticalSection(&LockBuffer);
			auto pHeader = WaveHeaders + BufferIndex;
			BufferIndex = ++BufferIndex % BufferCount;
			if (pHeader->dwFlags & WHDR_INQUEUE) {
				nonInqueues--;
				LeaveCriticalSection(&LockBuffer);
				continue;
			}
			pHeader->dwFlags |= WHDR_INQUEUE;
			if (Pause || EndOfFile) {
				memcpy_s(pHeader->lpData, BufferSize, MuteData, BufferSize);
				Paused = true;
			} else {
				fpWriteBuffer(pHeader->lpData);
			}
			LeaveCriticalSection(&LockBuffer);
		}
		if (++ProcessInterval >= PROCESS_TIMEOUT) {
			Closing = true;
			break;
		}
		if (Paused && EndOfFile) {
			EndOfFile = false;
			Pause = true;
			//new Task(() = > { OnEndOfFile(); }).Start();
		}
		Sleep(1);
	}
}

void WaveOut::Callback(HWAVEOUT hwo, WORD uMsg, DWORD_PTR dwInstance, LPWAVEHDR lpWaveHdr, DWORD dwParam2) {
	auto self = (WaveOut*)dwInstance;
	switch (uMsg) {
	case MM_WOM_OPEN:
		self->DeviceEnabled = true;
		break;
	case MM_WOM_CLOSE:
		self->DeviceEnabled = false;
		break;
	case MM_WOM_DONE:
		self->CallbackEnabled = true;
		self->ProcessInterval = 0;
		if (self->Closing) {
			break;
		}
		EnterCriticalSection(&self->LockBuffer);
		waveOutWrite(hwo, lpWaveHdr, sizeof(WAVEHDR));
		lpWaveHdr->dwFlags &= ~(DWORD)WHDR_INQUEUE;
		LeaveCriticalSection(&self->LockBuffer);
		break;
	}
}
