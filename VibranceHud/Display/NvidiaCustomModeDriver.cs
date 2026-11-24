using System;
using System.Collections.Generic;
using NvAPIWrapper.Display;
using NvAPIWrapper.Native;
using NvAPIWrapper.Native.Display;
using NvAPIWrapper.Native.Display.Structures;
using NvAPIWrapper.Native.Exceptions;
using NvAPIWrapper.Native.General;
using VibranceHud.Capabilities;

namespace VibranceHud.Display;

public sealed class NvidiaCustomModeDriver : ICustomModeDriver
{
	private const uint ColourDepth = 32u;

	internal static readonly TimingOverride[] NativeTimingFormulas = new TimingOverride[3]
	{
		TimingOverride.EDID,
		TimingOverride.Current,
		TimingOverride.CVTReducedBlanking
	};

	private readonly Func<string?, IReadOnlyList<DisplayMode>> _supportedModes;

	public bool Available
	{
		get
		{
			if (!Nvapi.Ready())
			{
				return false;
			}
			try
			{
				return GpuDetection.PrimaryVendor() == GpuVendor.Nvidia && Device(null) != null;
			}
			catch
			{
				return false;
			}
		}
	}

	internal static (uint W, uint H, uint Hz)? OutputFor(int width, int height, int hz, DisplayMode native)
	{
		if (native.Width <= 0 || native.Height <= 0)
		{
			return null;
		}
		if (width <= 0 || height <= 0)
		{
			return null;
		}
		if (width > native.Width || height > native.Height)
		{
			return null;
		}
		int num = ((hz > 0) ? hz : native.RefreshHz);
		if (num <= 0)
		{
			return null;
		}
		return ((uint)native.Width, (uint)native.Height, (uint)num);
	}

	internal static bool IsNativeSignal(int visibleWidth, int visibleHeight, DisplayMode native)
	{
		if (native.Width > 0 && native.Height > 0 && visibleWidth == native.Width)
		{
			return visibleHeight == native.Height;
		}
		return false;
	}

	internal static bool TimingFitsThisMonitor(int savedPixelClock10K, int savedHTotal, int savedVTotal, int ownPixelClock10K, int ownHTotal, int ownVTotal)
	{
		if (savedPixelClock10K <= 0 || savedHTotal <= 0 || savedVTotal <= 0)
		{
			return false;
		}
		if (ownPixelClock10K <= 0 || ownHTotal <= 0 || ownVTotal <= 0)
		{
			return false;
		}
		if (Close(savedPixelClock10K, ownPixelClock10K))
		{
			return Close((double)savedPixelClock10K / (double)savedHTotal, (double)ownPixelClock10K / (double)ownHTotal);
		}
		return false;
		static bool Close(double a, double b)
		{
			return Math.Abs(a - b) <= 0.05 * b;
		}
	}

	internal static DisplayMode? NativeModeOf(IReadOnlyList<DisplayMode> reported)
	{
		PanelSize panelSize = CustomModePanel.Native(reported);
		if (!panelSize.Known)
		{
			return null;
		}
		int num = 0;
		foreach (DisplayMode item in reported)
		{
			if (item.Width == panelSize.Width && item.Height == panelSize.Height && item.RefreshHz > num)
			{
				num = item.RefreshHz;
			}
		}
		return new DisplayMode(panelSize.Width, panelSize.Height, num);
	}

	public NvidiaCustomModeDriver(Func<string?, IReadOnlyList<DisplayMode>>? supportedModes = null)
	{
		_supportedModes = supportedModes ?? ((Func<string, IReadOnlyList<DisplayMode>>)((string? d) => DisplayController.SupportedModes(d)));
	}

	private DisplayMode? NativeFor(string? deviceName, DisplayMode? given)
	{
		if (given.HasValue)
		{
			return given.GetValueOrDefault();
		}
		try
		{
			return NativeModeOf(_supportedModes(deviceName));
		}
		catch
		{
			return null;
		}
	}

	internal static bool MeansNoPathAtAll(Status status)
	{
		return status switch
		{
			Status.NvidiaDeviceNotFound => true, 
			Status.NVIDIADisplayNotFound => true, 
			Status.ApiNotInitialized => true, 
			Status.NoImplementation => true, 
			Status.LibraryNotFound => true, 
			_ => false, 
		};
	}

