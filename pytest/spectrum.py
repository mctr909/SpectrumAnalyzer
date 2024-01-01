import math

class _ToneBank:
    DELTA: float
    SIGMA: float

    Phase: float = 0.0
    Re: float = 0.0
    Im: float = 0.0

    def __init__(self, sampleRate: int, freq: float):
        MIN_WIDTH = 1.0
        MIN_WIDTH_AT_FREQ = 800
        SIDEROBE = 1.5
        halfToneWidth = MIN_WIDTH + math.log(MIN_WIDTH_AT_FREQ / freq, 2.0)
        if (halfToneWidth < MIN_WIDTH):
            halfToneWidth = MIN_WIDTH
        delta = freq / sampleRate
        self.DELTA = delta
        if (delta > sampleRate * 0.01):
            delta = sampleRate * 0.01
        omega = 2 * math.pi * delta
        s = math.sin(omega)
        x = math.log(2, math.exp(1)) / 4 * omega / s * halfToneWidth / 12.0
        self.SIGMA = s * math.sinh(x*SIDEROBE)

class _BPFBank:
    DELTA: float
    KB0: float
    KA1: float
    KA2: float

    a1: float = 0.0
    a2: float = 0.0
    b1: float = 0.0
    b2: float = 0.0

    def __init__(self, sampleRate: int, freq: float):
        MIN_WIDTH = 1
        MIN_WIDTH_AT_FREQ = 660
        halfToneWidth = MIN_WIDTH + math.log(MIN_WIDTH_AT_FREQ / freq, 2.0)
        if (halfToneWidth < MIN_WIDTH):
            halfToneWidth = MIN_WIDTH
        self.DELTA = freq / sampleRate
        omega = 2 * math.pi * self.DELTA
        s = math.sin(omega)
        x = math.log(2, math.exp(1)) / 4 * omega / s * halfToneWidth / 12.0
        alpha = s * math.sinh(x)
        a0 = 1.0 + alpha
        self.KB0 = alpha / a0
        self.KA1 = -2.0 * math.cos(omega) / a0
        self.KA2 = (1.0 - alpha) / a0

