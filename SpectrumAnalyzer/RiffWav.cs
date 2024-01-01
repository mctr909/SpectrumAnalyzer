using System;
using System.IO;
using System.Runtime.InteropServices;

public abstract class RiffWav : IDisposable {
	protected const uint SIGN_RIFF = 0x46464952;
	protected const uint TYPE_WAVE = 0x45564157;
	protected const uint SIGN_FMT_ = 0x20746d66;
	protected const uint SIGN_DATA = 0x61746164;

	public struct FMT {
		public TYPE FormatID;
		public ushort Channel;
		public uint SampleRate;
		public uint BytesPerSecond;
		public ushort BlockSize;
		public ushort BitsPerSample;

		public enum TYPE : ushort {
			PCM_INT = 1,
			PCM_FLOAT = 3
		}
	}

	public FMT Format;
	public int Length { get; protected set; } = 0;
	public int Cursor { get; protected set; } = 0;

	protected FileStream mFs;
	protected long mDataSize;
	protected long mDataBegin;

	public virtual void Dispose() {
		if (null == mFs) {
			return;
		}
		mFs.Close();
		mFs.Dispose();
	}

	public void SeekCurrent(int samples) {
		mFs.Seek(Format.BlockSize * samples, SeekOrigin.Current);
		Cursor += samples;
	}

	public void SeekBegin(int samples) {
		mFs.Seek(mDataBegin + Format.BlockSize * samples, SeekOrigin.Begin);
		Cursor = samples;
	}
}

public class WavReader : RiffWav {
	public double Position { get; set; } = 0;
	public double Speed { get; set; } = 1.0;

	public delegate void DRead(IntPtr output);
	public DRead Read = (p) => { };

	readonly int OUTPUT_SAMPLES;
	readonly int BUFFER_SAMPLES;
	readonly int BUFFER_SIZE;
	readonly double DELTA;

	BinaryReader mBr;
	IntPtr mBuffer;
	byte[] mMuteData;
	int mOffset;

	const float SCALE_8BIT = 1.0f / (1<<7);
	const float SCALE_16BIT = 1.0f / (1<<15);
	const float SCALE_32BIT = 1.0f / (1<<31);

	public WavReader() {
		Length = 1;
	}

	public WavReader(string filePath, int sampleRate, int outputSamples, double bufferUnitSec) {
		mFs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		mBr = new BinaryReader(mFs);
		if (!ReadHeader()) {
			return;
		}
		if (!CheckFormat()) {
			return;
		}
		Length = (int)(mDataSize / Format.BlockSize);
		OUTPUT_SAMPLES = outputSamples;
		BUFFER_SAMPLES = (int)(Format.SampleRate * bufferUnitSec);
		BUFFER_SIZE = Format.BlockSize * (BUFFER_SAMPLES + 1);
		DELTA = (double)Format.SampleRate / sampleRate;
		mBuffer = Marshal.AllocHGlobal(BUFFER_SIZE);
		mMuteData = new byte[BUFFER_SIZE];
		mOffset = 0;
		SetBuffer();
	}

	public override void Dispose() {
		base.Dispose();
		Marshal.FreeHGlobal(mBuffer);
	}

	bool ReadHeader() {
		var fileSign = mBr.ReadUInt32();
		if (fileSign != SIGN_RIFF)
			return false;

		var fileSize = mBr.ReadUInt32();
		var fileType = mBr.ReadUInt32();
		if (fileType != TYPE_WAVE)
			return false;

		uint chunkSign;
		uint chunkSize;
		while (mFs.Position < fileSize) {
			chunkSign = mBr.ReadUInt32();
			chunkSize = mBr.ReadUInt32();
			switch (chunkSign) {
			case SIGN_FMT_:
				Format.FormatID = (FMT.TYPE)mBr.ReadUInt16();
				Format.Channel = mBr.ReadUInt16();
				Format.SampleRate = mBr.ReadUInt32();
				Format.BytesPerSecond = mBr.ReadUInt32();
				Format.BlockSize = mBr.ReadUInt16();
				Format.BitsPerSample = mBr.ReadUInt16();
				if (chunkSize > 16)
					mFs.Seek(chunkSize - 16, SeekOrigin.Current);
				break;
			case SIGN_DATA:
				mDataSize = chunkSize;
				mDataBegin = mFs.Position;
				mFs.Seek(chunkSize, SeekOrigin.Current);
				break;
			default:
				mFs.Seek(chunkSize, SeekOrigin.Current);
				break;
			}
		}

		mFs.Seek(mDataBegin, SeekOrigin.Begin);
		return true;
	}

