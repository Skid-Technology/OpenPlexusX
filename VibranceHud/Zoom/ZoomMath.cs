using System;
using System.Drawing;

namespace VibranceHud.Zoom;

internal static class ZoomMath
{
	public const float MinLevel = 1.5f;

	public const float MaxLevel = 4f;

	public const float Step = 0.25f;

	public const float DefaultLevel = 2f;

	public static float ClampSetting(float level)
	{
		if (float.IsNaN(level) || float.IsInfinity(level))
		{
			return 2f;
		}
		level = Math.Clamp(level, 1.5f, 4f);
		return (float)(Math.Round(level / 0.25f, MidpointRounding.AwayFromZero) * 0.25);
	}

	public static (int X, int Y) CentreOffsets(Rectangle bounds, float level)
	{
		if (!(level > 1f))
		{
			return (X: 0, Y: 0);
		}
		double num = (1.0 - 1.0 / (double)level) / 2.0;
		return (X: bounds.Left + (int)((double)bounds.Width * num), Y: bounds.Top + (int)((double)bounds.Height * num));
	}

	public static float Ease(float from, float to, float t)
	{
		if (!(t < 1f))
		{
			return to;
		}
		if (t <= 0f)
		{
			return from;
		}
		float num = 1f - t;
		return from + (to - from) * (1f - num * num * num);
	}
}
