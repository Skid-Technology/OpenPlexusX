using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace VibranceHud.Crosshair;

public static class CrosshairRender
{
	private static readonly int ClassicOutlineArgb = -1107296256;

	public static void Draw(Graphics g, CrosshairConfig config)
	{
		Bitmap bitmap = ImageCrosshair.Load(config.ImagePath);
		if (bitmap != null)
		{
			DrawImage(g, config, bitmap);
			return;
		}
		CrosshairShapes shapes = CrosshairGeometry.Build(config);
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Color color = Color.FromArgb(config.ResolvedColourArgb);
		SolidBrush fill = new SolidBrush(color);
		try
		{
			int resolvedOutlineColourArgb = config.ResolvedOutlineColourArgb;
			Color color2 = Color.FromArgb(resolvedOutlineColourArgb);
			SolidBrush halo = new SolidBrush(color2);
			try
			{
				bool hasDot = shapes.DotIndex >= 0;
				bool fadedDot = hasDot && config.ResolvedDotOpacity < 100;
				SolidBrush dotFill = new SolidBrush(Color.FromArgb(config.ResolvedDotColourArgb));
				try
				{
					SolidBrush dotHalo = new SolidBrush(Color.FromArgb(color2.A * config.ResolvedDotOpacity / 100, color2.R, color2.G, color2.B));
					try
					{
						float outline = config.ResolvedOutlineThickness;
						float num = Math.Max(0.5f, config.ResolvedThickness);
						if (outline > 0f)
						{
							bool oneShape = resolvedOutlineColourArgb != ClassicOutlineArgb || shapes.Angled.Count > 0;
							GraphicsPath armsOutline = ((oneShape | fadedDot) ? Arms(shapes, outline, oneShape && !fadedDot) : null);
							try
							{
								Turned(g, shapes.Pivot, shapes.RotationDegrees, () =>
								{
									if (oneShape)
									{
										g.FillPath(halo, armsOutline);
									}
									else
									{
										for (int i = 0; i < shapes.Bars.Count; i++)
										{
											if (i != shapes.DotIndex)
											{
												g.FillRectangle(halo, RectangleF.Inflate(shapes.Bars[i], outline, outline));
											}
										}
									}
									if (hasDot)
									{
										RectangleF rect = RectangleF.Inflate(shapes.Bars[shapes.DotIndex], outline, outline);
										if (fadedDot)
										{
											GraphicsState gstate = g.Save();
											g.SetClip(armsOutline, CombineMode.Exclude);
											g.FillRectangle(dotHalo, rect);
											g.Restore(gstate);
										}
										else if (!oneShape)
										{
											g.FillRectangle(halo, rect);
										}
									}
								});
								RectangleF? circle = shapes.Circle;
								if (circle.HasValue)
								{
									RectangleF valueOrDefault = circle.GetValueOrDefault();
									using Pen pen = new Pen(color2, num + outline * 2f);
									g.DrawEllipse(pen, valueOrDefault);
								}
							}
							finally
							{
								if (armsOutline != null)
								{
									((IDisposable)armsOutline).Dispose();
								}
							}
						}
						GraphicsPath armsFill = Arms(shapes, 0f, withDot: false);
						try
						{
							Turned(g, shapes.Pivot, shapes.RotationDegrees, () =>
							{
								g.FillPath(fill, armsFill);
								if (hasDot)
								{
									g.FillRectangle(dotFill, shapes.Bars[shapes.DotIndex]);
								}
							});
							RectangleF? circle = shapes.Circle;
							if (circle.HasValue)
							{
								RectangleF valueOrDefault2 = circle.GetValueOrDefault();
								using Pen pen2 = new Pen(color, num);
								g.DrawEllipse(pen2, valueOrDefault2);
								return;
							}
						}
						finally
						{
							if (armsFill != null)
							{
								((IDisposable)armsFill).Dispose();
							}
						}
					}
					finally
					{
						if (dotHalo != null)
						{
							((IDisposable)dotHalo).Dispose();
						}
					}
				}
				finally
				{
					if (dotFill != null)
					{
						((IDisposable)dotFill).Dispose();
					}
				}
			}
			finally
			{
				if (halo != null)
				{
					((IDisposable)halo).Dispose();
				}
			}
		}
		finally
		{
			if (fill != null)
			{
				((IDisposable)fill).Dispose();
			}
		}
	}

