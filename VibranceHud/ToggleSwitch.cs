using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class ToggleSwitch : Control
{
	private sealed class ToggleAccessibleObject : ControlAccessibleObject
	{
		private readonly ToggleSwitch _owner;

		public override AccessibleStates State => (AccessibleStates)((int)base.State | (_owner.Checked ? 16 : 0));

		public ToggleAccessibleObject(ToggleSwitch owner)
			: base(owner)
		{
			_owner = owner;
		}
	}

	private bool _checked;

	private readonly HoverFade _fade;

	private readonly HoverFade _press;

	private readonly OutsideFocusRing _ring;

	public bool Checked
	{
		get
		{
			return _checked;
		}
		set
		{
			if (_checked != value)
			{
				_checked = value;
				Invalidate();
				CheckedChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	internal bool KeyboardFocusForTest { get; set; }

	public override bool Focused
	{
		get
		{
			if (!KeyboardFocusForTest)
			{
				return base.Focused;
			}
			return true;
		}
	}

	protected override bool ShowFocusCues
	{
		get
		{
			if (!KeyboardFocusForTest)
			{
				return base.ShowFocusCues;
			}
			return true;
		}
	}

	internal bool FocusRingShownForTest => _ring.Shown;

	public event EventHandler? CheckedChanged;

	public ToggleSwitch()
	{
		_fade = new HoverFade(this);
		_press = new HoverFade(this);
		_ring = new OutsideFocusRing(this, () => (float)Height / 2f);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Size = new Size(40, 20);
		Cursor = Cursors.Hand;
		TabStop = true;
		SetStyle(ControlStyles.Selectable, value: true);
		AccessibleRole = AccessibleRole.CheckButton;
	}

	protected override void OnClick(EventArgs e)
	{
		base.OnClick(e);
		Checked = !Checked;
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

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		_fade.Set(hot: true);
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_press.Set(hot: false);
		_fade.Set(hot: false);
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

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = (Enabled ? Cursors.Hand : Cursors.Default);
		_fade.Snap(hot: false);
		_press.Snap(hot: false);
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

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new ToggleAccessibleObject(this);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		int dpi = ControlInk.Dpi(this);
		float num = (Enabled ? _fade.Value : 0f);
		float num2 = (Enabled ? _press.Value : 0f);
		Color fill;
		Color edge;
		Color color;
		if (_checked)
		{
			if (!Enabled)
			{
				fill = (edge = ControlInk.Surface2);
				color = ControlInk.TextDisabledInk;
			}
			else
			{
				fill = (edge = ControlInk.Mix(ControlInk.Mix(ControlInk.Accent, ControlInk.AccentHover, num), ControlInk.AccentPressed, num2));
				color = ControlInk.OnAccent;
			}
		}
		else if (!Enabled)
		{
			fill = Color.Transparent;
			edge = ControlInk.TextDisabled;
			color = ControlInk.TextDisabledInk;
		}
		else
		{
			Color hover = ControlInk.Hover;
			fill = ControlInk.Mix(ControlInk.Mix(Color.FromArgb(0, hover), hover, num), ControlInk.Pressed, num2);
			edge = ControlInk.Edge;
			color = ControlInk.Mix(ControlInk.Text2, ControlInk.Text1, Math.Max(num, num2));
		}
		ControlInk.PaintBox(graphics, ClientSize, (float)Height / 2f, ControlInk.Hairline(dpi), fill, edge);
		int num3 = ControlInk.Px(dpi, _checked ? 12 : 10);
		int num4 = (Height - num3) / 2;
		int x = (_checked ? (Width - num4 - num3) : num4);
		SmoothingMode smoothingMode = graphics.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = graphics.PixelOffsetMode;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.PixelOffsetMode = PixelOffsetMode.Half;
		using (SolidBrush brush = new SolidBrush(color))
		{
			graphics.FillEllipse(brush, x, num4, num3, num3);
		}
		graphics.SmoothingMode = smoothingMode;
		graphics.PixelOffsetMode = pixelOffsetMode;
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
