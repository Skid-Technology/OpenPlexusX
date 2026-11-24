namespace VibranceHud;

public static class CaptureStatus
{
	public const string AlwaysTrue = "In OBS use Display Capture, not Game Capture - Game Capture reads the game directly and never sees this. Gamma is never captured either way.";

	public static CaptureState Resolve(OverlayMode overlay, bool driverVibranceAvailable, bool streamingMode)
	{
		if (overlay == OverlayMode.Mag)
		{
			return CaptureState.DependsOnCaptureMethod;
		}
		if (!driverVibranceAvailable)
		{
			return CaptureState.Visible;
		}
		if (!streamingMode)
		{
			return CaptureState.NeedsStreamingMode;
		}
		return CaptureState.Visible;
	}

	public static bool ToggleCanHelp(OverlayMode overlay, bool driverVibranceAvailable)
	{
		return driverVibranceAvailable;
	}

	public static string Headline(CaptureState state)
	{
		return state switch
		{
			CaptureState.Visible => "Recordings show your colours.", 
			CaptureState.NeedsStreamingMode => "Recordings are missing your vibrance right now.", 
			_ => "Recordings show your colours - screen share may not.", 
		};
	}

	public static string Reason(CaptureState state, bool driverVibranceAvailable)
	{
		switch (state)
		{
		case CaptureState.NeedsStreamingMode:
			return "Your vibrance is being applied by the graphics driver, after recording software has already read the screen. Turn on \"Move my colours where recording can see them\" above to move it somewhere they can see it.";
		case CaptureState.DependsOnCaptureMethod:
			if (driverVibranceAvailable)
			{
				return "Recordings and screen share both show your colours - most of it comes from your NVIDIA driver, which every capture path can see.\n\nSaturation pushed past 100 is done in software, and that part reaches OBS but not Discord screen share.";
			}
			return "OBS records your colours with its default Display Capture settings - confirmed on AMD. If they don't appear, open your Display Capture source and set Capture Method to \"Windows 10 (1903 and up)\".\n\nDiscord screen share reads the screen a different way and won't show them. Your card has no driver-level colour for PlexusX to use yet, which is what would fix that.";
		default:
			return driverVibranceAvailable ? "Your vibrance is being applied early enough for capture to pick it up." : "Your colours are already applied in software, which capture reads - so there was never anything to move. That switch is for NVIDIA driver vibrance.";
		}
	}
}