	private static GraphicsPath Arms(CrosshairShapes shapes, float grow, bool withDot)
	{
		GraphicsPath graphicsPath = new GraphicsPath(FillMode.Winding);
		for (int i = 0; i < shapes.Bars.Count; i++)
		{
			if (i != shapes.DotIndex || withDot)
			{
				graphicsPath.AddRectangle((grow > 0f) ? RectangleF.Inflate(shapes.Bars[i], grow, grow) : shapes.Bars[i]);
			}
		}
		foreach (AngledBar item in shapes.Angled)
		{
			using GraphicsPath graphicsPath2 = new GraphicsPath(FillMode.Winding);
			graphicsPath2.AddRectangle((grow > 0f) ? RectangleF.Inflate(item.Rect, grow, grow) : item.Rect);
			using Matrix matrix = new Matrix();
			matrix.RotateAt(item.Degrees, shapes.Pivot);
			graphicsPath2.Transform(matrix);
			graphicsPath.AddPath(graphicsPath2, connect: false);
		}
		return graphicsPath;
	}

	private static void Turned(Graphics g, PointF pivot, float degrees, Action draw)
	{
		if (degrees == 0f)
		{
			draw();
			return;
		}
		GraphicsState gstate = g.Save();
		int num;
		if (pivot.X == 0f)
		{
			num = ((pivot.Y != 0f) ? 1 : 0);
			if (num == 0)
			{
				goto IL_004f;
			}
		}
		else
		{
			num = 1;
		}
		g.TranslateTransform(pivot.X, pivot.Y);
		goto IL_004f;
		IL_004f:
		g.RotateTransform(degrees);
		if (num != 0)
		{
			g.TranslateTransform(0f - pivot.X, 0f - pivot.Y);
		}
		draw();
		g.Restore(gstate);
	}

	private static void DrawImage(Graphics g, CrosshairConfig config, Image image)
	{
		float num = (float)Math.Max(1, config.ImageScalePercent) / 100f;
		float num2 = (float)image.Width * num;
		float num3 = (float)image.Height * num;
		float num4 = config.ResolvedOffsetX;
		float num5 = config.ResolvedOffsetY;
		InterpolationMode interpolationMode = g.InterpolationMode;
		PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
		g.InterpolationMode = InterpolationMode.NearestNeighbor;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		int num6 = Math.Clamp(config.Opacity, 0, 100);
		if (num6 >= 100)
		{
			g.DrawImage(image, (0f - num2) / 2f + num4, (0f - num3) / 2f + num5, num2, num3);
		}
		else
		{
			using ImageAttributes imageAttributes = new ImageAttributes();
			ColorMatrix colorMatrix = new ColorMatrix
			{
				Matrix33 = (float)num6 / 100f
			};
			imageAttributes.SetColorMatrix(colorMatrix);
			g.DrawImage(image, new Rectangle((int)((0f - num2) / 2f) + (int)num4, (int)((0f - num3) / 2f) + (int)num5, (int)num2, (int)num3), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
		}
		g.InterpolationMode = interpolationMode;
		g.PixelOffsetMode = pixelOffsetMode;
	}

	public static void Draw(Graphics g, CrosshairConfig config, Rectangle target)
	{
		Draw(g, config, target, 1f);
	}

	public static void Draw(Graphics g, CrosshairConfig config, Rectangle target, float maxScale)
	{
		RectangleF bounds = CrosshairGeometry.Build(config).Bounds;
		float num = maxScale;
		if (bounds.Width > 0f && bounds.Height > 0f)
		{
			num = Math.Min(maxScale, 0.82f * Math.Min((float)target.Width / bounds.Width, (float)target.Height / bounds.Height));
		}
		GraphicsState gstate = g.Save();
		g.SetClip(target);
		g.TranslateTransform((float)target.X + (float)target.Width / 2f, (float)target.Y + (float)target.Height / 2f);
		g.ScaleTransform(num, num);
		Draw(g, config);
		g.Restore(gstate);
	}

	public static void DrawCheckerboard(Graphics g, Rectangle rect, int cell)
	{
		using Region clip = g.Clip;
		g.SetClip(rect, CombineMode.Intersect);
		using (SolidBrush solidBrush = new SolidBrush(Color.FromArgb(255, 58, 58, 64)))
		{
			using SolidBrush solidBrush2 = new SolidBrush(Color.FromArgb(255, 78, 78, 86));
			for (int i = rect.Top; i < rect.Bottom; i += cell)
			{
				for (int j = rect.Left; j < rect.Right; j += cell)
				{
					g.FillRectangle((((j - rect.Left) / cell + (i - rect.Top) / cell) % 2 == 0) ? solidBrush : solidBrush2, j, i, cell, cell);
				}
			}
		}
		g.Clip = clip;
	}
}
