namespace VibranceHud.Licensing;

public static class ServedData
{
	public static void Refresh()
	{
		Install();
	}

	internal static void Install()
	{
		ServedLooks.Install();
		ServedCodes.Install();
		ServedPairing.Install();
		ServedCrosshairs.Install();
		ServedNightVision.Install();
	}

	internal static void ResetForTest()
	{
		Install();
	}
}
