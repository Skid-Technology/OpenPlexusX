using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

internal static class ControlInk
{
	internal const double LockedOpacity = 0.38;

	private static bool Slate => SlateSurface.Active;

	internal static Color Accent
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Accent;
			}
			return SignalTokens.Slate.Accent;
		}
	}

	internal static Color AccentHover
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.AccentHover;
			}
			return SignalTokens.Slate.AccentHover;
		}
	}

	internal static Color AccentPressed
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.AccentPressed;
			}
			return SignalTokens.Slate.AccentPressed;
		}
	}

	internal static Color OnAccent
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.OnAccent;
			}
			return SignalTokens.Slate.OnAccent;
		}
	}

	internal static Color Hover
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Hover;
			}
			return SignalTokens.Slate.Hover;
		}
	}

	internal static Color Pressed
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Pressed;
			}
			return SignalTokens.Slate.Pressed;
		}
	}

	internal static Color Surface2
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Surface2;
			}
			return SignalTokens.Slate.Surface2;
		}
	}

	internal static Color FieldFill
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.FieldFill;
			}
			return SignalTokens.Slate.FieldFill;
		}
	}

	internal static Color Overlay
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Surface3;
			}
			return SignalTokens.Slate.SlateBase;
		}
	}

	internal static Color Edge
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.BorderStrong;
			}
			return SignalTokens.Slate.BorderStrong;
		}
	}

	internal static Color Border
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Border;
			}
			return SignalTokens.Slate.Border;
		}
	}

	internal static Color WindowEdge
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.WindowEdge;
			}
			return SignalTokens.Slate.WindowEdge;
		}
	}

	internal static Color Track
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Track;
			}
			return SignalTokens.Slate.Track;
		}
	}

	internal static Color Focus
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Focus;
			}
			return SignalTokens.Slate.Focus;
		}
	}

	internal static Color KnobRing
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Surface1;
			}
			return SignalTokens.Slate.KnobRing;
		}
	}

	internal static Color Text1 => Theme.Text;

	internal static Color Text2 => Theme.TextDim;

	internal static Color Text3 => Theme.TextFaint;

	internal static Color TextDisabled
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.TextDisabled;
			}
			return SignalTokens.Slate.TextDisabled;
		}
	}

	internal static Color TextDisabledInk
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.TextDisabled;
			}
			return Over(SignalTokens.Slate.TextDisabled, SignalTokens.Slate.SlateBase);
		}
	}

	internal static Color Danger
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Danger;
			}
			return SignalTokens.Slate.Danger;
		}
	}

	internal static Color Success
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Success;
			}
			return SignalTokens.Slate.Success;
		}
	}

	internal static Color Warning
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Warning;
			}
			return SignalTokens.Slate.Warning;
		}
	}

	internal static Color Info
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.Info;
			}
			return SignalTokens.Slate.Info;
		}
	}

	internal static Color SelectedSuffix => SignalTokens.Root.PInk2;

	internal static Color Locked(Color ink, Color ground)
	{
		int num = (int)Math.Round(0.299 * (double)(int)ink.R + 0.587 * (double)(int)ink.G + 0.114 * (double)(int)ink.B);
		return Over(Color.FromArgb((int)Math.Round(96.9), num, num, num), Color.FromArgb(255, ground));
	}

	internal static Color Over(Color top, Color ground)
	{
		return Color.FromArgb(255, ground.R + top.A * (top.R - ground.R) / 255, ground.G + top.A * (top.G - ground.G) / 255, ground.B + top.A * (top.B - ground.B) / 255);
	}

	internal static int Dpi(Control c)
	{
		return Math.Max((c.DeviceDpi > 0) ? c.DeviceDpi : 96, Tokens.Dpi);
	}

	internal static int LabelInkInset(Font font)
	{
		int width = TextRenderer.MeasureText("A", font).Width;
		int width2 = TextRenderer.MeasureText("A", font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
		return Math.Max(0, (width - width2 + 2) / 4);
	}

	internal static int Px(int dpi, int logical)
	{
		return Tokens.ScaleAt(dpi, logical);
	}

	internal static float PxF(int dpi, float logical)
	{
		return logical * (float)dpi / 96f;
	}

	internal static int Hairline(int dpi)
	{
		return Math.Max(1, dpi / 96);
	}

	internal static Color Mix(Color a, Color b, float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		if (t <= 0f)
		{
			return a;
		}
		if (t >= 1f)
		{
			return b;
		}
		return Color.FromArgb((int)Math.Round((float)(int)a.A + (float)(b.A - a.A) * t), (int)Math.Round((float)(int)a.R + (float)(b.R - a.R) * t), (int)Math.Round((float)(int)a.G + (float)(b.G - a.G) * t), (int)Math.Round((float)(int)a.B + (float)(b.B - a.B) * t));
	}

	internal static void PaintBox(Graphics g, Size size, float radius, int line, Color fill, Color edge)
	{
		if (size.Width < 2 || size.Height < 2)
		{
			return;
		}
		SmoothingMode smoothingMode = g.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		try
		{
			bool flag = edge.A > 0;
			if (fill.A > 0)
			{
				float num = (flag ? ((float)line / 2f) : 0f);
				using GraphicsPath path = Glass.RoundedPath(new RectangleF(num, num, (float)size.Width - 2f * num, (float)size.Height - 2f * num), Math.Max(0f, radius - num));
				using SolidBrush brush = new SolidBrush(fill);
				g.FillPath(brush, path);
			}
			if (!flag)
			{
				return;
			}
			float num2 = (float)line / 2f;
			using GraphicsPath path2 = Glass.RoundedPath(new RectangleF(num2, num2, size.Width - line, size.Height - line), Math.Max(0f, radius - num2));
			using Pen pen = new Pen(edge, line);
			g.DrawPath(pen, path2);
		}
		finally
		{
			g.SmoothingMode = smoothingMode;
			g.PixelOffsetMode = pixelOffsetMode;
		}
	}

	internal static void PaintFocusRing(Graphics g, Rectangle bounds, float radius, int dpi)
	{
		int num = Px(dpi, 2);
		float num2 = (float)Px(dpi, 2) + (float)num / 2f;
		SmoothingMode smoothingMode = g.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		try
		{
			using GraphicsPath path = Glass.RoundedPath(new RectangleF((float)bounds.X - num2, (float)bounds.Y - num2, (float)bounds.Width + 2f * num2, (float)bounds.Height + 2f * num2), radius + num2);
			using Pen pen = new Pen(Focus, num);
			g.DrawPath(pen, path);
		}
		finally
		{
			g.SmoothingMode = smoothingMode;
			g.PixelOffsetMode = pixelOffsetMode;
		}
	}

	internal static int FocusRingReach(int dpi)
	{
		return Px(dpi, 2) + Px(dpi, 2) + 1;
	}
}
