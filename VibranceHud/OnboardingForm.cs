using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Onboarding;

namespace VibranceHud;

public sealed class OnboardingForm : Form
{
	internal sealed class GameRow : Control
	{
		private bool _active;

		private readonly HoverFade _fade;

		private const int PadX = 12;

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

		public GameRow()
		{
			_fade = new HoverFade(this);
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Theme.Background;
			Cursor = Cursors.Hand;
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
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
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
			PixelOffsetMode pixelOffsetMode = graphics.PixelOffsetMode;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			int dpi = SignalInk.DpiFor(this);
			using (GraphicsPath path = Glass.RoundedPath(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Sc(4)))
			{
				if (_active)
				{
					using SolidBrush brush = new SolidBrush(Theme.Accent);
					graphics.FillPath(brush, path);
				}
				else if (_fade.Value > 0.01f)
				{
					using SolidBrush brush2 = new SolidBrush(Theme.SurfaceHover);
					graphics.FillPath(brush2, path);
					using Pen pen = new Pen(Theme.GlassEdge, 1f);
					graphics.DrawPath(pen, path);
				}
				else
				{
					using Pen pen2 = new Pen(Theme.GlassEdge, 1f);
					graphics.DrawPath(pen2, path);
				}
			}
			Color foreColor = (_active ? Theme.OnAccent : Theme.Text);
			Font font = (_active ? Fonts.BodyBold : Fonts.Body);
			int num = Sc(12);
			TextRenderer.DrawText(graphics, Text, font, new Rectangle(num, 0, Math.Max(1, Width - num * 2), Height), foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			graphics.PixelOffsetMode = pixelOffsetMode;
			if (Focused && ShowFocusCues)
			{
				UiHelpers.DrawFocusRing(graphics, ClientRectangle, Sc(4));
			}
			int Sc(int v)
			{
				return Tokens.ScaleAt(dpi, v);
			}
		}
	}

	private const int WindowW = 680;

	private const int WindowH = 520;

	private const int HeaderH = 40;

	private const int HeaderPadX = 16;

	private const int MarkSize = 20;

	private const int MarkInk = 19;

	private const int MarkGap = 4;

	private const int CloseCellW = 46;

	private const int Gutter = 24;

	private const int ContentTop = 56;

	private const int DashW = 20;

	private const int DashH = 2;

	private const int DashGap = 4;

	private const int DashLead = 10;

	private const int TitleGap = 8;

	private const int SubGap = 4;

	private const int GridGap = 20;

	private const int TitleInkLift = 2;

	private const int SubInkLift = 1;

	private const int RowW = 212;

	private const int RowH = 40;

	private const int RowGap = 8;

	private const int ColGap = 24;

	private const int RightW = 396;

	private const int SplitH = 216;

	private const int CaptionGap = 8;

	private const int BoldGap = 10;

	private const int FooterH = 64;

	private const int ButtonH = 32;

	private const int ButtonPadX = 12;

	private const int SeamW = 2;

	private const int HandleSize = 36;

	private const int HandleMark = 20;

	private const int ChipInset = 8;

	private const int ChipH = 24;

	private const int ChipPadX = 8;

	private const int ChipRadius = 4;

	private readonly Timer _timer;

	private DateTime _last = DateTime.UtcNow;

	private float _reveal;

	private int _step;

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly GlassButton _primary;

	private readonly GlassButton _back;

	private readonly GlassButton _skip;

	private readonly List<GameRow> _gameRows = new List<GameRow>();

	private readonly List<GameRow> _complaintChips = new List<GameRow>();

	private readonly SetupAnswers _answers = new SetupAnswers();

	private string _favoriteGame = "";

	private readonly int _dpi;

	private readonly int _stepLine;

	private readonly int _titleTop;

	private readonly int _titleLine;

	private readonly int _subTop;

	private readonly int _subLine;

	private readonly int _gridTop;

	private readonly int _footerRuleY;

	private readonly int _buttonTop;

	private readonly int _rightX;

	private readonly int _contentW;

	private readonly int _splitH;

	private readonly VibranceEngine? _demo;

	private string _demoLine = "";

	private const int LastStep = 2;

	private const int CountedSteps = 2;

	private const string GameTitle = "Which game do you play most?";

