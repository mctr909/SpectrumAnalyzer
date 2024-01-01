#include <stdint.h>
#include <stdlib.h>
#include <Windows.h>
#include <mmsystem.h>
#include <mmreg.h>

#include "waveout.h"

#pragma comment (lib, "winmm.lib")

enum BUFFERSTATE {
	BUFFERSTATE_UNWRITABLE = 0,
	BUFFERSTATE_WRITABLE = 1
};

enum PLAYSTATE {
	PLAYSTATE_REQUEST_PAUSE = 0b000,
	PLAYSTATE_PAUSED        = 0b001,
	PLAYSTATE_REQUEST_PLAY  = 0b100,
	PLAYSTATE_PLAY          = 0b110
};

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
			EnterCriticalSection(&p_instance->lock_buffer);
			p_hdr->dwUser = BUFFERSTATE_WRITABLE;
			LeaveCriticalSection(&p_instance->lock_buffer);
			SetEvent(p_instance->write_ready);
		}
		break;
	}
}

static DWORD CALLBACK
write_task(LPVOID lpParameter) {
	WAVEOUT *p_instance = (WAVEOUT *)lpParameter;
	WAVEHDR *p_hdrs = p_instance->p_wave_hdrs;
	int32_t write_index = 0;
	while (p_instance->enable_writer) {
		WaitForSingleObject(p_instance->write_ready, INFINITE);
		while (p_instance->enable_writer) {
			WAVEHDR *p_hdr = p_hdrs + write_index;
			EnterCriticalSection(&p_instance->lock_buffer);
			if (p_hdr->dwUser == BUFFERSTATE_WRITABLE) {
				p_hdr->dwUser = BUFFERSTATE_UNWRITABLE;
				LeaveCriticalSection(&p_instance->lock_buffer);
			} else {
				LeaveCriticalSection(&p_instance->lock_buffer);
				break;
			}
			EnterCriticalSection(&p_instance->lock_playstate);
			if (p_instance->playstate & PLAYSTATE_PLAY) {
				p_instance->fp_writer((float *)p_hdr->lpData, p_instance->buffer_samples);
				p_instance->playstate = PLAYSTATE_PLAY;
			} else {
				memset(p_hdr->lpData, 0, p_instance->buffer_size);
				p_instance->playstate = PLAYSTATE_PAUSED;
			}
			LeaveCriticalSection(&p_instance->lock_playstate);
			waveOutWrite(p_instance->device_handle, p_hdr, sizeof(WAVEHDR));
			write_index = ++write_index % p_instance->buffer_count; 
		}
	}
	return 0;
}

static void
wait_value(uint8_t *flag, uint8_t val) {
	for (int32_t i = 0; i < 100 && *flag != val; i++) {
		Sleep(10);
	}
}

static BOOL
open_device(WAVEOUT *p_instance, uint32_t init_device) {
	/* 状態のクリア */
	p_instance->enable_device = FALSE;
	p_instance->enable_writer = TRUE;
	p_instance->playstate = PLAYSTATE_PAUSED;
	/* 再生フォーマット(32bit float ステレオ固定) */
	WAVEFORMATEX *p_fmt = &p_instance->wave_format;
	p_fmt->wFormatTag = WAVE_FORMAT_IEEE_FLOAT;
	p_fmt->nChannels = 2;
	p_fmt->wBitsPerSample = 32;
	p_fmt->nBlockAlign = 8;
	p_fmt->nAvgBytesPerSec = p_fmt->nBlockAlign * p_fmt->nSamplesPerSec;
	p_fmt->cbSize = 0;
	/* デバイスオープン */
	MMRESULT res = waveOutOpen(
		&p_instance->device_handle,
		init_device,
		p_fmt,
		(DWORD_PTR)waveOutProc,
		(DWORD_PTR)p_instance,
		CALLBACK_FUNCTION
	);
	if (res != MMSYSERR_NOERROR) {
		return FALSE;
	}
	wait_value(&p_instance->enable_device, TRUE);
	return TRUE;
}

static BOOL
alloc_buffer(WAVEOUT *p_instance) {
	p_instance->p_wave_hdrs = (LPWAVEHDR)calloc(p_instance->buffer_count, sizeof(WAVEHDR));
	if (NULL == p_instance->p_wave_hdrs) {
		return FALSE;
	}
	for (int32_t i = 0; i < p_instance->buffer_count; ++i) {
		LPWAVEHDR p_hdr = p_instance->p_wave_hdrs + i;
		p_hdr->lpData = (LPSTR)calloc(1, p_instance->buffer_size);
		if (NULL == p_hdr->lpData) {
			return FALSE;
		}
		p_hdr->dwBufferLength = p_instance->buffer_size;
		p_hdr->dwUser = 0;
		p_hdr->dwFlags = 0;
		p_hdr->dwLoops = 0;
		DWORD res = waveOutPrepareHeader(p_instance->device_handle, p_hdr, sizeof(WAVEHDR));
		if (MMSYSERR_NOERROR != res) {
			return FALSE;
		}
	}
	return TRUE;
}

