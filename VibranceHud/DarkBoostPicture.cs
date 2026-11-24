using System;

namespace VibranceHud;

internal static class DarkBoostPicture
{
	internal const int MatrixEntries = 25;

	internal const int Levels = 256;

	internal const int RampEntries = 768;

	internal const int WindowsRampRange = 128;

	internal const int WhiteLevel = 200;

	private static readonly (int, int, int)[] Hues = new (int, int, int)[7]
	{
		(1, 1, 1),
		(1, 0, 0),
		(0, 1, 0),
		(0, 0, 1),
		(1, 1, 0),
		(0, 1, 1),
		(1, 0, 1)
	};

	internal static bool Structural(float[] m)
	{
		if (m == null || m.Length != 25)
		{
			return false;
		}
		for (int i = 0; i < m.Length; i++)
		{
			if (!float.IsFinite(m[i]))
			{
				return false;
			}
		}
		if (m[3] != 0f || m[8] != 0f || m[13] != 0f || m[18] != 1f || m[23] != 0f)
		{
			return false;
		}
		if (m[15] != 0f || m[16] != 0f || m[17] != 0f || m[19] != 0f)
		{
			return false;
		}
		if (m[4] != 0f || m[9] != 0f || m[14] != 0f || m[24] != 1f)
		{
			return false;
		}
		if (m[20] > 0f || m[21] > 0f || m[22] > 0f)
		{
			return false;
		}
		return true;
	}

	internal static bool Safe(ushort[] ramp)
	{
		if (ramp == null || ramp.Length != 768)
		{
			return false;
		}
		for (int i = 0; i < 3; i++)
		{
			int num = i * 256;
			if (ramp[num + 256 - 1] != ushort.MaxValue)
			{
				return false;
			}
			for (int j = 0; j < 256; j++)
			{
				if (j > 0 && ramp[num + j] < ramp[num + j - 1])
				{
					return false;
				}
				if (Math.Abs((ramp[num + j] >> 8) - j) > 128)
				{
					return false;
				}
			}
		}
		return true;
	}

	internal static ushort[] Straight()
	{
		return GammaCurve.Identity();
	}

	internal static bool IsStraight(ushort[] ramp)
	{
		ushort[] array = Straight();
		if (ramp.Length != array.Length)
		{
			return false;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (ramp[i] != array[i])
			{
				return false;
			}
		}
		return true;
	}

	internal static bool NeverWhiterThan(float[] matrix, ushort[] ramp, float[] reference)
	{
		ushort[] ramp2 = Straight();
		Span<byte> span = stackalloc byte[3];
		Span<byte> span2 = stackalloc byte[3];
		(int, int, int)[] hues = Hues;
		for (int i = 0; i < hues.Length; i++)
		{
			(int, int, int) tuple = hues[i];
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			int item3 = tuple.Item3;
			for (int j = 0; j < 256; j++)
			{
				byte r = (byte)(item * j);
				byte g = (byte)(item2 * j);
				byte b = (byte)(item3 * j);
				Pixel(r, g, b, matrix, ramp, span);
				Pixel(r, g, b, reference, ramp2, span2);
				bool flag = IsWhite(span);
				bool flag2 = IsWhite(span2);
				if (flag && !flag2)
				{
					return false;
				}
				if (IsCyan(span) && !flag2 && !IsCyan(span2))
				{
					return false;
				}
			}
		}
		return true;
	}

	internal static bool IsWhite(ReadOnlySpan<byte> p)
	{
		if (p[0] >= 200 && p[1] >= 200)
		{
			return p[2] >= 200;
		}
		return false;
	}

	internal static bool IsCyan(ReadOnlySpan<byte> p)
	{
		if (p[1] >= 200 && p[2] >= 200)
		{
			return p[0] < 200;
		}
		return false;
	}

	internal static float[] Matrix(float[] served, double strength)
	{
		double num = Math.Clamp(strength, 0.0, 1.0);
		float[] array = (float[])served.Clone();
		if (num >= 1.0)
		{
			return array;
		}
		for (int i = 0; i < 25; i++)
		{
			array[i] = (float)(num * (double)served[i] + (1.0 - num) * (double)((i / 5 == i % 5) ? 1 : 0));
		}
		return array;
	}

	internal static ushort[] Ramp(ushort[] served, double strength)
	{
		double num = Math.Clamp(strength, 0.0, 1.0);
		ushort[] array = (ushort[])served.Clone();
		if (num >= 1.0)
		{
			return array;
		}
		ushort[] array2 = Straight();
		for (int i = 0; i < 768; i++)
		{
			array[i] = (ushort)Math.Clamp(Math.Round(num * (double)(int)served[i] + (1.0 - num) * (double)(int)array2[i]), 0.0, 65535.0);
		}
		return array;
	}

	internal static ushort[] ComposeAfter(ushort[] user, ushort[] boost)
	{
		ushort[] array = new ushort[768];
		for (int i = 0; i < 3; i++)
		{
			int num = i * 256;
			for (int j = 0; j < 256; j++)
			{
				double num2 = (double)(int)user[num + j] * 255.0 / 65535.0;
				int num3 = Math.Clamp((int)Math.Floor(num2), 0, 255);
				int num4 = Math.Min(num3 + 1, 255);
				double num5 = num2 - (double)num3;
				array[num + j] = (ushort)Math.Clamp(Math.Round((double)(int)boost[num + num3] + (double)(boost[num + num4] - boost[num + num3]) * num5), 0.0, 65535.0);
			}
		}
		return array;
	}

	internal static void Pixel(byte r, byte g, byte b, float[] m, ushort[] ramp, Span<byte> outRgb)
	{
		double num = (double)(int)r / 255.0;
		double num2 = (double)(int)g / 255.0;
		double num3 = (double)(int)b / 255.0;
		for (int i = 0; i < 3; i++)
		{
			int num4 = Q8(num * (double)m[i] + num2 * (double)m[5 + i] + num3 * (double)m[10 + i] + (double)m[20 + i]);
			outRgb[i] = (byte)Q8((double)(int)ramp[i * 256 + num4] / 65535.0);
		}
	}

	internal static void Render(byte[] rgb, float[] m, ushort[] ramp)
	{
		Span<byte> outRgb = stackalloc byte[3];
		for (int i = 0; i + 2 < rgb.Length; i += 3)
		{
			Pixel(rgb[i], rgb[i + 1], rgb[i + 2], m, ramp, outRgb);
			rgb[i] = outRgb[0];
			rgb[i + 1] = outRgb[1];
			rgb[i + 2] = outRgb[2];
		}
	}

	private static int Q8(double v)
	{
		return (int)Math.Clamp(Math.Round(Math.Clamp(v, 0.0, 1.0) * 255.0), 0.0, 255.0);
	}
}
