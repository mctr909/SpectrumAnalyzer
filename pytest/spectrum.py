import math

class _BPFBank:
    KB0: float
    KA1: float
    KA2: float
    SIGMA: float

    a1: float = 0.0
    a2: float = 0.0
    b1: float = 0.0
    b2: float = 0.0
    power: float = 0.0

    def __init__(self, sampleRate: int, freq: float):
        alpha = self.__GetAlpha(sampleRate, freq)
        omega = 2 * math.pi * freq / sampleRate
        a0 = 1.0 + alpha
        self.KB0 = alpha / a0
        self.KA1 = -2.0 * math.cos(omega) / a0
        self.KA2 = (1.0 - alpha) / a0
        self.SIGMA = self.__GetAlpha(sampleRate / 2, freq)

    def __GetAlpha(self, sampleRate: int, freq: float):
        MIN_WIDTH = 1.0
        MIN_WIDTH_AT_FREQ = 1000
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
    __LOW_FREQ_MAX: int = 80
    __MID_FREQ_MIN: int = 220
    __LOW_TONE_MAX: int
    __MID_TONE_MIN: int
    __LOW_TONE_GAIN: float = 2.0
    __MID_TONE_GAIN: float = 1.0
    __LOW_TONE_WIDTH: int
    __MID_TONE_WIDTH: int

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
        self.__LOW_TONE_WIDTH = int(octDiv * 5 / 12)
        self.__MID_TONE_WIDTH = int(octDiv * 1 / 12)
        self.__LOW_TONE_MAX = int(octDiv * math.log(self.__LOW_FREQ_MAX / self.__BASE_FREQ, 2))
        self.__MID_TONE_MIN = int(octDiv * math.log(self.__MID_FREQ_MIN / self.__BASE_FREQ, 2))
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
        for idxB in range(self.BANK_COUNT):
            # 閾値幅と閾値ゲインを設定
            gain: float
            width: int
            if (idxB < self.__LOW_TONE_MAX):
                gain = self.__LOW_TONE_GAIN
                width = self.__LOW_TONE_WIDTH
            elif (idxB < self.__MID_TONE_MIN):
                a2b = (idxB - self.__LOW_TONE_MAX) / (self.__MID_TONE_MIN - self.__LOW_TONE_MAX)
                gain = self.__MID_TONE_GAIN * a2b + self.__LOW_TONE_GAIN * (1 - a2b)
                width = int(self.__MID_TONE_WIDTH * a2b + self.__LOW_TONE_WIDTH * (1 - a2b))
            else:
                gain = self.__MID_TONE_GAIN
                width = self.__MID_TONE_WIDTH
            # 閾値幅で指定される範囲の平均値を閾値にする
            threshold: float = 0.0
            for divB in range(-width, width):
                bd = min(self.BANK_COUNT - 1, max(0, idxB + divB))
                threshold += self.__BPFBanks[bd].power
            threshold /= width*2 + 1
            # パワー⇒リニア⇒db変換後、閾値ゲインを掛ける
            threshold = math.sqrt(threshold)
            if (threshold < self.__AMP_MIN):
                threshold = self.__AMP_MIN
            threshold = 20*math.log10(threshold) + gain
            self.Threshold[idxB] = threshold
            # ピークを抽出
            self.Peak[idxB] = self.DB_MIN
            amp = self.Amp[idxB]
            if (amp < threshold):
                if (0 <= lastAmpIndex):
                    self.Peak[lastAmpIndex] = lastAmp
                amp = self.DB_MIN
                lastAmp = self.DB_MIN
                lastAmpIndex = -1
            if (lastAmp < amp):
                lastAmp = amp
                lastAmpIndex = idxB
        if (0 <= lastAmpIndex):
            self.Peak[lastAmpIndex] = lastAmp

    def exec(self):
        for idxB in range(self.BANK_COUNT):
            bank = self.__BPFBanks[idxB]
            for idxT in range(self.BUFFER_SAMPLES):
                # 帯域通過フィルタに通す
                b0 = self.InputBuffer[idxT]
                a0 = bank.KB0 * b0 - bank.KB0 * bank.b2 - bank.KA1 * bank.a1 - bank.KA2 * bank.a2
                bank.a2 = bank.a1
                bank.a1 = a0
                bank.b2 = bank.b1
                bank.b1 = b0
                # パワースペクトルを得る
                bank.power += (a0 * a0 - bank.power) * bank.SIGMA
            # パワー⇒リニア⇒db変換
            amp = math.sqrt(bank.power)
            if (amp < self.__AMP_MIN):
                amp = self.__AMP_MIN
            self.Amp[idxB] = 20*math.log10(amp)
        # 閾値を格納
        self.__calcThreshold()
