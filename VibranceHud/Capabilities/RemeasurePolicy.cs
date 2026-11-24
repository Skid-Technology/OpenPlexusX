namespace VibranceHud.Capabilities;

internal static class RemeasurePolicy
{
	internal const int MaxTries = 3;

	internal static bool ShouldRemeasure(MachineCapabilities current, int triesSoFar)
	{
		if (current.GammaRamp != GammaSupport.Refused)
		{
			return false;
		}
		if (current.RefusedWhy != GammaRefusal.ScreenUnreadable)
		{
			return triesSoFar < 1;
		}
		return triesSoFar < 3;
	}
}
