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
	int64_t DataOffset = 0;
	int64_t DataSize = 0;
	int32_t OutputSampleNum = 0;
	int32_t BufferSampleNum = 0;
	int32_t BufferSize = 0;
	int32_t LoadOffset = 0;
	int32_t ReadOffset = 0;
	double Delta = 1.0;
	void* MuteData = nullptr;
	void* Buffers[2] = { nullptr, nullptr };
    void* LoadBuffer = nullptr;
	void* ReadBuffer = nullptr;

public:
	RiffWav();
	~RiffWav();
	bool Load(LPCWSTR fileName, int32_t playbackSampleRate, int32_t outputSamples, double loadUnitTime);

private:
	bool LoadHeader();
	void LoadData();
	bool CheckFormat();
	void Dispose();

	static void ReadI16S(RiffWav* self, float* output);
};
