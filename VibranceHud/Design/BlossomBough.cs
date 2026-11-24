using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using VibranceHud.Theming;

namespace VibranceHud.Design;

public sealed class BlossomBough : IBackdrop
{
	private readonly record struct Depth(int Divisor, float Scale, int Alpha, int Clusters, int PerCluster, int Seed);

	private Bitmap? _cache;

	private string _cacheKey = "";

	private int _w;

	private int _h;

	private static readonly Depth[] Layers = new Depth[3]
	{
		new Depth(10, 0.34f, 58, 17, 11, 11),
		new Depth(6, 0.44f, 72, 14, 9, 29),
		new Depth(4, 0.62f, 82, 10, 7, 47)
	};

	internal int CacheBuilds { get; private set; }

	public bool Moves => false;

	public void Resize(int w, int h)
	{
		_w = Math.Max(1, w);
		_h = Math.Max(1, h);
	}

	public void Update(double dtSeconds)
	{
	}

	public void Paint(Graphics g, int offsetX, int offsetY)
	{
		if (_w > 1 && _h > 1)
		{
			EnsureCache();
			if (_cache != null)
			{
				g.DrawImageUnscaled(_cache, -offsetX, -offsetY);
			}
		}
	}

	private void EnsureCache()
	{
		string text = Theme.CurrentName + "|" + _w + "x" + _h + (AppBackground.IsSet ? "|img" : "");
		if (_cache != null && _cacheKey == text)
		{
			return;
		}
		_cache?.Dispose();
		_cache = null;
		_cacheKey = text;
		CacheBuilds++;
		using Bitmap bitmap = new Bitmap(_w, _h);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.Clear(Color.Transparent);
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			int strength = ((!AppBackground.IsSet) ? 1 : 3);
			Depth[] layers = Layers;
			foreach (Depth depth in layers)
			{
				DrawLayer(graphics, depth, strength);
			}
		}
		FadeDownward(bitmap);
		_cache = Premultiplied(bitmap);
	}

	private static Bitmap Premultiplied(Bitmap argb)
	{
		Bitmap bitmap = new Bitmap(argb.Width, argb.Height, PixelFormat.Format32bppPArgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.CompositingMode = CompositingMode.SourceCopy;
		graphics.DrawImageUnscaled(argb, 0, 0);
		return bitmap;
	}

	private static void FadeDownward(Bitmap bmp)
	{
		Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
		BitmapData bitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
		try
		{
			int stride = bitmapData.Stride;
			byte[] array = new byte[stride];
			for (int i = 0; i < bmp.Height; i++)
			{
				float num = (float)i / (float)Math.Max(1, bmp.Height);
				float num2;
				if (num <= 0.14f)
				{
					num2 = 1f;
				}
				else
				{
					num2 = ((num >= 0.58f) ? 0f : (1f - (num - 0.14f) / 0.44f));
				}
				if (!(num2 >= 0.999f))
				{
					nint num3 = bitmapData.Scan0 + i * stride;
					Marshal.Copy(num3, array, 0, stride);
					for (int j = 3; j < stride; j += 4)
					{
						array[j] = (byte)((float)(int)array[j] * num2);
					}
					Marshal.Copy(array, 0, num3, stride);
				}
			}
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
		}
	}

	private void DrawLayer(Graphics g, Depth depth, int strength)
	{
		int num = Math.Max(2, _w / depth.Divisor);
		int num2 = Math.Max(2, _h / depth.Divisor);
		using Bitmap image = new Bitmap(num, num2);
		using (Graphics graphics = Graphics.FromImage(image))
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.Clear(Color.Transparent);
			PaintBough(graphics, num, num2, depth, strength);
		}
		using ImageAttributes imageAttributes = new ImageAttributes();
		imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
		g.DrawImage(image, new Rectangle(0, 0, _w, _h), 0, 0, num, num2, GraphicsUnit.Pixel, imageAttributes);
	}

	private static void PaintBough(Graphics g, int w, int h, Depth depth, int strength)
	{
		Random random = new Random(depth.Seed);
		Color petal = Color.FromArgb(depth.Alpha / strength, Theme.PlexusNodeA);
		Color centre = Color.FromArgb(depth.Alpha / (strength * 2), Theme.PlexusNodeB);
		Color color = Color.FromArgb(Math.Min(255, depth.Alpha + 78) / strength, Theme.PlexusLine);
		PointF[][] array = new PointF[2][]
		{
			new PointF[4]
			{
				new PointF(-0.06f, 0.1f),
				new PointF(0.28f, 0.3f),
				new PointF(0.58f, 0.02f),
				new PointF(1.06f, 0.22f)
			},
			new PointF[4]
			{
				new PointF(0.1f, -0.05f),
				new PointF(0.2f, 0.26f),
				new PointF(0.16f, 0.52f),
				new PointF(0.26f, 0.86f)
			}
		};
		float width = Math.Max(1.6f, (float)w * 0.02f * depth.Scale);
		using (Pen pen = new Pen(color, width)
		{
			LineJoin = LineJoin.Round,
			StartCap = LineCap.Round,
			EndCap = LineCap.Round
		})
		{
			PointF[][] array2 = array;
			foreach (PointF[] array3 in array2)
			{
				g.DrawBezier(pen, array3[0].X * (float)w, array3[0].Y * (float)h, array3[1].X * (float)w, array3[1].Y * (float)h, array3[2].X * (float)w, array3[2].Y * (float)h, array3[3].X * (float)w, array3[3].Y * (float)h);
			}
		}
		PointF[] p = array[0];
		float num = Math.Max(2f, (float)w * 0.02f * depth.Scale);
		for (int j = 0; j < depth.Clusters; j++)
		{
			float t = ((float)j + 0.5f) / (float)depth.Clusters;
			PointF pointF = Bezier(p, t);
			float num2 = pointF.X * (float)w + (float)(random.NextDouble() - 0.5) * (float)w * 0.06f;
			float num3 = pointF.Y * (float)h + (float)(random.NextDouble() - 0.5) * (float)h * 0.1f;
			for (int k = 0; k < depth.PerCluster; k++)
			{
				Flower(g, num2 + (float)(random.NextDouble() - 0.5) * num * 3.4f, num3 + (float)(random.NextDouble() - 0.5) * num * 3f, num * (0.7f + (float)random.NextDouble() * 0.6f), petal, centre, (float)random.NextDouble() * 72f);
			}
		}
		for (int l = 0; l < Math.Max(2, depth.Clusters / 3); l++)
		{
			PointF pointF2 = Bezier(array[1], ((float)l + 1f) / ((float)depth.Clusters / 3f + 1f));
			for (int m = 0; m < depth.PerCluster - 1; m++)
			{
				Flower(g, pointF2.X * (float)w + (float)(random.NextDouble() - 0.5) * num * 3.2f, pointF2.Y * (float)h + (float)(random.NextDouble() - 0.5) * num * 2.8f, num * (0.6f + (float)random.NextDouble() * 0.5f), petal, centre, (float)random.NextDouble() * 72f);
			}
		}
	}

	private static PointF Bezier(PointF[] p, float t)
	{
		float num = 1f - t;
		float num2 = num * num * num;
		float num3 = 3f * num * num * t;
		float num4 = 3f * num * t * t;
		float num5 = t * t * t;
		return new PointF(num2 * p[0].X + num3 * p[1].X + num4 * p[2].X + num5 * p[3].X, num2 * p[0].Y + num3 * p[1].Y + num4 * p[2].Y + num5 * p[3].Y);
	}

	private static void Flower(Graphics g, float cx, float cy, float r, Color petal, Color centre, float rotation)
	{
		if (r <= 0.4f)
		{
			return;
		}
		GraphicsState gstate = g.Save();
		g.TranslateTransform(cx, cy);
		g.RotateTransform(rotation);
		using (SolidBrush brush = new SolidBrush(petal))
		{
			for (int i = 0; i < 5; i++)
			{
				GraphicsState gstate2 = g.Save();
				g.RotateTransform((float)i * 72f);
				g.FillEllipse(brush, (0f - r) * 0.55f, (0f - r) * 1.02f, r * 1.1f, r * 0.9f);
				g.Restore(gstate2);
			}
		}
		using (SolidBrush brush2 = new SolidBrush(centre))
		{
			g.FillEllipse(brush2, (0f - r) * 0.26f, (0f - r) * 0.26f, r * 0.52f, r * 0.52f);
		}
		g.Restore(gstate);
	}
}
