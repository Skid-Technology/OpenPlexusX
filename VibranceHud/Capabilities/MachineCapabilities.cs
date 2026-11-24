using System;
using System.Collections.Generic;

namespace VibranceHud.Capabilities;

public sealed record MachineCapabilities(GammaSupport GammaRamp = GammaSupport.Untested, bool HdrActive = false, GpuVendor Gpu = GpuVendor.Unknown, bool DriverVibrance = false, int MonitorCount = 1, bool MixedDpi = false, bool Elevated = false, OverlayMode OverlayPath = OverlayMode.Mag, GammaRefusal RefusedWhy = GammaRefusal.None, IReadOnlyList<DeviceGamma>? PerDeviceGamma = null)
{
	public static MachineCapabilities Unknown => new MachineCapabilities();

	public IReadOnlyList<DeviceGamma> Devices => PerDeviceGamma ?? Array.Empty<DeviceGamma>();

	public bool ToneControlsWork
	{
		get
		{
			GammaSupport gammaRamp = GammaRamp;
			if ((uint)(gammaRamp - 1) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public string ToneLimitation
	{
		get
		{
			switch (GammaRamp)
			{
			case GammaSupport.Refused:
			{
				if (HdrActive)
				{
					return "HDR is on, and Windows ignores screen-colour changes while it is. Turn HDR off to use these.";
				}
				if (RefusedWhy == GammaRefusal.ScreenUnreadable)
				{
					return "PlexusX couldn't find your screen when it started - maybe the monitor was off. Restart PlexusX with the screen on to use these. Saturation above works either way.";
				}
				if (!Elevated)
				{
					return "Windows is blocking screen-colour changes because PlexusX is not running as administrator. Settings has a Fix this button that restarts it properly. Saturation above works either way.";
				}
				if (Gpu == GpuVendor.Nvidia)
				{
					return "Your NVIDIA driver is refusing screen-colour changes. Something else is usually holding them - Windows Night light, f.lux, or NVIDIA Freestyle. Close those and reopen PlexusX. Saturation above works either way.";
				}
				GpuVendor gpu = Gpu;
				if ((uint)(gpu - 2) <= 1u)
				{
					return "Your graphics driver is refusing screen-colour changes. Check for a colour or night-light setting in your GPU control panel, and close anything else that tints the screen. Saturation above works either way.";
				}
				return "Your graphics driver is refusing screen-colour changes, so these won't do anything. Saturation and vibrance above still work.";
			}
			case GammaSupport.Clamped:
				if (HdrActive)
				{
					return "HDR is on, so Windows is limiting these. They'll work, but weaker than the numbers suggest.";
				}
				return "Windows is limiting how far these can go on this PC, so they'll be weaker than the numbers suggest.";
			case GammaSupport.Untested:
				return "";
			default:
				return "";
			}
		}
	}
}