	bool CheckFormat() {
		switch (Format.FormatID) {
		case FMT.TYPE.PCM_INT:
			switch (Format.BitsPerSample) {
			case 8:
				switch (Format.Channel) {
				case 1:
					Read = ReadI8Mono;
					break;
				case 2:
					Read = ReadI8Stereo;
					break;
				default:
					return false;
				}
				break;
			case 16:
				switch (Format.Channel) {
				case 1:
					Read = ReadI16Mono;
					break;
				case 2:
					Read = ReadI16Stereo;
					break;
				default:
					return false;
				}
				break;
			case 24:
				switch (Format.Channel) {
				case 1:
					Read = ReadI24Mono;
					break;
				case 2:
					Read = ReadI24Stereo;
					break;
				default:
					return false;
				}
				break;
			default:
				return false;
			}
			break;
		case FMT.TYPE.PCM_FLOAT:
			switch (Format.BitsPerSample) {
			case 32:
				switch (Format.Channel) {
				case 1:
					Read = ReadF32Mono;
					break;
				case 2:
					Read = ReadF32Stereo;
					break;
				default:
					return false;
				}
				break;
			default:
				return false;
			}
			break;
		default:
			return false;
		}

		return true;
	}

	void SetBuffer() {
		if (Length - Cursor <= BUFFER_SAMPLES) {
			var readSize = (Length - Cursor) * Format.BlockSize;
			Marshal.Copy(mMuteData, 0, mBuffer, BUFFER_SIZE);
			Marshal.Copy(mBr.ReadBytes(readSize), 0, mBuffer, readSize);
			Cursor = Length;
		}
		else {
			Marshal.Copy(mBr.ReadBytes(BUFFER_SIZE), 0, mBuffer, BUFFER_SIZE);
			mFs.Seek(Format.BlockSize * -1, SeekOrigin.Current);
			Cursor += BUFFER_SAMPLES;
		}
	}

