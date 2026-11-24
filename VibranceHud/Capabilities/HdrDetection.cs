using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using VibranceHud.Controls;

namespace VibranceHud.Capabilities;

internal static class HdrDetection
{
	private struct DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

		public uint value;
	}

	private struct LUID
	{
		public uint LowPart;

		public int HighPart;
	}

	private struct DISPLAYCONFIG_PATH_SOURCE_INFO
	{
		public LUID adapterId;

		public uint id;

		public uint modeInfoIdx;

		public uint statusFlags;
	}

	private struct DISPLAYCONFIG_RATIONAL
	{
		public uint Numerator;

		public uint Denominator;
	}

	private struct DISPLAYCONFIG_PATH_TARGET_INFO
	{
		public LUID adapterId;

		public uint id;

		public uint modeInfoIdx;

		public uint outputTechnology;

		public uint rotation;

		public uint scaling;

		public DISPLAYCONFIG_RATIONAL refreshRate;

		public uint scanLineOrdering;

		[MarshalAs(UnmanagedType.Bool)]
		public bool targetAvailable;

		public uint statusFlags;
	}

	private struct DISPLAYCONFIG_PATH_INFO
	{
		public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;

		public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;

		public uint flags;
	}

	[StructLayout(LayoutKind.Sequential, Size = 64)]
	private struct DISPLAYCONFIG_MODE_INFO
	{
	}

	private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
	{
		public int type;

		public int size;

		public LUID adapterId;

		public uint id;
	}

	private struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

		public uint value;

		public uint colorEncoding;

		public int bitsPerColorChannel;
	}

	private struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

		public uint value;

		public uint colorEncoding;

		public uint bitsPerColorChannel;

		public uint activeColorMode;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string viewGdiDeviceName;
	}

	private const uint ColorModeWcg = 1u;

	private const uint ColorModeHdr = 2u;

	private const uint QDC_ONLY_ACTIVE_PATHS = 2u;

	private const int DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;

	private const int DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;

	private const int DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10;

	private const int DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2 = 15;

	private const int DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE = 16;

	public static bool AnyDisplayInHdr()
	{
		return AnyDisplayInHdr(ActivePaths(), AdvancedColourOf);
	}

	public static IReadOnlyList<HdrDisplay> Report()
	{
		try
		{
			return Report(ActivePaths(), AdvancedColourOf, SourceNameOf);
		}
		catch
		{
			return Array.Empty<HdrDisplay>();
		}
	}

	internal static bool AnyDisplayInHdr<TPath>(IReadOnlyList<TPath>? paths, Func<TPath, (uint Flags, int BitsPerColorChannel)?> advancedColour)
	{
		return Displays(paths, advancedColour, (TPath _) => "", includeNames: false).Any((HdrDisplay d) => d.Enabled);
	}

	internal static IReadOnlyList<HdrDisplay> Report<TPath>(IReadOnlyList<TPath>? paths, Func<TPath, (uint Flags, int BitsPerColorChannel)?> advancedColour, Func<TPath, string> sourceName)
	{
		return Displays(paths, advancedColour, sourceName, includeNames: true).ToList();
	}

	private static IEnumerable<HdrDisplay> Displays<TPath>(IReadOnlyList<TPath>? paths, Func<TPath, (uint Flags, int BitsPerColorChannel)?> advancedColour, Func<TPath, string> sourceName, bool includeNames)
	{
		if (paths == null)
		{
			yield break;
		}
		foreach (TPath path in paths)
		{
			(uint, int)? tuple = advancedColour(path);
			if (tuple.HasValue)
			{
				(uint, int) valueOrDefault = tuple.GetValueOrDefault();
				string device = (includeNames ? sourceName(path) : "");
				bool supported = (valueOrDefault.Item1 & 1) != 0;
				bool enabled = (valueOrDefault.Item1 & 2) != 0 && (valueOrDefault.Item1 & 4) == 0;
				int item = valueOrDefault.Item2;
				yield return new HdrDisplay(device, supported, enabled, (item > 0 && item < 64) ? valueOrDefault.Item2 : 0);
			}
		}
	}

	internal static (uint Flags, int BitsPerColorChannel) FromAdvancedColorInfo2(uint value, uint activeColorMode, int bitsPerColorChannel)
	{
		uint num = 0u;
		if ((value & 0x10) != 0)
		{
			num |= 1;
		}
		switch (activeColorMode)
		{
		case 2u:
			num |= 2;
			break;
		case 1u:
			num |= 6;
			break;
		}
		return (Flags: num, BitsPerColorChannel: bitsPerColorChannel);
	}

	internal static IReadOnlyList<(string Device, uint Value, uint Mode)>? RawInfo2ForTest()
	{
		IReadOnlyList<DISPLAYCONFIG_PATH_INFO> readOnlyList = ActivePaths();
		if (readOnlyList == null)
		{
			return null;
		}
		List<(string, uint, uint)> list = new List<(string, uint, uint)>();
		foreach (DISPLAYCONFIG_PATH_INFO item in readOnlyList)
		{
			(uint, uint, uint)? tuple = ReadInfo2(item);
			if (tuple.HasValue)
			{
				(uint, uint, uint) valueOrDefault = tuple.GetValueOrDefault();
				list.Add((SourceNameOf(item), valueOrDefault.Item1, valueOrDefault.Item2));
				continue;
			}
			return null;
		}
		return list;
	}

	internal static IReadOnlyList<int> PickPaths(IReadOnlyList<string> sourceNames, string? device)
	{
		List<int> list = new List<int>();
		if (string.IsNullOrEmpty(device))
		{
			return list;
		}
		for (int i = 0; i < sourceNames.Count; i++)
		{
			if (string.Equals(sourceNames[i], device, StringComparison.OrdinalIgnoreCase))
			{
				list.Add(i);
			}
		}
		return list;
	}

	internal static bool AllColouredDisplaysInHdr(IReadOnlyList<string> coloured, IReadOnlyList<HdrDisplay> report)
	{
		if (coloured.Count == 0)
		{
			return false;
		}
		foreach (string device in coloured)
		{
			if (!report.Any((HdrDisplay d) => d.Enabled && string.Equals(d.Device, device, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}
		}
		return true;
	}

	public static bool AllColouredDisplaysInHdr()
	{
		try
		{
			return AllColouredDisplaysInHdr(MultiMonitorGammaRamp.Devices(), Report());
		}
		catch
		{
			return false;
		}
	}

	public static bool? IsHdrOn(string? gdiDeviceName)
	{
		try
		{
			string b = ResolveDevice(gdiDeviceName);
			foreach (HdrDisplay item in Report())
			{
				if (string.Equals(item.Device, b, StringComparison.OrdinalIgnoreCase))
				{
					return item.Enabled;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string? ResolveDevice(string? gdiDeviceName)
	{
		if (!string.IsNullOrEmpty(gdiDeviceName))
		{
			return gdiDeviceName;
		}
		return Screen.PrimaryScreen?.DeviceName;
	}

	internal static HdrSwitchOutcome SwitchWithRevert(bool enabled, string? device, Func<string?, bool?> isHdrOn, Func<bool, string?, bool> set, Func<bool> keep)
	{
		bool? flag = isHdrOn(device);
		if (flag.HasValue)
		{
			bool valueOrDefault = flag == true;
			if (valueOrDefault == enabled)
			{
				return HdrSwitchOutcome.Unchanged;
			}
			if (!set(enabled, device))
			{
				return HdrSwitchOutcome.Failed;
			}
			if (isHdrOn(device) != enabled)
			{
				set(valueOrDefault, device);
				return HdrSwitchOutcome.Failed;
			}
			if (keep())
			{
				return HdrSwitchOutcome.Kept;
			}
			if (!set(valueOrDefault, device))
			{
				return HdrSwitchOutcome.Failed;
			}
			return HdrSwitchOutcome.Reverted;
		}
		return HdrSwitchOutcome.Failed;
	}

	internal static HdrSwitchOutcome SwitchWithRevertReal(bool enabled, string? device, TimeSpan grace)
	{
		string name = ResolveDevice(device) ?? "";
		return SwitchWithRevert(enabled, name, IsHdrOn, TrySetHdr, () =>
		{
			bool confirmed = false;
			using RevertConfirmForm revertConfirmForm = new RevertConfirmForm($"HDR is now {(enabled ? "on" : "off")} for {name}.", Math.Max(1, (int)Math.Ceiling(grace.TotalSeconds)));
			revertConfirmForm.Confirmed += () =>
			{
				confirmed = true;
			};
			revertConfirmForm.ShowDialog();
			return confirmed;
		});
	}

	private static IReadOnlyList<DISPLAYCONFIG_PATH_INFO>? ActivePaths()
	{
		if (GetDisplayConfigBufferSizes(2u, out var pathCount, out var modeCount) != 0)
		{
			return null;
		}
		DISPLAYCONFIG_PATH_INFO[] array = new DISPLAYCONFIG_PATH_INFO[pathCount];
		DISPLAYCONFIG_MODE_INFO[] modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
		if (QueryDisplayConfig(2u, ref pathCount, array, ref modeCount, modes, IntPtr.Zero) != 0)
		{
			return null;
		}
		return new ArraySegment<DISPLAYCONFIG_PATH_INFO>(array, 0, (int)pathCount);
	}

	private static (uint Flags, int BitsPerColorChannel)? AdvancedColourOf(DISPLAYCONFIG_PATH_INFO path)
	{
		(uint, uint, uint)? tuple = ReadInfo2(path);
		if (tuple.HasValue)
		{
			(uint, uint, uint) valueOrDefault = tuple.GetValueOrDefault();
			return FromAdvancedColorInfo2(valueOrDefault.Item1, valueOrDefault.Item2, (int)valueOrDefault.Item3);
		}
		DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO info = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
		{
			header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
			{
				type = 9,
				size = Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>(),
				adapterId = path.targetInfo.adapterId,
				id = path.targetInfo.id
			}
		};
		if (DisplayConfigGetDeviceInfo(ref info) != 0)
		{
			return null;
		}
		return (info.value, info.bitsPerColorChannel);
	}

	private static (uint Value, uint Mode, uint Bits)? ReadInfo2(DISPLAYCONFIG_PATH_INFO path)
	{
		DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2 info = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2
		{
			header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
			{
				type = 15,
				size = Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2>(),
				adapterId = path.targetInfo.adapterId,
				id = path.targetInfo.id
			}
		};
		if (DisplayConfigGetDeviceInfo(ref info) != 0)
		{
			return null;
		}
		return (info.value, info.activeColorMode, info.bitsPerColorChannel);
	}

	private static string SourceNameOf(DISPLAYCONFIG_PATH_INFO path)
	{
		try
		{
			DISPLAYCONFIG_SOURCE_DEVICE_NAME info = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
			{
				header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
				{
					type = 1,
					size = Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
					adapterId = path.sourceInfo.adapterId,
					id = path.sourceInfo.id
				}
			};
			return (DisplayConfigGetDeviceInfo(ref info) == 0) ? (info.viewGdiDeviceName ?? "").TrimEnd('\0') : "";
		}
		catch
		{
			return "";
		}
	}

	public static bool TrySetHdr(bool enabled, string? gdiDeviceName)
	{
		IReadOnlyList<DISPLAYCONFIG_PATH_INFO> readOnlyList = ActivePaths();
		if (readOnlyList == null)
		{
			return false;
		}
		IReadOnlyList<int> readOnlyList2 = PickPaths(readOnlyList.Select(SourceNameOf).ToList(), ResolveDevice(gdiDeviceName));
		if (readOnlyList2.Count == 0)
		{
			return false;
		}
		bool result = false;
		foreach (int item in readOnlyList2)
		{
			DISPLAYCONFIG_PATH_INFO path = readOnlyList[item];
			(uint, uint, uint)? tuple = ReadInfo2(path);
			if (tuple.HasValue)
			{
				(uint, uint, uint) valueOrDefault = tuple.GetValueOrDefault();
				if ((valueOrDefault.Item1 & 0x10) == 0)
				{
					continue;
				}
				if (valueOrDefault.Item2 == 2 == enabled)
				{
					result = true;
					continue;
				}
				DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE info = new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
				{
					header = SetHeader(16, path),
					value = (enabled ? 1u : 0u)
				};
				if (DisplayConfigSetDeviceInfo(ref info) != 0)
				{
					continue;
				}
				for (int i = 0; i < 4; i++)
				{
					tuple = ReadInfo2(path);
					if (tuple.HasValue && tuple.GetValueOrDefault().Item2 == 2 == enabled)
					{
						result = true;
						break;
					}
					Thread.Sleep(150);
				}
				continue;
			}
			DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO info2 = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
			{
				header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
				{
					type = 9,
					size = Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>(),
					adapterId = path.targetInfo.adapterId,
					id = path.targetInfo.id
				}
			};
			if (DisplayConfigGetDeviceInfo(ref info2) != 0 || (info2.value & 1) == 0)
			{
				continue;
			}
			if (((info2.value & 2) != 0 && (info2.value & 4) == 0) == enabled)
			{
				result = true;
				continue;
			}
			DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE info3 = new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
			{
				header = SetHeader(10, path),
				value = (enabled ? 1u : 0u)
			};
			if (DisplayConfigSetDeviceInfo(ref info3) == 0)
			{
				result = true;
			}
		}
		return result;
	}

	private static DISPLAYCONFIG_DEVICE_INFO_HEADER SetHeader(int type, DISPLAYCONFIG_PATH_INFO path)
	{
		return new DISPLAYCONFIG_DEVICE_INFO_HEADER
		{
			type = type,
			size = Marshal.SizeOf<DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE>(),
			adapterId = path.targetInfo.adapterId,
			id = path.targetInfo.id
		};
	}

	[DllImport("user32.dll")]
	private static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE info);

	[DllImport("user32.dll")]
	private static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

	[DllImport("user32.dll")]
	private static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] DISPLAYCONFIG_PATH_INFO[] paths, ref uint modeCount, [Out] DISPLAYCONFIG_MODE_INFO[] modes, nint currentTopologyId);

	[DllImport("user32.dll")]
	private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO info);

	[DllImport("user32.dll")]
	private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME info);

	[DllImport("user32.dll")]
	private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2 info);

	internal static (int Target, int Path) MarshalledSizesForTest()
	{
		return (Target: Marshal.SizeOf<DISPLAYCONFIG_PATH_TARGET_INFO>(), Path: Marshal.SizeOf<DISPLAYCONFIG_PATH_INFO>());
	}

	internal static int SourceNameSizeForTest()
	{
		return Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
	}

	internal static int AdvancedColorInfo2SizeForTest()
	{
		return Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2>();
	}
}
