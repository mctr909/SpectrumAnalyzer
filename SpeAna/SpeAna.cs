using System;

namespace SpeAna {
    class SpeAna : WinMM.WaveIn {
        public double[] Amp { get; private set; }
        private readonly BiQuadFilter[] mFilter;

        public SpeAna(int banks, int sampleRate, double baseFreq)
            : base(sampleRate, 2, sampleRate / 60 + (sampleRate / 60 % 2)) {
            Amp = new double[banks];
            mFilter = new BiQuadFilter[banks];
            var oct = Math.Log2(sampleRate / baseFreq / 2);
            var oct_div = oct / mFilter.Length;
            for (int i = 0; i < mFilter.Length; i++) {
                var freq = baseFreq * Math.Pow(2.0, i * oct_div);
                mFilter[i] = new BiQuadFilter(sampleRate);
                mFilter[i].BandPass(freq, oct_div);
            }
        }

        protected override void GetData() {
            ExecStereo(WaveBuffer);
        }

        public void ExecStereo(short[] input) {
            for (int b = 0; b < mFilter.Length; b++) {
                var filter = mFilter[b];
                var sum = 0.0;
                for (int i = 0; i < input.Length; i += 2) {
                    filter.Exec((input[i] + input[i + 1]) / 65536.0);
                    sum += filter.Output * filter.Output;
                }
                sum = 2.0 * sum / input.Length;
                var db = Math.Sqrt(sum);
                if (db < 1.0 / 32768) {
                    db = 1.0 / 32768;
                }
                Amp[b] = 20 * Math.Log10(db);
            }
        }
    }
}
