namespace Spectrum {
	public partial class Spectrum {
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 120;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 4;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;
		/// <summary>フィルタバンク数</summary>
		public const int BANK_COUNT = HALFTONE_COUNT * HALFTONE_DIV;
		/// <summary>フィルタバンク中での半音の中間位置</summary>
		public const int HALFTONE_CENTER = HALFTONE_DIV / 2;

		public static bool EnableAutoGain = true;
		public static bool EnableNormalize = false;

		/// <summary>ゲイン自動調整 最大[10^-(db/20)]</summary>
		const double AUTOGAIN_MAX = 6.31e-02;
		/// <summary>ゲイン自動調整 速度[秒]</summary>
		const double AUTOGAIN_SPEED = 6.0;

		/// <summary>フィルタ遮断幅が1半音に至る周波数[Hz]</summary>
		const double FREQ_AT_HALFTONE_WIDTH = 700.0;

		/// <summary>低音域 終了バンク</summary>
		const int END_LOW_BANK = HALFTONE_DIV * 36;
		/// <summary>低音域 閾値幅[フィルタバンク数]</summary>
		const int THRESHOLD_WIDTH_LOW = HALFTONE_DIV * 9;
		/// <summary>低音域 閾値ゲイン[10^(db/20)]</summary>
		const double THRESHOLD_GAIN_LOW = 1.122;
		/// <summary>中高音域 開始バンク</summary>
		const int BEGIN_MID_BANK = HALFTONE_DIV * 48;
		/// <summary>中高音域 閾値幅[フィルタバンク数]</summary>
		const int THRESHOLD_WIDTH_MID = HALFTONE_DIV * 2;
		/// <summary>中高音域 閾値ゲイン[10^(db/20)]</summary>
		const double THRESHOLD_GAIN_MID = 1.0;
	}
}
