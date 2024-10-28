#pragma once
#include <mmsystem.h>
#include "Wave.h"

class WaveOut : public Wave {
private:
	int32_t ProcessInterval = 0;
	bool EndOfFile = false;
	void (*fpWriteBuffer)(LPSTR lpData) = nullptr;

public:
	WaveOut(int32_t sampleRate, int32_t channels, EBufferType bufferType, int32_t bufferSamples, int32_t bufferCount, void (*fpWriteBuffer)(LPSTR lpData));
	~WaveOut();

protected:
	bool InitializeTask() override;
	void FinalizeTask() override;
	void BufferTask() override;

private:
	static void Callback(HWAVEOUT hwo, WORD uMsg, DWORD_PTR dwInstance, LPWAVEHDR lpWaveHdr, DWORD dwParam2);
};
