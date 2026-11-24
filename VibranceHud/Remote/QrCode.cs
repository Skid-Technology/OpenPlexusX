using System;
using System.Collections.Generic;
using System.Text;

namespace VibranceHud.Remote;

public static class QrCode
{
	private sealed class BitBucket
	{
		private readonly List<bool> _bits = new List<bool>();

		public int Length => _bits.Count;

		public void Add(int value, int bits)
		{
			for (int num = bits - 1; num >= 0; num--)
			{
				_bits.Add(((value >> num) & 1) == 1);
			}
		}

		public void PadToByte()
		{
			while (_bits.Count % 8 != 0)
			{
				_bits.Add(item: false);
			}
		}

		public byte[] ToBytes()
		{
			byte[] array = new byte[_bits.Count / 8];
			for (int i = 0; i < _bits.Count; i++)
			{
				if (_bits[i])
				{
					array[i / 8] |= (byte)(1 << 7 - i % 8);
				}
			}
			return array;
		}
	}

	private const int EccLevelBits = 0;

	public const int MinVersion = 1;

	public const int MaxVersion = 6;

	private static readonly (int Total, int EccPerBlock, int Blocks)[] Layout = new (int, int, int)[7]
	{
		(0, 0, 0),
		(26, 10, 1),
		(44, 16, 1),
		(70, 26, 1),
		(100, 18, 2),
		(134, 24, 2),
		(172, 16, 4)
	};

	private static int RemainderBits(int version)
	{
		if (version != 1)
		{
			return 7;
		}
		return 0;
	}

	private static int Size(int version)
	{
		return 17 + 4 * version;
	}

	private static int DataCodewords(int version)
	{
		var (num, num2, num3) = Layout[version];
		return num - num2 * num3;
	}

	public static int Capacity(int version)
	{
		return DataCodewords(version) - 2;
	}

	public static int SmallestVersionFor(string text)
	{
		int byteCount = Encoding.UTF8.GetByteCount(text ?? "");
		for (int i = 1; i <= 6; i++)
		{
			if (byteCount <= Capacity(i))
			{
				return i;
			}
		}
		return 0;
	}

