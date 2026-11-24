using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public static class LockPill
{
	public const string Text = "COMES WITH A PLAN";

	private const int Height = 24;

	private const int PadX = 8;

	private const int Icon = 16;

	private const int Gap = 8;

	private const int Radius = 4;

	public static string Label => char.ToUpperInvariant("COMES WITH A PLAN"[0]) + "COMES WITH A PLAN".Substring(1).ToLowerInvariant();

	private static Font WordFont => Fonts.CaptionBold;

	private static int S(int px)
	{
		return Tokens.Scale(px);
	}

	public static Size Measure()
	{
		Size size = TextRenderer.MeasureText(Label, WordFont, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
		return new Size(S(8) + S(16) + S(8) + size.Width + S(8), S(24));
	}

	public static void Draw(Graphics g, Rectangle tag)
	{
		if (tag.Width >= 4 && tag.Height >= 4)
		{
			int dpi = Tokens.Dpi;
			GraphicsState gstate = g.Save();
			g.TranslateTransform(tag.X, tag.Y);
			ControlInk.PaintBox(g, tag.Size, S(4), ControlInk.Hairline(dpi), Color.Empty, ControlInk.Border);
			g.Restore(gstate);
			Color text = ControlInk.Text2;
			int num = S(16);
			RectangleF box = new RectangleF(tag.X + S(8), (float)tag.Y + (float)(tag.Height - num) / 2f, num, num);
			SignalIcons.Draw(g, SignalIcon.Lock, box, text);
			int num2 = (int)box.Right + S(8);
			TextRenderer.DrawText(g, Label, WordFont, new Rectangle(num2, tag.Y, Math.Max(0, tag.Right - S(8) - num2 + S(2)), tag.Height), text, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
	}
}
