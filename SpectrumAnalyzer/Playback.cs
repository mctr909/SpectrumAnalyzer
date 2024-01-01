using System;
using System.Threading;
using WINMM;

namespace SpectrumAnalyzer {
	public class Playback : WaveOut {
		double mDelta;
		double mTime;
		int mIndexOffset;
		OscBank mOscBank;
		WavReader mFile;

		delegate void DReadFile(IntPtr output);
		DReadFile mReadFile;

		public Spectrum FilterBank;

		public delegate void DTerminated();
		public DTerminated OnTerminated;

		public double Position {
			get { return mTime; }
			set { mTime = value; }
		}
		public int Length { get; private set; } = 1;
		public double Speed { get; set; } = 1.0;

		public Playback(int sampleRate, DTerminated onTerminated = null)
			: base(sampleRate, 2, BUFFER_TYPE.F32, sampleRate / 800, 100) {
			mDelta = 0.0;
			mTime = 0.0;
			mReadFile = ReadInvalid;
			mOscBank = new OscBank(BufferSamples, Settings.NOTE_COUNT);
			FilterBank = new Spectrum(sampleRate, Settings.BASE_FREQ, Settings.NOTE_COUNT, BufferSamples, true);
			if (null == onTerminated) {
				OnTerminated = () => {
					mTime -= Length * (int)(mTime / Length);
				};
			} else {
				OnTerminated = onTerminated;
			}
		}

		public void OpenFile(string filePath) {
			Pause();
			if (null != mFile) {
				mFile.Dispose();
			}
			mFile = new WavReader(filePath, 1024);
			mDelta = (double)mFile.Fmt.SampleRate / SampleRate;
			mTime = 0.0;
			mIndexOffset = 0;
			Position = 0;
			Length = (int)mFile.Samples;
			switch (mFile.Fmt.BitsPerSample) {
			case 16:
				if (mFile.Fmt.FormatID == RiffWav.FMT.TYPE.PCM_INT) {
					switch (mFile.Fmt.Channel) {
					case 1:
						mReadFile = Read16Mono;
						break;
					case 2:
						mReadFile = Read16Stereo;
						break;
					default:
						mReadFile = ReadInvalid;
						break;
					}
				}
				else {
					mReadFile = ReadInvalid;
				}
				break;
			case 32:
				if (mFile.Fmt.FormatID == RiffWav.FMT.TYPE.PCM_FLOAT) {
					switch (mFile.Fmt.Channel) {
					case 1:
						mReadFile = Read32fMono;
						break;
					case 2:
						mReadFile = Read32fStereo;
						break;
					default:
						mReadFile = ReadInvalid;
						break;
					}
				}
				else {
					mReadFile = ReadInvalid;
				}
				break;
			default:
				mReadFile = ReadInvalid;
				break;
			}
		}

		void ReadInvalid(IntPtr output) { }

		unsafe void Read16Mono(IntPtr output) {
			var pOutput = (float*)output;
			var pInput = (short*)mFile.Buffer;
			for (int s = 0; s < BufferSamples; ++s) {
				var remain = mTime - mIndexOffset;
				if (remain >= mFile.BufferSamples || remain <= -1) {
					mIndexOffset += (int)(remain + Math.Sign(remain - (int)remain));
					mFile.SetBuffer(mIndexOffset);
				}
				var idxF = (float)(mTime - mIndexOffset);
				mTime += mDelta * Speed;
				if (mTime >= Length) {
					new Thread(() => { OnTerminated(); }).Start();
					break;
				}
				var idxI = (int)idxF;
				var kb = idxF - idxI;
				var ka = (1 - kb) / 32768;
				kb /= 32768;
				var p = pInput + idxI;
				*pOutput = *p++ * ka + *p * kb;
				*pOutput++ = *pOutput++;
			}
		}

		unsafe void Read16Stereo(IntPtr output) {
			var pOutput = (float*)output;
			var pInput = (short*)mFile.Buffer;
			for (int s = 0; s < BufferSamples; ++s) {
				var remain = mTime - mIndexOffset;
				if (remain >= mFile.BufferSamples || remain <= -1) {
					mIndexOffset += (int)(remain + Math.Sign(remain - (int)remain));
					mFile.SetBuffer(mIndexOffset);
				}
				var idxF = (float)(mTime - mIndexOffset);
				mTime += mDelta * Speed;
				if (mTime >= Length) {
					new Thread(() => { OnTerminated(); }).Start();
					break;
				}
				var idxI = (int)idxF;
				var kb = idxF - idxI;
				var ka = (1 - kb) / 32768;
				kb /= 32768;
				var p = pInput + (idxI << 1);
				var l = *p++ * ka;
				var r = *p++ * ka;
				l += *p++ * kb;
				r += *p * kb;
				*pOutput++ = l;
				*pOutput++ = r;
			}
		}

		unsafe void Read32fMono(IntPtr output) {
			var pOutput = (float*)output;
			var pInput = (float*)mFile.Buffer;
			for (int s = 0; s < BufferSamples; ++s) {
				var remain = mTime - mIndexOffset;
				if (remain >= mFile.BufferSamples || remain <= -1) {
					mIndexOffset += (int)(remain + Math.Sign(remain - (int)remain));
					mFile.SetBuffer(mIndexOffset);
				}
				var idxF = (float)(mTime - mIndexOffset);
				mTime += mDelta * Speed;
				if (mTime >= Length) {
					new Thread(() => { OnTerminated(); }).Start();
					break;
				}
				var idxI = (int)idxF;
				var kb = idxF - idxI;
				var ka = 1 - kb;
				var p = pInput + idxI;
				*pOutput = *p++ * ka + *p * kb;
				*pOutput++ = *pOutput++;
			}
		}

		unsafe void Read32fStereo(IntPtr output) {
			var pOutput = (float*)output;
			var pInput = (float*)mFile.Buffer;
			for (int s = 0; s < BufferSamples; ++s) {
				var remain = mTime - mIndexOffset;
				if (remain >= mFile.BufferSamples || remain <= -1) {
					mIndexOffset += (int)(remain + Math.Sign(remain - (int)remain));
					mFile.SetBuffer(mIndexOffset);
				}
				var idxF = (float)(mTime - mIndexOffset);
				mTime += mDelta * Speed;
				if (mTime >= Length) {
					new Thread(() => { OnTerminated(); }).Start();
					break;
				}
				var idxI = (int)idxF;
				var kb = idxF - idxI;
				var ka = 1 - kb;
				var p = pInput + (idxI << 1);
				var l = *p++ * ka;
				var r = *p++ * ka;
				l += *p++ * kb;
				r += *p * kb;
				*pOutput++ = l;
				*pOutput++ = r;
			}
		}

		protected override void WriteBuffer(IntPtr pBuffer) {
			mReadFile(pBuffer);
			FilterBank.SetValue(pBuffer, BufferSamples);
			mOscBank.SetWave(FilterBank, pBuffer);
		}
	}
}
