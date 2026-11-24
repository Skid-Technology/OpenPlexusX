using System;

namespace VibranceHud;

public static class ColourBalance
{
	public const int MinHue = -180;

	public const int MaxHue = 180;

	public const int MinGain = 50;

	public const int MaxGain = 150;

	public static bool GainsNeutral(int red, int green, int blue)
	{
		if (red == 100 && green == 100)
		{
			return blue == 100;
		}
		return false;
	}

	public static bool IsNeutral(int hue, int red, int green, int blue)
	{
		if (hue == 0)
		{
			return GainsNeutral(red, green, blue);
		}
		return false;
	}

	public static float[] HueMatrix(int degrees)
	{
		double num = (double)Math.Clamp(degrees, -180, 180) * Math.PI / 180.0;
		double num2 = Math.Cos(num);
		double num3 = Math.Sin(num);
		double num4 = (1.0 - num2) / 3.0;
		double num5 = num3 / Math.Sqrt(3.0);
		int[,] array = new int[3, 3]
		{
			{ 0, -1, 1 },
			{ 1, 0, -1 },
			{ -1, 1, 0 }
		};
		float[] array2 = new float[25];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				array2[i * 5 + j] = (float)(((i == j) ? num2 : 0.0) + num4 + num5 * (double)array[j, i]);
			}
		}
		array2[18] = 1f;
		array2[24] = 1f;
		return array2;
	}

	public static float[] GainMatrix(int red, int green, int blue)
	{
		return new float[25]
		{
			(float)Math.Clamp(red, 50, 150) / 100f,
			0f,
			0f,
			0f,
			0f,
			0f,
			(float)Math.Clamp(green, 50, 150) / 100f,
			0f,
			0f,
			0f,
			0f,
			0f,
			(float)Math.Clamp(blue, 50, 150) / 100f,
			0f,
			0f,
			0f,
			0f,
			0f,
			1f,
			0f,
			0f,
			0f,
			0f,
			0f,
			1f
		};
	}

	public static float[] ComposeInto(float[] matrix, int hue, int red, int green, int blue)
	{
		if (IsNeutral(hue, red, green, blue))
		{
			return matrix;
		}
		float[] array = matrix;
		if (hue != 0)
		{
			array = Multiply(array, HueMatrix(hue));
		}
		if (!GainsNeutral(red, green, blue))
		{
			array = Multiply(array, GainMatrix(red, green, blue));
		}
		return array;
	}

	internal static float[] Multiply(float[] a, float[] b)
	{
		float[] array = new float[25];
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				double num = 0.0;
				for (int k = 0; k < 5; k++)
				{
					num += (double)a[i * 5 + k] * (double)b[k * 5 + j];
				}
				array[i * 5 + j] = (float)num;
			}
		}
		return array;
	}
}
