namespace VibranceHud;

public static class LookRequirement
{
	public static bool NeedsColourMatrix(ColourVisionMode mode)
	{
		return mode != ColourVisionMode.Off;
	}

	public static LookOutcome Decide(ColourVisionMode mode, bool overlayLive, bool paidLookAllowed)
	{
		if (NeedsColourMatrix(mode))
		{
			if (!LookCorrection.IsALook(mode) || paidLookAllowed)
			{
				if (!overlayLive)
				{
					return LookOutcome.Refused;
				}
				return LookOutcome.Ready;
			}
			return LookOutcome.Refused;
		}
		return LookOutcome.NotNeeded;
	}

	public static string WhyRefused(ColourVisionMode mode, ColourReachFacts facts, bool paidLookAllowed)
	{
		if (!LookCorrection.IsALook(mode) || paidLookAllowed)
		{
			return ColourReachStatus.For(facts).Text;
		}
		return "This look needs the paid colour controls, and they are off on this PC right now.";
	}
}
