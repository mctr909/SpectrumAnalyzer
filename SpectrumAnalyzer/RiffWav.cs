using System;
using System.IO;
using System.Runtime.InteropServices;

public abstract class RiffWav {
	protected struct RIFF {
		public uint Sign;
		public uint Size;
		public uint Type;
		public const uint SIGN = 0x46464952;
		public const uint TYPE_WAVE = 0x45564157;
		public void Write(BinaryWriter bw) {
			bw.Write(SIGN);
			bw.Write(Size);
			bw.Write(Type);
		}
	}

	public struct FMT {
		public uint Sign;
		public uint Size;
		public ushort FormatID;
		public ushort Channel;
		public uint SamplingFrequency;
		public uint BytePerSecond;
		public ushort BlockSize;
		public ushort BitPerSample;
		public const uint SIGN = 0x20746d66;
		public const ushort TYPE_PCM = 1;
		public const ushort TYPE_PCM_FLOAT = 3;
		public void Write(BinaryWriter bw) {
			bw.Write(SIGN);
			bw.Write((uint)16);
			bw.Write(FormatID);
			bw.Write(Channel);
			bw.Write(SamplingFrequency);
			bw.Write(BytePerSecond);
			bw.Write(BlockSize);
			bw.Write(BitPerSample);
		}
	}

	public struct DATA {
		public uint Sign;
		public uint Size;
		public const uint SIGN = 0x61746164;
		public void Write(BinaryWriter bw) {
			bw.Write(SIGN);
			bw.Write(Size);
		}
	}

	protected RIFF Riff;
	public FMT Fmt;
	public DATA Data;
}

public class WavReader : RiffWav, IDisposable {
	public delegate void Reader(ref short left, ref short right);
	public delegate void ReaderM(ref short mono);

	public Reader Read;
	public ReaderM ReadMono;

	FileStream mFs;
	BinaryReader mBr;

	public WavReader(string filePath) {
		mFs = new FileStream(filePath, FileMode.Open);
		mBr = new BinaryReader(mFs);

		Riff.Sign = mBr.ReadUInt32();
		Riff.Size = mBr.ReadUInt32();
		Riff.Type = mBr.ReadUInt32();

		if (Riff.Sign != RIFF.SIGN)
			return;
		if (Riff.Type != RIFF.TYPE_WAVE)
			return;

		Fmt.Sign = mBr.ReadUInt32();
		Fmt.Size = mBr.ReadUInt32();
		Fmt.FormatID = mBr.ReadUInt16();
		Fmt.Channel = mBr.ReadUInt16();
		Fmt.SamplingFrequency = mBr.ReadUInt32();
		Fmt.BytePerSecond = mBr.ReadUInt32();
		Fmt.BlockSize = mBr.ReadUInt16();
		Fmt.BitPerSample = mBr.ReadUInt16();

		if (Fmt.Sign != FMT.SIGN)
			return;
		if (Fmt.FormatID != FMT.TYPE_PCM && Fmt.FormatID != FMT.TYPE_PCM_FLOAT)
			return;
		if (Fmt.Size > 16)
			mFs.Seek(Fmt.Size - 16, SeekOrigin.Current);
		else if (Fmt.Size < 16)
			return;

		switch (Fmt.Channel) {
		case 1:
			switch (Fmt.BitPerSample) {
			case 8:
				ReadMono = read8;
				break;
			case 16:
				ReadMono = read16;
				break;
			case 24:
				ReadMono = read24;
				break;
			case 32:
				ReadMono = read32;
				break;
			default:
				ReadMono = readInvalid;
				break;
			}
			break;
		case 2:
			switch (Fmt.BitPerSample) {
			case 8:
				Read = read8;
				break;
			case 16:
				Read = read16;
				break;
			case 24:
				Read = read24;
				break;
			case 32:
				Read = read32;
				break;
			default:
				Read = readInvalid;
				break;
			}
			break;
		default:
			ReadMono = readInvalid;
			Read = readInvalid;
			break;
		}

		Data.Sign = mBr.ReadUInt32();
		Data.Size = mBr.ReadUInt32();

		if (Data.Sign != DATA.SIGN)
			return;
	}

