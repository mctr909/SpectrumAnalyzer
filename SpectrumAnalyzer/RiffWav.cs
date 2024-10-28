using System;
using System.IO;
using System.Runtime.InteropServices;

public abstract class RiffWav : IDisposable {
	protected enum SIGN : uint {
		RIFF = 0x46464952,
		WAVE = 0x45564157,
		fmt_ = 0x20746d66,
		data = 0x61746164
	}

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

	public FMT Format = new FMT() {
		FormatID = FMT.TYPE.PCM_INT,
		Channel = 2,
		SampleRate = 44100,
		BitsPerSample = 16
	};
	public int SampleCount { get; protected set; } = 0;
	public int Cursor { get; protected set; } = 0;

	protected FileStream File;
	protected long DataSize;
	protected long DataBegin;

	public virtual void Dispose() {
		if (null == File) {
			return;
		}
		File.Close();
		File.Dispose();
	}

	public void SeekCurrent(int samples) {
		File.Seek(Format.BlockSize * samples, SeekOrigin.Current);
		Cursor += samples;
	}

	public void SeekBegin(int samples) {
		File.Seek(DataBegin + Format.BlockSize * samples, SeekOrigin.Begin);
		Cursor = samples;
	}
}

public class WavReader : RiffWav {
	public readonly bool IsOpened;

	public double Position { get; set; } = 0;
	public double Speed { get; set; } = 1.0;

	public delegate void DRead(IntPtr output);
	public DRead Read = (p) => { };

	readonly int OUTPUT_SAMPLES;
	readonly int BUFFER_SAMPLES;
	readonly int BUFFER_SIZE;
	readonly double DELTA;

	BinaryReader Br;
	byte[] MuteData;
	IntPtr Buffer;
	int BufferOffset;

	const float SCALE_8BIT = 1.0f / (1<<7);
	const float SCALE_16BIT = 1.0f / (1<<15);
	const float SCALE_32BIT = 1.0f / (1<<31);

	public WavReader() {
		SampleCount = 1;
	}

	public WavReader(string filePath, int sampleRate = 44100, int outputSamples = 1024, double bufferUnitSec = 0.1) {
		IsOpened = false;
		try {
			File = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		}
		catch {
			return;
		}
		Br = new BinaryReader(File);
		if (!LoadHeader()) {
			return;
		}
		if (!CheckFormat()) {
			return;
		}
		SampleCount = (int)(DataSize / Format.BlockSize);
		OUTPUT_SAMPLES = outputSamples;
		BUFFER_SAMPLES = (int)(Format.SampleRate * bufferUnitSec);
		BUFFER_SIZE = Format.BlockSize * (BUFFER_SAMPLES + 1);
		DELTA = (double)Format.SampleRate / sampleRate;
		Buffer = Marshal.AllocHGlobal(BUFFER_SIZE);
		MuteData = new byte[BUFFER_SIZE];
		if (Format.BitsPerSample == 8) {
			for (int i = 0; i < BUFFER_SIZE; ++i) {
				MuteData[i] = 128;
			}
		}
		BufferOffset = 0;
		LoadData();
		IsOpened = true;
	}

	public override void Dispose() {
		base.Dispose();
		Marshal.FreeHGlobal(Buffer);
	}

