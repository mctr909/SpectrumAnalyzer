#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>

#include <Windows.h>

#include "wave.h"
#include "wave_out.h"
#include "riff_wav.h"
#include "playback.h"

constexpr int32_t DIV = 20;

Playback::Playback(
	int sampleRate,
	void(*fpOnTerminated)(void)
) : WaveOut(
	BUFFER_TYPE::F32,
	sampleRate, 2,
	sampleRate / 1000 * DIV,
	10
) {
	DIV_SAMPLES = BufferSamples / DIV;
	DIV_SIZE = WaveFormatEx.nBlockAlign * DIV_SAMPLES;
	mfpOnTerminated = fpOnTerminated;
}

void
Playback::Open() {
	OpenDevice();
}

void
Playback::Close() {
	CloseDevice();
}

void
Playback::OpenFile(LPCWCHAR filePath) {
	Pause();
	if (File == nullptr) {
		delete File;
	}
	File = new WavReader(filePath, SampleRate, BufferSamples, 1.0);
}

void
Playback::WriteBuffer(LPSTR lpData) {
	if (File == nullptr) {
		return;
	}
	File->fpRead(File, (float*)lpData);
	auto pDivBuffer = lpData;
	for (int d = 0; d < DIV; ++d) {
		//Spectrum.Calc((float*)pDivBuffer, DIV_SAMPLES);
		//memset(pDivBuffer, 0, DIV_SIZE);
		//mOsc.WriteBuffer((float*)pDivBuffer, DIV_SAMPLES);
		//pDivBuffer += DIV_SIZE;
	}
	if (File->Position >= File->Length) {
		mTerminate = true;
	}
}
