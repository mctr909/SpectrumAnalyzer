#pragma once

/* 再生状態の定義 */
typedef uint8_t PLAYSTATE;
enum PLAYSTATE {
	/* 書込処理タイムアウト */
	PLAYSTATE_WRITER_TIMEOUT = 0b0000,
	/* 一時停止要求 */
	PLAYSTATE_REQUEST_PAUSE  = 0b0010,
	/* 一時停止中 */
	PLAYSTATE_PAUSE          = 0b0011,
	/* 再生要求 */
	PLAYSTATE_REQUEST_PLAY   = 0b1000,
	/* 再生中 */
	PLAYSTATE_PLAY           = 0b1100
};

/**
 * 波形書込コールバック関数の定義
 * @param p_output 書込先バッファ(ステレオインターリーブLRLR...)
 * @param samples 書込サンプル数(LRペア数)
 */
typedef void (*WAVEOUT_FUNCTION)(float* p_output, int32_t samples);

/* 音声出力デバイスのインスタンス構造体 */
typedef struct _WAVEOUT {
	/* 再生状態 */
	PLAYSTATE play_state;
	/* デバイスが有効であるか */
	uint8_t enable_device;
	/* 書込スレッドが有効であるか */
	uint8_t enable_writer;
	/* バッファの数 */
	uint8_t buffer_count;

	/* 1バッファに対してのサンプル数 */
	int32_t buffer_samples;
	/* 1バッファに対してのバイト数 */
	int32_t buffer_size;
	/* バッファ書込率 */
	float write_rate;
	/* 平均バッファ書込率 */
	float avg_write_rate;

	/* デバイスハンドル */
	HWAVEOUT device_handle;
	/* 再生フォーマット情報 */
	WAVEFORMATEX wave_format;
	/* 書込バッファ情報(buffer_countで指定された数だけ確保) */
	WAVEHDR *p_wavehdrs;

	/* 書込バッファのクリティカルセクション */
	CRITICAL_SECTION lock_buffer;
	/* 再生状態のクリティカルセクション */
	CRITICAL_SECTION lock_play_state;
	/* 書込要求イベント */
	HANDLE write_request;
	/* 書込スレッドハンドル */
	HANDLE write_thread;
	/* 書込スレッドID */
	uint32_t write_thread_id;
	/* 書込コールバック関数 */
	WAVEOUT_FUNCTION fp_writer;
} WAVEOUT;

/**
 * 音声出力デバイスのオープンとインスタンス生成を行う
 * @param pp_instance 生成されたインスタンス
 * @param device_id デバイスID(-1でデフォルトデバイス)
 * @param sample_rate サンプリングレート
 * @param buffer_samples 1バッファに対してのサンプル数
 * @param buffer_count バッファの数
 * @param fp_writer 書込コールバック関数
 */
void waveout_create(
	WAVEOUT **pp_instance,
	uint32_t device_id,
	int32_t sample_rate,
	int32_t buffer_samples,
	uint8_t buffer_count,
	WAVEOUT_FUNCTION fp_writer
);

/**
 * 音声出力デバイスのクローズとインスタンス破棄を行う
 * @param pp_instance 破棄するインスタンス
 */
void waveout_dispose(WAVEOUT **pp_instance);

/**
 * 音声出力デバイスの選択を行う
 * @param pp_instance インスタンス
 * @param device_id デバイスID(-1でデフォルトデバイス)
 */
void waveout_select_device(WAVEOUT **pp_instance, uint32_t device_id);

/**
 * 音声出力デバイスの再生開始を行う
 * @param p_instance インスタンス
 */
void waveout_start(WAVEOUT *p_instance);

/**
 * 音声出力デバイスの一時停止を行う
 * @param p_instance インスタンス
 */
void waveout_pause(WAVEOUT *p_instance);
