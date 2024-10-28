namespace Spectrum {
	public partial class Spectrum {
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 125;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 4;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;
		/// <summary>フィルタバンク数</summary>
		public const int BANK_COUNT = HALFTONE_COUNT * HALFTONE_DIV;
		/// <summary>フィルタバンク中での半音の中間位置</summary>
		public const int HALFTONE_CENTER = HALFTONE_DIV / 2;

		/// <summary>表示応答速度[Hz]</summary>
		const double DISP_SPEED = 90;
		/// <summary>フィルタ遮断幅が1半音に至る周波数[Hz]</summary>
		const double FREQ_AT_HALFTONE_WIDTH = 660.0;

		/// <summary>低音域 終了バンク</summary>
		const int END_LOW_BANK = HALFTONE_DIV * 32;
		/// <summary>低音域 閾値幅[フィルタバンク数]</summary>
		const int THRESHOLD_WIDTH_LOW = HALFTONE_DIV * 8;
		/// <summary>低音域 閾値ゲイン[10^(db/20)]</summary>
		const double THRESHOLD_GAIN_LOW = 1.122;
		/// <summary>中高音域 開始バンク</summary>
		const int BEGIN_MID_BANK = HALFTONE_DIV * 44;
		/// <summary>中高音域 閾値幅[フィルタバンク数]</summary>
		const int THRESHOLD_WIDTH_MID = HALFTONE_DIV * 3 / 2;
		/// <summary>中高音域 閾値ゲイン[10^(db/20)]</summary>
		const double THRESHOLD_GAIN_MID = 1.035;
	}
}
