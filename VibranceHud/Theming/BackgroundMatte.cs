using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace VibranceHud.Theming;

public static class BackgroundMatte
{
	public static readonly Color Black = Color.FromArgb(255, 0, 0, 0);

	public static readonly Color White = Color.FromArgb(255, 255, 255, 255);

	public static Color Default => Black;

	public static bool NeedsOne(Image? image)
	{
		if (image == null)
		{
			return false;
		}
		PixelFormat pixelFormat = image.PixelFormat;
		if ((pixelFormat & PixelFormat.Indexed) != 0)
		{
			ColorPalette palette;
			try
			{
				palette = image.Palette;
			}
			catch
			{
				return false;
			}
			Color[] entries = palette.Entries;
			foreach (Color color in entries)
			{
				if (color.A < byte.MaxValue)
				{
					return true;
				}
			}
			return false;
		}
		if (!Image.IsAlphaPixelFormat(pixelFormat))
		{
			return false;
		}
		return AnyPixelSeesThrough(image);
	}

	public static bool NeedsOne(string path)
	{
		try
		{
			using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
			using Image image = Image.FromStream(stream);
			return NeedsOne(image);
		}
		catch
		{
			return false;
		}
	}

	private static bool AnyPixelSeesThrough(Image image)
	{
		Bitmap bitmap = null;
		try
		{
			Bitmap bitmap2 = image as Bitmap;
			if (bitmap2 == null || bitmap2.PixelFormat != PixelFormat.Format32bppArgb)
			{
				bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					graphics.CompositingMode = CompositingMode.SourceCopy;
					graphics.DrawImage(image, 0, 0, image.Width, image.Height);
				}
				bitmap2 = bitmap;
			}
			Rectangle rect = new Rectangle(0, 0, bitmap2.Width, bitmap2.Height);
			BitmapData bitmapData = bitmap2.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			try
			{
				byte[] array = new byte[bitmapData.Stride];
				for (int i = 0; i < bitmapData.Height; i++)
				{
					Marshal.Copy(bitmapData.Scan0 + i * bitmapData.Stride, array, 0, bitmapData.Stride);
					for (int j = 3; j < bitmapData.Width * 4; j += 4)
					{
						if (array[j] < byte.MaxValue)
						{
							return true;
						}
					}
				}
			}
			finally
			{
				bitmap2.UnlockBits(bitmapData);
			}
			return false;
		}
		catch
		{
			return false;
		}
		finally
		{
			bitmap?.Dispose();
		}
	}
}
