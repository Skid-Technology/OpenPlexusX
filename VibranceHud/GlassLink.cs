using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class GlassLink : Control
{
	private bool _hover;

	private bool _pressed;

	private readonly OutsideFocusRing _ring;

	private ContentAlignment _align = ContentAlignment.MiddleCenter;

	private const TextFormatFlags Measure = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

	public ContentAlignment TextAlign
	{
		get
		{
			return _align;
		}
		set
		{
			if (_align != value)
			{
				_align = value;
				Invalidate();
			}
		}
	}

	public GlassLink()
	{
		_ring = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 4f));
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Height = 20;
		Font = Fonts.Body;
		TabStop = true;
		AccessibleRole = AccessibleRole.Link;
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		AccessibleName = Text;
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		_hover = true;
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_hover = false;
		_pressed = false;
		Invalidate();
	}

	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		Invalidate();
	}

	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);
		Invalidate();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		Focus();
		if (e.Button == MouseButtons.Left)
		{
			_pressed = true;
			Invalidate();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (_pressed)
		{
			_pressed = false;
			Invalidate();
		}
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = (Enabled ? Cursors.Hand : Cursors.Default);
		_hover = false;
		_pressed = false;
		Invalidate();
	}

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		_ring.Rehook();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_ring.Release();
		}
		base.Dispose(disposing);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		Keys keyCode = e.KeyCode;
		if ((keyCode == Keys.Return || keyCode == Keys.Space) ? true : false)
		{
			e.Handled = true;
			OnClick(EventArgs.Empty);
		}
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	public void PerformClick()
	{
		if (Enabled)
		{
			OnClick(EventArgs.Empty);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		int dpi = ControlInk.Dpi(this);
		Color foreColor;
		Color color;
		if (!Enabled)
		{
			foreColor = ControlInk.TextDisabledInk;
			color = Color.Transparent;
		}
		else if (_pressed)
		{
			foreColor = (color = ControlInk.Text2);
		}
		else
		{
			foreColor = ControlInk.Text1;
			color = (_hover ? ControlInk.Text1 : ControlInk.Edge);
		}
		Size size = TextRenderer.MeasureText(graphics, Text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
		int x = _align switch
		{
			ContentAlignment.MiddleLeft => 0, 
			ContentAlignment.MiddleRight => Width - size.Width, 
			_ => (Width - size.Width) / 2, 
		};
		TextRenderer.DrawText(graphics, Text, Font, new Rectangle(x, 0, size.Width + 2, Height), foreColor, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		if (color.A > 0 && size.Width > 0)
		{
			FontFamily fontFamily = Font.FontFamily;
			float num = (float)(Font.Height * fontFamily.GetCellAscent(Font.Style)) / (float)fontFamily.GetLineSpacing(Font.Style);
			int num2 = ControlInk.Hairline(dpi);
			int val = (int)Math.Round((float)(Height - Font.Height) / 2f + num) + ControlInk.Px(dpi, 3);
			val = Math.Min(val, Height - num2);
			using SolidBrush brush = new SolidBrush(color);
			graphics.FillRectangle(brush, x, val, size.Width, num2);
		}
		if (Focused && Enabled && ShowFocusCues)
		{
			_ring.Show(show: true);
		}
		else
		{
			_ring.Show(show: false);
		}
	}
}
