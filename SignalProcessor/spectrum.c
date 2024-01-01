#include <stdint.h>
#include <stdbool.h>
#include <math.h>
#include <stdlib.h>
#include <string.h>

#ifdef _M_X64
#include <xmmintrin.h>
#include <pmmintrin.h>
#endif

#include "spectrum.h"

#define DEFAULT_SAMPLE_RATE        44100
#define DEFAULT_PITCH_A4           442.0
#define DEFAULT_HALFTONE_AT_FREQ   300.0

#define DEFAULT_RMS_SPEED_MIN      150.0
#define DEFAULT_RMS_SPEED_MAX      DEFAULT_SAMPLE_RATE

#define DEFAULT_AUTOGAIN_DEC_TIME  3.0f
#define DEFAULT_AUTOGAIN_INC_TIME  1e-2f
#define DEFAULT_AUTOGAIN_LIMIT_MIN 1e-2f

static void
disable_denormals() {
#ifdef _M_X64
	/* MXCSRを読み取る */
	uint32_t csr = _mm_getcsr();
	csr |= _MM_FLUSH_ZERO_ON;     // FTZ（結果がデノーマルなら0に丸める）
	csr |= _MM_DENORMALS_ZERO_ON; // DAZ（入力のデノーマルを0として扱う）
	/* 設定書き戻し */
	_mm_setcsr(csr);
#endif
}

bool
spectrum_create(SPECTRUM **pp_instance) {
	if (NULL != *pp_instance) {
		spectrum_dispose(pp_instance);
	}

	/* インスタンスを生成 */
	*pp_instance = (SPECTRUM *)malloc(sizeof(SPECTRUM));
	if (NULL == *pp_instance) {
		return false;
	}
	SPECTRUM* p_instance = *pp_instance;

	/* 設定値の初期化(フィルタ) */
	SPECTRUM_SETTINGS fs = {
		.sample_rate = DEFAULT_SAMPLE_RATE,
		.base_freq = DEFAULT_PITCH_A4 * pow(2, 3.0 / 12.0 + (1.0 / HALFTONE_DIV - 1) / 12.0 - 5),
		.halftone_at_freq = DEFAULT_HALFTONE_AT_FREQ,
		.rms_speed_min = DEFAULT_RMS_SPEED_MIN,
		.rms_speed_max = DEFAULT_RMS_SPEED_MAX
	};
	spectrum_setup(p_instance, fs, true);

	/* 設定値の初期化(自動ゲイン) */
	SPECTRUM_AUTOGAIN ag = {
		.dec_time = DEFAULT_AUTOGAIN_DEC_TIME,
		.inc_time = DEFAULT_AUTOGAIN_INC_TIME,
		.limit_min = DEFAULT_AUTOGAIN_LIMIT_MIN,
		.autogain_l = 0,
		.autogain_r = 0,
		.max_l = 0,
		.max_r = 0
	};
	p_instance->autogain = ag;

	/* デノーマルフロート無効化 */
	disable_denormals();

	return true;
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
	SPECTRUM* p_instance,
	SPECTRUM_SETTINGS filter_settings,
	bool initialize
) {
	if (NULL == p_instance) {
		return;
	}

	memmove(&p_instance->settings, &filter_settings, sizeof(filter_settings));

	for (int32_t ixb = 0; ixb < SPECTRUM_BANK_COUNT; ++ixb) {
		SPECTRUM_BANK* p_banks = p_instance->banks + ixb;
		if (initialize) {
			/* 状態変数の初期化 */
			p_banks->la1 = 0;
			p_banks->la2 = 0;
			p_banks->lb1 = 0;
			p_banks->lb2 = 0;
			p_banks->ra1 = 0;
			p_banks->ra2 = 0;
			p_banks->rb1 = 0;
			p_banks->rb2 = 0;
			p_banks->power_l = 0;
			p_banks->power_r = 0;
		}

		/* フィルタリング周波数 */
		double frequency = filter_settings.base_freq * pow(2.0, (double)ixb / OCT_DIV);

		/* フィルタリング周波数によってバンド幅を変える */
		double band_width = 0.5 + log2(filter_settings.halftone_at_freq / frequency);
		if (band_width < 0.5) {
			band_width = 0.5;
		}
		band_width /= 12.0;

		/* バイカッドフィルタ(BPF)の係数を設定 */
		double omega = 8.0 * atan(1.0) * frequency / filter_settings.sample_rate;
		double c = cos(omega);
		double s = sin(omega);
		double x = log(2.0) / 2.0 * omega / s * band_width;
		double alpha = s * sinh(x);
		double a0 = 1.0 + alpha;
		p_banks->ka1 = (float)(2.0 * c / a0);
		p_banks->ka2 = (float)(-(1.0 - alpha) / a0);
		p_banks->kb0 = (float)(alpha / a0);

		/* RMSの応答速度を設定 */
		if (frequency < filter_settings.rms_speed_min) {
			frequency = filter_settings.rms_speed_min;
		} else if (frequency > filter_settings.rms_speed_max) {
			frequency = filter_settings.rms_speed_max;
		}
		p_banks->rms_speed = (float)(0.5 * frequency / filter_settings.sample_rate);
	}

	if (initialize) {
		/* 自動ゲインの値を初期化 */
		p_instance->autogain.autogain_l = p_instance->autogain.limit_min;
		p_instance->autogain.autogain_r = p_instance->autogain.limit_min;
		p_instance->autogain.max_l = p_instance->autogain.limit_min;
		p_instance->autogain.max_r = p_instance->autogain.limit_min;
	}
}

