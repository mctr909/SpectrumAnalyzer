namespace Spectrum {
	public static class Settings {
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 126;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 4;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;
		/// <summary>フィルタバンク数</summary>
		public const int BANK_COUNT = HALFTONE_COUNT * HALFTONE_DIV;
		/// <summary>フィルタバンク中での半音の中間位置</summary>
		public const int HALFTONE_CENTER = HALFTONE_DIV / 2;

		/// <summary>表示応答速度[Hz]</summary>
		internal const double DISP_SPEED = 60;
		/// <summary>フィルタ遮断幅が1半音に至る周波数[Hz]</summary>
		internal const double FREQ_AT_HALFTONE_WIDTH = 440.0;

		/// <summary>ゲイン自動調整 最大[10^-(db/10)]</summary>
		internal const double AUTOGAIN_MAX = 3.981e-03;
		/// <summary>ゲイン自動調整 速度[秒]</summary>
		internal const double AUTOGAIN_SPEED = 0.33;

		/// <summary>低音域 終了周波数[Hz]</summary>
		internal const int END_LOW_FREQ = 80;
		/// <summary>低音域 閾値幅[フィルタバンク数]</summary>
		internal const int THRESHOLD_WIDTH_LOW = HALFTONE_DIV * 4;
		/// <summary>低音域 閾値ゲイン[10^(db/20)]</summary>
		internal const double THRESHOLD_GAIN_LOW = 1.122;
		/// <summary>中高音域 開始周波数[Hz]</summary>
		internal const int BEGIN_MID_FREQ = 300;
		/// <summary>中高音域 閾値幅[フィルタバンク数]</summary>
		internal const int THRESHOLD_WIDTH_MID = HALFTONE_DIV;
		/// <summary>中高音域 閾値ゲイン[10^(db/20)]</summary>
		internal const double THRESHOLD_GAIN_MID = 1.0;

		/// <summary>波形合成 閾値[10^(db/20)]</summary>
		internal const double SYNTH_THRESHOLD = 0.0001;
		/// <summary>波形合成 低音域デクリック速度</summary>
		internal const double SYNTH_DECLICK_LOW_SPEED = 0.125;
		/// <summary>波形合成 中高音域デクリック速度</summary>
		internal const double SYNTH_DECLICK_MID_SPEED = 0.25;
		/// <summary>波形合成 中高音域開始[半音]</summary>
		internal const int SYNTH_BEGIN_MID_TONE = 36;

		public static bool AutoGain = true;
		public static bool NormGain = false;
	}
}
