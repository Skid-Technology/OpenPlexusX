using System;

namespace VibranceHud.Remote;

internal static class ReedSolomon
{
	private const int Primitive = 285;

	private static readonly byte[] Exp;

	private static readonly byte[] Log;

	static ReedSolomon()
	{
		Exp = new byte[512];
		Log = new byte[256];
		int num = 1;
		for (int i = 0; i < 255; i++)
		{
			Exp[i] = (byte)num;
			Log[num] = (byte)i;
			num <<= 1;
			if (num >= 256)
			{
				num ^= 0x11D;
			}
		}
		for (int j = 255; j < 512; j++)
		{
			Exp[j] = Exp[j - 255];
		}
	}

	internal static byte Multiply(byte a, byte b)
	{
		if (a != 0 && b != 0)
		{
			return Exp[Log[a] + Log[b]];
		}
		return 0;
	}

	internal static byte[] Generator(int degree)
	{
		byte[] array = new byte[1] { 1 };
		for (int i = 0; i < degree; i++)
		{
			byte[] array2 = new byte[array.Length + 1];
			for (int j = 0; j < array.Length; j++)
			{
				array2[j] ^= array[j];
				array2[j + 1] ^= Multiply(array[j], Exp[i]);
			}
			array = array2;
		}
		return array;
	}

	internal static byte[] Remainder(byte[] data, int degree)
	{
		byte[] array = Generator(degree);
		byte[] array2 = new byte[degree];
		for (int i = 0; i < data.Length; i++)
		{
			byte b = (byte)(data[i] ^ array2[0]);
			Array.Copy(array2, 1, array2, 0, degree - 1);
			array2[degree - 1] = 0;
			for (int j = 0; j < degree; j++)
			{
				array2[j] ^= Multiply(array[j + 1], b);
			}
		}
		return array2;
	}
}
