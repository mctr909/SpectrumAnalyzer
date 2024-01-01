#include <stdint.h>
#include <stdlib.h>
#include <Windows.h>
#include <mmsystem.h>
#include <mmreg.h>

#include "waveout.h"

#pragma comment (lib, "winmm.lib")

#define BUFFERSTATE_UNWRITABLE 0
#define BUFFERSTATE_WRITABLE   1
#define WRITE_REQUEST_TIMEOUT  2000
#define AVG_WRITE_COUNT_DELTA  0.1f

static void CALLBACK
waveOutProc(HWAVEOUT hwo, UINT uMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2) {
	WAVEOUT *p_instance = (WAVEOUT *)dwInstance;
	LPWAVEHDR p_hdr = (LPWAVEHDR)dwParam1;
	switch (uMsg) {
	case MM_WOM_OPEN:
		p_instance->enable_device = TRUE;
		break;
	case MM_WOM_CLOSE:
		p_instance->enable_device = FALSE;
		break;
	case MM_WOM_DONE:
		if (p_instance->enable_writer && NULL != p_hdr) {
			/* バッファの状態を書込可能に更新 */
			EnterCriticalSection(&p_instance->lock_buffer);
			p_hdr->dwUser = BUFFERSTATE_WRITABLE;
			LeaveCriticalSection(&p_instance->lock_buffer);
			/* 書込要求を送る */
			SetEvent(p_instance->write_request);
		}
		break;
	}
}

static DWORD CALLBACK
writer_task(LPVOID lpParameter) {
	WAVEOUT *p_instance = (WAVEOUT *)lpParameter;
	WAVEHDR *p_hdrs = p_instance->p_wavehdrs;
	int32_t write_index = 0;
	int32_t write_count = 0;
	float avg_write_count = 0;
	while (p_instance->enable_writer) {
		/* 書込要求を待つ */
		DWORD res = WaitForSingleObject(p_instance->write_request, WRITE_REQUEST_TIMEOUT);
		if (WAIT_TIMEOUT == res) {
			/* タイムアウトしたらタスクを終了 */
			EnterCriticalSection(&p_instance->lock_play_state);
			p_instance->play_state = PLAYSTATE_WRITER_TIMEOUT;
			LeaveCriticalSection(&p_instance->lock_play_state);
			p_instance->enable_writer = FALSE;
			break;
		}
		while (p_instance->enable_writer) {
			WAVEHDR *p_hdr = p_hdrs + write_index;
			/* 書込可能バッファであることを確認 */
			EnterCriticalSection(&p_instance->lock_buffer);
			if (p_hdr->dwUser == BUFFERSTATE_WRITABLE) {
				/* 書込不可に更新 */
				p_hdr->dwUser = BUFFERSTATE_UNWRITABLE;
				LeaveCriticalSection(&p_instance->lock_buffer);
				/* 書込数をカウント */
				write_count++;
			} else {
				/* 書込不可の場合 */
				LeaveCriticalSection(&p_instance->lock_buffer);
				/* 書込数を計測用項目に反映 */
				avg_write_count += (write_count - avg_write_count) * AVG_WRITE_COUNT_DELTA;
				p_instance->avg_write_rate = avg_write_count / p_instance->buffer_count;
				p_instance->write_rate = (float)write_count / p_instance->buffer_count;
				/* 書込数をクリア */
				write_count = 0;
				/* ループを抜けて書込要求を待つ */
				break;
			}
			/* バッファに書き込む */
			EnterCriticalSection(&p_instance->lock_play_state);
			if (p_instance->play_state & PLAYSTATE_PLAY) {
				/* 書込コールバック関数を実行 */
				p_instance->fp_writer((float *)p_hdr->lpData, p_instance->buffer_samples);
				p_instance->play_state = PLAYSTATE_PLAY;
			} else {
				/* 無音を書き込む */
				memset(p_hdr->lpData, 0, p_instance->buffer_size);
				p_instance->play_state = PLAYSTATE_PAUSE;
			}
			LeaveCriticalSection(&p_instance->lock_play_state);
			/* バッファをデバイスに送る */
			waveOutWrite(p_instance->device_handle, p_hdr, sizeof(WAVEHDR));
			/* 次のバッファへ */
			write_index = ++write_index % p_instance->buffer_count; 
		}
	}
	return 0;
}

static void
wait_state_changed(uint8_t *flag, uint8_t val) {
	for (int32_t i = 0; i < 20 && *flag != val; i++) {
		Sleep(50);
	}
}

