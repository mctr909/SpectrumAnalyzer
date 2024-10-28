#pragma once

#ifdef __cplusplus
extern "C" {
#endif
	__declspec(dllexport) void WINAPI OutputOpen(
		Playback** hInstance,
		int32_t sampleRate,
		void(*fpOnOpened)(bool),
		void(*fpOnTerminate)(void)
	);
	__declspec(dllexport) void WINAPI OutputClose(Playback** hInstance);
	__declspec(dllexport) void WINAPI OutputStart(Playback* hInstance);
	__declspec(dllexport) void WINAPI OutputPause(Playback* hInstance);
	__declspec(dllexport) void WINAPI SetPlaybackFile(Playback* hInstance, LPCWCHAR filePath);
	__declspec(dllexport) void WINAPI SetSpeed(Playback* hInstance, double speed);
	__declspec(dllexport) double WINAPI GetSpeed(Playback* hInstance);
	__declspec(dllexport) void WINAPI SetTranspose(Playback* hInstance, int32_t trancepose);
	__declspec(dllexport) int32_t WINAPI GetTranspose(Playback* hInstance);
#ifdef __cplusplus
}
#endif
