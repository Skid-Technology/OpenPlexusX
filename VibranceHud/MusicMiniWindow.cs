using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Music;

namespace VibranceHud;

internal sealed class MusicMiniWindow : SignalDialogWindow
{
	private const int W = 340;

	private const int H = 96;

	private const int Pad = 14;

	private const int ButtonSide = 26;

	private readonly MusicPoller _poller;

	private readonly MediaGlyphButton[] _buttons;

	private readonly AppSettings? _settings;

	private readonly Action? _savePosition;

	internal const string NothingPlaying = "Nothing playing";

	internal NowPlaying? Showing { get; private set; }

	internal string SongText => Showing?.Title ?? "";

	internal string ArtistText => Showing?.Artist ?? "Nothing playing";

	protected override bool ShowWithoutActivation => true;

	internal MusicMiniWindow(INowPlayingSource source, IMediaKeys keys, AppSettings? settings = null, Action? savePosition = null)
	{
		MusicMiniWindow musicMiniWindow = this;
		_settings = settings;
		_savePosition = savePosition;
		_poller = MusicPoller.For(source);
		StartPosition = FormStartPosition.Manual;
		Text = "PlexusX music";
		AccessibleRole = AccessibleRole.Window;
		ClientSize = new Size(S(340), S(96));
		TopMost = true;
		VibranceHud.Controls.CaptionButton captionButton = new VibranceHud.Controls.CaptionButton(VibranceHud.Controls.CaptionButton.Kind.Close)
		{
			Size = new Size(S(32), S(32)),
			Location = new Point(ClientSize.Width - S(32), 0)
		};
		captionButton.Click += (object? _, EventArgs _) =>
		{
			musicMiniWindow.Hide();
		};
		Controls.Add(captionButton);
		_buttons = new MediaGlyphButton[3]
		{
			new MediaGlyphButton(MediaKey.Previous),
			new MediaGlyphButton(MediaKey.PlayPause),
			new MediaGlyphButton(MediaKey.Next)
		};
		int num = ClientSize.Width - S(14) - 3 * S(26);
		MediaGlyphButton[] buttons = _buttons;
		foreach (MediaGlyphButton button in buttons)
		{
			button.Bounds = new Rectangle(num, ClientSize.Height - S(36), S(26), S(26));
			button.Click += (object? _, EventArgs _) =>
			{
				musicMiniWindow._poller.Command(button.Key, keys);
			};
			Controls.Add(button);
			num += S(26);
		}
		Rectangle rectangle = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
		int? num3 = settings?.MusicMiniX;
		Point point;
		if (num3.HasValue)
		{
			int valueOrDefault = num3.GetValueOrDefault();
			num3 = settings.MusicMiniY;
			if (num3.HasValue)
			{
				int valueOrDefault2 = num3.GetValueOrDefault();
				point = new Point(valueOrDefault, valueOrDefault2);
				goto IL_0274;
			}
		}
		point = new Point(rectangle.Right - Width - S(24), rectangle.Bottom - Height - S(24));
		goto IL_0274;
		IL_0274:
		Point wanted = point;
		Location = Clamp(wanted, Size);
		WindowDrag.Enable(this, this);
		_poller.Attach(this, ShowSnapshot);
	}

	internal static Point Clamp(Point wanted, Size size)
	{
		Rectangle workingArea = Screen.FromRectangle(new Rectangle(wanted, size)).WorkingArea;
		return new Point(Math.Clamp(wanted.X, workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - size.Width)), Math.Clamp(wanted.Y, workingArea.Top, Math.Max(workingArea.Top, workingArea.Bottom - size.Height)));
	}

	internal void ReadNow()
	{
		_poller.ReadForTestOrRefresh(ShowSnapshot);
	}

	private void ShowSnapshot(NowPlaying? now)
	{
		Showing = now;
		_buttons[0].Enabled = now?.CanPrevious ?? false;
		_buttons[1].Enabled = now?.CanPlayPause ?? false;
		_buttons[1].IsPlaying = now?.IsPlaying ?? false;
		_buttons[2].Enabled = now?.CanNext ?? false;
		Invalidate();
	}

	internal void Toggle()
	{
		if (Visible)
		{
			Hide();
		}
		else
		{
			Show();
		}
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		base.OnVisibleChanged(e);
		if (!Visible)
		{
			RememberPosition();
		}
	}

	protected override void OnResizeEnd(EventArgs e)
	{
		base.OnResizeEnd(e);
		RememberPosition();
	}

	private void RememberPosition()
	{
		if (_settings != null && IsHandleCreated && (_settings.MusicMiniX != Left || _settings.MusicMiniY != Top))
		{
			_settings.MusicMiniX = Left;
			_settings.MusicMiniY = Top;
			_savePosition?.Invoke();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			RememberPosition();
		}
		base.Dispose(disposing);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		int num = S(14);
		if (Showing?.Art != null)
		{
			Rectangle rectangle = new Rectangle(S(14), S(14), S(56), S(56));
			using GraphicsPath clip = Glass.RoundedPath(rectangle, S(6));
			using Region region = e.Graphics.Clip;
			e.Graphics.SetClip(clip);
			e.Graphics.DrawImage(Showing.Art, rectangle);
			e.Graphics.SetClip(region, CombineMode.Replace);
			num = rectangle.Right + S(10);
		}
		TextFormatFlags flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
		int width = ClientSize.Width - num - S(32);
		if (Showing != null)
		{
			TextRenderer.DrawText(e.Graphics, SongText, Fonts.BodyBold, new Rectangle(num, S(12), width, Fonts.BodyBold.Height), ControlInk.Text1, flags);
		}
		TextRenderer.DrawText(e.Graphics, ArtistText, Fonts.Caption, new Rectangle(num, S(14) + Fonts.BodyBold.Height, width, Fonts.Caption.Height), (Showing != null) ? ControlInk.Text2 : ControlInk.Text3, flags);
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if (e.CloseReason == CloseReason.UserClosing)
		{
			e.Cancel = true;
			Hide();
		}
		else
		{
			base.OnFormClosing(e);
		}
	}
}