static BOOL
open_device(WAVEOUT *p_instance, uint32_t init_device) {
	/* 再生フォーマット設定(32bit float ステレオ固定) */
	WAVEFORMATEX *p_fmt = &p_instance->wave_format;
	p_fmt->wFormatTag = WAVE_FORMAT_IEEE_FLOAT;
	p_fmt->nChannels = 2;
	p_fmt->wBitsPerSample = 32;
	p_fmt->nBlockAlign = p_fmt->wBitsPerSample * p_fmt->nChannels / 8;
	p_fmt->nAvgBytesPerSec = p_fmt->nBlockAlign * p_fmt->nSamplesPerSec;
	p_fmt->cbSize = 0;
	p_instance->buffer_size = p_fmt->nBlockAlign * p_instance->buffer_samples;
	/* デバイスを開く */
	MMRESULT res = waveOutOpen(
		&p_instance->device_handle,
		init_device,
		p_fmt,
		(DWORD_PTR)waveOutProc,
		(DWORD_PTR)p_instance,
		CALLBACK_FUNCTION
	);
	if (MMSYSERR_NOERROR != res) {
		return FALSE;
	}
	wait_state_changed(&p_instance->enable_device, TRUE);
	return TRUE;
}

static BOOL
alloc_buffer(WAVEOUT *p_instance) {
	/* 書込バッファ情報をbuffer_countで指定された数だけ確保 */
	p_instance->p_wavehdrs = (WAVEHDR*)calloc(p_instance->buffer_count, sizeof(WAVEHDR));
	if (NULL == p_instance->p_wavehdrs) {
		return FALSE;
	}
	/* 書込バッファ情報内のバッファを確保 */
	for (int32_t i = 0; i < p_instance->buffer_count; ++i) {
		WAVEHDR *p_hdr = p_instance->p_wavehdrs + i;
		p_hdr->lpData = (LPSTR)calloc(1, p_instance->buffer_size);
		if (NULL == p_hdr->lpData) {
			return FALSE;
		}
		p_hdr->dwBufferLength = p_instance->buffer_size;
		p_hdr->dwBytesRecorded = 0;
		p_hdr->dwUser = BUFFERSTATE_WRITABLE;
		p_hdr->dwFlags = 0;
		p_hdr->dwLoops = 0;
	}
	/* 書込バッファ情報をデバイスへ送り準備させる */
	for (int32_t i = 0; i < p_instance->buffer_count; ++i) {
		WAVEHDR *p_hdr = p_instance->p_wavehdrs + i;
		DWORD res = waveOutPrepareHeader(p_instance->device_handle, p_hdr, sizeof(WAVEHDR));
		if (MMSYSERR_NOERROR != res) {
			return FALSE;
		}
	}
	return TRUE;
}

static BOOL
start_writer(WAVEOUT *p_instance) {
	/* クリティカルセクションの初期化 */
	InitializeCriticalSection(&p_instance->lock_buffer);
	InitializeCriticalSection(&p_instance->lock_play_state);
	/* 書込要求イベントの作成 */
	p_instance->write_request = CreateEvent(NULL, FALSE, FALSE, NULL);
	if (NULL == p_instance->write_request) {
		return FALSE;
	}
	/* 書込スレッドの開始 */
	p_instance->enable_writer = TRUE;
	p_instance->write_thread = CreateThread(NULL, 0, writer_task, p_instance, 0, &p_instance->write_thread_id);
	if (NULL == p_instance->write_thread) {
		p_instance->enable_writer = FALSE;
		return FALSE;
	}
	SetThreadPriority(p_instance->write_thread, THREAD_PRIORITY_HIGHEST);
	return TRUE;
}

static void
purge(WAVEOUT *p_instance) {
	if (NULL == p_instance) {
		return;
	}
	/* デバイスを閉じる */
	if (p_instance->enable_device) {
		waveOutClose(p_instance->device_handle);
		wait_state_changed(&p_instance->enable_device, FALSE);
	}
	/* 書込スレッドの破棄 */
	if (NULL != p_instance->write_thread) {
		CloseHandle(p_instance->write_thread);
		p_instance->write_thread = NULL;
		p_instance->enable_writer = FALSE;
	}
	/* 書込要求イベントの破棄 */
	if (NULL != p_instance->write_request) {
		CloseHandle(p_instance->write_request);
		p_instance->write_request = NULL;
	}
	/* クリティカルセクションの削除 */
	DeleteCriticalSection(&p_instance->lock_buffer);
	DeleteCriticalSection(&p_instance->lock_play_state);
	/* 書込バッファの破棄 */
	if (NULL != p_instance->p_wavehdrs) {
		for (int32_t i = 0; i < p_instance->buffer_count; ++i) {
			WAVEHDR *p_hdr = p_instance->p_wavehdrs + i;
			if (NULL != p_hdr->lpData) {
				free(p_hdr->lpData);
			}
		}
		free(p_instance->p_wavehdrs);
		p_instance->p_wavehdrs = NULL;
	}
}

