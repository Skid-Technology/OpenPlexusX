using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using VibranceHud.Audio;
using VibranceHud.Controls;
using VibranceHud.Crosshair;
using VibranceHud.Customize;
using VibranceHud.Design;
using VibranceHud.Display;
using VibranceHud.Games;
using VibranceHud.Monitors;
using VibranceHud.Pages;
using VibranceHud.Presets;
using VibranceHud.Theming;
using VibranceHud.Zoom;

namespace VibranceHud;

public sealed class MainWindow : Form
{
	private struct LastInputInfo
	{
		public uint cbSize;
		public uint dwTime;
	}

	private const int DwmaCornerPreference = 33;
	private const int DwmcpRound = 2;
	private const int WM_NCLBUTTONDOWN = 161;
	private const int HTCAPTION = 2;
	private const int TitleH = 40;
	private const int NavW = 210;
	private const int CaptionW = 46;
	private const int CaptionGlyphFromRight = 22;
	private const int CaptionPitch = 46;
	private const int LogoH = 21;
	private const int LogoW = 102;
	private const int LogoX = 14;
	private const int LogoY = 8;
	private const int NavRailW = 76;
	private const int PageLeft = 76;
	private const int ShellBorder = 1;
	private const int ShellInset = 1;

	private float _navOpen;
	private bool _navWanted;

	private readonly VibranceEngine _engine;
	private readonly AppSettings _settings;
	private readonly SettingsStore _store;
	private readonly Action<string> _onThemeChanged;

	private readonly SwitchableBackdrop _field = new SwitchableBackdrop();

	internal const int TickIntervalMs = 47;
	private readonly Timer _timer;
	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private double _lastSeconds;

	private readonly GlowPanel _titleBar;
	private readonly GlowPanel _nav;
	private readonly Panel _contentHost;
	private readonly GlassScrollBar _scrollBar;
	private Control? _currentPage;

	private readonly VibrancePage _vibrancePage;
	private readonly SettingsPage _settingsPage;
	private readonly CrosshairPage _crosshairPage;
	private readonly ProfilesPage _profilesPage;
	private readonly CrosshairService _crosshair;

	private readonly NavButton _navVibrance;
	private readonly NavButton _navCrosshair;
	private readonly NavButton _navProfiles;
	private readonly NavButton _navSettings;
	private readonly NavButton _navMonitor;
	private readonly NavButton _navPanel;
	private readonly NavButton _navCustomize;
	private readonly NavButton _navLibrary;
	private readonly NavButton _navDarkBoost;
	private readonly NavButton _navMore;

	private ContextMenuStrip? _moreMenu;

	private readonly LibraryPage _libraryPage;
	private readonly DarkBoostPage _darkBoostPage;
	private readonly DebouncedAction _saveDarkBoost;
	private readonly MonitorHardwarePage _panelPage;
	private readonly MonitorPage _monitorPage;
	private readonly CustomizePage _customizePage;
	private readonly GameSelection _selection;
	private readonly AudioEdgeService? _audio;
	private readonly ProfileEngineCoordinator? _profileCoordinator;

	[ThreadStatic]
	internal static Func<Rectangle>? WorkingAreaForTest;

	internal Func<TimeSpan>? SinceLastInputForTest;

	private bool _active = true;

	private const int WM_CHANGEUISTATE = 295;
	private const int UIS_SET = 1;
	private const int UISF_HIDEFOCUS = 1;
	private const int UISF_HIDEACCEL = 2;

	private int _scrollTicks;
	private bool _userInteracting;
	private readonly List<Timer> _scrollIdleTimers = new List<Timer>();
	private const int RailFootH = 31;
	private const int RailFootDrop = 6;

	private readonly SignalToolTip _navTips = new SignalToolTip
	{
		InitialDelay = 420
	};

	private bool _cornersAreTheCompositors;
	private bool _navPinned;

	private LogoBox? _logo;
	private VibranceHud.Controls.CaptionButton? _closeButton;
	private VibranceHud.Controls.CaptionButton? _minButton;

	private readonly List<Panel> _dividers = new List<Panel>();
	private Label? _navVersion;

	private double _lastScrollSeconds = double.NegativeInfinity;
	private const double ScrollQuietSeconds = 0.15;

	private const int WM_ENTERSIZEMOVE = 561;
	private const int WM_EXITSIZEMOVE = 562;
	private bool _movingOrSizing;

	private const int WM_SYSCOMMAND = 274;
	private const int SC_CLOSE = 61536;

	public Action? PhoneWindowOpener
	{
		set => _vibrancePage.PhoneWindowOpener = value;
	}

	internal bool AnimationTimerRunning => _timer?.Enabled ?? false;
	internal double AnimationClockStampSeconds => _lastSeconds;
	internal bool IsActiveForTest => _active;
	internal GlowPage? CurrentPageForTest => _currentPage as GlowPage;

	private NavButton[] NavOrder => new NavButton[8]
	{
		_navVibrance, _navLibrary, _navMonitor, _navPanel,
		_navCustomize, _navCrosshair, _navProfiles, _navDarkBoost
	};

	internal string CornerRouteForTest => _cornersAreTheCompositors ? "dwm" : "square";
	internal Rectangle ContentHostBoundsForTest => _contentHost.Bounds;

	internal string ShellLayoutForTest => $"nav {_nav.Bounds} z{Controls.GetChildIndex(_nav)}  host {_contentHost.Bounds} z{Controls.GetChildIndex(_contentHost)}  rows {string.Join(",", from navButton in NavOrder.Where(n => n.Visible).Take(2) select navButton.Bounds.ToString())}";

	internal int NavRailWidthForTest => _nav.Width;
	internal float NavLabelOpacityForTest => NavOrder.First().LabelOpacity;

