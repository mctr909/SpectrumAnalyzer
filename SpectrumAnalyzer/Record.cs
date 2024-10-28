using System;
using WinMM;

namespace SpectrumAnalyzer {
	public class Record : WaveIn {
		public Spectrum.Spectrum Spectrum;

		public Record(int sampleRate)
			: base(sampleRate, 2, BUFFER_TYPE.F32, sampleRate / 400, 30) {
			Spectrum = new Spectrum.Spectrum(sampleRate, SettingsForm.BASE_FREQ);
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
