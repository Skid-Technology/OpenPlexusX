using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using VibranceHud.DarkBoost;

namespace VibranceHud.Pages;

internal static class DarkBoostScene
{
	internal const int W = 360;

	internal const int H = 120;

	private const double Gain = 4.0;

	private static float[]? _scene;

	private static double Hash(double x, double y)
	{
		double num = Math.Sin(x * 127.1 + y * 311.7) * 43758.5453;
		return num - Math.Floor(num);
	}

	internal static float[] Scene()
	{
		if (_scene != null)
		{
			return _scene;
		}
		float[] array = new float[129600];
		for (int i = 0; i < 120; i++)
		{
			for (int j = 0; j < 360; j++)
			{
				double num = 70.0 + 10.0 * Math.Sin((double)j / 40.0) + 5.0 * Math.Sin((double)j / 13.0);
				double num2 = (Hash(j, i) - 0.5) * 0.004;
				double num4;
				double num5;
				double num6;
				if ((double)i < num)
				{
					double num3 = 0.022 + 0.012 * ((double)i / num);
					num4 = num3 * 0.6;
					num5 = num3 * 0.75;
					num6 = num3;
				}
				else
				{
					double num7 = 0.016 + 0.005 * Math.Sin((double)j / 23.0) * Math.Sin((double)i / 11.0);
					num4 = num7 * 0.9;
					num5 = num7;
					num6 = num7 * 0.6;
				}
				double value = j % 70 - 35;
				double num8 = num - 34.0;
				if (j > 20 && j < 330 && (double)i > num8 + Math.Abs(value) * 1.3 && (double)i < num + 6.0 && Math.Abs(value) < 18.0 && Math.Floor((double)j / 70.0) % 2.0 == 0.0)
				{
					num4 = 0.006;
					num5 = 0.009;
					num6 = 0.006;
				}
				if (j >= 230 && j <= 300 && i >= 48 && i <= 96)
				{
					num4 = 0.03;
					num5 = 0.024;
					num6 = 0.02;
				}
				if (j >= 250 && j <= 266 && i >= 60 && i <= 74)
				{
					num4 = 0.85;
					num5 = 0.62;
					num6 = 0.3;
				}
				double num9 = Math.Sqrt((j - 120) * (j - 120) + (i - 30) * (i - 30));
				if (num9 < 4.0)
				{
					num4 = 0.95;
					num5 = 0.9;
					num6 = 0.75;
				}
				else if (num9 < 30.0)
				{
					double num10 = (30.0 - num9) / 30.0 * 0.05;
					num4 += num10;
					num5 += num10 * 0.9;
					num6 += num10 * 0.6;
				}
				if (Math.Sqrt(Math.Pow((double)(j - 60) / 1.6, 2.0) + Math.Pow(i - 104, 2.0)) < 9.0 || Math.Sqrt(Math.Pow((double)(j - 175) / 2.0, 2.0) + Math.Pow(i - 100, 2.0)) < 7.0)
				{
					num4 = 0.008;
					num5 = 0.008;
					num6 = 0.009;
				}
				int num11 = (i * 360 + j) * 3;
				array[num11] = (float)(num4 + num2);
				array[num11 + 1] = (float)(num5 + num2);
				array[num11 + 2] = (float)(num6 + num2);
			}
		}
		return _scene = array;
	}

	internal static float[] IllustrationBase()
	{
		double[] array = DarkBoostTints.PreviewWeights(DarkBoostTint.Green);
		float[] array2 = new float[25];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				array2[i * 5 + j] = (float)(4.0 * array[j]);
			}
		}
		for (int k = 0; k < 3; k++)
		{
			array2[20 + k] = (float)(-12.0 * array[k] / 255.0);
		}
		array2[18] = 1f;
		array2[24] = 1f;
		return array2;
	}

	internal static float[] PictureFor(DarkBoostSetup setup, double strength, float[]? basis)
	{
		return DarkBoostLook.Picture(basis ?? IllustrationBase(), setup.Tint, setup.BlackLevel, strength);
	}

	internal static double WhiteFrom(DarkBoostSetup setup, float[]? basis)
	{
		return DarkBoostLook.WhiteFrom(PictureFor(setup, (double)setup.Strength / 100.0, basis));
	}

	internal static Bitmap Render(float[] matrix, ushort[]? ramp)
	{
		float[] array = Scene();
		Bitmap bitmap = new Bitmap(360, 120, PixelFormat.Format32bppArgb);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, 360, 120), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		try
		{
			byte[] array2 = new byte[1440];
			for (int i = 0; i < 120; i++)
			{
				for (int j = 0; j < 360; j++)
				{
					int num = (i * 360 + j) * 3;
					var (b, b2, b3) = DarkBoostLook.Pixel(array[num], array[num + 1], array[num + 2], matrix, ramp);
					array2[j * 4] = b3;
					array2[j * 4 + 1] = b2;
					array2[j * 4 + 2] = b;
					array2[j * 4 + 3] = byte.MaxValue;
				}
				Marshal.Copy(array2, 0, bitmapData.Scan0 + i * bitmapData.Stride, array2.Length);
			}
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
		}
		return bitmap;
	}
}