	internal IEnumerable<(string Label, string Tip)> NavTipsForTest =>
		NavOrder.Select(n => (n.Text, _navTips.GetToolTip(n) ?? string.Empty));

	internal ProfilesPage ProfilesPageForTest => _profilesPage;
	internal VibrancePage VibrancePageForStatus => _vibrancePage;
	internal DarkBoostPage DarkBoostPageForTest => _darkBoostPage;

	internal IReadOnlyList<string> NavLabelsForTest =>
		NavOrder.Select(n => n.Text).Concat(new[] { _navSettings.Text }).ToList();

	internal IReadOnlyList<string> RailLabelsForTest =>
		(from navButton in NavOrder where navButton.Visible select navButton.Text).ToList();

	internal NavButton MoreButtonForTest => _navMore;
	internal bool MoreMenuOpenForTest => _moreMenu?.Visible ?? false;
	internal ContextMenuStrip? MoreMenuForTest => _moreMenu;

	internal IReadOnlyList<string> MoreMenuItemsForTest
	{
		get
		{
			using ContextMenuStrip menu = BuildMoreMenu();
			return (from ToolStripItem i in menu.Items select i.Text ?? "").ToList();
		}
	}

	internal Action? OfflineNoticeRetry { get; set; }

	internal bool HasPendingOfflineNoticeForTest => false;

	public event Action<string?>? PresetApplied;

	internal static string? DiscordCardLine(AppSettings settings)
	{
		if (string.IsNullOrEmpty(settings.ColourPresetGame) || string.IsNullOrEmpty(settings.ColourPresetName))
			return null;
		return settings.ColourPresetGame + " · " + settings.ColourPresetName;
	}

