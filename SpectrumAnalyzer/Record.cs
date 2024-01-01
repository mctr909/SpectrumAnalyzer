using System;
using WinMM;

namespace SpectrumAnalyzer {
	public class Record : WaveIn {
		public Spectrum Spectrum;

		public Record(int sampleRate)
			: base(sampleRate, 1, BUFFER_TYPE.F32, sampleRate / 400, 30) {
			Spectrum = new Spectrum(sampleRate, Settings.BASE_FREQ, Settings.NOTE_COUNT, false);
		}

		public void Open() {
			OpenDevice();
		}

		public void Close() {
			CloseDevice();
		}

		protected unsafe override void ReadBuffer(IntPtr pInput) {
			Spectrum.Calc((float*)pInput, BufferSamples);
		}
	}
}
