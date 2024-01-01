using System;
using System.Runtime.InteropServices;
using WinMM;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		public WavReader File = new WavReader();
		public Spectrum Spectrum;
		public OscBank Osc;

		public delegate void DOpened(bool isOpened);

		readonly int DIV_SAMPLES;
		readonly int DIV_SIZE;

		DOpened mOnOpened;
		float[] mMuteData;

		public Playback(int sampleRate, DOpened onOpened, DTerminated onTerminated)
			: base(sampleRate, 2, BUFFER_TYPE.F32, sampleRate / 800 << 4, 16) {
			DIV_SAMPLES = BufferSamples >> 4;
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
			for (int i = 0, ofs = 0; i < 16; ++i, ofs += DIV_SIZE) {
				Spectrum.SetValue(pBuffer + ofs, DIV_SAMPLES);
				Marshal.Copy(mMuteData, 0, pBuffer + ofs, mMuteData.Length);
				Osc.WriteBuffer(pBuffer + ofs, DIV_SAMPLES);
			}
			if (File.Position >= File.Length) {
				mTerminate = true;
			}
		}
	}
}
