using System;
using System.Drawing;

namespace VibranceHud;

public sealed record SliderPalette(Color Low, Color Mid, Color High)
{
	public static SliderPalette Plain => new SliderPalette(Theme.Accent, Theme.Accent, Theme.Accent);

	public static SliderPalette Temperature => Plain;

	public static SliderPalette Luminance => Plain;

	public static SliderPalette Contrast => Plain;

	public Color At(float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		if (!(t <= 0.5f))
		{
			return Blend(Mid, High, (t - 0.5f) * 2f);
		}
		return Blend(Low, Mid, t * 2f);
	}

	private static Color Blend(Color a, Color b, float k)
	{
		return Color.FromArgb((int)((float)(int)a.R + (float)(b.R - a.R) * k), (int)((float)(int)a.G + (float)(b.G - a.G) * k), (int)((float)(int)a.B + (float)(b.B - a.B) * k));
	}

	public static SliderPalette Accent()
	{
		return Plain;
	}
}
