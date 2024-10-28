#pragma once
#include <Windows.h>

class Wave {
public:
	enum struct BUFFER_TYPE {
		INTEGER = 0,
		I8 = INTEGER | 8,
		I16 = INTEGER | 16,
		I24 = INTEGER | 24,
		I32 = INTEGER | 32,
		BIT_MASK = 255,
		FLOAT = 256,
		F32 = FLOAT | 32,
	};

public:
	bool EnableDevice = false;
	bool Playing = false;
	uint32_t DeviceId = WAVE_MAPPER;
	int32_t BufferSamples = 44100;
	WAVEFORMATEX WaveFormatEx = { };

protected:
	HWAVE hHandle = nullptr;
	LPWAVEHDR ppWaveHdr = nullptr;
	HANDLE hThread = nullptr;
	DWORD ThreadId = 0;
	CRITICAL_SECTION BufferLock = { 0 };
	int32_t BufferSize = 0;
	int32_t BufferCount = 0;
	int32_t ProcessedBufferCount = 0;
	bool Closing = false;
	bool Pausing = false;
	bool Paused = true;
	bool Terminating = false;
	bool EnableCallback = true;

public:
	void SetDevice(uint32_t deviceId);
	void Pause();
	void Start();

protected:
	Wave(BUFFER_TYPE type, 
		int32_t sampleRate,
		int32_t channels,
		int32_t bufferSamples,
		int32_t bufferCount);
	~Wave();
	void Allocate();
	void Dispose();
	void CloseDevice();
	virtual void OpenDevice() = 0;
	DWORD(*fpBufferTask)(LPVOID) = nullptr;
	void (*fpOnTerminated)(void) = nullptr;
};

class WaveOut : public Wave {
public:
	WaveOut(BUFFER_TYPE type,
		int32_t sampleRate,
		int32_t channels,
		int32_t bufferSamples,
		int32_t bufferCount);

protected:
	void OpenDevice() override;
	virtual void WriteBuffer(void* lpData) = 0;

private:
	static void Callback(HWAVEOUT hwo, WORD uMsg, DWORD_PTR dwUser, DWORD_PTR dwParam1, DWORD_PTR dwParam2);
	static DWORD BufferTask(LPVOID lpInstance);
};
