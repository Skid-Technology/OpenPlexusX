using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Pages;

internal sealed class SceneTile : Control
{
	private sealed class TileAccessible : ControlAccessibleObject
	{
		private readonly SceneTile _tile;

		public override AccessibleRole Role => AccessibleRole.RadioButton;

		public override AccessibleStates State => (AccessibleStates)((int)base.State | (_tile.Active ? 16 : 0));

		public override string DefaultAction => "Select";

		internal TileAccessible(SceneTile tile)
			: base(tile)
		{
			_tile = tile;
		}

		public override void DoDefaultAction()
		{
			_tile.InvokeOnClick(_tile, EventArgs.Empty);
		}
	}

	private const int PhotoPx = 30;

	private const float NameStartPt = 9f;

	private const float NameFloorPt = 7.5f;

	private const float NameStepPt = 0.25f;

	private const float PillPt = 7f;

	private const float ChipPt = 7f;

	private const float NominalPt = 9f;

	private static readonly Dictionary<(string, float, FontStyle), Font> ScaledFonts = new Dictionary<(string, float, FontStyle), Font>();

	private const TextFormatFlags Measure = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

	private static readonly int[] StripRgb = new int[6] { 16726832, 16749824, 16766474, 3458905, 2001151, 15902074 };

	internal const int StripSegments = 6;

	private const int ActiveDotRgb = 6213251;

	private Image? _photo;

	private float[]? _matrix;

	private bool _lookOnPhoto;

	private bool _active;

	private bool _hover;

	private int _reading;

	private string? _boundHotkey;

	private Bitmap? _thumb;

	private Size _thumbFor;

	private Font? _nameFont;

	private bool _nameCut;

	private (string Text, int Width, Font Basis)? _nameFitFor;

	private readonly OutsideFocusRing _ring;

	internal static int PictureHeight => Tokens.Scale(30);

	private static int Pad => Tokens.Scale(6);

	private static int CardRadius => Tokens.Scale(8);

	private static int PhotoRadius => Tokens.Scale(4);

	private static int StripGap => Tokens.Scale(4);

	private static int StripHeight => Tokens.Scale(5);

	private static int SegmentGap => Tokens.Scale(2);

	private static int NameGap => Tokens.Scale(5);

	private static int NameLine => Tokens.Scale(14);

	private static int PillGap => Tokens.Scale(4);

	private static int PillHeight => Tokens.Scale(14);

	private static int ChipHeight => Tokens.Scale(13);

	internal static int BoxHeight => Pad + PictureHeight + StripGap + StripHeight + NameGap + NameLine + PillGap + PillHeight + Pad;

	internal static int ControlHeight => BoxHeight;

	public string Caption
	{
		get
		{
			return Text;
		}
		set
		{
			Text = value;
			Rename();
			Invalidate();
		}
	}

	public string Subtitle { get; set; } = "";

	public string Kind { get; set; } = "";

	public int Reading
	{
		get
		{
			return _reading;
		}
		set
		{
			_reading = value;
			Rename();
			Invalidate();
		}
	}

	public Image? Photo
	{
		get
		{
			return _photo;
		}
		set
		{
			if (_photo != value)
			{
				_photo = value;
				DropThumb();
				Invalidate();
			}
		}
	}

	public bool ShowsTheLookOnThePhoto
	{
		get
		{
			return _lookOnPhoto;
		}
		set
		{
			if (_lookOnPhoto != value)
			{
				_lookOnPhoto = value;
				DropThumb();
				Invalidate();
			}
		}
	}

	public float[]? Matrix
	{
		get
		{
			return _matrix;
		}
		set
		{
			_matrix = value;
			DropThumb();
			Invalidate();
		}
	}

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

	public bool Deletable { get; set; }

	public bool Bindable { get; set; }

	public string? BoundHotkey
	{
		get
		{
			return _boundHotkey;
		}
		set
		{
			if (!(_boundHotkey == value))
			{
				_boundHotkey = value;
				Invalidate();
			}
		}
	}

	private Rectangle PhotoRect => new Rectangle(Pad, Pad, Math.Max(1, Width - 2 * Pad), PictureHeight);

	internal Rectangle TestBox => PhotoRect;

	private Rectangle StripRect => new Rectangle(PhotoRect.X, PhotoRect.Bottom + StripGap, PhotoRect.Width, StripHeight);

	internal Rectangle TestStrip => StripRect;

	private Rectangle NameRect => new Rectangle(Pad, StripRect.Bottom + NameGap, Math.Max(1, Width - 2 * Pad), NameLine);

