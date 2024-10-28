#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>

#include <Windows.h>

#include "winmm_wave.h"
#include "riff_wav.h"
#include "spectrum.h"
#include "playback.h"
#include "main.h"

void WINAPI
OutputOpen(
	Playback** hInstance,
	int32_t sampleRate,
	void(*fpOnOpened)(bool),
	void(*fpOnTerminate)(void)
) {
	if (*hInstance == nullptr) {
		*hInstance = new Playback(sampleRate, fpOnOpened, fpOnTerminate);
	}
	(*hInstance)->Open();
}
void WINAPI
OutputClose(Playback** hInstance) {
	if (*hInstance == nullptr) {
		return;
	}
	(*hInstance)->Close();
	delete* hInstance;
	hInstance = nullptr;
}
void WINAPI
OutputStart(Playback* hInstance) {
	if (hInstance == nullptr) {
		return;
	}
	hInstance->Start();
}
void WINAPI
OutputPause(Playback* hInstance) {
	if (hInstance == nullptr) {
		return;
	}
	hInstance->Pause();
}
void WINAPI
SetPlaybackFile(Playback* hInstance, LPCWCHAR filePath) {
	if (hInstance == nullptr) {
		return;
	}
	hInstance->OpenFile((wchar_t*)filePath);
}
void WINAPI
SetSpeed(Playback* hInstance, double speed) {

}
double WINAPI
GetSpeed(Playback* hInstance) {
	if (hInstance == nullptr) {
		return 1.0;
	}
	return hInstance->cFile->Speed;
}
void WINAPI
SetTranspose(Playback* hInstance, int32_t trancepose) {

}
int32_t WINAPI
GetTranspose(Playback* hInstance) {
	if (hInstance == nullptr) {
		return 1.0;
	}
	return (int32_t)hInstance->cSpectrum->Transpose;
}
