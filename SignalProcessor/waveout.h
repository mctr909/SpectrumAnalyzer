#pragma once

typedef struct _WAVEOUT {
	int32_t buffer_samples;
	int32_t buffer_size;
	int32_t buffer_count;

	uint8_t enable_device;
	uint8_t enable_writer;
	uint8_t playstate;

	HWAVEOUT device_handle;
	WAVEFORMATEX wave_format;
	LPWAVEHDR p_wave_hdrs;

	CRITICAL_SECTION lock_buffer;
	CRITICAL_SECTION lock_playstate;
	HANDLE write_ready;
	HANDLE write_thread;
	uint32_t write_thread_id;
	void (*fp_writer)(float *p_output, int32_t samples);
} WAVEOUT;

void waveout_create(
	WAVEOUT **pp_instance,
	uint32_t device_id,
	uint32_t sample_rate,
	int32_t buffer_samples,
	int32_t buffer_count,
	void (*fp_writer)(float *p_output, int32_t samples)
);
void waveout_dispose(WAVEOUT **pp_instance);
void waveout_select_device(WAVEOUT **pp_instance, uint32_t device_id);
void waveout_start(WAVEOUT *p_instance);
void waveout_pause(WAVEOUT *p_instance);
