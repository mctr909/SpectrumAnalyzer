using System;
using WinMM;

namespace SpectrumAnalyzer {
	public class Record : WaveIn {
		public Spectrum.Spectrum Spectrum;

		public Record(int sampleRate, int bufferCount = 10)
			: base(sampleRate, 2, EBufferType.FLOAT32, sampleRate / 1000 * 10, bufferCount) {
			Spectrum = new Spectrum.Spectrum(sampleRate);
		}

		public void Open() {
			OpenDevice();
		}

		public void Close() {
			CloseDevice();
		}

		protected override void ReadBuffer(IntPtr pInput) {
			Spectrum.Update(pInput, BufferSamples);
		}
	}
}
