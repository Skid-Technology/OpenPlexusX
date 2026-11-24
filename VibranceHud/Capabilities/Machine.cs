namespace VibranceHud.Capabilities;

public static class Machine
{
	public static MachineCapabilities Current { get; private set; } = MachineCapabilities.Unknown;

	public static void Measure(bool driverVibrance, OverlayMode overlayPath)
	{
		Current = CapabilityProbe.Run(driverVibrance, overlayPath);
	}

	public static bool MeasureOrReuse(bool driverVibrance, OverlayMode overlayPath, AppSettings settings)
	{
		Current = CapabilityProbe.Run(driverVibrance, overlayPath, settings.GammaProbeMemory, out GammaProbeMemory measured);
		if (measured == null || object.Equals(measured, settings.GammaProbeMemory))
		{
			return false;
		}
		settings.GammaProbeMemory = measured;
		return true;
	}

	public static bool MeasureAndRemember(bool driverVibrance, OverlayMode overlayPath, AppSettings settings)
	{
		Current = CapabilityProbe.Run(driverVibrance, overlayPath, null, out GammaProbeMemory measured);
		if (measured == null || object.Equals(measured, settings.GammaProbeMemory))
		{
			return false;
		}
		settings.GammaProbeMemory = measured;
		return true;
	}

	internal static void OverrideForTests(MachineCapabilities caps)
	{
		Current = caps;
	}
}
