#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>

#include <Windows.h>
#include <mmsystem.h>
#pragma comment (lib, "winmm.lib")

#include "main.h"
#include "wave.h"
#include "wave_out.h"
#include "playback.h"

Playback* pp = nullptr;

uint8_t* WINAPI
WaveOutOpen(
	int32_t sample_rate,
	int32_t buffer_length,
	int32_t buffer_count,
	void(*fpOnTerminate)(void)
) {
	if (pp == nullptr) {
		pp = new Playback(44100, fpOnTerminate);
	}
	pp->Open();
	return nullptr;
}

void WINAPI
OpenFile(LPCWCHAR filePath) {
	if (pp == nullptr) {
		return;
	}
	pp->OpenFile(filePath);
}