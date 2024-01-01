using System;
using WinMM;
using SignalProcess;

namespace SpectrumAnalyzer {
	public class Record : WaveIn {
		public Spectrum Spectrum;

		public Record(int sampleRate, double unitTime, int unitCount) : base(sampleRate, unitTime, unitCount) {
			Spectrum = new Spectrum(sampleRate);
		}

		public void Open() {
			OpenDevice();
		}

		public void Close() {
			CloseDevice();
		}

		protected override void ReadBuffer(IntPtr pInput) {
			Spectrum.Update(pInput, BufferFrames);
		}
	}
}
