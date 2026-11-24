namespace VibranceHud;

public static class VibranceStatus
{
	public static VibranceDriverState Determine(bool nvidiaCardPresent, int nvidiaDisplayCount)
	{
		if (!nvidiaCardPresent)
		{
			return VibranceDriverState.NoNvidiaCard;
		}
		if (nvidiaDisplayCount <= 0)
		{
			return VibranceDriverState.DisplayNotOnNvidia;
		}
		return VibranceDriverState.Available;
	}

	public static string Readout(VibranceDriverState state, int vibrancePercent)
	{
		return $"{vibrancePercent}%";
	}

	public static string Qualifier(VibranceDriverState state)
	{
		return state switch
		{
			VibranceDriverState.Available => "", 
			VibranceDriverState.DisplayNotOnNvidia => "Applied by PlexusX rather than the NVIDIA driver - this display is not on the NVIDIA GPU. The slider works exactly the same.", 
			VibranceDriverState.NoNvidiaCard => "Applied by PlexusX rather than a driver - no NVIDIA graphics card here. The slider works exactly the same.", 
			_ => "Applied by PlexusX rather than a driver.", 
		};
	}
}
