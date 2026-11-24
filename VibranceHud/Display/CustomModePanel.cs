using System;
using System.Collections.Generic;

namespace VibranceHud.Display;

public static class CustomModePanel
{
	public static PanelSize Native(IReadOnlyList<DisplayMode> reported)
	{
		int width = 0;
		int height = 0;
		long num = 0L;
		foreach (DisplayMode item in reported)
		{
			long num2 = (long)item.Width * (long)item.Height;
			if (num2 > num)
			{
				num = num2;
				width = item.Width;
				height = item.Height;
			}
		}
		return new PanelSize(width, height);
	}

	public static DisplayMode FitInside(int width, int height, PanelSize panel)
	{
		if (width <= 0 || height <= 0 || !panel.Known)
		{
			return new DisplayMode(panel.Width, panel.Height, 0);
		}
		double num = Math.Min((double)panel.Width / (double)width, (double)panel.Height / (double)height);
		if (num >= 1.0)
		{
			return new DisplayMode(Even(width), Even(height), 0);
		}
		int num2 = Even((int)Math.Floor((double)width * num));
		int num3 = Even((int)Math.Floor((double)height * num));
		if (num2 < 640 || num3 < 480)
		{
			return new DisplayMode(panel.Width, panel.Height, 0);
		}
		return new DisplayMode(num2, num3, 0);
	}

	internal static int Even(int value)
	{
		return value - Math.Abs(value % 2);
	}
}
