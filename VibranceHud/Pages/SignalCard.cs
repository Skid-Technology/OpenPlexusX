using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Pages;

internal sealed class SignalCard
{
	internal sealed record Placed(int Top, int Height, Label Title, Label? Caption)
	{
		internal int Bottom => Top + Height;
	}

	private sealed class UnpaddedStatusLabel : Label
	{
		public override Size GetPreferredSize(Size proposedSize)
		{
			return TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
	}

	internal const int Inset = 21;

	internal const int PadY = 4;

	internal const int RowHeight = 52;

	internal const int TitleLine = 20;

	internal const int CaptionLine = 16;

	internal const int ControlHeight = 28;

	internal const int ToggleWidth = 40;

	internal const int ToggleHeight = 20;

	internal const int TextX = 18;

	internal const int CaptionX = 20;

	private readonly int _width;

	private int _rowTop = 5;

	private bool _first = true;

	internal CardPanel Panel { get; }

	internal int ContentWidth => _width - 42;

	internal int Bottom => _rowTop;

	internal (Label Lead, Label Tail, Control? Control)? StatusParts { get; private set; }

	internal static int RowHeightFor(int lines)
	{
		return Math.Max(52, 30 + 16 * Math.Max(0, lines));
	}

	internal SignalCard(int x, int y, int width)
	{
		_width = width;
		Panel = new CardPanel
		{
			Location = new Point(x, y),
			Size = new Size(width, 62)
		};
	}

	internal static ToggleSwitch Toggle(bool on)
	{
		return new ToggleSwitch
		{
			Size = new Size(40, 20),
			Checked = on
		};
	}

	internal Placed Row(string title, string caption = "", Control? control = null, string? value = null, int captionLines = 1, Color? captionInk = null, Color? titleInk = null)
	{
		if (caption.Length == 0)
		{
			captionLines = 0;
		}
		int rowTop = _rowTop;
		int num = RowHeightFor(captionLines);
		int num2 = rowTop;
		if (!_first)
		{
			Panel.Controls.Add(new Label
			{
				AutoSize = false,
				Text = "",
				BackColor = Theme.Border,
				Bounds = new Rectangle(21, rowTop, _width - 42, 1)
			});
			num2 = rowTop + 1;
		}
		_first = false;
		int num3 = 20 + 16 * captionLines;
		int num4 = num2 + (num - num3) / 2;
		int num5 = num2 + num / 2;
		int num6 = ((value != null) ? (TextRenderer.MeasureText(value, Fonts.Body).Width + 6) : 0);
		int num7 = ((control != null) ? (control.Width + 16) : 0) + ((num6 > 0) ? (num6 + 16) : 0);
		Label label = new Label
		{
			Text = title,
			ForeColor = (titleInk ?? Theme.Text),
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(18, num4 - 1),
			AutoSize = true,
			UseMnemonic = false
		};
		Panel.Controls.Add(label);
		Label label2 = null;
		if (captionLines > 0)
		{
			label2 = new Label
			{
				Text = caption,
				ForeColor = (captionInk ?? SettingsPage.RowCaptionInk),
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(20, num4 + 20 + 2),
				AutoSize = false,
				Size = new Size(Math.Max(40, ContentWidth - num7), 16 * captionLines),
				UseMnemonic = false
			};
			Panel.Controls.Add(label2);
		}
		int num8 = _width - 21 + 3;
		if (control != null)
		{
			if (string.IsNullOrEmpty(control.AccessibleName))
			{
				control.AccessibleName = title;
				control.AccessibleDescription = caption;
			}
			control.Location = new Point(_width - 21 - control.Width, num5 - control.Height / 2);
			Panel.Controls.Add(control);
			num8 = control.Left - 6;
		}
		if (value != null)
		{
			Panel.Controls.Add(new Label
			{
				Text = value,
				ForeColor = Theme.TextDim,
				BackColor = Color.Transparent,
				Font = Fonts.Body,
				AutoSize = false,
				TextAlign = ContentAlignment.MiddleRight,
				Bounds = new Rectangle(num8 - num6, num5 - 10, num6, 20),
				UseMnemonic = false
			});
		}
		_rowTop = rowTop + num;
		Panel.Height = _rowTop + 4 + 1;
		return new Placed(rowTop, num, label, label2);
	}

	internal void StatusRow(string lead, string tail, Control? control = null)
	{
		int rowTop = _rowTop;
		int num = rowTop + 26;
		UnpaddedStatusLabel unpaddedStatusLabel = new UnpaddedStatusLabel
		{
			Text = lead,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(18, num - 10),
			AutoSize = false,
			Size = new Size(TextRenderer.MeasureText(lead, Fonts.Body, Size.Empty, TextFormatFlags.NoPadding).Width, 20),
			UseMnemonic = false
		};
		Panel.Controls.Add(unpaddedStatusLabel);
		int width = unpaddedStatusLabel.Width;
		if (control != null)
		{
			control.Location = new Point(_width - 21 - control.Width, num - control.Height / 2);
			Panel.Controls.Add(control);
		}
		int num2 = 18 + width + 4;
		int val = ((control != null) ? (control.Left - 12) : (_width - 21)) - num2;
		UnpaddedStatusLabel unpaddedStatusLabel2 = new UnpaddedStatusLabel
		{
			Text = tail,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(num2, num - 10),
			AutoSize = false,
			Size = new Size(Math.Max(40, val), 20),
			TextAlign = ContentAlignment.MiddleLeft,
			UseMnemonic = false
		};
		Panel.Controls.Add(unpaddedStatusLabel2);
		StatusParts = (unpaddedStatusLabel, unpaddedStatusLabel2, control);
		_first = false;
		_rowTop = rowTop + 52;
		Panel.Height = _rowTop + 4 + 1;
	}

	internal void GrowToFit(Control child)
	{
		int num = child.Bottom + 4 + 1;
		if (num > Panel.Height)
		{
			Panel.Height = num;
		}
	}

	internal void GrowToFitAll()
	{
		int num = 0;
		foreach (Control control in Panel.Controls)
		{
			num = Math.Max(num, control.Bottom);
		}
		Panel.Height = Math.Max(Panel.Height, num + 4 + 1);
	}
}
