#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>
#include <random>

#include "spectrum.h"
#include "wave_synth.h"

std::mt19937_64 mt64(0);
std::uniform_real_distribution<double> getRand(0, 1);

constexpr double DECLICK_SPEED = 0.1;
constexpr double THRESHOLD = 0.001; /* -60db */
constexpr int32_t TABLE_LENGTH = 192;

static double TABLE[TABLE_LENGTH + 1];

WaveSynth::WaveSynth(Spectrum* lcSpectrum) {
	cSpectrum = lcSpectrum;
	ToneCount = cSpectrum->TONE_COUNT;
	ppTones = new Tone[ToneCount];
	for (int32_t idxT = 0; idxT < ToneCount; idxT++) {
		ppTones[idxT].Phase = getRand(mt64);
	}
	for (int32_t i = 0; i < TABLE_LENGTH + 1; i++) {
		TABLE[i] = sin(6.283 * i / TABLE_LENGTH);
	}
}
WaveSynth::~WaveSynth() {
	delete ppTones;
}
void WaveSynth::WriteBuffer(float* lpOutput, int32_t sampleCount) {
	auto lowToneIndex = 0;
	auto lowTonePhase = 0.0;
	auto lowToneAmp = 0.0;
	auto ppBank = cSpectrum->ppBank;
	for (int32_t idxT = 0, idxB = 0; idxT < ToneCount; idxT++, idxB += Spectrum::TONE_DIV) {
		auto specAmpL = 0.0;
		auto specAmpR = 0.0;
		auto specAmpC = 0.0;
		auto delta = ppBank[idxB + Spectrum::TONE_DIV_CENTER].DELTA;
		for (int32_t div = 0, divB = idxB; div < Spectrum::TONE_DIV; div++, divB++) {
			auto bank = ppBank[divB];
			auto peakC = fmax(bank.LPeak, bank.RPeak);
			if (specAmpL < bank.LPeak) {
				specAmpL = bank.LPeak;
			}
			if (specAmpR < bank.RPeak) {
				specAmpR = bank.RPeak;
			}
			if (specAmpC < peakC) {
				specAmpC = peakC;
				delta = bank.DELTA;
			}
		}
		auto pTone = ppTones + idxT;
		if (pTone->AmpL >= THRESHOLD || pTone->AmpR >= THRESHOLD) {
			lowToneIndex = idxT;
			lowTonePhase = pTone->Phase;
			lowToneAmp = fmax(pTone->AmpL, pTone->AmpR);
		}
		else {
			if (specAmpL >= THRESHOLD || specAmpR >= THRESHOLD) {
				auto highToneEnd = fmin(idxT + 12, ToneCount);
				auto highTonePhase = 0.0;
				auto highToneAmp = 0.0;
				for (int32_t h = idxT + 1; h < highToneEnd; h++) {
					auto hiTone = ppTones[h];
					highToneAmp = fmax(hiTone.AmpL, hiTone.AmpR);
					if (highToneAmp >= THRESHOLD) {
						highTonePhase = hiTone.Phase;
						break;
					}
				}
				if (12 < idxT - lowToneIndex) {
					lowToneAmp = 0.0;
				}
				if (lowToneAmp < highToneAmp) {
					pTone->Phase = highTonePhase;
				}
				else {
					pTone->Phase = lowTonePhase;
				}
			}
		}
		delta *= cSpectrum->Pitch;
		auto pWave = lpOutput;
		for (int32_t s = 0; s < sampleCount; ++s) {
			auto indexF = pTone->Phase * TABLE_LENGTH;
			auto indexI = (int32_t)indexF;
			auto a2b = indexF - indexI;
			pTone->Phase += delta;
			pTone->Phase -= (int32_t)pTone->Phase;
			pTone->AmpL += (specAmpL - pTone->AmpL) * DECLICK_SPEED;
			pTone->AmpR += (specAmpR - pTone->AmpR) * DECLICK_SPEED;
			auto wave = TABLE[indexI] * (1.0 - a2b) + TABLE[indexI + 1] * a2b;
			*pWave++ += (float)(wave * pTone->AmpL);
			*pWave++ += (float)(wave * pTone->AmpR);
		}
	}
}