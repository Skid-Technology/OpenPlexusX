using System;
using System.Collections.Generic;

namespace VibranceHud.Display;

public static class CustomModeOffers
{
	private static readonly DisplayMode[] FourThreeLadder = new DisplayMode[4]
	{
		new DisplayMode(1600, 1200, 0),
		new DisplayMode(1280, 960, 0),
		new DisplayMode(1024, 768, 0),
		new DisplayMode(800, 600, 0)
	};

	private const int LadderRungs = 2;

	public static IReadOnlyList<DisplayMode> For(PanelSize panel)
	{
		if (!panel.Known)
		{
			return Array.Empty<DisplayMode>();
		}
		List<DisplayMode> list = new List<DisplayMode>();
		Offer(list, panel, panel.Height, panel.Height);
		Offer(list, panel, CustomModePanel.Even(panel.Height * 4 / 3), panel.Height);
		int num = 0;
		DisplayMode[] fourThreeLadder = FourThreeLadder;
		for (int i = 0; i < fourThreeLadder.Length; i++)
		{
			DisplayMode displayMode = fourThreeLadder[i];
			if (num >= 2)
			{
				break;
			}
			if (displayMode.Height < panel.Height && Offer(list, panel, displayMode.Width, displayMode.Height))
			{
				num++;
			}
		}
		return list;
	}

	private static bool Offer(List<DisplayMode> into, PanelSize panel, int w, int h)
	{
		if (w > panel.Width || h > panel.Height)
		{
			return false;
		}
		if (w < 640 || h < 480)
		{
			return false;
		}
		if (w == panel.Width && h == panel.Height)
		{
			return false;
		}
		DisplayMode item = new DisplayMode(w, h, 0);
		if (into.Contains(item))
		{
			return false;
		}
		into.Add(item);
		return true;
	}
}
