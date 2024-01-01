#include <stdint.h>
#include <math.h>
#include <stdlib.h>

#include "spectrum.h"

#define FALSE 0
#define TRUE 1

uint8_t
spectrum_create(SPECTRUM **pp_instance) {
	if (NULL != *pp_instance) {
		spectrum_dispose(pp_instance);
	}

	SPECTRUM *p_instance = (SPECTRUM *)malloc(sizeof(SPECTRUM));
	if (NULL == p_instance) {
		return FALSE;
	}

	*pp_instance = p_instance;

	/* 設定値の初期化(自動ゲイン) */
	p_instance->autogain.min = 1e-2;
	p_instance->autogain.dec_time = 3.0;
	p_instance->autogain.inc_time = 1e-2;

	/* 設定値の初期化(閾値) */
	p_instance->threshold.lowtone_radius = 4;
	p_instance->threshold.midtone_start = HALFTONE_DIV * 24;
	p_instance->threshold.hightone_radius = 1;
	p_instance->threshold.hightone_start = HALFTONE_DIV * 72;
	p_instance->threshold.avg_gain = sqrt(pow(10, 1.0 / 20.0));

	/* 設定値の初期化(フィルタ) */
	FILTER_SETTINGS fs = { 0 };
	fs.base_freq = 442 * pow(2, 3.0 / 12.0 + (1.0 / HALFTONE_DIV - 1) / 12.0 - 5);
	fs.halftone_at_freq = 300.0;
	fs.rms_speed_low = 150.0;
	fs.rms_speed_high = 44100.0;
	fs.sample_rate = 44100;
	spectrum_setup(p_instance, fs, 1);

	return TRUE;
}

void
spectrum_dispose(SPECTRUM **pp_instance) {
	if (NULL != *pp_instance) {
		free(*pp_instance);
		*pp_instance = NULL;
	}
}

void
spectrum_setup(
	SPECTRUM *p_instance,
	FILTER_SETTINGS filter_settings,
	uint8_t initialize
) {
	if (NULL == p_instance) {
		return;
	}

	for (int32_t i = 0; i < BANK_COUNT; ++i) {
		BPF_BANK *p_banks = p_instance->banks + i;
		if (initialize) {
			/* 状態変数の初期化 */
			p_banks->lb1 = 0;
			p_banks->lb2 = 0;
			p_banks->la1 = 0;
			p_banks->la2 = 0;
			p_banks->rb1 = 0;
			p_banks->rb2 = 0;
			p_banks->ra1 = 0;
			p_banks->ra2 = 0;
			p_banks->square_l = 0;
			p_banks->square_r = 0;
		}

		/* フィルタリング周波数によってバンド幅を変える */
		double frequency = filter_settings.base_freq * pow(2.0, (double)i / OCT_DIV);
		double halftone = 0.5 + log2(filter_settings.halftone_at_freq / frequency);
		if (halftone < 0.5) {
			halftone = 0.5;
		}
		double band_width = halftone / 12.0;

		/* バイクアッドフィルタ(BPF)の係数を設定 */
		double omega = 8.0 * atan(1) * frequency / filter_settings.sample_rate;
		double c = cos(omega);
		double s = sin(omega);
		double x = log(2.0) / 2.0 * band_width * omega / s;
		double alpha = s * sinh(x);
		double a0 = 1.0 + alpha;
		p_banks->kb0 = (float)(alpha / a0);
		p_banks->ka1 = (float)(-2.0 * c / a0);
		p_banks->ka2 = (float)((1.0 - alpha) / a0);

		/* RMSの応答速度を設定 */
		if (frequency < filter_settings.rms_speed_low) {
			frequency = filter_settings.rms_speed_low;
		}
		else if (frequency > filter_settings.rms_speed_high) {
			frequency = filter_settings.rms_speed_high;
		}
		p_banks->rms_speed = (float)(0.5 * frequency / filter_settings.sample_rate);
	}
}

void
spectrum_mean_square(
	SPECTRUM *p_instance,
	float *p_input,
	int32_t sample_count
) {
	/* フィルタバンクループ */
	BPF_BANK *p_bank = (*p_instance).banks;
	for (int ixb = BANK_COUNT; ixb != 0; --ixb) {
		float lb0;
		float lb1 = p_bank->lb1;
		float lb2 = p_bank->lb2;
		float la0;
		float la1 = p_bank->la1;
		float la2 = p_bank->la2;
		float rb0;
		float rb1 = p_bank->rb1;
		float rb2 = p_bank->rb2;
		float ra0;
		float ra1 = p_bank->ra1;
		float ra2 = p_bank->ra2;
		float square_l = p_bank->square_l;
		float square_r = p_bank->square_r;
		float kb0 = p_bank->kb0;
		float ka1 = p_bank->ka1;
		float ka2 = p_bank->ka2;
		float rms_speed = p_bank->rms_speed;
		/* 波形サンプルループ */
		float *p_wave = p_input;
		for (int ixs = sample_count; ixs != 0; --ixs) {
			/* BPF */
			lb0 = *p_wave++;
			rb0 = *p_wave++;
			la0 = lb0 - lb2;
			la0 *= kb0;
			la0 -= la1 * ka1;
			la0 -= la2 * ka2;
			ra0 = rb0 - rb2;
			ra0 *= kb0;
			ra0 -= ra1 * ka1;
			ra0 -= ra2 * ka2;
			lb2 = lb1;
			lb1 = lb0;
			la2 = la1;
			la1 = la0;
			rb2 = rb1;
			rb1 = rb0;
			ra2 = ra1;
			ra1 = ra0;
			/* 2乗平均 */
			la0 *= la0;
			la0 -= square_l;
			square_l += la0 * rms_speed;
			ra0 *= ra0;
			ra0 -= square_r;
			square_r += ra0 * rms_speed;
		}
		p_bank->lb1 = lb1;
		p_bank->lb2 = lb2;
		p_bank->la1 = la1;
		p_bank->la2 = la2;
		p_bank->rb1 = rb1;
		p_bank->rb2 = rb2;
		p_bank->ra1 = ra1;
		p_bank->ra2 = ra2;
		p_bank->square_l = square_l;
		p_bank->square_r = square_r;
		/* 次のバンクへ */
		++p_bank;
	}
}
