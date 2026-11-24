using System;
using System.Collections.Generic;
using System.Drawing;

namespace VibranceHud.Design;

public static class Readability
{
	public const double AA = 4.5;

	public const double BrightestFraction = 0.05;

	public static double RelativeLuminance(Color c)
	{
		return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
	}

	private static double Channel(int v)
	{
		double num = (double)v / 255.0;
		if (!(num <= 0.04045))
		{
			return Math.Pow((num + 0.055) / 1.055, 2.4);
		}
		return num / 12.92;
	}

	public static double Contrast(Color a, Color b)
	{
		double val = RelativeLuminance(a);
		double val2 = RelativeLuminance(b);
		double num = Math.Max(val, val2);
		double num2 = Math.Min(val, val2);
		return (num + 0.05) / (num2 + 0.05);
	}

	public static Color Over(Color overlay, int alpha, Color ground)
	{
		int num = Math.Clamp(alpha, 0, 255);
		return Color.FromArgb(255, ground.R + num * (overlay.R - ground.R) / 255, ground.G + num * (overlay.G - ground.G) / 255, ground.B + num * (overlay.B - ground.B) / 255);
	}

	public static Color BrightestFivePercent(IReadOnlyList<Color> pixels)
	{
		if (pixels == null || pixels.Count == 0)
		{
			return Color.Black;
		}
		List<(double, Color)> list = new List<(double, Color)>(pixels.Count);
		foreach (Color pixel in pixels)
		{
			list.Add((RelativeLuminance(pixel), pixel));
		}
		list.Sort(((double L, Color C) x, (double L, Color C) y) => x.L.CompareTo(y.L));
		int value = (int)Math.Floor(0.95 * (double)(list.Count - 1));
		return list[Math.Clamp(value, 0, list.Count - 1)].Item2;
	}
}
