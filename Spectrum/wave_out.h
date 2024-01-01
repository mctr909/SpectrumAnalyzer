#pragma once

class WaveOut : protected Wave {
public:
	WaveOut(BUFFER_TYPE type,
		int32_t sampleRate,
		int32_t channels,
		int32_t bufferSamples,
		int32_t bufferCount);

protected:
	virtual void WriteBuffer(LPSTR lpData) { }

private:
	static void Callback(HWAVEOUT hwo, UINT uMsg, DWORD_PTR dwInstance, DWORD dwParam1, DWORD dwParam2);
	static DWORD BufferTask(LPVOID param);
};
