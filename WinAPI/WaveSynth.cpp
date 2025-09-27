#include <stdlib.h>
#include <math.h>
#include "Spectrum.h"
#include "WaveSynth.h"

#define LIMIT(v, min, max) ((v) < (min) ? (min) : (v) > (max) ? (max) : (v))

constexpr auto TERGET_THRESHOLD = 1e-3;
constexpr auto DECLICK_SPEED = 0.03;
constexpr auto SIN_TABLE_LENGTH = 48;
const double SIN_TABLE[] = {
	 0.0000, 0.1305, 0.2588, 0.3827, 0.5000, 0.6088, 0.7071, 0.7934, 0.8660, 0.9239, 0.9659, 0.9914,
	 1.0000, 0.9914, 0.9659, 0.9239, 0.8660, 0.7934, 0.7071, 0.6088, 0.5000, 0.3827, 0.2588, 0.1305,
	 0.0000,-0.1305,-0.2588,-0.3827,-0.5000,-0.6088,-0.7071,-0.7934,-0.8660,-0.9239,-0.9659,-0.9914,
	-1.0000,-0.9914,-0.9659,-0.9239,-0.8660,-0.7934,-0.7071,-0.6088,-0.5000,-0.3827,-0.2588,-0.1305,
	 0.0000
};

WaveSynth::WaveSynth(Spectrum *spectrum) {
	clSpectrum = spectrum;
	BankCount = spectrum->BankCount / HALFTONE_DIV;
	Banks = (OSC_BANK *)calloc(BankCount, sizeof(OSC_BANK));
	for (int32_t ixT = 0; ixT < BankCount; ixT++) {
		Banks[ixT].phase = (double)rand() / RAND_MAX;
	}
}

WaveSynth::~WaveSynth() {
	free(Banks);
}

void WaveSynth::WriteBuffer(float *output, int32_t sampleCount) {
	/* �p�����[�^��ݒ� */
	SetParameter();
	/* �g�`���������s */
	DoWaveSynth(output, sampleCount);
}

void WaveSynth::SetParameter() {
	auto threshold = TERGET_THRESHOLD * clSpectrum->Max;
	for (int32_t ixT = 0, ixS = 0; ixT < BankCount; ++ixT, ixS += HALFTONE_DIV) {
		/* 1�������̃X�y�N�g������ő�U���̂��̂��擾���� */
		auto p_osc = Banks + ixT;
		p_osc->delta = sqrt(clSpectrum->PeakBanks[ixS].DELTA * clSpectrum->PeakBanks[ixS + HALFTONE_DIV - 1].DELTA);
		p_osc->amp_l = threshold;
		p_osc->amp_r = threshold;
		auto amp_c = threshold;
		for (int32_t i = HALFTONE_DIV, div = ixS; i != 0; --i, ++div) {
			auto spec = clSpectrum->PeakBanks[div];
			if (spec.L > p_osc->amp_l) {
				p_osc->amp_l = spec.L;
			}
			if (spec.R > p_osc->amp_r) {
				p_osc->amp_r = spec.R;
			}
			auto spec_c = fmax(spec.L, spec.R);
			if (spec_c > amp_c) {
				amp_c = spec_c;
				p_osc->delta = spec.DELTA;
			}
		}
		p_osc->delta *= clSpectrum->Pitch;
		/* 臒l�ȉ��̐U����0�N���A */
		if (p_osc->amp_l <= threshold) {
			p_osc->amp_l = 0;
		}
		if (p_osc->amp_r <= threshold) {
			p_osc->amp_r = 0;
		}
		if (p_osc->declicked_l <= threshold) {
			p_osc->declicked_l = 0;
		}
		if (p_osc->declicked_r <= threshold) {
			p_osc->declicked_r = 0;
		}
		/* �U����臒l�ȉ��̏ꍇ
		 * �ŋߒቹ���܂��͍ŋߍ������̐U�����傫�����̈ʑ����擾���Đݒ肷�� */
		if (p_osc->declicked_l == 0 && p_osc->declicked_r == 0) {
			/* �ŋߒቹ�� */
			auto low_end = fmax(ixT - 7, 0);
			auto low_amp = threshold;
			for (int32_t t = ixT - 1; t >= low_end; --t) {
				auto low_osc = Banks[t];
				auto amp = fmax(low_osc.declicked_l, low_osc.declicked_r);
				if (amp > low_amp) {
					low_amp = amp;
					p_osc->phase = low_osc.phase;
					break;
				}
			}
			/* �ŋߍ����� */
			auto high_end = fmin(ixT + 7, BankCount - 1);
			for (int32_t t = ixT + 1; t <= high_end; ++t) {
				auto high_osc = Banks[t];
				auto amp = fmax(high_osc.declicked_l, high_osc.declicked_r);
				if (amp > low_amp) {
					p_osc->phase = high_osc.phase;
					break;
				}
			}
		}
	}
}

void WaveSynth::DoWaveSynth(float *output, int32_t sampleCount) {
	for (int32_t ixO = 0; ixO < BankCount; ++ixO) {
		auto p_osc = Banks + ixO;
		/* �U����0�Ȃ�g�`�������s��Ȃ� */
		if (p_osc->amp_l == 0 && p_osc->declicked_l == 0 &&
			p_osc->amp_r == 0 && p_osc->declicked_r == 0) {
			// �ʑ���i�߂�
			p_osc->phase += p_osc->delta * sampleCount;
			p_osc->phase -= (int32_t)p_osc->phase;
			continue;
		}
		/* �g�`���� */
		auto phase = p_osc->phase;
		auto declicked_l = p_osc->declicked_l;
		auto declicked_r = p_osc->declicked_r;
		auto delta = p_osc->delta;
		auto amp_l = p_osc->amp_l;
		auto amp_r = p_osc->amp_r;
		auto wave = output;
		for (int32_t ixS = sampleCount; ixS != 0; --ixS) {
			// �����g�e�[�u���̒l����`���
			auto ixD = phase * SIN_TABLE_LENGTH;
			auto ixI = (int32_t)ixD;
			auto a2b = ixD - ixI;
			auto sin = 1.0 - a2b;
			sin *= SIN_TABLE[ixI];
			sin += SIN_TABLE[ixI + 1] * a2b;
			// �ʑ���i�߂�
			phase += delta;
			phase -= (int32_t)phase;
			// �U�����X�V
			declicked_l += (amp_l - declicked_l) * DECLICK_SPEED;
			declicked_r += (amp_r - declicked_r) * DECLICK_SPEED;
			// �g�`����
			*wave++ += (float)(sin * declicked_l);
			*wave++ += (float)(sin * declicked_r);
		}
		p_osc->phase = phase;
		p_osc->declicked_l = declicked_l;
		p_osc->declicked_r = declicked_r;
	}
}
