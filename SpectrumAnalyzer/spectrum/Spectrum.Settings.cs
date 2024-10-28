using System;

namespace Spectrum {
	public partial class Spectrum {
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 120;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 3;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;
		/// <summary>フィルタバンク数</summary>
		public const int BANK_COUNT = HALFTONE_COUNT * HALFTONE_DIV;
		/// <summary>フィルタバンクでの半音の中間位置</summary>
		public const int HALFTONE_CENTER = HALFTONE_DIV / 2;

		/// <summary>C0基本周波数[Hz]</summary>
		public static readonly double BASE_FREQ = 442 * Math.Pow(2, HALFTONE_CENTER / OCT_DIV + 3 / 12.0 - 5);

		public static bool EnableAutoGain = true;
		public static bool EnableNormalize = false;

		/// <summary>ゲイン自動調整 最小値</summary>
		const double AUTOGAIN_MIN = 1.0 / 100;
		/// <summary>ゲイン自動調整 減少時間[秒]</summary>
		const double AUTOGAIN_TIME_DOWN = 4.0;
		/// <summary>ゲイン自動調整 増加時間[秒]</summary>
		const double AUTOGAIN_TIME_UP = 0.1;

		/// <summary>フィルタ帯域幅に至る周波数[Hz]</summary>
		const double FREQ_AT_BANDWIDTH = 400.0;

		/// <summary>中音域 開始位置[フィルタバンク数]</summary>
		const int BEGIN_MID = HALFTONE_DIV * 36;
		/// <summary>高音域 開始位置[フィルタバンク数]</summary>
		const int BEGIN_HIGH = HALFTONE_DIV * 60;
		/// <summary>低音域 閾値幅[フィルタバンク数]</summary>
		const int THRESHOLD_WIDTH_LOW = HALFTONE_DIV * 6;
		/// <summary>高音域 閾値幅[フィルタバンク数]</summary>
		const int THRESHOLD_WIDTH_HIGH = HALFTONE_DIV;
		/// <summary>閾値ゲイン</summary>
		const double THRESHOLD_GAIN = 1.0;
	}
}
