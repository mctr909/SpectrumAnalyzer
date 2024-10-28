#include <iostream>
#include "WaveOut.h"

WaveOut::WaveOut(
	int32_t sampleRate,
	int32_t channels,
	EBufferType bufferType,
	int32_t bufferSamples,
	int32_t bufferCount,
	void (*fpWriteBuffer)(LPSTR lpData),
	void (*fpOnEndOfFile)(void)
) : Wave(sampleRate, channels, bufferType, bufferSamples, bufferCount) {
	this->fpWriteBuffer = fpWriteBuffer;
	this->fpOnEndOfFile = fpOnEndOfFile;
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
		std::cout << "[WaveOut] Device Enabled\r\n";
	} else {
		std::cout << "[WaveOut] Device Open error:" << res << "\r\n";
		return false;
	}
	for (int i = 0; i < BufferCount; i++) {
		auto pHeader = WaveHeaders + i;
		pHeader->dwUser = WHDR_INQUEUE;
		waveOutPrepareHeader((HWAVEOUT)DeviceHandle, pHeader, sizeof(WAVEHDR));
		waveOutWrite((HWAVEOUT)DeviceHandle, pHeader, sizeof(WAVEHDR));
	}
	std::cout << "[WaveOut] Header Prepared\r\n";
	WaitEnable(&CallbackEnabled);
	if (CallbackEnabled) {
		std::cout << "[WaveOut] Callback Enabled\r\n";
	} else {
		std::cout << "[WaveOut] Callback error\r\n";
		return false;
	}
	return true;
}

void WaveOut::FinalizeTask() {
	waveOutReset((HWAVEOUT)DeviceHandle);
	std::cout << "[WaveOut] Reset Device\r\n";
	for (int i = 0; i < BufferCount; i++) {
		auto pHeader = WaveHeaders + i;
		waveOutUnprepareHeader((HWAVEOUT)DeviceHandle, pHeader, sizeof(WAVEHDR));
	}
	std::cout << "[WaveOut] Header Unprepared\r\n";
	waveOutClose((HWAVEOUT)DeviceHandle);
	WaitDisable(&DeviceEnabled);
	std::cout << "[WaveOut] Device Closed\r\n";
}

void WaveOut::BufferTask() {
	int32_t writeIndex = 0;
	while (!Closing) {
		bool enableWait;
		auto pHeader = WaveHeaders + writeIndex;
		EnterCriticalSection(&LockBuffer);
		if (pHeader->dwUser & WHDR_INQUEUE) {
			enableWait = true;
		} else {
			enableWait = false;
			if (Pause || EndOfFile) {
				memcpy_s(pHeader->lpData, BufferSize, MuteData, BufferSize);
				Paused = true;
			} else {
				fpWriteBuffer(pHeader->lpData);
			}
			writeIndex = ++writeIndex % BufferCount;
			pHeader->dwUser |= WHDR_INQUEUE;
		}
		LeaveCriticalSection(&LockBuffer);
		if (enableWait) {
			Sleep(10);
		}
		if (Paused && EndOfFile) {
			EndOfFile = false;
			Pause = true;
			if (nullptr != fpOnEndOfFile) {
				fpOnEndOfFile();
			}
		}
	}
}

void WaveOut::Callback(HWAVEOUT hwo, WORD uMsg, WaveOut *self, LPWAVEHDR lpWaveHdr, DWORD dwParam2) {
	switch (uMsg) {
	case MM_WOM_OPEN:
		self->DeviceEnabled = true;
		break;
	case MM_WOM_CLOSE:
		self->DeviceEnabled = false;
		break;
	case MM_WOM_DONE:
		self->CallbackEnabled = true;
		if (self->Closing) {
			break;
		}
		EnterCriticalSection(&self->LockBuffer);
		waveOutWrite(hwo, lpWaveHdr, sizeof(WAVEHDR));
		lpWaveHdr->dwUser &= ~(DWORD)WHDR_INQUEUE;
		LeaveCriticalSection(&self->LockBuffer);
		break;
	}
}
