#include "Main.h"

/******************************************************************************/
/* WinMain - メインエントリポイント
/******************************************************************************/
int WINAPI
WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpszCmdLine, int nCmdShow) {
    hInst = hInstance;

    // ウインドウクラスの登録
    WNDCLASSEX wc = {
        sizeof(WNDCLASSEX),
        CS_CLASSDC,
        mainWndProc,
        0L,
        0L,
        GetModuleHandle(NULL),
        NULL,
        NULL,
        NULL,
        NULL,
        CLASS_NAME,
        NULL
    };
    if (NULL == RegisterClassEx(&wc)) {
        return 0;
    }

    // タイトルバーとウインドウ枠の分を含めてウインドウサイズを設定
    RECT rect;
    SetRect(&rect, 0, 0, WINDOW_WIDTH, WINDOW_HEIGHT);
    AdjustWindowRect(&rect, WS_OVERLAPPEDWINDOW, FALSE);
    rect.right = rect.right - rect.left;
    rect.bottom = rect.bottom - rect.top;
    rect.top = 0;
    rect.left = 0;

    // ウインドウの生成
    HWND hWnd = CreateWindow(
        CLASS_NAME,
        WINDOW_TITLE,
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT,
        CW_USEDEFAULT,
        rect.right,
        rect.bottom,
        NULL,
        NULL,
        wc.hInstance,
        NULL
    );
    if (NULL == hWnd) {
        return 0;
    }

    // メインウィンドウを表示する
    ShowWindow(hWnd, nCmdShow);
    UpdateWindow(hWnd);

    // ウィンドウメッセージを処理する
    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return msg.wParam;
}

/******************************************************************************/
/* mainWndProc - メインウィンドウのメッセージ処理
/******************************************************************************/
LRESULT CALLBACK
mainWndProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
    // メッセージを処理
    switch (uMsg) {
    case WM_CREATE:
        return wmCreate(hWnd, uMsg, wParam, lParam);

    case WM_DESTROY:
        return wmDestroy(hWnd, uMsg, wParam, lParam);

    case WM_SIZE:
        SendMessage(hToolWnd, WM_SIZE, 0, 0);
        return DefWindowProc(hWnd, uMsg, wParam, lParam);

    case WM_PAINT:
        return wmPaint(hWnd, uMsg, wParam, lParam);

    case WM_QUERYNEWPALETTE:
    {	// フォーカスを得る直前に自分のパレットを実体化
        HDC hDC = GetDC(hWnd);
        SelectPalette(hDC, hPalette, FALSE);
        if (RealizePalette(hDC)) {
            InvalidateRect(hWnd, NULL, TRUE);
        }
        ReleaseDC(hWnd, hDC);
        break;
    }

    case WM_PALETTECHANGED:
    {	// 別のアプリケーションがパレットを変更した
        if ((HWND)wParam != hWnd) {
            HDC hDC = GetDC(hWnd);
            SelectPalette(hDC, hPalette, FALSE);
            if (RealizePalette(hDC)) {
                UpdateColors(hDC);
            }
            ReleaseDC(hWnd, hDC);
        }
        break;
    }

    case WM_USER:
        return wmUser(hWnd, uMsg, wParam, lParam);

    default:
        return DefWindowProc(hWnd, uMsg, wParam, lParam);
    }
    return (LRESULT)0;
}

LRESULT
wmCreate(HWND& hWnd, UINT& uMsg, WPARAM& wParam, LPARAM& lParam) {
    // パレットを作成する
    createPallet(hWnd);

    // ゲージを描画する
    drawGauge(hWnd, hMemBitmap);

    // 録音デバイスクラス
    cWaveIn = new WaveIn(hWnd);

    // DFT
    if (NULL != gpAmp) {
        free(gpAmp);
    }
    gpAmp = (double*)malloc(sizeof(double)*BANKS);
    dft::init(PITCH, SIGMA, BANKS, 12 * NOTE_DIV, DFT_LENGTH, WaveIn::SAMPLE_RATE);

    // プロセスの優先順位を NORMAL にする
    SetPriorityClass(GetCurrentProcess(), NORMAL_PRIORITY_CLASS);

    InvalidateRect(hWnd, NULL, FALSE);
    return (LRESULT)0;
}

