#pragma once

class Spectrum;

class WaveSynth {
private:
	struct Tone {
		double AmpL;
		double AmpR;
		double Phase;
	};

private:
	int32_t ToneCount = 0;
	Tone* ppTones = nullptr;
	Spectrum* cSpectrum = nullptr;

public:
	WaveSynth(Spectrum* lcSpectrum);
	~WaveSynth();
	void WriteBuffer(float* lpOutput, int32_t sampleCount);
};
