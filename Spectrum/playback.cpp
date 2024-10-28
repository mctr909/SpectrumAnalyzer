#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>

#include "winmm_wave.h"
#include "riff_wav.h"
#include "spectrum.h"
#include "wave_synth.h"
#include "playback.h"

constexpr int32_t DIV = 25;
constexpr int32_t NOTE_COUNT = 126;
constexpr double PITCH = 440;

Playback::Playback(
	int32_t sampleRate,
	void(*fpOnOpened)(bool),
	void(*fpOnTerminated)(void)
) : WaveOut(
	BUFFER_TYPE::F32,
	sampleRate, 2,
	sampleRate / 1000 * DIV,
	10
) {
	DIV_SAMPLES = BufferSamples / DIV;
	DIV_SIZE = WaveFormatEx.nBlockAlign * DIV_SAMPLES;
	auto baseFreq = PITCH * pow(2.0, 3 / 12.0 - 5);
	cSpectrum = new Spectrum(sampleRate, baseFreq, NOTE_COUNT, true);
	cWaveSynth = new WaveSynth(cSpectrum);
	this->fpOnOpened = fpOnOpened;
	this->fpOnTerminated = fpOnTerminated;
}
void Playback::Open() {
	OpenDevice();
}
void Playback::Close() {
	CloseDevice();
}
void Playback::OpenFile(wchar_t* filePath) {
	Pause();
	if (cFile != nullptr) {
		delete cFile;
	}
	cFile = new WavReader(filePath, WaveFormatEx.nSamplesPerSec, BufferSamples, 1.0);
	fpOnOpened(cFile->IsOpened);
}
void Playback::WriteBuffer(void* lpData) {
	if (cFile == nullptr) {
		return;
	}
	cFile->fpRead(cFile, (float*)lpData);
	auto pDivBuffer = (uint8_t*)lpData;
	for (int32_t d = 0; d < DIV; ++d) {
		cSpectrum->Calc((float*)pDivBuffer, DIV_SAMPLES);
		memset(pDivBuffer, 0, DIV_SIZE);
		cWaveSynth->WriteBuffer((float*)pDivBuffer, DIV_SAMPLES);
		pDivBuffer += DIV_SIZE;
	}
	if (cFile->Position >= cFile->SampleCount) {
		Terminating = true;
	}
}
