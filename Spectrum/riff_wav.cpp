#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <math.h>

#include "riff_wav.h"

constexpr uint32_t SIGN_RIFF = 0x46464952;
constexpr uint32_t TYPE_WAVE = 0x45564157;
constexpr uint32_t SIGN_FMT_ = 0x20746d66;
constexpr uint32_t SIGN_DATA = 0x61746164;

constexpr float SCALE_8BIT = 1.0f / (1 << 7);
constexpr float SCALE_16BIT = 1.0f / (1 << 15);
constexpr float SCALE_32BIT = 1.0f / (1 << 31);

RiffWav::~RiffWav() {
	if (Fp == nullptr) {
		return;
	}
	fclose(Fp);
}
void RiffWav::SeekCurrent(int32_t samples) {
	fseek(Fp, Format.BlockSize * samples, SEEK_CUR);
	Cursor += samples;
}
void RiffWav::SeekBegin(int32_t samples) {
	fseek(Fp, DataBegin + Format.BlockSize * samples, SEEK_SET);
	Cursor = samples;
}

WavReader::WavReader(wchar_t* filePath, int32_t sampleRate, int32_t outputSamples, double bufferUnitSec) {
	_wfopen_s(&Fp, filePath, L"rb");
	if (Fp == nullptr) {
		return;
	}
	if (!ReadHeader()) {
		return;
	}
	if (!CheckFormat()) {
		return;
	}
	OUTPUT_SAMPLES = outputSamples;
	BUFFER_SAMPLES = (int32_t)(Format.SampleRate * bufferUnitSec);
	BUFFER_SIZE = Format.BlockSize * (BUFFER_SAMPLES + 1);
	DELTA = (double)Format.SampleRate / sampleRate;
	Offset = 0;
	pBuffer = (uint8_t*)calloc(BUFFER_SIZE, sizeof(uint8_t));
	SampleCount = (int32_t)(DataSize / Format.BlockSize);
	SetBuffer();
	IsOpened = true;
}
WavReader::~WavReader() {
	free(pBuffer);
	fclose(Fp);
}
bool WavReader::CheckFormat() {
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
bool WavReader::ReadHeader() {
	uint32_t fileSign;
	fread_s(&fileSign, sizeof(fileSign), sizeof(fileSign), 1, Fp);
	if (fileSign != SIGN_RIFF)
		return false;

	uint32_t fileSize;
	uint32_t fileType;
	fread_s(&fileSize, sizeof(fileSize), sizeof(fileSize), 1, Fp);
	fread_s(&fileType, sizeof(fileType), sizeof(fileType), 1, Fp);
	if (fileType != TYPE_WAVE)
		return false;

	uint32_t chunkSign;
	uint32_t chunkSize;
	uint32_t position = 12;
	while (position < fileSize) {
		fread_s(&chunkSign, sizeof(chunkSign), sizeof(chunkSign), 1, Fp);
		fread_s(&chunkSize, sizeof(chunkSize), sizeof(chunkSize), 1, Fp);
		position += 8;
		switch (chunkSign) {
		case SIGN_FMT_:
			fread_s(&Format, sizeof(Format), sizeof(Format), 1, Fp);
			if (chunkSize > sizeof(Format))
				fseek(Fp, chunkSize - sizeof(Format), SEEK_CUR);
			break;
		case SIGN_DATA:
			DataSize = chunkSize;
			DataBegin = position;
			fseek(Fp, chunkSize, SEEK_CUR);
			break;
		default:
			fseek(Fp, chunkSize, SEEK_CUR);
			break;
		}
		position += chunkSize;
	}

	fseek(Fp, DataBegin, SEEK_SET);
	return true;
}
void WavReader::SetBuffer() {
	if (SampleCount - Cursor <= BUFFER_SAMPLES) {
		auto readSamples = SampleCount - Cursor;
		memset(pBuffer, 0, BUFFER_SIZE);
		fread_s(pBuffer, BUFFER_SIZE, Format.BlockSize, readSamples, Fp);
		Cursor = SampleCount;
	}
	else {
		fread_s(pBuffer, BUFFER_SIZE, Format.BlockSize, BUFFER_SAMPLES + 1, Fp);
		fseek(Fp, -Format.BlockSize, SEEK_CUR);
		Cursor += BUFFER_SAMPLES;
	}
}
void WavReader::ReadI8M(WavReader* lpThis, float* lpOutput) {
	int32_t s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->SampleCount; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->Offset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->Offset += (int32_t)remain;
			auto sign = remain - (int32_t)remain;
			if (sign > 0)
				++lpThis->Offset;
			if (sign < 0)
				--lpThis->Offset;
			lpThis->SeekBegin(lpThis->Offset);
			lpThis->SetBuffer();
		}
		auto indexD = lpThis->Position - lpThis->Offset;
		auto indexI = (int32_t)indexD;
		auto b = (float)(indexD - indexI) * SCALE_8BIT;
		auto a = SCALE_8BIT - b;
		auto p = lpThis->pBuffer + indexI;
		*lpOutput = (*p++ - 128) * a + (*p - 128) * b;
		*lpOutput++ = *lpOutput++;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
void WavReader::ReadI8S(WavReader* lpThis, float* lpOutput) {
	int32_t s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->SampleCount; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->Offset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->Offset += (int32_t)remain;
			auto sign = remain - (int32_t)remain;
			if (sign > 0)
				++lpThis->Offset;
			if (sign < 0)
				--lpThis->Offset;
			lpThis->SeekBegin(lpThis->Offset);
			lpThis->SetBuffer();
		}
		auto indexD = lpThis->Position - lpThis->Offset;
		auto indexI = (int32_t)indexD;
		auto b = (float)(indexD - indexI) * SCALE_8BIT;
		auto a = SCALE_8BIT - b;
		auto p = lpThis->pBuffer + indexI * 2;
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
void WavReader::ReadI16M(WavReader* lpThis, float* lpOutput) {
	int32_t s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->SampleCount; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->Offset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->Offset += (int32_t)remain;
			auto sign = remain - (int32_t)remain;
			if (sign > 0)
				++lpThis->Offset;
			if (sign < 0)
				--lpThis->Offset;
			lpThis->SeekBegin(lpThis->Offset);
			lpThis->SetBuffer();
		}
		auto indexD = lpThis->Position - lpThis->Offset;
		auto indexI = (int32_t)indexD;
		auto b = (float)(indexD - indexI) * SCALE_16BIT;
		auto a = SCALE_16BIT - b;
		auto p = (int16_t*)lpThis->pBuffer + indexI;
		*lpOutput = *p++ * a + *p * b;
		*lpOutput++ = *lpOutput++;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
void WavReader::ReadI16S(WavReader* lpThis, float* lpOutput) {
	int32_t s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->SampleCount; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->Offset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->Offset += (int32_t)remain;
			auto sign = remain - (int32_t)remain;
			if (sign > 0)
				++lpThis->Offset;
			if (sign < 0)
				--lpThis->Offset;
			lpThis->SeekBegin(lpThis->Offset);
			lpThis->SetBuffer();
		}
		auto indexD = lpThis->Position - lpThis->Offset;
		auto indexI = (int32_t)indexD;
		auto b = (float)(indexD - indexI) * SCALE_16BIT;
		auto a = SCALE_16BIT - b;
		auto p = (int16_t*)lpThis->pBuffer + indexI * 2;
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
void WavReader::ReadI24M(WavReader* lpThis, float* lpOutput) {
	int32_t s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->SampleCount; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->Offset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->Offset += (int32_t)remain;
			auto sign = remain - (int32_t)remain;
			if (sign > 0)
				++lpThis->Offset;
			if (sign < 0)
				--lpThis->Offset;
			lpThis->SeekBegin(lpThis->Offset);
			lpThis->SetBuffer();
		}
		auto indexD = lpThis->Position - lpThis->Offset;
		auto indexI = (int32_t)indexD;
		auto b = (float)(indexD - indexI) * SCALE_32BIT;
		auto a = SCALE_32BIT - b;
		auto p = lpThis->pBuffer + indexI * 3;
		auto m1 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p++ << 8);
		auto m2 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p << 8);
		*lpOutput = (int32_t)m1 * a + (int32_t)m2 * b;
		*lpOutput++ = *lpOutput++;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
