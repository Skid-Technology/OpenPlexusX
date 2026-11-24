using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VibranceHud.Controls;
using VibranceHud.Display;

namespace VibranceHud;

public static class DisplayController
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct DEVMODE
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string dmDeviceName;

		public short dmSpecVersion;

		public short dmDriverVersion;

		public short dmSize;

		public short dmDriverExtra;

		public int dmFields;

		public int dmPositionX;

		public int dmPositionY;

		public int dmDisplayOrientation;

		public int dmDisplayFixedOutput;

		public short dmColor;

		public short dmDuplex;

		public short dmYResolution;

		public short dmTTOption;

		public short dmCollate;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string dmFormName;

		public short dmLogPixels;

		public int dmBitsPerPel;

		public int dmPelsWidth;

		public int dmPelsHeight;

		public int dmDisplayFlags;

		public int dmDisplayFrequency;

		public int dmICMMethod;

		public int dmICMIntent;

		public int dmMediaType;

		public int dmDitherType;

		public int dmReserved1;

		public int dmReserved2;

		public int dmPanningWidth;

		public int dmPanningHeight;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct DISPLAY_DEVICE
	{
		public int cb;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string DeviceName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceString;

		public int StateFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceID;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceKey;
	}

	private const int ENUM_CURRENT_SETTINGS = -1;

	private const int CDS_UPDATEREGISTRY = 1;

	private const int CDS_TEST = 2;

	private const int CDS_NORESET = 268435456;

	private const int DM_POSITION = 32;

	private const int DM_PELSWIDTH = 524288;

	private const int DM_PELSHEIGHT = 1048576;

	private const int DM_DISPLAYFREQUENCY = 4194304;

	private const int DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 1;

	private const int DISPLAY_DEVICE_PRIMARY_DEVICE = 4;

	public static Action? OnRestored { get; set; }

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE dm);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int ChangeDisplaySettingsEx(string? lpszDeviceName, ref DEVMODE lpDevMode, nint hwnd, int dwflags, nint lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
	private static extern int CommitStagedDisplaySettings(nint lpszDeviceName, nint lpDevMode, nint hwnd, int dwflags, nint lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool EnumDisplayDevices(string? lpDevice, int iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, int dwFlags);

	private static DEVMODE NewDevMode()
	{
		DEVMODE result = new DEVMODE
		{
			dmDeviceName = new string('\0', 32),
			dmFormName = new string('\0', 32)
		};
		result.dmSize = (short)Marshal.SizeOf<DEVMODE>();
		return result;
	}

	internal static DispChangeResult MapDispChange(int code)
	{
		return code switch
		{
			0 => DispChangeResult.Successful, 
			1 => DispChangeResult.Restart, 
			-2 => DispChangeResult.BadMode, 
			-3 => DispChangeResult.NotUpdated, 
			-4 => DispChangeResult.BadFlags, 
			-5 => DispChangeResult.BadParam, 
			-6 => DispChangeResult.BadDualView, 
			_ => DispChangeResult.Failed, 
		};
	}

	public static IReadOnlyList<MonitorDescriptor> Monitors()
	{
		List<MonitorDescriptor> list = new List<MonitorDescriptor>();
		DISPLAY_DEVICE lpDisplayDevice = new DISPLAY_DEVICE
		{
			cb = Marshal.SizeOf<DISPLAY_DEVICE>()
		};
		for (int i = 0; EnumDisplayDevices(null, i, ref lpDisplayDevice, 0); i++)
		{
			if ((lpDisplayDevice.StateFlags & 1) != 0)
			{
				DISPLAY_DEVICE lpDisplayDevice2 = new DISPLAY_DEVICE
				{
					cb = Marshal.SizeOf<DISPLAY_DEVICE>()
				};
				string friendlyName = (EnumDisplayDevices(lpDisplayDevice.DeviceName, 0, ref lpDisplayDevice2, 0) ? lpDisplayDevice2.DeviceString.TrimEnd('\0') : lpDisplayDevice.DeviceString.TrimEnd('\0'));
				list.Add(new MonitorDescriptor(lpDisplayDevice.DeviceName.TrimEnd('\0'), friendlyName, (lpDisplayDevice.StateFlags & 4) != 0));
			}
			lpDisplayDevice = new DISPLAY_DEVICE
			{
				cb = Marshal.SizeOf<DISPLAY_DEVICE>()
			};
		}
		list.Sort((MonitorDescriptor a, MonitorDescriptor b) => b.IsPrimary.CompareTo(a.IsPrimary));
		return list;
	}

	internal static IReadOnlyList<MonitorPlacement> Placements()
	{
		List<MonitorPlacement> list = new List<MonitorPlacement>();
		foreach (MonitorDescriptor item in Monitors())
		{
			DEVMODE dm = NewDevMode();
			if (EnumDisplaySettings(item.DeviceName, -1, ref dm))
			{
				list.Add(new MonitorPlacement(item.DeviceName, dm.dmPositionX, dm.dmPositionY));
			}
		}
		return list;
	}

	internal static bool PlaceMonitors(IReadOnlyList<MonitorPlacement> placements)
	{
		if (placements.Count == 0)
		{
			return true;
		}
		foreach (MonitorPlacement placement in placements)
		{
			DEVMODE dm = NewDevMode();
			if (!EnumDisplaySettings(placement.DeviceName, -1, ref dm))
			{
				return false;
			}
			dm.dmPositionX = placement.X;
			dm.dmPositionY = placement.Y;
			dm.dmFields = 32;
			if (MapDispChange(ChangeDisplaySettingsEx(placement.DeviceName, ref dm, IntPtr.Zero, 268435457, IntPtr.Zero)) != DispChangeResult.Successful)
			{
				return false;
			}
		}
		return MapDispChange(CommitStagedDisplaySettings(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero)) == DispChangeResult.Successful;
	}

	public static IReadOnlyList<DisplayMode> SupportedModes(string? deviceName = null)
	{
		List<DisplayMode> list = new List<DisplayMode>();
		DEVMODE dm = NewDevMode();
		for (int i = 0; EnumDisplaySettings(deviceName, i, ref dm); i++)
		{
			if (dm.dmBitsPerPel == 32)
			{
				list.Add(new DisplayMode(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency));
			}
			dm = NewDevMode();
		}
		return list;
	}

	public static DisplayMode? Current(string? deviceName = null)
	{
		DEVMODE dm = NewDevMode();
		if (!EnumDisplaySettings(deviceName, -1, ref dm))
		{
			return null;
		}
		return new DisplayMode(dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency);
	}

	public static DispChangeResult ApplyEx(int width, int height, int hz, string? deviceName = null, bool persist = false)
	{
		IReadOnlyList<DisplayMode> modes = SupportedModes(deviceName);
		if (hz <= 0)
		{
			hz = DisplayModes.MaxRefreshFor(modes, width, height);
		}
		if (hz <= 0 || !DisplayModes.IsSupported(modes, width, height, hz))
		{
			return DispChangeResult.BadMode;
		}
		DEVMODE dm = NewDevMode();
		if (!EnumDisplaySettings(deviceName, -1, ref dm))
		{
			return DispChangeResult.Failed;
		}
		dm.dmPelsWidth = width;
		dm.dmPelsHeight = height;
		dm.dmDisplayFrequency = hz;
		dm.dmFields = 5767200;
		DispChangeResult dispChangeResult = MapDispChange(ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, 2, IntPtr.Zero));
		if (dispChangeResult != DispChangeResult.Successful)
		{
			return dispChangeResult;
		}
		int dwflags = (persist ? 1 : 0);
		return MapDispChange(ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, dwflags, IntPtr.Zero));
	}

	public static bool Apply(int width, int height, int hz = 0, string? deviceName = null)
	{
		return ApplyEx(width, height, hz, deviceName) switch
		{
			DispChangeResult.Successful => true, 
			DispChangeResult.BadMode => CustomModeRegistrar.Real().SwitchToSaved(width, height, hz, deviceName), 
			_ => false, 
		};
	}

	public static bool Restore(DisplayMode mode, string? deviceName = null)
	{
		return Apply(mode.Width, mode.Height, mode.RefreshHz, deviceName);
	}

	public static DispChangeResult TryApplyWithRevert(int width, int height, int hz, string? deviceName, TimeSpan grace)
	{
		DisplayMode? displayMode = Current(deviceName);
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			DispChangeResult dispChangeResult = ApplyEx(width, height, hz, deviceName);
			if (dispChangeResult != DispChangeResult.Successful)
			{
				return dispChangeResult;
			}
			bool confirmed = false;
			using (RevertConfirmForm revertConfirmForm = new RevertConfirmForm($"Switched to {width} x {height}" + ((hz > 0) ? $" at {hz} Hz" : "") + ".", Math.Max(1, (int)Math.Ceiling(grace.TotalSeconds))))
			{
				revertConfirmForm.Confirmed += () =>
				{
					confirmed = true;
				};
				revertConfirmForm.ShowDialog();
			}
			if (!confirmed)
			{
				if (!Restore(valueOrDefault, deviceName))
				{
					return DispChangeResult.Failed;
				}
				return DispChangeResult.Reverted;
			}
			ApplyEx(width, height, hz, deviceName, persist: true);
			return dispChangeResult;
		}
		return DispChangeResult.Failed;
	}

	public static void RestoreWhenGameExits(string processName, DisplayMode original, TimeSpan waitForStart, string? deviceName = null, IReadOnlyList<string>? alsoProcessNames = null)
	{
		if (string.IsNullOrWhiteSpace(processName))
		{
			return;
		}
		Task.Run((Func<Task?>)(async () =>
		{
			_ = 1;
			try
			{
				DateTime deadline = DateTime.UtcNow + waitForStart;
				List<string> names = new List<string> { processName };
				if (alsoProcessNames != null)
				{
					names.AddRange(alsoProcessNames);
				}
				while (!Running())
				{
					if (DateTime.UtcNow > deadline)
					{
						Restore(original, deviceName);
						try
						{
							OnRestored?.Invoke();
							return;
						}
						catch
						{
							return;
						}
					}
					await Task.Delay(2000);
				}
				while (Running())
				{
					await Task.Delay(3000);
				}
				Restore(original, deviceName);
				try
				{
					OnRestored?.Invoke();
				}
				catch
				{
				}
				bool Running()
				{
					return names.Any((string n) => Process.GetProcessesByName(n).Length != 0);
				}
			}
			catch
			{
				try
				{
					Restore(original, deviceName);
				}
				catch
				{
				}
				try
				{
					OnRestored?.Invoke();
				}
				catch
				{
				}
			}
		}));
	}

	public static void RestoreWhenRustExits(DisplayMode original, TimeSpan waitForStart)
	{
		RestoreWhenGameExits("RustClient", original, waitForStart);
	}

	internal static string DescribeOwed(DisplayMode mode, string? deviceName)
	{
		if (!string.IsNullOrWhiteSpace(deviceName))
		{
			return deviceName + "|" + Describe(mode);
		}
		return Describe(mode);
	}

	internal static (string? Device, DisplayMode Mode)? ParseOwed(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		int num = text.IndexOf('|');
		string text2 = null;
		if (num >= 0)
		{
			text2 = text.Substring(0, num);
			if (string.IsNullOrWhiteSpace(text2))
			{
				return null;
			}
			text = text.Substring(num + 1);
		}
		DisplayMode? displayMode = Parse(text);
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			return (text2, valueOrDefault);
		}
		return null;
	}

	internal static bool IsAttached(string? deviceName, IEnumerable<MonitorDescriptor> monitors)
	{
		if (!string.IsNullOrWhiteSpace(deviceName))
		{
			return monitors.Any((MonitorDescriptor m) => string.Equals(m.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
		}
		return true;
	}

	public static string Describe(DisplayMode mode)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(2, 3, invariantCulture);
		handler.AppendFormatted(mode.Width);
		handler.AppendLiteral("x");
		handler.AppendFormatted(mode.Height);
		handler.AppendLiteral("@");
		handler.AppendFormatted(mode.RefreshHz);
		return string.Create(invariantCulture, ref handler);
	}

	public static DisplayMode? Parse(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		string[] array = text.Split('@');
		if (array.Length != 2)
		{
			return null;
		}
		string[] array2 = array[0].Split('x');
		if (array2.Length != 2)
		{
			return null;
		}
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		if (!int.TryParse(array2[0], NumberStyles.Integer, invariantCulture, out var result) || !int.TryParse(array2[1], NumberStyles.Integer, invariantCulture, out var result2) || !int.TryParse(array[1], NumberStyles.Integer, invariantCulture, out var result3))
		{
			return null;
		}
		if (result <= 0 || result2 <= 0 || result3 < 0)
		{
			return null;
		}
		return new DisplayMode(result, result2, result3);
	}
}
