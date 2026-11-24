using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Pages;

internal static class WordsFit
{
	private const TextFormatFlags Wrapped = TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak;

	internal static int Needed(Label l)
	{
		return TextRenderer.MeasureText(l.Text, l.Font, new Size(Math.Max(1, l.Width), int.MaxValue), TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak).Height;
	}

	internal static int OneLine(Font font)
	{
		return TextRenderer.MeasureText("Hg", font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl).Height;
	}

	internal static bool TypeAndBoxesAgree(int captionLineAtPageScale, int pageDpi)
	{
		if (pageDpi != Tokens.Dpi)
		{
			return false;
		}
		return Math.Abs(OneLine(Fonts.Caption) - captionLineAtPageScale) * 8 <= captionLineAtPageScale;
	}

	internal static int GrowWrappedLabels(Control card, Func<Label, bool>? skip = null)
	{
		int num = 0;
		foreach (Label item in (from l in card.Controls.OfType<Label>()
			orderby l.Top
			select l).ToList())
		{
			if (item.AutoSize || item.Width <= 0 || item.Height <= 0 || string.IsNullOrEmpty(item.Text) || (skip != null && skip(item)))
			{
				continue;
			}
			int num2 = Needed(item);
			if (num2 <= OneLine(item.Font) + 2 || item.Height >= num2)
			{
				continue;
			}
			int num3 = num2 - item.Height;
			int bottom = item.Bottom;
			int num4 = int.MaxValue;
			foreach (Control control3 in card.Controls)
			{
				if (control3 != item && control3.Height > 0 && control3.Top >= bottom - 1 && control3.Left < item.Right && control3.Right > item.Left)
				{
					num4 = Math.Min(num4, control3.Top);
				}
			}
			int num5 = ((num4 != int.MaxValue) ? Math.Max(0, num4 - bottom) : 0);
			item.Height = num2;
			int num6 = Math.Max(0, num3 - num5);
			if (num6 == 0)
			{
				continue;
			}
			foreach (Control control4 in card.Controls)
			{
				if (control4 != item && control4.Top >= bottom - 1)
				{
					control4.Top += num6;
				}
			}
			card.Height += num6;
			num += num6;
		}
		return num;
	}

	internal static void GrowNarrowButtons(Control card, int inset)
	{
		foreach (GlassButton item in (from b in card.Controls.OfType<GlassButton>()
			orderby b.Left
			select b).ToList())
		{
			int width = item.GetPreferredSize(Size.Empty).Width;
			if (item.Width >= width)
			{
				continue;
			}
			int num = width - item.Width;
			if (Math.Abs(item.Right - (card.Width - inset)) <= 2)
			{
				item.SetBounds(item.Left - num, item.Top, width, item.Height);
				continue;
			}
			int right = item.Right;
			item.Width = width;
			foreach (Control control in card.Controls)
			{
				if (control != item && control.Left >= right - 1 && control.Top < item.Bottom && control.Bottom > item.Top)
				{
					control.Left += num;
				}
			}
		}
	}
}
