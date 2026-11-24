using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using A;
using VibranceHud.Controls;
using VibranceHud.DarkBoost;
using VibranceHud.Design;
using VibranceHud.Games;
using VibranceHud.Licensing;
using VibranceHud.Zoom;

namespace VibranceHud.Pages;

public sealed class DarkBoostPage : GlowPage
{
	private sealed class StateLabel : Control
	{
		public StateLabel()
		{
			TabStop = false;
			SetStyle(ControlStyles.Selectable, value: false);
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Size = new Size(S(60), S(20));
			Text = "Ready";
		}

		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			Remeasure();
		}

		internal void Remeasure()
		{
			Size = new Size(TextRenderer.MeasureText(Text, Fonts.Body, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + 2, S(20));
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			TextRenderer.DrawText(e.Graphics, Text, Fonts.Body, ClientRectangle, Theme.TextDim, TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
	}

	private sealed class WarningIcon : Control
	{
		public WarningIcon()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			TabStop = false;
			AccessibleRole = AccessibleRole.Graphic;
			AccessibleName = "Warning";
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			SignalIcons.Draw(e.Graphics, SignalIcon.Warning, new RectangleF(0f, 0f, Width, Height), Theme.Negative);
		}
	}

	private sealed class AddOnTag : Control
	{
		private readonly bool _lock;

		internal Size NaturalSize
		{
			get
			{
				int width = TextRenderer.MeasureText(Text, Fonts.CaptionBold, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
				return new Size(S(8) + (_lock ? (S(12) + S(4)) : 0) + width + S(8) + 2, S(20));
			}
		}

		public AddOnTag(bool withLock)
		{
			TabStop = false;
			SetStyle(ControlStyles.Selectable, value: false);
			_lock = withLock;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Text = "Add-on";
			AccessibleName = (withLock ? "Add-on, locked" : "Add-on");
			AccessibleRole = AccessibleRole.StaticText;
			Size = NaturalSize;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			int dpi = ControlInk.Dpi(this);
			ControlInk.PaintBox(graphics, Size, ControlInk.PxF(dpi, 4f), ControlInk.Hairline(dpi), Color.Transparent, ControlInk.Edge);
			int num = S(8);
			if (_lock)
			{
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				SignalIcons.Draw(graphics, SignalIcon.Lock, new RectangleF(num, (float)(Height - S(12)) / 2f, S(12), S(12)), Theme.TextDim);
				num += S(12) + S(4);
			}
			TextRenderer.DrawText(graphics, Text, Fonts.CaptionBold, new Rectangle(num, 0, Width - num, Height), Theme.TextDim, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
	}

	private sealed class FactLine : Control
	{
		private readonly string _bold;

		private readonly string _rest;

		public FactLine(string bold, string rest)
		{
			TabStop = false;
			SetStyle(ControlStyles.Selectable, value: false);
			_bold = bold;
			_rest = rest;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Text = bold + rest;
			AccessibleRole = AccessibleRole.StaticText;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			float num = S(6);
			using (SolidBrush brush = new SolidBrush(Theme.TextFaint))
			{
				graphics.FillEllipse(brush, 0f, ((float)Height - num) / 2f, num, num);
			}
			int num2 = S(16);
			TextRenderer.DrawText(graphics, _bold, Fonts.BodyBold, new Rectangle(num2, 0, Width - num2, Height), Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			num2 += TextRenderer.MeasureText(graphics, _bold, Fonts.BodyBold, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding).Width;
			TextRenderer.DrawText(graphics, _rest, Fonts.Body, new Rectangle(num2, 0, Math.Max(0, Width - num2), Height), Theme.TextDim, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
	}

	private sealed class TintTile : Control
	{
		private sealed class TintAccessibility : ControlAccessibleObject
		{
			private readonly TintTile _tile;

			public override AccessibleStates State => (AccessibleStates)((int)base.State | (_tile.Selected ? 16 : 0));

			internal TintAccessibility(TintTile tile)
				: base(tile)
			{
				_tile = tile;
			}
		}

		private bool _selected;

		private bool _hover;

		private const double CleanHi = 0.8;

		private const double CleanLo = 4.0 / 15.0;

		private static readonly double[] CleanRed = new double[3]
		{
			0.8,
			4.0 / 15.0,
			4.0 / 15.0
		};

		private static readonly double[] CleanGreen = new double[3]
		{
			4.0 / 15.0,
			0.8,
			4.0 / 15.0
		};

		private static readonly double[] CleanBlue = new double[3]
		{
			4.0 / 15.0,
			4.0 / 15.0,
			0.8
		};

		internal DarkBoostTint Tint { get; }

		internal bool Selected
		{
			get
			{
				return _selected;
			}
			set
			{
				if (_selected != value)
				{
					_selected = value;
					AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
					Invalidate();
				}
			}
		}

		public TintTile(DarkBoostTint tint)
		{
			Tint = tint;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Text = DarkBoostTints.Name(tint);
			AccessibleName = Text;
			AccessibleRole = AccessibleRole.RadioButton;
			TabStop = true;
			Cursor = Cursors.Hand;
		}

		protected override AccessibleObject CreateAccessibilityInstance()
		{
			return new TintAccessibility(this);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			Keys keyCode = e.KeyCode;
			if ((keyCode == Keys.Return || keyCode == Keys.Space) ? true : false)
			{
				OnClick(EventArgs.Empty);
				e.Handled = true;
			}
			base.OnKeyDown(e);
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			Invalidate();
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
			_hover = true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_hover = false;
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			int dpi = ControlInk.Dpi(this);
			int line = ControlInk.Hairline(dpi);
			float radius = ControlInk.PxF(dpi, 4f);
			if (_selected)
			{
				ControlInk.PaintBox(graphics, Size, radius, line, Enabled ? ControlInk.Accent : ControlInk.Surface2, Color.Transparent);
			}
			else
			{
				ControlInk.PaintBox(graphics, Size, radius, line, (Enabled && _hover) ? ControlInk.Hover : Color.Transparent, Enabled ? ControlInk.Edge : ControlInk.TextDisabled);
			}
			RectangleF rect = new RectangleF((float)(Width - S(28)) / 2f, S(12), S(28), S(6));
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = Glass.RoundedPath(rect, rect.Height / 2f))
			{
				double[] array = DarkBoostTints.PreviewWeights(Tint);
				if (array != null)
				{
					using SolidBrush brush = new SolidBrush(Swatch(array));
					graphics.FillPath(brush, path);
				}
				else
				{
					using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(rect, Theme.TextDim, Theme.TextDim, LinearGradientMode.Horizontal);
					linearGradientBrush.InterpolationColors = new ColorBlend
					{
						Colors = new Color[3]
						{
							Swatch(CleanRed),
							Swatch(CleanGreen),
							Swatch(CleanBlue)
						},
						Positions = new float[3] { 0f, 0.5f, 1f }
					};
					graphics.FillPath(linearGradientBrush, path);
				}
			}
			Color foreColor;
			if (Enabled)
			{
				foreColor = (_selected ? ControlInk.OnAccent : Theme.Text);
			}
			else
			{
				foreColor = ControlInk.TextDisabledInk;
			}
			TextRenderer.DrawText(graphics, Text, Fonts.BodyBold, new Rectangle(0, S(24), Width, S(20)), foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			if (Focused && ShowFocusCues)
			{
				ControlInk.PaintBox(graphics, Size, radius, ControlInk.Px(dpi, 2), Color.Transparent, _selected ? ControlInk.OnAccent : ControlInk.Focus);
			}
		}

		private static Color Swatch(double[] w)
		{
			return Color.FromArgb(255, (int)Math.Round(w[0] * 255.0), (int)Math.Round(w[1] * 255.0), (int)Math.Round(w[2] * 255.0));
		}
	}

	private sealed class ScenePreview : Control
	{
		private Bitmap? _picture;

		internal Bitmap? Picture
		{
			get
			{
				return _picture;
			}
			set
			{
				Bitmap? picture = _picture;
				_picture = value;
				picture?.Dispose();
				Invalidate();
			}
		}

		public ScenePreview()
		{
			TabStop = false;
			SetStyle(ControlStyles.Selectable, value: false);
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			AccessibleRole = AccessibleRole.Graphic;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			int num = ControlInk.Hairline(ControlInk.Dpi(this));
			if (_picture != null)
			{
				graphics.InterpolationMode = InterpolationMode.Bilinear;
				graphics.PixelOffsetMode = PixelOffsetMode.Half;
				using ImageAttributes imageAttributes = new ImageAttributes();
				imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
				graphics.DrawImage(_picture, new Rectangle(num, num, Width - 2 * num, Height - 2 * num), 0, 0, _picture.Width, _picture.Height, GraphicsUnit.Pixel, imageAttributes);
				graphics.PixelOffsetMode = PixelOffsetMode.Default;
			}
			using Pen pen = new Pen(ControlInk.Border, num)
			{
				Alignment = PenAlignment.Inset
			};
			graphics.DrawRectangle(pen, 0, 0, Width - num, Height - num);
		}
	}

	private const int Pad = 21;

	private const int CardGap = 16;

	private const int ViewGap = 16;

	private const int ViewHeight = 182;

	private const int StatusHeight = 84;

	private const int ColumnGap = 32;

	private const int TileHeight = 56;

	private const int TileGap = 8;

	private const int RowHeight = 56;

	private readonly VibranceEngine _engine;

	private readonly AppSettings _settings;

	private readonly Action _save;

	private readonly List<DarkBoostSetup> _setups;

	private int _selected;

	private readonly Label _title;

	private readonly Label _subtitle;

	private readonly StateLabel _state;

	private readonly AddOnTag _headerTag;

	private readonly CardPanel _weekCard;

	private readonly CardPanel _banCard;

	private readonly Label _banTitle;

	private readonly Label _banBody;

	private readonly Label _weekTitle;

	private readonly Label _weekCaption;

	private readonly GlassButton _manage;

	private readonly GlassButton _buy;

	private readonly CardPanel _viewsCard;

	private readonly ScenePreview _now;

	private readonly ScenePreview _with;

	private readonly Label _nowCaption;

	private readonly Label _nowState;

	private readonly Label _withCaption;

	private readonly Label _withState;

	private readonly Label _tintFact;

	private readonly CardPanel _lockCard;

	private readonly Label _lockTitle;

	private readonly Label _lockCaption;

	private readonly Label _lockFootnote;

	private readonly FactLine[] _facts;

	private readonly CardPanel _settingsCard;

	private CardPanel? _zoomCard;

	private ZoomSection? _zoomSection;

	private readonly Label _settingsTitle;

	private readonly AddOnTag _settingsTag;

	private readonly Label _switchTitle;

	private readonly Label _switchCaption;

	private readonly Label _colourTitle;

	private readonly ToggleSwitch _switch;

	private readonly TintTile[] _tiles;

	private readonly Label _strengthLabel;

	private readonly Label _strengthValue;

	private readonly Label _whiteLabel;

	private readonly Label _whiteValue;

	private readonly Label _blackLabel;

	private readonly Label _blackValue;

	private readonly Label _blackReadout;

	private readonly FlatSlider _strength;

	private readonly FlatSlider _black;

	private readonly Label _keyTitle;

	private readonly Label _keyCaption;

	private readonly Label _keyError;

	private readonly SegmentStrip _mode;

	private readonly HotkeyPicker _key;

	private readonly Label _gamesTitle;

	private readonly Label _gamesCaption;

	private readonly ToggleSwitch _games;

	private readonly Label _fadeTitle;

	private readonly Label _fadeCaption;

	private readonly ToggleSwitch _fade;

	private readonly Label _setupsTitle;

	private readonly Label _setupsCaption;

	private readonly GlassButton _saveNew;

	private readonly GlassButton _delete;

	private readonly List<ChipButton> _setupChips = new List<ChipButton>();

	private readonly Label _nextTitle;

	private readonly Label _nextCaption;

	private readonly HotkeyPicker _nextKey;

	private bool _owned;

	private bool _syncing;

	private int? _weekDaysLeft;

	private double _fadeFrom;

	private double _fadeTo;

	private double _nowAmount;

	private DateTime _fadeStart;

	private readonly Timer _fadeTimer = new Timer
	{
		Interval = 15
	};

	internal const string KeyRefused = "That key can't be used for Dark Boost.";

	public static readonly string GamesOnlyCaption = "Works while " + string.Join(", ", from g in SupportedGames.All.Take(SupportedGames.All.Count - 1)
		select g.DisplayName) + " or " + SupportedGames.All.Last().DisplayName + " runs. Turn this off for any other game.";

	public const string GamesOnlyShortCaption = "Works while one of the six supported games runs. Turn this off for any other game.";

	internal const string OnYourAccount = "Dark Boost & Zoom is on your account.";

	internal const string SeparateFromYourPlan = "Separate from your plan.";

	internal const string InviteLine = "Get a free week of Dark Boost & Zoom when 3 new players sign up with your code and open PlexusX on their own PC this month.";

	internal const string FriendPromptLine = "New here? Got a code from a friend?";

	internal const string StartWeekLabel = "Start my free week";

	internal const string BuyLabel = "Get Dark Boost & Zoom";

	internal const string FreeWeekCaption = "First week free, once per account. Card needed to start it.";

	private GlassTextBox? _friendCode;

	private bool _hasWeekRows;

	internal ZoomSection? ZoomSectionForTest => _zoomSection;

	internal Control? ZoomCardForTest => _zoomCard;

	protected override bool LaysOutInLogicalPixels => false;

	internal bool Owned => _owned;

	internal int? WeekDaysLeft
	{
		get
		{
			return _weekDaysLeft;
		}
		set
		{
			_weekDaysLeft = value;
			ApplyStatus();
			LayoutContent();
		}
	}

	private bool OnTheWeek
	{
		get
		{
			int? weekDaysLeft = _weekDaysLeft;
			if (weekDaysLeft.HasValue)
			{
				return weekDaysLeft.GetValueOrDefault() > 0;
			}
			return false;
		}
	}

	internal Func<IWin32Window?, bool>? AskBanRiskForTest { get; set; }

	internal Control BanCardForTest => _banCard;

	internal Label BanTitleForTest => _banTitle;

	internal Label BanBodyForTest => _banBody;

	internal bool FadeRunningForTest => _fadeTimer.Enabled;

	private DarkBoostSetup Current => _setups[DarkBoostSetups.Selected(_selected, _setups.Count)];

	internal IReadOnlyList<DarkBoostSetup> Setups => _setups;

	internal int SelectedSetup => _selected;

	internal IReadOnlyList<ChipButton> SetupChipsForTest => _setupChips;

	internal string BuyUrl => "https://plexusx.lol/#pricing";

	internal GlassButton BuyForTest => _buy;

	internal GlassTextBox FriendCodeForTest => _friendCode;

	internal ToggleSwitch SwitchForTest => _switch;

	internal ToggleSwitch FadeForTest => _fade;

	internal ToggleSwitch GamesOnlyForTest => _games;

	internal FlatSlider StrengthForTest => _strength;

	internal FlatSlider BlackForTest => _black;

	internal IReadOnlyList<Control> TintTilesForTest => _tiles;

	internal HotkeyPicker KeyPickerForTest => _key;

	internal HotkeyPicker NextKeyPickerForTest => _nextKey;

	internal SegmentStrip ModeForTest => _mode;

	internal GlassButton SaveNewForTest => _saveNew;

	internal GlassButton DeleteForTest => _delete;

	internal Control LockCardForTest => _lockCard;

	internal Control WeekCardForTest => _weekCard;

	internal GlassButton ManageForTest => _manage;

	internal Control SettingsCardForTest => _settingsCard;

	internal Label KeyErrorForTest => _keyError;

	internal string StateTextForTest => _state.Text;

	internal string WhiteValueTextForTest => _whiteValue.Text;

	internal Bitmap? WithPictureForTest => _with.Picture;

	internal bool StateShownForTest => _state.Visible;

	public event Func<uint, uint, bool>? KeyChanged;

	public event Action? NextKeyChanged;

	public event Action? ManageRequested;

	public DarkBoostPage(VibranceEngine engine, AppSettings settings, Action save)
	{
		_engine = engine ?? throw new ArgumentNullException("engine");
		_settings = settings ?? throw new ArgumentNullException("settings");
		_save = save ?? ((Action)(() =>
		{
		}));
		_setups = DarkBoostSetups.Normalise(settings.DarkBoostSetups);
		_selected = DarkBoostSetups.Selected(settings.DarkBoostSelected, _setups.Count);
		_title = new Label
		{
			Text = "Dark Boost & Zoom",
			Font = Fonts.Display,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft,
			UseMnemonic = false,
			Name = "PageTitle"
		};
		_subtitle = new Label
		{
			Text = "Brings out detail in very dark scenes. Hold a key to zoom in.",
			Font = Fonts.Label,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.TopLeft,
			UseMnemonic = false,
			Name = "PageSubtitle"
		};
		_state = new StateLabel();
		_headerTag = new AddOnTag(withLock: false);
		_banCard = new CardPanel
		{
			Name = "BanWarningCard",
			AccessibleName = "Dark Boost can get you banned"
		};
		_banCard.Paint += (object? _, PaintEventArgs e2) =>
		{
			PaintBanFrame(e2.Graphics, _banCard.ClientSize);
		};
		_banCard.Controls.Add(new WarningIcon());
		_banTitle = Words(_banCard, "Dark Boost can get you banned", Fonts.Title, Theme.Negative, ContentAlignment.MiddleLeft);
		_banBody = Words(_banCard, "Some games, like Rust, ban players who see in the dark. It mostly happens when another player reports you. PlexusX never touches the game, but the ban risk is yours.", Fonts.Body, Theme.Text);
		_banTitle.Name = "BanWarningTitle";
		_banBody.Name = "BanWarningBody";
		_weekCard = new CardPanel
		{
			Visible = false
		};
		_weekTitle = Words(_weekCard, "Free week. 7 days left.", Fonts.BodyBold, Theme.Text);
		_weekCaption = Words(_weekCard, "No card. It turns off by itself.", Fonts.Caption, Theme.TextFaint);
		_manage = new GlassButton
		{
			Text = "Manage",
			Kind = GlassButtonKind.Secondary,
			AccessibleName = "Manage on the Account page"
		};
		_manage.Click += (object? _, EventArgs _) =>
		{
			ManageRequested?.Invoke();
		};
		_weekCard.Controls.Add(_manage);
		_viewsCard = new CardPanel();
		_now = new ScenePreview
		{
			AccessibleName = "Your screen now"
		};
		_with = new ScenePreview
		{
			AccessibleName = "With Dark Boost"
		};
		_viewsCard.Controls.Add(_now);
		_viewsCard.Controls.Add(_with);
		_nowCaption = Words(_viewsCard, "Now", Fonts.CaptionBold, Theme.TextDim);
		_nowState = Words(_viewsCard, "Off", Fonts.Caption, Theme.TextFaint, ContentAlignment.TopRight);
		_withCaption = Words(_viewsCard, "With Dark Boost", Fonts.CaptionBold, Theme.TextDim);
		_withState = Words(_viewsCard, "Your settings below", Fonts.Caption, Theme.TextFaint, ContentAlignment.TopRight);
		_tintFact = Words(_viewsCard, "", Fonts.Caption, Theme.TextFaint);
		_lockCard = new CardPanel();
		_lockCard.Paint += (object? _, PaintEventArgs e2) =>
		{
			if (!_hasWeekRows)
			{
				return;
			}
			using Pen pen = new Pen(ControlInk.Edge, ControlInk.Hairline(ControlInk.Dpi(_lockCard)));
			e2.Graphics.DrawLine(pen, S(21), S(225), _lockCard.Width - S(21), S(225));
		};
		_lockTitle = Words(_lockCard, "Get Dark Boost & Zoom", Fonts.Heading, Theme.Text, ContentAlignment.MiddleLeft);
		_lockCaption = Words(_lockCard, "First week free, once per account. Card needed to start it.", Fonts.Caption, Theme.TextFaint);
		_facts = new FactLine[8]
		{
			new FactLine("Strength", " from soft to full."),
			new FactLine("5 colours", " to pick from."),
			new FactLine("Black level", " keeps black spots black."),
			new FactLine("Your key", ", hold or toggle."),
			new FactLine("Saved setups", ", one key flips them."),
			new FactLine("Only in games", ", or always."),
			new FactLine("Hold to zoom", ", 1.5x to 4x."),
			new FactLine("Smooth zoom", " if you want.")
		};
		_buy = new GlassButton
		{
			Text = "Get Dark Boost & Zoom",
			Kind = GlassButtonKind.Primary,
			AccessibleName = "Get Dark Boost & Zoom"
		};
		_buy.Click += (object? _, EventArgs _) =>
		{
			LoopbackSignIn.OpenBrowser(BuyUrl);
		};
		_lockCard.Controls.Add(_buy);
		FactLine[] facts = _facts;
		foreach (FactLine value in facts)
		{
			_lockCard.Controls.Add(value);
		}
		_lockFootnote = Words(_lockCard, "One-off owners: one free week. No card. Ends by itself.", Fonts.Caption, Theme.TextFaint);
		_settingsCard = new CardPanel();
		_settingsTitle = Words(_settingsCard, "Dark Boost settings", Fonts.Heading, Theme.Text, ContentAlignment.MiddleLeft);
		_settingsTag = new AddOnTag(withLock: true);
		_settingsCard.Controls.Add(_settingsTag);
		_switchTitle = Words(_settingsCard, "Dark Boost", Fonts.BodyBold, Theme.Text);
		_switchCaption = Words(_settingsCard, "Stays on until you turn it off.", Fonts.Caption, Theme.TextFaint);
		_switch = new ToggleSwitch
		{
			Size = new Size(40, 20),
			AccessibleName = "Dark Boost"
		};
		_switch.CheckedChanged += (object? _, EventArgs _) =>
		{
			OnSwitch();
		};
		_settingsCard.Controls.Add(_switch);
		_colourTitle = Words(_settingsCard, "Colour", Fonts.BodyBold, Theme.Text);
		_tiles = DarkBoostTints.All.Select((DarkBoostTint t) =>
		{
			TintTile tintTile = new TintTile(t);
			tintTile.Click += (object? _, EventArgs _) =>
			{
				ChooseTint(t);
			};
			_settingsCard.Controls.Add(tintTile);
			return tintTile;
		}).ToArray();
		_strengthLabel = Words(_settingsCard, "Strength", Fonts.Body, Theme.TextDim);
		_strengthValue = Words(_settingsCard, "", Fonts.BodyBold, Theme.Text, ContentAlignment.TopRight);
		_strength = new FlatSlider
		{
			Minimum = 0,
			Maximum = 100,
			AccessibleName = "Strength"
		};
		_strength.ValueChanged += (object? _, EventArgs _) =>
		{
			if (!_syncing)
			{
				SetStrength(_strength.Value);
			}
		};
		_settingsCard.Controls.Add(_strength);
		_whiteLabel = Words(_settingsCard, "Turns white above", Fonts.Caption, Theme.TextFaint);
		_whiteValue = Words(_settingsCard, "", Fonts.CaptionBold, Theme.TextDim, ContentAlignment.TopRight);
		_blackLabel = Words(_settingsCard, "Black level", Fonts.Body, Theme.TextDim);
		_blackValue = Words(_settingsCard, "", Fonts.BodyBold, Theme.Text, ContentAlignment.TopRight);
		_black = new FlatSlider
		{
			Minimum = 0,
			Maximum = 100,
			AccessibleName = "Black level"
		};
		_black.ValueChanged += (object? _, EventArgs _) =>
		{
			if (!_syncing)
			{
				SetBlackLevel(_black.Value);
			}
		};
		_settingsCard.Controls.Add(_black);
		_blackReadout = Words(_settingsCard, "Higher keeps dark spots black", Fonts.Caption, Theme.TextFaint);
		_keyTitle = Words(_settingsCard, "Key", Fonts.BodyBold, Theme.Text);
		_keyCaption = Words(_settingsCard, "", Fonts.Caption, Theme.TextFaint);
		_keyError = Words(_settingsCard, "That key can't be used for Dark Boost.", Fonts.Caption, Theme.Negative);
		_keyError.Visible = false;
		_mode = new SegmentStrip(new string[2] { "Toggle", "Hold" })
		{
			AccessibleName = "Key mode"
		};
		_mode.Chosen += (object? _, string kind) =>
		{
			if (!_syncing)
			{
				ChooseHold(kind == "Hold");
			}
		};
		_settingsCard.Controls.Add(_mode);
		_key = new HotkeyPicker
		{
			AllowMouseButtons = true,
			AccessibleName = "Dark Boost key"
		};
		_key.HotkeyChanged += (uint mask, uint vk) =>
		{
			ChooseKey(mask, vk);
		};
		_settingsCard.Controls.Add(_key);
		_gamesTitle = Words(_settingsCard, "Only while a game is running", Fonts.BodyBold, Theme.Text);
		_gamesCaption = Words(_settingsCard, GamesOnlyCaption, Fonts.Caption, Theme.TextFaint);
		_games = new ToggleSwitch
		{
			Size = new Size(40, 20),
			AccessibleName = "Only while a game is running"
		};
		_games.CheckedChanged += (object? _, EventArgs _) =>
		{
			if (!_syncing)
			{
				SetGamesOnly(_games.Checked);
			}
		};
		_settingsCard.Controls.Add(_games);
		_fadeTitle = Words(_settingsCard, "Smooth fade", Fonts.BodyBold, Theme.Text);
		_fadeCaption = Words(_settingsCard, "No sudden flash.", Fonts.Caption, Theme.TextFaint);
		_fade = new ToggleSwitch
		{
			Size = new Size(40, 20),
			AccessibleName = "Smooth fade"
		};
		_fade.CheckedChanged += (object? _, EventArgs _) =>
		{
			if (!_syncing)
			{
				SetFade(_fade.Checked);
			}
		};
		_settingsCard.Controls.Add(_fade);
		_setupsTitle = Words(_settingsCard, "Saved setups", Fonts.BodyBold, Theme.Text);
		_setupsCaption = Words(_settingsCard, "The sliders save into the one you pick.", Fonts.Caption, Theme.TextFaint);
		_saveNew = new GlassButton
		{
			Text = "Save new",
			Kind = GlassButtonKind.Secondary
		};
		_saveNew.Click += (object? _, EventArgs _) =>
		{
			SaveNewSetup();
		};
		_delete = new GlassButton
		{
			Text = "Delete",
			Kind = GlassButtonKind.Quiet
		};
		_delete.Click += (object? _, EventArgs _) =>
		{
			DeleteSetup();
		};
		_settingsCard.Controls.Add(_saveNew);
		_settingsCard.Controls.Add(_delete);
		_nextTitle = Words(_settingsCard, "Next setup key", Fonts.BodyBold, Theme.Text);
		_nextCaption = Words(_settingsCard, "Flips to the next setup.", Fonts.Caption, Theme.TextFaint);
		_nextKey = new HotkeyPicker
		{
			AllowMouseButtons = true,
			AccessibleName = "Next setup key"
		};
		_nextKey.HotkeyChanged += (uint mask, uint vk) =>
		{
			ChooseNextKey(mask, vk);
		};
		_settingsCard.Controls.Add(_nextKey);
		Controls.Add(_banCard);
		Controls.Add(_viewsCard);
		Controls.Add(_weekCard);
		Controls.Add(_lockCard);
		Controls.Add(_settingsCard);
		UseHeaderPanel(_title, _subtitle, new Control[2] { _state, _headerTag });
		_fadeTimer.Tick += (object? _, EventArgs _) =>
		{
			FadeTick();
		};
		_owned = global::A.A.Available;
		_nowAmount = (_fadeFrom = (_fadeTo = ((_owned && _engine.NightVision) ? 1 : 0)));
		RebuildSetupChips();
		ApplyOffer();
		SyncControls();
		VisibleChanged += (object? _, EventArgs _) =>
		{
			if (Visible)
			{
				ReloadOffer();
			}
		};
	}

	public void AttachZoom(AppSettings settings, Action save, Func<bool> allowed, ZoomHooks hooks)
	{
		_zoomCard?.Dispose();
		_zoomCard = new CardPanel
		{
			Name = "ZoomCard",
			AccessibleName = "Zoom"
		};
		_zoomSection = new ZoomSection(settings, save, allowed, hooks.BindKey, hooks.WhyRefused);
		_zoomSection.Changed += hooks.Changed;
		_zoomCard.Controls.Add(_zoomSection);
		Controls.Add(_zoomCard);
		_zoomCard.TabIndex = _settingsCard.TabIndex;
		_settingsCard.TabIndex = _zoomCard.TabIndex + 1;
		LayoutContent();
	}

	public void RefreshZoomStanding()
	{
		_zoomSection?.Sync();
	}

	private static int S(int logical)
	{
		return Tokens.Scale(logical);
	}

	private static Label Words(Control parent, string text, Font font, Color ink, ContentAlignment align = ContentAlignment.TopLeft)
	{
		Label label = new Label
		{
			Text = text,
			Font = font,
			ForeColor = ink,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = align,
			UseMnemonic = false
		};
		parent.Controls.Add(label);
		return label;
	}

	internal void ReloadOffer()
	{
		RefreshZoomStanding();
		bool flag;
		try
		{
			flag = global::A.A.Available;
		}
		catch
		{
			flag = false;
		}
		if (flag == _owned)
		{
			SyncSwitch();
			return;
		}
		_owned = flag;
		ApplyOffer();
		SyncControls();
		SyncSwitch();
		LayoutContent();
		Invalidate(invalidateChildren: true);
	}

	internal IEnumerable<Control> InteractiveControls()
	{
		yield return _switch;
		TintTile[] tiles = _tiles;
		for (int i = 0; i < tiles.Length; i++)
		{
			yield return tiles[i];
		}
		yield return _strength;
		yield return _black;
		yield return _mode;
		yield return _key;
		yield return _games;
		yield return _fade;
		yield return _saveNew;
		yield return _delete;
		foreach (ChipButton setupChip in _setupChips)
		{
			yield return setupChip;
		}
		yield return _nextKey;
	}

	private void ApplyOffer()
	{
		_lockCard.Visible = !_owned;
		_settingsTag.Visible = !_owned;
		_state.Visible = _owned;
		ApplyStatus();
		foreach (Control item in InteractiveControls())
		{
			item.Enabled = _owned;
		}
		_delete.Enabled = _owned && DarkBoostSetups.CanDelete(_setups.Count);
		if (!_owned)
		{
			_keyError.Visible = false;
			StopFade(0.0);
		}
		if (HasHeaderPanel)
		{
			LayoutHeaderPanel();
		}
	}

	private void ApplyStatus()
	{
		_weekCard.Visible = _owned;
		if (OnTheWeek)
		{
			_weekTitle.Text = ((_weekDaysLeft == 1) ? "Free week. 1 day left." : $"Free week. {_weekDaysLeft} days left.");
		}
		else
		{
			_weekTitle.Text = "Dark Boost & Zoom is on your account.";
		}
		_weekCaption.Text = (OnTheWeek ? "No card. It turns off by itself." : "Separate from your plan.");
		_manage.Visible = !OnTheWeek;
	}

	public void SyncSwitch()
	{
		bool flag = _owned && _engine.NightVision;
		if (_switch.Checked != flag)
		{
			_syncing = true;
			try
			{
				_switch.Checked = flag;
			}
			finally
			{
				_syncing = false;
			}
			StartFade(flag ? 1 : 0);
		}
		UpdateState();
	}

	private void OnSwitch()
	{
		if (_syncing)
		{
			return;
		}
		if (!global::A.A.Available)
		{
			_syncing = true;
			try
			{
				_switch.Checked = false;
				return;
			}
			finally
			{
				_syncing = false;
			}
		}
		bool flag = _switch.Checked;
		if (flag && !new DarkBoostBanGate(_settings, _save, AskBanRiskForTest).Pass(FindForm()))
		{
			_syncing = true;
			try
			{
				_switch.Checked = false;
				return;
			}
			finally
			{
				_syncing = false;
			}
		}
		_engine.NightVision = flag && global::A.A.Available;
		_settings.NightVisionOn = flag;
		_save();
		StartFade(flag ? 1 : 0);
		UpdateState();
	}

	internal void SetOnForTest(bool on)
	{
		_switch.Checked = on;
	}

	private void UpdateState()
	{
		_state.Text = (_switch.Checked ? ("On · " + Current.Name) : "Ready");
		_nowState.Text = (_switch.Checked ? "On" : "Off");
		if (HasHeaderPanel)
		{
			LayoutHeaderPanel();
		}
		_state.Invalidate();
	}

	private void StartFade(double to)
	{
		_fadeFrom = _nowAmount;
		_fadeTo = to;
		_fadeStart = DateTime.UtcNow;
		if (!_settings.DarkBoostFade || _fadeFrom == _fadeTo)
		{
			StopFade(to);
		}
		else
		{
			_fadeTimer.Start();
		}
	}

	private void StopFade(double at)
	{
		_fadeTimer.Stop();
		_nowAmount = (_fadeFrom = (_fadeTo = at));
		RenderNow();
	}

	private void FadeTick()
	{
		TimeSpan timeSpan = DateTime.UtcNow - _fadeStart;
		_nowAmount = NowAmountAt(timeSpan);
		if (timeSpan >= DarkBoostFade.Duration)
		{
			StopFade(_fadeTo);
		}
		else
		{
			RenderNow();
		}
	}

	internal double NowAmountAt(TimeSpan elapsed)
	{
		return DarkBoostFade.Amount(_fadeFrom, _fadeTo, elapsed, _settings.DarkBoostFade);
	}

	private void Replace(DarkBoostSetup s)
	{
		_setups[_selected] = s.Clamped();
		Persist();
		SyncControls();
	}

	private void Persist()
	{
		_settings.DarkBoostSetups = _setups.ToList();
		_settings.DarkBoostSelected = _selected;
		_save();
		PushToEngine();
	}

	private void PushToEngine()
	{
		DarkBoostSetup current = Current;
		_engine.SetDarkBoostLook(current.Tint, current.Strength, current.BlackLevel, _settings.DarkBoostFade);
	}

	internal void ChooseTint(DarkBoostTint tint)
	{
		if (global::A.A.Available)
		{
			Replace(Current with
			{
				Tint = tint
			});
		}
	}

	internal void SetStrength(int value)
	{
		if (!global::A.A.Available)
		{
			SyncControls();
			return;
		}
		Replace(Current with
		{
			Strength = value
		});
	}

	internal void SetBlackLevel(int value)
	{
		if (!global::A.A.Available)
		{
			SyncControls();
			return;
		}
		Replace(Current with
		{
			BlackLevel = value
		});
	}

	internal void SelectSetup(int index)
	{
		if (global::A.A.Available)
		{
			_selected = DarkBoostSetups.Selected(index, _setups.Count);
			Persist();
			SyncControls();
			UpdateState();
		}
	}

	internal void NextSetup()
	{
		if (global::A.A.Available)
		{
			SelectSetup(DarkBoostSetups.Next(_selected, _setups.Count));
		}
	}

	internal void ReloadSetupsFromSettings()
	{
		_setups.Clear();
		_setups.AddRange(DarkBoostSetups.Normalise(_settings.DarkBoostSetups));
		_selected = DarkBoostSetups.Selected(_settings.DarkBoostSelected, _setups.Count);
		RebuildSetupChips();
		SyncControls();
		UpdateState();
		LayoutContent();
	}

	internal void SaveNewSetup()
	{
		if (global::A.A.Available)
		{
			_selected = DarkBoostSetups.SaveNew(_setups, _selected);
			Persist();
			RebuildSetupChips();
			SyncControls();
			UpdateState();
			LayoutContent();
		}
	}

	internal void DeleteSetup()
	{
		if (global::A.A.Available && DarkBoostSetups.CanDelete(_setups.Count))
		{
			_selected = DarkBoostSetups.Delete(_setups, _selected);
			Persist();
			RebuildSetupChips();
			SyncControls();
			UpdateState();
			LayoutContent();
		}
	}

	private void RebuildSetupChips()
	{
		foreach (ChipButton setupChip in _setupChips)
		{
			_settingsCard.Controls.Remove(setupChip);
			setupChip.Dispose();
		}
		_setupChips.Clear();
		for (int i = 0; i < _setups.Count; i++)
		{
			int at = i;
			ChipButton chipButton = new ChipButton
			{
				Text = _setups[i].Name,
				Enabled = _owned
			};
			chipButton.Click += (object? _, EventArgs _) =>
			{
				SelectSetup(at);
			};
			_setupChips.Add(chipButton);
			_settingsCard.Controls.Add(chipButton);
		}
		_delete.Enabled = _owned && DarkBoostSetups.CanDelete(_setups.Count);
		_nextKey.TabIndex = _settingsCard.Controls.Cast<Control>().Max((Control control) => control.TabIndex) + 1;
	}

	private void SetFade(bool on)
	{
		if (!global::A.A.Available)
		{
			SyncControls();
			return;
		}
		_settings.DarkBoostFade = on;
		_save();
		PushToEngine();
	}

	private void SetGamesOnly(bool on)
	{
		if (!global::A.A.Available)
		{
			SyncControls();
			return;
		}
		_settings.HotkeysOnlyInGames = on;
		_save();
		if (_settings.NightVisionHotkeyEnabled && _settings.NightVisionHotkeyVirtualKey != 0)
		{
			KeyChanged?.Invoke(_settings.NightVisionHotkeyModifierMask, _settings.NightVisionHotkeyVirtualKey);
		}
		NextKeyChanged?.Invoke();
	}

	internal void ChooseHold(bool hold)
	{
		if (!global::A.A.Available)
		{
			SyncControls();
			return;
		}
		_settings.NightVisionHotkeyHold = hold;
		_save();
		if (_settings.NightVisionHotkeyEnabled && _settings.NightVisionHotkeyVirtualKey != 0)
		{
			KeyChanged?.Invoke(_settings.NightVisionHotkeyModifierMask, _settings.NightVisionHotkeyVirtualKey);
		}
		SyncControls();
	}

	internal void ChooseKey(uint mask, uint vk)
	{
		if (!global::A.A.Available)
		{
			SyncControls();
			return;
		}
		bool flag = KeyChanged?.Invoke(mask, vk) ?? false;
		_keyError.Visible = !flag;
		_keyCaption.Visible = flag;
		SyncKeyPicker();
		RefreshZoomStanding();
	}

	internal void ChooseNextKey(uint mask, uint vk)
	{
		if (!global::A.A.Available)
		{
			SyncControls();
			return;
		}
		if (vk != 0 && !HotkeyPicker.IsBindable(mask, vk, out string _))
		{
			SyncControls();
			return;
		}
		if (vk != 0 && _settings.NightVisionHotkeyEnabled && vk == _settings.NightVisionHotkeyVirtualKey && mask == _settings.NightVisionHotkeyModifierMask)
		{
			SyncControls();
			return;
		}
		if (vk != 0 && HotkeyConflicts.Owner(_settings, HotkeySlot.NightVision, mask, vk).HasValue)
		{
			SyncControls();
			return;
		}
		_settings.DarkBoostNextKeyModifierMask = ((vk != 0) ? mask : 0u);
		_settings.DarkBoostNextKeyVirtualKey = vk;
		_save();
		SyncControls();
		NextKeyChanged?.Invoke();
		RefreshZoomStanding();
	}

	private void SyncKeyPicker()
	{
		_key.ModifierMask = _settings.NightVisionHotkeyModifierMask;
		_key.VirtualKey = (_settings.NightVisionHotkeyEnabled ? _settings.NightVisionHotkeyVirtualKey : 0u);
	}

	private void SyncControls()
	{
		_syncing = true;
		try
		{
			DarkBoostSetup current = Current;
			TintTile[] tiles = _tiles;
			foreach (TintTile obj in tiles)
			{
				obj.Selected = obj.Tint == current.Tint;
			}
			_strength.Value = current.Strength;
			_black.Value = current.BlackLevel;
			_strengthValue.Text = current.Strength + "%";
			_blackValue.Text = ((current.BlackLevel == 0) ? "Off" : (current.BlackLevel + "%"));
			_whiteValue.Text = (int)Math.Round(DarkBoostScene.WhiteFrom(current, _engine.DarkBoostPreviewBasis().Basis) * 100.0, MidpointRounding.ToEven) + "% brightness";
			_tintFact.Text = DarkBoostTints.Fact(current.Tint);
			_fade.Checked = _settings.DarkBoostFade;
			_games.Checked = _settings.HotkeysOnlyInGames;
			_mode.Active = (_settings.NightVisionHotkeyHold ? "Hold" : "Toggle");
			_keyCaption.Text = (_settings.NightVisionHotkeyHold ? "On only while you hold the key." : "Press once for on, again for off.");
			SyncKeyPicker();
			_nextKey.ModifierMask = _settings.DarkBoostNextKeyModifierMask;
			_nextKey.VirtualKey = _settings.DarkBoostNextKeyVirtualKey;
			if (!_owned)
			{
				_switch.Checked = false;
			}
			for (int j = 0; j < _setupChips.Count; j++)
			{
				_setupChips[j].Text = _setups[j].Name;
				_setupChips[j].Active = j == _selected;
			}
		}
		finally
		{
			_syncing = false;
		}
		_with.Picture = RenderSetup(1.0);
		RenderNow();
		Invalidate(invalidateChildren: true);
	}

	private void RenderNow()
	{
		_now.Picture = RenderSetup(_nowAmount);
	}

	private Bitmap RenderSetup(double amount)
	{
		(float[]? Basis, ushort[]? Ramp) tuple = _engine.DarkBoostPreviewBasis();
		float[] item = tuple.Basis;
		ushort[] item2 = tuple.Ramp;
		DarkBoostSetup current = Current;
		double strength = (double)current.Strength / 100.0 * amount;
		return DarkBoostScene.Render(DarkBoostScene.PictureFor(current, strength, item), (item2 == null) ? null : DarkBoostPicture.Ramp(item2, strength));
	}

	private int LayoutWeeks(int top, int inner)
	{
		return 0;
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		LayoutContent();
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		LayoutContent();
	}

	private void LayoutContent()
	{
		if (Width <= 0)
		{
			return;
		}
		_headerTag.Size = _headerTag.NaturalSize;
		_state.Remeasure();
		if (HasHeaderPanel)
		{
			LayoutHeaderPanel();
		}
		int contentLeft = GlowPage.ContentLeft;
		int num = Math.Max(S(480), ContentRight - GlowPage.ContentLeft);
		int num2 = ContentTop + AutoScrollPosition.Y;
		int num3 = S(21);
		int num4 = num - 2 * num3;
		int num5 = S(24);
		int num6 = num3 + num5 + S(12);
		int num7 = Math.Max(S(120), num - num3 - num6);
		int num8 = S(28);
		int num9 = TextRenderer.MeasureText("Some games, like Rust, ban players who see in the dark. It mostly happens when another player reports you. PlexusX never touches the game, but the ban risk is yours.", Fonts.Body, new Size(num7, 0), TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height + S(4);
		int num10 = num3 + num8 + S(6) + num9 + num3 - S(2);
		_banCard.SetBounds(contentLeft, num2, num, num10);
		foreach (Control control in _banCard.Controls)
		{
			if (control is WarningIcon)
			{
				control.SetBounds(num3, num3 - S(1) + (num8 - num5) / 2, num5, num5);
			}
		}
		Place(_banTitle, num6, num3 - S(1), num7, num8);
		Place(_banBody, num6, num3 - S(1) + num8 + S(6), num7, num9);
		num2 += num10 + S(16);
		int num11 = (num4 - S(16)) / 2;
		int num12 = S(182);
		_viewsCard.SetBounds(contentLeft, num2, num, S(272));
		_now.SetBounds(num3, num3, num11, num12);
		_with.SetBounds(num3 + num11 + S(16), num3, num11, num12);
		int y = num3 + num12 + S(6);
		Place(_nowCaption, num3, y, num11 / 2, S(16));
		Place(_nowState, num3 + num11 / 2, y, num11 - num11 / 2, S(16), right: true);
		Place(_withCaption, _with.Left, y, num11 / 2, S(16));
		Place(_withState, _with.Left + num11 / 2, y, num11 - num11 / 2, S(16), right: true);
		Place(_tintFact, num3, S(235), num4, S(16));
		num2 += _viewsCard.Height + S(16);
		if (_owned)
		{
			_weekCard.SetBounds(contentLeft, num2, num, S(84));
			_manage.Size = new Size(Math.Max(S(82), _manage.PreferredSize.Width), S(32));
			_manage.Location = new Point(num - num3 - _manage.Width, (S(84) - _manage.Height) / 2);
			int w = _manage.Left - num3 - S(16);
			Place(_weekTitle, num3, S(21), w, S(22));
			Place(_weekCaption, num3, S(47), w, S(16));
			num2 += _weekCard.Height + S(16);
		}
		if (!_owned)
		{
			int num13 = LayoutWeeks(S(241), num4);
			_hasWeekRows = num13 > 0;
			_lockCard.SetBounds(contentLeft, num2, num, S(234) + num13 + (_hasWeekRows ? S(16) : 0));
			int num14 = Math.Max(S(130), _buy.PreferredSize.Width);
			_buy.SetBounds(num - num3 - num14, num3 - S(1), num14, S(32));
			Place(_lockTitle, num3, num3 - S(1), num4 - num14 - S(12), S(28));
			Place(_lockCaption, num3, S(53), num4, S(16));
			int num15 = (num4 + S(16)) / 2 + S(4);
			for (int i = 0; i < _facts.Length; i++)
			{
				_facts[i].SetBounds(num3 + i % 2 * num15, S(83) + i / 2 * S(28), num15 - S(8), S(20));
			}
			Place(_lockFootnote, num3, S(197), num4, S(16));
			num2 += _lockCard.Height + S(16);
		}
		if (_zoomCard != null && _zoomSection != null)
		{
			int gutter = _zoomSection.Gutter;
			_zoomSection.Width = num4 + 2 * gutter;
			int wantedHeight = _zoomSection.WantedHeight;
			_zoomSection.SetBounds(num3 - gutter, num3, num4 + 2 * gutter, wantedHeight);
			_zoomCard.SetBounds(contentLeft, num2, num, wantedHeight + 2 * num3);
			num2 += _zoomCard.Height + S(16);
		}
		LayoutSettings(contentLeft, num2, num);
		FitScrollToContent(S(16));
	}

	private void LayoutSettings(int left, int top, int w)
	{
		int num = S(21);
		int num2 = w - 2 * num;
		int num3 = w - num;
		Place(_settingsTitle, num, num - S(1), num2 / 2, S(28));
		_settingsTag.Size = _settingsTag.NaturalSize;
		_settingsTag.Location = new Point(num3 - _settingsTag.Width, S(25));
		Place(_switchTitle, num, S(57), num2 - S(60), S(20));
		Place(_switchCaption, num, S(79), num2 - S(60), S(16));
		_switch.SetBounds(num3 - S(40), S(68), S(40), S(20));
		Place(_colourTitle, num, S(121), num2, S(20));
		int num4 = _tiles.Length;
		for (int i = 0; i < num4; i++)
		{
			int num5 = num + (int)Math.Round((double)(i * (num2 + S(8))) / (double)num4);
			int num6 = num + (int)Math.Round((double)((i + 1) * (num2 + S(8))) / (double)num4) - S(8);
			_tiles[i].SetBounds(num5, S(149), num6 - num5, S(56));
		}
		int num7 = (num2 - S(32)) / 2;
		int num8 = num + num7 + S(32);
		Place(_strengthLabel, num, S(221), num7 / 2, S(20));
		Place(_strengthValue, num + num7 / 2, S(221), num7 - num7 / 2, S(20), right: true);
		_strength.SetTrackBounds(num, S(241), num7);
		Place(_whiteLabel, num, S(275), num7 / 2, S(16));
		Place(_whiteValue, num + num7 / 2, S(275), num7 - num7 / 2, S(16), right: true);
		Place(_blackLabel, num8, S(221), num7 / 2, S(20));
		Place(_blackValue, num8 + num7 / 2, S(221), num7 - num7 / 2, S(20), right: true);
		_black.SetTrackBounds(num8, S(241), num7);
		Place(_blackReadout, num8, S(275), num7, S(16));
		int num9 = S(307);
		_key.Size = new Size(S(260), S(40));
		_key.Location = new Point(num3 - _key.Width, num9 + (S(56) - _key.Height) / 2);
		int num10 = Math.Max(S(120), _mode.NaturalWidth);
		_mode.SetBounds(_key.Left - S(8) - num10, num9 + (S(56) - S(32)) / 2, num10, S(32));
		RowText(_keyTitle, _keyCaption, num9, _mode.Left - num - S(16));
		Place(_keyError, num, num9 + S(28), _mode.Left - num - S(16), S(16));
		num9 += S(56);
		int num11 = num2 - S(60);
		_gamesCaption.Text = ((TextRenderer.MeasureText(GamesOnlyCaption, _gamesCaption.Font).Width <= num11) ? GamesOnlyCaption : "Works while one of the six supported games runs. Turn this off for any other game.");
		int num12 = Math.Max(S(16), TextRenderer.MeasureText(_gamesCaption.Text, _gamesCaption.Font, new Size(num11, int.MaxValue), TextFormatFlags.WordBreak).Height);
		int num13 = Math.Max(S(56), S(28) + num12 + S(12));
		RowText(_gamesTitle, _gamesCaption, num9, num11);
		Place(_gamesCaption, num, num9 + S(28), num11, num12);
		_games.SetBounds(num3 - S(40), num9 + (num13 - S(20)) / 2, S(40), S(20));
		num9 += num13;
		RowText(_fadeTitle, _fadeCaption, num9, num2 - S(60));
		_fade.SetBounds(num3 - S(40), num9 + (S(56) - S(20)) / 2, S(40), S(20));
		num9 += S(56);
		RowText(_setupsTitle, _setupsCaption, num9, num2 - S(220));
		int num14 = S(28);
		int y = num9 + (S(56) - num14) / 2;
		_delete.Size = new Size(Math.Max(S(64), _delete.PreferredSize.Width), num14);
		_delete.Location = new Point(num3 - _delete.Width, y);
		_saveNew.Size = new Size(Math.Max(S(84), _saveNew.PreferredSize.Width), num14);
		_saveNew.Location = new Point(_delete.Left - S(8) - _saveNew.Width, y);
		num9 += S(56);
		int num15 = num;
		foreach (ChipButton setupChip in _setupChips)
		{
			int num16 = TextRenderer.MeasureText(setupChip.Text, Fonts.BodyBold).Width + S(24);
			if (num15 + num16 > num3 && num15 > num)
			{
				num15 = num;
				num9 += S(36);
			}
			setupChip.SetBounds(num15, num9, num16, S(28));
			num15 += num16 + S(8);
		}
		num9 += S(28) + S(8);
		_nextKey.Size = new Size(S(260), S(40));
		_nextKey.Location = new Point(num3 - _nextKey.Width, num9 + (S(56) - _nextKey.Height) / 2);
		RowText(_nextTitle, _nextCaption, num9, _nextKey.Left - num - S(16));
		num9 += S(56);
		_settingsCard.SetBounds(left, top, w, num9 + num - S(8));
	}

	private static void RowText(Label title, Label caption, int top, int width)
	{
		Place(title, S(21), top + S(8), width, S(20));
		Place(caption, S(21), top + S(28), width, S(16));
	}

	private static void Place(Label l, int x, int y, int w, int h, bool right = false)
	{
		int num = ControlInk.LabelInkInset(l.Font);
		l.SetBounds(right ? (x + num) : (x - num), y, w, h + S(2));
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_fadeTimer.Stop();
			_fadeTimer.Dispose();
			_now.Picture = null;
			_with.Picture = null;
		}
		base.Dispose(disposing);
	}

	private static void PaintBanFrame(Graphics g, Size size)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		float num = S(8);
		RectangleF rectangleF = new RectangleF(0.75f, 0.75f, (float)size.Width - 1.5f, (float)size.Height - 1.5f);
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddArc(rectangleF.Left, rectangleF.Top, num * 2f, num * 2f, 180f, 90f);
		graphicsPath.AddArc(rectangleF.Right - num * 2f, rectangleF.Top, num * 2f, num * 2f, 270f, 90f);
		graphicsPath.AddArc(rectangleF.Right - num * 2f, rectangleF.Bottom - num * 2f, num * 2f, num * 2f, 0f, 90f);
		graphicsPath.AddArc(rectangleF.Left, rectangleF.Bottom - num * 2f, num * 2f, num * 2f, 90f, 90f);
		graphicsPath.CloseFigure();
		GraphicsState gstate = g.Save();
		try
		{
			using SolidBrush brush = new SolidBrush(Theme.Negative);
			float width = Math.Max(4f, S(5));
			RectangleF rect = new RectangleF(rectangleF.Left + 1f, rectangleF.Top, width, rectangleF.Height);
			g.SetClip(graphicsPath, CombineMode.Intersect);
			g.FillRectangle(brush, rect);
		}
		finally
		{
			g.Restore(gstate);
		}
		using Pen pen = new Pen(Theme.Negative, 1.5f);
		g.DrawPath(pen, graphicsPath);
	}
}
