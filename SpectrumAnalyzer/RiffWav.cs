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

	public FMT Fmt;
	public long Samples { get; protected set; } = 0;
	public int Position { get; protected set; } = 0;

	protected FileStream mFs;
	protected long mDataSize;
	protected long mDataBegin;

	public virtual void Dispose() {
		mFs.Close();
		mFs.Dispose();
	}

	public void Seek(int samples) {
		mFs.Seek(Fmt.BlockSize * samples, SeekOrigin.Current);
		Position += samples;
	}

	public void SeekBegin(int samples) {
		mFs.Seek(Fmt.BlockSize * samples, SeekOrigin.Begin);
		Position = samples;
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
	public readonly int BufferSamples;
	public IntPtr Buffer = IntPtr.Zero;

	BinaryReader mBr;
	byte[] mMuteData;

	public WavReader(string filePath, int bufferSamples) {
		BufferSamples = bufferSamples;
		mFs = new FileStream(filePath, FileMode.Open);
		mBr = new BinaryReader(mFs);
		if (!ReadHeader()) {
			return;
		}
		Buffer = Marshal.AllocHGlobal(Fmt.BlockSize * bufferSamples);
		mMuteData = new byte[Fmt.BlockSize * bufferSamples];
		SetBuffer(0);
	}

	public override void Dispose() {
		base.Dispose();
		Marshal.FreeHGlobal(Buffer);
	}

	public void SetBuffer(int offset) {
		SeekBegin(offset);
		if (Samples - Position <= BufferSamples) {
			var readSize = (int)(Samples - Position) * Fmt.BlockSize;
			Marshal.Copy(mMuteData, 0, Buffer, mMuteData.Length);
			Marshal.Copy(mBr.ReadBytes(readSize), 0, Buffer, readSize);
			Position = (int)Samples;
		}
		else {
			var readSize = (BufferSamples + 1) * Fmt.BlockSize;
			Marshal.Copy(mBr.ReadBytes(readSize), 0, Buffer, readSize);
			Seek(-1);
			Position += BufferSamples;
		}
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
