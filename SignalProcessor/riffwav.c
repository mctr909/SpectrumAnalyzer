#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

#include "riffwav.h"

#define FALSE 0
#define TRUE 1

const float SCALE_8BIT = 1.0f / (1 << 7);
const float SCALE_16BIT = 1.0f / (1 << 15);
const float SCALE_32BIT = 1.0f / (1 << 31);

static uint8_t
read_header(RIFFWAV *p_instance, FILE *fp) {
	RIFFCHUNK chunk;
	fread_s(&chunk, sizeof(chunk), sizeof(chunk), 1, fp);
	if (feof(fp)) {
		return FALSE;
	}
	if (RIFFSIGN_RIFF != chunk.sign) {
		return FALSE;
	}
	if (chunk.size < 4) {
		return FALSE;
	}

	uint32_t filetype;
	fread_s(&filetype, sizeof(filetype), sizeof(filetype), 1, fp);
	if (RIFFSIGN_WAVE != filetype) {
		return FALSE;
	}

	uint32_t pos = 12;
	while (1 == fread_s(&chunk, sizeof(chunk), sizeof(chunk), 1, fp)) {
		pos += sizeof(chunk);
		switch (chunk.sign) {
		case RIFFSIGN_fmt_:
			if (chunk.size < sizeof(FMT)) {
				return FALSE;
			}
			fread_s(&p_instance->format, sizeof(FMT), sizeof(FMT), 1, fp);
			if (chunk.size > sizeof(FMT)) {
				fseek(fp, chunk.size - sizeof(FMT), SEEK_CUR);
			}
			break;
		case RIFFSIGN_data:
			fseek(fp, chunk.size, SEEK_CUR);
			p_instance->data_size = chunk.size;
			p_instance->data_offset = pos;
			break;
		default:
			fseek(fp, chunk.size, SEEK_CUR);
			break;
		}
		pos += chunk.size;
	}

	p_instance->fp = fp;
	fseek(fp, p_instance->data_offset, SEEK_SET);
	return TRUE;
}

static void
load_input(RIFFWAV *p_instance, float *indexF) {
	int32_t input_position = p_instance->input_position;
	if (p_instance->position < input_position) {
		input_position -= p_instance->input_samples;
	} else {
		input_position += p_instance->input_samples;
	}

	int32_t brock_bytes = p_instance->format.block_size;
	int32_t read_samples = input_position + 1;
	int32_t read_bytes = brock_bytes * read_samples;
	int32_t remain_samples = p_instance->total_samples - input_position;

	if (remain_samples < 0 || remain_samples > p_instance->total_samples) {
		memset(p_instance->p_input, 0, read_bytes);
		*indexF = 0;
	} else {
		int32_t data_position = brock_bytes * input_position;
		fseek(p_instance->fp, p_instance->data_offset + data_position, SEEK_SET);
		if (read_samples >= remain_samples) {
			int32_t remain_bytes = brock_bytes * remain_samples;
			memset(p_instance->p_input, 0, read_bytes);
			fread_s(p_instance->p_input, read_bytes, remain_bytes, 1, p_instance->fp);
		} else {
			fread_s(p_instance->p_input, read_bytes, read_bytes, 1, p_instance->fp);
			fseek(p_instance->fp, -brock_bytes, SEEK_CUR);
		}
		p_instance->input_position = input_position;
		*indexF = p_instance->position - input_position;
	}
}

static void
read_i8m(RIFFWAV *p_instance) {
}

static void
read_i8s(RIFFWAV *p_instance) {
}

static void
read_i16m(RIFFWAV *p_instance) {
}

static void
read_i16s(RIFFWAV *p_instance) {
	int16_t *p_input = (int16_t*)p_instance->p_input;
	int16_t *p_wave;
	float *p_output = p_instance->p_output;
	float delta = p_instance->delta;
	uint32_t output_samples = p_instance->output_samples;
	uint32_t total_samples = p_instance->total_samples;
	uint32_t ix;
	for (ix = 0; ix < output_samples; ++ix) {
		if (p_instance->position < 0 || p_instance->position >= total_samples) {
			break;
		}
		float indexF = p_instance->position - p_instance->input_position;
		if (indexF >= p_instance->input_samples || indexF < 0) {
			load_input(p_instance, &indexF);
		}
		int32_t indexI = (int32_t)floorf(indexF);
		float b = (indexF - indexI) * SCALE_16BIT;
		float a = SCALE_16BIT - b;
		p_wave = p_input + indexI * 2;
		float l = *p_wave++ * a;
		float r = *p_wave++ * a;
		l += *p_wave++ * b;
		r += *p_wave * b;
		*p_output++ = l;
		*p_output++ = r;
		p_instance->position += delta * p_instance->speed;
	}
	for (; ix < output_samples; ++ix) {
		*p_output++ = 0;
		*p_output++ = 0;
	}
}

