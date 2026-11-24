using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Theming;

namespace VibranceHud;

public static class Glass
{
	private const float SoftRadiusScale = 1.45f;

	public static SurfaceStyle Style { get; set; } = SurfaceStyle.Glass;

	public static Chrome ActiveChrome { get; set; } = Chrome.Plexus;

	private static bool Flat => Style == SurfaceStyle.Flat;

	private static bool Soft => ActiveChrome == Chrome.Blossom;

	internal static int SlateSurfacesDrawn { get; private set; }

	internal static int SlateOutlinesTraced { get; private set; }

	internal static bool SlateInteriorShortcutForTests { get; set; } = true;

	public static Point BackdropOffsetOf(Control? control)
	{
		int num = 0;
		int num2 = 0;
		Control control2 = control;
		while (control2 != null && !(control2 is Form))
		{
			num += control2.Left;
			num2 += control2.Top;
			control2 = control2.Parent;
		}
		return new Point(num, num2);
	}

	public static GraphicsPath RoundedPath(RectangleF rect, float radius)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		if (rect.Width <= 0f || rect.Height <= 0f)
		{
			return graphicsPath;
		}
		if (Flat)
		{
			radius = 0f;
		}
		radius = Math.Max(0f, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f));
		if (radius <= 0.01f)
		{
			graphicsPath.AddRectangle(rect);
			return graphicsPath;
		}
		float num = radius * 2f;
		graphicsPath.AddArc(rect.X, rect.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(rect.Right - num, rect.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(rect.Right - num, rect.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(rect.X, rect.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	private static Color Lift(Color c, float k)
	{
		if (!(k >= 0f))
		{
			return Color.FromArgb((int)((float)(int)c.R * (1f + k)), (int)((float)(int)c.G * (1f + k)), (int)((float)(int)c.B * (1f + k)));
		}
		return Color.FromArgb((int)((float)(int)c.R + (float)(255 - c.R) * k), (int)((float)(int)c.G + (float)(255 - c.G) * k), (int)((float)(int)c.B + (float)(255 - c.B) * k));
	}

	public static void PaintPanel(Graphics g, RectangleF rect, float radius, int fillAlpha = 140, int rimAlpha = 105, bool exactAlpha = false)
	{
		PaintPanel(g, rect, radius, Point.Empty, fillAlpha, rimAlpha, exactAlpha);
	}

	public static void PaintPanel(Graphics g, RectangleF rect, float radius, Point backdropOffset, int fillAlpha = 140, int rimAlpha = 105, bool exactAlpha = false)
	{
		if (rect.Width < 2f || rect.Height < 2f)
		{
			return;
		}
		if (SlateSurface.Active)
		{
			PaintSlateSurface(g, rect, radius, backdropOffset, SlateSurface.PanelAlpha);
			return;
		}
		int alpha = (exactAlpha ? fillAlpha : Math.Min(255, fillAlpha + 74));
		if (Soft)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			float num = radius * 1.45f;
			using GraphicsPath path = RoundedPath(rect, num);
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, Theme.Surface)))
			{
				g.FillPath(brush, path);
			}
			using (Pen pen = new Pen(Color.FromArgb(Math.Min(255, rimAlpha), Theme.Border), 1.6f))
			{
				g.DrawPath(pen, path);
			}
			RectangleF rect2 = RectangleF.Inflate(rect, -1.6f, -1.6f);
			if (!(rect2.Width > 2f) || !(rect2.Height > 2f))
			{
				return;
			}
			using GraphicsPath path2 = RoundedPath(rect2, Math.Max(1f, num - 1.6f));
			using Pen pen2 = new Pen(Color.FromArgb(Math.Min(255, rimAlpha + 40), Theme.GlassEdge), 1f);
			g.DrawPath(pen2, path2);
			return;
		}
		g.SmoothingMode = SmoothingMode.AntiAlias;
		using GraphicsPath path3 = RoundedPath(rect, radius);
		if (Flat)
		{
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(fillAlpha, Theme.GlassFill));
			g.FillPath(brush2, path3);
		}
		else
		{
			using LinearGradientBrush brush3 = new LinearGradientBrush(new RectangleF(rect.X, rect.Y - 1f, rect.Width, rect.Height + 2f), Color.FromArgb(Math.Min(255, fillAlpha + 16), Lift(Theme.GlassFill, 0.05f)), Color.FromArgb(fillAlpha, Theme.GlassFill), LinearGradientMode.Vertical)
			{
				WrapMode = WrapMode.TileFlipXY
			};
			g.FillPath(brush3, path3);
		}
		using (Pen pen3 = new Pen(Color.FromArgb(rimAlpha, Theme.GlassEdge), 1.2f))
		{
			g.DrawPath(pen3, path3);
		}
		if (!Flat && rect.Width > radius * 2f + 4f)
		{
			using (Pen pen4 = new Pen(Color.FromArgb(Theme.IsLight ? 90 : 42, 255, 255, 255), 1f))
			{
				g.DrawLine(pen4, rect.X + radius, rect.Y + 1.5f, rect.Right - radius, rect.Y + 1.5f);
				return;
			}
		}
	}

	public static void PaintChrome(Graphics g, RectangleF rect, float radius, Point backdropOffset, int fillAlpha = 140, int rimAlpha = 105)
	{
		if (!(rect.Width < 2f) && !(rect.Height < 2f))
		{
			if (SlateSurface.Active)
			{
				PaintSlateChrome(g, Rectangle.Round(rect), backdropOffset, SlateSurface.ChromeAlpha);
			}
			else
			{
				PaintPanel(g, rect, radius, backdropOffset, fillAlpha, rimAlpha);
			}
		}
	}

	private static void PaintSlateChrome(Graphics g, Rectangle rect, Point backdropOffset, int alpha)
	{
		SmoothingMode smoothingMode = g.SmoothingMode;
		g.SmoothingMode = SmoothingMode.None;
		try
		{
			if (SlateSurface.MatteBlur && AppBackground.IsSet)
			{
				using GraphicsPath graphicsPath = new GraphicsPath();
				graphicsPath.AddRectangle(rect);
				AppBackground.FillWithBlurred(g, graphicsPath, backdropOffset.X, backdropOffset.Y);
			}
			using SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, SlateSurface.Base));
			g.FillRectangle(brush, rect);
		}
		finally
		{
			g.SmoothingMode = smoothingMode;
		}
	}

	private static void PaintSlateSurface(Graphics g, RectangleF rect, float radius, Point backdropOffset, int alpha)
	{
		SlateSurfacesDrawn++;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		if (SlateInteriorShortcutForTests)
		{
			RectangleF? rectangleF = PlainInsideToFill(g, rect, radius);
			if (rectangleF.HasValue)
			{
				RectangleF valueOrDefault = rectangleF.GetValueOrDefault();
				if (SlateSurface.MatteBlur && AppBackground.IsSet)
				{
					AppBackground.FillWithBlurred(g, valueOrDefault, backdropOffset.X, backdropOffset.Y);
				}
				using SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, SlateSurface.Base));
				g.FillRectangle(brush, valueOrDefault);
				return;
			}
		}
		SlateOutlinesTraced++;
		using GraphicsPath path = RoundedPath(rect, radius);
		if (SlateSurface.MatteBlur && AppBackground.IsSet)
		{
			AppBackground.FillWithBlurred(g, path, backdropOffset.X, backdropOffset.Y);
		}
		using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(alpha, SlateSurface.Base)))
		{
			g.FillPath(brush2, path);
		}
		using Pen pen = new Pen(SignalTokens.Slate.Border, 1f);
		g.DrawPath(pen, path);
	}

	private static RectangleF? PlainInsideToFill(Graphics g, RectangleF rect, float radius)
	{
		GraphicsUnit pageUnit = g.PageUnit;
		bool flag = (uint)(pageUnit - 1) <= 1u;
		if (!flag || g.PageScale != 1f)
		{
			return null;
		}
		Matrix3x2 transformElements = g.TransformElements;
		if (transformElements.M11 != 1f || transformElements.M12 != 0f || transformElements.M21 != 0f || transformElements.M22 != 1f)
		{
			return null;
		}
		if (Flat)
		{
			radius = 0f;
		}
		radius = Math.Max(0f, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f));
		RectangleF visibleClipBounds = g.VisibleClipBounds;
		if (visibleClipBounds.Width <= 0f || visibleClipBounds.Height <= 0f)
		{
			return null;
		}
		RectangleF rectangleF = RectangleF.FromLTRB(MathF.Floor(visibleClipBounds.Left) - 1f, MathF.Floor(visibleClipBounds.Top) - 1f, MathF.Ceiling(visibleClipBounds.Right) + 1f, MathF.Ceiling(visibleClipBounds.Bottom) + 1f);
		float num = radius + 2f;
		RectangleF rectangleF2 = RectangleF.FromLTRB(rect.Left + num, rect.Top + num, rect.Right - num, rect.Bottom - num);
		if (!(rectangleF2.Width > 0f) || !(rectangleF2.Height > 0f) || !rectangleF2.Contains(rectangleF))
		{
			return null;
		}
		return rectangleF;
	}

	public static void PaintAccent(Graphics g, RectangleF rect, float radius, Color accent, int alpha = 210)
	{
		if (rect.Width < 2f || rect.Height < 2f)
		{
			return;
		}
		if (SlateSurface.Active)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			using GraphicsPath path = RoundedPath(rect, radius);
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, accent)))
			{
				g.FillPath(brush, path);
			}
			using Pen pen = new Pen(SignalTokens.Slate.Border, 1f);
			g.DrawPath(pen, path);
			return;
		}
		if (Soft)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			using GraphicsPath path2 = RoundedPath(rect, radius * 1.45f);
			using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(alpha, accent)))
			{
				g.FillPath(brush2, path2);
			}
			using Pen pen2 = new Pen(Color.FromArgb(200, accent), 1f);
			g.DrawPath(pen2, path2);
			return;
		}
		g.SmoothingMode = SmoothingMode.AntiAlias;
		using GraphicsPath path3 = RoundedPath(rect, radius);
		if (Flat)
		{
			using SolidBrush brush3 = new SolidBrush(Color.FromArgb(alpha, accent));
			g.FillPath(brush3, path3);
		}
		else
		{
			using LinearGradientBrush brush4 = new LinearGradientBrush(new RectangleF(rect.X, rect.Y - 1f, rect.Width, rect.Height + 2f), Color.FromArgb(alpha, Lift(accent, 0.12f)), Color.FromArgb(alpha, accent), LinearGradientMode.Vertical)
			{
				WrapMode = WrapMode.TileFlipXY
			};
			g.FillPath(brush4, path3);
		}
		using (Pen pen3 = new Pen(Color.FromArgb(120, Theme.GlassEdge), 1.2f))
		{
			g.DrawPath(pen3, path3);
		}
		if (!Flat && rect.Width > radius * 2f + 4f)
		{
			using (Pen pen4 = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
			{
				g.DrawLine(pen4, rect.X + radius, rect.Y + 1.5f, rect.Right - radius, rect.Y + 1.5f);
				return;
			}
		}
	}
}
