using System;
using System.Threading.Tasks;
using WINMM;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		public delegate void DTerminated();

		public WavReader File = new WavReader();
		public Spectrum FilterBank;

		OscBank mOscBank;
		DTerminated mOnTerminated;

		public Playback(int sampleRate, DTerminated onTerminated = null)
			: base(sampleRate, 2, BUFFER_TYPE.F32, sampleRate / 800, 120) {
			mOscBank = new OscBank(BufferSamples, Settings.NOTE_COUNT);
			FilterBank = new Spectrum(sampleRate, Settings.BASE_FREQ, Settings.NOTE_COUNT, BufferSamples, true);
			if (null == onTerminated) {
				mOnTerminated = () => {
					File.Position = 0;
					Start();
				};
			} else {
				mOnTerminated = onTerminated;
			}
		}

		public void OpenFile(string filePath) {
			Pause();
			File.Dispose();
			File = new WavReader(filePath, SampleRate, BufferSamples, 0.1);
		}

		protected override void WriteBuffer(IntPtr pBuffer) {
			File.Read(pBuffer);
			FilterBank.SetValue(pBuffer, BufferSamples);
			mOscBank.SetWave(FilterBank, pBuffer);
			if (File.Position >= File.Length) {
				new Task(() => {
					Pause();
					mOnTerminated();
				}).Start();
			}
		}
	}
}
