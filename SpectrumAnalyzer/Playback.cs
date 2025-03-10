﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using WinMM;
using Spectrum;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		const int DIV_COUNT = 10;
		readonly int DIV_SAMPLES;
		readonly int DIV_SIZE;

		public WavReader File = new WavReader();

		public Spectrum.Spectrum Spectrum { get; private set; }

		public WaveSynth Osc { get; private set; }

		public string PlayingName { get; private set; } = "";

		int PlayFileIndex = 0;
		List<string> FileList = new List<string>();

		delegate void DOpened(bool isOpened);

		DOpened OnOpened;
		float[] MuteData;

		public Playback(int sampleRate, int bufferCount = 20)
			: base(sampleRate, 2, BUFFER_TYPE.F32, sampleRate / 1000 * DIV_COUNT, bufferCount) {
			DIV_SAMPLES = BufferSamples / DIV_COUNT;
			DIV_SIZE = WaveFormatEx.nBlockAlign * DIV_SAMPLES;
			Spectrum = new Spectrum.Spectrum(sampleRate);
			OnOpened = (isOpen) => {
				PlayingName = Path.GetFileNameWithoutExtension(FileList[PlayFileIndex]);
				File.Speed = Forms.Settings.Speed;
			};
			mOnTerminated = () => {
				if (FileList.Count > 0) {
					PlayFileIndex = ++PlayFileIndex % FileList.Count;
					OpenFile(FileList[PlayFileIndex]);
					Start();
				}
			};
			MuteData = new float[DIV_SAMPLES * 2];
			Osc = new WaveSynth(Spectrum);
		}

		public void Open() {
			OpenDevice();
		}

		public void Close() {
			CloseDevice();
		}

		public void SetFiles(List<string> fileList) {
			if (fileList == null || fileList.Count == 0) {
				return;
			}
			PlayFileIndex = 0;
			FileList.Clear();
			FileList.AddRange(fileList);
			OpenFile(FileList[PlayFileIndex]);
		}

		public void Previous() {
			if (FileList.Count == 0) {
				return;
			}
			PlayFileIndex = (FileList.Count + PlayFileIndex - 1) % FileList.Count;
			OpenFile(FileList[PlayFileIndex]);
		}

		public void Next() {
			if (FileList.Count == 0) {
				return;
			}
			PlayFileIndex = ++PlayFileIndex % FileList.Count;
			OpenFile(FileList[PlayFileIndex]);
		}

		void OpenFile(string filePath) {
			var playing = Playing;
			Pause();
			File.Dispose();
			File = new WavReader(filePath, SampleRate, BufferSamples, 1.0);
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
				Marshal.Copy(MuteData, 0, pDivBuffer, MuteData.Length);
				Osc.WriteBuffer(pDivBuffer, DIV_SAMPLES);
				pDivBuffer += DIV_SIZE;
			}
			if (File.Position >= File.SampleCount) {
				mTerminate = true;
			}
		}
	}
}
