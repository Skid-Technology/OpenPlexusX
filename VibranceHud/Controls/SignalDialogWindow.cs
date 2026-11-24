using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public abstract class SignalDialogWindow : Form
{
	public static class Measure
	{
		public const int Radius = 8;

		public const int Pad = 24;

		public const int TitleY = 23;

		public const int BodyY = 60;

		public const int BodyToContent = 16;

		public const int ContentToFooter = 24;

		public const int ButtonMinWidth = 96;

		public const int ButtonPadX = 12;

		public const int ButtonGap = 8;

		public const int FooterBottom = 20;

		public const int ButtonPad = 20;

		public const int WidthSm = 400;

		public const int WidthMd = 460;

		public const int WidthLg = 560;
	}

	private readonly int _dpi;

	protected int ChromeDpi => _dpi;

	protected string? ChromeTitle { get; set; }

	protected string? ChromeBody { get; set; }

	protected int ChromeBodyHeight { get; set; }

	protected int TextWidth => ClientSize.Width - 2 * S(24);

	protected int BodyTop => S(60);

	protected int ContentTop => BodyTop + ChromeBodyHeight + S(16);

	protected virtual bool Square => false;

	internal static Color EdgeInk
	{
		get
		{
			if (!SlateSurface.Active)
			{
				return SignalTokens.Ash.WindowEdge;
			}
			return ControlInk.Over(SignalTokens.Slate.WindowEdge, ControlInk.Overlay);
		}
	}

	protected SignalDialogWindow()
	{
		_dpi = SignalInk.DpiFor(this);
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		BackColor = Theme.Background;
		DoubleBuffered = true;
		Icon = AppIcon.Value;
		KeyPreview = true;
		Font = Fonts.Body;
		AccessibleRole = AccessibleRole.Dialog;
		Opacity = 0.99;
		Load += (object? _, EventArgs _) =>
		{
			ApplyRegion();
		};
		Resize += (object? _, EventArgs _) =>
		{
			ApplyRegion();
		};
	}

	protected int S(int logical)
	{
		return Tokens.ScaleAt(_dpi, logical);
	}

	internal static int MeasureBody(string? body, int textWidth)
	{
		int val = Fonts.LineHeight(FontRole.Body);
		if (string.IsNullOrEmpty(body))
		{
			return 0;
		}
		int height = TextRenderer.MeasureText(body, Fonts.Body, new Size(Math.Max(1, textWidth), 0), TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak).Height;
		return Math.Max(val, height);
	}

	internal static Label TitleLabel(string text, int inkLeft, int top, int width)
	{
		return new Label
		{
			Text = text,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Font = Fonts.Title,
			AutoSize = false,
			TextAlign = ContentAlignment.TopLeft,
			UseMnemonic = false,
			Location = new Point(Ink(inkLeft, Fonts.Title), top),
			Size = new Size(width, Fonts.LineHeight(FontRole.Title))
		};
	}

	internal static Label BodyLabel(string text, int inkLeft, int top, int width, int height)
	{
		return new Label
		{
			Text = text,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			AutoSize = false,
			TextAlign = ContentAlignment.TopLeft,
			UseMnemonic = false,
			Location = new Point(Ink(inkLeft, Fonts.Body), top),
			Size = new Size(width, height)
		};
	}

	internal static int TwinButtonWidth(string text, Font font, int scaledPad, int scaledMin)
	{
		return Math.Max(scaledMin, TextRenderer.MeasureText(text, font).Width + scaledPad);
	}

	protected GlassButton FooterButton(string text, GlassButtonKind kind, string? name = null)
	{
		GlassButton glassButton = new GlassButton
		{
			Text = text,
			Kind = kind
		};
		if (name != null)
		{
			glassButton.Name = name;
		}
		int width = Math.Max(S(96), TextRenderer.MeasureText(text, glassButton.Font).Width + 2 * S(12));
		glassButton.Size = new Size(width, S(32));
		glassButton.AccessibleName = text;
		return glassButton;
	}

	protected void PlaceFooter(params GlassButton[] leftToRight)
	{
		int num = S(24);
		int num2 = ClientSize.Width - num;
		for (int num3 = leftToRight.Length - 1; num3 >= 0; num3--)
		{
			GlassButton glassButton = leftToRight[num3];
			num2 -= glassButton.Width;
			glassButton.Location = new Point(num2, ClientSize.Height - S(20) - glassButton.Height);
			glassButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			num2 -= S(8);
		}
	}

	private void ApplyRegion()
	{
		if (Square)
		{
			if (Region != null)
			{
				Region = null;
			}
			return;
		}
		using GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, ClientSize.Width, ClientSize.Height), S(8));
		Region = new Region(path);
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		using SolidBrush brush = new SolidBrush(Theme.Background);
		e.Graphics.FillRectangle(brush, ClientRectangle);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		PaintChrome(e.Graphics, ClientSize, _dpi, Square);
		PaintWords(e.Graphics);
		base.OnPaint(e);
	}

	internal static void PaintChrome(Graphics g, Size size, int dpi, bool square = false)
	{
		using (SolidBrush brush = new SolidBrush(Theme.Background))
		{
			g.FillRectangle(brush, new Rectangle(Point.Empty, size));
		}
		if (square)
		{
			using (SolidBrush brush2 = new SolidBrush(ControlInk.Overlay))
			{
				g.FillRectangle(brush2, new Rectangle(Point.Empty, size));
			}
			PaintSquareEdge(g, size, dpi);
		}
		else
		{
			ControlInk.PaintBox(g, size, Tokens.ScaleAt(dpi, 8), ControlInk.Hairline(dpi), ControlInk.Overlay, EdgeInk);
		}
	}

	internal static void PaintSquareEdge(Graphics g, Size size, int dpi)
	{
		int num = Math.Min(ControlInk.Hairline(dpi), Math.Min(size.Width, size.Height) / 2);
		SmoothingMode smoothingMode = g.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
		g.SmoothingMode = SmoothingMode.None;
		g.PixelOffsetMode = PixelOffsetMode.None;
		try
		{
			using SolidBrush brush = new SolidBrush(EdgeInk);
			g.FillRectangle(brush, 0, 0, size.Width, num);
			g.FillRectangle(brush, 0, size.Height - num, size.Width, num);
			g.FillRectangle(brush, 0, 0, num, size.Height);
			g.FillRectangle(brush, size.Width - num, 0, num, size.Height);
		}
		finally
		{
			g.SmoothingMode = smoothingMode;
			g.PixelOffsetMode = pixelOffsetMode;
		}
	}

	private void PaintWords(Graphics g)
	{
		int num = S(24);
		int textWidth = TextWidth;
		if (!string.IsNullOrEmpty(ChromeTitle))
		{
			TextRenderer.DrawText(g, ChromeTitle, Fonts.Title, new Rectangle(Ink(num + S(2), Fonts.Title), S(23), textWidth + GlyphPad(Fonts.Title), Fonts.LineHeight(FontRole.Title)), Theme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
		}
		if (!string.IsNullOrEmpty(ChromeBody))
		{
			TextRenderer.DrawText(g, ChromeBody, Fonts.Body, new Rectangle(Ink(num + S(2), Fonts.Body), BodyTop, textWidth + GlyphPad(Fonts.Body), ChromeBodyHeight), Theme.TextDim, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak);
		}
	}

	internal static int GlyphPad(Font font)
	{
		return (TextRenderer.MeasureText("H", font).Width - TextRenderer.MeasureText("H", font, Size.Empty, TextFormatFlags.NoPadding).Width) / 2;
	}

	internal static int Ink(int x, Font font)
	{
		return x - GlyphPad(font);
	}
}
