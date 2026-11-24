using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VibranceHud.Fullscreen;

public static class ForegroundWindowProcess
{
	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

	public static string? Name()
	{
		try
		{
			nint foregroundWindow = GetForegroundWindow();
			if (foregroundWindow == IntPtr.Zero)
			{
				return null;
			}
			if (GetWindowThreadProcessId(foregroundWindow, out var processId) == 0 || processId == 0)
			{
				return null;
			}
			using Process process = Process.GetProcessById((int)processId);
			string processName = process.ProcessName;
			return string.IsNullOrEmpty(processName) ? null : processName;
		}
		catch
		{
			return null;
		}
	}
}
