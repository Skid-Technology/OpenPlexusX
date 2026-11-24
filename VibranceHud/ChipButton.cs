using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class ChipButton : Control
{
	private sealed class ChipAccessibleObject : ControlAccessibleObject
	{
		private readonly ChipButton _owner;

		public override AccessibleStates State => (AccessibleStates)((int)base.State | (_owner._active ? 16 : 0));

		public ChipAccessibleObject(ChipButton owner)
			: base(owner)
		{
			_owner = owner;
		}
	}

	private bool _active;

	private readonly HoverFade _fade;

	private readonly HoverFade _press;

	private readonly OutsideFocusRing _ring;

	private string _suffix = "";

	private ContentAlignment _align = ContentAlignment.MiddleCenter;

	public int Level { get; init; }

	public bool Active
	{
		get
		{
			return _active;
		}
		set
		{
			if (_active != value)
			{
				_active = value;
				Invalidate();
				if (IsHandleCreated)
				{
					AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
				}
			}
		}
	}

	public string Suffix
	{
		get
		{
			return _suffix;
		}
		set
		{
			if (value == null)
			{
				value = "";
			}
			if (!(_suffix == value))
			{
				_suffix = value;
				Invalidate();
			}
		}
	}

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

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new ChipAccessibleObject(this);
	}

	public ChipButton()
	{
		_fade = new HoverFade(this);
		_press = new HoverFade(this);
		_ring = new OutsideFocusRing(this, () => Radius(ControlInk.Dpi(this)));
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Height = 32;
		TabStop = true;
		SetStyle(ControlStyles.Selectable, value: true);
		AccessibleRole = AccessibleRole.PushButton;
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		AccessibleName = Text;
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
			_press.Snap(hot: true);
			Invalidate();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		_press.Set(hot: false);
		Invalidate();
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

	internal void TestSetHover(bool on)
	{
		_fade.Snap(on);
	}

	internal void TestSetPressed(bool on)
	{
		_press.Snap(on);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		_fade.Set(hot: true);
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_fade.Set(hot: false);
		_press.Set(hot: false);
		Invalidate();
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = (Enabled ? Cursors.Hand : Cursors.Default);
		_fade.Snap(hot: false);
		_press.Snap(hot: false);
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

	private static float Radius(int dpi)
	{
		return ControlInk.PxF(dpi, 4f);
	}

	private Font LabelFont(bool strong)
	{
		if (!strong)
		{
			return Font;
		}
		if (Math.Abs(Font.SizeInPoints - Fonts.Body.SizeInPoints) < 0.1f)
		{
			return Fonts.BodyBold;
		}
		if (Math.Abs(Font.SizeInPoints - Fonts.Caption.SizeInPoints) < 0.1f)
		{
			return Fonts.CaptionBold;
		}
		return Font;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		int dpi = ControlInk.Dpi(this);
		Color transparent = Color.Transparent;
		float t = (Enabled ? _fade.Value : 0f);
		float t2 = (Enabled ? _press.Value : 0f);
		Color fill;
		Color edge;
		Color foreColor;
		Color foreColor2;
		if (!Enabled)
		{
			fill = (_active ? ControlInk.Surface2 : transparent);
			edge = (_active ? ControlInk.Surface2 : ControlInk.TextDisabled);
			foreColor = (foreColor2 = ControlInk.TextDisabledInk);
		}
		else if (_active)
		{
			fill = (edge = ControlInk.Accent);
			foreColor = ControlInk.OnAccent;
			foreColor2 = ControlInk.SelectedSuffix;
		}
		else
		{
			Color hover = ControlInk.Hover;
			fill = ControlInk.Mix(ControlInk.Mix(Color.FromArgb(0, hover), hover, t), ControlInk.Pressed, t2);
			edge = ControlInk.Edge;
			foreColor = ControlInk.Text1;
			foreColor2 = ControlInk.Text3;
		}
		ControlInk.PaintBox(graphics, ClientSize, Radius(dpi), ControlInk.Hairline(dpi), fill, edge);
		Font font = LabelFont(_active);
		int width = TextRenderer.MeasureText(graphics, Text, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding).Width;
		int num = ControlInk.Px(dpi, 8);
		Font caption = Fonts.Caption;
		int num2 = ((_suffix.Length != 0) ? TextRenderer.MeasureText(graphics, _suffix, caption, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding).Width : 0);
		int num3 = width + ((num2 > 0) ? (num + num2) : 0);
		int num4 = ((_align == ContentAlignment.MiddleLeft) ? ControlInk.Px(dpi, 12) : ((Width - num3) / 2));
		TextRenderer.DrawText(graphics, Text, font, new Rectangle(num4, 0, width + 2, Height), foreColor, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		if (num2 > 0)
		{
			TextRenderer.DrawText(graphics, _suffix, caption, new Rectangle(num4 + width + num, 0, num2 + 2, Height), foreColor2, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
		if (Focused && ShowFocusCues)
		{
			_ring.Show(show: true);
		}
		else
		{
			_ring.Show(show: false);
		}
	}
}
