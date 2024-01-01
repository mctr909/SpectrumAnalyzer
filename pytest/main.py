import matplotlib.pyplot as plt
import matplotlib.animation as animation

from spectrum import Spectrum

#==========[ 定数宣言 ]==========
OCT_DIV: int = 48
OCT_COUNT: float = 8.0

#==========[ 変数宣言 ]==========
mInputPhase: float = 0.0
mInputFreq: float = 110.0
mInputFreqD: float = 1 + 4 / 44100
mLabelXPos: list[int] = []
mLabelXName: list[int] = []
mSpec: Spectrum
mFig: plt.Figure
mAni: animation.FuncAnimation

def setInputData():
    global mInputPhase
    global mInputFreq
    global mInputFreqD
    for i in range(mSpec.BUFFER_SAMPLES):
        mSpec.InputBuffer[i] = mInputPhase*2 - 1
        mInputPhase += mInputFreq / mSpec.SAMPLE_RATE
        mInputPhase -= int(mInputPhase)
        mInputFreq *= mInputFreqD
        if (mInputFreq >= 440):
            mInputFreqD = 1 - 4.0 / mSpec.SAMPLE_RATE
        if (mInputFreq <= 55):
            mInputFreqD = 1 + 4.0 / mSpec.SAMPLE_RATE

def plot(data):
    # 入力データ生成
    setInputData()
    # DFT実行
    mSpec.execDFT()
    # 現在描写されているグラフを消去
    plt.cla()
    # グリッドを表示する
    plt.grid(True)
    # x軸ラベル
    plt.xlabel("Octave")
    plt.xticks(mLabelXPos, mLabelXName)
    # y軸ラベル
    plt.ylabel("Amplitude(db)")
    # y軸範囲
    plt.ylim(mSpec.DB_MIN, 0)
    # グラフを生成
    plt.plot(mSpec.Amp)
    plt.plot(mSpec.Threshold)
    plt.plot(mSpec.Peak)

def main():
    # スペクトラムのインスタンスを生成
    global mSpec
    mSpec = Spectrum(44100, int(44100/800), int(OCT_DIV*OCT_COUNT), OCT_DIV)
    # X軸ラベルの値を設定
    global mLabelXPos
    global mLabelXName
    oct = 1
    for i in range(mSpec.BANK_COUNT):
        if (i % mSpec.OCT_DIV == 0):
            mLabelXPos.append(i)
            mLabelXName.append(oct)
            oct += 1
    # アニメーションに図とコールバック関数を設定
    global mFig
    global mAni
    mFig = plt.figure()
    mAni = animation.FuncAnimation(mFig, plot, interval=50)
    plt.show()

#========[ エントリーポイント ]========
main()
