#pragma once

class WavReader;

class Playback : protected WaveOut {
public:
	int32_t DIV_SAMPLES;
	int32_t DIV_SIZE;
	WavReader* File = nullptr;

public:
	Playback(int sampleRate, void(*fpOnTerminated)(void));
	void Open();
	void Close();
	void OpenFile(LPCWCHAR filePath);

protected:
	void WriteBuffer(LPSTR lpData);
};
