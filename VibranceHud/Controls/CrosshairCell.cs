using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Crosshair;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class CrosshairCell : Control
{
	private const int HeartSize = 16;

	private bool _hover;

	private bool _hoverHeart;

	private readonly OutsideFocusRing _ring;

	private bool _active;

	private bool _favourite;

	public CrosshairGallery.GalleryItem Item { get; }

	public CrosshairConfig? PreviewStyle { get; set; }

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

	public bool Favourite
	{
		get
		{
			return _favourite;
		}
		set
		{
			if (_favourite != value)
			{
				_favourite = value;
				Invalidate();
			}
		}
	}

	private static Color TextOnFill
	{
		get
		{
			if (!SlateSurface.Active)
			{
				return SignalTokens.Ash.TextOnFill;
			}
			return SignalTokens.Slate.TextOnFill;
		}
	}

	private Rectangle HeartRect => new Rectangle(Width - 16 - 6, 6, 16, 16);

	private static int NameStrip => Math.Max(Fonts.LineHeight(FontRole.Caption), TextRenderer.MeasureText("Hg", Fonts.Caption).Height);

	public event EventHandler? FavouriteToggled;

	public CrosshairCell(CrosshairGallery.GalleryItem item)
	{
		Item = item;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		TabStop = true;
		AccessibleRole = AccessibleRole.PushButton;
		AccessibleName = item.Name;
		_ring = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 4f));
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

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		bool flag = HeartRect.Contains(e.Location);
		if (flag != _hoverHeart)
		{
			_hoverHeart = flag;
			Invalidate();
		}
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
		_hoverHeart = false;
		Invalidate();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		Focus();
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && HeartRect.Contains(e.Location))
		{
			FavouriteToggled?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			base.OnMouseUp(e);
		}
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

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		RectangleF rect = new RectangleF(0f, 0f, Width, Height);
		float num = Tokens.Scale(4);
		using (GraphicsPath path = Glass.RoundedPath(rect, num))
		{
			using SolidBrush brush = new SolidBrush((_hover && !_active) ? ControlInk.Hover : ControlInk.Surface2);
			graphics.FillPath(brush, path);
		}
		if (_active)
		{
			float num2 = Tokens.Scale(2);
			using Pen pen = new Pen(ControlInk.Accent, num2);
			using GraphicsPath path2 = Glass.RoundedPath(RectangleF.Inflate(rect, (0f - num2) / 2f, (0f - num2) / 2f), Math.Max(1f, num - num2 / 2f));
			graphics.DrawPath(pen, path2);
		}
		DrawPreview(graphics);
		DrawHeart(graphics);
		DrawName(graphics);
		if (Focused && ShowFocusCues)
		{
			_ring.Show(Enabled);
		}
		else
		{
			_ring.Show(show: false);
		}
	}

	private (Rectangle Glyph, Rectangle Name) Column()
	{
		int num = Tokens.Scale(36);
		int num2 = Tokens.Scale(8);
		int nameStrip = NameStrip;
		int num3 = Math.Max(0, (Height - num - num2 - nameStrip) / 2);
		return (Glyph: new Rectangle((Width - num) / 2, num3, num, num), Name: new Rectangle(2, num3 + num + num2, Width - 4, nameStrip));
	}

	private void DrawPreview(Graphics g)
	{
		CrosshairConfig crosshairConfig = Item.Config.Clone();
		if (PreviewStyle != null)
		{
			crosshairConfig.ColourArgb = PreviewStyle.ColourArgb;
			crosshairConfig.Opacity = PreviewStyle.Opacity;
			crosshairConfig.Outline = PreviewStyle.Outline;
		}
		Rectangle item = Column().Glyph;
		if (item.Width > 4)
		{
			CrosshairRender.Draw(g, crosshairConfig, item, (float)Tokens.Scale(100) / 100f);
		}
	}

	private void DrawHeart(Graphics g)
	{
		if (!_favourite && !_hover)
		{
			return;
		}
		Rectangle heartRect = HeartRect;
		Color color = (_favourite ? Theme.Text : Color.FromArgb(_hoverHeart ? 200 : 90, Theme.GlassEdge));
		using GraphicsPath path = HeartPath(heartRect);
		if (_favourite)
		{
			using (SolidBrush brush = new SolidBrush(color))
			{
				g.FillPath(brush, path);
				return;
			}
		}
		using Pen pen = new Pen(color, 1.4f);
		g.DrawPath(pen, path);
	}

	private static GraphicsPath HeartPath(Rectangle r)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		float num = r.Width;
		float num2 = r.Height;
		float num3 = (float)r.X + num / 2f;
		graphicsPath.AddBezier(num3, (float)r.Y + num2 * 0.28f, num3, r.Y, r.X, (float)r.Y + num2 * 0.05f, r.X, (float)r.Y + num2 * 0.38f);
		graphicsPath.AddBezier(r.X, (float)r.Y + num2 * 0.38f, r.X, (float)r.Y + num2 * 0.68f, num3, (float)r.Y + num2 * 0.82f, num3, r.Bottom);
		graphicsPath.AddBezier(num3, r.Bottom, num3, (float)r.Y + num2 * 0.82f, r.Right, (float)r.Y + num2 * 0.68f, r.Right, (float)r.Y + num2 * 0.38f);
		graphicsPath.AddBezier(r.Right, (float)r.Y + num2 * 0.38f, r.Right, (float)r.Y + num2 * 0.05f, num3, r.Y, num3, (float)r.Y + num2 * 0.28f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	private void DrawName(Graphics g)
	{
		Graphics dc = g;
		string name = Item.Name;
		Font caption = Fonts.Caption;
		Rectangle item = Column().Name;
		Color foreColor;
		if (Enabled)
		{
			foreColor = ((_active || _hover) ? ControlInk.Text1 : TextOnFill);
		}
		else
		{
			foreColor = ControlInk.TextDisabledInk;
		}
		TextRenderer.DrawText(dc, name, caption, item, foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
	}
}
