import math

class _BPFBank:
    SIGMA: float
    KB0: float
    KA1: float
    KA2: float

    a1: float = 0.0
    a2: float = 0.0
    b1: float = 0.0
    b2: float = 0.0
    power: float = 0.0

    def __init__(self, sampleRate: int, freq: float):
        alpha = self.__GetAlpha(sampleRate, freq)
        omega = 2 * math.pi * freq / sampleRate
        a0 = 1.0 + alpha
        self.SIGMA = self.__GetAlpha(sampleRate / 2, freq)
        self.KB0 = alpha / a0
        self.KA1 = -2.0 * math.cos(omega) / a0
        self.KA2 = (1.0 - alpha) / a0

    def __GetAlpha(self, sampleRate: int, freq: float):
        MIN_WIDTH = 1.0
        MIN_WIDTH_AT_FREQ = 660
        halfToneWidth = MIN_WIDTH + math.log(MIN_WIDTH_AT_FREQ / freq, 2.0)
        if (halfToneWidth < MIN_WIDTH):
            halfToneWidth = MIN_WIDTH
        omega = 2 * math.pi * freq / sampleRate
        s = math.sin(omega)
        x = math.log(2, math.exp(1)) / 4 * omega / s * halfToneWidth / 12.0
        return s * math.sinh(x)

class Spectrum:
    SAMPLE_RATE: int 
    BUFFER_SAMPLES: int
    BANK_COUNT: int
    OCT_DIV: int
    DB_MIN: float = -40

    __AMP_MIN: float = math.pow(10, DB_MIN/20)
    __BASE_FREQ: float = 13.75
    __LOW_FREQ: int = 80
    __MID_FREQ: int = 350
    __LOW_TONE: int
    __MID_TONE: int
    __THRESHOLD_WIDE: int
    __THRESHOLD_NARROW: int
    __THRESHOLD_OFFSET: float = 0.5

    __BPFBanks: list[_BPFBank] = []

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
        self.__THRESHOLD_WIDE = int(octDiv * 7 / 12)
        self.__THRESHOLD_NARROW = int(octDiv * 2 / 12)
        self.__LOW_TONE = int(octDiv * math.log(self.__LOW_FREQ / self.__BASE_FREQ, 2))
        self.__MID_TONE = int(octDiv * math.log(self.__MID_FREQ / self.__BASE_FREQ, 2))
        # 入力バッファを作成
        for i in range(bufferLength):
            self.InputBuffer.append(0)
        # フィルタバンクの作成
        for i in range(bankCount):
            freq = self.__BASE_FREQ * math.pow(2, i / octDiv)
            self.__BPFBanks.append(_BPFBank(sampleRate, freq))
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
                threshold += self.__BPFBanks[bw].power
            threshold = math.sqrt(threshold / (width*2 + 1))
            if (threshold < self.__AMP_MIN):
                threshold = self.__AMP_MIN
            threshold = self.__THRESHOLD_OFFSET + 20*math.log10(threshold)
            self.Threshold[b] = threshold
            # ピークを抽出
            self.Peak[b] = self.DB_MIN
            amp = self.Amp[b]
            if (amp < threshold):
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

    def exec(self):
        for b in range(self.BANK_COUNT):
            bpf = self.__BPFBanks[b]
            for i in range(self.BUFFER_SAMPLES):
                # BPFを通す
                b0 = self.InputBuffer[i]
                a0 = bpf.KB0 * b0 - bpf.KB0 * bpf.b2 - bpf.KA1 * bpf.a1 - bpf.KA2 * bpf.a2
                bpf.a2 = bpf.a1
                bpf.a1 = a0
                bpf.b2 = bpf.b1
                bpf.b1 = b0
                # 振幅の2乗の平均
                bpf.power += (a0 * a0 - bpf.power) * bpf.SIGMA
            # 振幅(db)を格納
            amp = math.sqrt(bpf.power)
            if (amp < self.__AMP_MIN):
                amp = self.__AMP_MIN
            self.Amp[b] = 20*math.log10(amp)
        # 閾値を格納
        self.__calcThreshold()
