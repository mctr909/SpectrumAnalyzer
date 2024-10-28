namespace Spectrum {
	public static class Settings {
		/// <summary>半音数</summary>
		public const int NOTE_COUNT = 126;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 4;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;

		/// <summary>低域最大周波数(80Hz)</summary>
		internal const int LOW_FREQ_MAX = 80;
		/// <summary>中域最小周波数(300Hz)</summary>
		internal const int MID_FREQ_MIN = 300;
		/// <summary>低域閾値幅(±4半音)</summary>
		internal const int THRESHOLD_WIDTH_LOW = HALFTONE_DIV * 4;
		/// <summary>中高域閾値幅(±1半音)</summary>
		internal const int THRESHOLD_WIDTH_MID = HALFTONE_DIV;
		/// <summary>低域閾値ゲイン(+1.0db)</summary>
		internal const double THRESHOLD_GAIN_LOW = 1.122;
		/// <summary>中高域閾値ゲイン(+0.0db)</summary>
		internal const double THRESHOLD_GAIN_MID = 1.000;
		/// <summary>フィルタ遮断幅が1半音に至る周波数(440Hz)</summary>
		internal const double HALFTONE_WIDTH_AT_FREQ = 440.0;
		/// <summary>表示応答周波数(60Hz)</summary>
		internal const double DISP_FREQ = 60;
		/// <summary>自動調整最大ゲイン(+24db)</summary>
		internal const double AUTOGAIN_MAX = 3.981e-03; // 24db {10^(-24/10)}
		/// <summary>波形合成デクリック速度(中高音域)</summary>
		internal const double DECLICK_MID_SPEED = 0.25;
		/// <summary>波形合成デクリック速度(低音域)</summary>
		internal const double DECLICK_LOW_SPEED = 0.1;
		/// <summary>波形合成デクリック中音域定義</summary>
		internal const int DECLICK_MID_TONE = 30;
		/// <summary>フィルタバンク数</summary>
		internal const int BANK_COUNT = NOTE_COUNT * HALFTONE_DIV;

		public static bool AutoGain = true;
		public static bool NormGain = false;
		public static double Pitch = 1.0;
		public static double Transpose = 0.0;
	}
}
