using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using WinMM;
using SignalProcess;
using System.Xml;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		public WavReader File = new WavReader();
		public Spectrum Spectrum { get; private set; }
		public WaveSynth Osc { get; private set; }
		public string PlayingName { get; private set; } = "";

		private int PlayFileIndex = 0;
		private readonly List<string> FileList = new List<string>();
		private readonly int UnitFrames;
		private readonly int UnitSize;
		private readonly int UnitCount;
		private readonly DOpened OnOpened;
		delegate void DOpened(bool isOpened);

		public Playback(int sampleRate, double unitTime, int unitCount) : base(sampleRate, unitTime, unitCount) {
			UnitFrames = (int)(sampleRate * unitTime);
			UnitSize = WaveFormatEx.nBlockAlign * UnitFrames;
			UnitCount = unitCount;
			Spectrum = new Spectrum(sampleRate);
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
			var playing = IsPlaying;
			Stop();
			File.Dispose();
			File = new WavReader(filePath, SampleRate, BufferFrames, 2.0);
			OnOpened(File.IsOpened);
			if (playing) {
				Start();
			}
		}

		protected override void WriteBuffer(IntPtr pBuffer) {
			File.Read(pBuffer);
			var pDivBuffer = pBuffer;
			for (int d = 0; d < UnitCount; ++d) {
				Spectrum.Update(pDivBuffer, UnitFrames);
				Marshal.Copy(MuteData, 0, pDivBuffer, UnitSize);
				Osc.WriteBuffer(pDivBuffer, UnitFrames);
				pDivBuffer += UnitSize;
			}
			if (File.Position >= File.SampleCount) {
				NotifyEndOfFile = true;
			}
		}
	}
}
