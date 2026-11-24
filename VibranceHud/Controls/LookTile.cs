using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Theming;

namespace VibranceHud.Controls;

public sealed class LookTile : Control
{
	private readonly string _caption;

	private readonly string _name;

	private readonly HoverFade _fade;

	private readonly OutsideFocusRing _ring;

	private bool _active;

	private const int Pad = 12;

	private const int PreviewH = 104;

	private const int NameGap = 12;

	private const int NameH = 20;

	private const int CaptionGap = 2;

	private const int RadioD = 20;

	private const int RadioDot = 8;

	private static readonly Rectangle[] Boxes = new Rectangle[5]
	{
		new Rectangle(6, 6, 22, 92),
		new Rectangle(34, 6, 199, 18),
		new Rectangle(34, 30, 199, 34),
		new Rectangle(34, 70, 120, 28),
		new Rectangle(160, 70, 73, 28)
	};

	private const int BoxesW = 242;

	public ThemePalette Palette { get; }

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

	internal string ShownName => _name;

	internal string Caption => _caption;

	internal Action<string>? FailPointForTest { get; set; }

	private bool IsSlate => Palette.Name == "Slate";

	public LookTile(ThemePalette palette, string caption, string? name = null)
	{
		Palette = palette;
		_caption = caption;
		_name = name ?? palette.Name;
		_fade = new HoverFade(this);
		_ring = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 8f));
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Size = new Size(266, 194);
		AccessibleName = _name;
		AccessibleRole = AccessibleRole.RadioButton;
		TabStop = true;
		SetStyle(ControlStyles.Selectable, value: true);
	}

	internal static int HeightFor(int width, string caption, int dpi)
	{
		int width2 = Math.Max(1, width - 2 * ControlInk.Px(dpi, 12));
		int height = TextRenderer.MeasureText(caption, Fonts.Caption, new Size(width2, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
		return ControlInk.Px(dpi, 150) + height + ControlInk.Px(dpi, 12);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		_fade.Set(hot: true);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_fade.Set(hot: false);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
		{
			Focus();
			Invalidate();
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

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		_ring.Rehook();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		bool flag = !Enabled;
		if (!flag)
		{
			Keys keyCode = e.KeyCode;
			bool flag2 = ((keyCode == Keys.Return || keyCode == Keys.Space) ? true : false);
			flag = !flag2;
		}
		if (!flag)
		{
			e.Handled = true;
			OnClick(EventArgs.Empty);
		}
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = (Enabled ? Cursors.Hand : Cursors.Default);
		if (!Enabled)
		{
			_fade.Snap(hot: false);
		}
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		PaintGuard.Try(this, () =>
		{
			PaintTile(g);
		});
	}

	private void PaintTile(Graphics g)
	{
		int dpi = ControlInk.Dpi(this);
		int num = ControlInk.Px(dpi, 12);
		int num2 = ControlInk.Hairline(dpi);
		float num3 = ControlInk.PxF(dpi, 8f);
		Color fill = (Enabled ? ControlInk.Mix(Color.FromArgb(0, ControlInk.Hover), ControlInk.Hover, _fade.Value) : Color.Transparent);
		Color edge;
		if (Enabled)
		{
			edge = (_active ? ControlInk.Accent : ControlInk.Edge);
		}
		else
		{
			edge = ControlInk.TextDisabled;
		}
		ControlInk.PaintBox(g, ClientSize, num3, num2, fill, edge);
		if (_active && Enabled && Width > 4 && Height > 4)
		{
			Size size = new Size(Width - 2 * num2, Height - 2 * num2);
			GraphicsState gstate = g.Save();
			g.TranslateTransform(num2, num2);
			ControlInk.PaintBox(g, size, Math.Max(0f, num3 - (float)num2), num2, Color.Transparent, ControlInk.Accent);
			g.Restore(gstate);
		}
		Rectangle well = new Rectangle(num, num, Math.Max(1, Width - 2 * num), Math.Min(ControlInk.Px(dpi, 104), Math.Max(1, Height - 2 * num)));
		if (well.Width > 4 && well.Height > 4)
		{
			PaintGuard.Try(this, () =>
			{
				PaintPreview(g, well, dpi);
			});
		}
		int num4 = well.Bottom + ControlInk.Px(dpi, 12);
		int num5 = ControlInk.Px(dpi, 20);
		int num6 = ControlInk.Px(dpi, 20);
		Rectangle box = new Rectangle(Width - num - num6, num4 + (num5 - num6) / 2, num6, num6);
		Color foreColor = (Enabled ? ControlInk.Text1 : ControlInk.TextDisabledInk);
		Color foreColor2 = (Enabled ? ControlInk.Text3 : ControlInk.TextDisabledInk);
		TextRenderer.DrawText(g, _name, Fonts.BodyBold, new Rectangle(num, num4, Math.Max(1, box.Left - num - ControlInk.Px(dpi, 8)), num5), foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		PaintRadio(g, box, dpi);
		int num7 = num4 + num5 + ControlInk.Px(dpi, 2);
		TextRenderer.DrawText(g, _caption, Fonts.Caption, new Rectangle(num, num7, Math.Max(1, Width - 2 * num), Math.Max(1, Height - num7 - num / 2)), foreColor2, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
		_ring.Show(Focused && ShowFocusCues && Enabled);
	}

	private void PaintPreview(Graphics g, Rectangle well, int dpi)
	{
		FailPointForTest?.Invoke("draw");
		float radius = ControlInk.PxF(dpi, 4f);
		using GraphicsPath path = Glass.RoundedPath(new RectangleF(well.X, well.Y, well.Width, well.Height), radius);
		using Region clip = g.Clip;
		g.SetClip(path, CombineMode.Intersect);
		try
		{
			using (SolidBrush brush = new SolidBrush(SlateSurface.Active ? SignalTokens.Slate.PreviewWell : SignalTokens.Ash.PreviewWell))
			{
				g.FillRectangle(brush, well);
			}
			if (AppBackground.IsSet)
			{
				GraphicsState gstate = g.Save();
				g.TranslateTransform(well.X, well.Y);
				AppBackground.PaintInto(g, well.Width, well.Height);
				g.Restore(gstate);
			}
			float num = (float)well.Width / 242f;
			Color color = (IsSlate ? Color.FromArgb((int)Math.Round(168.3), SignalTokens.Slate.SlateBase) : SignalTokens.Ash.Surface1);
			Color color2 = (IsSlate ? SignalTokens.Slate.Border : SignalTokens.Ash.Border);
			if (!Enabled)
			{
				color = Color.FromArgb(color.A / 2, color);
			}
			SmoothingMode smoothingMode = g.SmoothingMode;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			using SolidBrush brush2 = new SolidBrush(color);
			using Pen pen = new Pen(color2, 1f);
			Rectangle[] boxes = Boxes;
			for (int i = 0; i < boxes.Length; i++)
			{
				Rectangle rectangle = boxes[i];
				RectangleF rect = new RectangleF((float)well.X + (float)rectangle.X * num, (float)well.Y + (float)rectangle.Y * num, (float)rectangle.Width * num, Math.Min((float)rectangle.Height * num, (float)well.Bottom - ((float)well.Y + (float)rectangle.Y * num) - 1f));
				if (!(rect.Width < 2f) && !(rect.Height < 2f))
				{
					using GraphicsPath path2 = Glass.RoundedPath(rect, ControlInk.PxF(dpi, 2f));
					g.FillPath(brush2, path2);
					g.DrawPath(pen, path2);
				}
			}
			g.SmoothingMode = smoothingMode;
		}
		finally
		{
			g.Clip = clip;
		}
	}

	private void PaintRadio(Graphics g, Rectangle box, int dpi)
	{
		if (box.Width < 4)
		{
			return;
		}
		SmoothingMode smoothingMode = g.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		try
		{
			float num = (float)ControlInk.Hairline(dpi) / 2f;
			RectangleF rect = new RectangleF((float)box.X + num, (float)box.Y + num, (float)box.Width - 2f * num, (float)box.Height - 2f * num);
			if (_active && Enabled)
			{
				using (SolidBrush brush = new SolidBrush(ControlInk.Accent))
				{
					g.FillEllipse(brush, box);
				}
				float num2 = ControlInk.PxF(dpi, 8f);
				using SolidBrush brush2 = new SolidBrush(ControlInk.OnAccent);
				g.FillEllipse(brush2, (float)box.X + ((float)box.Width - num2) / 2f, (float)box.Y + ((float)box.Height - num2) / 2f, num2, num2);
				return;
			}
			using Pen pen = new Pen(Enabled ? ControlInk.Edge : ControlInk.TextDisabled, ControlInk.Hairline(dpi));
			g.DrawEllipse(pen, rect);
		}
		finally
		{
			g.SmoothingMode = smoothingMode;
			g.PixelOffsetMode = pixelOffsetMode;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_ring.Release();
		}
		base.Dispose(disposing);
	}
}
