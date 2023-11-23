using System;
using System.IO;

public class RiffWAV
{
	public struct RiffChunk
	{
		public uint RIFF;
		public uint FileSize;
		public uint WaveSign;
	}

	public struct fmtChunk
	{
		public uint fmtSign;
		public uint fmtSize;
		public ushort FormatID;
		public ushort Channel;
		public uint SamplingFrequency;
		public uint BytePerSecond;
		public ushort BlockSize;
		public ushort BitPerSample;
	}

	public struct DataChunk
	{
		public uint DataSign;
		public uint DataSize;
	}

	public struct ChunkSign
	{
		public const uint RIFF = 0x46464952;
		public const uint WAVE = 0x45564157;
		public const uint FMT = 0x20746d66;
		public const ushort PCM = 1;
		public const uint DATA = 0x61746164;
	}

	public struct ChunkSize
	{
		public const int ALL = 44;
		public const int RIFF = 12;
		public const int FMT = 24;
		public const int DATA = 8;
	}

	private FileStream fs;
	BinaryReader br;
	BinaryWriter bw;
	private uint samples;

	public RiffChunk riff;
	public fmtChunk fmt;
	public DataChunk data;

	public RiffWAV(string strFileName, bool WriteFlag)
	{
		if (WriteFlag) {
			fs = new FileStream(strFileName, FileMode.OpenOrCreate);
			bw = new BinaryWriter(fs);

			bw.Write(new byte[ChunkSize.ALL]);
			samples = 0;
		}
		else {
			fs = new FileStream(strFileName, FileMode.Open);
			br = new BinaryReader(fs);

			riff.RIFF = br.ReadUInt32();
			riff.FileSize = br.ReadUInt32();
			riff.WaveSign = br.ReadUInt32();

			if (riff.RIFF != ChunkSign.RIFF)
				return;
			if (riff.WaveSign != ChunkSign.WAVE)
				return;

			fmt.fmtSign = br.ReadUInt32();
			fmt.fmtSize = br.ReadUInt32();
			fmt.FormatID = br.ReadUInt16();
			fmt.Channel = br.ReadUInt16();
			fmt.SamplingFrequency = br.ReadUInt32();
			fmt.BytePerSecond = br.ReadUInt32();
			fmt.BlockSize = br.ReadUInt16();
			fmt.BitPerSample = br.ReadUInt16();

			if (fmt.fmtSign != ChunkSign.FMT)
				return;
			if (fmt.FormatID != ChunkSign.PCM)
				return;

			if (fmt.fmtSize > 16)
				fs.Seek(fmt.fmtSize - 16, SeekOrigin.Current);
			else if (fmt.fmtSize < 16)
				return;

			data.DataSign = br.ReadUInt32();
			data.DataSize = br.ReadUInt32();

			if (data.DataSign != ChunkSign.DATA)
				return;
		}
	}

	public void Save(uint samplerate, ushort ch, ushort bits)
	{
		fs.Seek(0, SeekOrigin.Begin);

		riff.RIFF = ChunkSign.RIFF;
		riff.FileSize = (uint)(ch * bits * samples / 8 + 36);
		riff.WaveSign = ChunkSign.WAVE;

		fmt.fmtSign = ChunkSign.FMT;
		fmt.fmtSize = 16;
		fmt.FormatID = ChunkSign.PCM;
		fmt.Channel = ch;
		fmt.SamplingFrequency = samplerate;
		fmt.BytePerSecond = (uint)(ch * bits * samplerate / 8);
		fmt.BlockSize = (ushort)(ch * bits / 8);
		fmt.BitPerSample = bits;

		data.DataSign = ChunkSign.DATA;
		data.DataSize = (uint)(ch * bits * samples / 8);

		bw.Write(riff.RIFF);
		bw.Write(riff.FileSize);
		bw.Write(riff.WaveSign);

		bw.Write(fmt.fmtSign);
		bw.Write(fmt.fmtSize);
		bw.Write(fmt.FormatID);
		bw.Write(fmt.Channel);
		bw.Write(fmt.SamplingFrequency);
		bw.Write(fmt.BytePerSecond);
		bw.Write(fmt.BlockSize);
		bw.Write(fmt.BitPerSample);

		bw.Write(data.DataSign);
		bw.Write(data.DataSize);

		fs.Close();
	}

	public void read16(ref short left, ref short right)
	{
		left = br.ReadInt16();
		right = br.ReadInt16();
	}

	public void read16(ref short mono)
	{
		mono = br.ReadInt16();
	}

	public void read8(ref short left, ref short right)
	{
		left = (short)(br.ReadByte() - 128);
		right = (short)(br.ReadByte() - 128);
	}

	public void read8(ref short mono)
	{
		mono = (short)(br.ReadByte() - 128);
	}

	public void write16(short left, short right)
	{
		bw.Write(left);
		bw.Write(right);
		samples++;
	}

	public void write16(short mono)
	{
		bw.Write(mono);
		samples++;
	}

	public void close()
	{
		fs.Close();
		fs.Dispose();
	}
}