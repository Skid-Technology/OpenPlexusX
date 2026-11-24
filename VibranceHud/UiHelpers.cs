using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

internal static class UiHelpers
{
	private const float DisabledFade = 0.62f;

	public static Color Muted(Color colour)
	{
		return Blend(colour, Theme.Background, 0.62f);
	}

	public static Color Blend(Color from, Color to, float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return Color.FromArgb(from.A, (int)Math.Round((float)(int)from.R + (float)(to.R - from.R) * t), (int)Math.Round((float)(int)from.G + (float)(to.G - from.G) * t), (int)Math.Round((float)(int)from.B + (float)(to.B - from.B) * t));
	}

	public static Color ReadableOn(Color background)
	{
		double a = RelativeLuminance(background);
		if (!(Contrast(a, RelativeLuminance(Theme.Text)) >= Contrast(a, RelativeLuminance(Theme.Background))))
		{
			return Theme.Background;
		}
		return Theme.Text;
	}

	public static double Contrast(double a, double b)
	{
		return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
	}

	public static double RelativeLuminance(Color c)
	{
		return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
		static double Channel(int v)
		{
			double num = (double)v / 255.0;
			if (!(num <= 0.04045))
			{
				return Math.Pow((num + 0.055) / 1.055, 2.4);
			}
			return num / 12.92;
		}
	}

	public static void DrawFocusRing(Graphics g, Rectangle bounds, float radius)
	{
		Rectangle rectangle = Rectangle.Inflate(bounds, -2, -2);
		if (rectangle.Width <= 0 || rectangle.Height <= 0)
		{
			return;
		}
		using Pen pen = new Pen(Theme.Accent, 1.4f)
		{
			DashStyle = DashStyle.Dot
		};
		using GraphicsPath path = Glass.RoundedPath(new RectangleF(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height), radius);
		g.DrawPath(pen, path);
	}

	public static string Spaced(string text)
	{
		return text;
	}

	public static Label Caption(string text, int x, int y, int width, ContentAlignment align = ContentAlignment.MiddleLeft)
	{
		return new Label
		{
			Text = Spaced(text),
			ForeColor = Theme.TextDim,
			Font = Fonts.CaptionBold,
			Location = new Point(x, y),
			Size = new Size(width, Fonts.Step(FontRole.CaptionBold).LinePx),
			TextAlign = align,
			BackColor = Color.Transparent,
			UseMnemonic = false
		};
	}
}
