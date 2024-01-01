using System;
using System.Runtime.InteropServices;
using WinMM;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		const int DIV = 20;
		readonly int DIV_SAMPLES;
		readonly int DIV_SIZE;

		public WavReader File = new WavReader();
		public Spectrum Spectrum;

		public delegate void DOpened(bool isOpened);

		DOpened mOnOpened;
		float[] mMuteData;
		OscBank mOsc;

		public Playback(int sampleRate, DOpened onOpened, DTerminated onTerminated)
			: base(sampleRate, 2, BUFFER_TYPE.F32, sampleRate / 1000 * DIV, 10) {
			DIV_SAMPLES = BufferSamples / DIV;
			DIV_SIZE = WaveFormatEx.nBlockAlign * DIV_SAMPLES;
			Spectrum = new Spectrum(sampleRate, Settings.BASE_FREQ, Settings.NOTE_COUNT, true);
			mOnOpened = onOpened;
			mOnTerminated = onTerminated;
			mMuteData = new float[DIV_SAMPLES * 2];
			mOsc = new OscBank(Settings.NOTE_COUNT, Spectrum);
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
			File = new WavReader(filePath, SampleRate, BufferSamples, 1.0);
			mOnOpened(File.IsOpened);
		}

		protected unsafe override void WriteBuffer(IntPtr pBuffer) {
			File.Read(pBuffer);
			var pDivBuffer = pBuffer;
			for (int d = 0; d < DIV; ++d) {
				Spectrum.Calc((float*)pDivBuffer, DIV_SAMPLES);
				Marshal.Copy(mMuteData, 0, pDivBuffer, mMuteData.Length);
				mOsc.WriteBuffer((float*)pDivBuffer, DIV_SAMPLES);
				pDivBuffer += DIV_SIZE;
			}
			if (File.Position >= File.Length) {
				mTerminate = true;
			}
		}
	}
}