static BOOL
init_device(WAVEOUT *p_instance, uint32_t init_device) {
	/* 状態のクリア */
	p_instance->enable_device = FALSE;
	p_instance->enable_writer = FALSE;
	p_instance->play_state = PLAYSTATE_PAUSE;
	/* デバイスを開く */
	if (!open_device(p_instance, init_device)) {
		return FALSE;
	}
	/* バッファの確保 */
	if (!alloc_buffer(p_instance)) {
		purge(p_instance);
		return FALSE;
	}
	/* 書込スレッドの開始 */
	if (!start_writer(p_instance)) {
		purge(p_instance);
		return FALSE;
	}
	/* 最初の書込要求を送る */
	SetEvent(p_instance->write_request);
	return TRUE;
}

static void
fin_device(WAVEOUT *p_instance) {
	/* 書込スレッド終了待ち */
	p_instance->enable_writer = FALSE;
	SetEvent(p_instance->write_request);
	WaitForSingleObject(p_instance->write_thread, INFINITE);
	/* デバイスリセット */
	waveOutReset(p_instance->device_handle);
	for (int32_t i = 0; i < p_instance->buffer_count; ++i) {
		waveOutUnprepareHeader(
			p_instance->device_handle,
			p_instance->p_wavehdrs + i,
			sizeof(WAVEHDR)
		);
	}
	/* 破棄 */
	purge(p_instance);
}

void
waveout_create(
	WAVEOUT **pp_instance,
	uint32_t device_id,
	int32_t sample_rate,
	int32_t buffer_samples,
	uint8_t buffer_count,
	WAVEOUT_FUNCTION fp_writer
) {
	/* 既存インスタンスの破棄 */
	if (NULL != *pp_instance) {
		waveout_dispose(pp_instance);
	}
	/* インスタンス生成 */
	WAVEOUT *p_instance = (WAVEOUT *)malloc(sizeof(WAVEOUT));
	if (NULL == p_instance) {
		return;
	}
	*pp_instance = p_instance;
	/* 引数の値を設定 */
	p_instance->wave_format.nSamplesPerSec = (DWORD)sample_rate;
	p_instance->buffer_samples = buffer_samples;
	p_instance->buffer_count = buffer_count;
	p_instance->fp_writer = fp_writer;
	/* 計測用項目を初期化 */
	p_instance->write_rate = 0;
	p_instance->avg_write_rate = 0;
	/* デバイスの初期化とオープン */
	if (!init_device(p_instance, device_id)) {
		free(p_instance);
		*pp_instance = NULL;
	}
}

void
waveout_dispose(WAVEOUT **pp_instance) {
	WAVEOUT *p_instance = *pp_instance;
	if (NULL == p_instance) {
		return;
	}
	fin_device(p_instance);
	free(p_instance);
	*pp_instance = NULL;
}

void
waveout_select_device(WAVEOUT **pp_instance, uint32_t device_id) {
	WAVEOUT *p_instance = *pp_instance;
	if (NULL == p_instance) {
		return;
	}
	uint8_t play_state = p_instance->play_state;
	fin_device(p_instance);
	if (init_device(p_instance, device_id)) {
		EnterCriticalSection(&p_instance->lock_play_state);
		p_instance->play_state = play_state;
		LeaveCriticalSection(&p_instance->lock_play_state);
	}
}

void
waveout_start(WAVEOUT *p_instance) {
	if (NULL == p_instance) {
		return;
	}
	EnterCriticalSection(&p_instance->lock_play_state);
	if (p_instance->play_state & PLAYSTATE_PAUSE) {
		p_instance->play_state = PLAYSTATE_REQUEST_PLAY;
		LeaveCriticalSection(&p_instance->lock_play_state);
		wait_state_changed(&p_instance->play_state, PLAYSTATE_PLAY);
	} else {
		LeaveCriticalSection(&p_instance->lock_play_state);
	}
}

void
waveout_pause(WAVEOUT *p_instance) {
	if (NULL == p_instance) {
		return;
	}
	EnterCriticalSection(&p_instance->lock_play_state);
	if (p_instance->play_state & PLAYSTATE_PLAY) {
		p_instance->play_state = PLAYSTATE_REQUEST_PAUSE;
		LeaveCriticalSection(&p_instance->lock_play_state);
		wait_state_changed(&p_instance->play_state, PLAYSTATE_PAUSE);
	} else {
		LeaveCriticalSection(&p_instance->lock_play_state);
	}
}
