namespace Spectrum {
	public partial class WaveSynth {
		/// <summary>対象閾値</summary>
		const double TERGET_THRESHOLD = 1.0 / 1000;
		/// <summary>デクリック速度</summary>
		const double DECLICK_SPEED = 0.05;
		/// <summary>正弦波テーブルの長さ</summary>
		const int SIN_TABLE_LENGTH = 48;
		/// <summary>正弦波テーブル</summary>
		static readonly double[] SIN_TABLE = {
			 0.0000, 0.1305, 0.2588, 0.3827, 0.5000, 0.6088, 0.7071, 0.7934, 0.8660, 0.9239, 0.9659, 0.9914,
			 1.0000, 0.9914, 0.9659, 0.9239, 0.8660, 0.7934, 0.7071, 0.6088, 0.5000, 0.3827, 0.2588, 0.1305,
			 0.0000,-0.1305,-0.2588,-0.3827,-0.5000,-0.6088,-0.7071,-0.7934,-0.8660,-0.9239,-0.9659,-0.9914,
			-1.0000,-0.9914,-0.9659,-0.9239,-0.8660,-0.7934,-0.7071,-0.6088,-0.5000,-0.3827,-0.2588,-0.1305,
			 0.0000
		};
	}
}
