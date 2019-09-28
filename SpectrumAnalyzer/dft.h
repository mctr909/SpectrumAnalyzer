#pragma once

/******************************************************************************/
class dft {
public:
    static int init(double baseFreq, double sigma, int notes, int octDiv, int length, int sampleRate);
    static void purge();
    static void exec(short *pInput, double *pAmp, int samples);
};
