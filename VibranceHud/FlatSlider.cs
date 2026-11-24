using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class FlatSlider : Control
{
	private sealed class SliderAccessibleObject : ControlAccessibleObject
	{
		private readonly FlatSlider _owner;

		public override string? Value => _owner.Value.ToString();

		public SliderAccessibleObject(FlatSlider owner)
			: base(owner)
		{
			_owner = owner;
		}
	}

	private sealed class KnobFocusRing
	{
		private readonly FlatSlider _owner;

		private Control? _parent;

		private bool _shown;

		private Point _at;

		internal bool Shown => _shown;

		public KnobFocusRing(FlatSlider owner)
		{
			_owner = owner;
		}

		public void Rehook()
		{
			Unhook();
			_parent = _owner.Parent;
			if (_parent != null)
			{
				_parent.Paint += OnParentPaint;
			}
		}

		public void Release()
		{
			Unhook();
		}

		private void Unhook()
		{
			if (_parent != null)
			{
				_parent.Paint -= OnParentPaint;
			}
			_parent = null;
		}

		public void Show(bool show, Point knob)
		{
			if (show != _shown || (show && !(knob == _at)))
			{
				if (_shown)
				{
					InvalidateAround(_at);
				}
				_shown = show;
				_at = knob;
				if (_shown)
				{
					InvalidateAround(_at);
				}
			}
		}

		private void InvalidateAround(Point knob)
		{
			if (_parent != null && !_parent.IsDisposed)
			{
				int num = (int)Math.Ceiling(_owner.FocusReach()) + 2;
				_parent.Invalidate(new Rectangle(_owner.Left + knob.X - num, _owner.Top + knob.Y - num, 2 * num, 2 * num));
			}
		}

		private void OnParentPaint(object? sender, PaintEventArgs e)
		{
			if (!_shown || !_owner.Visible || _owner.IsDisposed)
			{
				return;
			}
			Graphics graphics = e.Graphics;
			GraphicsState gstate = graphics.Save();
			try
			{
				graphics.ExcludeClip(_owner.Bounds);
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				graphics.PixelOffsetMode = PixelOffsetMode.Half;
				SliderPaint.FocusRing(graphics, ControlInk.Dpi(_owner), lg: false, _owner.Left + _at.X, _owner.Top + _at.Y);
			}
			finally
			{
				graphics.Restore(gstate);
			}
		}
	}

	public const int EdgeInsetPx = 12;

	private int _minimum;

	private int _maximum = 100;

	private int _value;

	private int? _notch;

	private bool _dragging;

	private int _boxInset = -1;

	private readonly KnobFocusRing _ring;

	public static int EdgeInset => Tokens.Scale(12);

	public int? Notch
	{
		get
		{
			return _notch;
		}
		set
		{
			_notch = value;
			Invalidate();
		}
	}

	public int Minimum
	{
		get
		{
			return _minimum;
		}
		set
		{
			_minimum = value;
			Invalidate();
		}
	}

	public int Maximum
	{
		get
		{
			return _maximum;
		}
		set
		{
			_maximum = Math.Max(value, _minimum + 1);
			Invalidate();
		}
	}

	public int Value
	{
		get
		{
			return _value;
		}
		set
		{
			int num = Math.Clamp(value, _minimum, _maximum);
			if (num != _value)
			{
				_value = num;
				Invalidate();
				ValueChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	private int Dpi => ControlInk.Dpi(this);

	private int Pad
	{
		get
		{
			if (_boxInset < 0)
			{
				return EdgeInsetAt(Dpi);
			}
			return _boxInset;
		}
	}

	internal bool KeyboardFocusForTest { get; set; }

	internal Point KnobCentreForTest => new Point(XFromValue(_value), Height / 2);

	internal bool FocusRingShownForTest => _ring.Shown;

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

	public event EventHandler? ValueChanged;

	public static int EdgeInsetAt(int dpi)
	{
		return Tokens.ScaleAt(dpi, 12);
	}

	public FlatSlider()
	{
		_ring = new KnobFocusRing(this);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Height = 32;
		Cursor = Cursors.Hand;
		TabStop = true;
		SetStyle(ControlStyles.Selectable, value: true);
		AccessibleRole = AccessibleRole.Slider;
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

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = (Enabled ? Cursors.Hand : Cursors.Default);
		if (_dragging)
		{
			_dragging = false;
			DragTracker.End();
		}
		Invalidate();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		int num;
		switch (e.KeyCode)
		{
		case Keys.Left:
		case Keys.Down:
			num = -1;
			break;
		case Keys.Up:
		case Keys.Right:
			num = 1;
			break;
		case Keys.Next:
			num = -10;
			break;
		case Keys.Prior:
			num = 10;
			break;
		default:
			num = 0;
			break;
		}
		int num2 = num;
		if (num2 != 0)
		{
			Value = Math.Clamp(Value + num2, Minimum, Maximum);
			e.Handled = true;
		}
		else if (e.KeyCode == Keys.Home)
		{
			Value = Minimum;
			e.Handled = true;
		}
		else if (e.KeyCode == Keys.End)
		{
			Value = Maximum;
			e.Handled = true;
		}
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	protected override bool IsInputKey(Keys keyData)
	{
		if ((uint)(keyData - 33) <= 7u)
		{
			return true;
		}
		return base.IsInputKey(keyData);
	}

	internal bool TestClaimsKey(Keys key)
	{
		return IsInputKey(key);
	}

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new SliderAccessibleObject(this);
	}

	public void SetTrackBounds(int x, int y, int width, int height = 32)
	{
		int num = (_boxInset = EdgeInsetAt(Dpi));
		SetBounds(x - num, y, width + 2 * num, height);
	}

	protected override Rectangle GetScaledBounds(Rectangle bounds, SizeF factor, BoundsSpecified specified)
	{
		Rectangle scaledBounds = base.GetScaledBounds(bounds, factor, specified);
		if (_boxInset < 0 || (specified & (BoundsSpecified.X | BoundsSpecified.Width)) != (BoundsSpecified.X | BoundsSpecified.Width))
		{
			return scaledBounds;
		}
		int num = (int)Math.Round((float)(bounds.X + _boxInset) * factor.Width);
		int num2 = (int)Math.Round((float)(bounds.Right - _boxInset) * factor.Width);
		int num3 = EdgeInsetAt(Dpi);
		return new Rectangle(num - num3, scaledBounds.Y, num2 - num + 2 * num3, scaledBounds.Height);
	}

	protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
	{
		base.ScaleControl(factor, specified);
		if (_boxInset >= 0 && (specified & (BoundsSpecified.X | BoundsSpecified.Width)) == (BoundsSpecified.X | BoundsSpecified.Width))
		{
			_boxInset = EdgeInsetAt(Dpi);
		}
	}

	private int XFromValue(int value)
	{
		float num = (float)(value - _minimum) / (float)(_maximum - _minimum);
		return Pad + (int)Math.Round(num * (float)(Width - 2 * Pad));
	}

	private int ValueFromX(int x)
	{
		float value = (float)(x - Pad) / (float)Math.Max(1, Width - 2 * Pad);
		return _minimum + (int)Math.Round(Math.Clamp(value, 0f, 1f) * (float)(_maximum - _minimum));
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
		{
			_dragging = true;
			DragTracker.Begin();
			Value = LandingFromX(e.X);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_dragging)
		{
			Value = LandingFromX(e.X);
		}
	}

	private int LandingFromX(int x)
	{
		return SliderSnap.Land(ValueFromX(x), _minimum, _maximum, _notch);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (_dragging)
		{
			_dragging = false;
			DragTracker.End();
		}
	}

	internal int XForTest(int value)
	{
		return XFromValue(value);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		int pad = Pad;
		if (Width - 2 * pad < 2 || Height < 2)
		{
			_ring.Show(show: false, Point.Empty);
			return;
		}
		SmoothingMode smoothingMode = graphics.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = graphics.PixelOffsetMode;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.PixelOffsetMode = PixelOffsetMode.Half;
		try
		{
			int dpi = Dpi;
			int num = Height / 2;
			int num2 = XFromValue(_value);
			SliderPaint.Track(graphics, dpi, pad, Width - pad, num);
			SliderPaint.Fill(graphics, pad, num2, num, ControlInk.Px(dpi, SliderPaint.FillPx(lg: false)), SliderPaint.FillInk(Enabled));
			int? notch = _notch;
			if (notch.HasValue)
			{
				int valueOrDefault = notch.GetValueOrDefault();
				if (valueOrDefault > _minimum && valueOrDefault < _maximum)
				{
					SliderPaint.Tick(graphics, dpi, XFromValue(valueOrDefault), num, ControlInk.Px(dpi, SliderPaint.TickPx(lg: false)), Enabled);
				}
			}
			SliderPaint.Knob(graphics, dpi, ControlInk.PxF(dpi, SliderPaint.KnobPx(lg: false)), num2, num, ControlInk.KnobRing, Enabled);
			if (Focused && ShowFocusCues)
			{
				SliderPaint.FocusRing(graphics, dpi, lg: false, num2, num);
				_ring.Show(show: true, new Point(num2, num));
			}
			else
			{
				_ring.Show(show: false, Point.Empty);
			}
		}
		finally
		{
			graphics.SmoothingMode = smoothingMode;
			graphics.PixelOffsetMode = pixelOffsetMode;
		}
	}

	private float FocusReach()
	{
		return ControlInk.PxF(Dpi, SliderPaint.FocusReachPx(lg: false));
	}
}
