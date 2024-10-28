#pragma once

class RiffWav {
public:
	enum struct TYPE : uint16_t {
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

public:
	FMT Format = { };
	int32_t SampleCount = 0;

protected:
	FILE* Fp = nullptr;
	int32_t DataSize = 0;
	int32_t DataBegin = 0;
	int32_t Cursor = 0;

public:
	~RiffWav();
	void SeekCurrent(int32_t samples);
	void SeekBegin(int32_t samples);
};

class WavReader : public RiffWav {
public:
	bool IsOpened = false;
	double Position = 0.0;
	double Speed = 1.0;
	void(*fpRead)(WavReader* lpInstance, float* lpOutput) = nullptr;

private:
	int32_t OUTPUT_SAMPLES = 0;
	int32_t BUFFER_SAMPLES = 0;
	int32_t BUFFER_SIZE = 0;
	int32_t Offset = 0;
	double DELTA = 0;
	uint8_t* pBuffer = nullptr;

public:
	WavReader(wchar_t* filePath,
		int32_t sampleRate = 44100,
		int32_t outputSamples = 1024,
		double bufferUnitSec = 0.1);
	~WavReader();
	bool CheckFormat();
	void SetBuffer();

private:
	bool ReadHeader();
	static void ReadI8M(WavReader* lpThis, float* lpOutput);
	static void ReadI8S(WavReader* lpThis, float* lpOutput);
	static void ReadI16M(WavReader* lpThis, float* lpOutput);
	static void ReadI16S(WavReader* lpThis, float* lpOutput);
	static void ReadI24M(WavReader* lpThis, float* lpOutput);
	static void ReadI24S(WavReader* lpThis, float* lpOutput);
	static void ReadF32M(WavReader* lpThis, float* lpOutput);
	static void ReadF32S(WavReader* lpThis, float* lpOutput);
};

class WavWriter : public RiffWav {
public:
	WavWriter(wchar_t* filePath);
	~WavWriter();
	void Write(void* buffer, int32_t sampleCount);
};
