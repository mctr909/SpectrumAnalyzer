#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>

#include <Windows.h>

#include "riff_wav.h"

constexpr uint32_t SIGN_RIFF = 0x46464952;
constexpr uint32_t TYPE_WAVE = 0x45564157;
constexpr uint32_t SIGN_FMT_ = 0x20746d66;
constexpr uint32_t SIGN_DATA = 0x61746164;

constexpr float SCALE_8BIT = 1.0f / (1 << 7);
constexpr float SCALE_16BIT = 1.0f / (1 << 15);
constexpr float SCALE_32BIT = 1.0f / (1 << 31);

static void ReadI8M(WavReader* lpThis, float* lpOutput) {
	auto pInput = (byte*)lpThis->mBuffer;
	int s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->Length; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->mOffset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->mOffset += (int)remain;
			auto sign = remain - (int)remain;
			if (sign > 0)
				++lpThis->mOffset;
			if (sign < 0)
				--lpThis->mOffset;
			lpThis->SeekBegin(lpThis->mOffset);
			lpThis->SetBuffer();
		}
		auto indexF = (float)(lpThis->Position - lpThis->mOffset);
		auto indexI = (int)indexF;
		auto b = (indexF - indexI) * SCALE_8BIT;
		auto a = SCALE_8BIT - b;
		auto p = pInput + indexI;
		*lpOutput = (*p++ - 128) * a + (*p - 128) * b;
		*lpOutput++ = *lpOutput++;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
static void ReadI8S(WavReader* lpThis, float* lpOutput) {
	auto pInput = (byte*)lpThis->mBuffer;
	int s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->Length; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->mOffset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->mOffset += (int)remain;
			auto sign = remain - (int)remain;
			if (sign > 0)
				++lpThis->mOffset;
			if (sign < 0)
				--lpThis->mOffset;
			lpThis->SeekBegin(lpThis->mOffset);
			lpThis->SetBuffer();
		}
		auto indexF = (float)(lpThis->Position - lpThis->mOffset);
		auto indexI = (int)indexF;
		auto b = (indexF - indexI) * SCALE_8BIT;
		auto a = SCALE_8BIT - b;
		auto p = pInput + (indexI << 1);
		auto l = (*p++ - 128) * a;
		auto r = (*p++ - 128) * a;
		l += (*p++ - 128) * b;
		r += (*p - 128) * b;
		*lpOutput++ = l;
		*lpOutput++ = r;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
static void ReadI16M(WavReader* lpThis, float* lpOutput) {
	auto pInput = (byte*)lpThis->mBuffer;
	int s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->Length; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->mOffset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->mOffset += (int)remain;
			auto sign = remain - (int)remain;
			if (sign > 0)
				++lpThis->mOffset;
			if (sign < 0)
				--lpThis->mOffset;
			lpThis->SeekBegin(lpThis->mOffset);
			lpThis->SetBuffer();
		}
		auto indexF = (float)(lpThis->Position - lpThis->mOffset);
		auto indexI = (int)indexF;
		auto b = (indexF - indexI) * SCALE_16BIT;
		auto a = SCALE_16BIT - b;
		auto p = pInput + indexI;
		*lpOutput = *p++ * a + *p * b;
		*lpOutput++ = *lpOutput++;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
static void ReadI16S(WavReader* lpThis, float* lpOutput) {
	auto pInput = (byte*)lpThis->mBuffer;
	int s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->Length; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->mOffset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->mOffset += (int)remain;
			auto sign = remain - (int)remain;
			if (sign > 0)
				++lpThis->mOffset;
			if (sign < 0)
				--lpThis->mOffset;
			lpThis->SeekBegin(lpThis->mOffset);
			lpThis->SetBuffer();
		}
		auto indexF = (float)(lpThis->Position - lpThis->mOffset);
		auto indexI = (int)indexF;
		auto b = (indexF - indexI) * SCALE_16BIT;
		auto a = SCALE_16BIT - b;
		auto p = pInput + (indexI << 1);
		auto l = *p++ * a;
		auto r = *p++ * a;
		l += *p++ * b;
		r += *p * b;
		*lpOutput++ = l;
		*lpOutput++ = r;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
