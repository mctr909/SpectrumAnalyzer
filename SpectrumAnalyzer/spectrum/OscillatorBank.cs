using System.Runtime.InteropServices;

namespace Spectrum {
	[StructLayout(LayoutKind.Sequential)]
	internal struct OscillatorBank {
		public double Delta;
		public double Phase;
		public double L;
		public double DeclickedL;
		public double R;
		public double DeclickedR;
	}
}
