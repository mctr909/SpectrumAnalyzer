#pragma once

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
	bool Enabled = false;
	bool Playing = false;
	uint32_t DeviceId = WAVE_MAPPER;
	int32_t SampleRate = 44100;
	int32_t Channels = 2;
	int32_t BufferSamples = 44100;
	WAVEFORMATEX WaveFormatEx;

protected:
	void* mHandle = nullptr;
	LPWAVEHDR mpWaveHdr = nullptr;
	HANDLE mThread = nullptr;
	DWORD mThreadId = 0;
	CRITICAL_SECTION mBufferLock = { 0 };
	int32_t mBufferSize = 0;
	int32_t mBufferCount = 0;
	int32_t mProcessedBufferCount = 0;
	bool mStop = false;
	bool mThreadStopped = true;
	bool mPause = false;
	bool mTerminate = false;
	bool mBufferPaused = true;
	bool mEnableCallback = true;

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
	void AllocHeader();
	void DisposeHeader();
	void OpenDevice();
	void CloseDevice();
	DWORD(*mfpBufferTask)(LPVOID) = nullptr;
	void (*mfpOnTerminated)(void) = nullptr;
};
