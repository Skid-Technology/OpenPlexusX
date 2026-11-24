using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace VibranceHud.Capture;

public static class ColourScreenshot
{
	public static string Folder => AppFolders.Pictures;

	public static void ApplyLook(Bitmap bmp, float[]? matrix, ushort[]? ramp)
	{
		if (bmp == null)
		{
			throw new ArgumentNullException("bmp");
		}
		if (matrix == null && ramp == null)
		{
			return;
		}
		byte[] array = null;
		if (ramp != null)
		{
			array = new byte[768];
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 256; j++)
				{
					array[i * 256 + j] = (byte)(ramp[i * 256 + j] >> 8);
				}
			}
		}
		Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
		BitmapData bitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
		try
		{
			int num = Math.Abs(bitmapData.Stride) * bmp.Height;
			byte[] array2 = new byte[num];
			Marshal.Copy(bitmapData.Scan0, array2, 0, num);
			for (int k = 0; k < num; k += 4)
			{
				float num2 = (float)(int)array2[k] / 255f;
				float num3 = (float)(int)array2[k + 1] / 255f;
				float num4 = (float)(int)array2[k + 2] / 255f;
				if (matrix != null)
				{
					float num5 = num4 * matrix[0] + num3 * matrix[5] + num2 * matrix[10] + matrix[20];
					float num6 = num4 * matrix[1] + num3 * matrix[6] + num2 * matrix[11] + matrix[21];
					float num7 = num4 * matrix[2] + num3 * matrix[7] + num2 * matrix[12] + matrix[22];
					num4 = num5;
					num3 = num6;
					num2 = num7;
				}
				byte b = Clamp(num4);
				byte b2 = Clamp(num3);
				byte b3 = Clamp(num2);
				if (array != null)
				{
					b = array[b];
					b2 = array[256 + b2];
					b3 = array[512 + b3];
				}
				array2[k] = b3;
				array2[k + 1] = b2;
				array2[k + 2] = b;
			}
			Marshal.Copy(array2, 0, bitmapData.Scan0, num);
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private static byte Clamp(float v)
	{
		if (!(v <= 0f))
		{
			if (!(v >= 1f))
			{
				return (byte)(v * 255f + 0.5f);
			}
			return byte.MaxValue;
		}
		return 0;
	}

	public static Bitmap Grab()
	{
		Rectangle virtualScreen = SystemInformation.VirtualScreen;
		Bitmap bitmap = new Bitmap(virtualScreen.Width, virtualScreen.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.CopyFromScreen(virtualScreen.Left, virtualScreen.Top, 0, 0, virtualScreen.Size, CopyPixelOperation.SourceCopy);
		return bitmap;
	}

	public static string FileNameFor(DateTime when)
	{
		return $"PlexusX-{when:yyyy-MM-dd-HHmmss}.png";
	}

	internal static string UniquePath(string folder, DateTime now)
	{
		string text = Path.Combine(folder, FileNameFor(now));
		int num = 2;
		while (File.Exists(text) && num < 100)
		{
			text = Path.Combine(folder, Path.GetFileNameWithoutExtension(FileNameFor(now)) + " (" + num + ").png");
			num++;
		}
		return text;
	}

	public static string Take(VibranceEngine engine, DateTime now)
	{
		if (engine == null)
		{
			throw new ArgumentNullException("engine");
		}
		using Bitmap bitmap = Grab();
		ApplyLook(bitmap, engine.CurrentMatrix(), engine.CurrentRamp());
		Directory.CreateDirectory(Folder);
		string text = UniquePath(Folder, now);
		bitmap.Save(text, ImageFormat.Png);
		try
		{
			Clipboard.SetImage(bitmap);
		}
		catch (ExternalException)
		{
		}
		catch (ThreadStateException)
		{
		}
		return text;
	}
}
