using System.Collections.Generic;
using System.Linq;

namespace VibranceHud;

public static class LookCorrection
{
	public static void ChosenByThem(AppSettings settings, ColourVisionMode mode)
	{
		settings.ColourVisionFromPreset = false;
		settings.UserColourVision = ColourVisionMode.Off;
		settings.ColourVision = mode;
	}

	public static void BroughtByALook(AppSettings settings, ColourVisionMode mode)
	{
		if (!settings.ColourVisionFromPreset)
		{
			settings.UserColourVision = ((!IsALook(settings.ColourVision)) ? settings.ColourVision : ColourVisionMode.Off);
			settings.ColourVisionFromPreset = true;
		}
		settings.ColourVision = mode;
	}

	public static ColourVisionMode SetAside(AppSettings settings)
	{
		if (!settings.ColourVisionFromPreset || IsALook(settings.UserColourVision))
		{
			return ColourVisionMode.Off;
		}
		return settings.UserColourVision;
	}

	public static void NormaliseOnLoad(AppSettings settings)
	{
		if (IsALook(settings.UserColourVision))
		{
			settings.UserColourVision = ColourVisionMode.Off;
		}
		if (IsALook(settings.ColourVision) && !settings.ColourVisionFromPreset)
		{
			settings.ColourVisionFromPreset = true;
			settings.UserColourVision = ColourVisionMode.Off;
		}
	}

	public static bool IsALook(ColourVisionMode mode)
	{
		return mode == ColourVisionMode.HoloBlack;
	}

	public static ColourVisionMode? GiveTheirsBack(AppSettings settings)
	{
		if (!settings.ColourVisionFromPreset)
		{
			return null;
		}
		ColourVisionMode userColourVision = settings.UserColourVision;
		ChosenByThem(settings, userColourVision);
		return userColourVision;
	}

	public static IReadOnlyList<DisplayPreset> OfferableWithoutANote(IEnumerable<DisplayPreset> presets)
	{
		return presets.Where((DisplayPreset p) => !LookRequirement.NeedsColourMatrix(p.ColourVision)).ToList();
	}
}
