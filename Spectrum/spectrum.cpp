#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>

#include "spectrum.h"

const int32_t Spectrum::TONE_DIV = 1;
const int32_t Spectrum::TONE_DIV_CENTER = TONE_DIV / 2;
const int32_t Spectrum::OCT_DIV = TONE_DIV * 12;

constexpr int32_t LOW_FREQ = 55;
constexpr int32_t MID_FREQ = 180;
constexpr int32_t THRESHOLD_WIDE = Spectrum::TONE_DIV * 6;
constexpr int32_t THRESHOLD_NARROW = Spectrum::TONE_DIV;
constexpr double THRESHOLD_GAIN_LOW = 1.059;
constexpr double THRESHOLD_GAIN_MID = 1.122;
constexpr double RMS_MIN = 1e-6;
constexpr double NARROW_WIDTH = 1.0;
constexpr double NARROW_WIDTH_AT_FREQ = 660.0;

Spectrum::Spectrum(int32_t sampleRate, double baseFrequency, int32_t tones, bool stereo) {
	SAMPLE_RATE = sampleRate;
	BANK_COUNT = tones * TONE_DIV;
	LOW_TONE = (int32_t)(OCT_DIV * log2(LOW_FREQ / baseFrequency));
	MID_TONE = (int32_t)(OCT_DIV * log2(MID_FREQ / baseFrequency));
	pPeak = new double[BANK_COUNT];
	pCurve = new double[BANK_COUNT];
	pThreshold = new double[BANK_COUNT];
	MaxL = RMS_MIN;
	MaxR = RMS_MIN;
	ppBank = new BPFBank[BANK_COUNT];
	for (int32_t b = 0; b < BANK_COUNT; ++b) {
		auto frequency = baseFrequency * pow(2.0, (b - 0.5 * TONE_DIV) / OCT_DIV);
		SetBPF(&ppBank[b], frequency);
	}
	SetResponceSpeed(16);
	if (stereo) {
		fpCalc = CalcStereo;
	}
	else {
		fpCalc = CalcMono;
	}
}
void Spectrum::SetResponceSpeed(double frequency) {
	ResponceSpeed = frequency;
	for (int32_t b = 0; b < BANK_COUNT; ++b) {
		auto pBank = &ppBank[b];
		auto limitFreq = pBank->DELTA * SAMPLE_RATE / 2;
		if (frequency > limitFreq) {
			pBank->SIGMA_DISP = GetAlpha(SAMPLE_RATE / 4, limitFreq);
		}
		else {
			pBank->SIGMA_DISP = GetAlpha(SAMPLE_RATE / 4, frequency);
		}
	}
}
void Spectrum::SetBPF(BPFBank* lpBank, double frequency) {
	lpBank->DELTA = frequency / SAMPLE_RATE;
	lpBank->SIGMA = GetAlpha(SAMPLE_RATE / 2, frequency);
	auto alpha = GetAlpha(SAMPLE_RATE, frequency);
	auto a0 = 1.0 + alpha;
	lpBank->KB0 = alpha / a0;
	lpBank->KA1 = -2.0 * cos(6.283 * lpBank->DELTA) / a0;
	lpBank->KA2 = (1.0 - alpha) / a0;
}
double Spectrum::GetAlpha(int32_t sampleRate, double frequency) {
	auto halfToneWidth = NARROW_WIDTH + log2(NARROW_WIDTH_AT_FREQ / frequency);
	if (halfToneWidth < NARROW_WIDTH) {
		halfToneWidth = NARROW_WIDTH;
	}
	auto omega = 6.283 * frequency / sampleRate;
	auto s = sin(omega);
	auto x = log10(2) / 4 * halfToneWidth / 12.0 * omega / s;
	auto a = s * sinh(x);
	if (a > 1) {
		return 1;
	}
	return a;
}
void Spectrum::Calc(float* lpInput, int32_t sampleCount) {
	if (NormGain) {
		MaxL = RMS_MIN;
		MaxR = RMS_MIN;
	}
	if (AutoGain) {
		auto autoGainAttenuation = (double)sampleCount / SAMPLE_RATE;
		MaxL += (RMS_MIN - MaxL) * autoGainAttenuation;
		MaxR += (RMS_MIN - MaxR) * autoGainAttenuation;
	}
	fpCalc(this, lpInput, sampleCount);
	if (!(AutoGain || NormGain)) {
		MaxL = 1;
		MaxR = 1;
	}

	auto lastL = 0.0;
	auto lastR = 0.0;
	auto lastIndexL = -1;
	auto lastIndexR = -1;
	auto lastDisplay = 0.0;
	auto lastDisplayIndex = -1;
	for (int32_t idxB = 0; idxB < BANK_COUNT; ++idxB) {
		/* Calc threshold */
		auto thL = 0.0;
		auto thR = 0.0;
		auto thDisplayL = 0.0;
		auto thDisplayR = 0.0;
		{
			int32_t width;
			double gain;
			auto transposeB = idxB + Transpose * TONE_DIV;
			if (transposeB < LOW_TONE) {
				width = THRESHOLD_WIDE;
				gain = THRESHOLD_GAIN_LOW;
			}
			else if (transposeB < MID_TONE) {
				auto a2b = (double)(transposeB - LOW_TONE) / (MID_TONE - LOW_TONE);
				width = (int32_t)(THRESHOLD_NARROW * a2b + THRESHOLD_WIDE * (1 - a2b));
				gain = THRESHOLD_GAIN_MID * a2b + THRESHOLD_GAIN_LOW * (1 - a2b);
			}
			else {
				width = THRESHOLD_NARROW;
				gain = THRESHOLD_GAIN_MID;
			}
			for (int32_t w = -width; w <= width; ++w) {
				auto bw = (int32_t)fmin(BANK_COUNT - 1, fmax(0, idxB + w));
				auto b = ppBank[bw];
				thL += b.LPower;
				thR += b.RPower;
				thDisplayL += b.LPowerDisp;
				thDisplayR += b.RPowerDisp;
			}
			width = 1 + width << 1;
			thL /= width;
			thR /= width;
			thDisplayL /= width * MaxL;
			thDisplayR /= width * MaxR;
			thL = gain * sqrt(thL);
			thR = gain * sqrt(thR);
			thDisplayL = gain * sqrt(fmax(thDisplayL, thDisplayR));
		}
		/* Set peak */
		auto bank = ppBank[idxB];
		(ppBank + idxB)->LPeak = 0.0;
		(ppBank + idxB)->RPeak = 0.0;
		auto l = sqrt(bank.LPower);
		auto r = sqrt(bank.RPower);
		if (l < thL) {
			if (0 <= lastIndexL) {
				(ppBank + lastIndexL)->LPeak = lastL;
			}
			l = 0.0;
			lastL = 0.0;
			lastIndexL = -1;
		}
		if (lastL < l) {
			lastL = l;
			lastIndexL = idxB;
		}
		if (r < thR) {
			if (0 <= lastIndexR) {
				(ppBank + lastIndexR)->RPeak = lastR;
			}
			r = 0.0;
			lastR = 0.0;
			lastIndexR = -1;
		}
		if (lastR < r) {
			lastR = r;
			lastIndexR = idxB;
		}
		/* Set display value */
		auto display = sqrt(fmax(
			bank.LPowerDisp / MaxL,
			bank.RPowerDisp / MaxR
		));
		pPeak[idxB] = 0.0;
		pCurve[idxB] = display;
		pThreshold[idxB] = thDisplayL;
		if (display < thDisplayL) {
			if (0 <= lastDisplayIndex) {
				pPeak[lastDisplayIndex] = lastDisplay;
			}
			display = 0.0;
			lastDisplay = 0.0;
			lastDisplayIndex = -1;
		}
		if (lastDisplay < display) {
			lastDisplay = display;
			lastDisplayIndex = idxB;
		}
	}
	if (0 <= lastIndexL) {
		(ppBank + lastIndexL)->LPeak = lastL;
	}
	if (0 <= lastIndexR) {
		(ppBank + lastIndexR)->RPeak = lastR;
	}
}
void Spectrum::CalcMono(Spectrum* lpThis, float* lpInput, int32_t sampleCount) {
	for (int32_t b = 0; b < lpThis->BANK_COUNT; ++b) {
		auto pWave = lpInput;
		auto pBank = (lpThis->ppBank + b);
		for (int32_t s = 0; s < sampleCount; ++s) {
			auto lb0 = *pWave++;
			auto la0
				= pBank->KB0 * (lb0 - pBank->Lb2)
				- pBank->KA2 * pBank->La2
				- pBank->KA1 * pBank->La1
			;
			pBank->Lb2 = pBank->Lb1;
			pBank->Lb1 = lb0;
			pBank->La2 = pBank->La1;
			pBank->La1 = la0;
			la0 *= la0;
			pBank->LPower += (la0 - pBank->LPower) * pBank->SIGMA;
			pBank->LPowerDisp += (la0 - pBank->LPowerDisp) * pBank->SIGMA_DISP;
		}
		pBank->RPower = pBank->LPower;
		pBank->RPowerDisp = pBank->LPowerDisp;
		lpThis->MaxL = fmax(lpThis->MaxL, pBank->LPowerDisp);
		lpThis->MaxR = lpThis->MaxL;
	}
}
void Spectrum::CalcStereo(Spectrum* lpThis, float* lpInput, int32_t sampleCount) {
	for (int32_t b = 0; b < lpThis->BANK_COUNT; ++b) {
		auto pWave = lpInput;
		auto pBank = (lpThis->ppBank + b);
		for (int32_t s = 0; s < sampleCount; ++s) {
			auto lb0 = *pWave++;
			auto rb0 = *pWave++;
			auto la0
				= pBank->KB0 * (lb0 - pBank->Lb2)
				- pBank->KA2 * pBank->La2
				- pBank->KA1 * pBank->La1
			;
			auto ra0
				= pBank->KB0 * (rb0 - pBank->Rb2)
				- pBank->KA2 * pBank->Ra2
				- pBank->KA1 * pBank->Ra1
			;
			pBank->Lb2 = pBank->Lb1;
			pBank->Lb1 = lb0;
			pBank->La2 = pBank->La1;
			pBank->La1 = la0;
			pBank->Rb2 = pBank->Rb1;
			pBank->Rb1 = rb0;
			pBank->Ra2 = pBank->Ra1;
			pBank->Ra1 = ra0;
			la0 *= la0;
			ra0 *= ra0;
			pBank->LPower += (la0 - pBank->LPower) * pBank->SIGMA;
			pBank->RPower += (ra0 - pBank->RPower) * pBank->SIGMA;
			pBank->LPowerDisp += (la0 - pBank->LPowerDisp) * pBank->SIGMA_DISP;
			pBank->RPowerDisp += (ra0 - pBank->RPowerDisp) * pBank->SIGMA_DISP;
		}
		lpThis->MaxL = fmax(lpThis->MaxL, pBank->LPowerDisp);
		lpThis->MaxR = fmax(lpThis->MaxR, pBank->RPowerDisp);
	}
}
