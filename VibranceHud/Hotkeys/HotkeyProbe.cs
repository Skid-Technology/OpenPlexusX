using System;
using System.Runtime.InteropServices;

namespace VibranceHud.Hotkeys;

internal static class HotkeyProbe
{
	internal const int ErrorHotkeyAlreadyRegistered = 1409;

	private const int ProbeId = 31317;

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UnregisterHotKey(nint hWnd, int id);

	internal static int Real(nint hwnd, uint mask, uint vk)
	{
		if (hwnd == IntPtr.Zero)
		{
			return 0;
		}
		return Run(() => RegisterHotKey(hwnd, 31317, mask, vk), () => Marshal.GetLastWin32Error(), () =>
		{
			UnregisterHotKey(hwnd, 31317);
		});
	}

	internal static int Run(Func<bool> register, Func<int> lastError, Action unregister)
	{
		if (register())
		{
			unregister();
			return 0;
		}
		int num = lastError();
		if (num != 0)
		{
			return num;
		}
		return -1;
	}
}