	[DllImport("user32.dll")] private static extern bool ReleaseCapture();
	[DllImport("user32.dll")] private static extern nint SendMessage(nint hWnd, int msg, int wParam, int lParam);
	[DllImport("user32.dll")] private static extern nint GetForegroundWindow();
	[DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInputInfo info);
	[DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hWnd);
	[DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

	public MainWindow(
		VibranceEngine engine,
		AppSettings settings,
		SettingsStore store,
		AudioEdgeService? audio,
		Action<string> onThemeChanged,
		CustomThemeService? custom = null,
		CrosshairService? crosshair = null,
		ProfileEngineCoordinator? profileCoordinator = null,
		Func<uint, uint, bool>? onHotkeyChanged = null,
		Func<uint, uint, bool, bool>? onMainHotkeyChanged = null,
		GameSelection? selection = null,
		Action<bool>? onLowLatencyChanged = null,
		IReadOnlyList<MonitorCapability>? monitorCapabilities = null,
		Func<bool>? onPresetBindingsChanged = null,
		Action? onMonitorColourChanged = null,
		Func<uint, uint, bool>? onScreenshotHotkeyChanged = null,
		Action? onDisplaySliderChanged = null,
		CustomModeRegistrar? customModes = null,
		ModeApplier? modeApply = null,
		Func<uint, uint, bool>? onNightVisionHotkeyChanged = null,
		Action? onBoostNextKeyChanged = null,
		ZoomHooks? zoomHooks = null,
		Action<bool>? onCompetitiveModeChanged = null)
	{
		MainWindow mainWindow = this;
		_crosshair = crosshair ?? new CrosshairService();
		_profileCoordinator = profileCoordinator;
		_engine = engine;
		_settings = settings;
		_store = store;
		_selection = selection ?? new GameSelection(settings, store);
		_audio = audio;
		_onThemeChanged = onThemeChanged;

		FormBorderStyle = FormBorderStyle.None;
		Text = "PlexusX";
		Icon = AppIcon.Value;
		BackColor = Theme.Background;
		SetStyle(ControlStyles.ResizeRedraw, true);
		Tokens.Dpi = DeviceDpi;
		ClientSize = DesignSize();
		LockToDesignSize();
		Opacity = Math.Clamp(settings.OpacityPercent, 50, 100) / 100.0;
		Font = Fonts.Label;
		DoubleBuffered = true;
		KeyPreview = true;

		Rectangle bounds = WindowBounds.ClampToVisible(
			new Rectangle(settings.WindowX, settings.WindowY, Width, Height),
			Screen.AllScreens.Select(s => s.WorkingArea));
		if (bounds.IsEmpty)
		{
			StartPosition = FormStartPosition.CenterScreen;
		}
		else
		{
			StartPosition = FormStartPosition.Manual;
			Bounds = bounds;
			LockToDesignSize();
		}

		_field.Use(BackdropLooks.Parse(_settings.BackdropLookName));
		Glass.Style = SurfaceStyles.Parse(_settings.SurfaceStyleName);
		Glass.ActiveChrome = Chromes.Parse(_settings.ChromeName);
		_field.Resize(ClientSize.Width, ClientSize.Height);
		AppBackground.Resize(ClientSize.Width, ClientSize.Height);

		int titleH = Tokens.Scale(40);
		Tokens.Scale(210);
		int navRailW = Tokens.Scale(76);

		_titleBar = new GlowPanel
		{
			Field = _field,
			Scrim = 110,
			Location = new Point(1, 1),
			Size = new Size(ClientSize.Width - 2, titleH - 1),
			Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right)
		};
		_titleBar.MouseDown += DragWindow;

		LogoBox logoBox = new LogoBox
		{
			Image = BrandAssets.HorizontalLogo(Theme.IsLight),
			Location = new Point(Tokens.Scale(14), Tokens.Scale(8)),
			Size = new Size(Tokens.Scale(102), Tokens.Scale(21))
		};
		logoBox.MouseDown += DragWindow;

		int closeX = CaptionCloseX();
		VibranceHud.Controls.CaptionButton closeButton = TitleButton(VibranceHud.Controls.CaptionButton.Kind.Close, closeX);
		closeButton.Click += (s, e2) => { mainWindow.SaveWindowBounds(); mainWindow.Hide(); };

		VibranceHud.Controls.CaptionButton minButton = TitleButton(VibranceHud.Controls.CaptionButton.Kind.Minimise, closeX - Tokens.Scale(46));
		minButton.Click += (s, e2) => { mainWindow.WindowState = FormWindowState.Minimized; };

		_titleBar.Controls.AddRange(new Control[] { logoBox, closeButton, minButton });
		_navTips.SetToolTip(minButton, "Minimise");
		_navTips.SetToolTip(closeButton, "Close");

		_logo = logoBox;
		_closeButton = closeButton;
		_minButton = minButton;
		Controls.Add(_titleBar);

		_vibrancePage = new VibrancePage(_engine, _settings, _store);
		_vibrancePage.PresetApplied += n => { mainWindow.PresetApplied?.Invoke(n); };
		_vibrancePage.HotkeyChanged += (mask, vk) => onHotkeyChanged?.Invoke(mask, vk) ?? true;
		_vibrancePage.MainHotkeyChanged += (mask, vk, en) => onMainHotkeyChanged?.Invoke(mask, vk, en) ?? true;
		_vibrancePage.ScreenshotHotkeyChanged += (mask, vk) => onScreenshotHotkeyChanged?.Invoke(mask, vk) ?? true;
		_vibrancePage.NightVisionHotkeyChanged += (mask, vk) => onNightVisionHotkeyChanged?.Invoke(mask, vk) ?? false;
		_vibrancePage.PresetBindingsChanged += () => onPresetBindingsChanged?.Invoke() ?? true;
		_vibrancePage.SliderMovedByUser += () => { onDisplaySliderChanged?.Invoke(); };

		_saveDarkBoost = new DebouncedAction(() => { mainWindow._store.Save(mainWindow._settings); }, 300);

		_darkBoostPage = new DarkBoostPage(_engine, _settings, _saveDarkBoost.Trigger);
		_darkBoostPage.KeyChanged += (mask, vk) => onNightVisionHotkeyChanged?.Invoke(mask, vk) ?? false;
		_darkBoostPage.NextKeyChanged += () => { onBoostNextKeyChanged?.Invoke(); };

		_settingsPage = new SettingsPage(
			_settings, _store, _engine, _audio,
			onShowDisplay: ShowVibrance,
			onShowLegal: ShowLegal,
			onLowLatencyChanged: onLowLatencyChanged,
			installerHash: null,
			onShowOnDiscordChanged: () => { mainWindow.PresetApplied?.Invoke(DiscordCardLine(mainWindow._settings)); },
			onCompetitiveModeChanged: onCompetitiveModeChanged);

		_crosshairPage = new CrosshairPage(_settings, _store, _crosshair);

		_darkBoostPage.AttachZoom(
			_settings,
			() => { mainWindow._store.Save(mainWindow._settings); },
			() => true,
			zoomHooks ?? new ZoomHooks((m, v) => true, () => "", () => { }));

		_profilesPage = new ProfilesPage(_settings, _store, _selection, _engine,
			look => { mainWindow._vibrancePage.ApplyStartingLook(look); });

		_monitorPage = new MonitorPage(_settings, _store, _selection, customModes, modeApply);
		_panelPage = new MonitorHardwarePage(_settings, _store, monitorCapabilities, null, onMonitorColourChanged, null);

		_libraryPage = new LibraryPage(
			_settings, _store,
			preset => mainWindow._vibrancePage.ApplyLibraryPreset(preset),
			() => mainWindow._engine.DriverState == VibranceDriverState.Available,
			incoming => { mainWindow._crosshairPage.ApplyLibraryCrosshair(incoming); },
			null,
			null,
			mode => mainWindow._monitorPage.ApplyLibraryMode(mode),
			null,
			Array.Empty<ConfigLibraryEntry>(),
			OpenPage);

		_vibrancePage.CrosshairForSharing = () => mainWindow._crosshairPage.CurrentCrosshair;

		_customizePage = new CustomizePage(_settings, _store, _onThemeChanged, RefreshBackdrop, SetWindowOpacity, custom, SetBackdropLook);

		GlowPage[] pages =
		{
			_vibrancePage, _settingsPage, _crosshairPage,
			_profilesPage, _monitorPage, _panelPage, _customizePage,
			_libraryPage, _darkBoostPage
		};
		foreach (GlowPage page in pages)
			AttachField(page);

		_nav = new GlowPanel
		{
			Field = _field,
			Scrim = 0,
			Location = new Point(1, titleH),
			Size = new Size(navRailW - 1, ClientSize.Height - titleH - 1),
			Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left)
		};

		_navVibrance  = MakeNav("Display", 0);
		_navMonitor   = MakeNav("Resolution", 7);
		_navPanel     = MakeNav("Monitor", 9);
		_navCrosshair = MakeNav("Crosshair", 5);
		_navProfiles  = MakeNav("Profiles", 1);
		_navSettings  = MakeNav("Settings", 6);
		_navCustomize = MakeNav("Customize", 8);
		_navLibrary   = MakeNav("Library", 10);
		_navDarkBoost = MakeNav("Dark Boost & Zoom", 11);
		_navMore      = MakeNav("More", 6);

