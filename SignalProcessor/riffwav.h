#pragma once
#include <stdint.h>

/* RIFFファイルで使用する識別子定義 */
typedef uint32_t RIFFSIGN;
enum RIFFSIGN {
	RIFFSIGN_RIFF = 'RIFF',
	RIFFSIGN_WAVE = 'WAVE',
	RIFFSIGN_fmt_ = 'fmt ',
	RIFFSIGN_data = 'data'
};

/* RIFFチャンク情報構造体 */
typedef struct _RIFFCHUNK {
	RIFFSIGN sign;
	uint32_t size;
} RIFFCHUNK;

/* RIFFWAVファイルで使用するデータ値の定義 */
typedef uint16_t WAVETYPE;
enum WAVETYPE {
	WAVETYPE_PCM_INT = 1,
	WAVETYPE_PCM_FLOAT = 3
};

/* RIFFWAVファイルで使用するフォーマット構造体 */
typedef struct _FMT {
	WAVETYPE format;
	uint16_t channel;
	uint32_t sample_rate;
	uint32_t bytes_per_second;
	uint16_t block_size;
	uint16_t bits_per_sample;
} FMT;

typedef struct _RIFFWAV {
	/* ファイルポインタ */
	FILE *fp;
	/* フォーマット情報 */
	FMT format;
	/* データチャンクのバイト数 */
	uint32_t data_size;
	/* データチャンクの開始位置 */
	uint32_t data_offset;
	/* 総サンプル数 */
	int32_t total_samples;

	/* 入力バッファのサンプル数 */
	int32_t input_samples;
	/* 入力バッファの開始位置 */
	int32_t input_position;
	/* 入力バッファ(input_samples+1サンプル分を読込) */
	void* p_input;

	/* 出力バッファのサンプル数 */
	int32_t output_samples;
	/* 出力バッファ(ステレオインターリーブ形式LRLR... output_samplesサンプル分を書込) */
	float* p_output;

	/* サンプリングレート変換係数 */
	float delta;
	/* 再生速度 */
	float speed;
	/* 現在位置 */
	float position;

	/* */
	void (*fp_reader)(void *p_instance);
} RIFFWAV;

/**
 * RIFFWAVファイルのオープンとインスタンス生成を行う
 * @param pp_instance 生成されたインスタンス
 * @param path ファイルパス
 * @param output_samples 出力バッファのサンプル数
 * @param sample_rate 再生サンプリングレート
 */
void riffwav_open(RIFFWAV **pp_instance, wchar_t *path, int32_t output_samples, int32_t sample_rate);

/**
 * RIFFWAVファイルのクローズとインスタンス破棄を行う
 * @param pp_instance 破棄するインスタンス
 */
void riffwav_dispose(RIFFWAV **pp_instance);
