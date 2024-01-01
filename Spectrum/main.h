#pragma once

#ifndef __cplusplus
extern "C" {
#endif
	__declspec(dllexport) uint8_t* WINAPI WaveOutOpen(
		int32_t sample_rate,
		int32_t buffer_length,
		int32_t buffer_count,
		void(*fpOnTerminate)(void)
	);
	__declspec(dllexport) void WINAPI OpenFile(LPCWCHAR filePath);
#ifndef __cplusplus
}
#endif
