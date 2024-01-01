#include <stdint.h>
#include <Windows.h>

#include "waveout.h"
#include "spectrum.h"
#include "dllmain.h"

double re = 1.0;
double im = 0.0;

static void
writer(float *p_output, int32_t samples) {
	const double k = 6.283 * 440 / 44100.0;
	for (int32_t i = samples; i != 0; --i) {
		*p_output++ = (float)re;
		*p_output++ = (float)re;
		re -= im * k;
		im += re * k;
	}
}

void WINAPI
player_create(
	void **pp_instance,
	uint32_t device_id,
	int32_t sample_rate,
	int32_t buffer_samples,
	int32_t buffer_count
) {
	waveout_create(
		(WAVEOUT **)pp_instance,
		device_id,
		sample_rate,
		buffer_samples,
		buffer_count,
		writer
	);
}

void WINAPI
player_dispose(void **pp_instance) {
	waveout_dispose((WAVEOUT **)pp_instance);
}

void WINAPI
player_select_device(void **pp_instance, uint32_t device_id) {
	waveout_select_device((WAVEOUT **)pp_instance, device_id);
}

void WINAPI
player_start(void *p_instance) {
	waveout_start((WAVEOUT *)p_instance);
}

void WINAPI
player_pause(void *p_instance) {
	waveout_pause((WAVEOUT *)p_instance);
}
