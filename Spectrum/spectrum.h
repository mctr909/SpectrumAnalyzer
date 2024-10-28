#pragma once

class Spectrum {
public:
	struct BPFBank {
		double KB0;
		double KA2;
		double KA1;
		double Lb2;
		double Lb1;
		double La2;
		double La1;
		double Rb2;
		double Rb1;
		double Ra2;
		double Ra1;

		double SIGMA;
		double LPower;
		double RPower;
		double SIGMA_DISP;
		double LPowerDisp;
		double RPowerDisp;

		double LPeak;
		double RPeak;
		double DELTA;
	};

public:
	static const int32_t TONE_DIV;
	static const int32_t TONE_DIV_CENTER;
	static const int32_t OCT_DIV;

public:
	int32_t TONE_COUNT = 0;
	bool AutoGain = true;
	bool NormGain = false;

	double Transpose = 0;
	double Pitch = 1.0;

	double* pPeak = nullptr;
	double* pCurve = nullptr;
	double* pThreshold = nullptr;
	BPFBank* ppBank = nullptr;

private:
	int32_t SAMPLE_RATE;
	int32_t BANK_COUNT;
	int32_t LOW_TONE;
	int32_t MID_TONE;
	double MaxL;
	double MaxR;
	double ResponceSpeed;
	void(*fpCalc)(Spectrum* lpThis, float* lpInput, int32_t sampleCount);

public:
	Spectrum(int32_t sampleRate, double baseFrequency, int32_t tones, bool stereo);
	void SetResponceSpeed(double frequency);
	void Calc(float* lpInput, int32_t sampleCount);

private:
	void SetBPF(BPFBank* lpBank, double frequency);
	double GetAlpha(int32_t sampleRate, double frequency);
	static void CalcMono(Spectrum* lpThis, float* lpInput, int32_t sampleCount);
	static void CalcStereo(Spectrum* lpThis, float* lpInput, int32_t sampleCount);
};