void WavReader::ReadI24S(WavReader* lpThis, float* lpOutput) {
	int32_t s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->SampleCount; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->Offset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->Offset += (int32_t)remain;
			auto sign = remain - (int32_t)remain;
			if (sign > 0)
				++lpThis->Offset;
			if (sign < 0)
				--lpThis->Offset;
			lpThis->SeekBegin(lpThis->Offset);
			lpThis->SetBuffer();
		}
		auto indexD = lpThis->Position - lpThis->Offset;
		auto indexI = (int32_t)indexD;
		auto b = (float)(indexD - indexI)* SCALE_32BIT;
		auto a = SCALE_32BIT - b;
		auto p = lpThis->pBuffer + indexI * 6;
		auto l1 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p++ << 8);
		auto r1 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p++ << 8);
		auto l2 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p++ << 8);
		auto r2 = ((uint32_t)*p++ << 16) | ((uint32_t)*p++ << 24) | ((uint32_t)*p << 8);
		*lpOutput++ = (int32_t)l1 * a + (int32_t)l2 * b;
		*lpOutput++ = (int32_t)r1 * a + (int32_t)r2 * b;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
void WavReader::ReadF32M(WavReader* lpThis, float* lpOutput) {
	int32_t s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->SampleCount; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->Offset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->Offset += (int32_t)remain;
			auto sign = remain - (int32_t)remain;
			if (sign > 0)
				++lpThis->Offset;
			if (sign < 0)
				--lpThis->Offset;
			lpThis->SeekBegin(lpThis->Offset);
			lpThis->SetBuffer();
		}
		auto indexD = lpThis->Position - lpThis->Offset;
		auto indexI = (int32_t)indexD;
		auto b = (float)(indexD - indexI);
		auto a = 1 - b;
		auto p = (float*)lpThis->pBuffer + indexI;
		*lpOutput = *p++ * a + *p * b;
		*lpOutput++ = *lpOutput++;
	}
	for (; s < lpThis->OUTPUT_SAMPLES; ++s) {
		*lpOutput++ = 0;
		*lpOutput++ = 0;
	}
}
void WavReader::ReadF32S(WavReader* lpThis, float* lpOutput) {
	int32_t s;
	for (s = 0; s < lpThis->OUTPUT_SAMPLES && lpThis->Position < lpThis->SampleCount; ++s, lpThis->Position += lpThis->DELTA * lpThis->Speed) {
		auto remain = lpThis->Position - lpThis->Offset;
		if (remain >= lpThis->BUFFER_SAMPLES || remain <= -1) {
			lpThis->Offset += (int32_t)remain;
			auto sign = remain - (int32_t)remain;
			if (sign > 0)
				++lpThis->Offset;
			if (sign < 0)
				--lpThis->Offset;
			lpThis->SeekBegin(lpThis->Offset);
			lpThis->SetBuffer();
		}
		auto indexD = lpThis->Position - lpThis->Offset;
		auto indexI = (int32_t)indexD;
		auto b = (float)(indexD - indexI);
		auto a = 1 - b;
		auto p = (float*)lpThis->pBuffer + indexI * 2;
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

WavWriter::WavWriter(wchar_t* filePath) {
	_wfopen_s(&Fp, filePath, L"wb");
	if (Fp == nullptr) {
		return;
	}

	uint32_t fileSize = 0;
	fwrite(&SIGN_RIFF, sizeof(SIGN_RIFF), 1, Fp);
	fwrite(&fileSize, sizeof(fileSize), 1, Fp);
	fwrite(&TYPE_WAVE, sizeof(TYPE_WAVE), 1, Fp);
	DataBegin += 12;

	uint32_t fmtSize = sizeof(Format);
	fwrite(&SIGN_FMT_, sizeof(SIGN_FMT_), 1, Fp);
	fwrite(&fmtSize, sizeof(fmtSize), 1, Fp);
	fwrite(&Format, fmtSize, 1, Fp);
	DataBegin += 8 + fmtSize;

	uint32_t dataSize = 0;
	fwrite(&SIGN_DATA, sizeof(SIGN_DATA), 1, Fp);
	fwrite(&dataSize, sizeof(dataSize), 1, Fp);
	DataBegin += 8;
}
WavWriter::~WavWriter() {
	uint32_t dataSize = Format.BlockSize * Cursor;
	uint32_t fileSize = 4 + 8 + 8 + sizeof(Format) + dataSize;

	fseek(Fp, 4, SEEK_SET);
	fwrite(&fileSize, sizeof(fileSize), 1, Fp);
	fwrite(&TYPE_WAVE, sizeof(TYPE_WAVE), 1, Fp);

	uint32_t fmtSize = 16;
	fwrite(&SIGN_FMT_, sizeof(SIGN_FMT_), 1, Fp);
	fwrite(&fmtSize, sizeof(fmtSize), 1, Fp);
	fwrite(&Format, sizeof(Format), 1, Fp);

	fwrite(&SIGN_DATA, sizeof(SIGN_DATA), 1, Fp);
	fwrite(&dataSize, sizeof(dataSize), 1, Fp);
	fclose(Fp);
}
void WavWriter::Write(void* buffer, int32_t sampleCount) {
	fwrite(buffer, Format.BlockSize * sampleCount, 1, Fp);
	Cursor += sampleCount;
}
