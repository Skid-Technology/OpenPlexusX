using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VibranceHud.Music;

internal sealed class MediaGlyphButton : Control
{
	private bool _hover;

	private bool _playing;

	internal MediaKey Key { get; }

	internal bool IsPlaying
	{
		get
		{
			return _playing;
		}
		set
		{
			_playing = value;
			if (Key == MediaKey.PlayPause)
			{
				AccessibleName = (value ? "Pause" : "Play");
			}
			Invalidate();
		}
	}

	internal MediaGlyphButton(MediaKey key)
	{
		Key = key;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		TabStop = false;
		AccessibleRole = AccessibleRole.PushButton;
		AccessibleName = key switch
		{
			MediaKey.Next => "Next track", 
			MediaKey.Previous => "Previous track", 
			_ => "Play or pause", 
		};
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

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		int dpi = ControlInk.Dpi(this);
		if (_hover)
		{
			using SolidBrush brush = new SolidBrush(ControlInk.Hover);
			using GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, Width - 1, Height - 1), ControlInk.PxF(dpi, 4f));
			graphics.FillPath(brush, path);
		}
		float num = ControlInk.PxF(dpi, 10f);
		float num2 = ((float)Width - num) / 2f;
		float num3 = ((float)Height - num) / 2f;
		Color color;
		if (Enabled)
		{
			color = (_hover ? ControlInk.Text1 : ControlInk.Text2);
		}
		else
		{
			color = ControlInk.Text3;
		}
		using SolidBrush brush2 = new SolidBrush(color);
		float num4 = Math.Max(1.5f, ControlInk.PxF(dpi, 1.6f));
		switch (Key)
		{
		case MediaKey.Previous:
			graphics.FillRectangle(brush2, num2, num3, num4, num);
			graphics.FillPolygon(brush2, new PointF[3]
			{
				new PointF(num2 + num, num3),
				new PointF(num2 + num, num3 + num),
				new PointF(num2 + num4 + 0.5f, num3 + num / 2f)
			});
			return;
		case MediaKey.Next:
			graphics.FillPolygon(brush2, new PointF[3]
			{
				new PointF(num2, num3),
				new PointF(num2, num3 + num),
				new PointF(num2 + num - num4 - 0.5f, num3 + num / 2f)
			});
			graphics.FillRectangle(brush2, num2 + num - num4, num3, num4, num);
			return;
		}
		if (IsPlaying)
		{
			graphics.FillRectangle(brush2, num2 + num4, num3, num4, num);
			graphics.FillRectangle(brush2, num2 + num - 2f * num4, num3, num4, num);
			return;
		}
		graphics.FillPolygon(brush2, new PointF[3]
		{
			new PointF(num2, num3),
			new PointF(num2, num3 + num),
			new PointF(num2 + num, num3 + num / 2f)
		});
	}
}
