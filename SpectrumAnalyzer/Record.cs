using System;
using WinMM;

namespace SpectrumAnalyzer {
	public class Record : WaveIn {
		const int DIV_COUNT = 10;
		readonly int DIV_SAMPLES;
		readonly int DIV_SIZE;

		public Spectrum.Spectrum Spectrum;

		public Record(int sampleRate, int bufferCount = 20)
			: base(sampleRate, 2, VALUE_TYPE.F32, sampleRate / 1000 * DIV_COUNT, bufferCount) {
			DIV_SAMPLES = BufferSamples / DIV_COUNT;
			DIV_SIZE = WaveFormatEx.nBlockAlign * DIV_SAMPLES;
			Spectrum = new Spectrum.Spectrum(sampleRate);
		}

		public void Open() {
			OpenDevice();
		}

		public void Close() {
			CloseDevice();
		}

		protected override void ReadBuffer(IntPtr pInput) {
			var pDivBuffer = pInput;
			for (int d = 0; d < DIV_COUNT; ++d) {
				Spectrum.Update(pDivBuffer, DIV_SAMPLES);
				pDivBuffer += DIV_SIZE;
			}
		}
	}
}
