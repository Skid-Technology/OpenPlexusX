using System;
using System.Drawing;

namespace VibranceHud.Theming;

public static class ImagePalette
{
	public const float MinValue = 0.25f;

	public const float MinSaturation = 0.2f;

	public const float MinAccentBrightness = 0.45f;

	public const int MinDim = 0;

	public const int MaxDim = 80;

	private const int HueBuckets = 24;

	public static ImageTheme Extract(Color[] pixels, Color fallbackAccent)
	{
		int[] array = new int[24];
		float[] array2 = new float[24];
		float[] array3 = new float[24];
		float num = 0f;
		for (int i = 0; i < pixels.Length; i++)
		{
			Color c = pixels[i];
			ToHsv(c, out var h, out var s, out var v);
			num += (0.2126f * (float)(int)c.R + 0.7152f * (float)(int)c.G + 0.0722f * (float)(int)c.B) / 255f;
			if (!(v < 0.25f) && !(s < 0.2f))
			{
				int num2 = Math.Min(23, (int)(h / 360f * 24f));
				array[num2]++;
				array2[num2] += s;
				array3[num2] += v;
			}
		}
		int num3 = -1;
		for (int j = 0; j < 24; j++)
		{
			if (array[j] > 0 && (num3 < 0 || array[j] > array[num3]))
			{
				num3 = j;
			}
		}
		int dim = SuggestedDim((pixels.Length == 0) ? 0f : (num / (float)pixels.Length));
		if (pixels.Length == 0)
		{
			return Derive(fallbackAccent, dim);
		}
		Color accent = ((num3 >= 0) ? Lift(FromHsv(((float)num3 + 0.5f) * 15f, array2[num3] / (float)array[num3], array3[num3] / (float)array[num3])) : FromHsv(0f, 0f, 0.86f));
		return Derive(accent, dim);
	}

	public static ImageTheme Derive(Color accent, int dim)
	{
		Color color = ShiftHue(accent, 40f);
		ToHsv(accent, out var h, out var s, out var _);
		float num = Math.Min(s, 0.65f);
		return new ImageTheme(accent, Scale(accent, 0.6f), accent, color, Blend(accent, color, 0.5f), FromHsv(h, num * 0.34f, 0.055f), FromHsv(h, num * 0.3f, 0.13f), FromHsv(h, num * 0.3f, 0.19f), FromHsv(h, num * 0.28f, 0.265f), FromHsv(h, num * 0.45f, 0.05f), FromHsv(h, num * 0.16f, 0.6f), dim);
	}

	public static int SuggestedDim(float meanLuma)
	{
		return Math.Clamp((int)Math.Round(meanLuma * 85f), 0, 80);
	}

	private static Color Lift(Color c)
	{
		ToHsv(c, out var h, out var s, out var v);
		for (int i = 0; i < 24; i++)
		{
			if (!(Luma(FromHsv(h, s, v)) < 0.45f))
			{
				break;
			}
			if (!(v < 1f))
			{
				break;
			}
			v = Math.Min(1f, v + 0.05f);
		}
		return FromHsv(h, s, v);
	}

	private static float Luma(Color c)
	{
		return (0.2126f * (float)(int)c.R + 0.7152f * (float)(int)c.G + 0.0722f * (float)(int)c.B) / 255f;
	}

	private static Color Scale(Color c, float factor)
	{
		return Color.FromArgb((int)Math.Clamp((float)(int)c.R * factor, 0f, 255f), (int)Math.Clamp((float)(int)c.G * factor, 0f, 255f), (int)Math.Clamp((float)(int)c.B * factor, 0f, 255f));
	}

	private static Color Blend(Color a, Color b, float t)
	{
		return Color.FromArgb((int)((float)(int)a.R + (float)(b.R - a.R) * t), (int)((float)(int)a.G + (float)(b.G - a.G) * t), (int)((float)(int)a.B + (float)(b.B - a.B) * t));
	}

	private static Color ShiftHue(Color c, float degrees)
	{
		ToHsv(c, out var h, out var s, out var v);
		return FromHsv((h + degrees) % 360f, s, v);
	}

	private static void ToHsv(Color c, out float h, out float s, out float v)
	{
		float num = (float)(int)c.R / 255f;
		float num2 = (float)(int)c.G / 255f;
		float num3 = (float)(int)c.B / 255f;
		float num4 = Math.Max(num, Math.Max(num2, num3));
		float num5 = Math.Min(num, Math.Min(num2, num3));
		float num6 = num4 - num5;
		v = num4;
		s = ((num4 <= 0f) ? 0f : (num6 / num4));
		if (num6 <= 0f)
		{
			h = 0f;
		}
		else if (num4 == num)
		{
			h = 60f * (((num2 - num3) / num6 + 6f) % 6f);
		}
		else if (num4 == num2)
		{
			h = 60f * ((num3 - num) / num6 + 2f);
		}
		else
		{
			h = 60f * ((num - num2) / num6 + 4f);
		}
	}

	private static Color FromHsv(float h, float s, float v)
	{
		float num = v * s;
		float num2 = num * (1f - Math.Abs(h / 60f % 2f - 1f));
		float num3 = v - num;
		float num4;
		float num5;
		float num6;
		if (h < 60f)
		{
			num4 = num;
			num5 = num2;
			num6 = 0f;
		}
		else if (h < 120f)
		{
			num4 = num2;
			num5 = num;
			num6 = 0f;
		}
		else if (h < 180f)
		{
			num4 = 0f;
			num5 = num;
			num6 = num2;
		}
		else if (h < 240f)
		{
			num4 = 0f;
			num5 = num2;
			num6 = num;
		}
		else if (h < 300f)
		{
			num4 = num2;
			num5 = 0f;
			num6 = num;
		}
		else
		{
			num4 = num;
			num5 = 0f;
			num6 = num2;
		}
		return Color.FromArgb((int)Math.Round((num4 + num3) * 255f), (int)Math.Round((num5 + num3) * 255f), (int)Math.Round((num6 + num3) * 255f));
	}
}
