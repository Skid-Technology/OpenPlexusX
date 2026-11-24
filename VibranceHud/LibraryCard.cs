using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Crosshair;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class LibraryCard : Control
{
	private readonly record struct Verticals(int HeadTop, int HeadH, int BylineTop, int BylineH, int DescTop, int DescBottom, int SatTop, int SatH, int BarTop, int BarH, int FactsTop, int FactsH, int ActionTop, int ActionH);

	private sealed class CardAccessibleObject : ControlAccessibleObject
	{
		private readonly LibraryCard _owner;

		public override AccessibleStates State => (AccessibleStates)((int)base.State | (_owner._active ? 16 : 0));

		public CardAccessibleObject(LibraryCard owner)
			: base(owner)
		{
			_owner = owner;
		}
	}

	internal readonly record struct CardLayout(Rectangle Glyph, Rectangle Name, Rectangle Byline, Rectangle Description, Rectangle SaturationCaption, Rectangle SaturationValue, Rectangle Bar, Rectangle Facts, Rectangle Action, Rectangle Badge, Rectangle Preview);

	private GlassButton? _reportButton;

	private const int Edge = 1;

	private const int PadY = 16;

	private const int PadX = 16;

	private const int HeadRow = 28;

	private const int BylineGap = 0;

	private const int DescGap = 8;

	private const int DescMin = 40;

	private const int SatGap = 8;

	private const int SatRow = 20;

	private const int BarGap = 8;

	private const int FooterGap = 8;

	private const int FooterRow = 28;

	private const int StackGap = 8;

	private const int RulerW = 266;

	private const int ButtonPadX = 12;

	private const int ButtonMinW = 56;

	private const int StatusIcon = 16;

	private const int StatusGap = 8;

	internal const int Chrome = 188;

	internal const int SidePad = 17;

	private const int CaptionLine = 16;

	private const int DescriptionLead = 20;

	private const int BarHeightLogical = 6;

	internal const int FullScale = 300;

	private const int Untouched = 100;

	private bool _active;

	private readonly HoverFade _fade;

	private string _caption = "";

	private string _author = "";

	private string _subtitle = "";

	private SignalToolTip? _titleTip;

	private string _titleTipText = "";

	private readonly OutsideFocusRing _ring;

	internal static int SharedFooterSaving => Row(FontRole.Caption) + S(8);

	internal static int MinimumDescription => S(40);

	public string Caption
	{
		get
		{
			return _caption;
		}
		set
		{
			if (value == null)
			{
				value = "";
			}
			if (!(_caption == value))
			{
				_caption = value;
				SyncAccessibility();
			}
		}
	}

	public string Author
	{
		get
		{
			return _author;
		}
		set
		{
			if (value == null)
			{
				value = "";
			}
			if (!(_author == value))
			{
				_author = value;
				SyncAccessibility();
			}
		}
	}

	public string Subtitle
	{
		get
		{
			return _subtitle;
		}
		set
		{
			if (value == null)
			{
				value = "";
			}
			if (!(_subtitle == value))
			{
				_subtitle = value;
				SyncAccessibility();
			}
		}
	}

	internal string TitleTipForTest => _titleTipText;

	public string Kind { get; set; } = "balanced";

	public int Saturation { get; set; }

	public int Vibrance { get; set; }

	public int Temperature { get; set; }

	public float[]? Matrix { get; set; }

	public CrosshairConfig? CrosshairPreview { get; set; }

	private bool ShowsPreview => CrosshairPreview != null;

	public string Footer { get; set; } = "";

	public bool Active
	{
		get
		{
			return _active;
		}
		set
		{
			if (Locked)
			{
				value = false;
			}
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

	public bool Locked { get; set; }

	internal string ActionLabel
	{
		get
		{
			if (!_active)
			{
				return "Apply";
			}
			return "In use";
		}
	}

	internal string BadgeLabel
	{
		get
		{
			if (!_active)
			{
				return "";
			}
			return "Applied";
		}
	}

	internal string SaturationText
	{
		get
		{
			if (Saturation <= 0)
			{
				return "";
			}
			return Saturation.ToString(CultureInfo.InvariantCulture) + "%";
		}
	}

	internal string FactsText
	{
		get
		{
			if (Footer.Length > 0)
			{
				return Footer;
			}
			if (Saturation <= 0)
			{
				return "";
			}
			return "Vibrance " + Vibrance.ToString(CultureInfo.InvariantCulture) + " · Temp " + SignedTemperature(Temperature);
		}
	}

	private static int HeadH => Math.Max(S(28), Row(FontRole.BodyBold));

	private static int SatH => Math.Max(S(20), Row(FontRole.BodyBold));

	private static Color LabelInk
	{
		get
		{
			if (!(Theme.CurrentName == "Slate"))
			{
				return SignalTokens.Ash.Text3;
			}
			return SignalTokens.Slate.Text3;
		}
	}

	private static Color[] Strands => new Color[6]
	{
		SignalTokens.Root.Signal1,
		SignalTokens.Root.Signal2,
		SignalTokens.Root.Signal3,
		SignalTokens.Root.Signal4,
		SignalTokens.Root.Signal5,
		SignalTokens.Root.Signal6
	};

	public new event EventHandler? Click;

	internal void OfferReport(Action report)
	{
		_reportButton = new GlassButton
		{
			Text = "Report",
			AccessibleName = "Report " + Caption,
			Kind = GlassButtonKind.Quiet,
			TabStop = true
		};
		_reportButton.Click += (object? _, EventArgs _) =>
		{
			report();
		};
		Controls.Add(_reportButton);
		PlaceReportButton();
	}

	private void PlaceReportButton()
	{
		if (_reportButton != null)
		{
			Rectangle byline = LayOut().Byline;
			_reportButton.Bounds = new Rectangle(Width - S(17) - ButtonWidth("Report"), byline.Top, ButtonWidth("Report"), byline.Height);
		}
	}

	internal static int HeightForDescription(int descHeight)
	{
		return Math.Max(204, descHeight + 188) + 2;
	}

	internal static int FixedChrome(bool sharedFooter)
	{
		Verticals verticals = Vertical(0, sharedFooter);
		return verticals.DescTop - verticals.DescBottom;
	}

	private static Verticals Vertical(int height, bool shared)
	{
		int num = S(17);
		int headH = HeadH;
		int num2 = Row(FontRole.Caption);
		int num3 = Math.Max(S(45), num + headH);
		int descTop = Math.Max(S(69), num3 + num2);
		int num4 = S(28);
		int num5 = height - S(45);
		int num6 = (shared ? num4 : Row(FontRole.Caption));
		int num7 = (shared ? num5 : (num5 - S(8) - num6));
		int num8 = 53 + ((!shared) ? 24 : 0);
		int num9 = S(6);
		int num10 = Math.Min(height - S(num8 + 6), num7 - S(8) - num9);
		int satH = SatH;
		int num11 = Math.Min(height - S(num8 + 6 + 8 + 20), num10 - S(8) - satH);
		return new Verticals(num, headH, num3, num2, descTop, num11 - S(8), num11, satH, num10, num9, num7, num6, num5, num4);
	}

	internal static bool FooterSharesARow(int width, string facts, string action)
	{
		int num = width - 2 * S(17);
		if (num <= 0)
		{
			return false;
		}
		int width2 = TextRenderer.MeasureText(facts, Fonts.Caption).Width;
		int num2 = Math.Max(ButtonWidth(action), StatusWidth(action));
		return width2 + S(12) + num2 <= num;
	}

	private void SyncAccessibility()
	{
		AccessibleName = ((_author.Length > 0) ? (_caption + " by " + _author) : _caption);
		AccessibleDescription = ((_subtitle.Length > 0) ? _subtitle : null);
		SyncTitleTip();
	}

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new CardAccessibleObject(this);
	}

	private void SyncTitleTip()
	{
		string text = "";
		if (_caption.Length > 0 && !IsDisposed)
		{
			int width = LayOut().Name.Width;
			if (Advance(_caption, Fonts.BodyBold) > width)
			{
				text = _caption;
			}
		}
		if (text == _titleTipText)
		{
			return;
		}
		_titleTipText = text;
		if (text.Length == 0)
		{
			_titleTip?.SetToolTip(this, null);
			return;
		}
		if (_titleTip == null)
		{
			_titleTip = new SignalToolTip
			{
				InitialDelay = 420
			};
		}
		_titleTip.SetToolTip(this, text);
	}

	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		PlaceReportButton();
		SyncTitleTip();
	}

	internal void PressForTest()
	{
		Click?.Invoke(this, EventArgs.Empty);
	}

	internal static string SignedTemperature(int temperature)
	{
		if (temperature <= 0)
		{
			if (temperature < 0)
			{
				return "−" + (-temperature).ToString(CultureInfo.InvariantCulture);
			}
			return "0";
		}
		return "+" + temperature.ToString(CultureInfo.InvariantCulture);
	}

	public LibraryCard()
	{
		_fade = new HoverFade(this);
		_ring = new OutsideFocusRing(this, () => S(8));
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Size = new Size(215, HeightForDescription(32));
		TabStop = true;
		AccessibleRole = AccessibleRole.RadioButton;
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
		Invalidate();
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
		{
			Click?.Invoke(this, EventArgs.Empty);
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Space)
		{
			Click?.Invoke(this, EventArgs.Empty);
			e.Handled = true;
		}
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
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
			_titleTip?.Dispose();
			_titleTip = null;
		}
		base.Dispose(disposing);
	}

	private static int LineBox(FontRole role)
	{
		return Tokens.Scale(Fonts.Step(role).LinePx);
	}

	private static int Row(FontRole role)
	{
		return Math.Max(LineBox(role), TextRenderer.MeasureText("Hg", Fonts.For(role)).Height);
	}

	private static int S(int logical)
	{
		return Tokens.Scale(logical);
	}

	private static int Advance(string text, Font font)
	{
		return TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
	}

	private static int ButtonWidth(string label)
	{
		return Math.Max(S(56), Advance(label, Fonts.BodyBold) + 2 * S(12));
	}

	private static int StatusWidth(string label)
	{
		return S(16) + S(8) + Advance(label, Fonts.BodyBold);
	}

	internal CardLayout TestLayout()
	{
		return LayOut();
	}

	private CardLayout LayOut()
	{
		int num = S(17);
		int num2 = Math.Max(1, Width - 2 * num);
		bool flag = FooterSharesARow(Width, FactsText, ActionLabel);
		Verticals verticals = Vertical(Height, flag);
		int num3 = Math.Min(Math.Max(0, num2 - S(32) - S(24)), S(16) + S(6) + Advance("Applied", Fonts.Body) + S(2));
		Rectangle rectangle = new Rectangle(num + num2 - num3, verticals.HeadTop, num3, verticals.HeadH);
		Rectangle badge = ((BadgeLabel.Length > 0) ? rectangle : Rectangle.Empty);
		int width = Math.Max(1, rectangle.Left - S(12) - num);
		Rectangle name = new Rectangle(num, verticals.HeadTop, width, verticals.HeadH);
		Rectangle byline = new Rectangle(num, verticals.BylineTop, Math.Max(1, num2 - ((_reportButton != null) ? (ButtonWidth("Report") + S(8)) : 0)), verticals.BylineH);
		int num4 = (_active ? Math.Min(num2, StatusWidth(ActionLabel)) : Math.Min(num2, ButtonWidth(ActionLabel)));
		Rectangle action = new Rectangle(num + num2 - num4, verticals.ActionTop, num4, verticals.ActionH);
		Rectangle facts = (flag ? new Rectangle(num, verticals.FactsTop, Math.Max(1, action.Left - S(12) - num), verticals.FactsH) : new Rectangle(num, verticals.FactsTop, num2, verticals.FactsH));
		Rectangle bar = new Rectangle(num, verticals.BarTop, Math.Min(num2, S(266)), verticals.BarH);
		int satTop = verticals.SatTop;
		int satH = verticals.SatH;
		int num5 = Math.Min(num2 / 2, Math.Max(S(32), TextRenderer.MeasureText(SaturationText, Fonts.BodyBold).Width));
		Rectangle saturationCaption = new Rectangle(num, satTop, num2 - num5, satH);
		Rectangle saturationValue = new Rectangle(saturationCaption.Right, satTop, num5, satH);
		Rectangle preview = Rectangle.Empty;
		if (ShowsPreview)
		{
			preview = new Rectangle(num, satTop, num2, bar.Bottom - satTop);
			saturationCaption = (saturationValue = (bar = Rectangle.Empty));
		}
		return new CardLayout(Description: new Rectangle(num, verticals.DescTop, num2, Math.Max(1, verticals.DescBottom - verticals.DescTop)), Glyph: Rectangle.Empty, Name: name, Byline: byline, SaturationCaption: saturationCaption, SaturationValue: saturationValue, Bar: bar, Facts: facts, Action: action, Badge: badge, Preview: preview);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		PaintGuard.Try(this, () =>
		{
			PaintCard(g);
		});
		base.OnPaint(e);
	}

	private void PaintCard(Graphics g)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		CardLayout cardLayout = LayOut();
		PaintBody(g);
		TextRenderer.DrawText(g, Caption, Fonts.BodyBold, cardLayout.Name, Theme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		if (Author.Length > 0)
		{
			TextRenderer.DrawText(g, "by " + Author, Fonts.Caption, cardLayout.Byline, LabelInk, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
		if (!cardLayout.Badge.IsEmpty)
		{
			DrawBadge(g, cardLayout.Badge);
		}
		DrawDescription(g, cardLayout.Description);
		if (ShowsPreview)
		{
			DrawPreview(g, cardLayout.Preview);
		}
		else if (Saturation > 0)
		{
			TextRenderer.DrawText(g, "Saturation", Fonts.Caption, cardLayout.SaturationCaption, LabelInk, TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
			TextRenderer.DrawText(g, SaturationText, Fonts.BodyBold, cardLayout.SaturationValue, Theme.Text, TextFormatFlags.NoClipping | TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			DrawSignal(g, cardLayout.Bar);
		}
		else if (Footer.Length == 0)
		{
			DrawSweep(g, cardLayout.Bar);
		}
		if (FactsText.Length > 0)
		{
			TextRenderer.DrawText(g, FactsText, Fonts.Caption, cardLayout.Facts, LabelInk, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
		DrawAction(g, cardLayout.Action);
	}

	private void PaintBody(Graphics g)
	{
		RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
		float radius = S(8);
		using GraphicsPath path = Glass.RoundedPath(rect, radius);
		if (SlateSurface.Active)
		{
			Glass.PaintPanel(g, rect, radius, Glass.BackdropOffsetOf(this));
		}
		else
		{
			using SolidBrush brush = new SolidBrush(Theme.Surface);
			g.FillPath(brush, path);
		}
		float t = (_active ? 1f : (0.45f * _fade.Value));
		using (Pen pen = new Pen(Blend(Theme.Border, Theme.GlassEdge, t), S(1)))
		{
			g.DrawPath(pen, path);
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

	private static Color Blend(Color from, Color to, float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return Color.FromArgb((int)Math.Round((float)(int)from.A + (float)(to.A - from.A) * t), (int)Math.Round((float)(int)from.R + (float)(to.R - from.R) * t), (int)Math.Round((float)(int)from.G + (float)(to.G - from.G) * t), (int)Math.Round((float)(int)from.B + (float)(to.B - from.B) * t));
	}

	internal static int DescriptionHeight(string text, int width)
	{
		return WrapDescription(text, width, int.MaxValue).Count * S(20);
	}

	private static List<string> WrapDescription(string text, int width, int maxLines)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrEmpty(text) || width <= 0 || maxLines <= 0)
		{
			return list;
		}
		Font body = Fonts.Body;
		StringBuilder stringBuilder = new StringBuilder();
		string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			string text3 = ((stringBuilder.Length == 0) ? text2 : (stringBuilder?.ToString() + " " + text2));
			if (stringBuilder.Length > 0 && Advance(text3, body) > width)
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
				stringBuilder.Append(text2);
				if (list.Count == maxLines)
				{
					return list;
				}
			}
			else
			{
				stringBuilder.Clear();
				stringBuilder.Append(text3);
			}
		}
		if (list.Count < maxLines && stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list;
	}

	private void DrawDescription(Graphics g, Rectangle box)
	{
		if (Subtitle.Length != 0 && box.Width > 0 && box.Height > 0)
		{
			int num = S(20);
			int maxLines = Math.Max(1, box.Height / Math.Max(1, num));
			List<string> list = WrapDescription(Subtitle, box.Width, maxLines);
			for (int i = 0; i < list.Count; i++)
			{
				bool flag = i == list.Count - 1;
				TextFormatFlags flags = (TextFormatFlags)(0x10000804 | (flag ? 32768 : 0));
				TextRenderer.DrawText(g, list[i], Fonts.Body, new Rectangle(box.X, box.Y + i * num, box.Width, num), Theme.TextDim, flags);
			}
		}
	}

	private void DrawPreview(Graphics g, Rectangle box)
	{
		if (CrosshairPreview == null || box.Width < 8 || box.Height < 8)
		{
			return;
		}
		using GraphicsPath path = Glass.RoundedPath(new RectangleF(box.X, box.Y, box.Width, box.Height), S(4));
		using Region clip = g.Clip;
		g.SetClip(path, CombineMode.Intersect);
		CrosshairRender.DrawCheckerboard(g, box, S(8));
		CrosshairRender.Draw(g, CrosshairPreview, box);
		g.Clip = clip;
	}

	private static void DrawBadge(Graphics g, Rectangle box)
	{
		int num = Advance("Applied", Fonts.Body);
		int num2 = S(16);
		int x = box.Right - num - S(6) - num2;
		LineIcons.Check(g, new Rectangle(x, box.Y + (box.Height - num2) / 2, num2, num2), Theme.Text);
		TextRenderer.DrawText(g, "Applied", Fonts.Body, new Rectangle(box.Right - num, box.Y, num + S(2), box.Height), Theme.Text, TextFormatFlags.NoClipping | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
	}

	private void DrawAction(Graphics g, Rectangle box)
	{
		if (Locked)
		{
			int num = S(16);
			SignalIcons.Draw(g, SignalIcon.Lock, new RectangleF((float)box.X + (float)(box.Width - num) / 2f, (float)box.Y + (float)(box.Height - num) / 2f, num, num), LabelInk);
			return;
		}
		if (_active)
		{
			int num2 = S(16);
			int num3 = Advance(ActionLabel, Fonts.BodyBold);
			int num4 = box.Right - num3 - S(8) - num2;
			SignalIcons.Draw(g, SignalIcon.Success, new RectangleF(num4, (float)box.Y + (float)(box.Height - num2) / 2f, num2, num2), ControlInk.Success);
			TextRenderer.DrawText(g, ActionLabel, Fonts.BodyBold, new Rectangle(box.Right - num3, box.Y, num3 + S(2), box.Height), Theme.Text, TextFormatFlags.NoClipping | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			return;
		}
		using GraphicsPath path = Glass.RoundedPath(new RectangleF((float)box.X + 0.5f, (float)box.Y + 0.5f, box.Width - 1, box.Height - 1), S(4));
		if (_fade.Value > 0.01f)
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb((int)(255f * _fade.Value), Theme.SurfaceHover));
			g.FillPath(brush, path);
		}
		using (Pen pen = new Pen(Theme.GlassEdge, S(1)))
		{
			g.DrawPath(pen, path);
		}
		TextRenderer.DrawText(g, ActionLabel, Fonts.BodyBold, box, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoClipping | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
	}

	private void DrawSignal(Graphics g, Rectangle bar)
	{
		if (bar.Width < 8 || bar.Height < 3)
		{
			return;
		}
		SmoothingMode smoothingMode = g.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
		g.SmoothingMode = SmoothingMode.None;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		try
		{
			int num = Math.Clamp(Saturation, 0, 300);
			float num2 = At(bar, num);
			float num3 = At(bar, 100);
			if (num < 300)
			{
				int num4 = Math.Max(1, S(2));
				using SolidBrush brush = new SolidBrush(Theme.AccentDim);
				g.FillRectangle(brush, num2, (float)bar.Y + (float)bar.Height / 2f - (float)num4 / 2f, (float)bar.Right - num2, num4);
			}
			using (SolidBrush brush2 = new SolidBrush(SignalTokens.Root.SignalStart))
			{
				g.FillRectangle(brush2, bar.X, bar.Y, Math.Min(num2, num3) - (float)bar.X, bar.Height);
			}
			if (num <= 100)
			{
				return;
			}
			Color[] strands = Strands;
			for (int i = 0; i < strands.Length; i++)
			{
				int num5 = bar.Y + i * bar.Height / strands.Length;
				int num6 = Math.Max(num5 + 1, bar.Y + (i + 1) * bar.Height / strands.Length);
				using LinearGradientBrush brush3 = new LinearGradientBrush(new RectangleF(num3, num5 - 1, (float)(bar.X + bar.Width) - num3, num6 - num5 + 2), SignalTokens.Root.SignalStart, strands[i], LinearGradientMode.Horizontal)
				{
					WrapMode = WrapMode.TileFlipX
				};
				g.FillRectangle(brush3, num3, num5, num2 - num3, num6 - num5);
			}
		}
		finally
		{
			g.SmoothingMode = smoothingMode;
			g.PixelOffsetMode = pixelOffsetMode;
		}
	}

	private static float At(Rectangle bar, int value)
	{
		return (float)bar.X + (float)bar.Width * ((float)value / 300f);
	}

	private void DrawSweep(Graphics g, Rectangle bar)
	{
		if (bar.Width < 8 || bar.Height < 2)
		{
			return;
		}
		Color[] strands = Strands;
		Color[] array = new Color[strands.Length];
		for (int i = 0; i < strands.Length; i++)
		{
			array[i] = Transform(strands[i]);
		}
		using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new Rectangle(bar.X, bar.Y - 1, bar.Width, bar.Height + 2), array[0], array[^1], LinearGradientMode.Horizontal);
		ColorBlend colorBlend = new ColorBlend(strands.Length)
		{
			Colors = array,
			Positions = new float[strands.Length]
		};
		for (int j = 0; j < strands.Length; j++)
		{
			colorBlend.Positions[j] = (float)j / (float)(strands.Length - 1);
		}
		linearGradientBrush.InterpolationColors = colorBlend;
		linearGradientBrush.WrapMode = WrapMode.TileFlipXY;
		g.FillRectangle(linearGradientBrush, bar);
	}

	private Color Transform(Color c)
	{
		float[] matrix = Matrix;
		if (matrix == null || matrix.Length < 25)
		{
			return c;
		}
		float num = (float)(int)c.R / 255f;
		float num2 = (float)(int)c.G / 255f;
		float num3 = (float)(int)c.B / 255f;
		float v = num * matrix[0] + num2 * matrix[5] + num3 * matrix[10] + matrix[20];
		float v2 = num * matrix[1] + num2 * matrix[6] + num3 * matrix[11] + matrix[21];
		float v3 = num * matrix[2] + num2 * matrix[7] + num3 * matrix[12] + matrix[22];
		return Color.FromArgb(Byte(v), Byte(v2), Byte(v3));
	}

	private static int Byte(float v)
	{
		return Math.Clamp((int)Math.Round(v * 255f), 0, 255);
	}
}
