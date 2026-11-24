using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class NavButton : Control
{
	private sealed class NavAccessibleObject : ControlAccessibleObject
	{
		private readonly NavButton _owner;

		public override AccessibleStates State => (AccessibleStates)((int)(base.State | AccessibleStates.Selectable | AccessibleStates.Focusable) | (_owner._active ? 2 : 0));

		public NavAccessibleObject(NavButton owner)
			: base(owner)
		{
			_owner = owner;
		}
	}

	private bool _active;

	private readonly HoverFade _fade;

	private readonly HoverFade _press;

	public const int RowHeight = 58;

	private const int RowGap = 2;

	private const int PillW = 64;

	private const int PillRadius = 4;

	private const int IconPx = 20;

	private const int IconTopInPill = 8;

	private const int NameTopInPill = 32;

	private const int MarkerW = 3;

	private const int MarkerH = 20;

	private const int MarkerOut = 6;

	private const int FocusInset = 2;

	private const int FocusRadius = 2;

	private const int LockPx = 12;

	private const int LockInset = 4;

	public const int IconDisplay = 0;

	public const int IconProfiles = 1;

	public const int IconAccount = 3;

	public const int IconCrosshair = 5;

	public const int IconSettings = 6;

	public const int IconResolution = 7;

	public const int IconCustomize = 8;

	public const int IconMonitor = 9;

	public const int IconLibrary = 10;

	public const int IconDarkBoost = 11;

	private bool _locked;

	private float _labelOpacity = 1f;

	private static readonly PointF[] Cog = new PointF[32]
	{
		new PointF(15.7f, 8.47f),
		new PointF(18f, 8.73f),
		new PointF(18f, 11.27f),
		new PointF(15.7f, 11.53f),
		new PointF(15.11f, 12.95f),
		new PointF(16.55f, 14.76f),
		new PointF(14.76f, 16.55f),
		new PointF(12.95f, 15.11f),
		new PointF(11.53f, 15.7f),
		new PointF(11.27f, 18f),
		new PointF(8.73f, 18f),
		new PointF(8.47f, 15.7f),
		new PointF(7.05f, 15.11f),
		new PointF(5.24f, 16.55f),
		new PointF(3.45f, 14.76f),
		new PointF(4.89f, 12.95f),
		new PointF(4.3f, 11.53f),
		new PointF(2f, 11.27f),
		new PointF(2f, 8.73f),
		new PointF(4.3f, 8.47f),
		new PointF(4.89f, 7.05f),
		new PointF(3.45f, 5.24f),
		new PointF(5.24f, 3.45f),
		new PointF(7.05f, 4.89f),
		new PointF(8.47f, 4.3f),
		new PointF(8.73f, 2f),
		new PointF(11.27f, 2f),
		new PointF(11.53f, 4.3f),
		new PointF(12.95f, 4.89f),
		new PointF(14.76f, 3.45f),
		new PointF(16.55f, 5.24f),
		new PointF(15.11f, 7.05f)
	};

	public int IconKind { get; init; }

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

	public bool Locked
	{
		get
		{
			return _locked;
		}
		set
		{
			if (_locked != value)
			{
				_locked = value;
				AccessibleDescription = (value ? "Comes with a plan" : null);
				Invalidate();
			}
		}
	}

	public float LabelOpacity
	{
		get
		{
			return _labelOpacity;
		}
		set
		{
			float num;
			if (value < 0f)
			{
				num = 0f;
			}
			else
			{
				num = ((value > 1f) ? 1f : value);
			}
			if (!(Math.Abs(num - _labelOpacity) < 0.004f))
			{
				_labelOpacity = num;
				Invalidate();
			}
		}
	}

	private static int NameLinePx => Fonts.Step(FontRole.Caption).LinePx;

	private bool TwoLineName => Text.Contains(" & ", StringComparison.Ordinal);

	internal Rectangle PillForTest => Pill();

	internal Rectangle IconBoxForTest => IconBox();

	internal Rectangle NameBoxForTest => NameBox();

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

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new NavAccessibleObject(this);
	}

	public NavButton()
	{
		_fade = new HoverFade(this);
		_press = new HoverFade(this);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		Height = 58;
		Cursor = Cursors.Hand;
		BackColor = Color.Transparent;
		TabStop = true;
		SetStyle(ControlStyles.Selectable, value: true);
		AccessibleRole = AccessibleRole.PageTab;
	}

	private Rectangle Pill()
	{
		int num = Math.Min(Width, Tokens.Scale(64));
		return new Rectangle((Width - num) / 2, 0, num, Math.Max(1, Height - Tokens.Scale(2)));
	}

	private Rectangle IconBox()
	{
		Rectangle rectangle = Pill();
		int num = Tokens.Scale(TwoLineName ? 16 : 20);
		return new Rectangle(rectangle.X + (rectangle.Width - num) / 2, rectangle.Y + Tokens.Scale(TwoLineName ? 4 : 8), num, num);
	}

	private Rectangle NameBox()
	{
		Rectangle rectangle = Pill();
		return new Rectangle(rectangle.X, rectangle.Y + Tokens.Scale(TwoLineName ? 21 : 32), rectangle.Width, Tokens.Scale(TwoLineName ? 34 : NameLinePx));
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		AccessibleName = Text;
	}

	public void PerformClick()
	{
		OnClick(EventArgs.Empty);
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

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.PixelOffsetMode = PixelOffsetMode.Half;
		Rectangle rectangle = Pill();
		RectangleF rect = new RectangleF(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
		float radius = Tokens.ScaleF(4f);
		float num = (Enabled ? _fade.Value : 0f);
		float num2 = (Enabled ? _press.Value : 0f);
		Color color;
		if (_active)
		{
			color = ControlInk.Surface2;
		}
		else
		{
			Color hover = ControlInk.Hover;
			color = ControlInk.Mix(ControlInk.Mix(Color.FromArgb(0, hover), hover, num), ControlInk.Pressed, num2);
		}
		if (color.A > 0)
		{
			using GraphicsPath path = Glass.RoundedPath(rect, radius);
			using SolidBrush brush = new SolidBrush(color);
			graphics.FillPath(brush, path);
		}
		if (_active)
		{
			int num3 = Tokens.Scale(20);
			float num4 = Tokens.ScaleF(3f);
			using GraphicsPath path2 = Glass.RoundedPath(new RectangleF(-1f, (float)rectangle.Y + (float)(rectangle.Height - num3) / 2f, num4, num3), num4 / 2f);
			using SolidBrush brush2 = new SolidBrush(ControlInk.Accent);
			graphics.FillPath(brush2, path2);
		}
		if (Focused && ShowFocusCues)
		{
			float num5 = Tokens.ScaleF(2f);
			float num6 = Tokens.ScaleF(2f) + num5 / 2f;
			using GraphicsPath path3 = Glass.RoundedPath(RectangleF.Inflate(rect, 0f - num6, 0f - num6), Tokens.ScaleF(2f));
			using Pen pen = new Pen(ControlInk.Focus, num5);
			graphics.DrawPath(pen, path3);
		}
		DrawGlyph(graphics, IconBox(), _active || Math.Max(num, num2) > 0.5f);
		if (_locked)
		{
			int num7 = Tokens.Scale(12);
			int num8 = Tokens.Scale(4);
			SignalIcons.Draw(graphics, SignalIcon.Lock, new RectangleF(rectangle.Right - num8 - num7, rectangle.Y + num8, num7, num7), Theme.TextFaint);
		}
		if (_labelOpacity > 0.02f)
		{
			Color color2 = ControlInk.Mix(Theme.TextDim, Theme.Text, _active ? 1f : Math.Max(num, num2));
			if (_labelOpacity < 0.99f)
			{
				color2 = Color.FromArgb((int)((float)(int)color2.A * _labelOpacity), color2);
			}
			Font caption = Fonts.Caption;
			TextRenderer.DrawText(graphics, TwoLineName ? Text.Replace(" & ", "\n& ") : Text, caption, NameBox(), color2, (TextFormatFlags)(5 | (TwoLineName ? 16 : 32800) | 0x10000000 | 0x800));
		}
	}

	private void DrawGlyph(Graphics g, Rectangle box, bool bright)
	{
		float num = (float)box.Width / 20f;
		GraphicsState gstate = g.Save();
		try
		{
			g.TranslateTransform(box.X, box.Y);
			g.ScaleTransform(num, num);
			DrawIcon(g, IconKind, bright ? Theme.Text : Theme.TextDim);
		}
		finally
		{
			g.Restore(gstate);
		}
	}

	private static void DrawIcon(Graphics g, int kind, Color color)
	{
		using Pen pen = new Pen(color, 1.5f)
		{
			StartCap = LineCap.Round,
			EndCap = LineCap.Round,
			LineJoin = LineJoin.Round
		};
		using SolidBrush brush = new SolidBrush(color);
		switch (kind)
		{
		case 0:
			g.DrawEllipse(pen, 3f, 3f, 14f, 14f);
			g.FillPie(brush, 3f, 3f, 14f, 14f, -90f, 180f);
			break;
		case 10:
			g.DrawPolygon(pen, new PointF[4]
			{
				new PointF(10f, 2.8f),
				new PointF(17.2f, 6.7f),
				new PointF(10f, 10.6f),
				new PointF(2.8f, 6.7f)
			});
			g.DrawLines(pen, new PointF[3]
			{
				new PointF(2.8f, 10.2f),
				new PointF(10f, 14.1f),
				new PointF(17.2f, 10.2f)
			});
			g.DrawLines(pen, new PointF[3]
			{
				new PointF(2.8f, 13.6f),
				new PointF(10f, 17.5f),
				new PointF(17.2f, 13.6f)
			});
			break;
		case 7:
			Screen(g, pen);
			g.DrawLines(pen, new PointF[3]
			{
				new PointF(5.5f, 9f),
				new PointF(5.5f, 6.5f),
				new PointF(8f, 6.5f)
			});
			g.DrawLines(pen, new PointF[3]
			{
				new PointF(14.5f, 9f),
				new PointF(14.5f, 11.5f),
				new PointF(12f, 11.5f)
			});
			break;
		case 9:
			Screen(g, pen);
			g.DrawEllipse(pen, 7.75f, 6.75f, 4.5f, 4.5f);
			break;
		case 8:
		{
			using (GraphicsPath path3 = RoundedRect(2.5f, 3.5f, 15f, 13f, 1.5f))
			{
				g.DrawPath(pen, path3);
			}
			g.DrawLine(pen, 7.5f, 3.5f, 7.5f, 16.5f);
			g.DrawLine(pen, 10.5f, 8f, 14.5f, 8f);
			g.DrawLine(pen, 10.5f, 11f, 13.5f, 11f);
			break;
		}
		case 11:
		{
			using GraphicsPath path2 = Crescent();
			g.DrawPath(pen, path2);
			break;
		}
		case 5:
			g.DrawEllipse(pen, 4.5f, 4.5f, 11f, 11f);
			g.DrawLine(pen, 10f, 2f, 10f, 6.5f);
			g.DrawLine(pen, 10f, 13.5f, 10f, 18f);
			g.DrawLine(pen, 2f, 10f, 6.5f, 10f);
			g.DrawLine(pen, 13.5f, 10f, 18f, 10f);
			g.FillEllipse(brush, 9.1f, 9.1f, 1.8f, 1.8f);
			break;
		case 1:
		{
			using (GraphicsPath path = RoundedRect(1.5f, 6.25f, 17f, 8.25f, 4.125f))
			{
				g.DrawPath(pen, path);
			}
			g.DrawLine(pen, 4.6f, 10.4f, 8.4f, 10.4f);
			g.DrawLine(pen, 6.5f, 8.5f, 6.5f, 12.3f);
			g.FillEllipse(brush, 12.6f, 8.9f, 1.8f, 1.8f);
			g.FillEllipse(brush, 14.4f, 10.7f, 1.8f, 1.8f);
			break;
		}
		case 6:
			g.DrawPolygon(pen, Cog);
			g.DrawEllipse(pen, 7.6f, 7.6f, 4.8f, 4.8f);
			break;
		default:
			g.DrawEllipse(pen, 6.8f, 3.8f, 6.4f, 6.4f);
			g.DrawArc(pen, 3.8f, 10.8f, 12.4f, 12.4f, 180f, 180f);
			break;
		}
	}

	private static GraphicsPath Crescent()
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddArc(2.5f, 2.5f, 15f, 15f, 5f, 260f);
		graphicsPath.AddArc(8.208333f, 0.1333332f, 11.666666f, 11.666666f, -143.6f, -162.8f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	private static void Screen(Graphics g, Pen pen)
	{
		using GraphicsPath path = RoundedRect(2.5f, 4f, 15f, 10f, 1.5f);
		g.DrawPath(pen, path);
		g.DrawLine(pen, 7.5f, 17f, 12.5f, 17f);
		g.DrawLine(pen, 10f, 14f, 10f, 17f);
	}

	private static GraphicsPath RoundedRect(float x, float y, float w, float h, float radius)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		float num = radius * 2f;
		graphicsPath.AddArc(x, y, num, num, 180f, 90f);
		graphicsPath.AddArc(x + w - num, y, num, num, 270f, 90f);
		graphicsPath.AddArc(x + w - num, y + h - num, num, num, 0f, 90f);
		graphicsPath.AddArc(x, y + h - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}
}
