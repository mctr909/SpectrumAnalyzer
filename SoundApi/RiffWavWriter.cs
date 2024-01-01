namespace SoundApi;

public class RiffWavWriter : RiffWav {
	BinaryWriter Bw;

	RiffWavWriter() { }

	public RiffWavWriter(string filePath, int sampleRate, int ch, int bits, bool enableFloat) {
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