	private const string GameSubtitle = "We'll start you on its presets. You can change it any time.";

	private const string ComplaintTitle = "What do you want it to fix?";

	private const string ComplaintSubtitle = "Pick one - your screen changes to match.";

	private const string SampleFrameLine = "A sample scene. Pick a game to see its look on your screen.";

	private const string NeverTouchesLine = "Changes what your monitor shows, never touches the game.";

	private const string WelcomeTitle = "Welcome to PlexusX";

	private const string WelcomeLead = "Let's set it up.";

	private const string UnknownCaptureLine = "Settings shows whether your recordings get your colours.";

	private readonly string[] _welcomeLines;

	private readonly Rectangle[] _welcomeBands = new Rectangle[4];

	internal static IReadOnlyList<string> WelcomeLines => WelcomeLinesWith("Settings shows whether your recordings get your colours.");

	internal IReadOnlyList<string> WelcomeLinesShownForTest => _welcomeLines;

	internal Rectangle[] WelcomeBandsForTest => (Rectangle[])_welcomeBands.Clone();

	internal static int CountedStepCount => 2;

	internal static string SampleLine => "A sample scene. Pick a game to see its look on your screen.";

	private static Font TitleFont => Fonts.Display;

	private static Font SubFont => Fonts.Body;

	private static Font StepFont => Fonts.Caption;

	private static Font CaptionFont => Fonts.Caption;

	private static Font BrandFont => Fonts.Title;

	internal bool BackVisibleForTest => _back.Visible;

	internal static int StepCount => 3;

	internal bool ShouldDemonstrate => _demo?.ScreenEffectIsLive ?? false;

	private static string CaptureLineFor(OverlayMode overlay, bool driverVibranceAvailable, bool streamingMode)
	{
		return CaptureStatus.Headline(CaptureStatus.Resolve(overlay, driverVibranceAvailable, streamingMode));
	}

	internal static IReadOnlyList<string> WelcomeLinesFor(OverlayMode overlay, bool driverVibranceAvailable, bool streamingMode)
	{
		return WelcomeLinesWith(CaptureLineFor(overlay, driverVibranceAvailable, streamingMode));
	}

	private static string[] WelcomeLinesWith(string captureLine)
	{
		return new string[4] { "Welcome to PlexusX", "Sharper colours. Every game.", "Let's set it up.", captureLine };
	}

	private int S(int logical)
	{
		return Tokens.ScaleAt(_dpi, logical);
	}

