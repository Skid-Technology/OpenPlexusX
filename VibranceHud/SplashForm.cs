using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class SplashForm : Form
{
	private enum TickFormat
	{
		Left,
		Centre,
		Right
	}

	private const int WindowW = 440;

	private const int WindowH = 250;

	private const int PadX = 32;

	private const int LockupTop = 40;

	private const int MarkSize = 32;

	private const int MarkInk = 29;

	private const int MarkGap = 7;

	private const int BarTop = 120;

	private const int BarW = 376;

	private const int BarH = 6;

	private const int SignalMax = 300;

	private const int SignalNeutral = 100;

	private const int TickTop = 134;

	private const int SignalSvgH = 31;

	private const int StatusTop = 162;

	private const int StatusLine = 20;

	private const int VersionW = 80;

	private const int RevealMs = 900;

	private readonly int _dpi;

	private readonly Timer _timer;

	private readonly Stopwatch _reveal = Stopwatch.StartNew();

	private string _status = "Starting…";

	private string _statusStrong = "";

	private int _progress = -1;

	private int _signalValue = -1;

	private const int DwmaCornerPreference = 33;

	private const int DwmcpRound = 2;

	private bool _cornersAreTheCompositors;

	private static Font BrandFont => Fonts.Display;

	private static Font StatusFont => Fonts.Body;

	private static Font StatusStrongFont => Fonts.BodyBold;

	private static Font TickFont => Fonts.Caption;

	internal string CornerRouteForTest
	{
		get
		{
			if (Region == null)
			{
				if (!_cornersAreTheCompositors)
				{
					return "square";
				}
				return "dwm";
			}
			return "region";
		}
	}

	private int S(int logical)
	{
		return Tokens.ScaleAt(_dpi, logical);
	}

	public SplashForm()
	{
		_dpi = SignalInk.DpiFor(this);
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterScreen;
		ShowInTaskbar = false;
		TopMost = true;
		ClientSize = new Size(S(440), S(250));
		BackColor = Theme.Background;
		DoubleBuffered = true;
		Icon = AppIcon.Value;
		_timer = new Timer
		{
			Interval = 16
		};
		_timer.Tick += (object? s, EventArgs e) =>
		{
			Invalidate();
			if (_reveal.ElapsedMilliseconds >= 900)
			{
				_timer.Stop();
			}
		};
		_timer.Start();
	}

	public void SetStatus(string text, int percent = -1)
	{
		if (InvokeRequired)
		{
			BeginInvoke(() =>
			{
				SetStatus(text, percent);
			});
			return;
		}
		_status = text;
		_statusStrong = "";
		_progress = percent;
		_signalValue = -1;
		Invalidate();
	}

	public void SetRestoring(string lookName, int saturationPercent)
	{
		if (InvokeRequired)
		{
			BeginInvoke(() =>
			{
				SetRestoring(lookName, saturationPercent);
			});
			return;
		}
		_status = "Putting back ";
		_statusStrong = $"{lookName} · {saturationPercent}%";
		_progress = -1;
		_signalValue = Math.Clamp(saturationPercent, 0, 300);
		Invalidate();
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		int value = 2;
		try
		{
			_cornersAreTheCompositors = DwmSetWindowAttribute(Handle, 33, ref value, 4) == 0;
		}
		catch (DllNotFoundException)
		{
			_cornersAreTheCompositors = false;
		}
		catch (EntryPointNotFoundException)
		{
			_cornersAreTheCompositors = false;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using (SolidBrush brush = new SolidBrush(Theme.Background))
		{
			graphics.FillRectangle(brush, ClientRectangle);
		}
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		int width = TextRenderer.MeasureText(graphics, "PlexusX", BrandFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
		int num = S(32);
		int num2 = num + S(7) + width;
		int num3 = (ClientSize.Width - num2) / 2;
		int num4 = S(40);
		int num5 = (num - S(29)) / 2;
		Rectangle r = new Rectangle(num3 + num5, num4 + num5, S(29), S(29));
		using (SolidBrush brush2 = new SolidBrush(Theme.Accent))
		{
			graphics.FillPolygon(brush2, StarPoints(r));
		}
		TextRenderer.DrawText(graphics, "PlexusX", BrandFont, new Rectangle(num3 + num + S(7), SignalInk.TopForBaseline(graphics, BrandFont, num4, num), width, BrandFont.Height), Theme.Text, TextFormatFlags.NoPadding);
		Rectangle bar = new Rectangle(S(32), S(120), S(376), S(6));
		DrawSignal(graphics, bar, SignalValueForDisplay(), S(5));
		int num6 = S(134);
		int num7 = Math.Max(S(16), TickFont.Height);
		num6 -= (num7 - S(16)) / 2;
		DrawTick(graphics, "0", bar.X, num6, num7, TickFormat.Left);
		DrawTick(graphics, 100.ToString(CultureInfo.InvariantCulture), bar.X + bar.Width * 100 / 300, num6, num7, TickFormat.Centre);
		DrawTick(graphics, 200.ToString(CultureInfo.InvariantCulture), bar.X + bar.Width * 100 * 2 / 300, num6, num7, TickFormat.Centre);
		DrawTick(graphics, 300.ToString(CultureInfo.InvariantCulture), bar.Right, num6, num7, TickFormat.Right);
		int num8 = Math.Max(S(20), StatusStrongFont.Height);
		int y = S(162) - (num8 - S(20)) / 2;
		int num9 = S(80);
		int width2 = TextRenderer.MeasureText(graphics, _status, StatusFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
		TextFormatFlags flags = TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
		TextRenderer.DrawText(graphics, _status, StatusFont, new Rectangle(bar.X, y, bar.Width - num9, num8), Theme.TextDim, flags);
		if (_statusStrong.Length > 0)
		{
			TextRenderer.DrawText(graphics, _statusStrong, StatusStrongFont, new Rectangle(bar.X + width2, y, Math.Max(1, bar.Width - width2 - num9), num8), Theme.Text, flags);
		}
		TextRenderer.DrawText(graphics, AppInfo.VersionText, TickFont, new Rectangle(bar.Right - num9, y, num9, num8), SignalInk.Faint, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		SmoothingMode smoothingMode = graphics.SmoothingMode;
		graphics.SmoothingMode = SmoothingMode.None;
		using (Pen pen = new Pen(SignalInk.WindowEdge, 1f))
		{
			graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
		}
		graphics.SmoothingMode = smoothingMode;
	}

	private int SignalValueForDisplay()
	{
		if (_signalValue < 0)
		{
			if (_progress >= 0)
			{
				return Math.Clamp(_progress * 3, 0, 300);
			}
			return 0;
		}
		return _signalValue;
	}

	private void DrawTick(Graphics g, string text, int x, int top, int line, TickFormat anchor)
	{
		int num = S(80);
		int x2 = anchor switch
		{
			TickFormat.Right => x - num, 
			TickFormat.Centre => x - num / 2, 
			_ => x, 
		};
		TextRenderer.DrawText(g, text, TickFont, new Rectangle(x2, top, num, line), SignalInk.Faint, (anchor switch
		{
			TickFormat.Right => TextFormatFlags.Right, 
			TickFormat.Centre => TextFormatFlags.HorizontalCenter, 
			_ => TextFormatFlags.Default, 
		}) | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
	}

	internal static void DrawSignal(Graphics g, Rectangle bar, int value, int notchProud, int max = 300, int neutral = 100)
	{
		PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		try
		{
			value = Math.Clamp(value, 0, max);
			float num = (float)bar.X + (float)bar.Width * ((float)neutral / (float)max);
			float num2 = bar.Right;
			float num3 = (float)bar.X + (float)bar.Width * ((float)value / (float)max);
			float num4 = Math.Max(2f, (float)bar.Height / 3f);
			if (num3 < num2)
			{
				using SolidBrush brush = new SolidBrush(Theme.AccentDim);
				g.FillRectangle(brush, num3, (float)bar.Y + (float)bar.Height / 2f - num4 / 2f, num2 - num3, num4);
			}
			float num5 = Math.Min(num3, num);
			if (num5 > (float)bar.X)
			{
				using SolidBrush brush2 = new SolidBrush(Theme.Accent);
				g.FillRectangle(brush2, bar.X, bar.Y, num5 - (float)bar.X, bar.Height);
			}
			if (value > neutral && num2 > num)
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
				float num6 = (float)bar.Height / (float)array.Length;
				for (int i = 0; i < array.Length; i++)
				{
					float y = (float)bar.Y + (float)i * num6;
					float num7 = Math.Max(0f, num3 - num);
					if (!(num7 <= 0f))
					{
						using LinearGradientBrush brush3 = new LinearGradientBrush(new RectangleF(num, y, num2 - num, Math.Max(1f, num6)), Theme.Accent, array[i], LinearGradientMode.Horizontal);
						g.FillRectangle(brush3, new RectangleF(num, y, num7, Math.Max(1f, num6)));
					}
				}
			}
			if (notchProud > 0)
			{
				using (SolidBrush brush4 = new SolidBrush(SignalInk.Faint))
				{
					g.FillRectangle(brush4, num - 0.5f, bar.Y - notchProud, 1f, (float)bar.Height + 2f * (float)notchProud);
					return;
				}
			}
		}
		finally
		{
			g.PixelOffsetMode = pixelOffsetMode;
		}
	}

	private static Point[] StarPoints(Rectangle r)
	{
		float num = (float)r.X + (float)r.Width / 2f;
		float num2 = (float)r.Y + (float)r.Height / 2f;
		float num3 = (float)r.Width / 2f;
		float num4 = num3 * 0.4f;
		Point[] array = new Point[24];
		for (int i = 0; i < 24; i++)
		{
			double num5 = Math.PI / 12.0 * (double)i - Math.PI / 2.0;
			float num6 = ((i % 2 == 0) ? num3 : num4);
			array[i] = new Point((int)((double)num + (double)num6 * Math.Cos(num5)), (int)((double)num2 + (double)num6 * Math.Sin(num5)));
		}
		return array;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_timer?.Stop();
			_timer?.Dispose();
		}
		base.Dispose(disposing);
	}
}