	unsafe void ReadI8Mono(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (byte*)mBuffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < Length; ++s, Position += DELTA * Speed) {
			var remain = Position - mOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				mOffset += (int)(remain + Math.Sign(remain - (int)remain));
				SeekBegin(mOffset);
				SetBuffer();
			}
			var indexF = (float)(Position - mOffset);
			var indexI = (int)indexF;
			var b = (indexF - indexI) * SCALE_8BIT;
			var a = SCALE_8BIT - b;
			var p = pInput + indexI;
			*pOutput = (*p++ - 128) * a + (*p - 128) * b;
			*pOutput++ = *pOutput++;
		}
		for (; s < OUTPUT_SAMPLES; ++s) {
			*pOutput++ = 0;
			*pOutput++ = 0;
		}
	}

	unsafe void ReadI8Stereo(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (byte*)mBuffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < Length; ++s, Position += DELTA * Speed) {
			var remain = Position - mOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				mOffset += (int)(remain + Math.Sign(remain - (int)remain));
				SeekBegin(mOffset);
				SetBuffer();
			}
			var indexF = (float)(Position - mOffset);
			var indexI = (int)indexF;
			var b = (indexF - indexI) * SCALE_8BIT;
			var a = SCALE_8BIT - b;
			var p = pInput + (indexI << 1);
			var l = (*p++ - 128) * a;
			var r = (*p++ - 128) * a;
			l += (*p++ - 128) * b;
			r += (*p - 128) * b;
			*pOutput++ = l;
			*pOutput++ = r;
		}
		for (; s < OUTPUT_SAMPLES; ++s) {
			*pOutput++ = 0;
			*pOutput++ = 0;
		}
	}

	unsafe void ReadI16Mono(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (short*)mBuffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < Length; ++s, Position += DELTA * Speed) {
			var remain = Position - mOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				mOffset += (int)(remain + Math.Sign(remain - (int)remain));
				SeekBegin(mOffset);
				SetBuffer();
			}
			var indexF = (float)(Position - mOffset);
			var indexI = (int)indexF;
			var b = (indexF - indexI) * SCALE_16BIT;
			var a = SCALE_16BIT - b;
			var p = pInput + indexI;
			*pOutput = *p++ * a + *p * b;
			*pOutput++ = *pOutput++;
		}
		for (; s < OUTPUT_SAMPLES; ++s) {
			*pOutput++ = 0;
			*pOutput++ = 0;
		}
	}

	unsafe void ReadI16Stereo(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (short*)mBuffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < Length; ++s, Position += DELTA * Speed) {
			var remain = Position - mOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				mOffset += (int)(remain + Math.Sign(remain - (int)remain));
				SeekBegin(mOffset);
				SetBuffer();
			}
			var indexF = (float)(Position - mOffset);
			var indexI = (int)indexF;
			var b = (indexF - indexI) * SCALE_16BIT;
			var a = SCALE_16BIT - b;
			var p = pInput + (indexI << 1);
			var l = *p++ * a;
			var r = *p++ * a;
			l += *p++ * b;
			r += *p * b;
			*pOutput++ = l;
			*pOutput++ = r;
		}
		for (; s < OUTPUT_SAMPLES; ++s) {
			*pOutput++ = 0;
			*pOutput++ = 0;
		}
	}

	unsafe void ReadI24Mono(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (byte*)mBuffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < Length; ++s, Position += DELTA * Speed) {
			var remain = Position - mOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				mOffset += (int)(remain + Math.Sign(remain - (int)remain));
				SeekBegin(mOffset);
				SetBuffer();
			}
			var indexF = (float)(Position - mOffset);
			var indexI = (int)indexF;
			var b = (indexF - indexI) * SCALE_32BIT;
			var a = SCALE_32BIT - b;
			var p = pInput + indexI * 3;
			var m1 = ((uint)*p++ << 16) | ((uint)*p++ << 24) | ((uint)*p++ << 8);
			var m2 = ((uint)*p++ << 16) | ((uint)*p++ << 24) | ((uint)*p << 8);
			*pOutput = (int)m1 * a + (int)m2 * b;
			*pOutput++ = *pOutput++;
		}
		for (; s < OUTPUT_SAMPLES; ++s) {
			*pOutput++ = 0;
			*pOutput++ = 0;
		}
	}

	unsafe void ReadI24Stereo(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (byte*)mBuffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < Length; ++s, Position += DELTA * Speed) {
			var remain = Position - mOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				mOffset += (int)(remain + Math.Sign(remain - (int)remain));
				SeekBegin(mOffset);
				SetBuffer();
			}
			var indexF = (float)(Position - mOffset);
			var indexI = (int)indexF;
			var b = (indexF - indexI) * SCALE_32BIT;
			var a = SCALE_32BIT - b;
			var p = pInput + (indexI * 6);
			var l1 = ((uint)*p++ << 16) | ((uint)*p++ << 24) | ((uint)*p++ << 8);
			var r1 = ((uint)*p++ << 16) | ((uint)*p++ << 24) | ((uint)*p++ << 8);
			var l2 = ((uint)*p++ << 16) | ((uint)*p++ << 24) | ((uint)*p++ << 8);
			var r2 = ((uint)*p++ << 16) | ((uint)*p++ << 24) | ((uint)*p << 8);
			*pOutput++ = (int)l1 * a + (int)l2 * b;
			*pOutput++ = (int)r1 * a + (int)r2 * b;
		}
		for (; s < OUTPUT_SAMPLES; ++s) {
			*pOutput++ = 0;
			*pOutput++ = 0;
		}
	}

	unsafe void ReadF32Mono(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (float*)mBuffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < Length; ++s, Position += DELTA * Speed) {
			var remain = Position - mOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				mOffset += (int)(remain + Math.Sign(remain - (int)remain));
				SeekBegin(mOffset);
				SetBuffer();
			}
			var indexF = (float)(Position - mOffset);
			var indexI = (int)indexF;
			var b = indexF - indexI;
			var a = 1 - b;
			var p = pInput + indexI;
			*pOutput = *p++ * a + *p * b;
			*pOutput++ = *pOutput++;
		}
		for (; s < OUTPUT_SAMPLES; ++s) {
			*pOutput++ = 0;
			*pOutput++ = 0;
		}
	}

	unsafe void ReadF32Stereo(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (float*)mBuffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < Length; ++s, Position += DELTA * Speed) {
			var remain = Position - mOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				mOffset += (int)(remain + Math.Sign(remain - (int)remain));
				SeekBegin(mOffset);
				SetBuffer();
			}
			var indexF = (float)(Position - mOffset);
			var indexI = (int)indexF;
			var b = indexF - indexI;
			var a = 1 - b;
			var p = pInput + (indexI << 1);
			var l = *p++ * a;
			var r = *p++ * a;
			l += *p++ * b;
			r += *p * b;
			*pOutput++ = l;
			*pOutput++ = r;
		}
		for (; s < OUTPUT_SAMPLES; ++s) {
			*pOutput++ = 0;
			*pOutput++ = 0;
		}
	}
}

