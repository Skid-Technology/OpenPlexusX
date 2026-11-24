using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace VibranceHud.Fullscreen;

public static class AnyFullScreenWindow
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;

		public Rectangle ToRectangle()
		{
			return Rectangle.FromLTRB(Left, Top, Right, Bottom);
		}
	}

	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	private const int QUNS_BUSY = 2;

	private const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;

	private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

	private const uint MONITOR_DEFAULTTONEAREST = 2u;

	[DllImport("shell32.dll")]
	private static extern int SHQueryUserNotificationState(out int state);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWindowVisible(nint hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsIconic(nint hWnd);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
	private static extern int GetClassName(nint hWnd, StringBuilder className, int maxCount);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowRect(nint hWnd, out RECT rect);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hWnd, int attribute, out RECT value, int size);

	[DllImport("user32.dll")]
	private static extern nint MonitorFromWindow(nint hWnd, uint flags);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetMonitorInfo(nint monitor, ref MONITORINFO info);

	internal static bool ForShellState(int state)
	{
		if (state != 2)
		{
			return state == 3;
		}
		return true;
	}

	internal static bool FillsItsMonitor(Rectangle window, Rectangle monitor, bool visible, bool minimized, bool isShellOrDesktop, bool isOwnProcess)
	{
		if (!visible | minimized | isShellOrDesktop | isOwnProcess)
		{
			return false;
		}
		if (monitor.Width <= 0 || monitor.Height <= 0)
		{
			return false;
		}
		if (window.Left <= monitor.Left && window.Top <= monitor.Top && window.Right >= monitor.Right)
		{
			return window.Bottom >= monitor.Bottom;
		}
		return false;
	}

	internal static bool IsShellOrDesktopClass(string? className)
	{
		switch (className)
		{
		case "Progman":
		case "WorkerW":
		case "Shell_TrayWnd":
		case "Shell_SecondaryTrayWnd":
			return true;
		default:
			return false;
		}
	}

	public static bool IsInFront()
	{
		try
		{
			if (SHQueryUserNotificationState(out var state) == 0 && ForShellState(state))
			{
				return true;
			}
			return ForegroundFillsItsMonitor();
		}
		catch
		{
			return false;
		}
	}

	internal static string? ForegroundMonitorDeviceName()
	{
		try
		{
			nint foregroundWindow = GetForegroundWindow();
			return (foregroundWindow == IntPtr.Zero) ? null : Screen.FromHandle(foregroundWindow).DeviceName;
		}
		catch
		{
			return null;
		}
	}

	private static bool ForegroundFillsItsMonitor()
	{
		nint foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == IntPtr.Zero)
		{
			return false;
		}
		bool visible = IsWindowVisible(foregroundWindow);
		bool minimized = IsIconic(foregroundWindow);
		StringBuilder stringBuilder = new StringBuilder(256);
		string className = ((GetClassName(foregroundWindow, stringBuilder, stringBuilder.Capacity) > 0) ? stringBuilder.ToString() : null);
		GetWindowThreadProcessId(foregroundWindow, out var processId);
		bool isOwnProcess = processId != 0 && processId == (uint)Environment.ProcessId;
		if (DwmGetWindowAttribute(foregroundWindow, 9, out var value, Marshal.SizeOf<RECT>()) != 0 && !GetWindowRect(foregroundWindow, out value))
		{
			return false;
		}
		nint num = MonitorFromWindow(foregroundWindow, 2u);
		if (num == IntPtr.Zero)
		{
			return false;
		}
		MONITORINFO info = new MONITORINFO
		{
			cbSize = Marshal.SizeOf<MONITORINFO>()
		};
		if (!GetMonitorInfo(num, ref info))
		{
			return false;
		}
		return FillsItsMonitor(value.ToRectangle(), info.rcMonitor.ToRectangle(), visible, minimized, IsShellOrDesktopClass(className), isOwnProcess);
	}
}
