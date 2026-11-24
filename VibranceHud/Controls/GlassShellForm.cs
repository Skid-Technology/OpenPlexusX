using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public class GlassShellForm : SignalDialogWindow
{
	private readonly string _title;

	private Rectangle _closeRect;

	private bool _overClose;

	private bool _showClose = true;

	private bool _paintsTitle = true;

	protected static int Pad => Tokens.Scale(24);

	protected new static int ContentTop => Tokens.Scale(56);

	public bool ShowClose
	{
		get
		{
			return _showClose;
		}
		set
		{
			if (_showClose != value)
			{
				_showClose = value;
				if (!value)
				{
					_closeRect = Rectangle.Empty;
					_overClose = false;
				}
				Invalidate();
			}
		}
	}

	public bool PaintsTitle
	{
		get
		{
			return _paintsTitle;
		}
		set
		{
			if (_paintsTitle != value)
			{
				_paintsTitle = value;
				Invalidate();
			}
		}
	}

	public GlassShellForm(string title)
	{
		_title = title;
		Text = title;
		AccessibleName = title;
		KeyDown += (object? _, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Escape)
			{
				CancelShell();
			}
		};
		WindowDrag.Enable(this, this, (Point p) => _closeRect.Contains(p));
	}

	protected virtual void CancelShell()
	{
		DialogResult = DialogResult.Cancel;
		Close();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		bool flag = _closeRect.Contains(e.Location);
		if (flag != _overClose)
		{
			_overClose = flag;
			Invalidate(new Rectangle(_closeRect.X - 4, _closeRect.Y - 4, _closeRect.Width + 8, _closeRect.Height + 8));
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		if (_overClose)
		{
			_overClose = false;
			Invalidate();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Left && _closeRect.Contains(e.Location))
		{
			CancelShell();
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		int num = S(24);
		int num2 = S(28);
		_closeRect = (_showClose ? new Rectangle(Width - num2 - S(12), S(12), num2, num2) : Rectangle.Empty);
		int num3 = (_showClose ? (_closeRect.Left - S(8)) : (Width - num));
		if (_paintsTitle)
		{
			TextRenderer.DrawText(graphics, _title, Fonts.Title, new Rectangle(SignalDialogWindow.Ink(num + S(2), Fonts.Title), S(18), num3 - (num + S(2)) + SignalDialogWindow.GlyphPad(Fonts.Title), S(28)), Theme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
		}
		if (_showClose)
		{
			if (_overClose)
			{
				GraphicsState gstate = graphics.Save();
				graphics.TranslateTransform(_closeRect.X, _closeRect.Y);
				ControlInk.PaintBox(graphics, _closeRect.Size, S(4), ControlInk.Hairline(ChromeDpi), ControlInk.Hover, Color.Empty);
				graphics.Restore(gstate);
			}
			int num4 = S(16);
			SignalIcons.Draw(graphics, SignalIcon.Close, new RectangleF((float)_closeRect.X + (float)(num2 - num4) / 2f, (float)_closeRect.Y + (float)(num2 - num4) / 2f, num4, num4), _overClose ? ControlInk.Text1 : ControlInk.Text2);
		}
	}
}
