using System;
using System.Collections.Generic;

namespace VibranceHud;

public static class DisplayOnlyLook
{
	public enum Knob
	{
		Stronger,
		Vibrance,
		Saturation
	}

	public static int SaturationAsDriverVibrance(int saturation)
	{
		saturation = Math.Clamp(saturation, 0, 300);
		if (saturation <= 100)
		{
			return (int)Math.Round((double)saturation * 0.5);
		}
		double num = (double)(saturation - 100) / 200.0;
		return (int)Math.Round(50.0 + num * 50.0);
	}

	public static int DriverLevelFor(int vibrance, int saturation)
	{
		return Math.Clamp(Math.Max(Math.Clamp(vibrance, 0, 100), SaturationAsDriverVibrance(saturation)), 0, 100);
	}

	public static int DriverLevelFor(int vibrance, int saturation, Knob follows)
	{
		return follows switch
		{
			Knob.Vibrance => Math.Clamp(vibrance, 0, 100), 
			Knob.Saturation => SaturationAsDriverVibrance(saturation), 
			_ => DriverLevelFor(vibrance, saturation), 
		};
	}

	public static ushort[] Compose(ushort[] toneRamp, int brightness, int contrast, int temperature)
	{
		return Compose(toneRamp, brightness, contrast, temperature, 100, 100, 100);
	}

	public static ushort[] Compose(ushort[] toneRamp, int brightness, int contrast, int temperature, int red, int green, int blue)
	{
		if (toneRamp == null)
		{
			throw new ArgumentNullException("toneRamp");
		}
		float num = (float)Math.Clamp(brightness, 50, 170) / 100f;
		float num2 = (float)Math.Clamp(contrast, 50, 150) / 100f;
		float num3 = (float)Math.Clamp(temperature, -100, 100) / 100f;
		if (IsNeutral(num, num2, num3) && ColourBalance.GainsNeutral(red, green, blue))
		{
			return toneRamp;
		}
		double[] array = new double[3]
		{
			(double)Math.Clamp(red, 50, 150) / 100.0,
			(double)Math.Clamp(green, 50, 150) / 100.0,
			(double)Math.Clamp(blue, 50, 150) / 100.0
		};
		float num4 = Math.Max(0f, num3);
		float num5 = Math.Max(0f, 0f - num3);
		float num6 = 1f - 0.38f * num5;
		float num7 = 1f - 0.12f * (num4 + num5);
		float num8 = 1f - 0.38f * num4;
		float[] array2 = new float[3]
		{
			num * num6 * num2,
			num * num7 * num2,
			num * num8 * num2
		};
		float num9 = 0.5f * (1f - num2);
		int num10 = 256;
		ushort[] array3 = new ushort[num10 * 3];
		for (int i = 0; i < 3; i++)
		{
			int num11 = i * num10;
			for (int j = 0; j < num10; j++)
			{
				int value = (int)Math.Round(Math.Clamp(((double)j / (double)(num10 - 1) * (double)array2[i] + (double)num9) * array[i], 0.0, 1.0) * (double)(num10 - 1));
				array3[num11 + j] = toneRamp[num11 + Math.Clamp(value, 0, num10 - 1)];
			}
		}
		for (int k = 0; k < 3; k++)
		{
			int num12 = k * num10;
			for (int l = 1; l < num10; l++)
			{
				if (array3[num12 + l] < array3[num12 + l - 1])
				{
					array3[num12 + l] = array3[num12 + l - 1];
				}
			}
		}
		return array3;
	}

	public static bool IsNeutral(float brightness, float contrast, float warmth)
	{
		if (Math.Abs(brightness - 1f) < 0.001f && Math.Abs(contrast - 1f) < 0.001f)
		{
			return Math.Abs(warmth) < 0.001f;
		}
		return false;
	}

	public static bool IsNeutral(int brightness, int contrast, int temperature)
	{
		return IsNeutral((float)brightness / 100f, (float)contrast / 100f, (float)temperature / 100f);
	}

	public static string? WhatTheDisplayCannotHold(ChannelMix mix, ColourVisionMode correction)
	{
		return WhatTheDisplayCannotHold(mix, correction, 0);
	}

	public static string? WhatTheDisplayCannotHold(ChannelMix mix, ColourVisionMode correction, int hue)
	{
		bool num = mix != null && !mix.IsIdentity;
		bool flag = correction != ColourVisionMode.Off;
		List<string> list = new List<string>();
		if (flag)
		{
			list.Add("the colour help you turned on");
		}
		if (num)
		{
			list.Add("the colour shift in this look");
		}
		if (hue != 0)
		{
			list.Add("the hue");
		}
		switch (list.Count)
		{
		case 0:
			return null;
		case 1:
			return list[0];
		case 2:
			return list[0] + " and " + list[1];
		default:
			return string.Join(", ", list.GetRange(0, list.Count - 1)) + ", and " + list[list.Count - 1];
		}
	}
}
