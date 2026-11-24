using System;

namespace VibranceHud;

public static class MonitorColourAdjust
{
	public const int NeutralGammaPercent = 100;

	public const int NeutralWarmthPercent = 0;

	public const int MinGammaPercent = 50;

	public const int MaxGammaPercent = 150;

	public const int MaxWarmthPercent = 100;

	public static bool IsNeutral(int gammaPercent, int warmthPercent)
	{
		if (gammaPercent == 100)
		{
			return warmthPercent == 0;
		}
		return false;
	}

	public static ushort[] Compose(ushort[] baseRamp, int gammaPercent, int warmthPercent)
	{
		if (baseRamp.Length != 768)
		{
			throw new ArgumentException($"expected a {768}-entry ramp", "baseRamp");
		}
		gammaPercent = Math.Clamp(gammaPercent, 50, 150);
		warmthPercent = Math.Clamp(warmthPercent, 0, 100);
		if (IsNeutral(gammaPercent, warmthPercent))
		{
			return baseRamp;
		}
		double y = 100.0 / (double)gammaPercent;
		double num = (double)warmthPercent / 100.0;
		double[] array = new double[3]
		{
			1.0,
			1.0 - 0.12 * num,
			1.0 - 0.38 * num
		};
		ushort[] array2 = new ushort[baseRamp.Length];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				double x = (double)(int)baseRamp[i * 256 + j] / 65535.0;
				x = Math.Pow(x, y) * array[i];
				array2[i * 256 + j] = (ushort)Math.Clamp(Math.Round(x * 65535.0), 0.0, 65535.0);
			}
		}
		return array2;
	}
}
