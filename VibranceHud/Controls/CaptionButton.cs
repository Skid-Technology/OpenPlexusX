using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class CaptionButton : Control
{
	public enum Kind
	{
		Minimise,
		Close
	}

	private readonly Kind _kind;

	private bool _hot;

	private bool _down;

	internal bool HotForTest
	{
		set
		{
			_hot = value;
			Invalidate();
		}
	}

	internal bool PressedForTest
	{
		set
		{
			_down = value;
			Invalidate();
		}
	}

	public CaptionButton(Kind kind)
	{
		_kind = kind;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		SetStyle(ControlStyles.Selectable, value: false);
		TabStop = false;
		AccessibleRole = AccessibleRole.PushButton;
		AccessibleName = ((kind == Kind.Close) ? "Close" : "Minimise");
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		_hot = true;
		Invalidate();
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		_hot = false;
		_down = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			_down = true;
			Invalidate();
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		_down = false;
		Invalidate();
		base.OnMouseUp(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		if (_down || _hot)
		{
			using SolidBrush brush = new SolidBrush(_down ? ControlInk.Pressed : ControlInk.Hover);
			graphics.FillRectangle(brush, 0, 0, Width, Height);
		}
		using Pen pen = new Pen((_hot || _down) ? Theme.Text : Theme.TextDim, 1.4f);
		PaintGlyph(graphics, pen, GlyphBox());
	}

	private void PaintGlyph(Graphics g, Pen pen, Rectangle r)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		float num = (float)r.Top + (float)(r.Height + 1) / 2f;
		if (_kind == Kind.Minimise)
		{
			g.DrawLine(pen, r.Left, num, r.Right, num);
			return;
		}
		g.DrawLine(pen, r.Left, r.Top, r.Right, r.Bottom);
		g.DrawLine(pen, r.Right, r.Top, r.Left, r.Bottom);
	}

	private Rectangle GlyphBox()
	{
		int num = Tokens.Scale(9);
		return new Rectangle((Width - num) / 2, (Height - num) / 2, num, num);
	}
}
