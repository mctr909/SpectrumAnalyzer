using System;
using WinMM;

namespace SpectrumAnalyzer {
	public class Record : WaveIn {
		public Spectrum FilterBank;

		public Record(int sampleRate)
			: base(sampleRate, 1, BUFFER_TYPE.F32, sampleRate / 400, 30) {
			FilterBank = new Spectrum(sampleRate, Settings.BASE_FREQ, Settings.NOTE_COUNT, false);
		}

		public void Open() {
			OpenDevice();
		}

		public void Close() {
			CloseDevice();
		}

		protected override void ReadBuffer(IntPtr pInput) {
			FilterBank.SetValue(pInput, BufferSamples);
		}
	}
}