LRESULT
wmDestroy(HWND& hWnd, UINT& uMsg, WPARAM& wParam, LPARAM& lParam) {
    // プロセスの優先順位を NORMAL に戻す
    // （さもなくばウィンドウが閉じられるまでに時間がかかることがある）
    SetPriorityClass(GetCurrentProcess(), NORMAL_PRIORITY_CLASS);

    // 確保していたメモリを開放
    GlobalFree(lpBitmap);
    DeleteObject(hMemBitmap);

    dft::purge();

    delete cWaveIn;
    cWaveIn = NULL;

    PostQuitMessage(0);
    return (LRESULT)0;
}

LRESULT
wmPaint(HWND& hWnd, UINT& uMsg, WPARAM& wParam, LPARAM& lParam) {
    PAINTSTRUCT ps;
    HDC         hMemDC;
    RECT        r;

    BeginPaint(hWnd, &ps);

    // パレットを選択して実体化する。
    SelectPalette(ps.hdc, hPalette, FALSE);
    RealizePalette(ps.hdc);

    GetClientRect(hWnd, &r);

    // DIBバッファの内容を描画
    //  ※ メモリ上に描画してスクリーンに転写する二度手間をかけているのは、
    //       1. ちらつきを抑える
    //       2. 大抵のビデオカードで、StretchDIBitsよりもStretchBltの方が高速
    //     であるため。
    hMemDC = CreateCompatibleDC(ps.hdc);
    SelectObject(hMemDC, hMemBitmap);

    // 上半分（スペクトル表示）をバッファに描画
    SetDIBitsToDevice(
        hMemDC,
        0, 0,
        DRAW_WIDTH, DRAW_HEIGHT,
        0, 0,
        0, DRAW_HEIGHT,
        lpBits,
        lpBitmap,
        DIB_RGB_COLORS
    );

    // 下半分をバッファに描画
    SetDIBitsToDevice(
        hMemDC,
        0, DRAW_HEIGHT + GAUGE_HEIGHT,
        DRAW_WIDTH, DRAW_HEIGHT,
        0, 0,
        0,
        DRAW_HEIGHT,
        lpBits + DRAW_WIDTH * DRAW_HEIGHT,
        lpBitmap,
        DIB_RGB_COLORS
    );

    // 枠を描画する
    DrawEdge(ps.hdc, &r, EDGE_SUNKEN, BF_RECT);

    // バッファの内容をウィンドウに転写
    r.left += GetSystemMetrics(SM_CXBORDER);
    r.right -= GetSystemMetrics(SM_CXBORDER);
    r.top += GetSystemMetrics(SM_CYBORDER);
    r.bottom -= GetSystemMetrics(SM_CYBORDER);
    SetStretchBltMode(ps.hdc, STRETCH_ORSCANS);
    StretchBlt(
        ps.hdc,
        r.left, r.top,
        r.right - r.left, r.bottom - r.top,
        hMemDC,
        0, 0,
        DRAW_WIDTH, GAUGE_HEIGHT + DRAW_HEIGHT * 2,
        SRCCOPY
    );

    DeleteDC(hMemDC);
    EndPaint(hWnd, &ps);

    return (LRESULT)0;
}

LRESULT
wmUser(HWND& hWnd, UINT& uMsg, WPARAM& wParam, LPARAM& lParam) {
    if (FALSE == cWaveIn->IsWaveOpen) {
        InvalidateRect(hWnd, NULL, TRUE);
        return (LRESULT)0;
    }

    // バッファに値をセット
    cWaveIn->SetBuffer(lParam);

    // データバッファブロックを再利用する
    cWaveIn->Reuse(lParam);

    // 描画処理
    plotSpectrum(hWnd);

    // 描画処理中に溜まった描画要求を削除する
    MSG msg;
    while (PeekMessage(&msg, hWnd, WM_USER, WM_USER, PM_REMOVE)) {
        // データバッファブロックを再利用する
        MMRESULT rc = cWaveIn->Reuse(msg.lParam);
        if (MMSYSERR_NOERROR != rc) {
            break;
        }
    }

    InvalidateRect(hWnd, NULL, TRUE);
    return (LRESULT)0;
}