	public bool CheckFormat() {
		switch (Format.FormatID) {
		case FMT.TYPE.PCM_INT:
			switch (Format.BitsPerSample) {
			case 8:
				switch (Format.Channel) {
				case 1:
					Read = ReadI8M;
					break;
				case 2:
					Read = ReadI8S;
					break;
				default:
					return false;
				}
				break;
			case 16:
				switch (Format.Channel) {
				case 1:
					Read = ReadI16M;
					break;
				case 2:
					Read = ReadI16S;
					break;
				default:
					return false;
				}
				break;
			case 24:
				switch (Format.Channel) {
				case 1:
					Read = ReadI24M;
					break;
				case 2:
					Read = ReadI24S;
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
					Read = ReadF32M;
					break;
				case 2:
					Read = ReadF32S;
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

	bool LoadHeader() {
		if (File.Length < 12)
			return false;

		var fileSign = (SIGN)Br.ReadUInt32();
		if (fileSign != SIGN.RIFF)
			return false;

		var fileSize = Br.ReadUInt32();
		var fileType = (SIGN)Br.ReadUInt32();
		if (fileType != SIGN.WAVE)
			return false;

		SIGN chunkSign;
		uint chunkSize;
		while (File.Position < fileSize) {
			chunkSign = (SIGN)Br.ReadUInt32();
			chunkSize = Br.ReadUInt32();
			switch (chunkSign) {
			case SIGN.fmt_:
				if (chunkSize < 16)
					return false;
				Format.FormatID = (FMT.TYPE)Br.ReadUInt16();
				Format.Channel = Br.ReadUInt16();
				Format.SampleRate = Br.ReadUInt32();
				Format.BytesPerSecond = Br.ReadUInt32();
				Format.BlockSize = Br.ReadUInt16();
				Format.BitsPerSample = Br.ReadUInt16();
				if (chunkSize > 16)
					File.Seek(chunkSize - 16, SeekOrigin.Current);
				break;
			case SIGN.data:
				DataSize = chunkSize;
				DataBegin = File.Position;
				File.Seek(chunkSize, SeekOrigin.Current);
				break;
			default:
				File.Seek(chunkSize, SeekOrigin.Current);
				break;
			}
		}

		File.Seek(DataBegin, SeekOrigin.Begin);
		return true;
	}

	void LoadData() {
		var diffPos = Position - BufferOffset;
		BufferOffset += (int)(diffPos + Math.Sign(diffPos - (int)diffPos));
		SeekBegin(BufferOffset);
		if (SampleCount - Cursor <= BUFFER_SAMPLES) {
			var readSize = (SampleCount - Cursor) * Format.BlockSize;
			Marshal.Copy(MuteData, 0, Buffer, BUFFER_SIZE);
			Marshal.Copy(Br.ReadBytes(readSize), 0, Buffer, readSize);
			Cursor = SampleCount;
		}
		else {
			Marshal.Copy(Br.ReadBytes(BUFFER_SIZE), 0, Buffer, BUFFER_SIZE);
			File.Seek(Format.BlockSize * -1, SeekOrigin.Current);
			Cursor += BUFFER_SAMPLES;
		}
	}

	unsafe void ReadI8M(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (byte*)Buffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < SampleCount; ++s, Position += DELTA * Speed) {
			var remain = Position - BufferOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				LoadData();
			}
			var indexD = Position - BufferOffset;
			var indexI = (int)indexD;
			var b = (float)(indexD - indexI) * SCALE_8BIT;
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
	unsafe void ReadI8S(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (byte*)Buffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < SampleCount; ++s, Position += DELTA * Speed) {
			var remain = Position - BufferOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				LoadData();
			}
			var indexD = Position - BufferOffset;
			var indexI = (int)indexD;
			var b = (float)(indexD - indexI) * SCALE_8BIT;
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
	unsafe void ReadI16M(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (short*)Buffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < SampleCount; ++s, Position += DELTA * Speed) {
			var remain = Position - BufferOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				LoadData();
			}
			var indexD = Position - BufferOffset;
			var indexI = (int)indexD;
			var b = (float)(indexD - indexI) * SCALE_16BIT;
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
	unsafe void ReadI16S(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (short*)Buffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < SampleCount; ++s, Position += DELTA * Speed) {
			var remain = Position - BufferOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				LoadData();
			}
			var indexD = Position - BufferOffset;
			var indexI = (int)indexD;
			var b = (float)(indexD - indexI) * SCALE_16BIT;
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
	unsafe void ReadI24M(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (byte*)Buffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < SampleCount; ++s, Position += DELTA * Speed) {
			var remain = Position - BufferOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				LoadData();
			}
			var indexD = Position - BufferOffset;
			var indexI = (int)indexD;
			var b = (float)(indexD - indexI) * SCALE_32BIT;
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
	unsafe void ReadI24S(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (byte*)Buffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < SampleCount; ++s, Position += DELTA * Speed) {
			var remain = Position - BufferOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				LoadData();
			}
			var indexD = Position - BufferOffset;
			var indexI = (int)indexD;
			var b = (float)(indexD - indexI) * SCALE_32BIT;
			var a = SCALE_32BIT - b;
			var p = pInput + indexI * 6;
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
	unsafe void ReadF32M(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (float*)Buffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < SampleCount; ++s, Position += DELTA * Speed) {
			var remain = Position - BufferOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				LoadData();
			}
			var indexD = Position - BufferOffset;
			var indexI = (int)indexD;
			var b = (float)(indexD - indexI);
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
	unsafe void ReadF32S(IntPtr output) {
		var pOutput = (float*)output;
		var pInput = (float*)Buffer;
		int s;
		for (s = 0; s < OUTPUT_SAMPLES && Position < SampleCount; ++s, Position += DELTA * Speed) {
			var remain = Position - BufferOffset;
			if (remain >= BUFFER_SAMPLES || remain <= -1) {
				LoadData();
			}
			var indexD = Position - BufferOffset;
			var indexI = (int)indexD;
			var b = (float)(indexD - indexI);
			var a = 1 - b;
			var p = pInput + indexI * 2;
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
	BinaryWriter Bw;

	WavWriter() { }

	public WavWriter(string filePath, int sampleRate, int ch, int bits, bool enableFloat) {
		Format.FormatID = enableFloat ? FMT.TYPE.PCM_FLOAT : FMT.TYPE.PCM_INT;
		Format.Channel = (ushort)ch;
		Format.SampleRate = (uint)sampleRate;
		Format.BitsPerSample = (ushort)bits;
		Format.BlockSize = (ushort)(Format.Channel * Format.BitsPerSample >> 3);
		Format.BytesPerSecond = Format.BlockSize * Format.SampleRate;

		File = new FileStream(filePath, FileMode.OpenOrCreate);
		Bw = new BinaryWriter(File);
		WriteHeader();
		DataBegin = File.Position;
		SampleCount = 0;
	}

	public void Save() {
		var currentPos = File.Position;

		File.Seek(4, SeekOrigin.Begin);
		Bw.Write((uint)(Format.BlockSize * SampleCount + 36));

		File.Seek(DataBegin - 4, SeekOrigin.Begin);
		Bw.Write((uint)(Format.BlockSize * SampleCount));

		File.Seek(currentPos, SeekOrigin.Begin);
	}

	public void Write(byte[] buffer, int begin, int samples) {
		Bw.Write(buffer, begin * Format.BlockSize, samples * Format.BlockSize);
		SampleCount += samples;
	}

	protected void WriteHeader() {
		Bw.Write((uint)SIGN.RIFF);
		Bw.Write((uint)0);
		Bw.Write((uint)SIGN.WAVE);

		Bw.Write((uint)SIGN.fmt_);
		Bw.Write((uint)16);
		Bw.Write((ushort)Format.FormatID);
		Bw.Write(Format.Channel);
		Bw.Write(Format.SampleRate);
		Bw.Write(Format.BytesPerSecond);
		Bw.Write(Format.BlockSize);
		Bw.Write(Format.BitsPerSample);

		Bw.Write((uint)SIGN.data);
		Bw.Write((uint)0);
	}
}
