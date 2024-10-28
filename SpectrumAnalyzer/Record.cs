using System;
using WinMM;

namespace SpectrumAnalyzer {
	public class Record : WaveIn {
		public Spectrum.Spectrum Spectrum;

		public Record(int sampleRate, double calcUnitTime, int divCount) : base(
			sampleRate, 2, (int)(sampleRate * calcUnitTime) * divCount, divCount * 4
		) {
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
