using System;

namespace SpectrumAnalyzer {
    class SpectrumAnalyzer : WinMM.WaveIn {
        public double[] Amp { get; private set; }
        private readonly BiQuadFilter[] mFilter;

        public SpectrumAnalyzer(int banks, int sampleRate, double baseFreq)
            : base(sampleRate, 1, 736, 2) {
            Amp = new double[banks];
            mFilter = new BiQuadFilter[banks];
            var oct = Math.Log2(sampleRate / baseFreq / 2);
            var oct_div = oct / mFilter.Length;
            for (int i = 0; i < mFilter.Length; i++) {
                var freq = baseFreq * Math.Pow(2.0, i * oct_div);
                mFilter[i] = new BiQuadFilter(sampleRate);
                mFilter[i].BandPass(freq, oct_div * 0.75);
            }
        }

        protected override void GetData() {
            for (int b = 0; b < mFilter.Length; b++) {
                var filter = mFilter[b];
                var sum = 0.0;
                for (int i = 0; i < WaveBuffer.Length; i++) {
                    filter.Exec(WaveBuffer[i] / 32768.0);
                    sum += filter.Output * filter.Output;
                }
                sum = 2.0 * sum / WaveBuffer.Length;
                var db = Math.Sqrt(sum);
                if (db < 1.0 / 32768) {
                    db = 1.0 / 32768;
                }
                Amp[b] = 20 * Math.Log10(db);
            }
        }
    }
}
