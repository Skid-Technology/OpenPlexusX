using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace VibranceHud.Theming;

internal static class SoftUpscale
{
	internal sealed class Small
	{
		public readonly int Width;

		public readonly int Height;

		public readonly int[] Pixels;

		public long Bytes => (long)Pixels.Length * 4L;

		public Small(int width, int height, int[] pixels)
		{
			Width = width;
			Height = height;
			Pixels = pixels;
		}

		public static Small From(Bitmap bmp)
		{
			Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			BitmapData bitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
			try
			{
				int[] array = new int[bmp.Width * bmp.Height];
				for (int i = 0; i < bmp.Height; i++)
				{
					Marshal.Copy(bitmapData.Scan0 + i * bitmapData.Stride, array, i * bmp.Width, bmp.Width);
				}
				return new Small(bmp.Width, bmp.Height, array);
			}
			finally
			{
				bmp.UnlockBits(bitmapData);
			}
		}
	}

	[ThreadStatic]
	private static int[]? _rowA;

	[ThreadStatic]
	private static int[]? _rowB;

	[ThreadStatic]
	private static int[]? _rows;

	[ThreadStatic]
	private static int[]? _x0;

	[ThreadStatic]
	private static int[]? _x1;

	[ThreadStatic]
	private static int[]? _fx;

	[ThreadStatic]
	private static int[]? _fy;

	private const uint LowPair = 16711935u;

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static void CoverInto(Small small, Rectangle cover, Bitmap target)
	{
		int width = target.Width;
		int height = target.Height;
		int width2 = small.Width;
		int height2 = small.Height;
		if (width <= 0 || height <= 0 || width2 <= 0 || height2 <= 0 || cover.Width <= 0 || cover.Height <= 0)
		{
			return;
		}
		int[] x = Grow(ref _x0, width);
		int[] x2 = Grow(ref _x1, width);
		int[] fx = Grow(ref _fx, width);
		Weights(width, cover.X, cover.Width, width2, x, x2, fx);
		int[] array = Grow(ref _rowA, width);
		int[] array2 = Grow(ref _rowB, width);
		int num = -1;
		int num2 = -1;
		int num3 = cover.Height / height2 + 2;
		int[] array3 = Grow(ref _rows, num3 * width);
		int[] array4 = Grow(ref _fy, num3);
		BitmapData bitmapData = target.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
		try
		{
			double ky = (double)height2 / (double)cover.Height;
			int num4;
			for (int i = 0; i < height; i += num4)
			{
				SourceRow(i, cover.Y, ky, height2, out var y, out var y2, out array4[0]);
				num4 = 1;
				while (i + num4 < height && num4 < num3)
				{
					SourceRow(i + num4, cover.Y, ky, height2, out var y3, out var y4, out var f);
					if (y3 != y || y4 != y2)
					{
						break;
					}
					array4[num4++] = f;
				}
				if (num != y)
				{
					if (num2 == y)
					{
						int[] array5 = array2;
						array2 = array;
						array = array5;
						int num5 = num2;
						num2 = num;
						num = num5;
					}
					else
					{
						Widen(small, y, x, x2, fx, array, width);
						num = y;
					}
				}
				if (y2 != y && num2 != y2)
				{
					Widen(small, y2, x, x2, fx, array2, width);
					num2 = y2;
				}
				if (y2 == y)
				{
					for (int j = 0; j < num4; j++)
					{
						Marshal.Copy(array, 0, bitmapData.Scan0 + (i + j) * bitmapData.Stride, width);
					}
					continue;
				}
				BlendRun(array, array2, array4, num4, array3, width);
				for (int k = 0; k < num4; k++)
				{
					Marshal.Copy(array3, k * width, bitmapData.Scan0 + (i + k) * bitmapData.Stride, width);
				}
			}
		}
		finally
		{
			target.UnlockBits(bitmapData);
		}
	}

