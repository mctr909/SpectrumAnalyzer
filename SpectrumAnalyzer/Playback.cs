using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using WinMM;
using Spectrum;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		private readonly int DIV_COUNT;
		private readonly int DIV_SAMPLES;
		private readonly int DIV_SIZE;

		public WavReader File = new WavReader();

		public Spectrum.Spectrum Spectrum { get; private set; }

		public string PlayingName { get; private set; } = "";

		private int PlayFileIndex = 0;
		private readonly List<string> FileList = new List<string>();
		private readonly WaveSynth Osc;

		private readonly DOpened OnOpened;

		delegate void DOpened(bool isOpened);

		public Playback(int sampleRate, double calcUnitTime, int divCount)
			: base(sampleRate, 2, EBufferType.FLOAT32, (int)(sampleRate * calcUnitTime) * divCount, divCount * 2) {
			DIV_COUNT = divCount;
			DIV_SAMPLES = BufferSamples / divCount;
			DIV_SIZE = WaveFormatEx.nBlockAlign * DIV_SAMPLES;
			Spectrum = new Spectrum.Spectrum(sampleRate);
			OnOpened = (isOpen) => {
				PlayingName = Path.GetFileNameWithoutExtension(FileList[PlayFileIndex]);
				File.Speed = Forms.Settings.Speed;
			};
			OnEndOfFile = () => {
				if (FileList.Count > 0) {
					PlayFileIndex = ++PlayFileIndex % FileList.Count;
					OpenFile(FileList[PlayFileIndex]);
					Start();
				}
			};
			Osc = new WaveSynth(Spectrum);
		}

		public void Open() {
			OpenDevice();
		}

		public void Close() {
			CloseDevice();
		}

		public void SetFileList(List<string> fileList) {
			if (fileList == null || fileList.Count == 0) {
				return;
			}
			PlayFileIndex = 0;
			FileList.Clear();
			FileList.AddRange(fileList);
			OpenFile(FileList[PlayFileIndex]);
		}

		public void PreviousFile() {
			if (FileList.Count == 0) {
				return;
			}
			PlayFileIndex = (FileList.Count + PlayFileIndex - 1) % FileList.Count;
			OpenFile(FileList[PlayFileIndex]);
		}

		public void NextFile() {
			if (FileList.Count == 0) {
				return;
			}
			PlayFileIndex = ++PlayFileIndex % FileList.Count;
			OpenFile(FileList[PlayFileIndex]);
		}

		private void OpenFile(string filePath) {
			var playing = Playing;
			Stop();
			File.Dispose();
			File = new WavReader(filePath, SampleRate, BufferSamples, 4.0);
			OnOpened(File.IsOpened);
			if (playing) {
				Start();
			}
		}

		protected override void WriteBuffer(IntPtr pBuffer) {
			File.Read(pBuffer);
			var pDivBuffer = pBuffer;
			for (int d = 0; d < DIV_COUNT; ++d) {
				Spectrum.Update(pDivBuffer, DIV_SAMPLES);
				Marshal.Copy(MuteData, 0, pDivBuffer, DIV_SIZE);
				Osc.WriteBuffer(pDivBuffer, DIV_SAMPLES);
				pDivBuffer += DIV_SIZE;
			}
			if (File.Position >= File.SampleCount) {
				EndOfFile = true;
			}
		}
	}
}
