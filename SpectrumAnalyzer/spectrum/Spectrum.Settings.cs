using System;

namespace Spectrum {
	public partial class Spectrum {
		/// <summary>半音数</summary>
		public const int HALFTONE_COUNT = 126;
		/// <summary>半音分割数</summary>
		public const int HALFTONE_DIV = 3;
		/// <summary>オクターブ分割数</summary>
		public const int OCT_DIV = HALFTONE_DIV * 12;
		/// <summary>フィルタバンク数</summary>
		public const int BANK_COUNT = HALFTONE_COUNT * HALFTONE_DIV;

		/// <summary>C0基本周波数[Hz]</summary>
		public static readonly double BASE_FREQ = 442 * Math.Pow(2, 3.0 / 12.0 + (1.0 / HALFTONE_DIV - 1) / 12.0 - 5);

		public static bool EnableAutoGain = true;
		public static bool EnableNormalize = false;

		/// <summary>ゲイン自動調整 最小値</summary>
		const double AUTOGAIN_MIN = 1e-2;
		/// <summary>ゲイン自動調整 減少時間[秒]</summary>
		const double AUTOGAIN_TIME_DOWN = 3.0;
		/// <summary>ゲイン自動調整 増加時間[秒]</summary>
		const double AUTOGAIN_TIME_UP = 0.01;

		/// <summary>フィルタ帯域幅に至る周波数[Hz]</summary>
		const double FREQ_AT_BANDWIDTH = 300.0;

		/// <summary>中音域 開始位置[フィルタバンク数]</summary>
		const int BEGIN_MID = HALFTONE_DIV * 30;
		/// <summary>高音域 開始位置[フィルタバンク数]</summary>
		const int BEGIN_HIGH = HALFTONE_DIV * 48;
		/// <summary>低音域 閾値幅[フィルタバンク数]</summary>
		const int THRESHOLD_WIDTH_LOW = HALFTONE_DIV * 11 / 3;
		/// <summary>高音域 閾値幅[フィルタバンク数]</summary>
		const int THRESHOLD_WIDTH_HIGH = HALFTONE_DIV * 2 / 3;
	}
}
