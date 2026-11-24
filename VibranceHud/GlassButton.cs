using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class GlassButton : Control
{
	private sealed class ButtonAccessibleObject : ControlAccessibleObject
	{
		private readonly GlassButton _owner;

		public override AccessibleStates State => (AccessibleStates)((int)base.State | (_owner._loading ? 2048 : 0));

		public ButtonAccessibleObject(GlassButton owner)
			: base(owner)
		{
			_owner = owner;
		}
	}

	private readonly HoverFade _fade;

	private readonly HoverFade _press;

	private readonly OutsideFocusRing _ring;

	private GlassButtonKind _kind = GlassButtonKind.Secondary;

	private SignalIcon _icon;

	private SignalIcon _trailing;

	private bool _loading;

	private Stopwatch? _loadingSince;

	private Timer? _spin;

	private (string Text, Font Font, Size Size)? _measured;

	public GlassButtonKind Kind
	{
		get
		{
			return _kind;
		}
		set
		{
			if (_kind != value)
			{
				_kind = value;
				AccessibleRole = ((value == GlassButtonKind.Link) ? AccessibleRole.Link : AccessibleRole.PushButton);
				_measured = null;
				Invalidate();
			}
		}
	}

	public SignalIcon Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			if (_icon != value)
			{
				_icon = value;
				Invalidate();
			}
		}
	}

	public SignalIcon TrailingIcon
	{
		get
		{
			return _trailing;
		}
		set
		{
			if (_trailing != value)
			{
				_trailing = value;
				Invalidate();
			}
		}
	}

	public bool Loading
	{
		get
		{
			return _loading;
		}
		set
		{
			if (_loading == value)
			{
				return;
			}
			_loading = value;
			if (value)
			{
				_loadingSince = Stopwatch.StartNew();
				if (_spin == null)
				{
					_spin = new Timer
					{
						Interval = 16
					};
				}
				_spin.Tick -= OnSpin;
				_spin.Tick += OnSpin;
				_spin.Start();
			}
			else
			{
				_spin?.Stop();
				_loadingSince = null;
			}
			Invalidate();
			if (IsHandleCreated)
			{
				AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
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

	private Font LabelFont
	{
		get
		{
			if (Kind != GlassButtonKind.Link)
			{
				return Font;
			}
			return Fonts.Body;
		}
	}

	private bool HasLead
	{
		get
		{
			if (_icon == SignalIcon.None)
			{
				return SpinnerShowing;
			}
			return true;
		}
	}

	private bool SpinnerShowing
	{
		get
		{
			if (!SpinnerForTest)
			{
				if (_loading)
				{
					Stopwatch loadingSince = _loadingSince;
					if (loadingSince != null)
					{
						return loadingSince.ElapsedMilliseconds >= 300;
					}
				}
				return false;
			}
			return true;
		}
	}

	internal bool SpinnerForTest { get; set; }

	public GlassButton()
	{
		_fade = new HoverFade(this);
		_press = new HoverFade(this);
		_ring = new OutsideFocusRing(this, () => Radius(ControlInk.Dpi(this)));
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Height = 32;
		Font = Fonts.BodyBold;
		TabStop = true;
		SetStyle(ControlStyles.Selectable, value: true);
		AccessibleRole = AccessibleRole.PushButton;
	}

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new ButtonAccessibleObject(this);
	}

	private void OnSpin(object? sender, EventArgs e)
	{
		if (Visible && !IsDisposed)
		{
			Invalidate();
		}
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		AccessibleName = Text;
		_measured = null;
	}

	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
		_measured = null;
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

	protected override void OnClick(EventArgs e)
	{
		if (!_loading)
		{
			base.OnClick(e);
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

	public void PerformClick()
	{
		if (Enabled)
		{
			OnClick(EventArgs.Empty);
		}
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = (Enabled ? Cursors.Hand : Cursors.Default);
		_fade.Snap(hot: false);
		_press.Snap(hot: false);
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
		if (e.Button == MouseButtons.Left)
		{
			Focus();
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

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		_ring.Rehook();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_spin?.Dispose();
			_ring.Release();
		}
		base.Dispose(disposing);
	}

	private static float Radius(int dpi)
	{
		return ControlInk.PxF(dpi, 4f);
	}

	private Size LabelSize()
	{
		Font labelFont = LabelFont;
		(string, Font, Size)? measured = _measured;
		if (measured.HasValue)
		{
			(string, Font, Size) valueOrDefault = measured.GetValueOrDefault();
			if (valueOrDefault.Item1 == Text && valueOrDefault.Item2 == labelFont)
			{
				return valueOrDefault.Item3;
			}
		}
		Size size = (string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(Text, labelFont, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding));
		_measured = (Text, labelFont, size);
		return size;
	}

	public override Size GetPreferredSize(Size proposedSize)
	{
		int dpi = ControlInk.Dpi(this);
		if (Kind == GlassButtonKind.Icon)
		{
			return new Size(Height, Height);
		}
		int num = ContentWidth(dpi);
		if (Kind == GlassButtonKind.Link)
		{
			return new Size(num, Height);
		}
		int num2 = ControlInk.Px(dpi, (Kind == GlassButtonKind.Quiet) ? 8 : 12);
		return new Size(Math.Max(ControlInk.Px(dpi, (Height >= ControlInk.Px(dpi, 30)) ? 64 : 56), num + 2 * num2), Height);
	}

	private int ContentWidth(int dpi)
	{
		int num = ControlInk.Px(dpi, 16);
		int num2 = ControlInk.Px(dpi, 8);
		int num3 = LabelSize().Width;
		if (HasLead)
		{
			num3 += num + ((num3 > 0) ? num2 : 0);
		}
		if (_trailing != SignalIcon.None)
		{
			num3 += num2 + num;
		}
		return num3;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		int dpi = ControlInk.Dpi(this);
		float hot = (Enabled ? _fade.Value : 0f);
		float down = (Enabled ? _press.Value : 0f);
		(Color, Color, Color, Color) tuple = Look(hot, down);
		if (Kind != GlassButtonKind.Link)
		{
			ControlInk.PaintBox(graphics, ClientSize, Radius(dpi), ControlInk.Hairline(dpi), tuple.Item1, tuple.Item2);
		}
		DrawContent(graphics, dpi, tuple.Item3, tuple.Item4);
		if (Focused && ShowFocusCues)
		{
			_ring.Show(show: true);
		}
		else
		{
			_ring.Show(show: false);
		}
	}

	private (Color Fill, Color Edge, Color Ink, Color Underline) Look(float hot, float down)
	{
		Color transparent = Color.Transparent;
		if (!Enabled)
		{
			switch (Kind)
			{
			case GlassButtonKind.Primary:
				return (Fill: ControlInk.Surface2, Edge: ControlInk.Surface2, Ink: ControlInk.TextDisabledInk, Underline: transparent);
			case GlassButtonKind.Secondary:
			case GlassButtonKind.Destructive:
				return (Fill: transparent, Edge: ControlInk.TextDisabled, Ink: ControlInk.TextDisabledInk, Underline: transparent);
			default:
				return (Fill: transparent, Edge: transparent, Ink: ControlInk.TextDisabledInk, Underline: transparent);
			}
		}
		Color hover = ControlInk.Hover;
		Color item = ControlInk.Mix(ControlInk.Mix(Color.FromArgb(0, hover), hover, hot), ControlInk.Pressed, down);
		switch (Kind)
		{
		case GlassButtonKind.Primary:
		{
			Color color = ControlInk.Mix(ControlInk.Mix(ControlInk.Accent, ControlInk.AccentHover, hot), ControlInk.AccentPressed, down);
			return (Fill: color, Edge: color, Ink: ControlInk.OnAccent, Underline: transparent);
		}
		case GlassButtonKind.Destructive:
			return (Fill: item, Edge: ControlInk.Edge, Ink: ControlInk.Danger, Underline: transparent);
		case GlassButtonKind.Quiet:
		case GlassButtonKind.Icon:
			return (Fill: item, Edge: transparent, Ink: ControlInk.Mix(ControlInk.Text2, ControlInk.Text1, Math.Max(hot, down)), Underline: transparent);
		case GlassButtonKind.Link:
		{
			Color item2 = ControlInk.Mix(ControlInk.Text1, ControlInk.Text2, down);
			Color item3 = ControlInk.Mix(ControlInk.Mix(ControlInk.Edge, ControlInk.Text1, hot), ControlInk.Text2, down);
			return (Fill: transparent, Edge: transparent, Ink: item2, Underline: item3);
		}
		default:
			return (Fill: item, Edge: ControlInk.Edge, Ink: ControlInk.Text1, Underline: transparent);
		}
	}

	private void DrawContent(Graphics g, int dpi, Color ink, Color underline)
	{
		int num = ControlInk.Px(dpi, 16);
		int num2 = ControlInk.Px(dpi, 8);
		int num3 = Height / 2;
		if (Kind == GlassButtonKind.Icon)
		{
			RectangleF box = new RectangleF((float)(Width - num) / 2f, (float)num3 - (float)num / 2f, num, num);
			if (SpinnerShowing)
			{
				DrawSpinner(g, box, ink, dpi);
			}
			else
			{
				SignalIcons.Draw(g, _icon, box, ink);
			}
			return;
		}
		Size size = LabelSize();
		int num4 = (Width - ContentWidth(dpi)) / 2;
		if (HasLead)
		{
			RectangleF box2 = new RectangleF(num4, (float)num3 - (float)num / 2f, num, num);
			if (SpinnerShowing)
			{
				DrawSpinner(g, box2, ink, dpi);
			}
			else
			{
				SignalIcons.Draw(g, _icon, box2, ink);
			}
			num4 += num + ((size.Width > 0) ? num2 : 0);
		}
		if (size.Width > 0)
		{
			Font labelFont = LabelFont;
			TextRenderer.DrawText(g, Text, labelFont, new Rectangle(num4, 0, size.Width + 2, Height), ink, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			if (Kind == GlassButtonKind.Link && underline.A > 0)
			{
				FontFamily fontFamily = labelFont.FontFamily;
				float num5 = (float)(labelFont.Height * fontFamily.GetCellAscent(labelFont.Style)) / (float)fontFamily.GetLineSpacing(labelFont.Style);
				int num6 = (int)Math.Round((float)(Height - labelFont.Height) / 2f + num5);
				int y = Math.Min(Height - ControlInk.Hairline(dpi), num6 + ControlInk.Px(dpi, 3));
				using SolidBrush brush = new SolidBrush(underline);
				g.FillRectangle(brush, num4, y, size.Width, ControlInk.Hairline(dpi));
			}
			num4 += size.Width;
		}
		if (_trailing != SignalIcon.None)
		{
			num4 += num2;
			SignalIcons.Draw(g, _trailing, new RectangleF(num4, (float)num3 - (float)num / 2f, num, num), ink);
		}
	}

	private void DrawSpinner(Graphics g, RectangleF box, Color ink, int dpi)
	{
		float num = ControlInk.PxF(dpi, 1.5f);
		RectangleF rect = RectangleF.Inflate(box, 0f - num, 0f - num);
		float num2;
		if (SystemInformation.UIEffectsEnabled)
		{
			Stopwatch loadingSince = _loadingSince;
			if (loadingSince != null)
			{
				num2 = (float)(loadingSince.ElapsedMilliseconds % 800) / 800f * 360f;
				goto IL_004a;
			}
		}
		num2 = 0f;
		goto IL_004a;
		IL_004a:
		float num3 = num2;
		SmoothingMode smoothingMode = g.SmoothingMode;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		if (Kind != GlassButtonKind.Primary)
		{
			using Pen pen = new Pen(ControlInk.Track, num);
			g.DrawEllipse(pen, rect);
		}
		using (Pen pen2 = new Pen(ink, num)
		{
			StartCap = LineCap.Round,
			EndCap = LineCap.Round
		})
		{
			g.DrawArc(pen2, rect, num3 - 90f, 270f);
		}
		g.SmoothingMode = smoothingMode;
	}
}