static BOOL
alloc_thread(WAVEOUT *p_instance) {
	InitializeCriticalSection(&p_instance->lock_buffer);
	InitializeCriticalSection(&p_instance->lock_playstate);
	p_instance->write_ready = CreateEvent(NULL, FALSE, FALSE, NULL);
	if (NULL == p_instance->write_ready) {
		return FALSE;
	}
	p_instance->write_thread = CreateThread(NULL, 0, write_task, p_instance, 0, &p_instance->write_thread_id);
	if (NULL == p_instance->write_thread) {
		return FALSE;
	}
	return TRUE;
}

static void
parge(WAVEOUT *p_instance) {
	if (NULL == p_instance) {
		return;
	}
	/* デバイスを閉じる */
	if (p_instance->enable_device) {
		waveOutClose(p_instance->device_handle);
		wait_value(&p_instance->enable_device, FALSE);
	}
	/* スレッドの破棄 */
	if (NULL != p_instance->write_thread) {
		CloseHandle(p_instance->write_thread);
		p_instance->write_thread = NULL;
	}
	if (NULL != p_instance->write_ready) {
		CloseHandle(p_instance->write_ready);
		p_instance->write_ready = NULL;
	}
	DeleteCriticalSection(&p_instance->lock_buffer);
	DeleteCriticalSection(&p_instance->lock_playstate);
	/* バッファの破棄 */
	if (NULL != p_instance->p_wave_hdrs) {
		for (int32_t i = 0; i < p_instance->buffer_count; ++i) {
			WAVEHDR *p_hdr = p_instance->p_wave_hdrs + i;
			if (NULL != p_hdr->lpData) {
				free(p_hdr->lpData);
			}
		}
		free(p_instance->p_wave_hdrs);
		p_instance->p_wave_hdrs = NULL;
	}
}

static BOOL
init_device(WAVEOUT *p_instance, uint32_t init_device) {
	if (!open_device(p_instance, init_device)) {
		return FALSE;
	}
	if (!alloc_buffer(p_instance)) {
		parge(p_instance);
		return FALSE;
	}
	if (!alloc_thread(p_instance)) {
		parge(p_instance);
		return FALSE;
	}
	/* バッファ出力 */
	for (int32_t i = 0; i < p_instance->buffer_count; ++i) {
		LPWAVEHDR p_hdr = p_instance->p_wave_hdrs + i;
		waveOutWrite(p_instance->device_handle, p_hdr, sizeof(WAVEHDR));
	}
	return TRUE;
}

static void
fin_device(WAVEOUT *p_instance) {
	/* 書き込みスレッド終了待ち */
	p_instance->enable_writer = FALSE;
	SetEvent(p_instance->write_ready);
	WaitForSingleObject(p_instance->write_thread, 1000);
	/* デバイスリセット */
	waveOutReset(p_instance->device_handle);
	for (int32_t i = 0; i < p_instance->buffer_count; ++i) {
		waveOutUnprepareHeader(
			p_instance->device_handle,
			p_instance->p_wave_hdrs + i,
			sizeof(WAVEHDR)
		);
	}
	/* 破棄 */
	parge(p_instance);
}

void
waveout_create(
	WAVEOUT **pp_instance,
	uint32_t device_id,
	uint32_t sample_rate,
	int32_t buffer_samples,
	int32_t buffer_count,
	void (*fp_writer)(float *p_output, int32_t samples)
) {
	if (NULL != *pp_instance) {
		waveout_dispose(pp_instance);
	}
	WAVEOUT *p_instance = (WAVEOUT *)malloc(sizeof(WAVEOUT));
	if (NULL == p_instance) {
		return;
	}
	*pp_instance = p_instance;
	p_instance->wave_format.nSamplesPerSec = sample_rate;
	p_instance->buffer_samples = buffer_samples;
	p_instance->buffer_count = buffer_count;
	p_instance->buffer_size = p_instance->buffer_samples * sizeof(float) * 2;
	p_instance->fp_writer = fp_writer;
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
	uint8_t playstate = p_instance->playstate;
	fin_device(p_instance);
	if (init_device(p_instance, device_id)) {
		EnterCriticalSection(&p_instance->lock_playstate);
		p_instance->playstate = playstate;
		LeaveCriticalSection(&p_instance->lock_playstate);
	} else {
		free(p_instance);
		*pp_instance = NULL;
	}
}

void
waveout_start(WAVEOUT *p_instance) {
	if (NULL == p_instance) {
		return;
	}
	EnterCriticalSection(&p_instance->lock_playstate);
	p_instance->playstate = PLAYSTATE_REQUEST_PLAY;
	LeaveCriticalSection(&p_instance->lock_playstate);
	wait_value(&p_instance->playstate, PLAYSTATE_PLAY);
}

void
waveout_pause(WAVEOUT *p_instance) {
	if (NULL == p_instance) {
		return;
	}
	EnterCriticalSection(&p_instance->lock_playstate);
	p_instance->playstate = PLAYSTATE_REQUEST_PAUSE;
	LeaveCriticalSection(&p_instance->lock_playstate);
	wait_value(&p_instance->playstate, PLAYSTATE_PAUSED);
}