	bool ICustomModeDriver.Trial(string? deviceName, int width, int height, int hz)
	{
		return Trial(deviceName, width, height, hz, null);
	}

	public bool Trial(string? deviceName, int width, int height, int hz, DisplayMode? native)
	{
		if (!Nvapi.Ready())
		{
			return false;
		}
		DisplayDevice displayDevice = Device(deviceName);
		if ((object)displayDevice == null)
		{
			return false;
		}
		DisplayMode? displayMode = NativeFor(deviceName, native);
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			(uint, uint, uint)? tuple = OutputFor(width, height, hz, valueOrDefault);
			if (tuple.HasValue)
			{
				(uint, uint, uint) valueOrDefault2 = tuple.GetValueOrDefault();
				NvAPIWrapper.Native.Display.Structures.Timing? timing = TimingFor(displayDevice.DisplayId, (int)valueOrDefault2.Item1, (int)valueOrDefault2.Item2, (int)valueOrDefault2.Item3);
				if (timing.HasValue)
				{
					NvAPIWrapper.Native.Display.Structures.Timing valueOrDefault3 = timing.GetValueOrDefault();
					CustomResolution customResolution = new CustomResolution((uint)width, (uint)height, 32u, valueOrDefault3);
					try
					{
						displayDevice.TrialCustomResolution(customResolution, hardwareModeSetOnly: false);
						return true;
					}
					catch (NVIDIAApiException ex) when (!MeansNoPathAtAll(ex.Status))
					{
						NvapiFallbackLog.NoteCustomMode($"trial {width}x{height}@{hz}", ex.Status.ToString());
						return false;
					}
				}
				return false;
			}
			return false;
		}
		return false;
	}

	public bool Save(string? deviceName)
	{
		if (!Nvapi.Ready())
		{
			return false;
		}
		DisplayDevice displayDevice = Device(deviceName);
		if ((object)displayDevice == null)
		{
			return false;
		}
		try
		{
			displayDevice.SaveCustomResolution(isThisOutputIdOnly: false);
			NvapiFallbackLog.NoteCustomModeStep("save", "ok");
			return true;
		}
		catch (NVIDIAApiException ex) when (!MeansNoPathAtAll(ex.Status))
		{
			NvapiFallbackLog.NoteCustomMode("save", ex.Status.ToString());
			return false;
		}
	}

	public bool Revert(string? deviceName)
	{
		if (!Nvapi.Ready())
		{
			return false;
		}
		DisplayDevice displayDevice = Device(deviceName);
		if ((object)displayDevice == null)
		{
			return false;
		}
		try
		{
			displayDevice.RevertCustomResolution();
			return true;
		}
		catch (NVIDIAApiException ex) when (!MeansNoPathAtAll(ex.Status))
		{
			return false;
		}
	}

	public IReadOnlyList<DisplayMode> Saved(string? deviceName)
	{
		if (!Nvapi.Ready())
		{
			return Array.Empty<DisplayMode>();
		}
		List<DisplayMode> list = new List<DisplayMode>();
		try
		{
			DisplayDevice displayDevice = Device(deviceName);
			if ((object)displayDevice == null)
			{
				return list;
			}
			foreach (CustomResolution customResolution in displayDevice.GetCustomResolutions())
			{
				int refreshRate = customResolution.Timing.Extra.RefreshRate;
				DisplayMode item = new DisplayMode((int)customResolution.Width, (int)customResolution.Height, refreshRate);
				if (item.Width > 0 && item.Height > 0 && !list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	public bool IsOnTheNativeSignal(string? deviceName, int width, int height, int hz, DisplayMode? native)
	{
		if (!Nvapi.Ready())
		{
			return false;
		}
		try
		{
			DisplayDevice displayDevice = Device(deviceName);
			if ((object)displayDevice == null)
			{
				return false;
			}
			DisplayMode? displayMode = NativeFor(deviceName, native);
			if (!displayMode.HasValue)
			{
				return false;
			}
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			foreach (CustomResolution customResolution in displayDevice.GetCustomResolutions())
			{
				if (customResolution.Width != width || customResolution.Height != height)
				{
					continue;
				}
				int refreshRate = customResolution.Timing.Extra.RefreshRate;
				if (hz <= 0 || refreshRate <= 0 || refreshRate == hz)
				{
					if (!IsNativeSignal(customResolution.Timing.HorizontalVisible, customResolution.Timing.VerticalVisible, valueOrDefault))
					{
						return false;
					}
					return SignalFitsThisMonitor(displayDevice.DisplayId, valueOrDefault, (refreshRate > 0) ? refreshRate : hz, customResolution.Timing.PixelClockIn10KHertz, customResolution.Timing.HorizontalTotal, customResolution.Timing.VerticalTotal);
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool SignalFitsThisMonitor(uint displayId, DisplayMode panel, int hz, int savedPixelClock10K, int savedHTotal, int savedVTotal)
	{
		if (hz <= 0)
		{
			return true;
		}
		NvAPIWrapper.Native.Display.Structures.Timing? timing = TimingFor(displayId, panel.Width, panel.Height, hz);
		if (timing.HasValue)
		{
			NvAPIWrapper.Native.Display.Structures.Timing valueOrDefault = timing.GetValueOrDefault();
			return TimingFitsThisMonitor(savedPixelClock10K, savedHTotal, savedVTotal, valueOrDefault.PixelClockIn10KHertz, valueOrDefault.HorizontalTotal, valueOrDefault.VerticalTotal);
		}
		return true;
	}

	internal bool CanComputeTiming(string? deviceName, int width, int height, int hz)
	{
		if (!Nvapi.Ready())
		{
			return false;
		}
		DisplayDevice displayDevice = Device(deviceName);
		if ((object)displayDevice == null)
		{
			return false;
		}
		DisplayMode? displayMode = NativeFor(deviceName, null);
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			(uint, uint, uint)? tuple = OutputFor(width, height, hz, valueOrDefault);
			if (tuple.HasValue)
			{
				(uint, uint, uint) valueOrDefault2 = tuple.GetValueOrDefault();
				return TimingFor(displayDevice.DisplayId, (int)valueOrDefault2.Item1, (int)valueOrDefault2.Item2, (int)valueOrDefault2.Item3).HasValue;
			}
			return false;
		}
		return false;
	}

	private static NvAPIWrapper.Native.Display.Structures.Timing? TimingFor(uint displayId, int width, int height, int hz)
	{
		if (!Nvapi.Ready())
		{
			return null;
		}
		TimingOverride[] nativeTimingFormulas = NativeTimingFormulas;
		foreach (TimingOverride timingType in nativeTimingFormulas)
		{
			try
			{
				NvAPIWrapper.Native.Display.Structures.Timing timing = DisplayApi.GetTiming(displayId, new TimingInput((uint)width, (uint)height, hz, timingType, isInterlaced: false, 0));
				if (timing.HorizontalVisible == width && timing.VerticalVisible == height)
				{
					return timing;
				}
			}
			catch (NVIDIAApiException ex) when (!MeansNoPathAtAll(ex.Status))
			{
			}
		}
		return null;
	}

	public static int? StoredScaling(string? deviceName)
	{
		if (!Nvapi.Ready())
		{
			return null;
		}
		try
		{
			DisplayDevice displayDevice = Device(deviceName);
			if ((object)displayDevice == null)
			{
				return null;
			}
			PathInfo[] displaysConfig = PathInfo.GetDisplaysConfig();
			for (int i = 0; i < displaysConfig.Length; i++)
			{
				PathTargetInfo[] targetsInfo = displaysConfig[i].TargetsInfo;
				foreach (PathTargetInfo pathTargetInfo in targetsInfo)
				{
					if (pathTargetInfo.DisplayDevice.DisplayId == displayDevice.DisplayId)
					{
						return (int)pathTargetInfo.Scaling;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static DisplayDevice? Device(string? deviceName)
	{
		if (!Nvapi.Ready())
		{
			return null;
		}
		if (string.IsNullOrWhiteSpace(deviceName))
		{
			return DisplayDevice.GetGDIPrimaryDisplayDevice();
		}
		return new DisplayDevice(deviceName);
	}
}
