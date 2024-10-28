using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using WinMM;
using Spectrum;
using System.Xml;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		public WavReader File = new WavReader();
		public Spectrum.Spectrum Spectrum { get; private set; }
		public string PlayingName { get; private set; } = "";

		private int PlayFileIndex = 0;
		private readonly List<string> FileList = new List<string>();
		private readonly int DivSamples;
		private readonly int DivSize;
		private readonly int DivCount;
		private readonly DOpened OnOpened;
		delegate void DOpened(bool isOpened);
		private readonly WaveSynth Osc;

		public Playback(int sampleRate, double calcUnitTime, int divCount) : base(
			sampleRate, 2, (int)(sampleRate * calcUnitTime) * divCount, divCount * 4
		) {
			DivSamples = (int)(sampleRate * calcUnitTime);
			DivSize = WaveFormatEx.nBlockAlign * DivSamples;
			DivCount = divCount;
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

		public void Save(string path) {
			var xml = new XmlDocument();
			var root = xml.CreateElement("playlist");
			foreach (var filePath in FileList) {
				var elm = xml.CreateElement("file");
				elm.InnerText = filePath;
				root.AppendChild(elm);
			}
			xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
			xml.AppendChild(root);
			xml.Save(Path.Combine(Path.GetDirectoryName(path), "playlist.xml"));
		}

		public void Load(string path) {
			var listFile = Path.Combine(Path.GetDirectoryName(path), "playlist.xml");
			if (!System.IO.File.Exists(listFile)) {
				return;
			}
			var xml = new XmlDocument();
			xml.Load(listFile);
			var list = new List<string>();
			foreach (var root in xml.ChildNodes) {
				if (root is XmlElement playlist && playlist.Name == "playlist") {
					foreach (var child in playlist.ChildNodes) {
						if (child is XmlElement elm) {
							if (elm.Name == "file") {
								list.Add(elm.InnerText);
							}
						}
					}
				}
			}
			SetFileList(list);
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
			File = new WavReader(filePath, SampleRate, BufferSamples, 2.0);
			OnOpened(File.IsOpened);
			if (playing) {
				Start();
			}
		}

		protected override void WriteBuffer(IntPtr pBuffer) {
			File.Read(pBuffer);
			var pDivBuffer = pBuffer;
			for (int d = 0; d < DivCount; ++d) {
				Spectrum.Update(pDivBuffer, DivSamples);
				Marshal.Copy(MuteData, 0, pDivBuffer, DivSize);
				Osc.WriteBuffer(pDivBuffer, DivSamples);
				pDivBuffer += DivSize;
			}
			if (File.Position >= File.SampleCount) {
				EndOfFile = true;
			}
		}
	}
}
