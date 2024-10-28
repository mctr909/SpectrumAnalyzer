#include <math.h>
#include <stdlib.h>
#include "Spectrum.h"

// 半音数
constexpr auto HALFTONE_COUNT = 126;
// オクターブ分割数
constexpr auto OCT_DIV = HALFTONE_DIV * 12;
// フィルタバンク数
constexpr auto BANK_COUNT = HALFTONE_COUNT * HALFTONE_DIV;
// フィルタ帯域幅に至る周波数[Hz]
constexpr auto FREQ_AT_BANDWIDTH = 300.0;
// C0基本周波数[Hz]
double BASE_FREQ = 442 * pow(2, 3.0 / 12.0 + (1.0 / HALFTONE_DIV - 1) / 12.0 - 5);

// 中音域 開始位置[フィルタバンク数]
constexpr auto BEGIN_MID = HALFTONE_DIV * 30;
// 高音域 開始位置[フィルタバンク数]
constexpr auto BEGIN_HIGH = HALFTONE_DIV * 48;
// 低音域 閾値幅[フィルタバンク数]
constexpr auto THRESHOLD_WIDTH_LOW = HALFTONE_DIV * 11 / 3;
// 高音域 閾値幅[フィルタバンク数]
constexpr auto THRESHOLD_WIDTH_HIGH = HALFTONE_DIV * 2 / 3;

// ゲイン自動調整 最小値
constexpr auto AUTOGAIN_MIN = 1e-2;
// ゲイン自動調整 減少時間[秒]
constexpr auto AUTOGAIN_TIME_DOWN = 3.0;
// ゲイン自動調整 増加時間[秒]
constexpr auto AUTOGAIN_TIME_UP = 0.01;

#define LIMIT(v, min, max) ((v) < (min) ? (min) : (v) > (max) ? (max) : (v))

Spectrum::Spectrum(int32_t sampleRate) {
	Max = AUTOGAIN_MIN;
	AutoGain = AUTOGAIN_MIN;
	SampleRate = sampleRate;
	BankCount = BANK_COUNT;
	Banks = (BPF_BANK *)calloc(BankCount, sizeof(BPF_BANK));
	PeakBanks = (PEAK_BANK *)calloc(BankCount, sizeof(PEAK_BANK));
	Curve = (double *)calloc(BankCount, sizeof(double));
	Threshold = (double *)calloc(BankCount, sizeof(double));
	Peak = (double *)calloc(BankCount, sizeof(double));
	for (int32_t ixB = 0; ixB < BankCount; ++ixB) {
		auto frequency = BASE_FREQ * pow(2.0, (double)ixB / OCT_DIV);
		SetBPFCoef(Banks + ixB, sampleRate, frequency);
		auto peak = PeakBanks + ixB;
		peak->DELTA = frequency / sampleRate;
	}
}

Spectrum::~Spectrum() {
	free(Banks);
	free(PeakBanks);
	free(Curve);
	free(Threshold);
	free(Peak);
}

void Spectrum::Update(float *input, int32_t sampleCount) {
	CalcMeanSquare(input, sampleCount);
	UpdateAutoGain(sampleCount);
	ExtractPeak();
}

void Spectrum::CalcMeanSquare(float *input, int32_t sampleCount) const {
	float l_b2, l_b1, l_a2, l_a1;
	float r_b2, r_b1, r_a2, r_a1;
	float ms_l, ms_r;
	float b0, a0;
	float k_b0, k_a2, k_a1;
	float delta;
	for (int32_t ixB = 0; ixB < BankCount; ++ixB) {
		auto bank = Banks + ixB;
		k_b0 = bank->k_b0;
		k_a2 = bank->k_a2;
		k_a1 = bank->k_a1;
		delta = bank->delta;
		l_b2 = bank->l_b2;
		l_b1 = bank->l_b1;
		l_a2 = bank->l_a2;
		l_a1 = bank->l_a1;
		r_b2 = bank->r_b2;
		r_b1 = bank->r_b1;
		r_a2 = bank->r_a2;
		r_a1 = bank->r_a1;
		ms_l = bank->ms_l;
		ms_r = bank->ms_r;
		auto wave = (float *)input;
		for (int32_t ixS = sampleCount; ixS != 0; --ixS) {
			/*** [左チャンネル] ***/
			/* 帯域通過フィルタ */
			b0 = *wave++;
			a0 = b0 - l_b2;
			a0 *= k_b0;
			a0 -= k_a2 * l_a2;
			a0 -= k_a1 * l_a1;
			l_b2 = l_b1;
			l_b1 = b0;
			l_a2 = l_a1;
			l_a1 = a0;
			/* 振幅の二乗平均 */
			a0 *= a0;
			a0 -= ms_l;
			ms_l += a0 * delta;
			/*** [右チャンネル] ***/
			/* 帯域通過フィルタ */
			b0 = *wave++;
			a0 = b0 - r_b2;
			a0 *= k_b0;
			a0 -= k_a2 * r_a2;
			a0 -= k_a1 * r_a1;
			r_b2 = r_b1;
			r_b1 = b0;
			r_a2 = r_a1;
			r_a1 = a0;
			/* 振幅の二乗平均 */
			a0 *= a0;
			a0 -= ms_r;
			ms_r += a0 * delta;
		}
		bank->l_b2 = l_b2;
		bank->l_b1 = l_b1;
		bank->l_a2 = l_a2;
		bank->l_a1 = l_a1;
		bank->r_b2 = r_b2;
		bank->r_b1 = r_b1;
		bank->r_a2 = r_a2;
		bank->r_a1 = r_a1;
		bank->ms_l = ms_l;
		bank->ms_r = ms_r;
	}
}

