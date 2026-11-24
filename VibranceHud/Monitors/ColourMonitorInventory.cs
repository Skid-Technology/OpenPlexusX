using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibranceHud.Monitors;

internal static class ColourMonitorInventory
{
	private struct Luid
	{
		public uint Low;

		public int High;
	}

	private struct Rational
	{
		public uint Numerator;

		public uint Denominator;
	}

	private struct Source
	{
		public Luid Adapter;

		public uint Id;

		public uint Mode;

		public uint Status;
	}

	private struct Target
	{
		public Luid Adapter;

		public uint Id;

		public uint Mode;

		public uint Technology;

		public uint Rotation;

		public uint Scaling;

		public Rational Refresh;

		public uint Scanline;

		public int Available;

		public uint Status;
	}

	private struct Path
	{
		public Source Source;

		public Target Target;

		public uint Flags;
	}

	[StructLayout(LayoutKind.Explicit, Size = 64)]
	private struct Mode
	{
		[FieldOffset(0)]
		public uint Type;
	}

	private struct Header
	{
		public uint Type;

		public uint Size;

		public Luid Adapter;

		public uint Id;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct SourceName
	{
		public Header Header;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string Name;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct TargetName
	{
		public Header Header;

		public uint Flags;

		public uint Technology;

		public ushort Manufacturer;

		public ushort Product;

		public uint Connector;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string Name;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DevicePath;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct Device
	{
		public int Size;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string Name;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string Description;

		public uint Flags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string Id;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string Key;
	}

	[DllImport("user32.dll")]
	private static extern int GetDisplayConfigBufferSizes(uint flags, out uint paths, out uint modes);

	[DllImport("user32.dll")]
	private static extern int QueryDisplayConfig(uint flags, ref uint paths, [In][Out] Path[] pathInfo, ref uint modes, [In][Out] Mode[] modeInfo, nint topology);

	[DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
	private static extern int GetSourceName(ref SourceName name);

	[DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
	private static extern int GetTargetName(ref TargetName name);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool EnumDisplayDevices(string device, uint index, ref Device monitor, uint flags);

	private static string InterfacePath(string deviceName)
	{
		Device monitor = new Device
		{
			Size = Marshal.SizeOf<Device>()
		};
		if (!EnumDisplayDevices(deviceName, 0u, ref monitor, 1u))
		{
			return "";
		}
		return monitor.Id ?? "";
	}

	internal static IReadOnlyList<ColourMonitor> Read()
	{
		Screen[] allScreens = Screen.AllScreens;
		List<ColourMonitor> list = new List<ColourMonitor>();
		for (int i = 0; i < 3; i++)
		{
			if (GetDisplayConfigBufferSizes(2u, out var paths, out var modes) != 0 || paths > 256 || modes > 1024)
			{
				break;
			}
			Path[] p = new Path[paths];
			Mode[] modeInfo = new Mode[modes];
			switch (QueryDisplayConfig(2u, ref paths, p, ref modes, modeInfo, IntPtr.Zero))
			{
			case 0:
			{
				int j;
				for (j = 0; j < paths; j++)
				{
					SourceName source = new SourceName
					{
						Header = new Header
						{
							Type = 1u,
							Size = (uint)Marshal.SizeOf<SourceName>(),
							Adapter = p[j].Source.Adapter,
							Id = p[j].Source.Id
						}
					};
					TargetName name = new TargetName
					{
						Header = new Header
						{
							Type = 2u,
							Size = (uint)Marshal.SizeOf<TargetName>(),
							Adapter = p[j].Target.Adapter,
							Id = p[j].Target.Id
						}
					};
					if (GetSourceName(ref source) != 0)
					{
						continue;
					}
					Screen screen = allScreens.FirstOrDefault((Screen s) => string.Equals(s.DeviceName, source.Name, StringComparison.OrdinalIgnoreCase));
					if (screen != null)
					{
						bool flag = GetTargetName(ref name) == 0;
						string text = (flag ? (name.DevicePath ?? "") : "");
						if (string.IsNullOrWhiteSpace(text) && p.Take((int)paths).Count((Path x) => x.Source.Adapter.Low == p[j].Source.Adapter.Low && x.Source.Adapter.High == p[j].Source.Adapter.High && x.Source.Id == p[j].Source.Id) == 1)
						{
							text = InterfacePath(screen.DeviceName);
						}
						string name2 = ((flag && !string.IsNullOrWhiteSpace(name.Name)) ? name.Name : source.Name);
						int refreshHz = ((p[j].Target.Refresh.Denominator != 0) ? ((int)Math.Round((double)p[j].Target.Refresh.Numerator / (double)p[j].Target.Refresh.Denominator)) : 0);
						list.Add(new ColourMonitor(text, screen.DeviceName, name2, screen.Bounds, screen.Primary, refreshHz));
					}
				}
				break;
			}
			case 122:
				continue;
			}
			break;
		}
		Screen[] array = allScreens;
		foreach (Screen screen2 in array)
		{
			if (!list.Any((ColourMonitor x) => string.Equals(x.DeviceName, screen2.DeviceName, StringComparison.OrdinalIgnoreCase)))
			{
				list.Add(new ColourMonitor(InterfacePath(screen2.DeviceName), screen2.DeviceName, screen2.DeviceName, screen2.Bounds, screen2.Primary));
			}
		}
		return list;
	}
}
