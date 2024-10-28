using System.Runtime.InteropServices;

namespace Spectrum {
	[StructLayout(LayoutKind.Sequential)]
	internal struct OscillatorBank {
		public double Delta;
		public double Phase;
		public double LTarget;
		public double LCurrent;
		public double RTarget;
		public double RCurrent;
	}
}
