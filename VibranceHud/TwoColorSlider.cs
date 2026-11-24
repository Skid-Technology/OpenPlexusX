using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class TwoColorSlider : Control
{
	public const int EdgeInsetPx = 14;

	public const int MinHeightPx = 30;

	private int _minimum;

	private int _maximum = 100;

	private int _value;

	private int? _notch;

	private bool _hover;

	private bool _dragging;

	private bool _hero;

	private SliderFill _fill;

	private int? _origin;

	private Color _knobRing = Color.Empty;

	public static int EdgeInset => Tokens.Scale(14);

	public static int MinHeight => Tokens.Scale(30);

	public SliderPalette Palette { get; set; } = SliderPalette.Plain;

	public bool Hero
	{
		get
		{
			return _hero;
		}
		set
		{
			if (_hero != value)
			{
				_hero = value;
				Invalidate();
			}
		}
	}

	public SliderFill Fill
	{
		get
		{
			return _fill;
		}
		set
		{
			if (_fill != value)
			{
				_fill = value;
				Invalidate();
			}
		}
	}

	public int? FillOrigin
	{
		get
		{
			return _origin;
		}
		set
		{
			_origin = value;
			Invalidate();
		}
	}

	public Color KnobRing
	{
		get
		{
			return _knobRing;
		}
		set
		{
			_knobRing = value;
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
			Value = _value;
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
			Value = _value;
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
				int x = XFromValue(_value);
				_value = num;
				int x2 = XFromValue(_value);
				InvalidateBetween(x, x2);
				ValueChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

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

	private int Dpi => ControlInk.Dpi(this);

	private int Pad => EdgeInsetAt(Dpi);

	public SliderSnapMode Snap { get; set; }

	internal bool KeyboardFocusForTest { get; set; }

	internal Point KnobCentreForTest => new Point(XFromValue(_value), Height / 2);

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

	public event EventHandler? DragBegin;

	public event EventHandler? DragEnd;

	public static int EdgeInsetAt(int dpi)
	{
		return Tokens.ScaleAt(dpi, 14);
	}

	public static int MinHeightAt(int dpi)
	{
		return Tokens.ScaleAt(dpi, 30);
	}

	private void InvalidateBetween(int x1, int x2)
	{
		int num = Px(14) + 2;
		int num2 = Math.Min(x1, x2) - num;
		int num3 = Math.Max(x1, x2) + num;
		Invalidate(new Rectangle(num2, 0, num3 - num2, Height));
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = (Enabled ? Cursors.Hand : Cursors.Default);
		_hover = false;
		if (_dragging)
		{
			_dragging = false;
		}
		Invalidate();
	}

	public TwoColorSlider()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Height = MinHeight;
		TabStop = true;
		Cursor = Cursors.Hand;
		AccessibleRole = AccessibleRole.Slider;
	}

	public void SetTrackBounds(int x, int y, int width)
	{
		int dpi = ControlInk.Dpi(this);
		int num = EdgeInsetAt(dpi);
		SetBounds(x - num, y, width + 2 * num, MinHeightAt(dpi));
	}

	private int Px(int logical)
	{
		return ControlInk.Px(Dpi, logical);
	}

	private float PxF(float logical)
	{
		return ControlInk.PxF(Dpi, logical);
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

	private int Origin()
	{
		int? origin = _origin;
		if (origin.HasValue)
		{
			int valueOrDefault = origin.GetValueOrDefault();
			return Math.Clamp(valueOrDefault, _minimum, _maximum);
		}
		if (_hero || _fill == SliderFill.Signal)
		{
			return _minimum;
		}
		origin = _notch;
		if (origin.HasValue)
		{
			int valueOrDefault2 = origin.GetValueOrDefault();
			if (valueOrDefault2 >= _minimum && valueOrDefault2 <= _maximum)
			{
				return valueOrDefault2;
			}
		}
		if (_minimum < 0 && _maximum > 0)
		{
			return 0;
		}
		return _minimum;
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
		Invalidate();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (Enabled && e.Button == MouseButtons.Left)
		{
			_dragging = true;
			DragTracker.Begin();
			Focus();
			DragBegin?.Invoke(this, EventArgs.Empty);
			Value = LandingFromX(e.X);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_dragging && Enabled)
		{
			Value = LandingFromX(e.X);
		}
	}

	private int LandingFromX(int x)
	{
		if (Snap != SliderSnapMode.Gentle)
		{
			return SliderSnap.Land(ValueFromX(x), _minimum, _maximum, _notch);
		}
		return SliderSnap.LandGently(ValueFromX(x), _minimum, _maximum, _notch);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Left && _dragging)
		{
			_dragging = false;
			DragTracker.End();
			DragEnd?.Invoke(this, EventArgs.Empty);
			Invalidate();
		}
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		if (Enabled)
		{
			Value += Math.Sign(e.Delta);
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (Enabled)
		{
			int num = ((!e.Shift) ? 1 : 10);
			switch (e.KeyCode)
			{
			case Keys.Left:
			case Keys.Down:
				Value -= num;
				e.Handled = true;
				break;
			case Keys.Up:
			case Keys.Right:
				Value += num;
				e.Handled = true;
				break;
			case Keys.Next:
				Value -= 10;
				e.Handled = true;
				break;
			case Keys.Prior:
				Value += 10;
				e.Handled = true;
				break;
			case Keys.Home:
				Value = _minimum;
				e.Handled = true;
				break;
			case Keys.End:
				Value = _maximum;
				e.Handled = true;
				break;
			}
		}
	}

	protected override bool IsInputKey(Keys keyData)
	{
		Keys keys = keyData & ~Keys.Shift;
		if ((uint)(keys - 33) <= 7u)
		{
			return true;
		}
		return base.IsInputKey(keyData);
	}

	internal bool TestClaimsKey(Keys key)
	{
		return IsInputKey(key);
	}

	internal void TestSetHover(bool on)
	{
		_hover = on;
	}

	internal void TestSetDragging(bool on)
	{
		_dragging = on;
	}

	internal int XForTest(int value)
	{
		return XFromValue(value);
	}

	internal static float KnobOuterRadiusForTest(bool hero, bool hot)
	{
		return ControlInk.PxF(Tokens.Dpi, (hero ? 8 : 7) + 1);
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

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		if (Width - 2 * Pad < 2 || Height < 2)
		{
			return;
		}
		SmoothingMode smoothingMode = graphics.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = graphics.PixelOffsetMode;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.PixelOffsetMode = PixelOffsetMode.Half;
		try
		{
			PaintSlider(graphics);
		}
		finally
		{
			graphics.SmoothingMode = smoothingMode;
			graphics.PixelOffsetMode = pixelOffsetMode;
		}
	}

	private void PaintSlider(Graphics g)
	{
		int cy = Height / 2;
		int pad = Pad;
		int right = Width - Pad;
		int fillH = Px(SliderPaint.FillPx(_hero));
		int tickH = Px(SliderPaint.TickPx(_hero));
		Color color = SliderPaint.FillInk(Enabled);
		SliderPaint.Track(g, Dpi, pad, right, cy);
		int num = XFromValue(_value);
		if (_fill == SliderFill.Signal)
		{
			PaintSignal(g, pad, num, cy, fillH, color);
		}
		else
		{
			int val = XFromValue(Origin());
			SliderPaint.Fill(g, Math.Min(val, num), Math.Max(val, num), cy, fillH, color);
		}
		int? notch = _notch;
		if (notch.HasValue)
		{
			int valueOrDefault = notch.GetValueOrDefault();
			if (valueOrDefault > _minimum && valueOrDefault < _maximum)
			{
				SliderPaint.Tick(g, Dpi, XFromValue(valueOrDefault), cy, tickH, Enabled);
			}
		}
		float radius = PxF(SliderPaint.KnobPx(_hero));
		SliderPaint.Knob(g, Dpi, radius, num, cy, _knobRing.IsEmpty ? ControlInk.KnobRing : _knobRing, Enabled);
		if (Focused && ShowFocusCues)
		{
			SliderPaint.FocusRing(g, Dpi, _hero, num, cy);
		}
	}

	private void PaintSignal(Graphics g, int left, int x, int cy, int fillH, Color disabledInk)
	{
		int? notch = _notch;
		int num;
		if (notch.HasValue)
		{
			int valueOrDefault = notch.GetValueOrDefault();
			if (valueOrDefault > _minimum && valueOrDefault < _maximum)
			{
				num = valueOrDefault;
				goto IL_0044;
			}
		}
		num = _minimum + (_maximum - _minimum) / 3;
		goto IL_0044;
		IL_0044:
		int value = num;
		int num2 = XFromValue(value);
		float num3 = cy - fillH / 2;
		if (!Enabled)
		{
			using (SolidBrush brush = new SolidBrush(disabledInk))
			{
				g.FillRectangle(brush, left, num3, x - left, fillH);
				return;
			}
		}
		using (SolidBrush brush2 = new SolidBrush(SignalTokens.Root.SignalStart))
		{
			g.FillRectangle(brush2, left, num3, Math.Min(x, num2) - left, fillH);
		}
		if (x <= num2)
		{
			return;
		}
		Color[] array = new Color[6]
		{
			SignalTokens.Root.Signal1,
			SignalTokens.Root.Signal2,
			SignalTokens.Root.Signal3,
			SignalTokens.Root.Signal4,
			SignalTokens.Root.Signal5,
			SignalTokens.Root.Signal6
		};
		int val = XFromValue(_maximum);
		float num4 = (float)fillH / 6f;
		for (int i = 0; i < array.Length; i++)
		{
			RectangleF rect = new RectangleF(num2, num3 + (float)i * num4, x - num2, num4);
			using LinearGradientBrush brush3 = new LinearGradientBrush(new PointF(num2, 0f), new PointF(Math.Max(num2 + 1, val), 0f), SignalTokens.Root.SignalStart, array[i]);
			g.FillRectangle(brush3, rect);
		}
	}
}
