using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VibranceHud.Monitors;

public static class MonitorControl
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct PHYSICAL_MONITOR
	{
		public nint hPhysicalMonitor;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string szPhysicalMonitorDescription;
	}

	private delegate bool MonitorEnumProc(nint hMonitor, nint hdc, ref RECT rect, nint data);

	private const uint Red = 0u;

	private const uint Green = 1u;

	private const uint Blue = 2u;

	private const byte VcpInputSource = 96;

	private const byte VcpRestoreColourDefaults = 8;

	[DllImport("user32.dll")]
	private static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc proc, nint data);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(nint h, ref uint count);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetPhysicalMonitorsFromHMONITOR(nint h, uint count, [Out] PHYSICAL_MONITOR[] monitors);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool DestroyPhysicalMonitor(nint h);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool SetMonitorBrightness(nint h, uint value);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetMonitorBrightness(nint h, ref uint min, ref uint cur, ref uint max);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool SetMonitorContrast(nint h, uint value);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool SetMonitorRedGreenOrBlueGain(nint h, uint gainType, uint value);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool SetVCPFeature(nint h, byte vcpCode, uint value);

	public static bool RestoreColourDefaults(int monitorIndex)
	{
		return OnMonitor(monitorIndex, (nint h) => SetVCPFeature(h, 8, 1u));
	}

	public static bool SetBrightnessPercent(int monitorIndex, PanelRange range, int percent)
	{
		return OnMonitor(monitorIndex, (nint h) => SetMonitorBrightness(h, (uint)range.FromPercent(percent)));
	}

	public static bool SetContrastPercent(int monitorIndex, PanelRange range, int percent)
	{
		return OnMonitor(monitorIndex, (nint h) => SetMonitorContrast(h, (uint)range.FromPercent(percent)));
	}

	internal static int LowBlueTarget(PanelRange gain, int strength)
	{
		int num = (gain.Current - gain.Min) / 2;
		return gain.Current - num * Math.Clamp(strength, 0, 100) / 100;
	}

	public static bool SetLowBlueLight(int monitorIndex, PanelRange gain, int strength)
	{
		int target = LowBlueTarget(gain, strength);
		return OnMonitor(monitorIndex, (nint h) => SetMonitorRedGreenOrBlueGain(h, 2u, (uint)Math.Clamp(target, gain.Min, gain.Max)));
	}

	public static bool SetRedGainPercent(int monitorIndex, PanelRange range, int percent)
	{
		return OnMonitor(monitorIndex, (nint h) => SetMonitorRedGreenOrBlueGain(h, 0u, (uint)range.FromPercent(percent)));
	}

	public static bool SetGreenGainPercent(int monitorIndex, PanelRange range, int percent)
	{
		return OnMonitor(monitorIndex, (nint h) => SetMonitorRedGreenOrBlueGain(h, 1u, (uint)range.FromPercent(percent)));
	}

	public static bool SetInputSource(int monitorIndex, byte value)
	{
		return OnMonitor(monitorIndex, (nint h) => SetVCPFeature(h, 96, value));
	}

	public static bool RestoreBrightness(int monitorIndex, int raw)
	{
		return OnMonitor(monitorIndex, (nint h) => SetMonitorBrightness(h, (uint)raw));
	}

	public static bool RestoreContrast(int monitorIndex, int raw)
	{
		return OnMonitor(monitorIndex, (nint h) => SetMonitorContrast(h, (uint)raw));
	}

	public static bool RestoreBlueGain(int monitorIndex, int raw)
	{
		return OnMonitor(monitorIndex, (nint h) => SetMonitorRedGreenOrBlueGain(h, 2u, (uint)raw));
	}

	public static bool RestoreRedGain(int monitorIndex, int raw)
	{
		return OnMonitor(monitorIndex, (nint h) => SetMonitorRedGreenOrBlueGain(h, 0u, (uint)raw));
	}

	public static bool RestoreGreenGain(int monitorIndex, int raw)
	{
		return OnMonitor(monitorIndex, (nint h) => SetMonitorRedGreenOrBlueGain(h, 1u, (uint)raw));
	}

	public static (bool Brightness, bool Contrast, bool BlueGain, bool RedGain, bool GreenGain) RestoreAll(int monitorIndex, int? brightness, int? contrast, int? blueGain, int? redGain = null, int? greenGain = null)
	{
		bool bOk = false;
		bool cOk = false;
		bool gOk = false;
		bool rOk = false;
		bool grOk = false;
		OnMonitor(monitorIndex, (nint h) =>
		{
			if (brightness.HasValue)
			{
				bOk = SetMonitorBrightness(h, (uint)brightness.Value);
			}
			if (contrast.HasValue)
			{
				cOk = SetMonitorContrast(h, (uint)contrast.Value);
			}
			if (blueGain.HasValue)
			{
				gOk = SetMonitorRedGreenOrBlueGain(h, 2u, (uint)blueGain.Value);
			}
			if (redGain.HasValue)
			{
				rOk = SetMonitorRedGreenOrBlueGain(h, 0u, (uint)redGain.Value);
			}
			if (greenGain.HasValue)
			{
				grOk = SetMonitorRedGreenOrBlueGain(h, 1u, (uint)greenGain.Value);
			}
			return true;
		});
		return (Brightness: bOk, Contrast: cOk, BlueGain: gOk, RedGain: rOk, GreenGain: grOk);
	}

	public static int? ReadTrustedBrightness(int monitorIndex)
	{
		int? result = ReadBrightness(monitorIndex);
		if (!result.HasValue || result.GetValueOrDefault() != 0)
		{
			return result;
		}
		int? result2 = ReadBrightness(monitorIndex);
		if (!result2.HasValue || result2.GetValueOrDefault() != 0)
		{
			return result2;
		}
		return null;
	}

	private static int? ReadBrightness(int monitorIndex)
	{
		int? result = null;
		OnMonitor(monitorIndex, (nint h) =>
		{
			uint min = 0u;
			uint cur = 0u;
			uint max = 0u;
			if (!GetMonitorBrightness(h, ref min, ref cur, ref max))
			{
				return false;
			}
			if (max <= min)
			{
				return false;
			}
			result = (int)cur;
			return true;
		});
		return result;
	}

	private static bool OnMonitor(int monitorIndex, Func<nint, bool> action)
	{
		try
		{
			List<nint> handles = new List<nint>();
			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Collect, IntPtr.Zero);
			return OnMonitorCore(monitorIndex, handles, OpenPhysical, ReleasePhysical, action);
			bool Collect(nint h, nint hdc, ref RECT r, nint d)
			{
				try
				{
					handles.Add(h);
				}
				catch
				{
				}
				return true;
			}
		}
		catch
		{
			return false;
		}
	}

	private static nint[]? OpenPhysical(nint hMonitor)
	{
		uint count = 0u;
		if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, ref count) || count == 0)
		{
			return null;
		}
		PHYSICAL_MONITOR[] array = new PHYSICAL_MONITOR[count];
		if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, count, array))
		{
			return null;
		}
		nint[] array2 = new nint[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = array[i].hPhysicalMonitor;
		}
		return array2;
	}

	private static void ReleasePhysical(nint[] handles)
	{
		foreach (nint h in handles)
		{
			try
			{
				DestroyPhysicalMonitor(h);
			}
			catch
			{
			}
		}
	}

	internal static bool OnMonitorCore(int monitorIndex, IReadOnlyList<nint> hMonitors, Func<nint, nint[]?> open, Action<nint[]> release, Func<nint, bool> action)
	{
		int num = 0;
		foreach (nint hMonitor in hMonitors)
		{
			nint[] array;
			try
			{
				array = open(hMonitor);
			}
			catch
			{
				array = null;
			}
			if (array == null || array.Length == 0)
			{
				if (num++ == monitorIndex)
				{
					return false;
				}
				continue;
			}
			try
			{
				nint[] array2 = array;
				foreach (nint arg in array2)
				{
					if (num++ == monitorIndex)
					{
						try
						{
							return action(arg);
						}
						catch
						{
							return false;
						}
					}
				}
			}
			finally
			{
				try
				{
					release(array);
				}
				catch
				{
				}
			}
		}
		return false;
	}
}