	public static bool[,] Encode(string text)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		int num = SmallestVersionFor(text);
		if (num == 0)
		{
			throw new ArgumentException($"That is {Encoding.UTF8.GetByteCount(text)} bytes and the largest symbol here holds {Capacity(6)}.", "text");
		}
		byte[] codewords = Codewords(text, num);
		bool[,] result = null;
		int num2 = int.MaxValue;
		for (int i = 0; i < 8; i++)
		{
			bool[,] array = Draw(codewords, num, i);
			int num3 = Penalty(array);
			if (num3 < num2)
			{
				num2 = num3;
				result = array;
			}
		}
		return result;
	}

	internal static bool[,] EncodeExactly(string text, int version, int mask)
	{
		if (version < 1 || version > 6)
		{
			throw new ArgumentOutOfRangeException("version");
		}
		if (mask < 0 || mask > 7)
		{
			throw new ArgumentOutOfRangeException("mask");
		}
		return Draw(Codewords(text, version), version, mask);
	}

	private static byte[] Codewords(string text, int version)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(text ?? "");
		if (bytes.Length > Capacity(version))
		{
			throw new ArgumentException("too long for this version", "text");
		}
		BitBucket bitBucket = new BitBucket();
		bitBucket.Add(4, 4);
		bitBucket.Add(bytes.Length, 8);
		byte[] array = bytes;
		foreach (byte value in array)
		{
			bitBucket.Add(value, 8);
		}
		int num = DataCodewords(version) * 8;
		bitBucket.Add(0, Math.Min(4, num - bitBucket.Length));
		bitBucket.PadToByte();
		byte[] array2 = bitBucket.ToBytes();
		byte[] array3 = new byte[DataCodewords(version)];
		Array.Copy(array2, array3, array2.Length);
		for (int j = array2.Length; j < array3.Length; j++)
		{
			array3[j] = (byte)(((j - array2.Length) % 2 == 0) ? 236 : 17);
		}
		return Interleave(array3, version);
	}

	private static byte[] Interleave(byte[] data, int version)
	{
		(int Total, int EccPerBlock, int Blocks) tuple = Layout[version];
		int item = tuple.Total;
		int item2 = tuple.EccPerBlock;
		int item3 = tuple.Blocks;
		int num = data.Length / item3;
		byte[][] array = new byte[item3][];
		byte[][] array2 = new byte[item3][];
		for (int i = 0; i < item3; i++)
		{
			array[i] = new byte[num];
			Array.Copy(data, i * num, array[i], 0, num);
			array2[i] = ReedSolomon.Remainder(array[i], item2);
		}
		byte[] array3 = new byte[item];
		int num2 = 0;
		for (int j = 0; j < num; j++)
		{
			for (int k = 0; k < item3; k++)
			{
				array3[num2++] = array[k][j];
			}
		}
		for (int l = 0; l < item2; l++)
		{
			for (int m = 0; m < item3; m++)
			{
				array3[num2++] = array2[m][l];
			}
		}
		return array3;
	}

	private static bool[,] Draw(byte[] codewords, int version, int mask)
	{
		int num = Size(version);
		bool[,] array = new bool[num, num];
		bool[,] array2 = new bool[num, num];
		PlaceFinder(array, array2, 0, 0, num);
		PlaceFinder(array, array2, 0, num - 7, num);
		PlaceFinder(array, array2, num - 7, 0, num);
		PlaceTiming(array, array2, num);
		PlaceAlignment(array, array2, version, num);
		ReserveFormat(array2, num);
		array[num - 8, 8] = true;
		array2[num - 8, 8] = true;
		PlaceData(array, array2, codewords, num, version, mask);
		PlaceFormat(array, num, mask);
		return array;
	}

	private static void PlaceFinder(bool[,] dark, bool[,] reserved, int row, int col, int size)
	{
		for (int i = -1; i <= 7; i++)
		{
			for (int j = -1; j <= 7; j++)
			{
				int num = row + i;
				int num2 = col + j;
				if (num >= 0 && num < size && num2 >= 0 && num2 < size)
				{
					bool flag = i >= 0 && i <= 6 && j >= 0 && j <= 6;
					bool flag2 = i == 0 || i == 6 || j == 0 || j == 6;
					bool flag3 = i >= 2 && i <= 4 && j >= 2 && j <= 4;
					dark[num, num2] = flag && (flag2 | flag3);
					reserved[num, num2] = true;
				}
			}
		}
	}

	private static void PlaceTiming(bool[,] dark, bool[,] reserved, int size)
	{
		for (int i = 8; i < size - 8; i++)
		{
			bool flag = (dark[6, i] = i % 2 == 0);
			reserved[6, i] = true;
			dark[i, 6] = flag;
			reserved[i, 6] = true;
		}
	}

	private static void PlaceAlignment(bool[,] dark, bool[,] reserved, int version, int size)
	{
		if (version < 2)
		{
			return;
		}
		int num = 4 * version + 10;
		for (int i = -2; i <= 2; i++)
		{
			for (int j = -2; j <= 2; j++)
			{
				int num2 = num + i;
				int num3 = num + j;
				dark[num2, num3] = Math.Max(Math.Abs(i), Math.Abs(j)) != 1;
				reserved[num2, num3] = true;
			}
		}
	}

	private static void ReserveFormat(bool[,] reserved, int size)
	{
		for (int i = 0; i < 9; i++)
		{
			if (i != 6)
			{
				reserved[8, i] = true;
				reserved[i, 8] = true;
			}
		}
		reserved[8, 6] = true;
		reserved[6, 8] = true;
		for (int j = 0; j < 8; j++)
		{
			reserved[8, size - 1 - j] = true;
			reserved[size - 1 - j, 8] = true;
		}
	}

	private static void PlaceData(bool[,] dark, bool[,] reserved, byte[] codewords, int size, int version, int mask)
	{
		int num = 0;
		int num2 = codewords.Length * 8 + RemainderBits(version);
		bool flag = true;
		for (int num3 = size - 1; num3 >= 1; num3 -= 2)
		{
			if (num3 == 6)
			{
				num3 = 5;
			}
			for (int i = 0; i < size; i++)
			{
				int num4 = (flag ? (size - 1 - i) : i);
				for (int j = 0; j < 2; j++)
				{
					int num5 = num3 - j;
					if (!reserved[num4, num5] && num < num2)
					{
						bool flag2 = num < codewords.Length * 8 && ((codewords[num / 8] >> 7 - num % 8) & 1) == 1;
						num++;
						dark[num4, num5] = flag2 ^ Mask(mask, num4, num5);
					}
				}
			}
			flag = !flag;
		}
	}

	private static bool Mask(int mask, int row, int col)
	{
		return mask switch
		{
			0 => (row + col) % 2 == 0, 
			1 => row % 2 == 0, 
			2 => col % 3 == 0, 
			3 => (row + col) % 3 == 0, 
			4 => (row / 2 + col / 3) % 2 == 0, 
			5 => row * col % 2 + row * col % 3 == 0, 
			6 => (row * col % 2 + row * col % 3) % 2 == 0, 
			7 => ((row + col) % 2 + row * col % 3) % 2 == 0, 
			_ => throw new ArgumentOutOfRangeException("mask"), 
		};
	}

	private static void PlaceFormat(bool[,] dark, int size, int mask)
	{
		int num = 0 | mask;
		int num2 = num << 10;
		for (int num3 = 14; num3 >= 10; num3--)
		{
			if (((num2 >> num3) & 1) == 1)
			{
				num2 ^= 1335 << num3 - 10;
			}
		}
		int num4 = ((num << 10) | num2) ^ 0x5412;
		for (int i = 0; i < 15; i++)
		{
			bool flag = ((num4 >> 14 - i) & 1) == 1;
			if (i < 6)
			{
				dark[8, i] = flag;
			}
			else
			{
				switch (i)
				{
				case 6:
					dark[8, 7] = flag;
					break;
				case 7:
					dark[8, 8] = flag;
					break;
				case 8:
					dark[7, 8] = flag;
					break;
				default:
					dark[14 - i, 8] = flag;
					break;
				}
			}
			if (i < 7)
			{
				dark[size - 1 - i, 8] = flag;
			}
			else
			{
				dark[8, size - 15 + i] = flag;
			}
		}
	}

	internal static int Penalty(bool[,] dark)
	{
		int length = dark.GetLength(0);
		int num = 0;
		for (int i = 0; i < length; i++)
		{
			num += RunPenalty(dark, i, horizontal: true, length);
			num += RunPenalty(dark, i, horizontal: false, length);
		}
		for (int j = 0; j < length - 1; j++)
		{
			for (int k = 0; k < length - 1; k++)
			{
				if (dark[j, k] == dark[j, k + 1] && dark[j, k] == dark[j + 1, k] && dark[j, k] == dark[j + 1, k + 1])
				{
					num += 3;
				}
			}
		}
		num += 40 * FinderLookalikes(dark, length);
		int num2 = 0;
		int upperBound = dark.GetUpperBound(0);
		int upperBound2 = dark.GetUpperBound(1);
		for (int l = dark.GetLowerBound(0); l <= upperBound; l++)
		{
			for (int m = dark.GetLowerBound(1); m <= upperBound2; m++)
			{
				if (dark[l, m])
				{
					num2++;
				}
			}
		}
		int num3 = num2 * 100 / (length * length);
		return num + Math.Abs(num3 - 50) / 5 * 10;
	}

	private static int RunPenalty(bool[,] dark, int line, bool horizontal, int size)
	{
		int num = 0;
		int num2 = 1;
		for (int i = 1; i < size; i++)
		{
			bool flag = (horizontal ? dark[line, i] : dark[i, line]);
			bool flag2 = (horizontal ? dark[line, i - 1] : dark[i - 1, line]);
			if (flag == flag2)
			{
				num2++;
				continue;
			}
			if (num2 >= 5)
			{
				num += num2 - 2;
			}
			num2 = 1;
		}
		if (num2 >= 5)
		{
			num += num2 - 2;
		}
		return num;
	}

	private static int FinderLookalikes(bool[,] dark, int size)
	{
		bool[] pattern = new bool[7] { true, false, true, true, true, false, true };
		int num = 0;
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j + 7 <= size; j++)
			{
				if (Matches(dark, i, j, pattern, horizontal: true, size) && (Quiet(dark, i, j - 4, 4, horizontal: true, size) || Quiet(dark, i, j + 7, 4, horizontal: true, size)))
				{
					num++;
				}
			}
			for (int k = 0; k + 7 <= size; k++)
			{
				if (Matches(dark, i, k, pattern, horizontal: false, size) && (Quiet(dark, i, k - 4, 4, horizontal: false, size) || Quiet(dark, i, k + 7, 4, horizontal: false, size)))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static bool Matches(bool[,] dark, int line, int start, bool[] pattern, bool horizontal, int size)
	{
		for (int i = 0; i < pattern.Length; i++)
		{
			int num = start + i;
			if (num < 0 || num >= size)
			{
				return false;
			}
			if ((horizontal ? dark[line, num] : dark[num, line]) != pattern[i])
			{
				return false;
			}
		}
		return true;
	}

	private static bool Quiet(bool[,] dark, int line, int start, int count, bool horizontal, int size)
	{
		for (int i = 0; i < count; i++)
		{
			int num = start + i;
			if (num >= 0 && num < size && (horizontal ? dark[line, num] : dark[num, line]))
			{
				return false;
			}
		}
		return true;
	}
}
