#pragma once

#define HALFTONE_COUNT 126;
#define HALFTONE_DIV 4
#define OCT_DIV 48
#define BANK_COUNT 504

#pragma pack(8)
typedef struct _BPF_BANK {
	float lb1;
	float lb2;
	float la1;
	float la2;

	float rb1;
	float rb2;
	float ra1;
	float ra2;

	float square_l;
	float square_r;

	float kb0;
	float ka1;
	float ka2;
	float rms_speed;
} BPF_BANK;
#pragma pack()

typedef struct _AUTOGAIN {
	/* 最小値 */
	double min;
	/* 減少時間[秒] */
	double dec_time;
	/* 増加時間[秒] */
	double inc_time;
} AUTOGAIN;

typedef struct _THRESHOLD {
	/* 低音域閾値 計算半径[フィルタバンク数] */
	uint32_t lowtone_radius;
	/* 中音域閾値 開始位置[フィルタバンク数] */
	uint32_t midtone_start;
	/* 高音域閾値 計算半径[フィルタバンク数] */
	uint32_t hightone_radius;
	/* 高音域閾値 開始位置[フィルタバンク数] */
	uint32_t hightone_start;
	/* 平均値ゲイン[sqrt(10^(デシベル/20))] */
	double avg_gain;
} THRESHOLD;

typedef struct _FILTER_SETTINGS {
	/* 基本周波数[Hz] */
	double base_freq;
	/* 帯域幅が1半音に至る周波数[Hz] */
	double halftone_at_freq;
	/* 低域RMS応答速度[Hz] */
	double rms_speed_low;
	/* 高域RMS応答速度[Hz] */
	double rms_speed_high;
	/* サンプリング周波数[Hz] */
	uint32_t sample_rate;
} FILTER_SETTINGS;

typedef struct _SPECTRUM {
	/* 自動ゲイン */
	AUTOGAIN autogain;
	/* 閾値 */
	THRESHOLD threshold;
	/* BPFバンク */
	BPF_BANK banks[BANK_COUNT];
} SPECTRUM;

/** スペクトラムの生成を行う */
uint8_t spectrum_create(SPECTRUM **pp_instance);

/** スペクトラムの破棄を行う */
void spectrum_dispose(SPECTRUM **pp_instance);

/**
 * スペクトラムの設定を行う
 * @param (p_instance) インスタンス
 * @param (filter_settings) フィルタ設定
 * @param (initialize) 初期化の有無
 */
void spectrum_setup(
	SPECTRUM *p_instance,
	FILTER_SETTINGS filter_settings,
	uint8_t initialize
);

/**
 * @param (p_instance) インスタンス
 * @param (p_input) 入力バッファ
 * @param (sample_count) サンプル数
 */
void spectrum_mean_square(
	SPECTRUM *p_instance,
	float *p_input,
	int32_t sample_count
);
