using System.Runtime.InteropServices;

namespace Spectrum {
	[StructLayout(LayoutKind.Sequential)]
	internal struct FilterBank {
		public double KB0;
		public double KA2;
		public double KA1;
		public double SIGMA;
		public double SIGMA_DISP;

		public double Lb2;
		public double Lb1;
		public double La2;
		public double La1;
		public double LPower;
		public double LPowerDisp;
		public double Rb2;
		public double Rb1;
		public double Ra2;
		public double Ra1;
		public double RPower;
		public double RPowerDisp;
	}
}