/******************************************************************************/
/* パレットを作成
/******************************************************************************/
void
createPallet(HWND& hWnd) {
    DWORD        pallet[PALLET_COLORS];
    LPLOGPALETTE lpPal;
    LPDWORD      dp;
    LPDWORD      sp;
    HDC          hDC;
    int          i, a, b;

    // カラーパレットを作成する。
    for (i = 0, a = 0, b = 0; i < PALLET_COLORS; ++i, a += 0x500 / PALLET_COLORS, b = a % 0x100) {
        if (a < 0x100) {
            // Black -> Blue
            pallet[i] = b;
        } else if (a < 0x200) {
            // Blue -> LightGreen
            pallet[i] = (b << 8) | 0xFF;
        } else if (a < 0x300) {
            // LightGreen -> Green
            pallet[i] = 0xFFFF - b;
        } else if (a < 0x400) {
            // Green -> Yellow
            pallet[i] = (b << 16) | 0xFF00;
        } else if (a < 0x500) {
            // Yellow -> Red
            pallet[i] = 0xFFFF00 - (b << 8);
        }
    }

    lpPal = (LPLOGPALETTE)GlobalAlloc(GPTR, sizeof(LOGPALETTE) + (PALLET_COLORS * sizeof(PALETTEENTRY)));
    lpPal->palVersion = 0x300;
    lpPal->palNumEntries = PALLET_COLORS;
    dp = (DWORD*)lpPal->palPalEntry;
    sp = (DWORD*)pallet;
    for (i = 0; i < PALLET_COLORS; ++i) {
        *(dp++) = ((DWORD)PC_NOCOLLAPSE << 24) | (DWORD)RGBQUAD2COLORREF(*(sp++));
    }
    hPalette = CreatePalette(lpPal);
    GlobalFree(lpPal);

    // DIBバッファ lpBits の構造 (サイズDRAW_WIDTH * DRAW_HEIGHT * 2面)
    // ※ DIBヘッダ lpBitmap の画像サイズは DRAW_WIDTH * DRAW_HEIGHT
    // lpBits -> +-------------+ 高さ
    //           |             | DRAW_HEIGHT	スペクトル表示用バッファ
    //           +-------------+
    //           |             | DRAW_HEIGHT	履歴スクロールバッファ
    //           +-------------+
    //           横幅 DRAW_WIDTH

    // DIBバッファを作成する。
    i = sizeof(BITMAPINFOHEADER) + PALLET_COLORS * 4;
    lpBitmap = (LPBITMAPINFO)GlobalAlloc(GPTR, i + DRAW_WIDTH * DRAW_HEIGHT * 2);
    lpBits = (LPBYTE)lpBitmap + i;
    memset(lpBits, 0, DRAW_WIDTH * DRAW_HEIGHT * 2);

    // DIBヘッダ
    lpBitmap->bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    lpBitmap->bmiHeader.biWidth = DRAW_WIDTH;
    lpBitmap->bmiHeader.biHeight = DRAW_HEIGHT;
    lpBitmap->bmiHeader.biPlanes = 1;
    lpBitmap->bmiHeader.biBitCount = 8;
    lpBitmap->bmiHeader.biCompression = BI_RGB;
    lpBitmap->bmiHeader.biSizeImage = DRAW_WIDTH * DRAW_HEIGHT;
    lpBitmap->bmiHeader.biXPelsPerMeter = 0;
    lpBitmap->bmiHeader.biYPelsPerMeter = 0;
    lpBitmap->bmiHeader.biClrUsed = PALLET_COLORS;
    lpBitmap->bmiHeader.biClrImportant = PALLET_COLORS;
    memcpy(&lpBitmap->bmiColors, pallet, PALLET_COLORS * 4);

    // 表示バッファ(DDB)を作成する
    hDC = GetDC(hWnd);
    hMemBitmap = CreateCompatibleBitmap(hDC, DRAW_WIDTH, DRAW_HEIGHT * 2 + GAUGE_HEIGHT);
    ReleaseDC(hWnd, hDC);
}