static void
read_i24m(RIFFWAV *p_instance) {
}

static void
read_i24s(RIFFWAV *p_instance) {
}

static void
read_i32m(RIFFWAV *p_instance) {
}

static void
read_i32s(RIFFWAV *p_instance) {
}

static void
read_f32m(RIFFWAV *p_instance) {
}

static void
read_f32s(RIFFWAV *p_instance) {
}

static uint8_t
set_reader(RIFFWAV *p_instance) {
	FMT *p_fmt = &p_instance->format;
	switch (p_fmt->format) {
	case WAVETYPE_PCM_INT:
		switch (p_fmt->bits_per_sample) {
		case 8:
			switch (p_fmt->channel) {
			case 1:
				p_instance->fp_reader = read_i8m;
				break;
			case 2:
				p_instance->fp_reader = read_i8s;
				break;
			default:
				return FALSE;
			}
			break;
		case 16:
			switch (p_fmt->channel) {
			case 1:
				p_instance->fp_reader = read_i16m;
				break;
			case 2:
				p_instance->fp_reader = read_i16s;
				break;
			default:
				return FALSE;
			}
			break;
		case 24:
			switch (p_fmt->channel) {
			case 1:
				p_instance->fp_reader = read_i24m;
				break;
			case 2:
				p_instance->fp_reader = read_i24s;
				break;
			default:
				return FALSE;
			}
			break;
		case 32:
			switch (p_fmt->channel) {
			case 1:
				p_instance->fp_reader = read_i32m;
				break;
			case 2:
				p_instance->fp_reader = read_i32s;
				break;
			default:
				return FALSE;
			}
			break;
		default:
			return FALSE;
		}
		break;
	case WAVETYPE_PCM_FLOAT:
		switch (p_fmt->bits_per_sample) {
		case 32:
			switch (p_fmt->channel) {
			case 1:
				p_instance->fp_reader = read_f32m;
				break;
			case 2:
				p_instance->fp_reader = read_f32s;
				break;
			default:
				return FALSE;
			}
			break;
		default:
			return FALSE;
		}
		break;
	default:
		return FALSE;
	}
	return TRUE;
}

void
riffwav_open(RIFFWAV **pp_instance, wchar_t *path, int32_t output_samples, int32_t sample_rate) {
	RIFFWAV *p_instance = *pp_instance;
	if (NULL != p_instance) {
		riffwav_dispose(pp_instance);
	}

	FILE *fp = NULL;
	_wfopen_s(&fp, path, L"rb");
	if (NULL == fp) {
		return;
	}

	p_instance = (RIFFWAV *)calloc(1, sizeof(RIFFWAV));
	if (NULL == p_instance) {
		goto purge;
	}

	if (!read_header(p_instance, fp)) {
		goto purge;
	}

	if (!set_reader(p_instance)) {
		goto purge;
	}

	p_instance->delta = (float)p_instance->format.sample_rate / sample_rate;
	p_instance->speed = 1.0f;
	p_instance->position = 0.0f;
	p_instance->total_samples = p_instance->data_size / p_instance->format.block_size;
	p_instance->input_samples = output_samples;
	p_instance->output_samples = output_samples;

	p_instance->p_output = (float*)calloc(output_samples, sizeof(float) * 2);
	if (NULL == p_instance->p_output) {
		goto purge;
	}
	p_instance->p_input = (void*)calloc(p_instance->input_samples + 1, p_instance->format.block_size);
	if (NULL == p_instance->p_input) {
		goto purge;
	}

	*pp_instance = p_instance;
	return;

purge:
	if (NULL != fp) {
		fclose(fp);
	}
	if (NULL != p_instance) {
		if (NULL != p_instance->p_output) {
			free(p_instance->p_output);
		}
		if (NULL != p_instance->p_input) {
			free(p_instance->p_input);
		}
		free(p_instance);
		p_instance = NULL;
	}
	*pp_instance = NULL;
}

void
riffwav_dispose(RIFFWAV **pp_instance) {
	RIFFWAV *p_instance = *pp_instance;
	if (NULL == p_instance) {
		return;
	}
	if (NULL != p_instance->p_output) {
		free(p_instance->p_output);
	}
	if (NULL != p_instance->fp) {
		fclose(p_instance->fp);
	}
	free(p_instance);
	*pp_instance = NULL;
}
