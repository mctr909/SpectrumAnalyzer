#pragma once
#include <cstdint>

class Spectrum;

class WaveSynth {
private:
	struct OSC_BANK {
		double delta;
		double phase;
		double amp_l;
		double amp_r;
		double declicked_l;
		double declicked_r;
	};

private:
	Spectrum *clSpectrum;
	OSC_BANK *Banks;
	int32_t BankCount;

public:
	WaveSynth(Spectrum *spectrum);
	~WaveSynth();
	void WriteBuffer(float *output, int32_t sampleCount);

private:
	void SetParameter();
	void DoWaveSynth(float *output, int32_t sampleCount);
};
