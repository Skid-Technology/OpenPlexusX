using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VibranceHud.Controls;

public sealed class GlassScrollBar : Control
{
	private int _value;

	private int _maximum = 100;

	private int _viewport = 10;

	private bool _hover;

	private bool _dragging;

	private int _dragOffset;

	public int Maximum
	{
		get
		{
			return _maximum;
		}
		set
		{
			_maximum = Math.Max(0, value);
			Clamp();
			Invalidate();
		}
	}

	public int Viewport
	{
		get
		{
			return _viewport;
		}
		set
		{
			_viewport = Math.Max(1, value);
			Clamp();
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
			int num = Math.Clamp(value, 0, Math.Max(0, _maximum - _viewport));
			if (num != _value)
			{
				_value = num;
				Invalidate();
			}
		}
	}

	public bool Needed => _maximum > _viewport;

	private int TrackTop => 4;

	private int TrackHeight => Math.Max(1, Height - 8);

	private int ThumbHeight
	{
		get
		{
			if (!Needed)
			{
				return TrackHeight;
			}
			float num = (float)_viewport / (float)_maximum;
			return Math.Max(28, (int)((float)TrackHeight * num));
		}
	}

	private int ThumbTop
	{
		get
		{
			if (!Needed)
			{
				return TrackTop;
			}
			float num = (float)_value / (float)(_maximum - _viewport);
			return TrackTop + (int)((float)(TrackHeight - ThumbHeight) * num);
		}
	}

	public event EventHandler? Scrolled;

	public GlassScrollBar()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Width = 8;
		TabStop = false;
	}

	private void Clamp()
	{
		Value = _value;
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
		if (e.Button == MouseButtons.Left && Needed)
		{
			if (e.Y >= ThumbTop && e.Y <= ThumbTop + ThumbHeight)
			{
				_dragging = true;
				DragTracker.BeginScrollBar();
				_dragOffset = e.Y - ThumbTop;
			}
			else
			{
				MoveThumbTo(e.Y - ThumbHeight / 2);
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_dragging)
		{
			MoveThumbTo(e.Y - _dragOffset);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (_dragging)
		{
			_dragging = false;
			DragTracker.EndScrollBar();
		}
	}

	private void MoveThumbTo(int top)
	{
		if (!Needed)
		{
			return;
		}
		int num = TrackHeight - ThumbHeight;
		if (num > 0)
		{
			int num2 = (int)Math.Round(Math.Clamp((float)(top - TrackTop) / (float)num, 0f, 1f) * (float)(_maximum - _viewport));
			if (num2 != _value)
			{
				_value = num2;
				Invalidate();
				Scrolled?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (!Needed)
		{
			return;
		}
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		float radius = Math.Max(1f, (float)(Width - 4) / 2f);
		using (GraphicsPath path = Glass.RoundedPath(new RectangleF(2f, TrackTop, Math.Max(1, Width - 4), TrackHeight), radius))
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(28, Theme.GlassEdge));
			graphics.FillPath(brush, path);
		}
		using GraphicsPath path2 = Glass.RoundedPath(new RectangleF(2f, ThumbTop, Math.Max(1, Width - 4), ThumbHeight), radius);
		Color baseColor = (Theme.IsLight ? Theme.AccentDim : Theme.Accent);
		using SolidBrush brush2 = new SolidBrush((_dragging || _hover) ? Color.FromArgb(_dragging ? 200 : 150, baseColor) : Color.FromArgb(Theme.IsLight ? 60 : 90, Theme.Text));
		graphics.FillPath(brush2, path2);
	}
}
