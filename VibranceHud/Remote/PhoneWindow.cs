using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;

namespace VibranceHud.Remote;

internal sealed class PhoneWindow : SignalDialogWindow
{
	private readonly PhoneListener _listener;

	private readonly Label _address = new Label();

	private readonly Label _advice = new Label();

	private readonly Label _hint = new Label();

	private readonly GlassButton _otherNetwork = new GlassButton();

	private readonly GlassButton _close = new GlassButton();

	private readonly Timer _hintTimer = new Timer();

	private IReadOnlyList<string> _candidates = Array.Empty<string>();

	private int _candidate;

	private Bitmap? _qr;

	private bool _paired;

	private SignalIcon _statusIcon;

	private const int QrSide = 172;

	private const int TilePad = 16;

	private const int TileSide = 204;

	private const int WindowWidth = 460;

	private const int Pad = 24;

	private const int TitleTop = 23;

	private const int TitleLine = 28;

	private const int LineTop = 60;

	private const int LineHeight = 20;

	private const int QrTop = 96;

	private const int AddressTop = 316;

	private const int FieldHeight = 32;

	private const int HelpTop = 352;

	private const int CaptionLine = 16;

	private const int StatusTop = 384;

	private const int IconSize = 16;

	private const int IconGap = 8;

	private const int TileRadius = 8;

	private const int FieldRadius = 4;

	private readonly float _scale = 1f;

	internal Rectangle QrTileForTest => Tile();

	internal Rectangle AddressFieldForTest => _address.Bounds;

	internal Label AddressLabelForTest => _address;

	internal float ScaleForTest => _scale;

	internal Label StatusLabelForTest => _hint;

	internal SignalIcon StatusIconForTest => _statusIcon;

	internal Color StatusInkForTest => InkFor(_statusIcon);

	internal PhoneWindow(PhoneListener listener)
	{
		_listener = listener;
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterParent;
		BackColor = ControlInk.Overlay;
		DoubleBuffered = true;
		Icon = AppIcon.Value;
		KeyPreview = true;
		_scale = (float)SignalInk.DpiFor(this) / 96f;
		int num = 412;
		int num2 = (int)Math.Round((float)SignalDialogWindow.GlyphPad(Fonts.Title) / _scale);
		Controls.Add(Words("Control PlexusX from your phone", Fonts.Title, Theme.Text, new Rectangle(26 - num2, 23, num, 28), ContentAlignment.TopLeft));
		int num3 = (int)Math.Round((float)SignalDialogWindow.GlyphPad(Fonts.Body) / _scale);
		if (_scale == 1f || _scale == 2f)
		{
			num3++;
		}
		Controls.Add(Words("Scan it. Your phone has to be on the same Wi-Fi.", Fonts.Body, Theme.TextDim, new Rectangle(26 - num3, 60, num + num3 - 2, 20), ContentAlignment.TopLeft));
		_address.Bounds = new Rectangle(128, 316, 204, 32);
		_address.ForeColor = Theme.Text;
		_address.Font = Fonts.Mono;
		_address.TextAlign = ContentAlignment.MiddleCenter;
		_address.BackColor = Color.Transparent;
		_address.UseMnemonic = false;
		Controls.Add(_address);
		Controls.Add(Words("Or type that into your phone's browser.", Fonts.Caption, Theme.TextFaint, new Rectangle(24, 352, num, 16), ContentAlignment.TopCenter));
		int num4 = 48;
		int width = 436 - num4;
		int num5 = Measure(WrongNetworkLine("255.255.255.255", "255.255.255.255"), Fonts.Caption, width, 16);
		_hint.Bounds = new Rectangle(num4, 384, width, num5);
		_hint.ForeColor = Theme.Text;
		_hint.Font = Fonts.Caption;
		_hint.TextAlign = ContentAlignment.TopLeft;
		_hint.BackColor = Color.Transparent;
		_hint.UseMnemonic = false;
		_hint.Visible = false;
		Controls.Add(_hint);
		int num6 = 384 + num5 + 16;
		int num7 = 16;
		foreach (NetworkCategory value in Enum.GetValues(typeof(NetworkCategory)))
		{
			num7 = Math.Max(num7, Measure(PhoneSession.FirewallAdvice(value), Fonts.Caption, num, 16));
		}
		int num8 = (int)Math.Round((float)SignalDialogWindow.GlyphPad(Fonts.Caption) / _scale);
		if (_scale == 1f)
		{
			num8 += 2;
		}
		_advice.Bounds = new Rectangle(26 - num8, num6, num + num8 - 2, num7);
		_advice.ForeColor = Theme.TextDim;
		_advice.Font = Fonts.Caption;
		_advice.BackColor = Color.Transparent;
		_advice.UseMnemonic = false;
		Controls.Add(_advice);
		int num9 = 32;
		int num10 = num6 + num7 + 24;
		_close.Text = "Turn off and close";
		int num11 = FooterWidth(_close);
		_close.SetBounds(436 - num11, num10, num11, num9);
		_close.Click += (object? s, EventArgs e) =>
		{
			Close();
		};
		Controls.Add(_close);
		_otherNetwork.Text = "Different network?";
		int num12 = FooterWidth(_otherNetwork);
		_otherNetwork.SetBounds(_close.Left - 8 - num12, num10, num12, num9);
		_otherNetwork.Click += (object? s, EventArgs e) =>
		{
			CycleAddress();
		};
		Controls.Add(_otherNetwork);
		ClientSize = new Size(460, num10 + num9 + 20);
		_hintTimer.Interval = (int)PhoneSession.HintAfter.TotalMilliseconds;
		_hintTimer.Tick += (object? s, EventArgs e) =>
		{
			_hintTimer.Stop();
			ShowWaitingHint();
		};
		_listener.PhoneArrived += OnPhoneArrived;
		_listener.WrongNetworkSeen += OnWrongNetwork;
		if (_scale != 1f)
		{
			Scale(new SizeF(_scale, _scale));
		}
	}