		_navVibrance.Click  += (s, e2) => mainWindow.ShowVibrance();
		_navMonitor.Click   += (s, e2) => mainWindow.Select(mainWindow._navMonitor, mainWindow._monitorPage);
		_navPanel.Click     += (s, e2) => mainWindow.Select(mainWindow._navPanel, mainWindow._panelPage);
		_navCustomize.Click += (s, e2) => mainWindow.Select(mainWindow._navCustomize, mainWindow._customizePage);
		_navLibrary.Click   += (s, e2) => mainWindow.Select(mainWindow._navLibrary, mainWindow._libraryPage);
		_navCrosshair.Click += (s, e2) => mainWindow.Select(mainWindow._navCrosshair, mainWindow._crosshairPage);
		_navProfiles.Click  += (s, e2) =>
		{
			mainWindow.Select(mainWindow._navProfiles, mainWindow._profilesPage);
			mainWindow._profilesPage.Refresh();
		};
		_navSettings.Click += (s, e2) => mainWindow.Select(mainWindow._navSettings, mainWindow._settingsPage);
		_navDarkBoost.Click += (s, e2) =>
		{
			mainWindow._darkBoostPage.ReloadOffer();
			mainWindow.Select(mainWindow._navDarkBoost, mainWindow._darkBoostPage);
		};
		_navMore.Click += (s, e2) => mainWindow.ToggleMoreMenu();

		_nav.Controls.AddRange(NavOrder);
		_nav.Controls.Add(_navMore);
		RovingFocus.Attach(
			NavOrder.Append(_navMore),
			control => control is NavButton n && n.Active,
			_ => false);

		Label versionLabel = _navVersion = new Label
		{
			Text = AppInfo.VersionText,
			ForeColor = FaintestGrey(),
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(Tokens.Scale(22), _nav.Height - Tokens.Scale(30)),
			AutoSize = true,
			Anchor = (AnchorStyles.Bottom | AnchorStyles.Left)
		};
		versionLabel.MouseDown += DragWindow;
		_nav.Controls.Add(versionLabel);
		Controls.Add(_nav);

