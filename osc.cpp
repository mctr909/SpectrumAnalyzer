#include <math.h>
#include <string.h>
#include "spectrum.h"
#include "osc.h"

#define AMP_MIN       0.001
#define DECLICK_SPEED 0.04
#define TABLE_LENGTH  96

double TABLE[TABLE_LENGTH] = { 0 };

void osc_init(OSC *p_instance, int tone_count) {
	osc_free(p_instance);
	p_instance->pitch = 1.0;
	p_instance->tone_count = tone_count;
	p_instance->tones = (TONE_BANK*)calloc(tone_count, sizeof(TONE_BANK));
	for (int i = 0; i < TABLE_LENGTH; ++i) {
		TABLE[i] = sin(8*atan(1)*i/TABLE_LENGTH);
	}
}

void osc_free(OSC *p_instance) {
	if (nullptr != p_instance->tones) {
		free(p_instance->tones);
		p_instance->tones = nullptr;
	}
}

void osc_exec(OSC *p_instance, SPECTRUM &spec, double *p_wave, int wave_samples) {
	memset(p_wave, 0, sizeof(double)*wave_samples*2);
	auto tone_low_idx = 0;
	auto tone_low_amp = AMP_MIN;
	auto tone_low_phase = 0.0;
	for (int idx_t = 0, idx_s = 0; idx_t < p_instance->tone_count; ++idx_t, idx_s += spec.tone_div) {
		auto peak_l = 0.0;
		auto peak_r = 0.0;
		auto peak_c = 0.0;
		auto delta = 0.0;
		for (int div_d = 0, div_s = idx_s; div_d < spec.tone_div; ++div_d, ++div_s) {
			auto spec_peak_l = spec.l_peaks[div_s];
			auto spec_peak_r = spec.r_peaks[div_s];
			auto spec_peak_c = fmax(spec_peak_l, spec_peak_r);
			if (peak_l < spec_peak_l) {
				peak_l = spec_peak_l;
			}
			if (peak_r < spec_peak_r) {
				peak_r = spec_peak_r;
			}
			if (peak_c < spec_peak_c) {
				peak_c = spec_peak_c;
				delta = spec.banks[div_s].delta;
			}
		}
		auto tone = &p_instance->tones[idx_t];
		if (tone->amp_l < AMP_MIN && tone->amp_r < AMP_MIN) {
			if (peak_l >= AMP_MIN || peak_r >= AMP_MIN) {
				auto tone_high_end = fmin(idx_t + 12, p_instance->tone_count);
				auto tone_high_amp = 0.0;
				auto tone_high_phase = 0.0;
				for (int idx_th = idx_t + 1; idx_th < tone_high_end; ++idx_th) {
					auto tone_high = p_instance->tones[idx_th];
					tone_high_amp = fmax(tone_high.amp_l, tone_high.amp_r);
					if (tone_low_amp < tone_high_amp) {
						tone_high_phase = tone_high.phase;
						break;
					}
				}
				if (12 < idx_t - tone_low_idx) {
					tone_low_amp = AMP_MIN;
				}
				if (tone_low_amp < tone_high_amp) {
					tone->phase = tone_high_phase;
				} else {
					tone->phase = tone_low_phase;
				}
			}
		} else {
			tone_low_idx = idx_t;
			tone_low_amp = fmax(tone->amp_l, tone->amp_r);
			tone_low_phase = tone->phase;
		}
		peak_l *= 2;
		peak_r *= 2;
		delta *= p_instance->pitch;
		for (int s = 0, i = 0; s < wave_samples; ++s, i += 2) {
			auto pos = tone->phase * TABLE_LENGTH;
			auto idx = (int)pos;
			auto a2b = pos - idx;
			tone->phase += delta;
			tone->phase -= (int)tone->phase;
			tone->amp_l += (peak_l - tone->amp_l) * DECLICK_SPEED;
			tone->amp_r += (peak_r - tone->amp_r) * DECLICK_SPEED;
			auto wave = TABLE[idx] * (1.0 - a2b) + TABLE[idx + 1] * a2b;
			p_wave[i    ] += wave * tone->amp_l;
			p_wave[i + 1] += wave * tone->amp_r;
		}
	}
}
