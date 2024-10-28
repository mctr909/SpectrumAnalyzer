using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SpectrumAnalyzer {
	public class SpectrumDll : IDisposable {
		public string PlayingName { get; private set; } = "";

		delegate void DOnOpened(bool isOpen);
		delegate void DOnTerminate();
		DOnOpened mOnOpened;
		DOnTerminate mOnTerminate;
		IntPtr mPlayback = IntPtr.Zero;
		int mPlayFileIndex = 0;
		List<string> mFileList = new List<string>();

		[DllImport("Spectrum.dll")]
		static extern void OutputOpen(ref IntPtr hInstance, int sampleRate, DOnOpened onOpened, DOnTerminate onTerminate);
		[DllImport("Spectrum.dll")]
		static extern void OutputClose(ref IntPtr hInstance);
		[DllImport("Spectrum.dll")]
		static extern void OutputStart(IntPtr hInstance);
		[DllImport("Spectrum.dll")]
		static extern void OutputPause(IntPtr hInstance);
		[DllImport("Spectrum.dll", CharSet = CharSet.Auto)]
		static extern void SetPlaybackFile(IntPtr hInstance, string filePath);
		[DllImport("Spectrum.dll")]
		static extern void SetSpeed(IntPtr hInstance, double speed);
		[DllImport("Spectrum.dll")]
		static extern double GetSpeed(IntPtr hInstance);
		[DllImport("Spectrum.dll")]
		static extern void SetTranspose(IntPtr hInstance, int trancepose);
		[DllImport("Spectrum.dll")]
		static extern int GetTranspose(IntPtr hInstance);

		public double Speed {
			set { SetSpeed(mPlayback, value); }
			get { return GetSpeed(mPlayback); }
		}
		public int Transpose {
			set { SetTranspose(mPlayback, value); }
			get { return GetTranspose(mPlayback); }
		}
		public bool Playing { get; set; }
		public double Position { get; set; }

		public SpectrumDll() {
			mOnOpened = (isOpen) => {
				PlayingName = Path.GetFileNameWithoutExtension(mFileList[mPlayFileIndex]);
				SetSpeed(mPlayback, Settings.Speed);
				//SetTranspose(mPlayback, Settings.Transpose);
			};
			mOnTerminate = () => {
				if (mFileList.Count > 0) {
					mPlayFileIndex = ++mPlayFileIndex % mFileList.Count;
					SetPlaybackFile(mPlayback, mFileList[mPlayFileIndex]);
					OutputStart(mPlayback);
				}
			};
			OutputOpen(ref mPlayback, 48000, mOnOpened, mOnTerminate);
		}
		public void Dispose() {
			OutputClose(ref mPlayback);
		}
		public void SetFiles(List<string> fileList) {
			if (fileList.Count == 0) {
				return;
			}
			mPlayFileIndex = 0;
			mFileList.Clear();
			mFileList.AddRange(fileList);
			SetPlaybackFile(mPlayback, mFileList[mPlayFileIndex]);
		}
		public void Start() {
			OutputStart(mPlayback);
		}
		public void Pause() {
			OutputPause(mPlayback);
		}
		public void Previous() {
			if (mFileList.Count == 0) {
				return;
			}
			var playing = Playing;
			mPlayFileIndex = (mFileList.Count + mPlayFileIndex - 1) % mFileList.Count;
			SetPlaybackFile(mPlayback, mFileList[mPlayFileIndex]);
			if (playing) {
				OutputStart(mPlayback);
			}
		}
		public void Next() {
			if (mFileList.Count == 0) {
				return;
			}
			var playing = Playing;
			mPlayFileIndex = ++mPlayFileIndex % mFileList.Count;
			SetPlaybackFile(mPlayback, mFileList[mPlayFileIndex]);
			if (playing) {
				OutputStart(mPlayback);
			}
		}
	}
}
