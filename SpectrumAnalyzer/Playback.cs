using System;
using System.Runtime.InteropServices;
using WinMM;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		public WavReader File = new WavReader();
		public Spectrum Spectrum;
		public OscBank Osc;

		public delegate void DOpened(bool isOpened);

		const int DIV = 32;
		readonly int DIV_SAMPLES;
		readonly int DIV_SIZE;

		DOpened mOnOpened;
		float[] mMuteData;

		public Playback(int sampleRate, DOpened onOpened, DTerminated onTerminated)
			: base(sampleRate, 2, BUFFER_TYPE.F32, sampleRate / 1000 * DIV, 6) {
			DIV_SAMPLES = BufferSamples / DIV;
			DIV_SIZE = WaveFormatEx.nBlockAlign * DIV_SAMPLES;
			mOnOpened = onOpened;
			mOnTerminated = onTerminated;
			mMuteData = new float[DIV_SAMPLES * 2];
			Spectrum = new Spectrum(sampleRate, Settings.BASE_FREQ, Settings.NOTE_COUNT, true);
			Osc = new OscBank(Settings.NOTE_COUNT, Spectrum);
		}

		public void Open() {
			OpenDevice();
		}

		public void Close() {
			CloseDevice();
		}

		public void OpenFile(string filePath) {
			Pause();
			File.Dispose();
			File = new WavReader(filePath, SampleRate, BufferSamples, 0.5);
			mOnOpened(File.IsOpened);
		}

		protected override void WriteBuffer(IntPtr pBuffer) {
			File.Read(pBuffer);
			var pDivBuffer = pBuffer;
			for (int d = 0; d < DIV; ++d) {
				Spectrum.SetValue(pDivBuffer, DIV_SAMPLES);
				Marshal.Copy(mMuteData, 0, pDivBuffer, mMuteData.Length);
				Osc.WriteBuffer(pDivBuffer, DIV_SAMPLES);
				pDivBuffer += DIV_SIZE;
			}
			if (File.Position >= File.Length) {
				mTerminate = true;
			}
		}
	}
}
