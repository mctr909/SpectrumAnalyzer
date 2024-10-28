using System.Runtime.InteropServices;

namespace Spectrum {
	[StructLayout(LayoutKind.Sequential)]
	internal struct OscillatorBank {
		public double delta;
		public double phase;
		public double amp_l;
		public double amp_r;
		public double declicked_l;
		public double declicked_r;
	}
}
