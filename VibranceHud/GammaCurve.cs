using System;

namespace VibranceHud;

public static class GammaCurve
{
	public const int Entries = 256;

	public static ushort[] Build(float gamma)
	{
		if (gamma <= 0f)
		{
			gamma = 1f;
		}
		ushort[] array = new ushort[768];
		double y = 1.0 / (double)gamma;
		for (int i = 0; i < 256; i++)
		{
			array[512 + i] = (array[256 + i] = (array[i] = (ushort)Math.Clamp(Math.Round(Math.Pow((double)i / 255.0, y) * 65535.0), 0.0, 65535.0)));
		}
		return array;
	}

	public static ushort[] Identity()
	{
		return Build(1f);
	}
}
