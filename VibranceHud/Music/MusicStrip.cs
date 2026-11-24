using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Music;

internal sealed class MusicStrip : Control
{
	private readonly Control _slot;

	private readonly IMediaKeys _keys;

	private readonly MusicPoller _poller;

	private readonly MediaGlyphButton[] _buttons;

	internal NowPlaying? Showing { get; private set; }

	internal MusicStrip(Control slot, INowPlayingSource source, IMediaKeys keys)
	{
		_slot = slot;
		_keys = keys;
		_poller = MusicPoller.For(source);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Bounds = slot.Bounds;
		Visible = false;
		TabStop = false;
		AccessibleName = "Now playing";
		_buttons = new MediaGlyphButton[3]
		{
			new MediaGlyphButton(MediaKey.Previous),
			new MediaGlyphButton(MediaKey.PlayPause),
			new MediaGlyphButton(MediaKey.Next)
		};
		MediaGlyphButton[] buttons = _buttons;
		foreach (MediaGlyphButton mediaGlyphButton in buttons)
		{
			MediaKey key = mediaGlyphButton.Key;
			mediaGlyphButton.Click += (object? _, EventArgs _) =>
			{
				_poller.Command(key, _keys);
			};
			Controls.Add(mediaGlyphButton);
		}
		LayoutButtons();
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		LayoutButtons();
	}

	internal void AttachPoller(Form popup)
	{
		_poller.Attach(popup, ShowSnapshot);
	}

	private void LayoutButtons()
	{
		if (_buttons != null)
		{
			int dpi = ControlInk.Dpi(this);
			int num = Math.Min(Height, ControlInk.Px(dpi, 24));
			int y = (Height - num) / 2;
			int num2 = ControlInk.Px(dpi, 3) + ((Showing?.Art != null) ? (num + ControlInk.Px(dpi, 4)) : 0);
			MediaGlyphButton[] buttons = _buttons;
			for (int i = 0; i < buttons.Length; i++)
			{
				buttons[i].Bounds = new Rectangle(num2, y, num, num);
				num2 += num;
			}
		}
	}

	internal void ReadNow()
	{
		_poller.ReadForTestOrRefresh(ShowSnapshot);
	}

	internal void ShowSnapshot(NowPlaying? now)
	{
		bool flag = now != Showing;
		Showing = now;
		Visible = now != null;
		_slot.Visible = now == null;
		_buttons[0].Enabled = now?.CanPrevious ?? false;
		_buttons[1].Enabled = now?.CanPlayPause ?? false;
		_buttons[1].IsPlaying = now?.IsPlaying ?? false;
		_buttons[2].Enabled = now?.CanNext ?? false;
		LayoutButtons();
		if (flag)
		{
			Invalidate();
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (!(Showing == null))
		{
			int dpi = ControlInk.Dpi(this);
			if (Showing.Art != null)
			{
				int num = Math.Min(Height, ControlInk.Px(dpi, 24));
				e.Graphics.DrawImage(Showing.Art, new Rectangle(ControlInk.Px(dpi, 3), (Height - num) / 2, num, num));
			}
			int num2 = _buttons[^1].Right + ControlInk.Px(dpi, 6);
			TextRenderer.DrawText(bounds: new Rectangle(num2, 0, Math.Max(0, Width - num2), Height), dc: e.Graphics, text: Showing.Line, font: Fonts.Caption, foreColor: ControlInk.Text2, flags: TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
		}
	}
}
