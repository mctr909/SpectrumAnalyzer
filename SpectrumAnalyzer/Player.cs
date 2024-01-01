using System;
using System.Runtime.InteropServices;

namespace SpectrumAnalyzer {
	internal class Player : IDisposable {
		#region dll
		[DllImport("SignalProcessor.dll")]
		static extern void player_create(ref IntPtr hwo, uint device_id, int sample_rate, int buffer_samples, int buffer_count);
		[DllImport("SignalProcessor.dll")]
		static extern void player_dispose(ref IntPtr hwo);
		[DllImport("SignalProcessor.dll")]
		static extern void player_select_device(ref IntPtr hwo, uint device_id);
		[DllImport("SignalProcessor.dll")]
		static extern void player_start(IntPtr hwo);
		[DllImport("SignalProcessor.dll")]
		static extern void player_pause(IntPtr hwo);
		#endregion

		IntPtr hwo;

		public Player() {
			player_create(ref hwo, 0, 44100, 512, 16);
			player_start(hwo);
		}

		public void Dispose() {
			player_dispose(ref hwo);
		}

		public void SelectDevice(uint deviceId) {
			player_select_device(ref hwo, deviceId);
		}

		public void Start() {
			player_start(hwo);
		}

		public void Pause() {
			player_pause(hwo);
		}
	}
}
