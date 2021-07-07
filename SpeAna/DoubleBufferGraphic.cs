using System;
using System.Drawing;
using System.Windows.Forms;

public class DoubleBufferGraphic : IDisposable {
    private BufferedGraphics mBuffer;
    private Image mBackGround;

    public DoubleBufferGraphic(Control control, Image backGround) {
        Dispose();

        var currentContext = BufferedGraphicsManager.Current;
        mBackGround = backGround;
        mBuffer = currentContext.Allocate(control.CreateGraphics(), control.DisplayRectangle);
    }

    ~DoubleBufferGraphic() {
        Dispose();
    }

    public void Dispose() {
        if (null != mBuffer) {
            mBuffer.Dispose();
            mBuffer = null;
        }
    }

    public void SizeChange(Control control) {
        if (null != mBuffer) {
            mBuffer.Dispose();
            mBuffer = null;
        }

        var currentContext = BufferedGraphicsManager.Current;
        mBuffer = currentContext.Allocate(control.CreateGraphics(), control.DisplayRectangle);
    }

    public void Render() {
        if (null != mBuffer) {
            try {
                mBuffer.Render();
            } catch { }
        }
    }

    public Graphics Graphics {
        get {
            mBuffer.Graphics.Clear(Color.Transparent);
            if (null != mBackGround) {
                mBuffer.Graphics.DrawImage(mBackGround, 0, 0);
            }
            return mBuffer.Graphics;
        }
    }
}