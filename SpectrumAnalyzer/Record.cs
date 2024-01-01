using System;
using WinMM;
using SignalProcess;

namespace SpectrumAnalyzer {
	public class Record : WaveIn {
		public BaseSpectrum Spectrum;

		public Record(int sampleRate, double unitTime, int unitCount, DNotify notify = null) : base(sampleRate, unitTime, unitCount, notify) {
			Spectrum = new IIRSpectrum(sampleRate);
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
