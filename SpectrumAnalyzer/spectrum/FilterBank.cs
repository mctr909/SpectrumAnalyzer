using System.Runtime.InteropServices;

namespace Spectrum {
	[StructLayout(LayoutKind.Sequential)]
	internal struct FilterBank {
		public double KB0;
		public double KA2;
		public double KA1;
		public double SIGMA;

		public double lb2;
		public double lb1;
		public double la2;
		public double la1;
		public double l;

		public double rb2;
		public double rb1;
		public double ra2;
		public double ra1;
		public double r;
	}
}
