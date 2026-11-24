using System;

namespace VibranceHud;

public static class DriverVibranceIntent
{
	public static int? For(bool userChoseAVibrance, int vibrance, int baseline, bool effectSuspended)
	{
		if (!userChoseAVibrance)
		{
			return null;
		}
		if (effectSuspended)
		{
			if (!IsARealBaseline(baseline))
			{
				return null;
			}
			return baseline;
		}
		return Clamp(vibrance);
	}

	private static bool IsARealBaseline(int baseline)
	{
		if (baseline >= 0)
		{
			return baseline <= 100;
		}
		return false;
	}

	private static int Clamp(int level)
	{
		return Math.Clamp(level, 0, 100);
	}
}
