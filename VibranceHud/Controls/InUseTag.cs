using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

internal sealed class InUseTag : Control
{
	private int Dpi => ControlInk.Dpi(this);

	internal Size NaturalSize
	{
		get
		{
			int num = ControlInk.Hairline(Dpi);
			return new Size(TextRenderer.MeasureText(Text, Fonts.CaptionBold, Size.Empty, TextFormatFlags.NoPadding).Width + 2 * Px(8) + 2 * num, Fonts.CaptionBold.Height + 2 * Px(2) + 2 * num);
		}
	}

	internal InUseTag()
		: this("In use")
	{
	}

	internal InUseTag(string text)
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.Selectable, value: false);
		BackColor = Color.Transparent;
		TabStop = false;
		Font = Fonts.CaptionBold;
		AccessibleRole = AccessibleRole.StaticText;
		Text = text;
	}

	private int Px(int logical)
	{
		return ControlInk.Px(Dpi, logical);
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		AccessibleName = Text;
		Size = NaturalSize;
		Invalidate();
	}

	protected override void OnDpiChangedAfterParent(EventArgs e)
	{
		base.OnDpiChangedAfterParent(e);
		Size = NaturalSize;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		int dpi = Dpi;
		ControlInk.PaintBox(graphics, Size, ControlInk.PxF(dpi, 4f), ControlInk.Hairline(dpi), ControlInk.Accent, ControlInk.Accent);
		TextRenderer.DrawText(graphics, Text, Fonts.CaptionBold, ClientRectangle, ControlInk.OnAccent, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
	}
}
