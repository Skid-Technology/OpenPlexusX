using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NvAPIWrapper;
using NvAPIWrapper.Display;
using NvAPIWrapper.Native.Display;
using VibranceHud.Capabilities;
using VibranceHud.Controls;

namespace VibranceHud.Display;

public static class ScalingModeController
{
	internal sealed record ScalingIo(Func<string?, ScalingMode?> Current, Func<string?, uint?> Raw, Func<ScalingMode, bool, string?, bool> Set, Func<string, TimeSpan, bool> Confirm);

	private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
	{
		public uint type;

		public int size;

		public LUID adapterId;

		public uint id;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
	{
		public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string viewGdiDeviceName;
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

	private const uint QDC_ONLY_ACTIVE_PATHS = 2u;

	private const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 32u;

	private const uint SDC_APPLY = 128u;

	private const uint SDC_SAVE_TO_DATABASE = 512u;

	private const uint SDC_ALLOW_CHANGES = 1024u;

	private const uint DISPLAYCONFIG_SCALING_IDENTITY = 1u;

	private const uint DISPLAYCONFIG_SCALING_CENTERED = 2u;

	private const uint DISPLAYCONFIG_SCALING_STRETCHED = 3u;

	private const uint DISPLAYCONFIG_SCALING_ASPECTRATIOCENTEREDMAX = 4u;

	private static bool? _nvidiaReady;

	private const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1u;

	internal static uint ToDisplayConfigScaling(ScalingMode mode)
	{
		return mode switch
		{
			ScalingMode.AspectRatio => 4u, 
			ScalingMode.NoScaling => 1u, 
			ScalingMode.FullScreen => 3u, 
			_ => 1u, 
		};
	}

	internal static ScalingMode? FromDisplayConfigScaling(uint value)
	{
		return value switch
		{
			4u => (ScalingMode?)ScalingMode.AspectRatio, 
			1u => ScalingMode.NoScaling, 
			2u => ScalingMode.NoScaling, 
			3u => ScalingMode.FullScreen, 
			_ => null, 
		};
	}

	internal static int ToNvidiaScaling(ScalingMode mode)
	{
		return mode switch
		{
			ScalingMode.FullScreen => 2, 
			ScalingMode.AspectRatio => 5, 
			ScalingMode.NoScaling => 3, 
			ScalingMode.Integer => 8, 
			_ => 1, 
		};
	}

	private static uint ExpectedRawAfterNvidia(ScalingMode mode)
	{
		return mode switch
		{
			ScalingMode.FullScreen => 3u, 
			ScalingMode.AspectRatio => 4u, 
			ScalingMode.NoScaling => 2u, 
			ScalingMode.Integer => 4u, 
			_ => 1u, 
		};
	}

	internal static (int Target, int Path) MarshalledSizesForTest()
	{
		return (Target: Marshal.SizeOf<DISPLAYCONFIG_PATH_TARGET_INFO>(), Path: Marshal.SizeOf<DISPLAYCONFIG_PATH_INFO>());
	}

	internal static uint? RawScaling(string? gdiDeviceName = null)
	{
		if (!QueryActivePaths(out DISPLAYCONFIG_PATH_INFO[] paths, out DISPLAYCONFIG_MODE_INFO[] _, out uint _, out uint _))
		{
			return null;
		}
		IReadOnlyList<int> readOnlyList = PickPaths(SourceNamesOf(paths), gdiDeviceName, PrimaryDeviceName);
		if (readOnlyList.Count == 0)
		{
			return null;
		}
		return paths[readOnlyList[0]].targetInfo.scaling;
	}

	public static ScalingMode? Current(string? gdiDeviceName = null)
	{
		uint? num = RawScaling(gdiDeviceName);
		if (num.HasValue)
		{
			uint valueOrDefault = num.GetValueOrDefault();
			return FromDisplayConfigScaling(valueOrDefault);
		}
		return null;
	}

	internal static ScalingMode? FromNvidiaScaling(int value)
	{
		return value switch
		{
			2 => (ScalingMode?)ScalingMode.FullScreen, 
			5 => ScalingMode.AspectRatio, 
			3 => ScalingMode.NoScaling, 
			8 => ScalingMode.Integer, 
			_ => null, 
		};
	}

	public static ScalingMode? Stored(string? gdiDeviceName = null)
	{
		if (GpuDetection.PrimaryVendor() == GpuVendor.Nvidia)
		{
			int? num = NvidiaCustomModeDriver.StoredScaling(gdiDeviceName);
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				return FromNvidiaScaling(valueOrDefault);
			}
		}
		uint? num2 = RawScaling(gdiDeviceName);
		if (num2.HasValue)
		{
			uint valueOrDefault2 = num2.GetValueOrDefault();
			if (valueOrDefault2 != 1)
			{
				return FromDisplayConfigScaling(valueOrDefault2);
			}
		}
		return null;
	}

	internal static IReadOnlyList<int> PickPaths(IReadOnlyList<string> sourceNames, string? gdiDeviceName, Func<string?> primaryName)
	{
		string text = (string.IsNullOrWhiteSpace(gdiDeviceName) ? primaryName() : gdiDeviceName);
		List<int> list = new List<int>();
		if (string.IsNullOrWhiteSpace(text))
		{
			return list;
		}
		for (int i = 0; i < sourceNames.Count; i++)
		{
			if (string.Equals(sourceNames[i], text, StringComparison.OrdinalIgnoreCase))
			{
				list.Add(i);
			}
		}
		return list;
	}

	private static string? PrimaryDeviceName()
	{
		try
		{
			foreach (MonitorDescriptor item in DisplayController.Monitors())
			{
				if (item.IsPrimary)
				{
					return item.DeviceName;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool QueryActivePaths(out DISPLAYCONFIG_PATH_INFO[] paths, out DISPLAYCONFIG_MODE_INFO[] modes, out uint pathCount, out uint modeCount)
	{
		paths = Array.Empty<DISPLAYCONFIG_PATH_INFO>();
		modes = Array.Empty<DISPLAYCONFIG_MODE_INFO>();
		if (GetDisplayConfigBufferSizes(2u, out pathCount, out modeCount) != 0 || pathCount == 0)
		{
			return false;
		}
		paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
		modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
		return QueryDisplayConfig(2u, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) == 0;
	}

	private static string[] SourceNamesOf(DISPLAYCONFIG_PATH_INFO[] paths)
	{
		string[] array = new string[paths.Length];
		for (int i = 0; i < paths.Length; i++)
		{
			try
			{
				DISPLAYCONFIG_SOURCE_DEVICE_NAME request = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
				{
					header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
					{
						type = 1u,
						size = Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
						adapterId = paths[i].sourceInfo.adapterId,
						id = paths[i].sourceInfo.id
					}
				};
				array[i] = ((DisplayConfigGetDeviceInfo(ref request) == 0) ? (request.viewGdiDeviceName ?? "").TrimEnd('\0') : "");
			}
			catch
			{
				array[i] = "";
			}
		}
		return array;
	}

	public static bool TrySet(ScalingMode mode, bool persist = false, string? gdiDeviceName = null)
	{
		if (GpuDetection.PrimaryVendor() == GpuVendor.Nvidia && TrySetNvidia(ToNvidiaScaling(mode), persist, gdiDeviceName))
		{
			return true;
		}
		if (mode == ScalingMode.Integer)
		{
			return false;
		}
		if (!QueryActivePaths(out DISPLAYCONFIG_PATH_INFO[] paths, out DISPLAYCONFIG_MODE_INFO[] modes, out uint pathCount, out uint modeCount))
		{
			return false;
		}
		IReadOnlyList<int> readOnlyList = PickPaths(SourceNamesOf(paths), gdiDeviceName, PrimaryDeviceName);
		if (readOnlyList.Count == 0)
		{
			return false;
		}
		uint scaling = ToDisplayConfigScaling(mode);
		foreach (int item in readOnlyList)
		{
			DISPLAYCONFIG_PATH_TARGET_INFO targetInfo = paths[item].targetInfo;
			targetInfo.scaling = scaling;
			paths[item].targetInfo = targetInfo;
		}
		uint num = 1184u;
		if (persist)
		{
			num |= 0x200;
		}
		return SetDisplayConfig(pathCount, paths, modeCount, modes, num) == 0;
	}

	internal static ScalingOutcome Classify(ScalingMode mode, uint? raw)
	{
		uint? num = raw;
		if (num.HasValue)
		{
			if (num == 1 && mode != ScalingMode.NoScaling)
			{
				return ScalingOutcome.SavedForLater;
			}
			if (mode == ScalingMode.Integer)
			{
				return ScalingOutcome.Unconfirmed;
			}
			if (raw == ExpectedRawAfterNvidia(mode))
			{
				return ScalingOutcome.Applied;
			}
			if (raw == ToDisplayConfigScaling(mode))
			{
				return ScalingOutcome.Applied;
			}
			return ScalingOutcome.Failed;
		}
		return ScalingOutcome.Unreadable;
	}

	private static bool Undo(ScalingMode? previous, string? gdiDeviceName, ScalingIo io)
	{
		if (previous.HasValue)
		{
			ScalingMode valueOrDefault = previous.GetValueOrDefault();
			return io.Set(valueOrDefault, arg2: false, gdiDeviceName);
		}
		return false;
	}

	private static ScalingIo RealIo()
	{
		return new ScalingIo((string? d) => Current(d), (string? d) => RawScaling(d), (ScalingMode m, bool persist, string? d) => TrySet(m, persist, d), (string message, TimeSpan grace) =>
		{
			bool confirmed = false;
			using RevertConfirmForm revertConfirmForm = new RevertConfirmForm(message, Math.Max(1, (int)Math.Ceiling(grace.TotalSeconds)));
			revertConfirmForm.Confirmed += () =>
			{
				confirmed = true;
			};
			revertConfirmForm.ShowDialog();
			return confirmed;
		});
	}

	public static ScalingOutcome TrySetWithRevert(ScalingMode mode, TimeSpan grace, string? gdiDeviceName = null)
	{
		return RunWithRevert(mode, grace, gdiDeviceName, RealIo());
	}

	internal static ScalingOutcome RunWithRevert(ScalingMode mode, TimeSpan grace, string? gdiDeviceName, ScalingIo io)
	{
		ScalingMode? previous = io.Current(gdiDeviceName);
		if (!io.Set(mode, arg2: false, gdiDeviceName))
		{
			return ScalingOutcome.Failed;
		}
		ScalingOutcome scalingOutcome = Classify(mode, io.Raw(gdiDeviceName));
		switch (scalingOutcome)
		{
		case ScalingOutcome.Failed:
			Undo(previous, gdiDeviceName, io);
			return ScalingOutcome.Failed;
		case ScalingOutcome.SavedForLater:
			io.Set(mode, arg2: true, gdiDeviceName);
			return scalingOutcome;
		default:
			if (io.Confirm("Scaling set to " + mode.Label() + ".", grace))
			{
				io.Set(mode, arg2: true, gdiDeviceName);
				return scalingOutcome;
			}
			if (!Undo(previous, gdiDeviceName, io))
			{
				return ScalingOutcome.RevertFailed;
			}
			return ScalingOutcome.Reverted;
		}
	}

	internal static bool NvidiaReadyForTest()
	{
		return NvidiaReady();
	}

	private static bool NvidiaReady()
	{
		bool? nvidiaReady = _nvidiaReady;
		if (nvidiaReady.HasValue)
		{
			return nvidiaReady == true;
		}
		try
		{
			Initialise();
			_nvidiaReady = true;
		}
		catch
		{
			_nvidiaReady = false;
		}
		return _nvidiaReady.Value;
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void Initialise()
		{
			NVIDIA.Initialize();
		}
	}

	internal static bool TrySetNvidia(int nvScaling, bool persist = false, string? gdiDeviceName = null)
	{
		if (!NvidiaReady())
		{
			return false;
		}
		try
		{
			return Write(nvScaling, persist, gdiDeviceName);
		}
		catch
		{
			return false;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static bool Write(int scaling, bool flag2, string? text)
		{
			PathInfo[] displaysConfig = PathInfo.GetDisplaysConfig();
			if (displaysConfig == null || displaysConfig.Length == 0)
			{
				return false;
			}
			uint num = (string.IsNullOrWhiteSpace(text) ? DisplayDevice.GetGDIPrimaryDisplayDevice().DisplayId : new DisplayDevice(text).DisplayId);
			bool flag = false;
			PathInfo[] array = displaysConfig;
			for (int i = 0; i < array.Length; i++)
			{
				PathTargetInfo[] targetsInfo = array[i].TargetsInfo;
				foreach (PathTargetInfo pathTargetInfo in targetsInfo)
				{
					if (pathTargetInfo.DisplayDevice.DisplayId == num)
					{
						pathTargetInfo.Scaling = (Scaling)scaling;
						flag = true;
					}
				}
			}
			if (!flag)
			{
				return false;
			}
			PathInfo.SetDisplaysConfig(displaysConfig, flag2 ? DisplayConfigFlags.SaveToPersistence : DisplayConfigFlags.None);
			return true;
		}
	}

	[DllImport("user32.dll")]
	private static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

	[DllImport("user32.dll")]
	private static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] DISPLAYCONFIG_PATH_INFO[] paths, ref uint modeCount, [Out] DISPLAYCONFIG_MODE_INFO[] modes, nint currentTopologyId);

	[DllImport("user32.dll")]
	private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME request);

	[DllImport("user32.dll")]
	private static extern int SetDisplayConfig(uint numPathArrayElements, [In] DISPLAYCONFIG_PATH_INFO[] pathArray, uint numModeInfoArrayElements, [In] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, uint flags);
}
