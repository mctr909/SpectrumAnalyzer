namespace WinMM {
	internal enum MMCallback : uint {
		NULL     = 0x00000000,
		WINDOW   = 0x00010000,
		TASK     = 0x00020000,
		FUNCTION = 0x00030000,
		EVENT    = 0x00050000
	}
}
