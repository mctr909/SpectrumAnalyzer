#pragma once

class WavReader;
class Spectrum;
class WaveSynth;

class Playback : public WaveOut {
public:
	WavReader* cFile = nullptr;
	Spectrum* cSpectrum = nullptr;
	WaveSynth* cWaveSynth = nullptr;

private:
	int32_t DIV_SAMPLES;
	int32_t DIV_SIZE;
	void (*fpOnOpened)(bool) = nullptr;

public:
	Playback(int32_t sampleRate,
		void(*fpOnOpened)(bool),
		void(*fpOnTerminated)(void));
	void Open();
	void Close();
	void OpenFile(wchar_t* filePath);

protected:
	void WriteBuffer(void* lpData) override;
};
