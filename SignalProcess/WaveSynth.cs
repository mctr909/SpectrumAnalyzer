namespace SignalProcess;
using static BaseSpectrum;

public class WaveSynth {
	/// <summary>ピッチ</summary>
	public double Pitch { get; set; } = 1.0;

	private readonly BaseSpectrum Spectrum;
	private readonly OscBank[] Banks = new OscBank[HALFTONE_COUNT];
	private readonly double[] Omega = new double[BankCount];
	private readonly float ClickDump;

	/// <summary>フィルタバンクにおける半音の中心インデックス</summary>
	private const int HALFTONE_CENTER = HALFTONE_DIV >> 1;

	private const float TARGET_RANGE = 1e-3f;
	private const int SIN_TABLE_LENGTH = 48;
	private static readonly double[] SIN_TABLE = {
			 0.0000, 0.1305, 0.2588, 0.3827, 0.5000, 0.6088,
			 0.7071, 0.7934, 0.8660, 0.9239, 0.9659, 0.9914,
			 1.0000, 0.9914, 0.9659, 0.9239, 0.8660, 0.7934,
			 0.7071, 0.6088, 0.5000, 0.3827, 0.2588, 0.1305,
			 0.0000,-0.1305,-0.2588,-0.3827,-0.5000,-0.6088,
			-0.7071,-0.7934,-0.8660,-0.9239,-0.9659,-0.9914,
			-1.0000,-0.9914,-0.9659,-0.9239,-0.8660,-0.7934,
			-0.7071,-0.6088,-0.5000,-0.3827,-0.2588,-0.1305,
			 0.0000
		};

	private class OscBank {
		public double Omega;
		public double Phase;
		public float AmpL;
		public float AmpR;
		public float DeclickedL;
		public float DeclickedR;
	}

	public WaveSynth(BaseSpectrum spectrum, int sampleRate) {
		Spectrum = spectrum;
		var random = new Random();
		for (var ixT = 0; ixT < HALFTONE_COUNT; ixT++) {
			Banks[ixT] = new OscBank {
				Phase = random.NextDouble()
			};
		}
		for (int i = 0; i < BankCount; i++) {
			var f0 = MinFreq * Math.Pow(2.0, (double)i / OCT_DIV);
			Omega[i] = f0 / sampleRate;
		}
		ClickDump = 1.0f - MathF.Exp(-2.0f * MathF.PI * 2000.0f / sampleRate);
	}

	/// <summary>
	/// 出力バッファへ書き込む
	/// </summary>
	/// <param name="pOutput">出力バッファ(2ch×サンプル数)</param>
	/// <param name="sampleCount">出力バッファのサンプル数</param>
	public unsafe void WriteBuffer(IntPtr pOutput, int sampleCount) {
		/* パラメータを設定 */
		SetParameter();
		/* 波形合成を実行 */
		DoWaveSynth((float*)pOutput, sampleCount);
	}

