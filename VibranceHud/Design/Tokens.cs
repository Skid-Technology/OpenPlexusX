using System;

namespace VibranceHud.Design;

public static class Tokens
{
	public const int BaseDpi = 96;

	public const int XXS = 2;

	public const int XS = 4;

	public const int S = 8;

	public const int M = 12;

	public const int L = 16;

	public const int L2 = 20;

	public const int XL = 24;

	public const int XXL = 32;

	public const int XXXXL = 40;

	public const int XXXL = 48;

	public static int Dpi { get; set; } = 96;

	public static int Scale(int logical)
	{
		return ScaleAt(Dpi, logical);
	}

	public static int ScaleAt(int dpi, int logical)
	{
		if (logical == 0)
		{
			return 0;
		}
		if (dpi <= 0)
		{
			dpi = 96;
		}
		int val = (int)Math.Round((double)logical * ((double)dpi / 96.0), MidpointRounding.AwayFromZero);
		if (logical <= 0)
		{
			return Math.Min(-1, val);
		}
		return Math.Max(1, val);
	}

	public static float ScaleF(float logical)
	{
		return logical * ((float)Dpi / 96f);
	}
}
