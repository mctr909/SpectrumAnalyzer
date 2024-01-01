#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>

#include <Windows.h>
#include <mmsystem.h>
#pragma comment (lib, "winmm.lib")

#include "wave.h"

Wave::Wave(BUFFER_TYPE type,
	int32_t sampleRate,
	int32_t channels,
	int32_t bufferSamples,
	int32_t bufferCount
) {
	auto bits = (uint16_t)type & (uint16_t)BUFFER_TYPE::BIT_MASK;
	auto bytesPerSample = channels * bits >> 3;
	SampleRate = sampleRate;
	Channels = channels;
	BufferSamples = bufferSamples;
	mBufferSize = bufferSamples * bytesPerSample;
	mBufferCount = bufferCount;

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

void
Wave::AllocHeader() {
	mpWaveHdr = (LPWAVEHDR)calloc(sizeof(WAVEHDR), mBufferCount);
	for (int i = 0; i < mBufferCount; ++i) {
		auto hdr = (LPWAVEHDR)&mpWaveHdr[i];
		hdr->dwFlags = WHDR_BEGINLOOP | WHDR_ENDLOOP;
		hdr->dwBufferLength = (uint32_t)mBufferSize;
		hdr->lpData = (LPSTR)calloc(mBufferSize, 1);
	}
}

void
Wave::DisposeHeader() {
	if (mpWaveHdr == nullptr) {
		return;
	}
	for (int i = 0; i < mBufferCount; ++i) {
		auto hdr = (LPWAVEHDR)&mpWaveHdr[i];
		if (hdr != nullptr && hdr->lpData != nullptr) {
			free(hdr->lpData);
		}
	}
	free(mpWaveHdr);
	mpWaveHdr = nullptr;
}

void
Wave::OpenDevice() {
	CloseDevice();
	mThread = CreateThread(
		nullptr,
		0,
		(LPTHREAD_START_ROUTINE)mfpBufferTask,
		mpWaveHdr,
		0,
		&mThreadId
	);
	if (mThread == nullptr) {
		return;
	}
	SetThreadPriority(mThread, THREAD_PRIORITY_HIGHEST);
}

void
Wave::CloseDevice() {
	if (mHandle == nullptr) {
		return;
	}
	mStop = true;
	for (int i = 0; i < 40 && !mThreadStopped; ++i) {
		Sleep(50);
	}
}

void
Wave::SetDevice(uint32_t deviceId) {
	auto enable = Enabled;
	CloseDevice();
	DeviceId = deviceId;
	if (enable) {
		OpenDevice();
	}
}

void
Wave::Pause() {
	mPause = true;
	if (Playing) {
		for (int i = 0; i < 40 && !mBufferPaused; i++) {
			Sleep(50);
		}
	}
	Playing = false;
}

void
Wave::Start() {
	mPause = false;
	mBufferPaused = false;
	Playing = Enabled;
}
