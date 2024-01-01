#include <math.h>
#include <string.h>
#include "spectrum.h"

#define HALFTONE_WIDTH         1.75
#define HALFTONE_WIDTH_AT_FREQ 440.0
#define MID_FREQ               80.0
#define HIGH_FREQ              3000.0
#define THRESHOLD_WIDE         4.0
#define THRESHOLD_NARROW       0.5
#define LN2_4                  0.173 /* ln(2)/4 */

void spectrum_set_bank(SPEC_BANK *p_bank, int sample_rate, double freq) {
	auto halfToneWidth = HALFTONE_WIDTH + log(HALFTONE_WIDTH_AT_FREQ / freq, 2.0);
	if (halfToneWidth < HALFTONE_WIDTH) {
		halfToneWidth = HALFTONE_WIDTH;
	}
	auto omega = 8 * atan(1) * freq / sampleRate;
	auto s = sin(omega);
	auto x = LN2_4 * halfToneWidth / 12.0 * omega / s;
	auto alpha = s * sinh(x);
	auto a0 = 1.0 + alpha;
	memset(p_bank, 0, sizeof(SPEC_BANK));
	p_bank->kb0 = alpha / a0;
	p_bank->ka1 = -2.0 * cos(omega) / a0;
	p_bank->ka2 = (1.0 - alpha) / a0;
	p_bank->delta = freq / sampleRate;
}

void spectrum_init(SPECTRUM *p_instance, int tone_count, int tone_div, int sample_rate, double base_freq) {
	spectrum_free(p_instance);
	auto oct_div = tone_div * 12;
	auto bank_count = tone_div * tone_count;
	p_instance->bank_count = bank_count;
	p_instance->tone_div = tone_div;
	p_instance->mid_bank = (int)(oct_div * log(MID_FREQ / base_freq, 2));
	p_instance->high_bank = (int)(oct_div * log(HIGH_FREQ / base_freq, 2));
	p_instance->l_peaks = (double*)calloc(bank_count, sizeof(double));
	p_instance->r_peaks = (double*)calloc(bank_count, sizeof(double));
	p_instance->banks = (SPEC_BANK*)calloc(bank_count, sizeof(SPEC_BANK));
	for (int idx_b = 0; idx_b < bank_count; ++idx_b) {
		auto freq = base_freq * pow(2.0, (idx_b - 0.5 * tone_div) / oct_div);
		spectrum_set_bank(&p_instance->banks[idx_b], sample_rate, freq);
	}
}

void spectrum_free(SPECTRUM *p_instance) {
	if (nullptr != p_instance->banks) {
		free(p_instance->l_peaks);
		free(p_instance->r_peaks);
		free(p_instance->banks);
		p_instance->l_peaks = nullptr;
		p_instance->r_peaks = nullptr;
		p_instance->banks = nullptr;
	}
}

void spectrum_exec(SPECTRUM *p_instance, double *p_wave, int wave_samples) {
	/*** calc RMS ***/
	for (int idx_b = 0; idx_b < p_instance->bank_count; ++idx_b) {
		auto bank = &p_instance->banks[idx_b];
		for (int s = 0, i = 0; s < wave_samples; ++s, i += 2) {
			/* left */
			{
				auto input = p_wave[i];
				auto filtered
					= bank->kb0 * input
					- bank->kb0 * bank->l_b2
					- bank->ka1 * bank->l_a1
					- bank->ka2 * bank->l_a2
				;
				bank->l_a2 = bank->l_a1;
				bank->l_a1 = filtered;
				bank->l_b2 = bank->l_b1;
				bank->l_b1 = input;
				filtered *= filtered;
				bank->l_rms += (filtered - bank->l_rms) * bank->delta;
			}
			/* right */
			{
				auto input = p_wave[i + 1];
				auto filtered
					= bank->kb0 * input
					- bank->kb0 * bank->r_b2
					- bank->ka1 * bank->r_a1
					- bank->ka2 * bank->r_a2
				;
				bank->r_a2 = bank->r_a1;
				bank->r_a1 = filtered;
				bank->r_b2 = bank->r_b1;
				bank->r_b1 = input;
				filtered *= filtered;
				bank->r_rms += (filtered - bank->r_rms) * bank->delta;
			}
		}
	}
	/*** set peaks ***/
	auto l_lastpeak = 0.0;
	auto l_lastpeak_idx = -1;
	auto r_lastpeak = 0.0;
	auto r_lastpeak_idx = -1;
	auto mid_bank = p_instance->mid_bank;
	auto high_bank = p_instance->high_bank;
	auto threshold_wide = (int)(THRESHOLD_WIDE * p_instance->tone_div);
	auto threshold_narrow = (int)(THRESHOLD_NARROW * p_instance->tone_div);
	auto transpose = p_instance->transpose * p_instance->tone_div;
	for (int idx_b = 0; idx_b < p_instance->bank_count; ++idx_b) {
		/* calc threshold */
		int threshold_width;
		auto transpose_b = transpose + idx_b;
		if (transpose_b < mid_bank) {
			threshold_width = threshold_wide;
		} else if (transpose_b < high_bank) {
			auto mid2high = (double)(transpose_b - mid_bank) / (high_bank - mid_bank);
			threshold_width = (int)(threshold_wide * (1 - mid2high) + threshold_narrow * mid2high);
		} else {
			threshold_width = threshold_narrow;
		}
		auto l_threshold = 0.0;
		auto r_threshold = 0.0;
		for (int w = -threshold_width; w <= threshold_width; ++w) {
			auto idx_bw = fmin(p_instance->bank_count - 1, fmax(0, idx_b + w));
			l_threshold += p_instance->banks[idx_bw].l_rms;
			r_threshold += p_instance->banks[idx_bw].r_rms;
		}
		l_threshold /= threshold_width * 2 + 1;
		r_threshold /= threshold_width * 2 + 1;
		/* extract peak */
		p_instance->l_peaks[idx_b] = 0.0;
		p_instance->r_peaks[idx_b] = 0.0;
		auto l_rms = p_instance->banks[idx_b].l_rms;
		auto r_rms = p_instance->banks[idx_b].r_rms;
		if (l_rms < l_threshold) {
			if (0 <= l_lastpeak_idx) {
				p_instance->l_peaks[l_lastpeak_idx] = sqrt(l_lastpeak);
			}
			l_rms = 0.0;
			l_lastpeak = 0.0;
			l_lastpeak_idx = -1;
		}
		if (l_lastpeak < l_rms) {
			l_lastpeak = l_rms;
			l_lastpeak_idx = idx_b;
		}
		if (r_rms < r_threshold) {
			if (0 <= r_lastpeak_idx) {
				p_instance->r_peaks[r_lastpeak_idx] = sqrt(r_lastpeak);
			}
			r_rms = 0.0;
			r_lastpeak = 0.0;
			r_lastpeak_idx = -1;
		}
		if (r_lastpeak < r_rms) {
			r_lastpeak = r_rms;
			r_lastpeak_idx = idx_b;
		}
	}
	if (0 <= l_lastpeak_idx) {
		p_instance->l_peaks[l_lastpeak_idx] = sqrt(l_lastpeak);
	}
	if (0 <= r_lastpeak_idx) {
		p_instance->r_peaks[r_lastpeak_idx] = sqrt(r_lastpeak);
	}
}
