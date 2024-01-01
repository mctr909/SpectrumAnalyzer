import math
import matplotlib.pyplot as plt
import matplotlib.animation as animation

from spectrum import Spectrum

#==========[ 定数宣言 ]==========
OCT_DIV: int = 36
OCT_COUNT: float = 8.0

#==========[ 変数宣言 ]==========
gInputPhase: float = 0.0
gInputFreq: float = 110.0
gInputFreqD: float = 1 + 5.0 / 44100
gLabelXPos: list[int] = []
gLabelXName: list[int] = []
gSpec: Spectrum
gFig: plt.Figure
gAni: animation.FuncAnimation

def setInputData():
    global gInputPhase
    global gInputFreq
    global gInputFreqD
    for i in range(gSpec.BUFFER_SAMPLES):
        gSpec.InputBuffer[i] = 2*gInputPhase - 1
        gInputPhase += gInputFreq / gSpec.SAMPLE_RATE
        gInputPhase -= int(gInputPhase)
        gInputFreq *= gInputFreqD
        if (gInputFreq >= 440):
            gInputFreqD = 1 - 5.0 / gSpec.SAMPLE_RATE
        if (gInputFreq <= 55):
            gInputFreqD = 1 + 5.0 / gSpec.SAMPLE_RATE

def plot(data):
    # 入力データ生成
    setInputData()
    # 実行
    gSpec.exec()
    # 現在描写されているグラフを消去
    plt.cla()
    # グリッドを表示する
    plt.grid(True)
    # x軸ラベル
    plt.xlabel("Octave")
    plt.xticks(gLabelXPos, gLabelXName)
    # y軸ラベル
    plt.ylabel("Amplitude(db)")
    # y軸範囲
    plt.ylim(gSpec.DB_MIN, 0)
    # グラフを生成
    plt.plot(gSpec.Amp, label="RMS of waveform\nthrough BPF.", linewidth=1, color="#808080")
    plt.plot(gSpec.Threshold, ":", label="Threshold", linewidth=1, color="#000000")
    plt.plot(gSpec.Peak, "o", label="Peak", markersize=3, color="#000000")
    plt.legend()

def main():
    # スペクトラムのインスタンスを生成
    global gSpec
    gSpec = Spectrum(44100, int(44100/100), int(OCT_DIV*OCT_COUNT), OCT_DIV)
    # X軸ラベルの値を設定
    global gLabelXPos
    global gLabelXName
    oct = 1
    for i in range(gSpec.BANK_COUNT):
        if (i % gSpec.OCT_DIV == 0):
            gLabelXPos.append(i)
            gLabelXName.append(oct)
            oct += 1
    # アニメーションに図とコールバック関数を設定
    global gFig
    global gAni
    gFig = plt.figure()
    gAni = animation.FuncAnimation(gFig, plot, interval=50)
    plt.show()

#========[ エントリーポイント ]========
main()