	private int Px(int logical)
	{
		return (int)Math.Round((float)logical * _scale);
	}

	private static Label Words(string text, Font font, Color ink, Rectangle bounds, ContentAlignment align)
	{
		return new Label
		{
			Text = text,
			Font = font,
			ForeColor = ink,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = align,
			UseMnemonic = false,
			Bounds = bounds
		};
	}

	private int Measure(string text, Font font, int width, int line)
	{
		int height = TextRenderer.MeasureText(text, font, new Size(Math.Max(1, Px(width)), int.MaxValue), TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak).Height;
		return Math.Max(1, (int)Math.Round((double)height / (double)Math.Max(1, font.Height))) * line;
	}

	private int FooterWidth(GlassButton button)
	{
		return Math.Max(96, (int)Math.Ceiling((float)button.GetPreferredSize(Size.Empty).Width / _scale));
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		_candidates = NetworkFacts.Candidates();
		_advice.Text = PhoneSession.FirewallAdvice(NetworkFacts.Category());
		_listener.Start();
		ShowCurrentAddress();
		_hintTimer.Start();
	}

	private void CycleAddress()
	{
		if (_candidates.Count > 1)
		{
			_candidate = (_candidate + 1) % _candidates.Count;
			ShowCurrentAddress();
		}
	}

	private void ShowCurrentAddress()
	{
		string text = ((_candidates.Count > 0) ? _candidates[_candidate] : "");
		_listener.AdvertisedAddress = text;
		_otherNetwork.Visible = _candidates.Count > 1;
		if (string.IsNullOrEmpty(text))
		{
			_address.Text = "No network found.";
			_qr?.Dispose();
			_qr = null;
			Invalidate();
			return;
		}
		string text2 = PhoneSession.Url(text, _listener.Port, _listener.Token);
		_address.Text = $"{text}:{_listener.Port}";
		_qr?.Dispose();
		_qr = RenderQr(text2);
		Invalidate();
	}

	private void ShowWaitingHint()
	{
		if (!_paired)
		{
			ShowStatus(SignalIcon.Info, "Nothing has connected yet. Check the phone is on the same Wi-Fi.");
		}
	}

	private void ShowStatus(SignalIcon icon, string words)
	{
		_statusIcon = icon;
		_hint.ForeColor = Theme.Text;
		_hint.Text = words;
		_hint.Visible = true;
		Invalidate(new Rectangle(Px(24), _hint.Top, Px(16), Px(16)));
	}

