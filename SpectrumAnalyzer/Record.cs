using System;
using WINMM;

public class Record : WaveIn {
	public Spectrum FilterBank;

	public Record(int sampleRate, int bufferSize, int notes, double baseFreq) : base(sampleRate, 1, bufferSize) {
		FilterBank = new Spectrum(sampleRate, baseFreq, notes, BufferSamples, false);
	}

	protected override void ReadBuffer(IntPtr pInput) {
		FilterBank.SetLevel(pInput, BufferSamples);
	}
}