		int hostLeft = Tokens.Scale(76);
		_contentHost = new Panel
		{
			Location = new Point(hostLeft, titleH),
			Size = new Size(ClientSize.Width - hostLeft - 1, ClientSize.Height - titleH - 1),
			BackColor = Theme.Background,
			AutoScroll = false,
			Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right)
		};
		Controls.Add(_contentHost);

		_scrollBar = new GlassScrollBar { Visible = false };
		_scrollBar.Scrolled += (s, e) => { (mainWindow._currentPage as GlowPage)?.ScrollTo(mainWindow._scrollBar.Value); };
		WheelRouter.CurrentPage = () => mainWindow._currentPage as GlowPage;
		Controls.Add(_scrollBar);
		_scrollBar.BringToFront();
		_nav.BringToFront();
		foreach (Panel divider in _dividers)
			divider.BringToFront();

		Resize += (s, e2) =>
		{
			mainWindow._field.Resize(mainWindow.ClientSize.Width, mainWindow.ClientSize.Height);
			AppBackground.Resize(mainWindow.ClientSize.Width, mainWindow.ClientSize.Height);
			mainWindow.LayoutNav();
		};

		_timer = new Timer { Interval = 47 };
		_timer.Tick += OnAnimationTick;
		SyncAnimationClock();
		AppBackground.DriveMotionFrom(this, RepaintAnimatedSurfaces);
		HookInteractionPauses();
		ClearStaleMaximizedState();

		FormClosing += (s, e2) => { mainWindow.SaveWindowBounds(); };
		Shown += (s, e2) => { mainWindow.LayoutNav(); mainWindow.ApplyNavRail(); };

		ShowVibrance();
		LayoutNav();
		ApplyNavRail();
	}

	private static Size DesignSize() => new Size(Tokens.Scale(1040), Tokens.Scale(680));

	internal string CaptionTipForTest(Control button) => _navTips.GetToolTip(button) ?? string.Empty;

	private void LockToDesignSize()
	{
		Rectangle workingArea = WorkingAreaForTest?.Invoke() ?? Screen.FromRectangle(Bounds).WorkingArea;
		Size size = WindowBounds.PinnedSize(DesignSize(), workingArea);
		MaximumSize = Size.Empty;
		MinimumSize = Size.Empty;
		Size = size;
		MinimumSize = size;
		MaximumSize = size;
		MaximizeBox = false;
	}

	private void SaveWindowBounds()
	{
		if (IsDisposed) return;
		Rectangle r = (WindowState == FormWindowState.Normal) ? Bounds : RestoreBounds;
		if (r.Width <= 0 || r.Height <= 0) return;
		_settings.WindowX = r.X;
		_settings.WindowY = r.Y;
		_settings.WindowWidth = r.Width;
		_settings.WindowHeight = r.Height;
		_settings.WindowMaximized = WindowState == FormWindowState.Maximized;
		try { _store.Save(_settings); } catch { }
	}

	protected override void OnDpiChanged(DpiChangedEventArgs e)
	{
		base.OnDpiChanged(e);
		Tokens.Dpi = e.DeviceDpiNew;
		Fonts.Rebuild();
		RelayoutForDpi();
	}

	private void RelayoutForDpi()
	{
		if (IsDisposed || !IsHandleCreated) return;
		int titleH = Tokens.Scale(40);
		Tokens.Scale(210);
		SuspendLayout();
		LockToDesignSize();
		Tokens.Scale(76);

		_titleBar.Location = new Point(1, 1);
		_titleBar.Size = new Size(ClientSize.Width - 2, titleH - 1);
		PlaceTitleBarContents();

		_nav.Location = new Point(1, titleH);
		_nav.Height = ClientSize.Height - titleH - 1;
		LayoutNav();
		ApplyNavRail();

		int hostLeft = Tokens.Scale(76);
		_contentHost.Location = new Point(hostLeft, titleH);
		_contentHost.Size = new Size(ClientSize.Width - hostLeft - 1, ClientSize.Height - titleH - 1);

		GlowPage[] pages =
		{
			_vibrancePage, _settingsPage, _crosshairPage,
			_profilesPage, _monitorPage, _panelPage, _customizePage,
			_libraryPage, _darkBoostPage
		};
		foreach (var page in pages) page?.ApplyDpi(Tokens.Dpi);
		if (_currentPage is GlowPage p) AttachField(p);

		ResumeLayout(true);
		Invalidate(true);
	}

	public void SetBackdropLook(BackdropLook look)
	{
		_field.Use(look);
		_field.Resize(ClientSize.Width, ClientSize.Height);
		_settings.BackdropLookName = BackdropLooks.Name(look);
		_store.Save(_settings);
		Invalidate(true);
	}

	public void SetSurfaceStyle(SurfaceStyle style)
	{
		Glass.Style = style;
		_settings.SurfaceStyleName = SurfaceStyles.Name(style);
		_store.Save(_settings);
		Invalidate(true);
	}

	private void AttachField(GlowPage page)
	{
		page.Field = _field;
		page.FieldOffset = new Point(Tokens.Scale(76), Tokens.Scale(40));
	}

	private static TimeSpan SinceLastInput()
	{
		LastInputInfo info = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
		if (!GetLastInputInfo(ref info)) return TimeSpan.Zero;
		return TimeSpan.FromMilliseconds((uint)(Environment.TickCount64 - info.dwTime));
	}

	private void SyncAnimationClock()
	{
		if (_timer == null) return;
		bool run = AnimationClockPolicy.ShouldRun(
			Visible, WindowState == FormWindowState.Minimized,
			_movingOrSizing, IsDisposed || Disposing, _active);
		if (run == _timer.Enabled) return;
		if (run)
		{
			_lastSeconds = _clock.Elapsed.TotalSeconds;
			_timer.Start();
		}
		else _timer.Stop();
	}

	protected override void OnDeactivate(EventArgs e)
	{
		base.OnDeactivate(e);
		_active = false;
		CloseNavRailNobodyIsHolding();
		SyncAnimationClock();
	}

	protected override void OnActivated(EventArgs e)
	{
		base.OnActivated(e);
		_active = true;
		SyncAnimationClock();
	}

	internal void NoteActivationForTest(bool active)
	{
		if (active) OnActivated(EventArgs.Empty);
		else OnDeactivate(EventArgs.Empty);
	}

	private void CloseNavRailNobodyIsHolding()
	{
		if (_navOpen > 0f && !_navPinned)
		{
			_navOpen = 0f;
			ApplyNavRail();
		}
	}

	protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); SyncAnimationClock(); }

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		try { SendMessage(Handle, 295, 196609, 0); }
		catch (EntryPointNotFoundException) { }
		catch (DllNotFoundException) { }
	}

	private void OnAnimationTick(object? sender, EventArgs e)
	{
		if (IsDisposed || Disposing || !IsHandleCreated) return;
		double totalSeconds = _clock.Elapsed.TotalSeconds;
		if (!BackgroundFramePump.InFront(this) || !Visible || WindowState == FormWindowState.Minimized)
		{
			CloseNavRailNobodyIsHolding();
			_lastSeconds = totalSeconds;
			return;
		}

		int interval = AnimationClockPolicy.IntervalFor(
			SinceLastInputForTest?.Invoke() ?? SinceLastInput(),
			AppBackground.HasMotion);
		if (_timer.Interval != interval) _timer.Interval = interval;

		double dt = Math.Min(totalSeconds - _lastSeconds, 0.1);
		UpdateNavRail((float)dt);

		if (_userInteracting || _movingOrSizing)
		{
			_lastSeconds = totalSeconds;
			return;
		}

		bool scrolling = DragTracker.AnyScrollBarDragging
			|| totalSeconds - _lastScrollSeconds < ScrollQuietSeconds;
		if (DragTracker.AnyDragging)
		{
			_lastSeconds = totalSeconds;
			return;
		}

		_field.Update(dt);
		_lastSeconds = totalSeconds;

		if (_field.Moves)
		{
			if (scrolling && (++_scrollTicks % 3 != 0))
				RepaintPageOnly();
			else
				RepaintAnimatedSurfaces();
		}
	}

	internal void RepaintAnimatedSurfaces()
	{
		_titleBar.Invalidate(true);
		_nav.Invalidate(true);
		if (_currentPage is GlowPage glowPage) glowPage.InvalidateBackdropOnly();
		else _currentPage?.Invalidate(true);
	}

	internal void RepaintPageOnly()
	{
		if (_currentPage is GlowPage glowPage) glowPage.InvalidateBackdropOnly();
		else _currentPage?.Invalidate(true);
	}

	internal void SelectLibraryCrosshairsForTest() => _libraryPage.TestSelectKind("Crosshairs");
	internal void ShowPageForTest(string nav) => OpenPage(nav);

	private void OpenPage(string nav)
	{
		switch (nav)
		{
			case "Display": _navVibrance.PerformClick(); break;
			case "Crosshair": _navCrosshair.PerformClick(); break;
			case "Zoom": _navDarkBoost.PerformClick(); break;
			case "Profiles": _navProfiles.PerformClick(); break;
			case "Settings": _navSettings.PerformClick(); break;
			case "Dark Boost":
			case "Dark Boost & Zoom": _navDarkBoost.PerformClick(); break;
			case "Customize": _navCustomize.PerformClick(); break;
			case "Library": _navLibrary.PerformClick(); break;
			case "Resolution": _navMonitor.PerformClick(); break;
			case "Monitor": _navPanel.PerformClick(); break;
			default: throw new ArgumentOutOfRangeException(nameof(nav), nav, "no such nav page");
		}
	}

	private void HookInteractionPauses()
	{
		MouseDown += (s, e2) => _userInteracting = true;
		MouseUp += (s, e2) => _userInteracting = false;
		if (Controls.Count > 0) HookScrollEvents(this);
	}

	private void HookScrollEvents(Control root)
	{
		foreach (Control control in root.Controls)
		{
			if (control is Panel { AutoScroll: not false } panel)
			{
				Timer t = new Timer { Interval = 120 };
				_scrollIdleTimers.Add(t);
				t.Tick += (s2, e2) =>
				{
					t.Stop();
					if (!IsDisposed && !Disposing) _userInteracting = false;
				};
				panel.Scroll += (s, e2) => { BeginInteraction(); t.Stop(); t.Start(); };
			}
			if (control.HasChildren) HookScrollEvents(control);
		}
	}

	private void BeginInteraction() => _userInteracting = true;

	private NavButton MakeNav(string label, int iconKind)
	{
		NavButton navButton = new NavButton
		{
			IconKind = iconKind,
			Text = label,
			Size = new Size(Tokens.Scale(210), Tokens.Scale(58))
		};
		_navTips.SetToolTip(navButton, label);
		return navButton;
	}

	private void LayoutNav()
	{
		int y = Tokens.Scale(4);
		int pitch = Tokens.Scale(58);
		int width = _nav.Width;
		int rowHeight = Tokens.Scale(58);

		NavButton[] extra = { _navMore };
		bool formVisible = Visible;
		int visibleCount = NavOrder.Count(n => !formVisible || n.Visible);

		int maxHeight = _nav.Height - Tokens.Scale(31) - rowHeight - y;
		if (visibleCount > 0 && visibleCount * pitch > maxHeight && maxHeight > 0)
		{
			pitch = Math.Max(Tokens.Scale(40), maxHeight / visibleCount);
			rowHeight = pitch;
		}

		foreach (NavButton n in NavOrder)
		{
			if (formVisible && !n.Visible) continue;
			n.SetBounds(0, y, width, rowHeight);
			y += pitch;
		}

		int bottom = _nav.Height - Tokens.Scale(31);
		for (int i = extra.Length - 1; i >= 0; i--)
		{
			if (formVisible && !extra[i].Visible) continue;
			bottom -= rowHeight;
			extra[i].SetBounds(0, Math.Max(y, bottom), width, rowHeight);
		}
	}

	private void UpdateNavRail(float dt) { }

	private void ApplyNavRail()
	{
		int railW = Tokens.Scale(76) - 1;
		int fullW = Tokens.Scale(210) - 1;
		int newW = railW + (int)Math.Round((float)(fullW - railW) * Eased(_navOpen));

		KeepTheRailInFront();

		if (_nav.Width != newW)
		{
			_nav.Width = newW;
			LayoutNav();
			if (newW > railW)
			{
				_contentHost.SendToBack();
				_nav.BringToFront();
				foreach (Panel divider in _dividers)
					divider.BringToFront();
			}
		}

		foreach (NavButton n in NavOrder) n.LabelOpacity = 1f;

		if (_navVersion != null)
		{
			_navVersion.Visible = true;
			_navVersion.Left = Math.Max(Tokens.Scale(3), (newW - _navVersion.Width) / 2);
			_navVersion.Top = Math.Max(0, _nav.Height - _navVersion.Height - Tokens.Scale(6));
		}
	}

	private void KeepTheRailInFront()
	{
		if (_contentHost == null || _nav == null) return;
		_contentHost.SendToBack();
		_nav.BringToFront();
		_scrollBar?.BringToFront();
	}

	private static float Eased(float t) => t * t * (3f - 2f * t);

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		RoundTheCorners();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics g = e.Graphics;
		int b = Tokens.Scale(1);
		if (_cornersAreTheCompositors)
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			using Pen pen = new Pen(WindowEdgeColour(), b);
			using GraphicsPath path = Glass.RoundedPath(
				new RectangleF(b / 2f, b / 2f, Width - b, Height - b),
				Tokens.ScaleF(8f));
			g.DrawPath(pen, path);
			return;
		}
		using SolidBrush brush = new SolidBrush(WindowEdgeColour());
		g.FillRectangle(brush, 0, 0, Width, b);
		g.FillRectangle(brush, 0, Height - b, Width, b);
		g.FillRectangle(brush, 0, b, b, Height - 2 * b);
		g.FillRectangle(brush, Width - b, b, b, Height - 2 * b);
	}

	private static Color WindowEdgeColour() => SignalTokens.Ash.WindowEdge;

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		RoundTheCorners();
		SyncAnimationClock();
	}

	private void RoundTheCorners()
	{
		if (!IsHandleCreated || _cornersAreTheCompositors) return;
		int value = 2;
		try { _cornersAreTheCompositors = DwmSetWindowAttribute(Handle, 33, ref value, 4) == 0; }
		catch (DllNotFoundException) { _cornersAreTheCompositors = false; }
		catch (EntryPointNotFoundException) { _cornersAreTheCompositors = false; }
		if (Region != null) { Region.Dispose(); Region = null; }
	}

	internal void SetNavRailForTest(bool open, bool pinned = true)
	{
		_navPinned = pinned;
		_navOpen = open ? 1f : 0f;
		_navWanted = open;
		ApplyNavRail();
		_nav.Invalidate(true);
	}

	internal void TickForTest(float dt) => UpdateNavRail(dt);

	private static Color FaintestGrey()
		=> SlateSurface.Active ? SignalTokens.Slate.Text3 : SignalTokens.Ash.Text3;

	private int CaptionCloseX() => _titleBar.Width - Tokens.Scale(22) - Tokens.Scale(46) / 2;

	private VibranceHud.Controls.CaptionButton TitleButton(VibranceHud.Controls.CaptionButton.Kind kind, int x)
		=> new VibranceHud.Controls.CaptionButton(kind)
		{
			Size = new Size(Tokens.Scale(46), Tokens.Scale(40) - 2),
			Location = new Point(x, 0),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};

	private void PlaceTitleBarContents()
	{
		if (_logo == null || _closeButton == null || _minButton == null) return;
		_logo.SetBounds(Tokens.Scale(14), Tokens.Scale(8), Tokens.Scale(102), Tokens.Scale(21));
		int w = Tokens.Scale(46);
		int h = Tokens.Scale(40) - 2;
		int closeX = CaptionCloseX();
		_closeButton.SetBounds(closeX, 0, w, h);
		_minButton.SetBounds(closeX - Tokens.Scale(46), 0, w, h);
	}

	private static Color RuleColor()
	{
		Color border = Theme.Border;
		Color background = Theme.Background;
		return Color.FromArgb(
			(int)Math.Round(border.R * 0.4 + background.R * 0.6),
			(int)Math.Round(border.G * 0.4 + background.G * 0.6),
			(int)Math.Round(border.B * 0.4 + background.B * 0.6));
	}

	private void AddDivider(Point loc, Size size, AnchorStyles anchor)
	{
		Panel panel = new Panel { Location = loc, Size = size, BackColor = RuleColor(), Anchor = anchor };
		Controls.Add(panel);
		panel.BringToFront();
		_dividers.Add(panel);
	}

	private void SetWindowOpacity(int percent) => Opacity = Math.Clamp(percent, 50, 100) / 100.0;

	private void DragWindow(object? sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			ReleaseCapture();
			SendMessage(Handle, 161, 2, 0);
		}
	}

	private void RefreshBackdrop()
	{
		Invalidate(true);
		AppBackground.Resize(ClientSize.Width, ClientSize.Height);
	}

	private void ShowVibrance()
	{
		Select(_navVibrance, _vibrancePage);
		_vibrancePage.Refresh();
	}

	private void ShowLocked(string feature) { }

	private void ShowLegal()
	{
		LegalPage legalPage = new LegalPage(() => { Select(_navSettings, _settingsPage); });
		AttachField(legalPage);
		SetContent(legalPage);
	}

	private void SyncScrollBar()
	{
		if (IsDisposed || _currentPage is not GlowPage glowPage)
		{
			_scrollBar.Visible = false;
			return;
		}
		_scrollBar.Maximum = glowPage.ScrollExtent;
		_scrollBar.Viewport = glowPage.ScrollViewport;
		_scrollBar.Value = glowPage.ScrollOffset;
		_scrollBar.Visible = _scrollBar.Needed;
		if (_scrollBar.Needed)
		{
			int w = Tokens.Scale(8);
			int pad = Tokens.Scale(4);
			_scrollBar.SetBounds(
				_contentHost.Right - w - pad,
				_contentHost.Top + pad,
				w,
				Math.Max(1, _contentHost.Height - pad * 2));
			_scrollBar.BringToFront();
		}
	}

	private void Select(NavButton button, Control page)
	{
		SetActive(button);
		SetContent(page);
	}

	private void SetActive(NavButton active)
	{
		foreach (NavButton obj in NavOrder) obj.Active = obj == active;
		_navSettings.Active = active == _navSettings;
		_navMore.Active = active == _navSettings;
	}

	private ContextMenuStrip BuildMoreMenu()
	{
		ContextMenuStrip menu = SignalMenu.ActionMenu(square: true);
		menu.Items.Add("Settings", null, (s, e) => { _navSettings.PerformClick(); })
			.Tag = new SignalMenu.RowArt(SignalMenu.DrawSettingsIcon, _navSettings.Active);
		return menu;
	}

	private void ToggleMoreMenu()
	{
		ContextMenuStrip? menu = _moreMenu;
		if (menu != null && menu.Visible) { _moreMenu.Close(); return; }
		_moreMenu?.Dispose();
		ContextMenuStrip built = _moreMenu = BuildMoreMenu();
		int height = built.Height;
		built.Show(_navMore, new Point(_navMore.Width + Tokens.Scale(4), _navMore.Height - height));
	}

	internal void CloseMoreMenuForTest() => _moreMenu?.Close();

	internal void PressMoreMenuItemForTest(string text)
	{
		using ContextMenuStrip menu = BuildMoreMenu();
		menu.Items.Cast<ToolStripItem>().Single(i => i.Text == text).PerformClick();
	}

	private void SetContent(Control page)
	{
		Control currentPage = _currentPage;
		if (currentPage is GlowPage glowPage)
			glowPage.ScrollStateChanged -= OnPageScrollStateChanged;

		bool hadHandle = _contentHost.IsHandleCreated;
		if (hadHandle) SendMessage(_contentHost.Handle, 11, 0, 0);
		try
		{
			_contentHost.SuspendLayout();
			_contentHost.Controls.Clear();
			page.Dock = DockStyle.Fill;
			if (page is ScrollableControl scrollableControl)
				scrollableControl.AutoScrollPosition = Point.Empty;
			_contentHost.Controls.Add(page);
			_contentHost.ResumeLayout();
		}
		finally
		{
			if (hadHandle)
			{
				SendMessage(_contentHost.Handle, 11, 1, 0);
				_contentHost.Refresh();
			}
		}

		_currentPage = page;
		if (page is GlowPage glowPage2)
		{
			glowPage2.ScrollStateChanged -= OnPageScrollStateChanged;
			glowPage2.ScrollStateChanged += OnPageScrollStateChanged;
		}
		SyncScrollBar();
		KeepTheRailInFront();

		if (currentPage != null && currentPage != page
			&& currentPage != _vibrancePage && currentPage != _settingsPage
			&& currentPage != _crosshairPage
			&& currentPage != _profilesPage && currentPage != _monitorPage
			&& currentPage != _panelPage && currentPage != _customizePage
			&& currentPage != _libraryPage && currentPage != _darkBoostPage)
		{
			currentPage.Dispose();
		}
	}

	private void OnPageScrollStateChanged(object? sender, EventArgs e)
	{
		_lastScrollSeconds = _clock.Elapsed.TotalSeconds;
		SyncScrollBar();
	}

	public void ShowAndFocus()
	{
		Show();
		if (WindowState == FormWindowState.Minimized)
			WindowState = FormWindowState.Normal;
		BringToFront();
		try { SetForegroundWindow(Handle); } catch { }
		Activate();
	}

	internal void HandPendingNoticesTo(MainWindow next)
	{
	}

	internal void ApplySharedSetup(string code)
	{
		ShowAndFocus();
		if (!ConfigCode.TryDecode(code, out ConfigCode.Parts parts))
		{
			GlassDialog.Show(this, "PlexusX", "That link did not carry a setup this version understands. Ask whoever shared it to copy the code again.");
			return;
		}
		ConfigLibraryEntry entry = new ConfigLibraryEntry("Shared setup", "", "", "", parts);
		string body = "A setup from the website wants to change:\n\n  " + string.Join("\n  ", ConfigCode.Describe(parts)) + "\n\nApply it?";
		if (GlassDialog.Show(this, "Use this setup?", body, GlassDialogButtons.YesNo, GlassDialogTone.Info, "Use setup") != DialogResult.Yes)
			return;

		List<string> applied = new();
		List<string> notes = new();
		DisplayPreset look = entry.Look;
		if (look != null) _vibrancePage.ApplyLibraryPreset(look).AddTo(applied, notes);
		CrosshairConfig crosshair = entry.Crosshair;
		if (crosshair != null)
		{
			_crosshairPage.ApplyLibraryCrosshair(crosshair);
			applied.Add("crosshair");
		}
		MonitorCode? mode = entry.Mode;
		if (mode.HasValue)
		{
			MonitorCode m = mode.GetValueOrDefault();
			notes.Add(_monitorPage.ApplyLibraryMode(m));
		}
		List<string> messages = new();
		if (applied.Count > 0) messages.Add(string.Join(" and ", applied) + " applied.");
		messages.AddRange(notes);
		if (messages.Count > 0) GlassDialog.Show(this, "PlexusX", string.Join("  ", messages));
	}

	internal static bool IsTheUsersClose(int msg, nint wParam)
		=> msg == 274 && ((ulong)wParam & 0xFFF0uL) == 61536;

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == SingleInstance.ShowWindowMessage && SingleInstance.ShowWindowMessage != 0)
			ShowAndFocus();

		if (IsTheUsersClose(m.Msg, m.WParam))
		{
			SaveWindowBounds();
			Hide();
			return;
		}

		if (m.Msg == WM_ENTERSIZEMOVE)
		{
			_movingOrSizing = true;
			SyncAnimationClock();
		}
		else if (m.Msg == WM_EXITSIZEMOVE)
		{
			_movingOrSizing = false;
			SyncAnimationClock();
		}

		base.WndProc(ref m);
	}

	public static int NextNavIndex(int current, int count, bool forward)
	{
		if (count <= 0) return 0;
		return ((current + (forward ? 1 : -1)) % count + count) % count;
	}

	private Control? FocusedControl()
	{
		Control? activeControl;
		for (activeControl = ActiveControl;
			activeControl is ContainerControl { ActiveControl: not null } containerControl;
			activeControl = containerControl.ActiveControl) { }
		return activeControl;
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData != Keys.Escape && FocusedControl() is HotkeyPicker { IsCapturing: not false })
			return base.ProcessCmdKey(ref msg, keyData);

		switch (keyData)
		{
			case Keys.Escape:
				if (FocusedControl() is HotkeyPicker { IsCapturing: not false } picker)
				{
					picker.CancelCapture();
					return true;
				}
				SaveWindowBounds();
				Hide();
				return true;
			case Keys.Tab | Keys.Control:
				StepNav(true);
				return true;
			case Keys.Tab | Keys.Shift | Keys.Control:
				StepNav(false);
				return true;
			default:
				if ((keyData & Keys.Control) == Keys.Control)
				{
					int idx = (int)((keyData & Keys.KeyCode) - 49);
					if (idx >= 0 && idx < 9)
					{
						List<NavButton> visible = VisibleNavButtons();
						if (idx < visible.Count)
						{
							visible[idx].PerformClick();
							return true;
						}
					}
				}
				return base.ProcessCmdKey(ref msg, keyData);
		}
	}

	private List<NavButton> VisibleNavButtons()
	{
		List<NavButton> list = NavOrder.Where(n => n.Visible).ToList();
		list.Add(_navSettings);
		return list;
	}

	private void StepNav(bool forward)
	{
		List<NavButton> list = VisibleNavButtons();
		if (list.Count == 0) return;
		int idx = list.FindIndex(n => n.Active);
		if (idx < 0) idx = 0;
		list[NextNavIndex(idx, list.Count, forward)].PerformClick();
	}

	private void ClearStaleMaximizedState()
	{
		if (!_settings.WindowMaximized) return;
		_settings.WindowMaximized = false;
		WindowState = FormWindowState.Normal;
		try { _store.Save(_settings); } catch { }
	}

	public void OnEngineChanged()
	{
		_vibrancePage.Refresh();
		_profilesPage.Refresh();
	}

	internal LookNote ApplyScenePresetByName(string name)
	{
		return _vibrancePage.ApplyPresetByBindingName(name);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_timer.Stop();
			_timer.Tick -= OnAnimationTick;
			_timer.Dispose();
			_navTips.Dispose();
			_moreMenu?.Dispose();
			_navSettings.Dispose();
			foreach (Timer scrollIdleTimer in _scrollIdleTimers)
			{
				scrollIdleTimer.Stop();
				scrollIdleTimer.Dispose();
			}
			_scrollIdleTimers.Clear();
		}
		base.Dispose(disposing);
	}
}