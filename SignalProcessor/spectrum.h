#pragma once
#include <stdint.h>
#include <stdbool.h>

#define HALFTONE_COUNT 126
#define HALFTONE_DIV 4
#define OCT_DIV 48
#define SPECTRUM_BANK_COUNT 504

/* スペクトラム設定 */
typedef struct _SPECTRUM_SETTINGS {
	/* サンプリング周波数[Hz] */
	uint32_t sample_rate;
	/* 基本周波数[Hz] */
	double base_freq;
	/* 帯域幅が1半音に至る周波数[Hz] */
	double halftone_at_freq;
	/* RMS応答速度 下限[Hz] */
	double rms_speed_min;
	/* RMS応答速度 上限[Hz] */
	double rms_speed_max;
} SPECTRUM_SETTINGS;

/* 自動ゲイン設定 */
typedef struct _SPECTRUM_AUTOGAIN {
	/* 減少時間[秒] */
	float dec_time;
	/* 増加時間[秒] */
	float inc_time;
	/* 下限 */
	float limit_min;
	/* 自動ゲイン(左) */
	float autogain_l;
	/* 自動ゲイン(右) */
	float autogain_r;
	/* 最大値(左) */
	float max_l;
	/* 最大値(右) */
	float max_r;
} SPECTRUM_AUTOGAIN;

/* BPFバンク */
typedef struct _SPECTRUM_BANK {
	float la1;
	float la2;
	float lb1;
	float lb2;

	float ra1;
	float ra2;
	float rb1;
	float rb2;

	float power_l;
	float power_r;

	float ka1;
	float ka2;
	float kb0;
	float rms_speed;
} SPECTRUM_BANK;

/* スペクトラムのインスタンス構造体 */
typedef struct _SPECTRUM {
	/* フィルタ設定 */
	SPECTRUM_SETTINGS settings;
	/* 自動ゲイン */
	SPECTRUM_AUTOGAIN autogain;
	/* BPFバンク */
	SPECTRUM_BANK banks[SPECTRUM_BANK_COUNT];
} SPECTRUM;

/**
 * スペクトラムの生成を行う
 * @param pp_instance インスタンス
 * @return 成功したかを返す
 */
bool spectrum_create(SPECTRUM **pp_instance);

/**
 * スペクトラムの破棄を行う
 * @param pp_instance インスタンス
 */
void spectrum_dispose(SPECTRUM **pp_instance);

/**
 * スペクトラムの設定を行う
 * @param p_instance インスタンス
 * @param filter_settings フィルタ設定
 * @param initialize 初期化の有無
 */
void spectrum_setup(
	SPECTRUM *p_instance,
	SPECTRUM_SETTINGS filter_settings,
	bool initialize
);

/**
 * 入力波形をもとに各バンドのパワースペクトルを取得する
 * 同時に最大値と自動ゲインも更新する
 * @param p_instance インスタンス
 * @param p_input 入力バッファ(ステレオインターリーブ形式LRLR...)
 * @param sample_count 入力バッファのサンプル数(LRペア数)
 */
void spectrum_calc_power(
	SPECTRUM *p_instance,
	float *p_input,
	int32_t sample_count
);
