public class Record : WaveIn {
	public Spectrum FilterBank;

	public Record(int sampleRate, int bufferSize, int notes, double baseFreq) : base(sampleRate, 1, bufferSize) {
		FilterBank = new Spectrum(sampleRate, baseFreq, notes);
	}

	protected override void SetData() {
		FilterBank.SetLevel(mBuffer);
	}
}
