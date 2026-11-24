using System;

namespace VibranceHud;

public static class ColorAdjust
{
	public const float WarmGreenCut = 0.12f;

	public const float WarmBlueCut = 0.38f;

	public const float VibranceRedHold = 0.55f;

	public static float[] Build(float saturation, float brightness, float warmth)
	{
		return BuildWithVibranceNumbers(saturation, 1f, 1f, 1f, brightness, warmth);
	}

	internal static float[] BuildWithVibranceNumbers(float saturation, float red, float chroma, float contrast, float brightness, float warmth)
	{
		return BuildWithVibranceNumbers(saturation, red, chroma, contrast, brightness, warmth, ColourDepth.Neutral);
	}

	internal static float[] BuildWithVibranceNumbers(float saturation, float red, float chroma, float contrast, float brightness, float warmth, ColourDepth depth)
	{
		float[] array = (depth.IsNeutral ? SaturationMatrix.Build(saturation * red, saturation * chroma, saturation * chroma) : SaturationMetrics.BuildWithVibranceNumbers(depth.Formula, saturation, red, chroma, depth));
		float num = Math.Max(0f, warmth);
		float num2 = Math.Max(0f, 0f - warmth);
		float num3 = 1f - 0.38f * num2;
		float num4 = 1f - 0.12f * (num + num2);
		float num5 = 1f - 0.38f * num;
		float num6 = brightness * num3 * contrast;
		float num7 = brightness * num4 * contrast;
		float num8 = brightness * num5 * contrast;
		float num9 = 0.5f * (1f - contrast);
		for (int i = 0; i < 3; i++)
		{
			array[i * 5] *= num6;
			array[i * 5 + 1] *= num7;
			array[i * 5 + 2] *= num8;
		}
		array[20] = num9;
		array[21] = num9;
		array[22] = num9;
		return array;
	}

	public static bool IsIdentity(float saturation, float brightness, float warmth)
	{
		return IsIdentity(saturation, 1f, brightness, warmth);
	}

	public static bool IsIdentity(float saturation, float vibrance, float brightness, float warmth)
	{
		return IsIdentity(saturation, vibrance, 1f, brightness, warmth);
	}

	public static bool IsIdentity(float saturation, float vibrance, float contrast, float brightness, float warmth)
	{
		if (Math.Abs(saturation - 1f) < 0.001f && Math.Abs(vibrance - 1f) < 0.001f && Math.Abs(contrast - 1f) < 0.001f && Math.Abs(brightness - 1f) < 0.001f)
		{
			return Math.Abs(warmth) < 0.001f;
		}
		return false;
	}
}