static void ReadI24M(WavReader* lpThis, float* lpOutput) {
	auto pInput = (byte*)lpThis->mBuffer;
	int s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->Length; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->mOffset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->mOffset += (int)remain;
			auto sign = remain - (int)remain;
			if (sign > 0)
				++lpThis->mOffset;
			if (sign < 0)
				--lpThis->mOffset;
			lpThis->SeekBegin(lpThis->mOffset);
			lpThis->SetBuffer();
		}
		auto indexF = (float)(lpThis->Position - lpThis->mOffset);
		auto indexI = (int)indexF;
		auto b = (indexF - indexI) * SCALE_32BIT;
		auto a = SCALE_32BIT - b;
		auto p = pInput + indexI * 3;
		auto m1 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p++ << 8);
		auto m2 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p << 8);
		*lpOutput = (int)m1 * a + (int)m2 * b;
		*lpOutput++ = *lpOutput++;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
static void ReadI24S(WavReader* lpThis, float* lpOutput) {
	auto pInput = (byte*)lpThis->mBuffer;
	int s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->Length; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->mOffset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->mOffset += (int)remain;
			auto sign = remain - (int)remain;
			if (sign > 0)
				++lpThis->mOffset;
			if (sign < 0)
				--lpThis->mOffset;
			lpThis->SeekBegin(lpThis->mOffset);
			lpThis->SetBuffer();
		}
		auto indexF = (float)(lpThis->Position - lpThis->mOffset);
		auto indexI = (int)indexF;
		auto b = (indexF - indexI) * SCALE_32BIT;
		auto a = SCALE_32BIT - b;
		auto p = pInput + (indexI * 6);
		auto l1 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p++ << 8);
		auto r1 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p++ << 8);
		auto l2 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p++ << 8);
		auto r2 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p << 8);
		*lpOutput++ = (int)l1 * a + (int)l2 * b;
		*lpOutput++ = (int)r1 * a + (int)r2 * b;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
static void ReadF32M(WavReader* lpThis, float* lpOutput) {
	auto pInput = (byte*)lpThis->mBuffer;
	int s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->Length; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->mOffset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->mOffset += (int)remain;
			auto sign = remain - (int)remain;
			if (sign > 0)
				++lpThis->mOffset;
			if (sign < 0)
				--lpThis->mOffset;
			lpThis->SeekBegin(lpThis->mOffset);
			lpThis->SetBuffer();
		}
		auto indexF = (float)(lpThis->Position - lpThis->mOffset);
		auto indexI = (int)indexF;
		auto b = indexF - indexI;
		auto a = 1 - b;
		auto p = pInput + indexI;
		*lpOutput = *p++ * a + *p * b;
		*lpOutput++ = *lpOutput++;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
static void ReadF32S(WavReader* lpThis, float* lpOutput) {
	auto pInput = (byte*)lpThis->mBuffer;
	int s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->Length; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->mOffset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->mOffset += (int)remain;
			auto sign = remain - (int)remain;
			if (sign > 0)
				++lpThis->mOffset;
			if (sign < 0)
				--lpThis->mOffset;
			lpThis->SeekBegin(lpThis->mOffset);
			lpThis->SetBuffer();
		}
		auto indexF = (float)(lpThis->Position - lpThis->mOffset);
		auto indexI = (int)indexF;
		auto b = indexF - indexI;
		auto a = 1 - b;
		auto p = pInput + (indexI << 1);
		auto l = *p++ * a;
		auto r = *p++ * a;
		l += *p++ * b;
		r += *p * b;
		*lpOutput++ = l;
		*lpOutput++ = r;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}

RiffWav::~RiffWav() {
	if (mFp == nullptr) {
		return;
	}
	fclose(mFp);
	mFp = nullptr;
}

void RiffWav::SeekCurrent(int samples) {
	fseek(mFp, Format.BlockSize * samples, SEEK_CUR);
	Cursor += samples;
}

void RiffWav::SeekBegin(int samples) {
	fseek(mFp, mDataBegin + Format.BlockSize * samples, SEEK_SET);
	Cursor = samples;
}