	public void Dispose() {
		mFs.Close();
		mFs.Dispose();
	}

	void read32(ref short left, ref short right) {
		var fL = mBr.ReadSingle();
		var fR = mBr.ReadSingle();
		if (fL < -1.0) {
			fL = -1.0f;
		}
		if (1.0 < fL) {
			fL = 1.0f;
		}
		if (fR < -1.0) {
			fR = -1.0f;
		}
		if (1.0 < fR) {
			fR = 1.0f;
		}
		left = (short)(fL * 32767);
		right = (short)(fR * 32767);
	}

	void read32(ref short mono) {
		var f = mBr.ReadSingle();
		if (f < -1.0) {
			f = -1.0f;
		}
		if (1.0 < f) {
			f = 1.0f;
		}
		mono = (short)(f * 32767);
	}

	void read24(ref short left, ref short right) {
		mBr.ReadByte();
		left = mBr.ReadInt16();
		mBr.ReadByte();
		right = mBr.ReadInt16();
	}

	void read24(ref short mono) {
		mBr.ReadByte();
		mono = mBr.ReadInt16();
	}

	void read16(ref short left, ref short right) {
		left = mBr.ReadInt16();
		right = mBr.ReadInt16();
	}

	void read16(ref short mono) {
		mono = mBr.ReadInt16();
	}

	void read8(ref short left, ref short right) {
		left = (short)((mBr.ReadByte() - 128) << 8);
		right = (short)((mBr.ReadByte() - 128) << 8);
	}

	void read8(ref short mono) {
		mono = (short)((mBr.ReadByte() - 128) << 8);
	}

	void readInvalid(ref short left, ref short right) {
		mBr.BaseStream.Position += Fmt.BlockSize;
		left = 0;
		right = 0;
	}

	void readInvalid(ref short mono) {
		mBr.BaseStream.Position += Fmt.BlockSize;
		mono = 0;
	}
}

public class WavWriter : RiffWav, IDisposable {
	FileStream mFs;
	BinaryWriter mBw;
	uint mSamples;

	public WavWriter(string filePath) {
		mFs = new FileStream(filePath, FileMode.OpenOrCreate);
		mBw = new BinaryWriter(mFs);
		mBw.Write(new byte[
			Marshal.SizeOf<RIFF>() +
			Marshal.SizeOf<FMT>() +
			Marshal.SizeOf<DATA>()
		]);
		mSamples = 0;
	}

	public void Dispose() {
		mFs.Close();
		mFs.Dispose();
	}

	public void Save(uint sampleRate, ushort ch, bool enableFloat) {
		mFs.Seek(0, SeekOrigin.Begin);

		var bits = enableFloat ? 32 : 16;

		Riff.Size = (uint)(ch * bits * mSamples / 8 + 36);
		Riff.Type = RIFF.TYPE_WAVE;
		Riff.Write(mBw);

		Fmt.FormatID = enableFloat ? FMT.TYPE_PCM_FLOAT : FMT.TYPE_PCM;
		Fmt.Channel = ch;
		Fmt.SamplingFrequency = sampleRate;
		Fmt.BytePerSecond = (uint)(ch * bits * sampleRate / 8);
		Fmt.BlockSize = (ushort)(ch * bits / 8);
		Fmt.BitPerSample = (ushort)bits;
		Fmt.Write(mBw);

		Data.Size = (uint)(ch * bits * mSamples / 8);
		Data.Write(mBw);

		mFs.Close();
	}

	public void Write(double left, double right) {
		mBw.Write((float)left);
		mBw.Write((float)right);
		mSamples++;
	}

	public void Write(double mono) {
		mBw.Write((float)mono);
		mSamples++;
	}

	public void Write(short left, short right) {
		mBw.Write(left);
		mBw.Write(right);
		mSamples++;
	}

	public void Write(short mono) {
		mBw.Write(mono);
		mSamples++;
	}
}
