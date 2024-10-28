#pragma once
#include <cstdint>

// îºâπï™äÑêî
constexpr auto HALFTONE_DIV = 3;

class Spectrum {
private:
	struct BPF_BANK {
		float k_b0;
		float k_a2;
		float k_a1;
		float delta;
		float l_b2;
		float l_b1;
		float l_a2;
		float l_a1;
		float r_b2;
		float r_b1;
		float r_a2;
		float r_a1;
		float ms_l;
		float ms_r;
	};

public:
	struct PEAK_BANK {
		double DELTA;
		double L;
		double R;
	};

public:
	int32_t BankCount;
	PEAK_BANK *PeakBanks;
	double *Curve;
	double *Threshold;
	double *Peak;
	double Max;
	double AutoGain;
	double Transpose = 0;
	double Pitch = 1;
	bool EnableAutoGain = true;
	bool EnableNormalize = false;

private:
	int32_t SampleRate;
	BPF_BANK *Banks;

public:
	Spectrum(int32_t sampleRate);
	~Spectrum();
	void Update(float *input, int32_t sampleCount);

private:
	void SetBPFCoef(BPF_BANK *banks, int32_t sampleCount, double frequency) const;
	void CalcMeanSquare(float *input, int32_t sampleCount) const;
	void UpdateAutoGain(int32_t sampleCount);
	void ExtractPeak();
};