void Spectrum::UpdateAutoGain(int32_t sampleCount) {
	/* 最大値を更新 */
	Max = AUTOGAIN_MIN;
	for (int32_t ixB = 0; ixB < BankCount; ++ixB) {
		auto b = Banks[ixB];
		auto amp = sqrt(fmax(b.ms_l, b.ms_r) * 2);
		Max = fmax(Max, amp);
	}

	/* 最大値に追随して自動ゲインを更新 */
	auto diff = Max - AutoGain;
	auto delta = (double)sampleCount / SampleRate;
	delta /= diff < 0 ? AUTOGAIN_TIME_DOWN : AUTOGAIN_TIME_UP;
	AutoGain += diff * delta;
	if (AutoGain < AUTOGAIN_MIN) {
		AutoGain = AUTOGAIN_MIN;
	}
}

void Spectrum::ExtractPeak() {
	for (int32_t ixB = 0; ixB < BankCount; ++ixB) {
		/*** ピーク抽出用の閾値を算出 ***/
		auto threshold_l = 0.0;
		auto threshold_r = 0.0;
		{
			/* 音域によって閾値幅を選択 */
			int32_t width;
			auto transposed = ixB + Transpose * HALFTONE_DIV;
			if (transposed < BEGIN_MID) {
				width = THRESHOLD_WIDTH_LOW;
			} else if (transposed < BEGIN_HIGH) {
				auto a2b = (double)(transposed - BEGIN_MID) / (BEGIN_HIGH - BEGIN_MID);
				width = (int)(THRESHOLD_WIDTH_HIGH * a2b + THRESHOLD_WIDTH_LOW * (1 - a2b));
			} else {
				width = THRESHOLD_WIDTH_HIGH;
			}
			/* 閾値幅で指定される範囲の最大値を閾値とする */
			/* 平均値を閾値の下限とする */
			auto ms_l = 0.0;
			auto ms_r = 0.0;
			for (int32_t ixW = -width; ixW <= width; ++ixW) {
				auto bw = ixB + ixW;
				bw = LIMIT(bw, 0, BankCount - 1);
				auto b = Banks[bw];
				ms_l += b.ms_l;
				ms_r += b.ms_r;
				threshold_l = fmax(threshold_l, b.ms_l);
				threshold_r = fmax(threshold_r, b.ms_r);
			}
			width <<= 1;
			width++;
			double ms_scale;
			if (ixB < BEGIN_HIGH) {
				ms_scale = 1.047; //+0.2db
			} else {
				ms_scale = 1.188; //+0.75db
			}
			ms_scale /= width;
			ms_l *= ms_scale;
			ms_r *= ms_scale;
			threshold_l = fmax(threshold_l, ms_l);
			threshold_r = fmax(threshold_r, ms_r);
			/* 2乗平均を振幅に変換 */
			threshold_l = sqrt(threshold_l * 2);
			threshold_r = sqrt(threshold_r * 2);
		}
		/*** 波形合成用のピークを抽出 ***/
		auto bank = Banks[ixB];
		auto amp_l = sqrt(bank.ms_l * 2);
		auto amp_r = sqrt(bank.ms_r * 2);
		auto peak = PeakBanks + ixB;
		peak->L = amp_l < threshold_l ? 0.0 : amp_l;
		peak->R = amp_r < threshold_r ? 0.0 : amp_r;
		/*** 表示用のピークを抽出、曲線と閾値を設定 ***/
		auto amp = fmax(amp_l, amp_r);
		auto threshold = fmax(threshold_l, threshold_r);
		if (EnableNormalize) {
			amp /= Max;
			threshold /= Max;
		}
		if (EnableAutoGain) {
			amp /= AutoGain;
			threshold /= AutoGain;
		}
		Curve[ixB] = amp;
		Threshold[ixB] = threshold;
		Peak[ixB] = amp < threshold ? 0.0 : amp;
	}
}

void Spectrum::SetBPFCoef(BPF_BANK *banks, int32_t sampleCount, double frequency) const {
	auto bandWidth = 1 + log2(FREQ_AT_BANDWIDTH / frequency);
	if (bandWidth < 0.666) {
		bandWidth = 0.666;
	}
	auto omega = 6.283 * frequency / SampleRate;
	auto c = cos(omega);
	auto s = sin(omega);
	auto x = log(2) / 2 * bandWidth / 12.0 * omega / s;
	auto sh = s * sinh(x);
	auto a0 = 1 + sh;
	banks->k_b0 = (float)(sh / a0);
	banks->k_a1 = (float)(-2 * c / a0);
	banks->k_a2 = (float)((1 - sh) / a0);
	banks->delta = (float)(1.0 * frequency / SampleRate);
}
