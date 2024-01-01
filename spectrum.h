struct SPEC_BANK {
	double kb0;
	double ka1;
	double ka2;
	double delta;
	double l_a1;
	double l_a2;
	double l_b1;
	double l_b2;
	double l_rms;
	double r_a1;
	double r_a2;
	double r_b1;
	double r_b2;
	double r_rms;
};

struct SPECTRUM {
	int bank_count = 0;
	int tone_div = 0;
	int mid_bank = 0;
	int high_bank = 0;
	double transpose = 0.0;
	double *l_peaks = nullptr;
	double *r_peaks = nullptr;
	SPEC_BANK *banks = nullptr;
};

void spectrum_init(SPECTRUM *p_instance, int tone_count, int tone_div, int sample_rate, double base_freq);
void spectrum_free(SPECTRUM *p_instance);
void spectrum_exec(SPECTRUM *p_instance, double *p_wave, int wave_samples);