	private static int[] Grow(ref int[]? buffer, int n)
	{
		if (buffer == null || buffer.Length < n)
		{
			buffer = new int[n];
		}
		return buffer;
	}

	private static void SourceRow(int y, int coverY, double ky, int sh, out int y0, out int y1, out int f)
	{
		double num = ((double)(y - coverY) + 0.5) * ky - 0.5;
		if (num <= 0.0)
		{
			y0 = (y1 = 0);
			f = 0;
			return;
		}
		y0 = (int)num;
		f = (int)Math.Round((num - (double)y0) * 256.0);
		if (f >= 256)
		{
			y0++;
			f = 0;
		}
		if (y0 >= sh - 1)
		{
			y0 = (y1 = sh - 1);
			f = 0;
		}
		else
		{
			y1 = ((f == 0) ? y0 : (y0 + 1));
		}
	}

	private static void Weights(int w, int coverX, int coverW, int sw, int[] x0, int[] x1, int[] fx)
	{
		double num = (double)sw / (double)coverW;
		for (int i = 0; i < w; i++)
		{
			double num2 = ((double)(i - coverX) + 0.5) * num - 0.5;
			int num3 = (int)Math.Floor(num2);
			int num4 = (int)Math.Round((num2 - (double)num3) * 256.0);
			if (num4 >= 256)
			{
				num3++;
				num4 = 0;
			}
			int num5 = Math.Clamp(num3 + 1, 0, sw - 1);
			num3 = Math.Clamp(num3, 0, sw - 1);
			if (num2 < 0.0)
			{
				num3 = (num5 = 0);
				num4 = 0;
			}
			x0[i] = num3;
			x1[i] = num5;
			fx[i] = ((num3 != num5) ? num4 : 0);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static void Widen(Small small, int row, int[] x0, int[] x1, int[] fx, int[] into, int w)
	{
		int[] pixels = small.Pixels;
		int num = row * small.Width;
		for (int i = 0; i < w; i++)
		{
			uint num2 = (uint)pixels[num + x0[i]];
			int num3 = fx[i];
			if (num3 == 0)
			{
				into[i] = (int)num2;
				continue;
			}
			uint num4 = (uint)pixels[num + x1[i]];
			uint num5 = (uint)(256 - num3);
			uint num6 = (uint)(((int)((num2 & 0xFF00FF) * num5) + (int)(num4 & 0xFF00FF) * num3 + 8388736 >>> 8) & 0xFF00FF);
			uint num7 = (uint)(((int)(((num2 >> 8) & 0xFF00FF) * num5) + (int)((num4 >> 8) & 0xFF00FF) * num3 + 8388736 >>> 8) & 0xFF00FF);
			into[i] = (int)(num6 | (num7 << 8));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static void BlendRun(int[] a, int[] b, int[] fy, int run, int[] into, int w)
	{
		if (Avx2.IsSupported)
		{
			BlendRunAvx2(a, b, fy, run, into, w);
			return;
		}
		for (int i = 0; i < run; i++)
		{
			BlendRow(a, b, fy[i], into, i * w, 0, w);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static void BlendRunAvx2(int[] a, int[] b, int[] fy, int run, int[] into, int w)
	{
		Span<ushort> span = stackalloc ushort[run];
		for (int i = 0; i < run; i++)
		{
			int num = fy[i] + 1 >> 1;
			span[i] = (ushort)((num > 0) ? ((num >= 128) ? ushort.MaxValue : ((ushort)((128 - num) | (num << 8)))) : 0);
		}
		ref byte source = ref Unsafe.As<int, byte>(ref MemoryMarshal.GetArrayDataReference(a));
		ref byte source2 = ref Unsafe.As<int, byte>(ref MemoryMarshal.GetArrayDataReference(b));
		ref byte source3 = ref Unsafe.As<int, byte>(ref MemoryMarshal.GetArrayDataReference(into));
		Vector256<short> right = Vector256.Create((short)64);
		int j;
		for (j = 0; j <= w - 8; j += 8)
		{
			Vector256<byte> vector = Unsafe.ReadUnaligned<Vector256<byte>>(in Unsafe.Add(ref source, j * 4));
			Vector256<byte> vector2 = Unsafe.ReadUnaligned<Vector256<byte>>(in Unsafe.Add(ref source2, j * 4));
			Vector256<byte> left = Avx2.UnpackLow(vector, vector2);
			Vector256<byte> left2 = Avx2.UnpackHigh(vector, vector2);
			for (int k = 0; k < run; k++)
			{
				ref byte destination = ref Unsafe.Add(ref source3, (k * w + j) * 4);
				ushort num2 = span[k];
				switch (num2)
				{
				case 0:
					Unsafe.WriteUnaligned(ref destination, vector);
					continue;
				case ushort.MaxValue:
					Unsafe.WriteUnaligned(ref destination, vector2);
					continue;
				}
				Vector256<sbyte> right2 = Vector256.Create(num2).AsSByte();
				Vector256<short> left3 = Avx2.ShiftRightLogical(Avx2.Add(Avx2.MultiplyAddAdjacent(left, right2), right), 7);
				Vector256<short> right3 = Avx2.ShiftRightLogical(Avx2.Add(Avx2.MultiplyAddAdjacent(left2, right2), right), 7);
				Unsafe.WriteUnaligned(ref destination, Avx2.PackUnsignedSaturate(left3, right3));
			}
		}
		if (j < w)
		{
			for (int l = 0; l < run; l++)
			{
				BlendRow(a, b, fy[l], into, l * w, j, w);
			}
		}
	}

	private static void BlendRow(int[] a, int[] b, int f, int[] into, int at, int from, int w)
	{
		uint num = (uint)(256 - f);
		int i = from;
		if (Vector.IsHardwareAccelerated && from == 0)
		{
			Span<uint> span = MemoryMarshal.Cast<int, uint>(a.AsSpan(0, w));
			Span<uint> span2 = MemoryMarshal.Cast<int, uint>(b.AsSpan(0, w));
			Span<uint> span3 = MemoryMarshal.Cast<int, uint>(into.AsSpan(at, w));
			Vector<uint> vector = new Vector<uint>(16711935u);
			Vector<uint> vector2 = new Vector<uint>(8388736u);
			Vector<uint> vector3 = new Vector<uint>(num);
			Vector<uint> vector4 = new Vector<uint>((uint)f);
			for (int count = Vector<uint>.Count; i <= w - count; i += count)
			{
				Vector<uint> vector5 = new Vector<uint>(span.Slice(i, count));
				Vector<uint> vector6 = new Vector<uint>(span2.Slice(i, count));
				Vector<uint> vector7 = Vector.ShiftRightLogical((vector5 & vector) * vector3 + (vector6 & vector) * vector4 + vector2, 8) & vector;
				Vector<uint> value = Vector.ShiftRightLogical((Vector.ShiftRightLogical(vector5, 8) & vector) * vector3 + (Vector.ShiftRightLogical(vector6, 8) & vector) * vector4 + vector2, 8) & vector;
				(vector7 | Vector.ShiftLeft(value, 8)).CopyTo(span3.Slice(i, count));
			}
		}
		for (; i < w; i++)
		{
			int num2 = a[i];
			uint num3 = (uint)b[i];
			uint num4 = (uint)(((num2 & 0xFF00FF) * (int)num + (int)(num3 & 0xFF00FF) * f + 8388736 >>> 8) & 0xFF00FF);
			uint num5 = (uint)((((num2 >>> 8) & 0xFF00FF) * (int)num + (int)((num3 >> 8) & 0xFF00FF) * f + 8388736 >>> 8) & 0xFF00FF);
			into[at + i] = (int)(num4 | (num5 << 8));
		}
	}
}
