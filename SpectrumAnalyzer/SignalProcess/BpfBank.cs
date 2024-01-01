using System.Runtime.InteropServices;

namespace SignalProcess {
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
	public struct BpfBank {
		public float La1;
		public float La2;
		public float Lb1;
		public float Lb2;

		public float Ra1;
		public float Ra2;
		public float Rb1;
		public float Rb2;

		public float PowerL;
		public float PowerR;
		public float PeakL;
		public float PeakR;

		public float Ka1;
		public float Ka2;
		public float Kb0;
		public float RmsSpeed;
	}
}
