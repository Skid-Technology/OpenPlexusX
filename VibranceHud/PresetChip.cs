using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class PresetChip : Control
{
	private const int RowHeightPx = 44;

	private const int MarkerGutterPx = 6;

	private const int BodyPadXPx = 8;

	private const int TileWPx = 64;

	private const int TileHPx = 36;

	private const int TileGapPx = 12;

	private const int NameLinePx = 20;

	private const int SignalGapPx = 6;

	private const int SignalHeightPx = 6;

	private const int RadiusPx = 4;

	private const int IconPx = 20;

	private const int IconReservePx = 24;

	private string _caption = "";

	private bool _hover;

	private bool _active;

	private readonly HoverFade _fade;

	private readonly OutsideFocusRing _ring;

	private string _kind = "balanced";

	private int _reading = 100;

	private Image? _photo;

	private bool _lookOnPhoto;

	private Bitmap? _tile;

	private Bitmap? _captionCache;

	private int _captionCacheWidth;

	private int _captionFailedWidth = -1;

	private Size _tileFailedSize = Size.Empty;

	private float[]? _matrix;

	private bool _overDelete;

	private bool _overBind;

	public static int RowHeight => Tokens.Scale(44);

	public static int MarkerGutter => Tokens.Scale(6);

	public string Caption
	{
		get
		{
			return _caption;
		}
		set
		{
			_caption = value ?? "";
			DropCaption();
			Invalidate();
		}
	}

	public string Subtitle { get; set; } = "";

	public string Kind
	{
		get
		{
			return _kind;
		}
		set
		{
			if (!(_kind == value))
			{
				_kind = value ?? "balanced";
				DropTile();
				Invalidate();
			}
		}
	}

	public int Reading
	{
		get
		{
			return _reading;
		}
		set
		{
			if (_reading != value)
			{
				_reading = value;
				Invalidate();
			}
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
				DropTile();
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
				DropTile();
				Invalidate();
			}
		}
	}

	internal Action<string>? FailPointForTest { get; set; }

	public float[]? Matrix
	{
		get
		{
			return _matrix;
		}
		set
		{
			_matrix = value;
			DropTile();
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
			}
		}
	}

	public bool Deletable { get; set; }

	public bool Bindable { get; set; }

	public string? BoundHotkey { get; set; }

	private Rectangle DeleteZone => IconZone(0);

	private Rectangle BindZone => IconZone(Deletable ? 1 : 0);

	internal Rectangle TestDeleteZone => DeleteZone;

	internal Rectangle TestBindZone => BindZone;

	private Rectangle TileRect
	{
		get
		{
			int width = Tokens.Scale(64);
			int num = Tokens.Scale(36);
			return new Rectangle(MarkerGutter + Tokens.Scale(8), (Height - num) / 2, width, num);
		}
	}

	internal Rectangle TestTile => TileRect;

	public event EventHandler? DeleteRequested;

	public event EventHandler? BindRequested;

	public new event EventHandler? Click;

	public static int WidthNeededFor(string caption, string reading, bool deletable, bool bindable)
	{
		int num = MarkerGutter + 2 * Tokens.Scale(8) + Tokens.Scale(64) + Tokens.Scale(12) + (deletable ? Tokens.Scale(24) : 0) + (bindable ? Tokens.Scale(24) : 0) + Tokens.Scale(6);
		int width = TextRenderer.MeasureText(caption, Fonts.BodyBold).Width;
		int width2 = TextRenderer.MeasureText(reading, Fonts.Body).Width;
		return num + width + width2;
	}

	private void DropTile()
	{
		_tile?.Dispose();
		_tile = null;
		_tileFailedSize = Size.Empty;
	}

	private void DropCaption()
	{
		_captionCache?.Dispose();
		_captionCache = null;
		_captionFailedWidth = -1;
	}

	private Rectangle IconZone(int fromTheRight)
	{
		return new Rectangle(Width - Tokens.Scale(8) - (fromTheRight + 1) * Tokens.Scale(24) + (Tokens.Scale(24) - Tokens.Scale(20)) / 2, (Height - Tokens.Scale(20)) / 2, Tokens.Scale(20), Tokens.Scale(20));
	}

	public PresetChip()
	{
		_fade = new HoverFade(this);
		_ring = new OutsideFocusRing(this, () => Tokens.Scale(4));
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Size = new Size(Tokens.Scale(300), RowHeight);
		TabStop = true;
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		_hover = true;
		_fade.Set(hot: true);
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_hover = false;
		_overDelete = false;
		_overBind = false;
		_fade.Set(hot: false);
		Invalidate();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		bool flag = Deletable && DeleteZone.Contains(e.Location);
		bool flag2 = Bindable && BindZone.Contains(e.Location);
		if (flag != _overDelete || flag2 != _overBind)
		{
			_overDelete = flag;
			_overBind = flag2;
			Cursor = Cursors.Hand;
			Invalidate();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
		{
			if (Deletable && DeleteZone.Contains(e.Location))
			{
				DeleteRequested?.Invoke(this, EventArgs.Empty);
			}
			else if (Bindable && BindZone.Contains(e.Location))
			{
				BindRequested?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				Click?.Invoke(this, EventArgs.Empty);
			}
		}
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
			Click?.Invoke(this, EventArgs.Empty);
			e.Handled = true;
		}
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	internal void TestMouseUp(MouseEventArgs e)
	{
		OnMouseUp(e);
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
		Graphics g = e.Graphics;
		PaintGuard.Try(this, () =>
		{
			PaintRow(g);
		});
		base.OnPaint(e);
	}

	private void PaintRow(Graphics g)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.HighQuality;
		g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		int markerGutter = MarkerGutter;
		int num = Tokens.Scale(8);
		int num2 = Tokens.Scale(4);
		Rectangle rectangle = new Rectangle(markerGutter, 0, Math.Max(1, Width - markerGutter), Height);
		if (_active || _fade.Value > 0.01f)
		{
			Color hover = ControlInk.Hover;
			using SolidBrush brush = new SolidBrush(_active ? ControlInk.Surface2 : Color.FromArgb((int)((float)(int)hover.A * _fade.Value), hover));
			using GraphicsPath path = Glass.RoundedPath(rectangle, num2);
			g.FillPath(brush, path);
		}
		if (_active)
		{
			float num3 = Tokens.ScaleF(3f);
			int num4 = Tokens.Scale(20);
			using SolidBrush brush2 = new SolidBrush(ControlInk.Accent);
			using GraphicsPath path2 = Glass.RoundedPath(new RectangleF(0f, (float)(Height - num4) / 2f, num3, num4), num3 / 2f);
			g.FillPath(brush2, path2);
		}
		Rectangle tileRect = TileRect;
		DrawTile(g, tileRect, num2);
		int num5 = tileRect.Right + Tokens.Scale(12);
		int num6 = (Deletable ? Tokens.Scale(24) : 0) + (Bindable ? Tokens.Scale(24) : 0);
		int num7 = rectangle.Right - num - num6;
		int num8 = Tokens.Scale(20);
		int num9 = Tokens.Scale(6);
		int num10 = num8 + Tokens.Scale(6) + num9;
		int num11 = Math.Max(0, (Height - num10) / 2);
		string text = _reading.ToString(CultureInfo.InvariantCulture) + "%";
		int num12 = TextRenderer.MeasureText(text, Fonts.Body).Width + Tokens.Scale(2);
		Rectangle bounds = new Rectangle(Math.Max(num5, num7 - num12), num11, Math.Min(num12, Math.Max(0, num7 - num5)), num8);
		TextRenderer.DrawText(g, text, Fonts.Body, bounds, Theme.TextDim, TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		Rectangle bounds2 = new Rectangle(num5, num11, Math.Max(0, bounds.X - Tokens.Scale(6) - num5), num8);
		if ((_captionCache == null || _captionCacheWidth != bounds2.Width) && _captionFailedWidth != bounds2.Width)
		{
			_captionCache?.Dispose();
			_captionCache = null;
			try
			{
				FailPointForTest?.Invoke("caption");
				_captionCache = RenderCaption(_caption, bounds2.Size);
				_captionCacheWidth = bounds2.Width;
			}
			catch (Exception ex) when (PaintGuard.IsGdiFailure(ex))
			{
				_captionFailedWidth = bounds2.Width;
				PaintGuard.LogOnce(this, ex);
			}
		}
		if (_captionCache != null)
		{
			g.DrawImageUnscaled(_captionCache, bounds2.Location);
		}
		else if (bounds2.Width > 0)
		{
			TextRenderer.DrawText(g, _caption, Fonts.BodyBold, bounds2, Theme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
		DrawSignal(g, new Rectangle(num5, num11 + num8 + Tokens.Scale(6), Math.Max(0, rectangle.Right - num - num5), num9));
		if (Deletable && _hover)
		{
			DrawCross(g, DeleteZone, _overDelete);
		}
		if (Bindable && (_hover || BoundHotkey != null))
		{
			DrawKeycap(g, BindZone, BoundHotkey != null, _overBind);
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

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		_ring.Rehook();
	}

	private static void DrawCross(Graphics g, Rectangle zone, bool hot)
	{
		if (hot)
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(60, Theme.Accent));
			g.FillEllipse(brush, zone);
		}
		using Pen pen = new Pen(hot ? Theme.Text : Theme.TextDim, Tokens.ScaleF(1.6f))
		{
			StartCap = LineCap.Round,
			EndCap = LineCap.Round
		};
		int num = zone.X + zone.Width / 2;
		int num2 = zone.Y + zone.Height / 2;
		int num3 = Tokens.Scale(4);
		g.DrawLine(pen, num - num3, num2 - num3, num + num3, num2 + num3);
		g.DrawLine(pen, num - num3, num2 + num3, num + num3, num2 - num3);
	}

	private static void DrawKeycap(Graphics g, Rectangle zone, bool bound, bool hot)
	{
		if (hot)
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(60, Theme.Accent));
			g.FillEllipse(brush, zone);
		}
		Color color;
		if (bound)
		{
			color = Theme.Accent;
		}
		else
		{
			color = (hot ? Theme.Text : Theme.TextDim);
		}
		Rectangle rectangle = new Rectangle(zone.X + Tokens.Scale(4), zone.Y + Tokens.Scale(5), Tokens.Scale(12), Tokens.Scale(10));
		using Pen pen = new Pen(color, Tokens.ScaleF(1.4f))
		{
			LineJoin = LineJoin.Round
		};
		using GraphicsPath path = Glass.RoundedPath(new RectangleF(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height), Tokens.ScaleF(3f));
		g.DrawPath(pen, path);
		g.DrawLine(pen, rectangle.X + Tokens.Scale(3), rectangle.Bottom - Tokens.Scale(3), rectangle.Right - Tokens.Scale(3), rectangle.Bottom - Tokens.Scale(3));
	}

	private static Bitmap RenderCaption(string text, Size size)
	{
		Bitmap bitmap = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height), PixelFormat.Format32bppArgb);
		try
		{
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			TextRenderer.DrawText(graphics, text, Fonts.BodyBold, new Rectangle(Point.Empty, size), Theme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			return bitmap;
		}
		catch
		{
			bitmap.Dispose();
			throw;
		}
	}

	private void DrawTile(Graphics g, Rectangle tile, int radius)
	{
		if (tile.Width < 2 || tile.Height < 2)
		{
			return;
		}
		Image photo = _photo;
		using GraphicsPath path = Glass.RoundedPath(tile, radius);
		if (photo != null && (_tile == null || _tile.Width != tile.Width || _tile.Height != tile.Height) && _tileFailedSize != tile.Size)
		{
			_tile?.Dispose();
			_tile = null;
			try
			{
				FailPointForTest?.Invoke("tile");
				_tile = RenderPhoto(photo, tile.Width, tile.Height, _lookOnPhoto ? _matrix : null);
			}
			catch (Exception ex) when (PaintGuard.IsGdiFailure(ex))
			{
				_tileFailedSize = tile.Size;
				PaintGuard.LogOnce(this, ex);
			}
		}
		if (photo == null || _tile == null)
		{
			using SolidBrush brush = new SolidBrush(SignalInk.PreviewWell);
			g.FillPath(brush, path);
		}
		else
		{
			using TextureBrush textureBrush = new TextureBrush(_tile)
			{
				WrapMode = WrapMode.Clamp
			};
			textureBrush.TranslateTransform(tile.X, tile.Y);
			g.FillPath(textureBrush, path);
		}
		using Pen pen = new Pen(Color.FromArgb(60, Theme.GlassEdge), Tokens.ScaleF(1f));
		g.DrawPath(pen, path);
	}

	private static Bitmap RenderPhoto(Image photo, int width, int height, float[]? look)
	{
		Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		try
		{
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
			if (look != null && look.Length >= 25)
			{
				imageAttributes.SetColorMatrix(ColourMatrixOf(look));
			}
			lock (BrandAssets.DrawGate)
			{
				float num = Math.Max((float)width / (float)photo.Width, (float)height / (float)photo.Height);
				float num2 = (float)width / num;
				float num3 = (float)height / num;
				graphics.DrawImage(photo, new Rectangle(0, 0, width, height), ((float)photo.Width - num2) / 2f, ((float)photo.Height - num3) / 2f, num2, num3, GraphicsUnit.Pixel, imageAttributes);
			}
			return bitmap;
		}
		catch
		{
			bitmap.Dispose();
			throw;
		}
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

	private void DrawSignal(Graphics g, Rectangle bar)
	{
		if (bar.Width < 4 || bar.Height < 2)
		{
			return;
		}
		int num = Math.Clamp(_reading, 0, 300);
		SmoothingMode smoothingMode = g.SmoothingMode;
		g.SmoothingMode = SmoothingMode.None;
		if (num < 300)
		{
			int num2 = Math.Max(1, Tokens.Scale(2));
			using SolidBrush brush = new SolidBrush(Theme.AccentDim);
			g.FillRectangle(brush, X(num), (float)bar.Y + (float)(bar.Height - num2) / 2f, (float)bar.Right - X(num), num2);
		}
		using (SolidBrush brush2 = new SolidBrush(SignalTokens.Root.SignalStart))
		{
			g.FillRectangle(brush2, bar.X, bar.Y, X(Math.Min(num, 100)) - (float)bar.X, bar.Height);
		}
		if (num > 100)
		{
			Color[] array = new Color[6]
			{
				SignalTokens.Root.Signal1,
				SignalTokens.Root.Signal2,
				SignalTokens.Root.Signal3,
				SignalTokens.Root.Signal4,
				SignalTokens.Root.Signal5,
				SignalTokens.Root.Signal6
			};
			float num3 = (float)bar.Height / (float)array.Length;
			float num4 = X(100f);
			float num5 = X(num);
			for (int i = 0; i < array.Length; i++)
			{
				using LinearGradientBrush brush3 = new LinearGradientBrush(new RectangleF(num4, bar.Y - 1, Math.Max(1f, X(300f) - num4), bar.Height + 2), SignalTokens.Root.SignalStart, array[i], LinearGradientMode.Horizontal);
				g.FillRectangle(brush3, num4, (float)bar.Y + (float)i * num3, num5 - num4, num3 + 0.5f);
			}
		}
		g.SmoothingMode = smoothingMode;
		float X(float t)
		{
			return (float)bar.X + t / 300f * (float)bar.Width;
		}
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		DropTile();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			DropTile();
			DropCaption();
			_ring.Release();
		}
		base.Dispose(disposing);
	}
}
