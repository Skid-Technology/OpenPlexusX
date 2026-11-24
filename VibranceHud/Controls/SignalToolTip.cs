using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class SignalToolTip : ToolTip
{
	internal const int PadX = 8;

	internal const int PadY = 4;

	internal const int MaxWidth = 280;

	internal const int Radius = 4;

	private static int Dpi => Tokens.Dpi;

	public SignalToolTip()
	{
		OwnerDraw = true;
		UseAnimation = false;
		UseFading = false;
		InitialDelay = 500;
		ReshowDelay = 120;
		AutoPopDelay = 32767;
		BackColor = ControlInk.Overlay;
		ForeColor = ControlInk.Text1;
		Draw += (object? _, DrawToolTipEventArgs e) =>
		{
			Paint(e.Graphics, e.Bounds, e.ToolTipText, GroundOf(e.AssociatedControl));
		};
		Popup += (object? _, PopupEventArgs e) =>
		{
			e.ToolTipSize = Measure(GetToolTip(e.AssociatedControl) ?? string.Empty);
		};
	}

	internal static Color GroundOf(Control? c)
	{
		while (c != null)
		{
			Color backColor = c.BackColor;
			if (backColor.A == byte.MaxValue && !backColor.IsEmpty && backColor.ToArgb() != Control.DefaultBackColor.ToArgb())
			{
				return backColor;
			}
			c = c.Parent;
		}
		return Theme.Background;
	}

	internal static Size Measure(string text)
	{
		int dpi = Dpi;
		int num = Tokens.ScaleAt(dpi, 8);
		int num2 = Tokens.ScaleAt(dpi, 4);
		int width = Tokens.ScaleAt(dpi, 280) - 2 * num;
		Size size = TextRenderer.MeasureText(text, Fonts.Caption, new Size(width, 0), TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
		return new Size(size.Width + 2 * num, size.Height + 2 * num2);
	}

	internal static void Paint(Graphics g, Rectangle bounds, string text, Color? ground = null)
	{
		int dpi = Dpi;
		using (SolidBrush brush = new SolidBrush(ground ?? Theme.Background))
		{
			g.FillRectangle(brush, bounds);
		}
		GraphicsState gstate = g.Save();
		g.TranslateTransform(bounds.X, bounds.Y);
		ControlInk.PaintBox(g, bounds.Size, Tokens.ScaleAt(dpi, 4), ControlInk.Hairline(dpi), ControlInk.Overlay, SignalDialogWindow.EdgeInk);
		g.Restore(gstate);
		int num = Tokens.ScaleAt(dpi, 8);
		int num2 = Tokens.ScaleAt(dpi, 4);
		TextRenderer.DrawText(g, text, Fonts.Caption, new Rectangle(bounds.X + num, bounds.Y + num2, bounds.Width - 2 * num, bounds.Height - 2 * num2), ControlInk.Text1, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
	}
}