	private void SetParameter() {
		var autoGain = Spectrum.Max;
		var threshold = TARGET_RANGE * autoGain;
		var threshold2 = threshold * threshold;
		for (int i = 0; i < HALFTONE_COUNT; ++i) {
			var osc = Banks[i];
			var enableL = (osc.DeclickedL > threshold) ? 1.0f : 0.0f;
			var enableR = (osc.DeclickedR > threshold) ? 1.0f : 0.0f;
			osc.DeclickedL *= enableL;
			osc.DeclickedR *= enableR;
		}
		for (int i = 0, iDivA = 0, iDivB = HALFTONE_DIV; i < HALFTONE_COUNT; ++i, iDivA += HALFTONE_DIV, iDivB += HALFTONE_DIV) {
			var osc = Banks[i];
			/***********************************************/
			/*** 1/12oct単位で最大パワーのスペクトルを参照する
			/*** 角速度は左右の2乗和で評価、振幅は左右独立
			/***********************************************/
			{
				var omega = Omega[iDivA + HALFTONE_CENTER];
				var maxAmpL = 0.0f;
				var maxAmpR = 0.0f;
				var maxNorm2 = threshold2;
				for (int iDiv = iDivA; iDiv < iDivB; ++iDiv) {
					var spec = Spectrum.Banks[iDiv];
					var l = spec.PeakL;
					var r = spec.PeakR;
					var norm2 = l * l + r * r;
					var enableOmega = (norm2 > maxNorm2) ? 1.0f : 0.0f;
					omega = (1.0f - enableOmega) * omega;
					omega += enableOmega * Omega[iDiv];
					maxAmpL = MathF.Max(l, maxAmpL);
					maxAmpR = MathF.Max(r, maxAmpR);
					maxNorm2 = MathF.Max(norm2, maxNorm2);
				}
				osc.Omega = omega * Pitch;
				osc.AmpL = (maxAmpL > threshold) ? maxAmpL : 0;
				osc.AmpR = (maxAmpR > threshold) ? maxAmpR : 0;
			}
			/*************************************************************/
			/*** 振幅が閾値以下の場合
			/*** 低音側または高音側のパワーが大きい方の位相を取得して設定する
			/*************************************************************/
			{
				var enableRef = (osc.DeclickedL + osc.DeclickedR) == 0 ? 1.0f : 0.0f;
				/* 低音側[-2] */
				var iNear = Math.Max(i - 2, 0);
				var oscNear = Banks[iNear];
				var l = oscNear.DeclickedL;
				var r = oscNear.DeclickedR;
				var norm2 = l * l + r * r;
				var enablePhase = (norm2 > threshold2) ? 1.0f : 0.0f;
				enablePhase *= enableRef;
				osc.Phase = (1.0f - enablePhase) * osc.Phase;
				osc.Phase += enablePhase * oscNear.Phase;
				var maxNorm2 = MathF.Max(norm2, threshold2);
				/* 低音側[-1] */
				iNear = Math.Max(i - 1, 0);
				oscNear = Banks[iNear];
				l = oscNear.DeclickedL;
				r = oscNear.DeclickedR;
				norm2 = l * l + r * r;
				enablePhase = (norm2 > maxNorm2) ? 1.0f : 0.0f;
				enablePhase *= enableRef;
				osc.Phase = (1.0f - enablePhase) * osc.Phase;
				osc.Phase += enablePhase * oscNear.Phase;
				maxNorm2 = MathF.Max(norm2, maxNorm2);
				/* 高音側[+1] */
				iNear = Math.Min(i + 1, HALFTONE_COUNT - 1);
				oscNear = Banks[iNear];
				l = oscNear.DeclickedL;
				r = oscNear.DeclickedR;
				norm2 = l * l + r * r;
				enablePhase = (norm2 > maxNorm2) ? 1.0f : 0.0f;
				enablePhase *= enableRef;
				osc.Phase = (1.0f - enablePhase) * osc.Phase;
				osc.Phase += enablePhase * oscNear.Phase;
				maxNorm2 = MathF.Max(norm2, maxNorm2);
				/* 高音側[+2] */
				iNear = Math.Min(i + 2, HALFTONE_COUNT - 1);
				oscNear = Banks[iNear];
				l = oscNear.DeclickedL;
				r = oscNear.DeclickedR;
				norm2 = l * l + r * r;
				enablePhase = (norm2 > maxNorm2) ? 1.0f : 0.0f;
				enablePhase *= enableRef;
				osc.Phase = (1.0f - enablePhase) * osc.Phase;
				osc.Phase += enablePhase * oscNear.Phase;
			}
		}
	}

	private unsafe void DoWaveSynth(float* pOutput, int sampleCount) {
		var pWaveTerm = pOutput + sampleCount*2;
		foreach (var osc in Banks) {
			/* 振幅が0なら波形合成を行わない */
			if ((osc.AmpL + osc.AmpR) == 0 && (osc.DeclickedL + osc.DeclickedR) == 0) {
				// 位相を進める
				osc.Phase += osc.Omega * sampleCount;
				osc.Phase -= (int)osc.Phase;
				continue;
			}
			/* 波形合成 */
			var phase = osc.Phase;
			var declickedL = osc.DeclickedL;
			var declickedR = osc.DeclickedR;
			var delta = osc.Omega;
			var ampL = osc.AmpL;
			var ampR = osc.AmpR;
			for (var pWave = pOutput; pWave < pWaveTerm; pWave += 2) {
				// 正弦波テーブルの値を線形補間
				var ixD = phase * SIN_TABLE_LENGTH;
				var ixI = (int)ixD;
				var a2b = ixD - ixI;
				var sin = 1.0 - a2b;
				sin *= SIN_TABLE[ixI];
				sin += SIN_TABLE[ixI + 1] * a2b;
				// 位相を進める
				phase += delta;
				phase -= (int)phase;
				// 振幅を更新
				declickedL += (ampL - declickedL) * ClickDump;
				declickedR += (ampR - declickedR) * ClickDump;
				// 波形合成
				pWave[0] += (float)(sin * declickedL);
				pWave[1] += (float)(sin * declickedR);
			}
			osc.Phase = phase;
			osc.DeclickedL = declickedL;
			osc.DeclickedR = declickedR;
		}
	}
}
