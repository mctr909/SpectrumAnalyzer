namespace Spectrum {
	internal class PeakBank {
		public readonly double DELTA;
		public double L;
		public double R;

		PeakBank() { }
		public PeakBank(double delta) {
			DELTA = delta;
		}
	}
}
