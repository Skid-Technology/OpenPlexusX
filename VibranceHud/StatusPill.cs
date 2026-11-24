using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class StatusPill : Control
{
	private StatusKind _status;

	public StatusKind Status
	{
		get
		{
			return _status;
		}
		set
		{
			if (_status != value)
			{
				_status = value;
				Invalidate();
			}
		}
	}

	private static Font WordFont => Fonts.BodyBold;

	private static int IconPx => Tokens.Scale(16);

	private static int GapPx => Tokens.Scale(8);

	internal string DrawnWordForTest => Text;

	public StatusPill()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		TabStop = false;
		AccessibleRole = AccessibleRole.StaticText;
		FitToText();
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		AccessibleName = Text;
		FitToText();
		Invalidate();
	}

	private void FitToText()
	{
		Size size = TextRenderer.MeasureText(Text ?? "", WordFont, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
		Size = new Size(IconPx + GapPx + size.Width + Tokens.Scale(2), Math.Max(Tokens.Scale(20), size.Height));
	}

	private (SignalIcon Icon, Color Ink) Mark()
	{
		return MarkOf(_status);
	}

	internal static (SignalIcon Icon, Color Ink) MarkOf(StatusKind status)
	{
		return status switch
		{
			StatusKind.Warning => (Icon: SignalIcon.Warning, Ink: ControlInk.Warning), 
			StatusKind.Danger => (Icon: SignalIcon.Danger, Ink: ControlInk.Danger), 
			StatusKind.Info => (Icon: SignalIcon.Info, Ink: ControlInk.Info), 
			_ => (Icon: SignalIcon.Success, Ink: ControlInk.Success), 
		};
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		(SignalIcon Icon, Color Ink) tuple = Mark();
		SignalIcon item = tuple.Icon;
		Color item2 = tuple.Ink;
		int iconPx = IconPx;
		SignalIcons.Draw(graphics, item, new RectangleF(0f, (float)(Height - iconPx) / 2f, iconPx, iconPx), item2);
		TextRenderer.DrawText(graphics, Text, WordFont, new Rectangle(iconPx + GapPx, 0, Math.Max(0, Width - iconPx - GapPx), Height), ControlInk.Text1, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
	}
}
