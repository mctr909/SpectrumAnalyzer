#pragma once

__declspec(dllexport) void WINAPI player_create(
	void **pp_instance,
	uint32_t device_id,
	int32_t sample_rate,
	int32_t buffer_samples,
	int32_t buffer_count
);
__declspec(dllexport) void WINAPI player_dispose(void **pp_instance);
__declspec(dllexport) void WINAPI player_select_device(void **pp_instance, uint32_t device_id);
__declspec(dllexport) void WINAPI player_start(void *p_instance);
__declspec(dllexport) void WINAPI player_pause(void *p_instance);
