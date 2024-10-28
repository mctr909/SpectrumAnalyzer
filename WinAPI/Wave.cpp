#include "Wave.h"

#pragma comment(lib, "winmm.lib")

Wave::Wave(int32_t sampleRate, int32_t channels, EBufferType bufferType, int32_t bufferSamples, int32_t bufferCount) {
	auto isFloat = ((uint16_t)bufferType & (uint16_t)EBufferType::FLOAT) != 0;
	auto bits = (uint16_t)bufferType & (uint16_t)EBufferType::BITS_MASK;
	auto bytesPerSample = channels * bits >> 3;
	SampleRate = sampleRate;
	Channels = channels;
	DeviceId = WAVE_MAPPER;
	BufferType = bufferType;
	BufferSamples = bufferSamples;
	BufferSize = bufferSamples * bytesPerSample;
	BufferCount = bufferCount;
	WaveFormatEx = {};
	WaveFormatEx.wFormatTag = isFloat ? 3 : 1;
	WaveFormatEx.nChannels = (WORD)channels;
	WaveFormatEx.nSamplesPerSec = (DWORD)sampleRate;
	WaveFormatEx.wBitsPerSample = (WORD)bits;
	WaveFormatEx.nBlockAlign = (WORD)((bits >> 3) * (WORD)channels);
	WaveFormatEx.nAvgBytesPerSec = WaveFormatEx.nBlockAlign * (DWORD)sampleRate;
	WaveFormatEx.cbSize = 0;
}

Wave::~Wave() {
	CloseDevice();
}

void Wave::SetDevice(uint32_t deviceId, bool withStart) {
	auto enable = DeviceEnabled;
	CloseDevice();
	DeviceId = deviceId;
	if (enable || withStart) {
		OpenDevice();
	}
}

void Wave::Start() {
	Pause = false;
	Paused = false;
	Playing = DeviceEnabled;
}

void Wave::Stop() {
	Pause = true;
	if (Playing) {
		WaitEnable(&Paused);
		Playing = false;
	}
}

void Wave::OpenDevice() {
	CloseDevice();
	ThreadBuffer = CreateThread(
		nullptr,
		0,
		(LPTHREAD_START_ROUTINE)Task,
		this,
		0,
		(LPDWORD)&ThreadBuffer
	);
	if (nullptr == ThreadBuffer) {
		return;
	}
	SetThreadPriority(ThreadBuffer, THREAD_PRIORITY_HIGHEST);
}

void Wave::CloseDevice() {
	if (nullptr == DeviceHandle) {
		return;
	}
	Closing = true;
	WaitForSingleObject(ThreadBuffer, 2000);
}

void Wave::WaitEnable(bool *flag) {
	for (int i = 0; i < 200 && !*flag; i++) {
		Sleep(10);
	}
}

void Wave::WaitDisable(bool *flag) {
	for (int i = 0; i < 200 && *flag; i++) {
		Sleep(10);
	}
}

void Wave::AllocateHeader() {
	WaveHeaders = (WAVEHDR*)malloc(sizeof(WAVEHDR) * BufferCount);
	if (nullptr == WaveHeaders) {
		return;
	}
	memset(WaveHeaders, 0, sizeof(WAVEHDR) * BufferCount);
	MuteData = malloc(BufferSize);
	if (nullptr != MuteData) {
		if (BufferType == EBufferType::INT8) {
			memset(MuteData, 128, BufferSize);
		} else {
			memset(MuteData, 0, BufferSize);
		}
	}
	for (int i = 0; i < BufferCount; ++i) {
		WAVEHDR header{};
		header.lpData = (LPSTR)malloc(BufferSize);
		header.dwBufferLength = (uint32_t)BufferSize;
		header.dwFlags = WHDR_BEGINLOOP | WHDR_ENDLOOP;
		memcpy_s(header.lpData, BufferSize, MuteData, BufferSize);
		memcpy_s(WaveHeaders + i, sizeof(WAVEHDR), &header, sizeof(WAVEHDR));
	}
}

void Wave::DisposeHeader() {
	if (nullptr == WaveHeaders) {
		return;
	}
	for (int i = 0; i < BufferCount; ++i) {
		auto pHeader = WaveHeaders + i;
		if (nullptr == pHeader) {
			continue;
		}
		if (nullptr != pHeader->lpData) {
			free(pHeader->lpData);
		}
	}
	free(WaveHeaders);
}

void Wave::ClearFlags() {
	Closing = false;
	Paused = false;
	DeviceEnabled = false;
	CallbackEnabled = false;
}

DWORD Wave::Task(LPVOID *lpParam) {
	auto self = (Wave*)lpParam;
	InitializeCriticalSection(&self->LockBuffer);
	self->AllocateHeader();
	self->ClearFlags();
	if (self->InitializeTask()) {
		self->BufferTask();
		self->FinalizeTask();
	}
	self->DisposeHeader();
	self->ClearFlags();
	self->DeviceHandle = nullptr;
	if (nullptr != self->LockBuffer.DebugInfo) {
		DeleteCriticalSection(&self->LockBuffer);
		self->LockBuffer.DebugInfo = nullptr;
	}
	return 0;
}
