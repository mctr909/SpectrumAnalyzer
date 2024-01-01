#pragma once

class RiffWav {
public:
	enum struct TYPE {
		PCM_INT = 1,
		PCM_FLOAT = 3
	};
	struct FMT {
		TYPE FormatID;
		uint16_t Channel;
		uint32_t SampleRate;
		uint32_t BytesPerSecond;
		uint16_t BlockSize;
		uint16_t BitsPerSample;
	};

protected:
	FILE* mFp = nullptr;
	int32_t mDataSize = 0;
	int32_t mDataBegin = 0;

public:
	FMT Format = { };
	int32_t Length = 0;
	int32_t Cursor = 0;

public:
	~RiffWav();
	void SeekCurrent(int samples);
	void SeekBegin(int samples);
};

class WavReader : public RiffWav {
public:
	int OUTPUT_SAMPLES;
	int BUFFER_SAMPLES;
	int BUFFER_SIZE;
	double DELTA;
	void* mBuffer = nullptr;
	int mOffset = 0;

public:
	bool IsOpened = false;
	double Position = 0;
	double Speed = 1.0;
	void(*fpRead)(WavReader* lpInstance, float* lpOutput);

public:
	WavReader(LPCWCHAR filePath,
		int32_t sampleRate = 44100,
		int32_t outputSamples = 1024,
		double bufferUnitSec = 0.1);
	~WavReader();
	bool CheckFormat();
	void SetBuffer();

private:
	bool ReadHeader();
};
