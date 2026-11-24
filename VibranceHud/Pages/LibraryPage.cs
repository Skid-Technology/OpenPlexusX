using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using A;
using VibranceHud.Controls;
using VibranceHud.Crosshair;
using VibranceHud.Design;
using VibranceHud.Display;
using VibranceHud.Licensing;
using VibranceHud.Presets;

namespace VibranceHud.Pages;

public sealed class LibraryPage : GlowPage
{
	private sealed record Fact(string Name, string Author, string Blurb, string Summary, string Glyph, Action Apply, string? CommunityId);

	internal sealed class StatusLine : Label
	{
		private readonly bool _link;

		private bool _hovered;

		private readonly OutsideFocusRing _focusRing;

		internal StatusLine(bool link = false)
		{
			_link = link;
			_focusRing = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 4f));
			if (link)
			{
				SetStyle(ControlStyles.Selectable, value: true);
			}
			TabStop = link;
		}

		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			AccessibleName = Text;
		}

		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			_focusRing?.Rehook();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_focusRing.Release();
			}
			base.Dispose(disposing);
		}

		protected override bool IsInputKey(Keys keyData)
		{
			if (!_link || (keyData & Keys.KeyCode) != Keys.Return)
			{
				return base.IsInputKey(keyData);
			}
			return true;
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			_hovered = true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_hovered = false;
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

		protected override void OnPaint(PaintEventArgs e)
		{
			_focusRing.Show(_link && Focused && ShowFocusCues);
			using Font font = ((_link && (_hovered || Focused)) ? new Font(Font, Font.Style | FontStyle.Underline) : null);
			Font font2 = font ?? Font;
			if (SlateSurface.Active && Text.Length > 0)
			{
				TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
				int num = Tokens.Scale(8);
				int width = Math.Max(1, Width - 2 * num);
				Size size = TextRenderer.MeasureText(e.Graphics, Text, font2, new Size(width, int.MaxValue), flags);
				RectangleF rect = new RectangleF(0f, 0f, Math.Min(Width, size.Width + 2 * num), Math.Min(Height, size.Height + 2));
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				Glass.PaintPanel(e.Graphics, rect, Tokens.Scale(4), Glass.BackdropOffsetOf(this));
				TextRenderer.DrawText(e.Graphics, Text, font2, new Rectangle(num, 1, width, Math.Max(1, Height - 1)), ForeColor, flags);
			}
			else if (_link)
			{
				TextRenderer.DrawText(e.Graphics, Text, font2, ClientRectangle, ForeColor, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak);
			}
			else
			{
				base.OnPaint(e);
			}
		}
	}

	private const int Pad = 16;

	private const int CardW = 932;

	private const int RightInset = 15;

	private new const int Gutter = 0;

	private const int CardGap = 16;

	private const int RowGap = 16;

	private const int WallTop = 0;

	private const int RowHeight = 32;

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly Func<DisplayPreset, LookNote> _onApply;

	private readonly Func<bool> _nvidiaDriverAvailable;

	internal const string ColourKind = "Colour looks";

	internal const string CrosshairKind = "Crosshairs";

	internal const string DisplayKind = "Resolutions";

	internal const string ConfigKind = "Setups";

	internal const string AllGamesLabel = "All games";

	internal static readonly string[] Kinds = new string[4] { "Colour looks", "Crosshairs", "Resolutions", "Setups" };

	private const int TabStripW = 369;

	private const int GamePickerW = 128;

	private const int SortSlotW = 112;

	private const int FilterGap = 8;

	private const int WallHeight = 424;

	private const int DesignCardH = 204;

	private readonly SegmentStrip _kindTabs;

	private readonly GlassTextBox _search;

	private readonly Panel _toolbar;

	private readonly GlassDropdown _gamePicker;

	private readonly List<LibraryCard> _crosshairChips = new List<LibraryCard>();

	private readonly Action<CrosshairConfig>? _onApplyCrosshair;

	private readonly Func<MonitorCode, string>? _onApplyMode;

	private string _kind = "Colour looks";

	private readonly QuietScrollPanel _wall;

	private readonly Label _status;

	private readonly List<LibraryCard> _chips = new List<LibraryCard>();

	private readonly GlassButton _upload;

	private readonly GlassButton _retry;

	private readonly Action<string>? _onOpenPage;

	private bool _fetchFailed;

	private readonly LibraryToast _toast;

	private LibraryPreset? _currentApply;

	private LibraryPreset? _previousApply;

	private readonly List<Label> _blurbs = new List<Label>();

	private readonly List<Control> _alsoInHere = new List<Control>();

	private readonly List<int> _alsoInHereOffsets = new List<int>();

	private string _game = "All";

	private IReadOnlyList<LibraryPreset> _looks;

	private IReadOnlyList<CrosshairLibraryEntry> _crosshairs;

	private IReadOnlyList<DisplayLibraryEntry> _displays;

	private IReadOnlyList<ConfigLibraryEntry> _configs;

	private bool _scaledByTheBase;

	private int _followedDpi = 96;

	internal static readonly (string What, string Tab)[] WhatsInHereLines = new (string, string)[4]
	{
		("Stretched 4:3 for Rust and CS2", "Resolution"),
		("Pick which monitor to change", "Display"),
		("Your panel's own brightness and RGB, over the cable", "Monitor"),
		("Share your crosshair as a code", "Crosshair")
	};

	private const int MaxWallPasses = 3;

	private bool _rebuildingWall;

	private bool _wallAskedAgain;

	private (int Width, int Scale) _wallBuiltFor = (Width: -1, Scale: -1);

	private int _blurbHeight;

	private bool FetchFailedWithoutCache
	{
		get
		{
			if (_fetchFailed && _looks.Count == 0 && _crosshairs.Count == 0 && _displays.Count == 0 && _configs.Count == 0)
			{
				return true;
			}
			return false;
		}
	}

	protected override string PageTitle => "Library";

	protected override string PageSubtitle => "Looks, crosshairs, resolutions and whole setups people share.";

	protected override bool UsesHeaderPanel => true;

	protected override Control[] HeaderActions => new Control[1] { _upload };

	protected override Control? HeaderToolbar => _toolbar;

	private float LaidOutScale
	{
		get
		{
			if (!_scaledByTheBase)
			{
				return 1f;
			}
			return (float)Tokens.Dpi / 96f;
		}
	}

	private int ContentW
	{
		get
		{
			int num = Width - Scaled(16) - Scaled(15);
			if (num < Scaled(240))
			{
				return Scaled(932);
			}
			return num;
		}
	}

	internal string TestUploadUrl => "https://plexusx.lol/dashboard?tab=presets";

	internal GlassButton TestUploadButton => _upload;

	internal int TestBlurbHeight => _blurbHeight;

	internal IReadOnlyList<LibraryCard> TestChips => _chips;

	internal Control TestKindTabs => _kindTabs;

	internal Control TestWall => _wall;

	internal Control TestSearchBox => _search;

	internal Control TestGamePicker => _gamePicker;

	internal IReadOnlyList<LibraryCard> TestCrosshairChips => _crosshairChips;

	internal string TestGame => _game;

	internal string TestKind => _kind;

	internal LibraryToast TestToast => _toast;

	internal IReadOnlyList<string> TestCardFooters => _chips.Select((LibraryCard libraryCard) => libraryCard.Footer).ToList();

	internal string TestStatus => _status.Text;

	internal Label TestStatusLabel => _status;

	public event EventHandler? RetryRequested;

	private IReadOnlyList<string> GameNames()
	{
		IEnumerable<string> first = new string[1] { "All" };
		return first.Concat(_kind switch
		{
			"Crosshairs" => _crosshairs.Select((CrosshairLibraryEntry crosshairLibraryEntry) => crosshairLibraryEntry.Game).Distinct(),
			"Resolutions" => _displays.Select((DisplayLibraryEntry displayLibraryEntry) => displayLibraryEntry.Game).Distinct(),
			"Setups" => _configs.Select((ConfigLibraryEntry configLibraryEntry) => configLibraryEntry.Game).Distinct(),
			_ => _looks.Select((LibraryPreset p) => p.Game).Distinct(),
		}).Distinct().ToList();
	}

	private IReadOnlyList<string> GameLabels()
	{
		return (from g in GameNames()
			select (!string.Equals(g, "All", StringComparison.OrdinalIgnoreCase)) ? g : "All games").ToList();
	}

	private IReadOnlyList<LibraryPreset> LooksForGame()
	{
		if (!string.Equals(_game, "All", StringComparison.OrdinalIgnoreCase))
		{
			return _looks.Where((LibraryPreset p) => string.Equals(p.Game, _game, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		return _looks;
	}

	private IReadOnlyList<CrosshairLibraryEntry> CrosshairsForGame()
	{
		if (!string.Equals(_game, "All", StringComparison.OrdinalIgnoreCase))
		{
			return _crosshairs.Where((CrosshairLibraryEntry crosshairLibraryEntry) => string.Equals(crosshairLibraryEntry.Game, _game, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		return _crosshairs;
	}

	private IReadOnlyList<DisplayLibraryEntry> DisplaysForGame()
	{
		if (!string.Equals(_game, "All", StringComparison.OrdinalIgnoreCase))
		{
			return _displays.Where((DisplayLibraryEntry displayLibraryEntry) => string.Equals(displayLibraryEntry.Game, _game, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		return _displays;
	}

	private IReadOnlyList<ConfigLibraryEntry> ConfigsForGame()
	{
		if (!string.Equals(_game, "All", StringComparison.OrdinalIgnoreCase))
		{
			return _configs.Where((ConfigLibraryEntry configLibraryEntry) => string.Equals(configLibraryEntry.Game, _game, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		return _configs;
	}

	public LibraryPage(
		AppSettings settings,
		SettingsStore store,
		Func<DisplayPreset, LookNote> onApply,
		Func<bool>? nvidiaDriverAvailable = null,
		Action<CrosshairConfig>? onApplyCrosshair = null,
		IReadOnlyList<LibraryPreset>? looks = null,
		IReadOnlyList<CrosshairLibraryEntry>? crosshairs = null,
		Func<MonitorCode, string>? onApplyMode = null,
		IReadOnlyList<DisplayLibraryEntry>? displays = null,
		IReadOnlyList<ConfigLibraryEntry>? configs = null,
		Action<string>? onOpenPage = null)
	{
		_onOpenPage = onOpenPage;
		_settings = settings;
		_store = store;
		_onApply = onApply;
		_onApplyCrosshair = onApplyCrosshair;
		_onApplyMode = onApplyMode;
		_nvidiaDriverAvailable = nvidiaDriverAvailable ?? (() => false);
		_looks = looks ?? PresetLibrary.All;
		_crosshairs = crosshairs ?? CrosshairLibrary.All;
		_displays = displays ?? DisplayLibrary.All;
		_configs = configs ?? ConfigLibrary.All;
		Padding = new Padding(0, 0, 0, 40);
		_toolbar = new Panel
		{
			Name = "LibraryToolbar",
			BackColor = Color.Transparent,
			Size = new Size(890, 32)
		};
		_kindTabs = new SegmentStrip(Kinds)
		{
			AccessibleName = "Content kind",
			Size = new Size(369, 32),
			Active = _kind,
			ArrowsChoose = true
		};
		_kindTabs.Chosen += (object? _, string kind) =>
		{
			SelectKind(kind);
		};
		_toolbar.Controls.Add(_kindTabs);
		_search = new GlassTextBox
		{
			PlaceholderText = "Search looks",
			AccessibleName = "Search",
			LeadingIcon = SignalIcon.Search,
			Size = new Size(240, 32)
		};
		_search.Inner.TextChanged += (object? _, EventArgs _) =>
		{
			RebuildWall();
		};
		_toolbar.Controls.Add(_search);
		_gamePicker = new GlassDropdown
		{
			AccessibleName = "Game",
			Size = new Size(128, 32)
		};
		_gamePicker.SetItems(GameLabels());
		_gamePicker.SelectedIndex = 0;
		_gamePicker.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			IReadOnlyList<string> readOnlyList = GameNames();
			if (_gamePicker.SelectedIndex >= 0 && _gamePicker.SelectedIndex < readOnlyList.Count)
			{
				_game = readOnlyList[_gamePicker.SelectedIndex];
				RebuildWall();
			}
		};
		_toolbar.Controls.Add(_gamePicker);
		_toolbar.Resize += (object? _, EventArgs _) =>
		{
			LayOutToolbar();
		};
		Controls.Add(_toolbar);
		_upload = new GlassButton
		{
			Text = "Upload yours",
			Kind = GlassButtonKind.Secondary,
			Icon = SignalIcon.Upload,
			Height = 32
		};
		_upload.Width = SettingsPage.PreferredLogicalWidth(_upload);
		_upload.Click += (object? _, EventArgs _) =>
		{
			OpenUploadPage();
		};
		Controls.Add(_upload);
		_wall = new QuietScrollPanel
		{
			Location = new Point(16, 0),
			Size = new Size(932, 424)
		};
		Controls.Add(_wall);
		int y = _wall.Bottom + 14;
		_status = new StatusLine
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(16, y),
			Size = new Size(932, 34)
		};
		Controls.Add(_status);
		_retry = new GlassButton
		{
			Text = "Retry",
			Kind = GlassButtonKind.Secondary,
			Height = 32,
			Visible = false
		};
		_retry.Width = SettingsPage.PreferredLogicalWidth(_retry);
		_retry.Click += (object? _, EventArgs _) =>
		{
			RetryRequested?.Invoke(this, EventArgs.Empty);
		};
		Controls.Add(_retry);
		_toast = new LibraryToast();
		Controls.Add(_toast);
		_toast.UndoRequested += (object? _, EventArgs _) =>
		{
			LibraryPreset previousApply = _previousApply;
			if ((object)previousApply != null)
			{
				Apply(previousApply);
			}
			else
			{
				_toast.Hide();
				_status.Visible = _status.Text.Length > 0;
			}
		};
		_toast.CloseRequested += (object? _, EventArgs _) =>
		{
			_toast.Hide();
			_status.Visible = _status.Text.Length > 0;
		};
		ScrollStateChanged += (object? _, EventArgs _) =>
		{
			RepositionToast();
		};
		Resize += (object? _, EventArgs _) =>
		{
			FollowTheMonitor();
			RelayoutHeader();
			if (TheWallMovedSinceItWasBuilt())
			{
				RebuildWall();
			}
			RepositionToast();
		};
		ParentChanged += (object? _, EventArgs _) =>
		{
			if (FollowTheMonitor())
			{
				Relayout();
			}
		};
		VisibleChanged += (object? _, EventArgs _) =>
		{
			if (Visible && FollowTheMonitor())
			{
				Relayout();
			}
		};
		RelayoutHeader();
		RebuildWall();
		BuildWhatsInHere(_status.Bottom + 26);
		SettleBelowWall();
		RepositionToast();
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		_scaledByTheBase = true;
		_followedDpi = Tokens.Dpi;
		Relayout();
	}

	private bool FollowTheMonitor()
	{
		if (!_scaledByTheBase || IsDisposed)
		{
			return false;
		}
		int dpi = Tokens.Dpi;
		if (dpi == _followedDpi)
		{
			return false;
		}
		ApplyDpi(dpi);
		_followedDpi = dpi;
		_status.Font = Fonts.Caption;
		for (int i = 0; i < _alsoInHere.Count; i++)
		{
			_alsoInHere[i].Font = ((i == 0) ? Fonts.CaptionBold : Fonts.Caption);
		}
		return true;
	}

	private void Relayout()
	{
		RelayoutHeader();
		RebuildWall();
		RepositionToast();
	}

	private void SelectKind(string kind)
	{
		if (!(_kind == kind))
		{
			_kind = kind;
			GlassTextBox search = _search;
			search.SetPlaceholder(kind switch
			{
				"Crosshairs" => "Search crosshairs",
				"Resolutions" => "Search resolutions",
				"Setups" => "Search setups",
				_ => "Search looks",
			});
			_search.Invalidate();
			_kindTabs.Active = kind;
			_gamePicker.SetItems(GameLabels());
			_gamePicker.SelectedIndex = 0;
			_game = "All";
			RebuildWall();
		}
	}

	private void SettleBelowWall()
	{
		_retry.Location = new Point(_status.Left, _status.Bottom + Scaled(8));
		int num = (FetchFailedWithoutCache ? _retry.Bottom : _status.Bottom) + Scaled(26);
		foreach (var item3 in _alsoInHere.Zip(_alsoInHereOffsets))
		{
			Control item = item3.First;
			int item2 = item3.Second;
			item.Location = new Point(item.Location.X, num + Scaled(item2));
		}
		FitScrollToContent();
	}

	private void BuildWhatsInHere(int y)
	{
		int num = y;
		Label label = UiHelpers.Caption("Also in here", 16, y, 260);
		Controls.Add(label);
		_alsoInHere.Add(label);
		_alsoInHereOffsets.Add(y - num);
		y += 26;
		(string, string)[] whatsInHereLines = WhatsInHereLines;
		for (int i = 0; i < whatsInHereLines.Length; i++)
		{
			(string What, string Tab) line = whatsInHereLines[i];
			StatusLine statusLine = new StatusLine(link: true)
			{
				Text = line.What + "  —  " + line.Tab + " page",
				Cursor = Cursors.Hand,
				AccessibleRole = AccessibleRole.Link,
				ForeColor = Theme.TextDim,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(16, y),
				Size = new Size(932, 20)
			};
			statusLine.Click += (object? _, EventArgs _) =>
			{
				_onOpenPage?.Invoke(line.Tab);
			};
			statusLine.KeyDown += (object? _, KeyEventArgs e2) =>
			{
				if (e2.KeyCode == Keys.Return)
				{
					_onOpenPage?.Invoke(line.Tab);
					e2.Handled = true;
					e2.SuppressKeyPress = true;
				}
			};
			Controls.Add(statusLine);
			_alsoInHere.Add(statusLine);
			_alsoInHereOffsets.Add(y - num);
			y += 22;
		}
	}

	private void RebuildWall()
	{
		if (_rebuildingWall)
		{
			_wallAskedAgain = true;
			return;
		}
		_rebuildingWall = true;
		(int, string)? had = FocusedCard();
		try
		{
			for (int i = 0; i < 3; i++)
			{
				_wallAskedAgain = false;
				RebuildWallOnce();
				if (!_wallAskedAgain)
				{
					break;
				}
			}
			GiveTheFocusBack(had);
		}
		finally
		{
			_rebuildingWall = false;
			_wallAskedAgain = false;
			_wallBuiltFor = (Width: _wall.Width, Scale: Scaled(1000));
		}
	}

	private bool TheWallMovedSinceItWasBuilt()
	{
		int width = _wall.Width;
		int num = Scaled(1000);
		(int, int) wallBuiltFor = _wallBuiltFor;
		if (width == wallBuiltFor.Item1)
		{
			return num != wallBuiltFor.Item2;
		}
		return true;
	}

	private (int Index, string Caption)? FocusedCard()
	{
		List<LibraryCard>[] array = new List<LibraryCard>[2] { _chips, _crosshairChips };
		foreach (List<LibraryCard> list in array)
		{
			for (int j = 0; j < list.Count; j++)
			{
				if (!list[j].IsDisposed && list[j].ContainsFocus)
				{
					return (j, list[j].Caption);
				}
			}
		}
		return null;
	}

	private void GiveTheFocusBack((int Index, string Caption)? had)
	{
		if (!had.HasValue)
		{
			return;
		}
		(int Index, string Caption) was = had.GetValueOrDefault();
		List<LibraryCard> list = ((_crosshairChips.Count > 0) ? _crosshairChips : _chips);
		if (list.Count != 0)
		{
			(list.FirstOrDefault((LibraryCard libraryCard) => libraryCard.Caption == was.Caption) ?? list[Math.Min(was.Index, list.Count - 1)]).Focus();
		}
	}

	private void RebuildWallOnce()
	{
		FollowTheMonitor();
		LibraryCard[] array = _chips.Concat(_crosshairChips).ToArray();
		Label[] array2 = _blurbs.ToArray();
		_chips.Clear();
		_crosshairChips.Clear();
		_blurbs.Clear();
		LibraryCard[] array3 = array;
		foreach (LibraryCard libraryCard in array3)
		{
			_wall.Controls.Remove(libraryCard);
			libraryCard.Dispose();
		}
		Label[] array4 = array2;
		foreach (Label label in array4)
		{
			_wall.Controls.Remove(label);
			label.Dispose();
		}
		if (_kind == "Crosshairs")
		{
			RebuildCrosshairWall();
			return;
		}
		if (_kind == "Resolutions")
		{
			RebuildFactWall((from displayLibraryEntry in DisplaysForGame()
				select new Fact(displayLibraryEntry.Name, displayLibraryEntry.Author, displayLibraryEntry.Blurb, displayLibraryEntry.Summary(), "screen", () =>
				{
					ApplyMode(displayLibraryEntry);
				}, displayLibraryEntry.CommunityId)).ToList(), "No resolutions here yet. Press UPLOAD to be the first.");
			return;
		}
		if (_kind == "Setups")
		{
			RebuildFactWall((from configLibraryEntry in ConfigsForGame()
				select new Fact(configLibraryEntry.Name, configLibraryEntry.Author, configLibraryEntry.Blurb, configLibraryEntry.Summary(), "setup", () =>
				{
					ApplyConfig(configLibraryEntry);
				}, configLibraryEntry.CommunityId)).ToList(), "No full setups here yet. Press UPLOAD to be the first.");
			return;
		}
		IReadOnlyList<LibraryPreset> readOnlyList = Matching(LooksForGame(), (LibraryPreset p) => p.Name, (LibraryPreset p) => p.Author, (LibraryPreset p) => p.Blurb);
		bool flag = _nvidiaDriverAvailable();
		(int[] Lefts, int[] Widths, int CardH) tuple = WallGeometry(3, _looks.Select((LibraryPreset p) => p.Blurb), LongerOf(SampleFacts(readOnlyList), ""));
		int[] item = tuple.Lefts;
		int[] item2 = tuple.Widths;
		int item3 = tuple.CardH;
		int num = item3 + Scaled(16);
		for (int num2 = 0; num2 < readOnlyList.Count; num2++)
		{
			LibraryPreset libraryPreset = readOnlyList[num2];
			DisplayPreset look = libraryPreset.Look;
			int x = item[num2 % 3];
			int width = item2[num2 % 3];
			int y = Scaled(0) + num2 / 3 * num;
			LibraryCard libraryCard2 = new LibraryCard
			{
				Caption = libraryPreset.Name,
				Author = ShortAuthor(libraryPreset.Author),
				Subtitle = libraryPreset.Blurb,
				Kind = look.Glyph,
				Size = new Size(width, item3),
				Location = new Point(x, y),
				Saturation = look.Saturation,
				Vibrance = look.Vibrance,
				Temperature = look.Temperature,
				Matrix = ColorAdjust.BuildWithVibranceNumbers((float)look.Saturation / 100f, e.A(look.Vibrance, flag, false).Red, e.A(look.Vibrance, flag, false).Chroma, (float)look.Contrast / 100f, (float)look.Brightness / 100f, (float)look.Temperature / 100f, look.Depth().Clamped())
			};
			LibraryPreset captured = libraryPreset;
			libraryCard2.Click += (object? _, EventArgs _) =>
			{
				Apply(captured);
			};
			OfferReport(libraryCard2, libraryPreset.CommunityId);
			_chips.Add(libraryCard2);
			_wall.Controls.Add(libraryCard2);
		}
		int num3 = readOnlyList.Count;
		int num4 = (num3 + 3 - 1) / 3;
		_wall.Height = Math.Max(Scaled(120), Scaled(0) * 2 + Math.Max(0, num4 * num - Scaled(16)));
		_status.Location = new Point(Scaled(16), _wall.Bottom + Scaled(14));
		SetStatus((num3 == 0) ? "No looks here yet. Press UPLOAD to be the first." : "");
		MarkApplied(_currentApply);
		SettleBelowWall();
	}

	private int Scaled(int logical)
	{
		return (int)Math.Round((float)logical * LaidOutScale);
	}

	private void RelayoutHeader()
	{
		FollowTheMonitor();
		int contentW = ContentW;
		_wall.Width = contentW;
		_status.Width = Math.Max(1, contentW - 2 * Scaled(0));
		LayOutToolbar();
	}

	private void LayOutToolbar()
	{
		if (_toolbar != null && _kindTabs != null && _search != null && _gamePicker != null)
		{
			int width = _toolbar.Width;
			int height = _toolbar.Height;
			int num = Scaled(8);
			int num2 = Math.Min(Scaled(369), Math.Max(1, width / 2));
			_kindTabs.SetBounds(0, 0, num2, height);
			int num3 = Scaled(128);
			int num4 = width - Scaled(112) - num;
			if (num4 - num3 - num - (num2 + num) < Scaled(120))
			{
				num4 = width;
			}
			int num5 = Math.Max(num2 + num, num4 - num3);
			_gamePicker.SetBounds(num5, 0, num3, height);
			int num6 = num2 + num;
			_search.SetBounds(num6, 0, Math.Max(Scaled(60), num5 - num - num6), height);
		}
	}

	private IReadOnlyList<T> Matching<T>(IReadOnlyList<T> all, params Func<T, string>[] fields)
	{
		string needle = _search.Text.Trim();
		if (needle.Length == 0)
		{
			return all;
		}
		return all.Where((T item) => fields.Any((Func<T, string> field) => (field(item) ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase))).ToList();
	}

	private (int[] Lefts, int[] Widths, int CardH) WallGeometry(int columns, IEnumerable<string> blurbs, string longestFacts)
	{
		int num = Scaled(16);
		double num2 = (double)(Math.Max(columns, _wall.Width - 2 * Scaled(0)) + num) / (double)columns;
		int[] array = new int[columns];
		int[] array2 = new int[columns];
		for (int i = 0; i < columns; i++)
		{
			int num3 = (int)Math.Round((double)i * num2);
			int num4 = (int)Math.Round((double)(i + 1) * num2 - (double)num);
			array[i] = Scaled(0) + num3;
			array2[i] = Math.Max(1, num4 - num3);
		}
		int num5 = array2.Min();
		int num6 = (_blurbHeight = Math.Max(LibraryCard.MinimumDescription, TallestBlurb(num5 - 2 * Scaled(17), blurbs)));
		bool sharedFooter = LibraryCard.FooterSharesARow(num5, longestFacts, "Apply");
		int item = Math.Max(Scaled(204), num6 + LibraryCard.FixedChrome(sharedFooter));
		return (Lefts: array, Widths: array2, CardH: item);
	}

	private static string SampleFacts(IReadOnlyList<LibraryPreset> presets)
	{
		string text = "";
		foreach (LibraryPreset preset in presets)
		{
			string text2 = "Vibrance " + preset.Look.Vibrance.ToString(CultureInfo.InvariantCulture) + " · Temp " + LibraryCard.SignedTemperature(preset.Look.Temperature);
			if (text2.Length > text.Length)
			{
				text = text2;
			}
		}
		if (text.Length <= 0)
		{
			return "Vibrance 100 · Temp 0";
		}
		return text;
	}

	private void RebuildCrosshairWall()
	{
		IReadOnlyList<CrosshairLibraryEntry> readOnlyList = Matching(CrosshairsForGame(), (CrosshairLibraryEntry crosshairLibraryEntry2) => crosshairLibraryEntry2.Name, (CrosshairLibraryEntry crosshairLibraryEntry2) => crosshairLibraryEntry2.Author, (CrosshairLibraryEntry crosshairLibraryEntry2) => crosshairLibraryEntry2.Blurb);
		string text = "";
		foreach (CrosshairLibraryEntry crosshair in _crosshairs)
		{
			string text2 = CrosshairFacts(crosshair.Crosshair);
			if (text2.Length > text.Length)
			{
				text = text2;
			}
		}
		(int[] Lefts, int[] Widths, int CardH) tuple = WallGeometry(3, _crosshairs.Select((CrosshairLibraryEntry crosshairLibraryEntry2) => crosshairLibraryEntry2.Blurb), text);
		int[] item = tuple.Lefts;
		int[] item2 = tuple.Widths;
		int item3 = tuple.CardH;
		int num = item3 + Scaled(16);
		for (int num2 = 0; num2 < readOnlyList.Count; num2++)
		{
			CrosshairLibraryEntry crosshairLibraryEntry = readOnlyList[num2];
			LibraryCard libraryCard = new LibraryCard
			{
				Caption = crosshairLibraryEntry.Name,
				Author = ShortAuthor(crosshairLibraryEntry.Author),
				Subtitle = crosshairLibraryEntry.Blurb,
				Kind = "custom",
				CrosshairPreview = crosshairLibraryEntry.Crosshair,
				Footer = CrosshairFacts(crosshairLibraryEntry.Crosshair),
				Size = new Size(item2[num2 % 3], item3),
				Location = new Point(item[num2 % 3], Scaled(0) + num2 / 3 * num)
			};
			CrosshairLibraryEntry captured = crosshairLibraryEntry;
			libraryCard.Click += (object? _, EventArgs _) =>
			{
				ApplyCrosshair(captured);
			};
			OfferReport(libraryCard, crosshairLibraryEntry.CommunityId);
			_crosshairChips.Add(libraryCard);
			_wall.Controls.Add(libraryCard);
		}
		int num3 = readOnlyList.Count;
		int num4 = (num3 + 3 - 1) / 3;
		_wall.Height = Math.Max(Scaled(120), Scaled(0) * 2 + Math.Max(0, num4 * num - Scaled(16)));
		_status.Location = new Point(Scaled(16), _wall.Bottom + Scaled(14));
		SetStatus((num3 == 0) ? "No crosshairs here yet. Press UPLOAD to be the first." : "");
		SettleBelowWall();
	}

	internal static string CrosshairFacts(CrosshairConfig config)
	{
		int num = (config.ResolvedArmTop ? 1 : 0) + (config.ResolvedArmBottom ? 1 : 0) + (config.ResolvedArmLeft ? 1 : 0) + (config.ResolvedArmRight ? 1 : 0);
		List<string> list = new List<string>();
		if (num == 4)
		{
			list.Add("Cross");
		}
		else if (num == 3)
		{
			list.Add("T");
		}
		else if (num > 0)
		{
			list.Add((num == 1) ? "1 arm" : $"{num} arms");
		}
		if (config.ResolvedShowCircle)
		{
			list.Add((list.Count == 0) ? "Ring" : "ring");
		}
		if (config.ResolvedCentreDot)
		{
			list.Add((list.Count == 0) ? "Dot" : "dot");
		}
		if (list.Count == 0)
		{
			return "";
		}
		string text = string.Join(" + ", list);
		if (num <= 0)
		{
			if (!config.ResolvedShowCircle)
			{
				return text + " · " + Px(config.ResolvedDotSize);
			}
			return text + " · " + Px(config.ResolvedCircleRadius);
		}
		return $"{text} · {Px(config.ResolvedSize)} · gap {Px(config.ResolvedGap)}";
		static string Px(float v)
		{
			return v.ToString("0.#", CultureInfo.InvariantCulture) + " px";
		}
	}

	private void OfferReport(LibraryCard card, string? id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return;
		}
		card.OfferReport(() =>
		{
			if (!LoopbackSignIn.TryOpenBrowser(AppInfo.PresetReportUrl(id)))
			{
				SetStatus("Couldn't open the report page. Try again.");
			}
		});
	}

	private static string LongerOf(string a, string b)
	{
		if (b.Length <= a.Length)
		{
			return a;
		}
		return b;
	}

	private static string LongestSummary(IReadOnlyList<Fact> facts)
	{
		string text = "";
		foreach (Fact fact in facts)
		{
			if (fact.Summary.Length > text.Length)
			{
				text = fact.Summary;
			}
		}
		return text;
	}

	private void RebuildFactWall(IReadOnlyList<Fact> facts, string empty)
	{
		(int[] Lefts, int[] Widths, int CardH) tuple = WallGeometry(2, facts.Select((Fact fact2) => fact2.Blurb), LongestSummary(facts));
		int[] item = tuple.Lefts;
		int[] item2 = tuple.Widths;
		int item3 = tuple.CardH;
		int num = item3 + Scaled(16);
		for (int num2 = 0; num2 < facts.Count; num2++)
		{
			Fact fact = facts[num2];
			LibraryCard libraryCard = new LibraryCard
			{
				Caption = fact.Name,
				Author = ShortAuthor(fact.Author),
				Subtitle = fact.Blurb,
				Kind = fact.Glyph,
				Footer = fact.Summary,
				Size = new Size(item2[num2 % 2], item3),
				Location = new Point(item[num2 % 2], Scaled(0) + num2 / 2 * num)
			};
			libraryCard.Click += (object? _, EventArgs _) =>
			{
				fact.Apply();
			};
			OfferReport(libraryCard, fact.CommunityId);
			_chips.Add(libraryCard);
			_wall.Controls.Add(libraryCard);
		}
		int num3 = facts.Count;
		int num4 = (num3 + 2 - 1) / 2;
		_wall.Height = Math.Max(Scaled(120), Scaled(0) * 2 + Math.Max(0, num4 * num - Scaled(16)));
		_status.Location = new Point(Scaled(16), _wall.Bottom + Scaled(14));
		SetStatus((num3 == 0) ? empty : "");
		SettleBelowWall();
	}

	private void ApplyMode(DisplayLibraryEntry entry)
	{
		if (_onApplyMode == null)
		{
			SetStatus(entry.Name + " — paste its code on the Resolution tab to use it.");
		}
		else
		{
			SetStatus(_onApplyMode(entry.Mode));
		}
	}

	private void ApplyConfig(ConfigLibraryEntry entry)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		DisplayPreset look = entry.Look;
		if ((object)look != null)
		{
			_onApply(look).AddTo(list, list2);
		}
		CrosshairConfig crosshair = entry.Crosshair;
		if (crosshair != null)
		{
			if (_onApplyCrosshair == null)
			{
				list2.Add("Its crosshair needs the Crosshair tab — paste the code there.");
			}
			else
			{
				_onApplyCrosshair(crosshair);
				list.Add("crosshair");
			}
		}
		MonitorCode? mode = entry.Mode;
		if (mode.HasValue)
		{
			MonitorCode valueOrDefault = mode.GetValueOrDefault();
			list2.Add((_onApplyMode == null) ? "Its resolution needs the Resolution tab — paste the code there." : _onApplyMode(valueOrDefault));
		}
		List<string> list3 = new List<string>();
		if (list.Count > 0)
		{
			list3.Add(entry.Name + ": " + string.Join(" and ", list) + " applied.");
		}
		else
		{
			list3.Add(entry.Name + ":");
		}
		list3.AddRange(list2);
		SetStatus(string.Join("  ", list3));
	}

	private void ApplyCrosshair(CrosshairLibraryEntry entry)
	{
		if (_onApplyCrosshair == null)
		{
			SetStatus(entry.Name + " — copy its code from the Crosshair tab to use it.");
			return;
		}
		_onApplyCrosshair(entry.Crosshair);
		SetStatus(entry.Name + " is on. Change it any way you like on the Crosshair tab.");
	}

	private void OpenUploadPage()
	{
		if (LoopbackSignIn.TryOpenBrowser("https://plexusx.lol/dashboard?tab=presets"))
		{
			SetStatus("Opened your browser - sign in and the Presets tab is already there.");
		}
		else
		{
			SetStatus("Could not open a browser. Go to https://plexusx.lol/dashboard?tab=presets");
		}
	}

	private static string ShortAuthor(string author)
	{
		string text = (author ?? "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		int num = text.IndexOf(' ');
		if (num > 0)
		{
			text = text.Substring(0, num);
		}
		if (text.Length <= 16)
		{
			return text;
		}
		return text.Substring(0, 16);
	}

	private static int TallestBlurb(int width, IEnumerable<string> blurbs)
	{
		return blurbs.Select((string text) => LibraryCard.DescriptionHeight(text, width)).DefaultIfEmpty(Tokens.Scale(20)).Max();
	}

	private void Apply(LibraryPreset entry)
	{
		LookNote lookNote = _onApply(entry.Look);
		if (lookNote.Refused)
		{
			SetStatus(entry.Name + ": " + lookNote.Text);
			return;
		}
		bool flag = !string.IsNullOrEmpty(lookNote.Text);
		SetStatus(flag ? (entry.Name + " applied. " + lookNote.Text) : (entry.Name + " applied. Save it below to keep it, or copy its code to share it."));
		_previousApply = _currentApply;
		_currentApply = entry;
		MarkApplied(entry);
		ShowAppliedToast(entry);
		_status.Visible = flag;
		RepositionToast();
	}

	private void MarkApplied(LibraryPreset? entry)
	{
		foreach (LibraryCard chip in _chips)
		{
			chip.Active = (object)entry != null && string.Equals(chip.Caption, entry.Name, StringComparison.Ordinal);
		}
	}

	private void ShowAppliedToast(LibraryPreset entry)
	{
		_toast.Show($"Applied · {entry.Name} · Saturation {entry.Look.Saturation}%, Vibrance {entry.Look.Vibrance}%");
		RepositionToast();
	}

	private void RepositionToast()
	{
		FollowTheMonitor();
		int contentW = ContentW;
		int num = Math.Min(contentW, _toast.PreferredWidth);
		int x = Scaled(16) + (contentW - num) / 2;
		int num2 = WindowFloor() - _toast.Height - Scaled(16);
		if (_status.Visible && _toast.Visible && num2 < _status.Bottom && num2 + _toast.Height > _status.Top)
		{
			num2 = _status.Top - Scaled(8) - _toast.Height;
		}
		_toast.SetBounds(x, num2, num, _toast.Height);
		_toast.ReassertOnTop();
	}

	private int WindowFloor()
	{
		Form form = FindForm();
		if (form == null || !IsHandleCreated)
		{
			return ClientSize.Height;
		}
		return PointToClient(form.PointToScreen(new Point(0, form.ClientSize.Height))).Y;
	}

	internal void SetCatalogue(IReadOnlyList<LibraryPreset> looks, IReadOnlyList<CrosshairLibraryEntry> crosshairs, IReadOnlyList<DisplayLibraryEntry> displays, IReadOnlyList<ConfigLibraryEntry> configs)
	{
		if (IsDisposed || Disposing)
		{
			return;
		}
		_looks = looks;
		_crosshairs = crosshairs;
		_displays = displays;
		_configs = configs;
		_fetchFailed = false;
		_gamePicker.SetItems(GameLabels());
		_gamePicker.SelectedIndex = 0;
		_game = "All";
		RebuildWall();
	}

	internal void ReadLooksAgainIfTheCodesMoved()
	{
		if (IsDisposed || Disposing)
		{
			return;
		}
		_looks = PresetLibrary.All;
		_crosshairs = CrosshairLibrary.All;
		_displays = DisplayLibrary.All;
		_configs = ConfigLibrary.All;
		RebuildWall();
	}

	internal void SetFetchFailed()
	{
		if (!IsDisposed && !Disposing)
		{
			_fetchFailed = true;
			if (FetchFailedWithoutCache)
			{
				SetStatus("");
				SettleBelowWall();
			}
		}
	}

	private void SetStatus(string text)
	{
		bool fetchFailedWithoutCache = FetchFailedWithoutCache;
		if (fetchFailedWithoutCache)
		{
			text = "Can't reach the Library right now. Check your connection.";
		}
		_retry.Visible = fetchFailedWithoutCache;
		_status.Text = text;
		_status.Visible = text.Length > 0;
	}

	internal void TestPressCard(string caption)
	{
		_chips.Concat(_crosshairChips).First((LibraryCard libraryCard) => libraryCard.Caption == caption).PressForTest();
	}

	internal void TestSearchFor(string text)
	{
		_search.Inner.Text = text;
	}

	internal void TestSelectKind(string kind)
	{
		if (Array.IndexOf(Kinds, kind) < 0)
		{
			throw new ArgumentOutOfRangeException("kind", kind, "no such kind");
		}
		SelectKind(kind);
	}

	internal void TestApplyModeByName(string name)
	{
		DisplayLibraryEntry entry = _displays.FirstOrDefault((DisplayLibraryEntry displayLibraryEntry) => string.Equals(displayLibraryEntry.Name, name, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentOutOfRangeException("name", name, "no such resolution");
		ApplyMode(entry);
	}

	internal void TestApplyConfigByName(string name)
	{
		ConfigLibraryEntry entry = _configs.FirstOrDefault((ConfigLibraryEntry configLibraryEntry) => string.Equals(configLibraryEntry.Name, name, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentOutOfRangeException("name", name, "no such config");
		ApplyConfig(entry);
	}

	internal void TestApplyCrosshairByName(string name)
	{
		CrosshairLibraryEntry entry = _crosshairs.FirstOrDefault((CrosshairLibraryEntry crosshairLibraryEntry) => string.Equals(crosshairLibraryEntry.Name, name, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentOutOfRangeException("name", name, "no such crosshair");
		ApplyCrosshair(entry);
	}

	internal void TestSelectGame(string game)
	{
		int num = GameNames().ToList().FindIndex((string g) => string.Equals(g, game, StringComparison.OrdinalIgnoreCase));
		if (num < 0)
		{
			throw new ArgumentOutOfRangeException("game", game, "no such filter");
		}
		_gamePicker.SelectedIndex = num;
	}

	internal void TestApplyByName(string name)
	{
		LibraryPreset entry = _looks.FirstOrDefault((LibraryPreset p) => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentOutOfRangeException("name", name, "no such preset");
		Apply(entry);
	}
}