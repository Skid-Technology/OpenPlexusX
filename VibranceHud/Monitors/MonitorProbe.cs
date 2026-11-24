using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace VibranceHud.Monitors;

public static class MonitorProbe
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

	[DllImport("user32.dll")]
	private static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc proc, nint data);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(nint hMonitor, ref uint count);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetPhysicalMonitorsFromHMONITOR(nint hMonitor, uint count, [Out] PHYSICAL_MONITOR[] monitors);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool DestroyPhysicalMonitor(nint hMonitor);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetMonitorBrightness(nint h, ref uint min, ref uint cur, ref uint max);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetMonitorContrast(nint h, ref uint min, ref uint cur, ref uint max);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetMonitorRedGreenOrBlueGain(nint h, uint gainType, ref uint min, ref uint cur, ref uint max);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetCapabilitiesStringLength(nint h, ref uint length);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool CapabilitiesRequestAndCapabilitiesReply(nint h, [Out] byte[] buffer, uint bufferLength);

	[DllImport("dxva2.dll", SetLastError = true)]
	private static extern bool GetVCPFeatureAndVCPFeatureReply(nint h, byte vcpCode, nint pvct, ref uint currentValue, ref uint maxValue);

	public static IReadOnlyList<MonitorCapability> Probe()
	{
		List<MonitorCapability> list = new List<MonitorCapability>();
		try
		{
			List<nint> handles = new List<nint>();
			EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Collect, IntPtr.Zero);
			list.AddRange(NumberThem(handles, Describe));
			bool Collect(nint hMonitor, nint hdc, ref RECT rect, nint data)
			{
				try
				{
					handles.Add(hMonitor);
				}
				catch
				{
				}
				return true;
			}
		}
		catch (Exception ex)
		{
			list.Add(new MonitorCapability("Display", SupportsBrightness: false, SupportsContrast: false, SupportsRgbGain: false, 0, 0, 0, "Windows would not list the monitors: " + ex.Message));
		}
		if (list.Count == 0)
		{
			list.Add(new MonitorCapability("Display", SupportsBrightness: false, SupportsContrast: false, SupportsRgbGain: false, 0, 0, 0, "No monitor answered. This is normal on a laptop's built-in screen."));
		}
		return list;
	}

	internal static List<MonitorCapability> NumberThem(IReadOnlyList<nint> hMonitors, Func<nint, List<MonitorCapability>> describe)
	{
		List<MonitorCapability> list = new List<MonitorCapability>();
		foreach (nint hMonitor in hMonitors)
		{
			foreach (MonitorCapability item in describe(hMonitor))
			{
				list.Add(item with
				{
					Index = list.Count
				});
			}
		}
		return list;
	}

	private static List<MonitorCapability> Describe(nint hMonitor)
	{
		List<MonitorCapability> list = new List<MonitorCapability>();
		uint count = 0u;
		PHYSICAL_MONITOR[] array;
		try
		{
			if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, ref count) || count == 0)
			{
				list.Add(Refused("Display", "This display does not expose a physical monitor to Windows."));
				return list;
			}
			array = new PHYSICAL_MONITOR[count];
			if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, count, array))
			{
				list.Add(Refused("Display", "Windows could not open a connection to this monitor."));
				return list;
			}
		}
		catch (Exception ex)
		{
			list.Add(Refused("Display", "This monitor could not be opened: " + ex.Message));
			return list;
		}
		PHYSICAL_MONITOR[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			PHYSICAL_MONITOR m = array2[i];
			try
			{
				list.Add(Interrogate(m));
			}
			catch (Exception ex2)
			{
				list.Add(Refused(Name(m.szPhysicalMonitorDescription), "This monitor stopped responding while being read: " + ex2.Message));
			}
			finally
			{
				try
				{
					DestroyPhysicalMonitor(m.hPhysicalMonitor);
				}
				catch
				{
				}
			}
		}
		return list;
	}

	internal static int TrustFirstBrightnessRead(int min, int first, Func<int?> readAgain)
	{
		if (first > min)
		{
			return first;
		}
		int? num = readAgain();
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (valueOrDefault > min)
			{
				return valueOrDefault;
			}
		}
		return -1;
	}

	private static MonitorCapability Interrogate(PHYSICAL_MONITOR m)
	{
		string description = Name(m.szPhysicalMonitorDescription);
		uint bMin = 0u;
		uint bCur = 0u;
		uint bMax = 0u;
		bool flag = TryRead(() => GetMonitorBrightness(m.hPhysicalMonitor, ref bMin, ref bCur, ref bMax));
		if (flag && (bMax <= bMin || bCur < bMin || bCur > bMax))
		{
			flag = false;
			bMin = (bCur = (bMax = 0u));
		}
		int brightnessCurrent = (int)bCur;
		if (flag)
		{
			brightnessCurrent = TrustFirstBrightnessRead((int)bMin, (int)bCur, () =>
			{
				uint min2 = 0u;
				uint cur2 = 0u;
				uint max2 = 0u;
				return (!TryRead(() => GetMonitorBrightness(m.hPhysicalMonitor, ref min2, ref cur2, ref max2)) || max2 <= min2 || cur2 < min2 || cur2 > max2) ? ((int?)null) : new int?((int)cur2);
			});
		}
		uint cMin = 0u;
		uint cCur = 0u;
		uint cMax = 0u;
		bool flag2 = TryRead(() => GetMonitorContrast(m.hPhysicalMonitor, ref cMin, ref cCur, ref cMax)) && cMax > cMin;
		uint gMin = 0u;
		uint gCur = 0u;
		uint gMax = 0u;
		bool flag3 = TryRead(() => GetMonitorRedGreenOrBlueGain(m.hPhysicalMonitor, 2u, ref gMin, ref gCur, ref gMax)) && gMax > gMin;
		uint rMin = 0u;
		uint rCur = 0u;
		uint rMax = 0u;
		bool flag4 = TryRead(() => GetMonitorRedGreenOrBlueGain(m.hPhysicalMonitor, 0u, ref rMin, ref rCur, ref rMax)) && rMax > rMin;
		uint grMin = 0u;
		uint grCur = 0u;
		uint grMax = 0u;
		bool flag5 = TryRead(() => GetMonitorRedGreenOrBlueGain(m.hPhysicalMonitor, 1u, ref grMin, ref grCur, ref grMax)) && grMax > grMin;
		IReadOnlyList<byte> readOnlyList = TryReadInputSourceValues(m.hPhysicalMonitor);
		byte? currentInputSource = null;
		if (readOnlyList.Count > 0)
		{
			uint ivCur = 0u;
			uint ivMax = 0u;
			if (TryRead(() => GetVCPFeatureAndVCPFeatureReply(m.hPhysicalMonitor, 96, IntPtr.Zero, ref ivCur, ref ivMax)))
			{
				currentInputSource = (byte)ivCur;
			}
		}
		string refusal = ((flag | flag2 | flag3) ? "" : "This monitor is connected but will not accept DDC/CI control. That is usually a monitor setting - look for \"DDC/CI\" in its on-screen menu.");
		return new MonitorCapability(description, flag, flag2, flag3, (int)bMin, brightnessCurrent, (int)bMax, refusal)
		{
			Contrast = (flag2 ? new PanelRange((int)cMin, (int)cCur, (int)cMax) : default(PanelRange)),
			BlueGain = (flag3 ? new PanelRange((int)gMin, (int)gCur, (int)gMax) : default(PanelRange)),
			SupportsRedGain = flag4,
			SupportsGreenGain = flag5,
			RedGain = (flag4 ? new PanelRange((int)rMin, (int)rCur, (int)rMax) : default(PanelRange)),
			GreenGain = (flag5 ? new PanelRange((int)grMin, (int)grCur, (int)grMax) : default(PanelRange)),
			InputSourceValues = readOnlyList,
			CurrentInputSource = currentInputSource
		};
	}

	private static IReadOnlyList<byte> TryReadInputSourceValues(nint h)
	{
		try
		{
			uint length = 0u;
			if (!GetCapabilitiesStringLength(h, ref length) || length == 0)
			{
				return Array.Empty<byte>();
			}
			byte[] array = new byte[length];
			if (!CapabilitiesRequestAndCapabilitiesReply(h, array, length))
			{
				return Array.Empty<byte>();
			}
			int num = Array.IndexOf(array, (byte)0);
			return ParseFeatureValues(Encoding.ASCII.GetString(array, 0, (num < 0) ? array.Length : num), 96);
		}
		catch
		{
			return Array.Empty<byte>();
		}
	}

	internal static IReadOnlyList<byte> ParseFeatureValues(string capabilities, byte code)
	{
		string text = code.ToString("X2");
		int startIndex = 0;
		int i;
		while (true)
		{
			startIndex = capabilities.IndexOf(text, startIndex, StringComparison.OrdinalIgnoreCase);
			if (startIndex < 0)
			{
				return Array.Empty<byte>();
			}
			int num = startIndex + text.Length;
			bool num2 = startIndex == 0 || !IsHexDigit(capabilities[startIndex - 1]);
			bool flag = num >= capabilities.Length || !IsHexDigit(capabilities[num]);
			if (num2 & flag)
			{
				for (i = num; i < capabilities.Length && capabilities[i] == ' '; i++)
				{
				}
				if (i < capabilities.Length && capabilities[i] == '(')
				{
					break;
				}
				startIndex = num;
			}
			else
			{
				startIndex = num;
			}
		}
		int num3 = capabilities.IndexOf(')', i);
		if (num3 < 0)
		{
			return Array.Empty<byte>();
		}
		List<byte> list = new List<byte>();
		int num4 = i + 1;
		string[] array = capabilities.Substring(num4, num3 - num4).Split(' ', StringSplitOptions.RemoveEmptyEntries);
		for (int j = 0; j < array.Length; j++)
		{
			if (byte.TryParse(array[j], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
			{
				list.Add(result);
			}
		}
		return list;
	}

	private static bool IsHexDigit(char c)
	{
		if ((c < '0' || c > '9') && (c < 'A' || c > 'F'))
		{
			if (c >= 'a')
			{
				return c <= 'f';
			}
			return false;
		}
		return true;
	}

	private static bool TryRead(Func<bool> call)
	{
		try
		{
			return call();
		}
		catch
		{
			return false;
		}
	}

	private static string Name(string? description)
	{
		if (!string.IsNullOrWhiteSpace(description))
		{
			return description.Trim();
		}
		return "Display";
	}

	private static MonitorCapability Refused(string name, string why)
	{
		return new MonitorCapability(name, SupportsBrightness: false, SupportsContrast: false, SupportsRgbGain: false, 0, 0, 0, why);
	}
}