WavReader::WavReader(LPCWCHAR filePath, int32_t sampleRate, int32_t outputSamples, double bufferUnitSec) {
	IsOpened = false;
	fopen_s(&mFp, (char*)filePath, "fr");
	if (mFp == nullptr) {
		return;
	}
	if (!ReadHeader()) {
		return;
	}
	if (!CheckFormat()) {
		return;
	}
	Length = (int)(mDataSize / Format.BlockSize);
	OUTPUT_SAMPLES = outputSamples;
	BUFFER_SAMPLES = (int)(Format.SampleRate * bufferUnitSec);
	BUFFER_SIZE = Format.BlockSize * (BUFFER_SAMPLES + 1);
	DELTA = (double)Format.SampleRate / sampleRate;
	mBuffer = calloc(1, BUFFER_SIZE);
	mOffset = 0;
	SetBuffer();
	IsOpened = true;
}

WavReader::~WavReader() {
	free(mBuffer);
}

bool
WavReader::CheckFormat() {
	switch (Format.FormatID) {
	case TYPE::PCM_INT:
		switch (Format.BitsPerSample) {
		case 8:
			switch (Format.Channel) {
			case 1:
				fpRead = ReadI8M;
				break;
			case 2:
				fpRead = ReadI8S;
				break;
			default:
				return false;
			}
			break;
		case 16:
			switch (Format.Channel) {
			case 1:
				fpRead = ReadI16M;
				break;
			case 2:
				fpRead = ReadI16S;
				break;
			default:
				return false;
			}
			break;
		case 24:
			switch (Format.Channel) {
			case 1:
				fpRead = ReadI24M;
				break;
			case 2:
				fpRead = ReadI24S;
				break;
			default:
				return false;
			}
			break;
		default:
			return false;
		}
		break;
	case TYPE::PCM_FLOAT:
		switch (Format.BitsPerSample) {
		case 32:
			switch (Format.Channel) {
			case 1:
				fpRead = ReadF32M;
				break;
			case 2:
				fpRead = ReadF32S;
				break;
			default:
				return false;
			}
			break;
		default:
			return false;
		}
		break;
	default:
		return false;
	}

	return true;
}

bool
WavReader::ReadHeader() {
	uint32_t fileSign;
	fread_s(&fileSign, sizeof(fileSign), sizeof(fileSign), 1, mFp);
	if (fileSign != SIGN_RIFF)
		return false;

	uint32_t fileSize;
	uint32_t fileType;
	fread_s(&fileSize, sizeof(fileSize), sizeof(fileSize), 1, mFp);
	fread_s(&fileType, sizeof(fileType), sizeof(fileType), 1, mFp);
	if (fileType != TYPE_WAVE)
		return false;

	uint32_t chunkSign;
	uint32_t chunkSize;
	uint32_t position = 12;
	while (position < fileSize) {
		fread_s(&chunkSign, sizeof(chunkSign), sizeof(chunkSign), 1, mFp);
		fread_s(&chunkSize, sizeof(chunkSize), sizeof(chunkSize), 1, mFp);
		position += 8;
		switch (chunkSign) {
		case SIGN_FMT_:
			fread_s(&Format, sizeof(Format), sizeof(Format), 1, mFp);
			if (chunkSize > sizeof(Format))
				fseek(mFp, chunkSize - sizeof(Format), SEEK_CUR);
			break;
		case SIGN_DATA:
			mDataSize = chunkSize;
			mDataBegin = position;
			fseek(mFp, chunkSize, SEEK_CUR);
			break;
		default:
			fseek(mFp, chunkSize, SEEK_CUR);
			break;
		}
		position += chunkSize;
	}

	fseek(mFp, mDataBegin, SEEK_SET);
	return true;
}

void
WavReader::SetBuffer() {
	if (Length - Cursor <= BUFFER_SAMPLES) {
		auto readSize = (Length - Cursor) * Format.BlockSize;
		memset(mBuffer, 0, BUFFER_SIZE);
		fread_s(mBuffer, BUFFER_SIZE, readSize, 1, mFp);
		Cursor = Length;
	}
	else {
		fread_s(mBuffer, BUFFER_SIZE, BUFFER_SIZE, 1, mFp);
		fseek(mFp, -Format.BlockSize, SEEK_CUR);
		Cursor += BUFFER_SAMPLES;
	}
}
