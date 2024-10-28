#pragma once
#include <stdint.h>

class Wave {
public:
	enum class EBufferType : uint16_t {
		INT8 = 8,
		INT16 = 16,
		INT24 = 24,
		INT32 = 32,
		BITS_MASK = 255,
		FLOAT = 256,
		FLOAT32 = FLOAT | 32,
	};

public:
	int32_t SampleRate;
	int32_t Channels;
	uint32_t DeviceId;

	bool Playing = false;

protected:
	EBufferType BufferType;
	int32_t BufferSamples;
	int32_t BufferSize;
	int32_t BufferCount;
	int32_t BufferIndex;
	WAVEFORMATEX WaveFormatEx;

	HANDLE DeviceHandle = nullptr;
	LPWAVEHDR WaveHeaders = nullptr;
	void *MuteData = nullptr;
	CRITICAL_SECTION LockBuffer = { 0 };
	bool DeviceEnabled = false;
	bool CallbackEnabled = false;
	bool Closing = false;
	bool Pause = false;
	bool Paused = false;

private:
	HANDLE ThreadBuffer = nullptr;

public:
	Wave(int32_t sampleRate, int32_t channels, EBufferType bufferType, int32_t bufferSamples, int32_t bufferCount);
	~Wave();
	void SetDevice(uint32_t deviceId, bool withStart);
	void Start();
	void Stop();

protected:
	void OpenDevice();
	void CloseDevice();
	virtual bool InitializeTask() { return false; }
	virtual void FinalizeTask() {}
	virtual void BufferTask() {}
	static void WaitEnable(bool* flag);
	static void WaitDisable(bool* flag);

private:
	void AllocateHeader();
	void DisposeHeader();
	void ClearFlags();
	static DWORD Task(LPVOID* lpParam);
};
