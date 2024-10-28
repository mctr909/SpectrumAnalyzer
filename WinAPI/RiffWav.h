#pragma once
class RiffWav {
public:
#pragma pack(push, 8)
	struct FMT {
		uint16_t FormatId;
		uint16_t Channel;
		uint32_t SampleRate;
		uint32_t BytesPerSecond;
		uint16_t BlockSize;
		uint16_t BitsPerSample;
	};
#pragma pack(pop)

public:
	FMT Format = {};
	int32_t SampleNum = 0;
	double Position = 0;
	double Speed = 1.0;
	bool IsOpened = false;
	void (*fpRead)(RiffWav*, float*) = nullptr;

private:
	FILE* fp = nullptr;
	uint32_t DataOffset = 0;
	uint32_t DataSize = 0;
	int32_t OutputSampleNum = 0;
	int32_t BufferSampleNum = 0;
	int32_t BufferSize = 0;
	int32_t Offset = 0;
	double Delta = 1.0;
	void* MuteData = nullptr;
	void* Buffer = nullptr;

public:
	RiffWav();
	~RiffWav();
	bool Load(const wchar_t* fileName, int32_t playbackSampleRate, int32_t outputSamples, double loadUnitTime);

private:
	bool LoadHeader();
	void LoadData();
	bool CheckFormat();
	void Dispose();

	static void ReadI16S(RiffWav* self, float* output);
};