class Spectrum:
    SAMPLE_RATE: int 
    BUFFER_SAMPLES: int
    BANK_COUNT: int
    OCT_DIV: int
    DB_MIN: float = -60

    __AMP_MIN: float = math.pow(10, DB_MIN/20)
    __BASE_FREQ: float = 13.75
    __LOW_FREQ: int = 80
    __MID_FREQ: int = 350
    __LOW_TONE: int
    __MID_TONE: int
    __THRESHOLD_WIDE: int
    __THRESHOLD_NARROW: int
    __BASISFUNC_LENGTH: int = 24
    __BASISFUNC_C: list[float] = []
    __BASISFUNC_S: list[float] = []

    __ToneBanks: list[_ToneBank] = []
    __BPFBanks: list[_BPFBank] = []
    __AvgPower: list[float] = []

    InputBuffer: list[float] = []
    Amp: list[float] = []
    Threshold: list[float] = []
    Peak: list[float] = []

    def __init__(self, sampleRate: int, bufferLength: int, bankCount: int, octDiv: int):
        # 設定値
        self.SAMPLE_RATE = sampleRate
        self.BUFFER_SAMPLES = bufferLength
        self.BANK_COUNT = bankCount
        self.OCT_DIV = octDiv
        self.__THRESHOLD_WIDE = int(octDiv * 3 / 4)
        self.__THRESHOLD_NARROW = int(octDiv / 12 * 2)
        self.__LOW_TONE = int(octDiv * math.log(self.__LOW_FREQ / self.__BASE_FREQ, 2))
        self.__MID_TONE = int(octDiv * math.log(self.__MID_FREQ / self.__BASE_FREQ, 2))
        # 基底関数(cos, sin)テーブルを作成
        for t in range(self.__BASISFUNC_LENGTH + 1):
            self.__BASISFUNC_C.append(math.cos(2*math.pi*t/self.__BASISFUNC_LENGTH))
            self.__BASISFUNC_S.append(math.sin(2*math.pi*t/self.__BASISFUNC_LENGTH))
        # 入力バッファを作成
        for i in range(bufferLength):
            self.InputBuffer.append(0)
        # トーンバンクリストの作成
        for i in range(bankCount):
            freq = self.__BASE_FREQ * math.pow(2, i / octDiv)
            self.__ToneBanks.append(_ToneBank(sampleRate, freq))
            self.__BPFBanks.append(_BPFBank(sampleRate, freq))
            self.__AvgPower.append(0)
            self.Amp.append(0)
            self.Threshold.append(0)
            self.Peak.append(0)

    def __calcThreshold(self):
        lastAmp = self.DB_MIN
        lastAmpIndex = -1
        for b in range(self.BANK_COUNT):
            # 閾値の幅
            width: int
            if (b < self.__LOW_TONE):
                width = self.__THRESHOLD_WIDE
            elif (b < self.__MID_TONE):
                a2b = (b - self.__LOW_TONE) / (self.__MID_TONE - self.__LOW_TONE)
                width = int(self.__THRESHOLD_NARROW * a2b + self.__THRESHOLD_WIDE * (1 - a2b))
            else:
                width = self.__THRESHOLD_NARROW
            # 閾値
            threshold: float = 0.0
            for w in range(-width, width):
                bw = min(self.BANK_COUNT - 1, max(0, b + w))
                threshold += self.__AvgPower[bw]
            threshold = math.sqrt(threshold / (width*2 + 1))
            if (threshold < self.__AMP_MIN):
                threshold = self.__AMP_MIN
            self.Threshold[b] = 20*math.log10(threshold)
            # ピークを抽出
            self.Peak[b] = self.DB_MIN
            amp = self.Amp[b]
            if (amp < self.Threshold[b]):
                if (0 <= lastAmpIndex):
                    self.Peak[lastAmpIndex] = lastAmp
                amp = self.DB_MIN
                lastAmp = self.DB_MIN
                lastAmpIndex = -1
            if (lastAmp < amp):
                lastAmp = amp
                lastAmpIndex = b
        if (0 <= lastAmpIndex):
            self.Peak[lastAmpIndex] = lastAmp

    def execDFT(self):
        for b in range(self.BANK_COUNT):
            tone = self.__ToneBanks[b]
            avgPower = self.__AvgPower[b]
            for i in range(self.BUFFER_SAMPLES):
                # 基底関数テーブルの参照
                idx_d = tone.Phase * self.__BASISFUNC_LENGTH
                tone.Phase += tone.DELTA
                tone.Phase -= int(tone.Phase)
                idx_i = int(idx_d)
                a2b = idx_d - idx_i
                bc = self.__BASISFUNC_C[idx_i]*(1-a2b) + self.__BASISFUNC_C[idx_i+1]*a2b
                bs = self.__BASISFUNC_S[idx_i]*(1-a2b) + self.__BASISFUNC_S[idx_i+1]*a2b
                # DFT実行
                tone.Re += (self.InputBuffer[i]*bc - tone.Re)*tone.SIGMA
                tone.Im += (self.InputBuffer[i]*bs - tone.Im)*tone.SIGMA
                # 振幅の2乗の平均
                power = tone.Re*tone.Re + tone.Im*tone.Im
                avgPower += (power - avgPower) * tone.DELTA
            # 振幅の2乗の平均を格納
            self.__AvgPower[b] = avgPower
            # 振幅(db)を格納
            amp = math.sqrt(avgPower)
            if (amp < self.__AMP_MIN):
                amp = self.__AMP_MIN
            self.Amp[b] = 20*math.log10(amp)
        # 閾値を格納
        self.__calcThreshold()

    def execRMS(self):
        for b in range(self.BANK_COUNT):
            bpf = self.__BPFBanks[b]
            avgPower = self.__AvgPower[b]
            for i in range(self.BUFFER_SAMPLES):
                # BPFを通す
                input = self.InputBuffer[i]
                output = bpf.KB0 * input - bpf.KB0 * bpf.b2 - bpf.KA1 * bpf.a1 - bpf.KA2 * bpf.a2
                bpf.a2 = bpf.a1
                bpf.a1 = output
                bpf.b2 = bpf.b1
                bpf.b1 = input
                # 振幅の2乗の平均
                avgPower += (output * output - avgPower) * bpf.DELTA
            # 振幅の2乗の平均を格納
            self.__AvgPower[b] = avgPower
            # 振幅(db)を格納
            amp = math.sqrt(avgPower)
            if (amp < self.__AMP_MIN):
                amp = self.__AMP_MIN
            self.Amp[b] = 20*math.log10(amp)
        # 閾値を格納
        self.__calcThreshold()
