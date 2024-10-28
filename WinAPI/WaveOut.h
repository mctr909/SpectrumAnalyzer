#pragma once
#include "Wave.h"

class WaveOut : public Wave {
public:
	bool EndOfFile = false;

private:
	void (*fpWriteBuffer)(LPSTR lpData) = nullptr;
	void (*fpOnEndOfFile)(void) = nullptr;

public:
	WaveOut(
		int32_t sampleRate,
		int32_t channels,
		EBufferType bufferType,
		int32_t bufferSamples,
		int32_t bufferCount,
		void (*fpWriteBuffer)(LPSTR lpData),
		void (*fpOnEndOfFile)(void));
	~WaveOut();

protected:
	bool InitializeTask() override;
	void FinalizeTask() override;
	void BufferTask() override;

private:
	static void Callback(HWAVEOUT hwo, WORD uMsg, WaveOut *self, LPWAVEHDR lpWaveHdr, DWORD dwParam2);
};
