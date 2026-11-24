using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VibranceHud.Monitors;

public static class MonitorDevices
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct MONITORINFOEX
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string szDevice;
	}

	private struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	private delegate bool MonitorEnumProc(nint hMonitor, nint hdc, nint rect, nint data);

	[DllImport("user32.dll")]
	private static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc proc, nint data);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX info);

	public static string? DeviceNameForIndex(int index)
	{
		List<string> names = new List<string>();
		EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Collect, IntPtr.Zero);
		if (index < 0 || index >= names.Count || names[index].Length <= 0)
		{
			return null;
		}
		return names[index];
		bool Collect(nint hMonitor, nint hdc, nint rect, nint data)
		{
			MONITORINFOEX info = new MONITORINFOEX
			{
				cbSize = Marshal.SizeOf<MONITORINFOEX>()
			};
			names.Add(GetMonitorInfo(hMonitor, ref info) ? info.szDevice : "");
			return true;
		}
	}
}
