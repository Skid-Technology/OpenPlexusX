using System;

namespace VibranceHud;

internal static class SliderSnap
{
	internal const int FiveFromRange = 80;

	internal const int Pull = 3;

	internal const int GentlePull = 1;

	internal static int StepFor(int minimum, int maximum)
	{
		if (maximum - minimum < 80)
		{
			return 1;
		}
		return 5;
	}

	internal static int Land(int raw, int minimum, int maximum, int? notch)
	{
		raw = Math.Clamp(raw, minimum, maximum);
		int num = StepFor(minimum, maximum);
		if (num == 1)
		{
			return raw;
		}
		int? num2 = null;
		int[] array = new int[2]
		{
			100,
			notch ?? 100
		};
		foreach (int num3 in array)
		{
			if (num3 < minimum || num3 > maximum || Math.Abs(raw - num3) > 3)
			{
				continue;
			}
			if (num2.HasValue)
			{
				int valueOrDefault = num2.GetValueOrDefault();
				if (Math.Abs(raw - num3) >= Math.Abs(raw - valueOrDefault))
				{
					continue;
				}
			}
			num2 = num3;
		}
		if (num2.HasValue)
		{
			return num2.GetValueOrDefault();
		}
		int num4 = (int)Math.Floor((double)raw / (double)num) * num;
		int value = ((raw - num4 <= num4 + num - raw) ? num4 : (num4 + num));
		value = Math.Clamp(value, minimum, maximum);
		int num5 = Math.Abs(raw - value);
		if (raw - minimum < num5)
		{
			return minimum;
		}
		if (maximum - raw < num5)
		{
			return maximum;
		}
		return value;
	}

	internal static int LandGently(int raw, int minimum, int maximum, int? notch)
	{
		raw = Math.Clamp(raw, minimum, maximum);
		if (StepFor(minimum, maximum) == 1)
		{
			return raw;
		}
		int? num = null;
		int[] array = new int[2]
		{
			100,
			notch ?? 100
		};
		foreach (int num2 in array)
		{
			if (num2 < minimum || num2 > maximum || Math.Abs(raw - num2) > 3)
			{
				continue;
			}
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				if (Math.Abs(raw - num2) >= Math.Abs(raw - valueOrDefault))
				{
					continue;
				}
			}
			num = num2;
		}
		if (num.HasValue)
		{
			return num.GetValueOrDefault();
		}
		int num3 = (int)Math.Floor((double)raw / 10.0) * 10;
		int num4 = ((raw - num3 <= num3 + 10 - raw) ? num3 : (num3 + 10));
		if (num4 >= minimum && num4 <= maximum && Math.Abs(raw - num4) <= 1)
		{
			return num4;
		}
		return raw;
	}
}
