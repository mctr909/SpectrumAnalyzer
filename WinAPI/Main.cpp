#include "framework.h"
#include "resource.h"
#include "WaveOut.h"
#include "RiffWav.h"
#include "Spectrum.h"
#include "WaveSynth.h"

constexpr auto MAX_LOADSTRING = 100;
constexpr auto DIV_COUNT = 16;

// グローバル変数:
HINSTANCE hInst;                                // 現在のインターフェイス
WCHAR szTitle[MAX_LOADSTRING];                  // タイトル バーのテキスト
WCHAR szWindowClass[MAX_LOADSTRING];            // メイン ウィンドウ クラス名
WaveOut* clWaveOut;
RiffWav* clRiffWav;
Spectrum *clSpectrum;
WaveSynth *clWaveSynth;
int32_t DIV_SAMPLES;
int32_t DIV_SIZE;
int32_t BUFFER_SAMPLES;

// 前方宣言:
LRESULT CALLBACK WndProc(HWND, UINT, WPARAM, LPARAM);
INT_PTR CALLBACK About(HWND, UINT, WPARAM, LPARAM);
void WriteBuffer(LPSTR lpData);

// エントリポイント:
int APIENTRY wWinMain(
	_In_ HINSTANCE hInstance,
	_In_opt_ HINSTANCE hPrevInstance,
	_In_ LPWSTR    lpCmdLine,
	_In_ int       nCmdShow
) {
	UNREFERENCED_PARAMETER(hPrevInstance);
	UNREFERENCED_PARAMETER(lpCmdLine);

	// グローバル文字列を初期化
	LoadStringW(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING);
	LoadStringW(hInstance, IDC_SPECTRUMANALYZER, szWindowClass, MAX_LOADSTRING);

	// アプリケーション初期化
	WNDCLASSEXW wcex{};
	wcex.cbSize = sizeof(WNDCLASSEX);
	wcex.style = CS_HREDRAW | CS_VREDRAW;
	wcex.lpfnWndProc = WndProc;
	wcex.cbClsExtra = 0;
	wcex.cbWndExtra = 0;
	wcex.hInstance = hInstance;
	wcex.hIcon = LoadIcon(hInstance, MAKEINTRESOURCE(IDI_SPECTRUMANALYZER));
	wcex.hCursor = LoadCursor(nullptr, IDC_ARROW);
	wcex.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
	wcex.lpszMenuName = MAKEINTRESOURCEW(IDC_SPECTRUMANALYZER);
	wcex.lpszClassName = szWindowClass;
	wcex.hIconSm = LoadIcon(wcex.hInstance, MAKEINTRESOURCE(IDI_SMALL));
	RegisterClassExW(&wcex);
	auto hWnd = CreateWindowW(
		szWindowClass, szTitle,
		WS_OVERLAPPEDWINDOW,
		CW_USEDEFAULT, 0, CW_USEDEFAULT, 0,
		nullptr, nullptr, hInstance, nullptr);
	if (!hWnd) {
		return FALSE;
	}
	ShowWindow(hWnd, nCmdShow);
	UpdateWindow(hWnd);

	auto calcUnitTime = 1e-3;
	auto sampleRate = 48000;
	DIV_SAMPLES = (int32_t)(sampleRate * calcUnitTime);
	DIV_SIZE = sizeof(float) * 2 * DIV_SAMPLES;
	BUFFER_SAMPLES = DIV_SAMPLES * DIV_COUNT;
	clRiffWav = new RiffWav();
	clRiffWav->Load(L"", sampleRate, BUFFER_SAMPLES, 0.5);
	clWaveOut = new WaveOut(
		sampleRate,
		2,
		Wave::EBufferType::FLOAT32,
		BUFFER_SAMPLES,
		16,
		WriteBuffer, nullptr
	);
	clWaveOut->SetDevice(0, true);
	clSpectrum = new Spectrum(sampleRate);
	clSpectrum->Pitch = 0.66;
	clWaveSynth = new WaveSynth(clSpectrum);

	// メイン メッセージ ループ
	MSG msg;
	auto hAccelTable = LoadAccelerators(hInstance, MAKEINTRESOURCE(IDC_SPECTRUMANALYZER));
	while (GetMessage(&msg, nullptr, 0, 0)) {
		if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg)) {
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}
	}

	return (int)msg.wParam;
}

// メッセージプロシージャ:
LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam) {
	switch (message) {
	case WM_COMMAND:
	{
		auto wmId = LOWORD(wParam);
		// 選択されたメニューの解析:
		switch (wmId) {
		case IDM_ABOUT:
			DialogBox(hInst, MAKEINTRESOURCE(IDD_ABOUTBOX), hWnd, About);
			break;
		case IDM_EXIT:
			DestroyWindow(hWnd);
			break;
		default:
			return DefWindowProc(hWnd, message, wParam, lParam);
		}
	}
	break;
	case WM_PAINT:
	{
		PAINTSTRUCT ps;
		auto hdc = BeginPaint(hWnd, &ps);
		EndPaint(hWnd, &ps);
	}
	break;
	case WM_DESTROY:
		PostQuitMessage(0);
		break;
	default:
		return DefWindowProc(hWnd, message, wParam, lParam);
	}
	return 0;
}

// バージョン情報ボックスのメッセージ ハンドラーです。
INT_PTR CALLBACK About(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam) {
	UNREFERENCED_PARAMETER(lParam);
	switch (message) {
	case WM_INITDIALOG:
		return (INT_PTR)TRUE;

	case WM_COMMAND:
		if (LOWORD(wParam) == IDOK || LOWORD(wParam) == IDCANCEL) {
			EndDialog(hDlg, LOWORD(wParam));
			return (INT_PTR)TRUE;
		}
		break;
	}
	return (INT_PTR)FALSE;
}

void WriteBuffer(LPSTR lpData) {
	auto pDivBuffer = (float *)lpData;
	clRiffWav->fpRead(clRiffWav, pDivBuffer);
	for (int d = 0; d < DIV_COUNT; ++d) {
		clSpectrum->Update(pDivBuffer, DIV_SAMPLES);
		memset(pDivBuffer, 0, DIV_SIZE);
		clWaveSynth->WriteBuffer(pDivBuffer, DIV_SAMPLES);
		pDivBuffer += DIV_SAMPLES * 2;
	}
	if (clRiffWav->Position >= clRiffWav->SampleNum) {
		clRiffWav->Position = 0;
	}
}
