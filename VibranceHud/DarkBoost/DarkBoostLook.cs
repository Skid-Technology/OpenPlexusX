using System;

namespace VibranceHud.DarkBoost;

internal static class DarkBoostLook
{
	internal const double CleanShare = 0.85;

	internal static double FloorScale(int blackLevel)
	{
		return 1.0 + (double)(7 * Math.Clamp(blackLevel, 0, 100)) / 100.0;
	}

	internal static int PeakColumn(float[] basis, out double columnSum)
	{
		int result = 0;
		columnSum = double.MinValue;
		for (int i = 0; i < 3; i++)
		{
			double num = basis[i] + basis[5 + i] + basis[10 + i];
			if (num > columnSum)
			{
				columnSum = num;
				result = i;
			}
		}
		return result;
	}

	internal static float[] Base(float[] basis, DarkBoostTint tint, int blackLevel)
	{
		float[] array = (float[])basis.Clone();
		double num = FloorScale(blackLevel);
		if (tint == DarkBoostTint.Green)
		{
			if (num != 1.0)
			{
				for (int i = 0; i < 3; i++)
				{
					array[20 + i] = (float)((double)basis[20 + i] * num);
				}
			}
			return array;
		}
		int num2 = PeakColumn(basis, out var columnSum);
		double[] array2 = DarkBoostTints.PreviewWeights(tint);
		for (int j = 0; j < 3; j++)
		{
			for (int k = 0; k < 3; k++)
			{
				ref float reference = ref array[j * 5 + k];
				float num3;
				if (array2 == null)
				{
					num3 = ((j == k) ? ((float)(columnSum * 0.85)) : 0f);
				}
				else
				{
					num3 = (float)((double)basis[j * 5 + num2] * array2[k]);
				}
				reference = num3;
			}
		}
		for (int l = 0; l < 3; l++)
		{
			array[20 + l] = (float)((double)basis[20 + num2] * ((array2 == null) ? 0.85 : array2[l]) * num);
		}
		return array;
	}

	internal static float[] Picture(float[] basis, DarkBoostTint tint, int blackLevel, double strength)
	{
		return DarkBoostPicture.Matrix(Base(basis, tint, blackLevel), strength);
	}

	internal static float[] Lerp(float[] own, float[] boost, double strength)
	{
		double num = (double.IsFinite(strength) ? Math.Clamp(strength, 0.0, 1.0) : 1.0);
		if (num <= 0.0)
		{
			return (float[])own.Clone();
		}
		if (num >= 1.0)
		{
			return (float[])boost.Clone();
		}
		float[] array = new float[own.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (float)((1.0 - num) * (double)own[i] + num * (double)boost[i]);
		}
		return array;
	}

	internal static float[] Identity()
	{
		float[] array = new float[25];
		for (int i = 0; i < 5; i++)
		{
			array[i * 5 + i] = 1f;
		}
		return array;
	}

	internal static (byte R, byte G, byte B) Pixel(double r, double g, double b, float[] m, ushort[]? ramp)
	{
		return (R: Ch(0), G: Ch(1), B: Ch(2));
		byte Ch(int c)
		{
			double num = r * (double)m[c] + g * (double)m[5 + c] + b * (double)m[10 + c] + (double)m[20 + c];
			int num2 = (int)Math.Clamp(Math.Round(255.0 * num, MidpointRounding.ToEven), 0.0, 255.0);
			if (ramp != null)
			{
				num2 = (int)Math.Clamp(Math.Round((double)(int)ramp[c * 256 + num2] / 257.0, MidpointRounding.ToEven), 0.0, 255.0);
			}
			return (byte)num2;
		}
	}

	internal static double WhiteFrom(float[] m)
	{
		double num = 1.0;
		for (int i = 0; i < 3; i++)
		{
			double num2 = m[i] + m[5 + i] + m[10 + i];
			if (num2 > 0.0)
			{
				num = Math.Min(num, (double)(1f - m[20 + i]) / num2);
			}
		}
		return Math.Clamp(num, 0.0, 1.0);
	}
}