	private static Color InkFor(SignalIcon icon)
	{
		return icon switch
		{
			SignalIcon.Success => ControlInk.Success, 
			SignalIcon.Warning => ControlInk.Warning, 
			SignalIcon.Danger => ControlInk.Danger, 
			_ => ControlInk.Info, 
		};
	}

	private void OnPhoneArrived()
	{
		if (IsDisposed)
		{
			return;
		}
		try
		{
			BeginInvoke(() =>
			{
				_paired = true;
				ShowStatus(SignalIcon.Success, "Connected. Your phone is controlling this PC.");
			});
		}
		catch
		{
		}
	}

	private void OnWrongNetwork(string theirs, string ours)
	{
		if (IsDisposed)
		{
			return;
		}
		try
		{
			BeginInvoke(() =>
			{
				if (!_paired)
				{
					_hintTimer.Stop();
					ShowStatus(SignalIcon.Warning, WrongNetworkLine(theirs, ours));
				}
			});
		}
		catch
		{
		}
	}

	private static string WrongNetworkLine(string theirs, string ours)
	{
		return $"A phone on {theirs} tried to connect. This PC is on {ours} - " + "different networks. Take the phone off guest Wi-Fi or mobile data.";
	}

	private static Bitmap RenderQr(string text)
	{
		bool[,] array = QrCode.Encode(text);
		int length = array.GetLength(0);
		int num = length + 8;
		int num2 = Math.Max(1, 172 / num);
		int num3 = num * num2;
		Bitmap bitmap = new Bitmap(num3, num3);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.SmoothingMode = SmoothingMode.None;
		graphics.Clear(Color.White);
		using SolidBrush brush = new SolidBrush(Color.Black);
		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < length; j++)
			{
				if (array[i, j])
				{
					graphics.FillRectangle(brush, (j + 4) * num2, (i + 4) * num2, num2, num2);
				}
			}
		}
		return bitmap;
	}

	private Rectangle Tile()
	{
		int num = Px(204);
		return new Rectangle((ClientSize.Width - num) / 2, Px(96), num, num);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		GraphicsState gstate = graphics.Save();
		try
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			Rectangle bounds = _address.Bounds;
			int num = ControlInk.Hairline(SignalInk.DpiFor(this));
			using (GraphicsPath path = Glass.RoundedPath(bounds, Px(4)))
			{
				using SolidBrush brush = new SolidBrush(ControlInk.Border);
				graphics.FillPath(brush, path);
			}
			using (GraphicsPath path2 = Glass.RoundedPath(RectangleF.Inflate(bounds, -num, -num), Math.Max(1, Px(4) - num)))
			{
				using SolidBrush brush2 = new SolidBrush(ControlInk.Surface2);
				graphics.FillPath(brush2, path2);
			}
			if (_hint.Visible && _statusIcon != SignalIcon.None)
			{
				SignalIcons.Draw(graphics, _statusIcon, new RectangleF(Px(24), _hint.Top, Px(16), Px(16)), InkFor(_statusIcon));
			}
			Bitmap qr = _qr;
			if (qr == null)
			{
				return;
			}
			Rectangle rectangle = Tile();
			int num2 = Px(16);
			using (GraphicsPath path3 = Glass.RoundedPath(rectangle, Px(8)))
			{
				using SolidBrush brush3 = new SolidBrush(Color.White);
				graphics.FillPath(brush3, path3);
			}
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.DrawImage(qr, new Rectangle(rectangle.X + num2, rectangle.Y + num2, rectangle.Width - 2 * num2, rectangle.Height - 2 * num2));
		}
		finally
		{
			graphics.Restore(gstate);
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Escape)
		{
			Close();
		}
		base.OnKeyDown(e);
	}

	internal void ShowWaitingHintForTest()
	{
		ShowWaitingHint();
	}

	internal void PhoneArrivedForTest()
	{
		OnPhoneArrived();
	}

	internal void WrongNetworkForTest(string theirs, string ours)
	{
		OnWrongNetwork(theirs, ours);
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		_listener.PhoneArrived -= OnPhoneArrived;
		_listener.WrongNetworkSeen -= OnWrongNetwork;
		_listener.Stop();
		_hintTimer.Stop();
		_hintTimer.Dispose();
		_qr?.Dispose();
		_qr = null;
		base.OnFormClosed(e);
	}
}
