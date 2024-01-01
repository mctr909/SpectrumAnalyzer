using System;
using System.IO;

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

	public FMT Fmt;
	public long Samples { get; protected set; } = 0;

	protected FileStream mFs;
	protected long mDataSize;
	protected long mDataBegin;

	public void Dispose() {
		mFs.Close();
		mFs.Dispose();
	}

	protected void WriteHeader(BinaryWriter bw) {
		bw.Write(SIGN_RIFF);
		bw.Write((uint)0);
		bw.Write(TYPE_WAVE);

		bw.Write(SIGN_FMT_);
		bw.Write((uint)16);
		bw.Write((ushort)Fmt.FormatID);
		bw.Write(Fmt.Channel);
		bw.Write(Fmt.SampleRate);
		bw.Write(Fmt.BytesPerSecond);
		bw.Write(Fmt.BlockSize);
		bw.Write(Fmt.BitsPerSample);

		bw.Write(SIGN_DATA);
		bw.Write((uint)0);
	}
}

public class WavReader : RiffWav {
	public delegate void Reader();

	public Reader Read;

	public double[] Values { get; private set; }

	BinaryReader mBr;

	public WavReader(string filePath) {
		mFs = new FileStream(filePath, FileMode.Open);
		mBr = new BinaryReader(mFs);
		if (!ReadHeader()) {
			Read = ReadInvalid;
			return;
		}
		SetReader();
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
				Fmt.FormatID = (FMT.TYPE)mBr.ReadUInt16();
				Fmt.Channel = mBr.ReadUInt16();
				Fmt.SampleRate = mBr.ReadUInt32();
				Fmt.BytesPerSecond = mBr.ReadUInt32();
				Fmt.BlockSize = mBr.ReadUInt16();
				Fmt.BitsPerSample = mBr.ReadUInt16();
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

		switch (Fmt.FormatID) {
		case FMT.TYPE.PCM_INT:
			switch (Fmt.BitsPerSample) {
			case 8:
			case 16:
			case 24:
			case 32:
				break;
			default:
				return false;
			}
			break;
		case FMT.TYPE.PCM_FLOAT:
			switch (Fmt.BitsPerSample) {
			case 32:
				break;
			default:
				return false;
			}
			break;
		default:
			return false;
		}

		Samples = mDataSize / Fmt.BlockSize;
		mFs.Seek(mDataBegin, SeekOrigin.Begin);
		return true;
	}

	void SetReader() {
		switch (Fmt.Channel) {
		case 1:
			switch (Fmt.BitsPerSample) {
			case 8:
				Read = Read8M;
				break;
			case 16:
				Read = Read16M;
				break;
			case 24:
				Read = Read24M;
				break;
			case 32:
				if (Fmt.FormatID == FMT.TYPE.PCM_FLOAT) {
					Read = Read32fM;
				}
				else {
					Read = Read32M;
				}
				break;
			default:
				Read = ReadInvalid;
				break;
			}
			break;
		case 2:
			switch (Fmt.BitsPerSample) {
			case 8:
				Read = Read8S;
				break;
			case 16:
				Read = Read16S;
				break;
			case 24:
				Read = Read24S;
				break;
			case 32:
				if (Fmt.FormatID == FMT.TYPE.PCM_FLOAT) {
					Read = Read32fS;
				}
				else {
					Read = Read32S;
				}
				break;
			default:
				Read = ReadInvalid;
				break;
			}
			break;
		default:
			Read = ReadInvalid;
			break;
		}
		Values = new double[Fmt.Channel];
	}

	void ReadInvalid() { }
	void Read8M() {
		Values[0] = (mBr.ReadByte() - 128) / 128.0;
	}
	void Read8S() {
		Values[0] = (mBr.ReadByte() - 128) / 128.0;
		Values[1] = (mBr.ReadByte() - 128) / 128.0;
	}
	void Read16M() {
		Values[0] = mBr.ReadInt16() / 32768.0;
	}
	void Read16S() {
		Values[0] = mBr.ReadInt16() / 32768.0;
		Values[1] = mBr.ReadInt16() / 32768.0;
	}
	void Read24M() {
		var temp = ((uint)mBr.ReadByte() << 8) | ((uint)mBr.ReadUInt16() << 16);
		Values[0] = (double)(int)temp / (1 << 31);
	}
	void Read24S() {
		var tempL = ((uint)mBr.ReadByte() << 8) | ((uint)mBr.ReadUInt16() << 16);
		var tempR = ((uint)mBr.ReadByte() << 8) | ((uint)mBr.ReadUInt16() << 16);
		Values[0] = (double)(int)tempL / (1 << 31);
		Values[1] = (double)(int)tempR / (1 << 31);
	}
	void Read32M() {
		Values[0] = (double)mBr.ReadInt32() / (1 << 31);
	}
	void Read32S() {
		Values[0] = (double)mBr.ReadInt32() / (1 << 31);
		Values[1] = (double)mBr.ReadInt32() / (1 << 31);
	}
	void Read32fM() {
		Values[0] = mBr.ReadSingle();
	}
	void Read32fS() {
		Values[0] = mBr.ReadSingle();
		Values[1] = mBr.ReadSingle();
	}
}

public class WavWriter : RiffWav {
	BinaryWriter mBw;

	public WavWriter(string filePath, int sampleRate, int ch, int bits, bool enableFloat) {
		Fmt.FormatID = enableFloat ? FMT.TYPE.PCM_FLOAT : FMT.TYPE.PCM_INT;
		Fmt.Channel = (ushort)ch;
		Fmt.SampleRate = (uint)sampleRate;
		Fmt.BitsPerSample = (ushort)(enableFloat ? 32 : bits);
		Fmt.BlockSize = (ushort)(Fmt.Channel * Fmt.BitsPerSample >> 3);
		Fmt.BytesPerSecond = Fmt.BlockSize * Fmt.SampleRate;

		mFs = new FileStream(filePath, FileMode.OpenOrCreate);
		mBw = new BinaryWriter(mFs);
		WriteHeader(mBw);
		mDataBegin = mFs.Position;
		Samples = 0;
	}

	public void Save() {
		var currentPos = mFs.Position;

		mFs.Seek(4, SeekOrigin.Begin);
		mBw.Write((uint)(Fmt.BlockSize * Samples + 36));

		mFs.Seek(mDataBegin - 4, SeekOrigin.Begin);
		mBw.Write((uint)(Fmt.BlockSize * Samples));

		mFs.Seek(currentPos, SeekOrigin.Begin);
	}

	public void WriteBuffer(byte[] buffer, int begin, int samples) {
		mBw.Write(buffer, begin * Fmt.BlockSize, samples * Fmt.BlockSize);
		Samples += samples;
	}
}