public class WavWriter : RiffWav {
	BinaryWriter mBw;

	public WavWriter(string filePath, int sampleRate, int ch, int bits, bool enableFloat) {
		Format.FormatID = enableFloat ? FMT.TYPE.PCM_FLOAT : FMT.TYPE.PCM_INT;
		Format.Channel = (ushort)ch;
		Format.SampleRate = (uint)sampleRate;
		Format.BitsPerSample = (ushort)(enableFloat ? 32 : bits);
		Format.BlockSize = (ushort)(Format.Channel * Format.BitsPerSample >> 3);
		Format.BytesPerSecond = Format.BlockSize * Format.SampleRate;

		mFs = new FileStream(filePath, FileMode.OpenOrCreate);
		mBw = new BinaryWriter(mFs);
		WriteHeader(mBw);
		mDataBegin = mFs.Position;
		Length = 0;
	}

	public void Save() {
		var currentPos = mFs.Position;

		mFs.Seek(4, SeekOrigin.Begin);
		mBw.Write((uint)(Format.BlockSize * Length + 36));

		mFs.Seek(mDataBegin - 4, SeekOrigin.Begin);
		mBw.Write((uint)(Format.BlockSize * Length));

		mFs.Seek(currentPos, SeekOrigin.Begin);
	}

	public void WriteBuffer(byte[] buffer, int begin, int samples) {
		mBw.Write(buffer, begin * Format.BlockSize, samples * Format.BlockSize);
		Length += samples;
	}

	protected void WriteHeader(BinaryWriter bw) {
		bw.Write(SIGN_RIFF);
		bw.Write((uint)0);
		bw.Write(TYPE_WAVE);

		bw.Write(SIGN_FMT_);
		bw.Write((uint)16);
		bw.Write((ushort)Format.FormatID);
		bw.Write(Format.Channel);
		bw.Write(Format.SampleRate);
		bw.Write(Format.BytesPerSecond);
		bw.Write(Format.BlockSize);
		bw.Write(Format.BitsPerSample);

		bw.Write(SIGN_DATA);
		bw.Write((uint)0);
	}
}