/******************************************************************************/
/* 目盛を描画
/******************************************************************************/
void
drawGauge(HWND hWnd, HBITMAP hMemBitmap) {
    RECT r;
    HDC hDC, hWindowDC;

    hWindowDC = GetDC(hWnd);
    hDC = CreateCompatibleDC(hWindowDC);
    ReleaseDC(hWnd, hWindowDC);
    SelectObject(hDC, hMemBitmap);

    r.left = 0;
    r.right = DRAW_WIDTH;
    r.top = DRAW_HEIGHT;
    r.bottom = r.top + GAUGE_HEIGHT;

    // 音階ゲージを描画
    HBRUSH hbr;
    UINT32 i;

    FillRect(hDC, &r, (HBRUSH)GetStockObject(BLACK_BRUSH));
    for (i = 0; i < BANKS; ++i) {
        COLORREF c;
        int octAlt = ((i / NOTE_DIV + START_NOTE + 9) / 12 & 1);

        switch ((i / NOTE_DIV + START_NOTE) % 12) {
        case 1:
        case 4:
        case 6:
        case 9:
        case 11:
            c = PALETTERGB(64, 64, 64);
            break;
        default:
            if (octAlt) {
                c = PALETTERGB(255, 255, 255);
            } else {
                c = PALETTERGB(128, 255, 128);
            }
        }

        r.left = i * DRAW_STEPX;
        r.right = r.left + DRAW_STEPX;

        hbr = CreateSolidBrush(c);
        FillRect(hDC, &r, hbr);
        DeleteObject(hbr);
    }

    DeleteDC(hDC);
}

/******************************************************************************/
/* スペクトルをDIBバッファに描画
/******************************************************************************/
void
plotSpectrum(HWND hWnd) {
    double maxLevel = 0.0;
    UINT32 level = 0;

    // スクロールバッファをスクロールダウン
    memcpy(
        lpBits + DRAW_WIDTH * DRAW_HEIGHT,
        lpBits + DRAW_WIDTH * DRAW_HEIGHT + DRAW_WIDTH,
        DRAW_WIDTH * (DRAW_HEIGHT - 1)
    );
    memset(lpBits, 0, DRAW_WIDTH * DRAW_HEIGHT);

    // DFT
    dft::exec((short*)cWaveIn->mpBuffer, gpAmp, WaveIn::SAMPLES);

    for (UINT32 i = 0; i < BANKS; ++i) {
        // 振幅
        {
            double amp = gpAmp[i];
            if (1.0 < amp) {
                amp = 1.0;
            }
            if (maxLevel < amp) {
                maxLevel = amp;
            }

            amp *= 40.0 / gAvgLevel;
            if (amp < 1.0) {
                amp = 0.0;
            } else {
                amp = log10(amp) / log10(40.0);
            }

            amp *= DRAW_HEIGHT;
            if (DRAW_HEIGHT <= amp) {
                amp = DRAW_HEIGHT - 1;
            }
            level = static_cast<UINT32>(amp);
        }

        // DIBバッファ上に棒グラフを描画
        {
            // UINT32値を使って、一度に４ピクセルずつ描画する
            UINT32 *pix4 = (UINT32*)lpBits + i;
            for (UINT32 j = 0; j < level; ++j) {
                UINT32 tmp = 8 + j * 56 / DRAW_HEIGHT;
                tmp |= (tmp << 8) | (tmp << 16) | (tmp << 24);
                *pix4 = tmp;
                pix4 += BANKS;
            }
        }

        // スクロールバッファにも描画
        {
            UINT32 rgb = level * PALLET_COLORS / DRAW_HEIGHT;
            rgb |= (rgb << 8) | (rgb << 16) | (rgb << 24);
            *((UINT32 *)(lpBits + DRAW_WIDTH * (DRAW_HEIGHT * 2 - 1)) + i) = rgb;
        }
    }

    if (gAvgLevel < maxLevel) {
        gAvgLevel = maxLevel;
    } else {
        gAvgLevel -= gAvgLevel * 0.01;
    }

    if (gAvgLevel < 1 / 1024.0) {
        gAvgLevel = 1 / 1024.0;
    }
}
