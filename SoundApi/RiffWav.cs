namespace SoundApi;

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

	public FMT Format = new() {
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
