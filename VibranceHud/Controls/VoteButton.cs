using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class VoteButton : Control
{
	private bool _active;

	private readonly HoverFade _fade;

	private readonly HoverFade _press;

	public bool ThumbUp { get; init; }

	public Func<Color> Tint { get; init; } = () => Theme.Accent;

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

	public VoteButton()
	{
		_fade = new HoverFade(this);
		_press = new HoverFade(this);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Size = new Size(64, 40);
		TabStop = true;
		SetStyle(ControlStyles.Selectable, value: true);
		AccessibleRole = AccessibleRole.RadioButton;
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		AccessibleName = Text;
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
		_fade.Set(hot: false);
		_press.Set(hot: false);
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

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Color color = Tint();
		float value = _press.Value;
		float num = 1.5f * value;
		RectangleF rect = new RectangleF(0.5f + num, 0.5f + num, (float)(Width - 1) - 2f * num, (float)(Height - 1) - 2f * num);
		if (rect.Width <= 0f || rect.Height <= 0f)
		{
			return;
		}
		float num2 = rect.Height / 2f;
		using (GraphicsPath path = Glass.RoundedPath(rect, num2))
		{
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(_active ? 210 : ((int)(26f + 34f * _fade.Value)), color)))
			{
				graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen(Color.FromArgb(_active ? 235 : 110, color), 1f);
			graphics.DrawPath(pen, path);
		}
		if (Focused)
		{
			using Pen pen2 = new Pen(Color.FromArgb(200, color), 2f);
			using GraphicsPath path2 = Glass.RoundedPath(RectangleF.Inflate(rect, -2.5f, -2.5f), Math.Max(1f, num2 - 2.5f));
			graphics.DrawPath(pen2, path2);
		}
		Color color2 = (_active ? Theme.OnAccent : color);
		DrawThumb(graphics, new Rectangle((int)(rect.X + rect.Width / 2f) - 9, (int)(rect.Y + rect.Height / 2f) - 9, 18, 18), color2, ThumbUp);
	}

	private static void DrawThumb(Graphics g, Rectangle r, Color color, bool up)
	{
		GraphicsState gstate = g.Save();
		try
		{
			g.TranslateTransform((float)r.X + (float)r.Width / 2f, (float)r.Y + (float)r.Height / 2f);
			if (!up)
			{
				g.ScaleTransform(1f, -1f);
			}
			float num = (float)r.Width / 20f;
			g.ScaleTransform(num, num);
			using SolidBrush brush = new SolidBrush(color);
			using (GraphicsPath path = Glass.RoundedPath(new RectangleF(-3.5f, -9f, 5f, 9f), 2.5f))
			{
				g.FillPath(brush, path);
			}
			using (GraphicsPath path2 = Glass.RoundedPath(new RectangleF(-3.5f, -2.5f, 11.5f, 9.5f), 2.5f))
			{
				g.FillPath(brush, path2);
			}
			using GraphicsPath path3 = Glass.RoundedPath(new RectangleF(-9f, -1.5f, 4f, 8.5f), 1.5f);
			g.FillPath(brush, path3);
		}
		finally
		{
			g.Restore(gstate);
		}
	}
}