	internal bool TestShowsActiveChip => _active;

	internal bool TestShowsHotkeyChip => !string.IsNullOrEmpty(_boundHotkey);

	internal float TestNamePoints
	{
		get
		{
			FitName();
			return _nameFont.SizeInPoints / Fonts.CaptionBold.SizeInPoints * 9f;
		}
	}

	internal bool TestNameIsCut
	{
		get
		{
			FitName();
			return _nameCut;
		}
	}

	public event EventHandler? DeleteRequested;

	public event EventHandler? BindRequested;

	private static Font House(Font basis, float pt, FontStyle? style = null)
	{
		float num = basis.SizeInPoints * pt / 9f;
		(string, float, FontStyle) key = (basis.FontFamily.Name, num, style ?? basis.Style);
		lock (ScaledFonts)
		{
			if (!ScaledFonts.TryGetValue(key, out Font value))
			{
				value = (ScaledFonts[key] = new Font(basis.FontFamily, num, style ?? basis.Style, GraphicsUnit.Point));
			}
			return value;
		}
	}

	private static int TextWidth(string text, Font font)
	{
		return TextRenderer.MeasureText(text, font, new Size(1073741823, 1073741823), TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
	}

	internal static Color[] StripColours(float[]? m)
	{
		Color[] array = new Color[StripRgb.Length];
		for (int i = 0; i < array.Length; i++)
		{
			Color color = Color.FromArgb(StripRgb[i]);
			float r = (float)(int)color.R / 255f;
			float g = (float)(int)color.G / 255f;
			float b = (float)(int)color.B / 255f;
			if (m != null && m.Length >= 25)
			{
				float num = Out(0);
				float num2 = Out(1);
				float num3 = Out(2);
				r = num;
				g = num2;
				b = num3;
			}
			array[i] = Color.FromArgb(255, (int)Math.Round(r * 255f), (int)Math.Round(g * 255f), (int)Math.Round(b * 255f));
			float Out(int c)
			{
				return Math.Clamp(r * m[c] + g * m[5 + c] + b * m[10 + c] + m[20 + c], 0f, 1f);
			}
		}
		return array;
	}

	public SceneTile()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.StandardClick, value: false);
		_ring = new OutsideFocusRing(this, () => CardRadius);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		TabStop = true;
		AccessibleRole = AccessibleRole.RadioButton;
	}

	private void Rename()
	{
		AccessibleName = string.Format(CultureInfo.InvariantCulture, "{0}, {1} percent", Text, _reading);
	}

	internal RectangleF TestSegment(int i)
	{
		return Segment(StripRect, i);
	}

	private static RectangleF Segment(Rectangle strip, int i)
	{
		float num = (float)(strip.Width - 5 * SegmentGap) / 6f;
		return new RectangleF((float)strip.X + (float)i * (num + (float)SegmentGap), strip.Y, num, strip.Height);
	}

	private void FitName()
	{
		Font captionBold = Fonts.CaptionBold;
		int width = NameRect.Width;
		(string, int, Font)? nameFitFor = _nameFitFor;
		if (nameFitFor.HasValue)
		{
			(string, int, Font) valueOrDefault = nameFitFor.GetValueOrDefault();
			if (valueOrDefault.Item1 == Text && valueOrDefault.Item2 == width && valueOrDefault.Item3 == captionBold && _nameFont != null)
			{
				return;
			}
		}
		Font nameFont = House(captionBold, 7.5f);
		_nameCut = true;
		for (float num = 9f; num >= 7.499f; num -= 0.25f)
		{
			Font font = House(captionBold, num);
			if (TextWidth(Text, font) <= width)
			{
				nameFont = font;
				_nameCut = false;
				break;
			}
		}
		_nameFont = nameFont;
		_nameFitFor = (Text, width, captionBold);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		_hover = true;
		Invalidate();
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		_hover = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnGotFocus(EventArgs e)
	{
		Invalidate();
		base.OnGotFocus(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		Invalidate();
		base.OnLostFocus(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (ClientRectangle.Contains(e.Location))
		{
			if (e.Button == MouseButtons.Left)
			{
				OnClick(EventArgs.Empty);
			}
			else if (e.Button == MouseButtons.Right)
			{
				ShowMenu(e.Location);
			}
		}
	}

	private void ShowMenu(Point at)
	{
		if (!Bindable && !Deletable)
		{
			return;
		}
		ContextMenuStrip menu = SignalMenu.ActionMenu();
		if (Bindable)
		{
			menu.Items.Add((BoundHotkey == null) ? "Set hotkey" : "Change hotkey", null, (object? _, EventArgs _) =>
			{
				BindRequested?.Invoke(this, EventArgs.Empty);
			});
		}
		if (Deletable)
		{
			menu.Items.Add("Delete look", null, (object? _, EventArgs _) =>
			{
				DeleteRequested?.Invoke(this, EventArgs.Empty);
			});
		}
		menu.Closed += (object? _, ToolStripDropDownClosedEventArgs _) =>
		{
			if (!menu.IsDisposed)
			{
				BeginInvoke(menu.Dispose);
			}
		};
		menu.Show(this, at);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (Deletable && e.KeyCode == Keys.Delete)
		{
			DeleteRequested?.Invoke(this, EventArgs.Empty);
			e.Handled = true;
		}
		else if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Space)
		{
			OnClick(EventArgs.Empty);
			e.Handled = true;
		}
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new TileAccessible(this);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		SmoothingMode smoothingMode = graphics.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = graphics.PixelOffsetMode;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.PixelOffsetMode = PixelOffsetMode.Half;
		Rectangle rectangle = new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height));
		using (GraphicsPath path = Glass.RoundedPath(rectangle, CardRadius))
		{
			using SolidBrush brush = new SolidBrush(ControlInk.Overlay);
			graphics.FillPath(brush, path);
		}
		Rectangle photoRect = PhotoRect;
		if (_photo == null)
		{
			using GraphicsPath path2 = Glass.RoundedPath(photoRect, PhotoRadius);
			using SolidBrush brush2 = new SolidBrush(SignalInk.PreviewWell);
			graphics.FillPath(brush2, path2);
		}
		if (_photo != null && photoRect.Width > 1 && photoRect.Height > 1)
		{
			graphics.PixelOffsetMode = PixelOffsetMode.Default;
			graphics.DrawImageUnscaled(Thumb(photoRect.Size), photoRect.Location);
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
		}
		DrawChips(graphics, photoRect);
		Rectangle stripRect = StripRect;
		Color[] array = StripColours(_matrix);
		float radius = (float)Tokens.Scale(3) / 2f;
		for (int i = 0; i < 6; i++)
		{
			using GraphicsPath path3 = Glass.RoundedPath(Segment(stripRect, i), radius);
			using SolidBrush brush3 = new SolidBrush(array[i]);
			graphics.FillPath(brush3, path3);
		}
		FitName();
		TextRenderer.DrawText(graphics, Text, _nameFont, NameRect, Theme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		DrawPill(graphics, NameRect.Bottom + PillGap);
		float num = (_active ? ((float)Tokens.Scale(3) / 2f) : ((float)Tokens.Scale(1)));
		Color color;
		if (_active)
		{
			color = ControlInk.Accent;
		}
		else
		{
			color = (_hover ? ControlInk.Edge : ControlInk.WindowEdge);
		}
		using (GraphicsPath path4 = Glass.RoundedPath(new RectangleF(num / 2f, num / 2f, (float)rectangle.Width - num, (float)rectangle.Height - num), Math.Max(0f, (float)CardRadius - num / 2f)))
		{
			using Pen pen = new Pen(color, num);
			graphics.DrawPath(pen, path4);
		}
		graphics.SmoothingMode = smoothingMode;
		graphics.PixelOffsetMode = pixelOffsetMode;
		_ring.Show(Focused && ShowFocusCues);
		base.OnPaint(e);
	}

	private void DrawChips(Graphics g, Rectangle photo)
	{
		Font font = House(Fonts.CaptionBold, 7f);
		int num = Tokens.Scale(4);
		int num2 = Tokens.Scale(5);
		int num3 = Tokens.Scale(5);
		int num4 = Tokens.Scale(3);
		int chipHeight = ChipHeight;
		int y = photo.Y + num;
		int num5 = 0;
		string text = _boundHotkey ?? "";
		if (text.Length > 0)
		{
			num5 = Math.Min(TextWidth(text, font) + 2 * num2, Math.Max(chipHeight, photo.Width - 2 * num));
		}
		int num6 = 2 * num2 + num3 + num4 + TextWidth("zapnuto", font);
		int num7 = 2 * num2 + num3;
		int width = 0;
		bool flag = false;
		if (_active)
		{
			int num8 = photo.Width - 2 * num - ((num5 > 0) ? (num5 + num) : 0);
			flag = num6 <= num8;
			width = (flag ? num6 : num7);
		}
		using SolidBrush brush = new SolidBrush(Color.FromArgb(230, SignalInk.PreviewWell));
		int num9 = Tokens.Scale(4);
		if (_active)
		{
			Rectangle rectangle = new Rectangle(photo.X + num, y, width, chipHeight);
			using (GraphicsPath path = Glass.RoundedPath(rectangle, num9))
			{
				g.FillPath(brush, path);
			}
			int num10 = rectangle.Y + chipHeight / 2;
			using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(255, Color.FromArgb(6213251))))
			{
				g.FillEllipse(brush2, rectangle.X + num2, (float)num10 - (float)num3 / 2f, num3, num3);
			}
			if (flag)
			{
				TextRenderer.DrawText(g, "zapnuto", font, new Rectangle(rectangle.X + num2 + num3 + num4, rectangle.Y, rectangle.Width - num2 - num3 - num4, chipHeight), Theme.Text, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			}
		}
		if (num5 > 0)
		{
			Rectangle rectangle2 = new Rectangle(photo.Right - num - num5, y, num5, chipHeight);
			using (GraphicsPath path2 = Glass.RoundedPath(rectangle2, num9))
			{
				g.FillPath(brush, path2);
			}
			TextRenderer.DrawText(g, text, font, rectangle2, Theme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
	}

	private void DrawPill(Graphics g, int y)
	{
		Font font = House(Fonts.Caption, 7f);
		string text = _reading.ToString(CultureInfo.InvariantCulture) + "%";
		int num = Tokens.Scale(8);
		int width = Math.Min(Width - 2 * Pad, TextWidth(text, font) + 2 * num);
		Rectangle rectangle = new Rectangle(Pad, y, width, PillHeight);
		using (GraphicsPath path = Glass.RoundedPath(rectangle, (float)rectangle.Height / 2f))
		{
			using SolidBrush brush = new SolidBrush(ControlInk.Surface2);
			g.FillPath(brush, path);
		}
		float num2 = Tokens.Scale(1);
		RectangleF rect = new RectangleF((float)rectangle.X + num2 / 2f, (float)rectangle.Y + num2 / 2f, (float)rectangle.Width - num2, (float)rectangle.Height - num2);
		using (GraphicsPath path2 = Glass.RoundedPath(rect, rect.Height / 2f))
		{
			using Pen pen = new Pen(ControlInk.WindowEdge, num2);
			g.DrawPath(pen, path2);
		}
		TextRenderer.DrawText(g, text, font, rectangle, Theme.TextDim, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
	}

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		_ring.Rehook();
	}

	private void DropThumb()
	{
		_thumb?.Dispose();
		_thumb = null;
	}

	private Bitmap Thumb(Size size)
	{
		if (_thumb != null && _thumbFor == size)
		{
			return _thumb;
		}
		_thumb?.Dispose();
		using Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			Image photo = _photo;
			float num = Math.Max((float)size.Width / (float)photo.Width, (float)size.Height / (float)photo.Height);
			float num2 = (float)size.Width / num;
			float num3 = (float)size.Height / num;
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
			if (_lookOnPhoto)
			{
				float[] matrix = _matrix;
				if (matrix != null && matrix.Length >= 25)
				{
					imageAttributes.SetColorMatrix(ColourMatrixOf(matrix));
				}
			}
			lock (BrandAssets.DrawGate)
			{
				graphics.DrawImage(photo, new Rectangle(Point.Empty, size), ((float)photo.Width - num2) / 2f, ((float)photo.Height - num3) / 2f, num2, num3, GraphicsUnit.Pixel, imageAttributes);
			}
		}
		Bitmap bitmap2 = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		using (Graphics graphics2 = Graphics.FromImage(bitmap2))
		{
			using TextureBrush brush = new TextureBrush(bitmap);
			using GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, size.Width, size.Height), PhotoRadius);
			graphics2.SmoothingMode = SmoothingMode.AntiAlias;
			graphics2.FillPath(brush, path);
		}
		_thumb = bitmap2;
		_thumbFor = size;
		return bitmap2;
	}

	private static ColorMatrix ColourMatrixOf(float[] m)
	{
		return new ColorMatrix(new float[5][]
		{
			Row(0),
			Row(1),
			Row(2),
			new float[5] { 0f, 0f, 0f, 1f, 0f },
			new float[5]
			{
				m[20],
				m[21],
				m[22],
				0f,
				1f
			}
		});
		float[] Row(int r)
		{
			return new float[5]
			{
				m[r * 5],
				m[r * 5 + 1],
				m[r * 5 + 2],
				0f,
				0f
			};
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			DropThumb();
			_ring.Release();
		}
		base.Dispose(disposing);
	}
}
