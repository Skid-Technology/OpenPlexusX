using System;

namespace VibranceHud.DarkBoost;

public static class DarkBoostFade
{
	public static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(250.0);

	public static double Amount(double from, double to, TimeSpan elapsed, bool fade)
	{
		if (!fade || elapsed >= Duration)
		{
			return to;
		}
		if (elapsed <= TimeSpan.Zero)
		{
			return from;
		}
		double num = elapsed.TotalMilliseconds / Duration.TotalMilliseconds;
		return from + (to - from) * num;
	}
}
