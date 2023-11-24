class Record : WaveIn {
	public Spectrum FilterBank;

	public bool Enabled { get; set; }

	public Record(int sampleRate, int bufferSize, int notes, double baseFreq) : base(sampleRate, 1, bufferSize) {
		FilterBank = new Spectrum(sampleRate, baseFreq, notes);
	}

	protected override void SetData() {
		if (Enabled) {
			FilterBank.SetLevel(mBuffer);
		}
	}
}
