using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class RevertConfirmForm : SignalDialogWindow
{
	private static class Shape
	{
		public const int Width = 462;

		public const int Pad = 24;

		public const int TitleY = 23;

		public const int BodyY = 60;

		public const int BarGap = 26;

		public const int FillH = 4;

		public const int TrackH = 2;

		public const int CountdownW = 24;

		public const int CountdownGap = 14;

		public const int CaptionGap = 15;

		public const int ButtonGap = 39;

		public const int BottomPad = 20;

		public const int ButtonPad = 20;

		public const int ButtonSplit = 8;

		public const int Radius = 8;
	}

	private sealed class ProgressTrack : Control
	{
		public float Fraction { get; set; } = 1f;

		public ProgressTrack()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			SetStyle(ControlStyles.Selectable, value: false);
			TabStop = false;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			float num = Math.Max(1, Tokens.Scale(2));
			float y = ((float)Height - num) / 2f;
			using (SolidBrush brush = new SolidBrush(Theme.AccentDim))
			{
				FillPill(graphics, brush, new RectangleF(0f, y, Width, num), num / 2f);
			}
			float num2 = Math.Clamp(Fraction, 0f, 1f) * (float)Width;
			if (num2 >= (float)Height)
			{
				using (SolidBrush brush2 = new SolidBrush(Theme.Accent))
				{
					FillPill(graphics, brush2, new RectangleF(0f, 0f, num2, Height), (float)Height / 2f);
				}
			}
		}

		private static void FillPill(Graphics g, Brush brush, RectangleF r, float radius)
		{
			if (r.Width < 1f || r.Height < 1f)
			{
				return;
			}
			using GraphicsPath path = Glass.RoundedPath(r, radius);
			g.FillPath(brush, path);
		}
	}

	private readonly Timer _timer;

	private readonly Label _title;

	private readonly Label _body;

	private readonly ProgressTrack _progress;

	private readonly Label _countdown;

	private readonly Label _safetyCaption;

	private readonly GlassButton _keep;

	private readonly GlassButton _goBack;

	private readonly int _totalSeconds;

	private int _secondsLeft;

	private static readonly TimeSpan ArmAfter = TimeSpan.FromMilliseconds(400.0);

	private bool _armed;

	internal GlassButton TestKeepButton => _keep;

	internal GlassButton TestGoBackButton => _goBack;

	internal string TestCountdownText => _countdown.Text;

	internal float TestProgressFraction => _progress.Fraction;

	internal string TestMessageText => _body.Text;

	internal string TestSafetyCaptionText => _safetyCaption.Text;

	internal int TestTimerIntervalMs => _timer.Interval;

	internal static int TestArmDelayMs => (int)ArmAfter.TotalMilliseconds;

	public event Action? Confirmed;

	private new static int S(int logical)
	{
		return Tokens.Scale(logical);
	}

	public RevertConfirmForm(string message, int graceSeconds)
	{
		_totalSeconds = Math.Max(1, graceSeconds);
		_secondsLeft = _totalSeconds;
		StartPosition = FormStartPosition.CenterScreen;
		TopMost = true;
		AccessibleName = "Keep this display mode?";
		AccessibleDescription = message;
		int num = S(24);
		int num2 = S(462);
		int num3 = num2 - 2 * num;
		_title = SignalDialogWindow.TitleLabel("Keep this display mode?", num + S(2), S(23), num3);
		Controls.Add(_title);
		int height = Math.Max(2 * Fonts.LineHeight(FontRole.Body), TextRenderer.MeasureText(message, Fonts.Body, new Size(num3, int.MaxValue), TextFormatFlags.WordBreak).Height);
		_body = SignalDialogWindow.BodyLabel(message, num + S(2), S(60), num3, height);
		Controls.Add(_body);
		int num4 = _body.Bottom + S(26);
		int num5 = S(24);
		int num6 = num3 - num5 - S(14);
		_progress = new ProgressTrack
		{
			Location = new Point(num, num4),
			Size = new Size(num6, S(4))
		};
		Controls.Add(_progress);
		_countdown = new Label
		{
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Font = Fonts.CaptionBold,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleRight,
			UseMnemonic = false,
			Location = new Point(num + num6 + S(14), num4 + S(4) / 2 - S(8)),
			Size = new Size(num5 + SignalDialogWindow.GlyphPad(Fonts.CaptionBold), S(16))
		};
		Controls.Add(_countdown);
		int num7 = num4 + S(4) + S(15);
		_safetyCaption = new Label
		{
			Text = "Doing nothing is safe: the old mode comes back.",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			AutoSize = false,
			TextAlign = ContentAlignment.TopLeft,
			UseMnemonic = false,
			Location = new Point(SignalDialogWindow.Ink(num + S(2), Fonts.Caption), num7),
			Size = new Size(num3, Fonts.LineHeight(FontRole.Caption))
		};
		Controls.Add(_safetyCaption);
		int num8 = num7 + _safetyCaption.Height + S(39);
		int num9 = S(32);
		_keep = new GlassButton
		{
			Text = "Keep this mode",
			Kind = GlassButtonKind.Primary,
			Name = "RevertConfirmKeep"
		};
		_keep.Size = new Size(ButtonWidth(_keep.Text, _keep.Font), num9);
		_keep.Location = new Point(num2 - num - _keep.Width, num8);
		_keep.Click += (object? _, EventArgs _) =>
		{
			Accept();
		};
		Controls.Add(_keep);
		_goBack = new GlassButton
		{
			Text = "Go back now",
			Kind = GlassButtonKind.Secondary,
			Name = "RevertConfirmGoBack"
		};
		_goBack.Size = new Size(ButtonWidth(_goBack.Text, _goBack.Font), num9);
		_goBack.Location = new Point(_keep.Left - S(8) - _goBack.Width, num8);
		_goBack.Click += (object? _, EventArgs _) =>
		{
			Close();
		};
		Controls.Add(_goBack);
		_goBack.TabIndex = 0;
		_keep.TabIndex = 1;
		ActiveControl = _goBack;
		ClientSize = new Size(num2, num8 + num9 + S(20));
		UpdateCountdown();
		_timer = new Timer
		{
			Interval = 1000
		};
		_timer.Tick += (object? _, EventArgs _) =>
		{
			Tick();
		};
		KeyDown += (object? _, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Escape)
			{
				Swallow(e);
				GoBack();
			}
			else if (e.KeyCode == Keys.Return)
			{
				Swallow(e);
				if (_armed)
				{
					GoBack();
				}
			}
			else if (!_armed)
			{
				Swallow(e);
			}
		};
	}

	private static void Swallow(KeyEventArgs e)
	{
		e.Handled = true;
		e.SuppressKeyPress = true;
	}

	private void GoBack()
	{
		_timer.Stop();
		_goBack.PerformClick();
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		if ((keyData & (Keys.Control | Keys.Alt)) == 0)
		{
			switch (keyData & Keys.KeyCode)
			{
			case Keys.Escape:
				GoBack();
				return true;
			case Keys.Return:
				if (_armed)
				{
					GoBack();
				}
				return true;
			case Keys.Tab:
				if (!_armed)
				{
					return true;
				}
				break;
			case Keys.Left:
			case Keys.Up:
			case Keys.Right:
			case Keys.Down:
				return true;
			}
		}
		return base.ProcessDialogKey(keyData);
	}

	private static int ButtonWidth(string text, Font font)
	{
		return TextRenderer.MeasureText(text, font).Width + S(20);
	}

	private void Accept()
	{
		_timer.Stop();
		Confirmed?.Invoke();
		Close();
	}

	private void Tick()
	{
		_secondsLeft--;
		if (_secondsLeft <= 0)
		{
			Close();
		}
		else
		{
			UpdateCountdown();
		}
	}

	private void UpdateCountdown()
	{
		_countdown.Text = $"{_secondsLeft} s";
		_progress.Fraction = ((_totalSeconds <= 0) ? 0f : ((float)_secondsLeft / (float)_totalSeconds));
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		_timer.Start();
		ActiveControl = _goBack;
		Activate();
		Timer arm = new Timer
		{
			Interval = (int)ArmAfter.TotalMilliseconds
		};
		arm.Tick += (object? _, EventArgs _) =>
		{
			_armed = true;
			arm.Stop();
			arm.Dispose();
		};
		arm.Start();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_timer.Dispose();
		}
		base.Dispose(disposing);
	}

	internal void TestAdvanceOneSecond()
	{
		Tick();
	}

	internal void TestArmEnterNow()
	{
		_armed = true;
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}
}
