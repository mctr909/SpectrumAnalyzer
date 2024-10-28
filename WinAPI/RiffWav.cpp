#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <stdint.h>
#include "RiffWav.h"

constexpr auto SCALE_8BIT = 1.0f / (1 << 7);;
constexpr auto SCALE_16BIT = 1.0f / (1 << 15);;
constexpr auto SCALE_32BIT = 1.0f / (1 << 31);;

constexpr uint32_t SIGN_RIFF = 0x46464952;
constexpr uint32_t SIGN_WAVE = 0x45564157;
constexpr uint32_t SIGN_fmt_ = 0x20746d66;
constexpr uint32_t SIGN_data = 0x61746164;

RiffWav::RiffWav() { }

RiffWav::~RiffWav() {
	Dispose();
}

bool RiffWav::Load(const wchar_t* fileName, int32_t playbackSampleRate, int32_t outputSamples, double loadUnitTime) {
	IsOpened = false;
	Dispose();
	_wfopen_s(&fp, fileName, L"rb");
	if (nullptr == fp) {
		return false;
	}
	if (!LoadHeader()) {
		fclose(fp);
		return false;
	}
	if (!CheckFormat()) {
		fclose(fp);
		return false;
	}
	SampleNum = (int32_t)(DataSize / Format.BlockSize);
	OutputSampleNum = outputSamples;
	BufferSampleNum = (int32_t)(Format.SampleRate * loadUnitTime);
	BufferSize = Format.BlockSize * (BufferSampleNum + 1);
	Delta = (double)Format.SampleRate / playbackSampleRate;
	MuteData = malloc(BufferSize);
	if (Format.BitsPerSample == 8) {
		memset(MuteData, 128, BufferSize);
	} else {
		memset(MuteData, 0, BufferSize);
	}
	Buffer = malloc(BufferSize);
	Offset = 0;
	LoadData();
	IsOpened = true;
	return true;
}

bool RiffWav::LoadHeader() {
	uint32_t fileSign;
	uint32_t fileSize;
	uint32_t fileType;
	uint32_t chunkSign;
	uint32_t chunkSize;
	uint32_t currentPosition;

	fread_s(&fileSign, sizeof(fileSign), sizeof(fileSign), 1, fp);
	fread_s(&fileSize, sizeof(fileSize), sizeof(fileSize), 1, fp);
	fread_s(&fileType, sizeof(fileType), sizeof(fileType), 1, fp);
	if (!(fileSign == SIGN_RIFF && fileType == SIGN_WAVE)) {
		return false;
	}

	currentPosition = sizeof(fileSign);
	currentPosition += sizeof(fileSize);
	currentPosition += sizeof(fileType);
	fileSize += sizeof(fileSign);
	fileSize += sizeof(fileSize);
	while (currentPosition < fileSize && !feof(fp)) {
		fread_s(&chunkSign, sizeof(chunkSign), sizeof(chunkSign), 1, fp);
		fread_s(&chunkSize, sizeof(chunkSize), sizeof(chunkSize), 1, fp);
		currentPosition += sizeof(chunkSign);
		currentPosition += sizeof(chunkSize);
		switch (chunkSign) {
		case SIGN_fmt_:
			if (chunkSize < sizeof(Format)) {
				return false;
			}
			fread_s(&Format, sizeof(Format), sizeof(Format), 1, fp);
			if (chunkSize > sizeof(Format)) {
				fseek(fp, chunkSize - sizeof(Format), SEEK_CUR);
			}
			break;
		case SIGN_data:
			DataOffset = currentPosition;
			DataSize = chunkSize;
			fseek(fp, chunkSize, SEEK_CUR);
			break;
		default:
			fseek(fp, chunkSize, SEEK_CUR);
			break;
		}
		currentPosition += chunkSize;
	}

	fseek(fp, DataOffset, SEEK_SET);
	return true;
}

void RiffWav::LoadData() {
	auto diffD = Position - Offset;
	auto diffI = (int32_t)diffD;
	diffI += _dsign(diffD - diffI);
	Offset += diffI;
	if (Offset > SampleNum) {
		Offset = SampleNum;
	}
	fseek(fp, DataOffset + Format.BlockSize * Offset, SEEK_SET);
	auto remainSampleNum = SampleNum - Offset;
	if (remainSampleNum <= BufferSampleNum) {
		memcpy_s(Buffer, BufferSize, MuteData, BufferSize);
		fread_s(Buffer, BufferSize, Format.BlockSize, remainSampleNum, fp);
		Offset = SampleNum;
	} else {
		fread_s(Buffer, BufferSize, BufferSize, 1, fp);
		fseek(fp, -Format.BlockSize, SEEK_CUR);
	}
}

bool RiffWav::CheckFormat() {
	switch (Format.FormatId) {
	case 1:
		switch (Format.BitsPerSample) {
		case 8:
			switch (Format.Channel) {
			case 1:
				//fpRead = ReadI8M;
				break;
			case 2:
				//fpRead = ReadI8S;
				break;
			default:
				return false;
			}
			break;
		case 16:
			switch (Format.Channel) {
			case 1:
				//fpRead = ReadI16M;
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
				//fpRead = ReadI24M;
				break;
			case 2:
				//fpRead = ReadI24S;
				break;
			default:
				return false;
			}
			break;
		default:
			return false;
		}
		break;
	case 3:
		switch (Format.BitsPerSample) {
		case 32:
			switch (Format.Channel) {
			case 1:
				//fpRead = ReadF32M;
				break;
			case 2:
				//fpRead = ReadF32S;
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

void RiffWav::Dispose() {
	if (nullptr != fp) {
		fclose(fp);
		fp = nullptr;
	}
	if (nullptr != MuteData) {
		free(MuteData);
		MuteData = nullptr;
	}
	if (nullptr != Buffer) {
		free(Buffer);
		Buffer = nullptr;
	}
}

void RiffWav::ReadI16S(RiffWav* self, float* output) {
	int32_t s;
	for (s = 0; s < self->OutputSampleNum && self->Position < self->SampleNum; ++s, self->Position += self->Delta * self->Speed) {
		auto diff = self->Position - self->Offset;
		if (diff >= self->BufferSampleNum || diff < 0) {
			self->LoadData();
		}
		auto indexD = self->Position - self->Offset;
		auto indexI = (int32_t)indexD;
		auto b = (float)(indexD - indexI) * SCALE_16BIT;
		auto a = SCALE_16BIT - b;
		auto buffer = (int16_t*)self->Buffer;
		buffer += indexI << 1;
		auto l = *buffer++ * a;
		auto r = *buffer++ * a;
		l += *buffer++ * b;
		r += *buffer * b;
		*output++ = l;
		*output++ = r;
	}
	for (; s < self->OutputSampleNum; ++s) {
		*output++ = 0;
		*output++ = 0;
	}
}