	public OnboardingForm(AppSettings settings, SettingsStore store, VibranceEngine? demo = null)
	{
		_settings = settings;
		_store = store;
		_demo = demo;
		_dpi = SignalInk.DpiFor(this);
		_welcomeLines = ((demo == null) ? WelcomeLinesWith("Settings shows whether your recordings get your colours.") : WelcomeLinesWith(CaptureLineFor(settings.OverlayMode, demo.DriverAvailable, settings.StreamingMode)));
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterScreen;
		ShowInTaskbar = true;
		Text = "Welcome to PlexusX";
		ClientSize = new Size(S(680), S(520));
		BackColor = Theme.Background;
		DoubleBuffered = true;
		KeyPreview = true;
		Icon = AppIcon.Value;
		Font = Fonts.Body;
		_contentW = ClientSize.Width - 2 * S(24);
		_rightX = S(24) + S(212) + S(24);
		_footerRuleY = ClientSize.Height - S(64);
		_buttonTop = _footerRuleY + (S(64) - S(32)) / 2 + 1;
		_stepLine = LineBox("Step 1 of 2", StepFont, _contentW, S(16));
		_titleLine = Math.Max(LineBox("Which game do you play most?", TitleFont, _contentW, S(36)), LineBox("What do you want it to fix?", TitleFont, _contentW, S(36)));
		_subLine = Math.Max(LineBox("We'll start you on its presets. You can change it any time.", SubFont, _contentW, S(20)), LineBox("Pick one - your screen changes to match.", SubFont, _contentW, S(20)));
		int num = S(56) + _stepLine + S(8);
		_titleTop = num - S(2);
		_subTop = num + _titleLine + S(4) - S(1);
		_gridTop = _subTop + S(1) + _subLine + S(20);
		int count = SetupAnswers.Games.Count;
		int val = _gridTop + count * S(40) + (count - 1) * S(8) + S(8) + S(64);
		ClientSize = new Size(S(680), Math.Max(S(520), val));
		_footerRuleY = ClientSize.Height - S(64);
		_buttonTop = _footerRuleY + (S(64) - S(32)) / 2 + 1;
		int num2 = _gridTop + S(216) + S(8) + FlowHeight("A sample scene. Pick a game to see its look on your screen.", CaptionFont, S(396)) + S(10) + FlowHeight("Changes what your monitor shows, never touches the game.", SubFont, S(396));
		_splitH = S(216) - Math.Max(0, num2 - (_footerRuleY - S(2)));
		VibranceHud.Controls.CaptionButton captionButton = new VibranceHud.Controls.CaptionButton(VibranceHud.Controls.CaptionButton.Kind.Close)
		{
			Location = new Point(ClientSize.Width - S(46), 0),
			Size = new Size(S(46), S(40)),
			AccessibleName = "Close setup"
		};
		captionButton.Click += (object? s, EventArgs e) =>
		{
			SkipForNow();
		};
		Controls.Add(captionButton);
		IReadOnlyList<string> games = SetupAnswers.Games;
		int num3 = _gridTop;
		foreach (string name in games)
		{
			GameRow row = new GameRow
			{
				Text = name,
				Location = new Point(S(24), num3),
				Size = new Size(S(212), S(40)),
				Visible = false
			};
			row.Click += (object? s, EventArgs e) =>
			{
				_favoriteGame = name;
				_answers.Game = name;
				foreach (GameRow gameRow in _gameRows)
				{
					gameRow.Active = gameRow == row;
				}
				ShowOnTheirScreen(DemoGrades.ForGame(name));
				_demoLine = MachineLine();
				Invalidate(invalidateChildren: true);
			};
			_gameRows.Add(row);
			Controls.Add(row);
			num3 += S(40) + S(8);
		}
		(string, Complaint)[] array = new (string, Complaint)[4]
		{
			("Everything looks grey and flat", Complaint.WashedOut),
			("I lose people in dark corners", Complaint.TooDark),
			("My recordings look flatter than my screen", Complaint.FlatRecordings),
			("My eyes ache after a long session", Complaint.EyeStrain)
		};
		for (int num4 = 0; num4 < array.Length; num4++)
		{
			(string, Complaint) tuple = array[num4];
			string item = tuple.Item1;
			Complaint value = tuple.Item2;
			GameRow chip = new GameRow
			{
				Text = item,
				Size = new Size(_contentW, S(40)),
				Location = new Point(S(24), _gridTop + num4 * (S(40) + S(8))),
				Visible = false
			};
			chip.Click += (object? s, EventArgs e) =>
			{
				_answers.Complaint = value;
				foreach (GameRow complaintChip in _complaintChips)
				{
					complaintChip.Active = complaintChip == chip;
				}
				ShowOnTheirScreen(DemoGrades.ForComplaint(value));
				_demoLine = DemoGrades.AnswerLine(value, DriverState());
				Invalidate(invalidateChildren: true);
			};
			_complaintChips.Add(chip);
			Controls.Add(chip);
		}
		_primary = new GlassButton
		{
			Text = "Continue",
			Kind = GlassButtonKind.Primary
		};
		_primary.Click += (object? s, EventArgs e) =>
		{
			Advance();
		};
		Controls.Add(_primary);
		int num5 = S(8);
		int width = TextRenderer.MeasureText("Skip for now", Fonts.BodyBold, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + 2 * num5;
		int width2 = TextRenderer.MeasureText("Back", Fonts.BodyBold, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + 2 * num5;
		_skip = new GlassButton
		{
			Text = "Skip for now",
			Kind = GlassButtonKind.Quiet,
			Location = new Point(S(24) - num5, _buttonTop),
			Size = new Size(width, S(32))
		};
		_skip.Click += (object? s, EventArgs e) =>
		{
			SkipForNow();
		};
		Controls.Add(_skip);
		_back = new GlassButton
		{
			Text = "Back",
			AccessibleName = "Back",
			Kind = GlassButtonKind.Quiet,
			Location = new Point(_skip.Right + S(8), _buttonTop),
			Size = new Size(width2, S(32)),
			Visible = false
		};
		_back.Click += (object? s, EventArgs e) =>
		{
			ShowStep(_step - 1);
		};
		Controls.Add(_back);
		int tabIndex = 0;
		foreach (GameRow gameRow2 in _gameRows)
		{
			gameRow2.TabIndex = tabIndex++;
		}
		foreach (GameRow complaintChip2 in _complaintChips)
		{
			complaintChip2.TabIndex = tabIndex++;
		}
		_skip.TabIndex = tabIndex++;
		_back.TabIndex = tabIndex++;
		_primary.TabIndex = tabIndex;
		WindowDrag.Enable(this, this);
		WizardChrome.Shape(this);
		_timer = new Timer
		{
			Interval = 33
		};
		_timer.Tick += OnTick;
		_timer.Start();
		ShowStep(0);
		ActiveControl = _primary;
	}

	private static int FlowHeight(string text, Font font, int width)
	{
		return TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
	}

	private static int LineBox(string text, Font font, int width, int designLine)
	{
		int num = FlowHeight(text, font, width);
		int num2 = Math.Max(1, (int)Math.Round((double)num / (double)Math.Max(1, font.Height)));
		if (num2 != 1)
		{
			return num2 * font.Height;
		}
		return designLine;
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == Keys.Escape)
		{
			SkipForNow();
			return true;
		}
		bool flag = keyData == Keys.Return;
		if (flag)
		{
			Control activeControl = ActiveControl;
			bool flag2 = ((activeControl is GlassButton || activeControl is ChipButton || activeControl is GameRow || activeControl is Button) ? true : false);
			flag = !flag2;
		}
		if (flag && _primary.Enabled)
		{
			Advance();
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	private void SkipForNow()
	{
		_settings.OnboardingComplete = true;
		_store.Save(_settings);
		DialogResult = DialogResult.OK;
		Close();
	}

	private void OnTick(object? sender, EventArgs e)
	{
		DateTime utcNow = DateTime.UtcNow;
		double num = Math.Min((utcNow - _last).TotalSeconds, 0.1);
		_last = utcNow;
		if (_reveal < 1f)
		{
			_reveal = Math.Min(1f, _reveal + (float)(num / 1.1));
		}
		Invalidate(invalidateChildren: true);
	}

	private void Advance()
	{
		if (_step == 2)
		{
			_answers.ApplyTo(_settings);
		}
		if (_step == 0 && !ShouldDemonstrate)
		{
			Finish();
		}
		else if (_step < 2)
		{
			ShowStep(_step + 1);
		}
		else
		{
			Finish();
		}
	}

	internal void ShowStepForTest(int step)
	{
		ShowStep(step);
	}

	private void ShowStep(int step)
	{
		_step = Math.Max(0, step);
		bool visible = _step == 1;
		bool visible2 = _step == 2;
		foreach (GameRow gameRow in _gameRows)
		{
			gameRow.Visible = visible;
		}
		foreach (GameRow complaintChip in _complaintChips)
		{
			complaintChip.Visible = visible2;
		}
		_back.Visible = _step > 0;
		GlassButton primary = _primary;
		primary.Text = _step switch
		{
			0 => "Get started", 
			2 => "See what it costs", 
			_ => "Continue", 
		};
		int num = TextRenderer.MeasureText(_primary.Text, Fonts.BodyBold).Width + 2 * S(12);
		_primary.Size = new Size(num, S(32));
		_primary.Location = new Point(ClientSize.Width - S(24) - num, _buttonTop);
		_skip.Visible = true;
		Invalidate(invalidateChildren: true);
	}

	private void ShowOnTheirScreen(DemoGrades.Grade grade)
	{
		if (_demo == null)
		{
			return;
		}
		try
		{
			grade.ApplyTo(_demo);
		}
		catch (Exception ex)
		{
			try
			{
				CrashLog.Write(ex);
			}
			catch
			{
			}
		}
	}

	private VibranceDriverState DriverState()
	{
		return _demo?.DriverState ?? VibranceDriverState.NoNvidiaCard;
	}

	private string MachineLine()
	{
		int value = _demo?.Saturation ?? DemoGrades.Opening.Saturation;
		return DriverState() switch
		{
			VibranceDriverState.Available => $"Vibrance in the NVIDIA panel stops at {100}. PlexusX takes vibrance to {350} and saturation to {300} - your screen is at {value} now.", 
			VibranceDriverState.DisplayNotOnNvidia => "Your NVIDIA card isn't driving this screen, so PlexusX does it instead.", 
			_ => "Your graphics card has no vibrance control, so PlexusX does it itself - your " + $"screen is at {value} now, and saturation goes to {300}.", 
		};
	}

	private void Finish()
	{
		_answers.Game = _favoriteGame;
		_answers.ApplyTo(_settings);
		_settings.OnboardingComplete = true;
		_store.Save(_settings);
		DialogResult = DialogResult.OK;
		Close();
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
		DrawHeader(graphics);
		if (_step == 0)
		{
			PaintWelcome(graphics);
		}
		else if (_step == 1)
		{
			PaintGameQuestion(graphics);
		}
		else if (_step == 2)
		{
			PaintComplaintQuestion(graphics);
		}
		PixelOffsetMode pixelOffsetMode = graphics.PixelOffsetMode;
		graphics.PixelOffsetMode = PixelOffsetMode.Half;
		using (SolidBrush brush2 = new SolidBrush(Theme.Border))
		{
			graphics.FillRectangle(brush2, 0, _footerRuleY, ClientSize.Width, 1);
		}
		graphics.PixelOffsetMode = pixelOffsetMode;
		if (_reveal < 1f)
		{
			using SolidBrush brush3 = new SolidBrush(Color.FromArgb((int)(255f * (1f - _reveal)), Theme.Background));
			graphics.FillRectangle(brush3, ClientRectangle);
		}
		WizardChrome.PaintRim(graphics, this);
		base.OnPaint(e);
	}

	private void DrawHeader(Graphics g)
	{
		int num = S(40);
		int num2 = S(20);
		int num3 = S(19);
		Rectangle r = new Rectangle(S(16) + (num2 - num3) / 2, (num - num3) / 2, num3, num3);
		using (SolidBrush brush = new SolidBrush(Theme.Accent))
		{
			g.FillPolygon(brush, StarPoints(r));
		}
		int num4 = S(16) + num2 + S(4);
		TextRenderer.DrawText(g, "PlexusX", BrandFont, new Rectangle(num4, SignalInk.TopForBaseline(g, BrandFont, 0, num) - S(1), ClientSize.Width - num4, BrandFont.Height), Theme.Text, TextFormatFlags.NoPadding);
	}

	private void PaintWelcome(Graphics g)
	{
		string[] welcomeLines = _welcomeLines;
		int y = _titleTop;
		Band(0, y, FlowHeight(welcomeLines[0], TitleFont, _contentW));
		DrawFlow(g, welcomeLines[0], TitleFont, Theme.Text, S(24), ref y, _contentW, S(4));
		Band(1, y, FlowHeight(welcomeLines[1], SubFont, _contentW));
		DrawFlow(g, welcomeLines[1], SubFont, Theme.TextDim, S(24), ref y, _contentW, 0);
		Band(2, y, FlowHeight(welcomeLines[2], SubFont, _contentW));
		DrawFlow(g, welcomeLines[2], SubFont, Theme.TextDim, S(24), ref y, _contentW, S(20));
		Band(3, y, FlowHeight(welcomeLines[3], SubFont, _contentW));
		DrawFlow(g, welcomeLines[3], SubFont, Theme.TextDim, S(24), ref y, _contentW, 0);
		void Band(int i, int top, int height)
		{
			_welcomeBands[i] = new Rectangle(S(24), top, _contentW, height);
		}
	}

	private void PaintGameQuestion(Graphics g)
	{
		DrawStepCaption(g, 1);
		DrawHeadings(g, "Which game do you play most?", "We'll start you on its presets. You can change it any time.");
		Rectangle box = new Rectangle(_rightX, _gridTop, S(396), _splitH);
		DrawSplitPreview(g, box);
		int y = box.Bottom + S(8);
		DrawFlow(g, "A sample scene. Pick a game to see its look on your screen.", CaptionFont, SignalInk.Faint, box.X, ref y, box.Width, S(10));
		DrawFlow(g, "Changes what your monitor shows, never touches the game.", SubFont, Theme.Text, box.X, ref y, box.Width, 0);
	}

	private void PaintComplaintQuestion(Graphics g)
	{
		DrawStepCaption(g, 2);
		DrawHeadings(g, "What do you want it to fix?", "Pick one - your screen changes to match.");
	}

	private void DrawHeadings(Graphics g, string title, string subtitle)
	{
		TextRenderer.DrawText(g, title, TitleFont, new Rectangle(S(24), _titleTop, _contentW, _titleLine), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
		TextRenderer.DrawText(g, subtitle, SubFont, new Rectangle(S(24), _subTop, _contentW, _subLine), Theme.TextDim, TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
	}

	private void DrawStepCaption(Graphics g, int oneBasedStep)
	{
		string text = $"Step {oneBasedStep} of {2}";
		int num = S(56);
		TextRenderer.DrawText(g, text, StepFont, new Rectangle(S(24), num, _contentW, _stepLine), SignalInk.Faint, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		int width = TextRenderer.MeasureText(g, text, StepFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
		int num2 = S(20);
		int num3 = S(2);
		int num4 = S(24) + width + S(10);
		int y = num + (_stepLine - num3) / 2;
		PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		for (int i = 0; i < 2; i++)
		{
			using SolidBrush brush = new SolidBrush((i < oneBasedStep) ? Theme.Accent : Theme.AccentDim);
			g.FillRectangle(brush, num4, y, num2, num3);
			num4 += num2 + S(4);
		}
		g.PixelOffsetMode = pixelOffsetMode;
	}

	private void DrawSplitPreview(Graphics g, Rectangle box)
	{
		int num = S(8);
		using (SolidBrush brush = new SolidBrush(SignalInk.PreviewWell))
		{
			using GraphicsPath path = Glass.RoundedPath(box, num);
			g.FillPath(brush, path);
		}
		using Region clip = g.Clip;
		using GraphicsPath path2 = Glass.RoundedPath(box, num);
		using (Region clip2 = new Region(path2))
		{
			g.Clip = clip2;
			DrawScene(g, new Rectangle(box.X, box.Y, box.Width / 2, box.Height), vivid: false);
			DrawScene(g, new Rectangle(box.X + box.Width / 2, box.Y, box.Width - box.Width / 2, box.Height), vivid: true);
			int num2 = S(2);
			PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			using (SolidBrush brush2 = new SolidBrush(Theme.Accent))
			{
				g.FillRectangle(brush2, box.X + box.Width / 2 - num2 / 2, box.Y, num2, box.Height);
			}
			g.PixelOffsetMode = pixelOffsetMode;
		}
		g.Clip = clip;
		int num3 = S(36);
		Rectangle rect = new Rectangle(box.X + box.Width / 2 - num3 / 2, box.Y + box.Height / 2 - num3 / 2, num3, num3);
		using (SolidBrush brush3 = new SolidBrush(Theme.Background))
		{
			g.FillEllipse(brush3, rect);
		}
		using (Pen pen = new Pen(Theme.Accent, 1f))
		{
			g.DrawEllipse(pen, rect);
		}
		int num4 = S(20);
		using (SolidBrush brush4 = new SolidBrush(Theme.Accent))
		{
			g.FillPolygon(brush4, StarPoints(new Rectangle(rect.X + (num3 - num4) / 2, rect.Y + (num3 - num4) / 2, num4, num4)));
		}
		DrawCornerChip(g, "100% ", "as recorded", new Point(box.X + S(8), box.Y + S(8)), left: true);
		DrawCornerChip(g, "230% ", "simulated", new Point(box.Right - S(8), box.Y + S(8)), left: false);
	}

	private static void DrawScene(Graphics g, Rectangle r, bool vivid)
	{
		Rectangle rectangle = new Rectangle(r.X, r.Y, r.Width, (int)((float)r.Height * 0.62f));
		using (LinearGradientBrush brush = new LinearGradientBrush((rectangle.Height > 0) ? rectangle : new Rectangle(r.X, r.Y, Math.Max(1, r.Width), 1), Grade(Color.FromArgb(255, 108, 168, 224)), Grade(Color.FromArgb(255, 186, 219, 246)), LinearGradientMode.Vertical))
		{
			g.FillRectangle(brush, rectangle);
		}
		using (SolidBrush brush2 = new SolidBrush(Grade(Color.FromArgb(255, 244, 248, 252))))
		{
			g.FillEllipse(brush2, (float)r.X + (float)r.Width * 0.15f, (float)r.Y + (float)r.Height * 0.16f, (float)r.Width * 0.34f, (float)r.Height * 0.14f);
		}
		Rectangle rect = new Rectangle(r.X, rectangle.Bottom, r.Width, Math.Max(0, r.Bottom - rectangle.Bottom));
		using (SolidBrush brush3 = new SolidBrush(Grade(Color.FromArgb(255, 96, 148, 74))))
		{
			g.FillRectangle(brush3, rect);
		}
		using (SolidBrush brush4 = new SolidBrush(Grade(Color.FromArgb(255, 58, 92, 54))))
		{
			for (int i = 0; i < 5; i++)
			{
				float num = (float)r.X + (float)r.Width * (0.05f + (float)i * 0.11f);
				float num2 = (float)rectangle.Bottom - (float)r.Height * 0.05f;
				g.FillPolygon(brush4, new PointF[3]
				{
					new PointF(num, num2 - (float)r.Height * 0.12f),
					new PointF(num - (float)r.Width * 0.035f, num2),
					new PointF(num + (float)r.Width * 0.035f, num2)
				});
			}
		}
		using (SolidBrush brush5 = new SolidBrush(Grade(Color.FromArgb(255, 128, 126, 118))))
		{
			g.FillPolygon(brush5, new PointF[4]
			{
				new PointF((float)r.X + (float)r.Width * 0.55f, r.Bottom),
				new PointF((float)r.X + (float)r.Width * 0.68f, (float)r.Bottom - (float)r.Height * 0.22f),
				new PointF((float)r.X + (float)r.Width * 0.86f, (float)r.Bottom - (float)r.Height * 0.08f),
				new PointF((float)r.X + (float)r.Width * 0.94f, r.Bottom)
			});
		}
		Color Grade(Color c)
		{
			if (!vivid)
			{
				return c;
			}
			float num3 = (float)(c.R + c.G + c.B) / 3f;
			int red = (int)Math.Clamp(num3 + ((float)(int)c.R - num3) * 1.55f, 0f, 255f);
			int green = (int)Math.Clamp(num3 + ((float)(int)c.G - num3) * 1.55f, 0f, 255f);
			int blue = (int)Math.Clamp(num3 + ((float)(int)c.B - num3) * 1.55f, 0f, 255f);
			return Color.FromArgb(red, green, blue);
		}
	}

	private void DrawCornerChip(Graphics g, string number, string rest, Point corner, bool left)
	{
		Font captionBold = Fonts.CaptionBold;
		Font caption = Fonts.Caption;
		TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
		int width = TextRenderer.MeasureText(g, number, captionBold, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
		int width2 = TextRenderer.MeasureText(g, rest, caption, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
		int num = S(8);
		int height = Math.Max(S(24), captionBold.Height);
		int num2 = width + width2 + num * 2;
		Rectangle rectangle = (left ? new Rectangle(corner.X, corner.Y, num2, height) : new Rectangle(corner.X - num2, corner.Y, num2, height));
		using (SolidBrush brush = new SolidBrush(Theme.Background))
		{
			using GraphicsPath path = Glass.RoundedPath(rectangle, S(4));
			g.FillPath(brush, path);
		}
		TextRenderer.DrawText(g, number, captionBold, new Rectangle(rectangle.X + num, rectangle.Y, width, height), Theme.Text, flags);
		TextRenderer.DrawText(g, rest, caption, new Rectangle(rectangle.X + num + width, rectangle.Y, width2, height), SignalInk.Faint, flags);
	}

	private void DrawDemoLine(Graphics g, int top)
	{
		if (_demoLine.Length != 0)
		{
			TextRenderer.DrawText(bounds: new Rectangle(S(24), top, _contentW, S(60)), dc: g, text: _demoLine, font: SubFont, foreColor: Theme.Accent, flags: TextFormatFlags.WordBreak);
		}
	}

	private void DrawFlow(Graphics g, string text, Font font, Color color, int x, ref int y, int width, int gapAfter)
	{
		int num = FlowHeight(text, font, width);
		TextRenderer.DrawText(g, text, font, new Rectangle(x, y, width, num), color, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
		y += num + gapAfter;
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
