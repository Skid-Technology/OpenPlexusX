using System;

namespace VibranceHud.Theming;

public static class BackgroundFrameRate
{
	public const int ComposeShare = 4;

	public static int IntervalMs(double frameSeconds, double lastComposeMs, int slowestMs)
	{
		double num = Math.Max(frameSeconds, 1.0 / 30.0);
		return IntervalMs(num, num, lastComposeMs, slowestMs);
	}

	public static int IntervalMs(double untilNextSeconds, double frameSeconds, double lastComposeMs, int slowestMs)
	{
		double num = Math.Max(frameSeconds, 1.0 / 30.0) * 1000.0;
		double num2 = Math.Max(0.0, lastComposeMs) * 4.0;
		return (int)Math.Clamp(Math.Round(Math.Max(0.0, untilNextSeconds) * 1000.0 + Math.Max(0.0, num2 - num)), 1.0, Math.Max(1, slowestMs));
	}
}
