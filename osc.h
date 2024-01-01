struct TONE_BANK {
	double amp_l;
	double amp_r;
	double phase;
};

struct OSC {
	double pitch = 1.0;
	int tone_count = 0;
	TONE_BANK *tones = nullptr;
};

struct SPECTRUM;

void osc_init(OSC *p_instance, int tone_count);
void osc_free(OSC *p_instance);
void osc_exec(OSC *p_instance, SPECTRUM &spec, double *p_wave, int wave_samples);