void
spectrum_calc_power(
	SPECTRUM *p_instance,
	float *p_input,
	int32_t sample_count
) {
	SPECTRUM_BANK* p_bank = p_instance->banks;
	SPECTRUM_BANK* p_bank_term = p_bank + SPECTRUM_BANK_COUNT;
	float* p_wave = p_input;
	float* p_wave_start = p_wave;
	float* p_wave_term = p_wave + sample_count * 2;
	/* フィルタバンクループ */
	while (p_bank < p_bank_term) {
		float la1 = p_bank->la1;
		float la2 = p_bank->la2;
		float lb1 = p_bank->lb1;
		float lb2 = p_bank->lb2;
		float ra1 = p_bank->ra1;
		float ra2 = p_bank->ra2;
		float rb1 = p_bank->rb1;
		float rb2 = p_bank->rb2;
		float a0;
		float b0;
		float power_l = p_bank->power_l;
		float power_r = p_bank->power_r;
		float ka1 = p_bank->ka1;
		float ka2 = p_bank->ka2;
		float kb0 = p_bank->kb0;
		float rms_speed = p_bank->rms_speed;
		/* 波形サンプルループ */
		while (p_wave < p_wave_term) {
			/* BPF(左) */
			b0 = *p_wave++;
			a0 = b0 - lb2;
			a0 *= kb0;
			a0 = fmaf(la1, ka1, a0);
			a0 = fmaf(la2, ka2, a0);
			/* 状態変数の更新(左) */
			la2 = la1;
			la1 = a0;
			lb2 = lb1;
			lb1 = b0;
			/* 2乗平均(左) */
			a0 *= a0;
			a0 -= power_l;
			power_l = fmaf(a0, rms_speed, power_l);
			/* BPF(右) */
			b0 = *p_wave++;
			a0 = b0 - rb2;
			a0 *= kb0;
			a0 = fmaf(ra1, ka1, a0);
			a0 = fmaf(ra2, ka2, a0);
			/* 状態変数の更新(右) */
			ra2 = ra1;
			ra1 = a0;
			rb2 = rb1;
			rb1 = b0;
			/* 2乗平均(右) */
			a0 *= a0;
			a0 -= power_r;
			power_r = fmaf(a0, rms_speed, power_r);
		}
		p_bank->la1 = la1;
		p_bank->la2 = la2;
		p_bank->lb1 = lb1;
		p_bank->lb2 = lb2;
		p_bank->ra1 = ra1;
		p_bank->ra2 = ra2;
		p_bank->rb1 = rb1;
		p_bank->rb2 = rb2;
		p_bank->power_l = power_l;
		p_bank->power_r = power_r;
		/* 次のバンクへ */
		p_bank++;
		p_wave = p_wave_start;
	}
	/* パワーの最大値を取得 */
	float max_l = 0;
	float max_r = 0;
	p_bank = p_instance->banks;
	while (p_bank < p_bank_term) {
		max_l = fmaxf(max_l, p_bank->power_l);
		max_r = fmaxf(max_r, p_bank->power_r);
		p_bank++;
	}
	/* パワーをリニアへ変換 */
	max_l = sqrtf(max_l * 2.0f);
	max_r = sqrtf(max_r * 2.0f);
	/* 最大値に追随して自動ゲインを更新 */
	SPECTRUM_AUTOGAIN* p_ag = &p_instance->autogain;
	float dec_time = p_ag->dec_time;
	float inc_time = p_ag->inc_time;
	float limit_min = p_ag->limit_min;
	float l_diff = max_l - p_ag->autogain_l;
	float r_diff = max_r - p_ag->autogain_r;
	float delta = (float)sample_count / p_instance->settings.sample_rate;
	l_diff *= delta / (l_diff < 0 ? dec_time : inc_time);
	r_diff *= delta / (r_diff < 0 ? dec_time : inc_time);
	p_ag->autogain_l += l_diff;
	p_ag->autogain_l = fmaxf(p_ag->autogain_l, limit_min);
	p_ag->autogain_r += r_diff;
	p_ag->autogain_r = fmaxf(p_ag->autogain_r, limit_min);
	p_ag->max_l = fmaxf(max_l, limit_min);
	p_ag->max_r = fmaxf(max_r, limit_min);
}
