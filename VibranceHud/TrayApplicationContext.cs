using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using NvAPIWrapper.Native.Exceptions;
using VibranceHud.Audio;
using VibranceHud.Capabilities;
using VibranceHud.Capture;
using VibranceHud.Controls;
using VibranceHud.Crosshair;
using VibranceHud.Customize;
using VibranceHud.DarkBoost;
using VibranceHud.Discord;
using VibranceHud.Display;
using VibranceHud.Fullscreen;
using VibranceHud.Games;
using VibranceHud.Hotkeys;
using VibranceHud.Licensing;
using VibranceHud.Monitors;
using VibranceHud.Music;
using VibranceHud.Pages;
using VibranceHud.Presets;
using VibranceHud.Remote;
using VibranceHud.Startup;
using VibranceHud.Support;
using VibranceHud.Theming;
using VibranceHud.Updates;
using VibranceHud.Zoom;

namespace VibranceHud;

public sealed class TrayApplicationContext : ApplicationContext
{
	internal delegate ISaturationOverlay? DxAttempt(out DxInitFailureKind failure, out string failureMessage, out int failureCode);

	private sealed class NoOverlay : ISaturationOverlay, IDisplayOverlay
	{
		public OverlayMode ActiveMode => OverlayMode.Dx;
		public DxInitFailureKind LastFailure => DxInitFailureKind.None;
		public string LastFailureMessage => "";
		public bool EffectIsLive => false;
		public void Apply(float[] matrix) { }
		public void Clear() { }
	}

	private sealed class MessageOnlyAnchor : Control
	{
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.Parent = new IntPtr(-3);
				return cp;
			}
		}
	}

	private const int HOTKEY_ID = 1;
	private const int MAIN_HOTKEY_ID = 2;
	private const int SCREENSHOT_HOTKEY_ID = 3;
	private const int PANIC_HOTKEY_ID = 4;
	private const uint PANIC_HOTKEY_MODS = 16391u;
	private const uint PANIC_HOTKEY_VK = 123u;

	private DarkBoostKeys? _darkBoostKeys;
	private readonly HashSet<string> _runningGames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private const int PRESET_BINDING_ID_BASE = 100;
	private int _boundPresetBindings;
	private HotkeyCaptureRelease? _captureRelease;

	private readonly NotifyIcon _trayIcon;
	private readonly Icon _trayIconFrame;
	private BackgroundUpdateChecker? _backgroundChecker;
	private readonly TaskCompletionSource _startupUpdateStep = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly HotkeyWindow _hotkeyWindow;
	private readonly IVibranceController _controller;
	private readonly RedetectingVibranceController _driverRedetect;
	private readonly ISaturationOverlay _overlay;
	private readonly MultiMonitorGammaRamp _gammaRamp;
	private readonly VibranceEngine _engine;
	private readonly MonitorSelection _monitorSelection;
	private readonly SelectedVibranceController _selectedDriver;

	private readonly DiscordPresenceClient _discordPresence = new DiscordPresenceClient();
	private readonly object _presenceGate = new object();
	private string? _presenceWanted;
	private bool _presenceSending;
	private bool _presencePending;
	private bool _presenceShown;

	private readonly SettingsStore _store;
	private readonly CustomThemeService _customTheme;
	private readonly CrosshairService _crosshair = new CrosshairService();
	private readonly AppSettings _settings;
	private bool _nightVisionCodeOk;
	private readonly SplashForm _splash;
	private readonly AudioEdgeService? _audioEdge;
	private RestorePointSaver? _restorePointSaver;
	private ProfileEngineCoordinator? _profileCoordinator;
	private MainWindow _window;

	private VibrancePopup? _vibrancePopup;
	private ToolStripMenuItem? _hotkeyMenuItem;
	private ToolStripMenuItem? _mainHotkeyMenuItem;
	private ToolStripMenuItem? _screenshotHotkeyMenuItem;
	private CompositionKeeper? _compositionKeeper;
	private System.Windows.Forms.Timer? _betaGateTimer;
	private System.Windows.Forms.Timer? _taskbarReapplyTimer;
	private bool _taskbarStyledThisSession;
	private bool _gameOwnsTheWholeScreen;
	private ColourWatchdog? _colourWatchdog;
	private System.Windows.Forms.Timer? _colourWatchdogTimer;
	private System.Windows.Forms.Timer? _colourForegroundTimer;
	private WinEventListener? _foregroundListener;
	private System.Windows.Forms.Timer? _colourTraceTimer;
	private int _remeasureTries;
	private int _restoringMonitor;
	private FullscreenWatch? _fullscreen;
	private FullscreenWatch? _competitiveWatch;
	private MainWindow? _fullscreenRecheckWindow;

	private GameSelection? _gameSelection;
	private string? _stagedUpdateVersion;
	private PhoneControl? _phoneControl;
	private PhoneListener? _phoneListener;
	private Control? _uiAnchor;
	private bool _constructed;
	private PhoneWindow? _phoneWindow;
	private bool _displayRestored;
	private readonly DarkBoostFollower _darkBoost = new DarkBoostFollower();
	private FullscreenZoom? _zoom;
	private System.Windows.Forms.Timer? _zoomTimer;
	private long _zoomLastTicks;
	private string _zoomBindNoted = "";
	private ZoomKeyBinder? _zoomKey;

	private GameSelection GameSelectionInstance =>
		_gameSelection ?? (_gameSelection = new GameSelection(_settings, _store));

	[DllImport("user32.dll")] private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);
	[DllImport("user32.dll")] private static extern bool UnregisterHotKey(nint hWnd, int id);

	private void SetPresenceInBackground(string? presetName)
	{
		if (RemoteSwitches.IsOff("discord")) return;
		lock (_presenceGate)
		{
			_presenceWanted = presetName;
			_presenceShown = _settings.ShowOnDiscord;
			_presencePending = true;
			if (_presenceSending) return;
			_presenceSending = true;
		}
		Thread t = new Thread(() =>
		{
			while (true)
			{
				string? wanted;
				bool shown;
				lock (_presenceGate)
				{
					if (!_presencePending) { _presenceSending = false; break; }
					wanted = _presenceWanted;
					shown = _presenceShown;
					_presencePending = false;
				}
				try
				{
					if (shown) _discordPresence.SetPreset(wanted);
					else _discordPresence.Clear();
				}
				catch { }
			}
		})
		{
			IsBackground = true,
			Name = "PlexusX Discord presence"
		};
		t.Start();
	}

	private void ApplyCompositionKeeper()
	{
		bool keep = _settings.KeepDesktopComposited;
		if (keep && _compositionKeeper == null) _compositionKeeper = new CompositionKeeper();
		else if (!keep && _compositionKeeper != null)
		{
			_compositionKeeper.Dispose();
			_compositionKeeper = null;
		}
	}

	internal static bool RunTheCompositionKeeperMigrations(AppSettings settings)
	{
		if (settings.CompositionKeeperDefaultOffDone) return false;
		settings.KeepDesktopComposited = false;
		settings.CompositionKeeperDefaultOffDone = true;
		settings.CompositionKeeperDefaultOnDone = true;
		settings.LowLatencyResetDone = true;
		return true;
	}

	internal static bool ShouldKeepTheTaskbarStyleAlive(bool aStyleIsSet, bool gameOwnsTheWholeScreen)
		=> aStyleIsSet && !gameOwnsTheWholeScreen;

	internal static void WireTaskbarKeepAliveToTheFullscreenWatch(FullscreenWatch watch, Action<bool> onGameOwnsTheWholeScreen)
	{
		watch.Changed += (fullscreen, _) => onGameOwnsTheWholeScreen(fullscreen);
	}

	private void RefreshTaskbarReapplyTimer()
	{
		if (!ShouldKeepTheTaskbarStyleAlive(CustomizeService.ShouldReapplyTaskbarStyle(_settings), _gameOwnsTheWholeScreen))
		{
			_taskbarReapplyTimer?.Stop();
			return;
		}
		if (_taskbarReapplyTimer == null)
		{
			_taskbarReapplyTimer = new System.Windows.Forms.Timer { Interval = 500 };
			_taskbarReapplyTimer.Tick += (s, e2) =>
			{
				if (!ShouldKeepTheTaskbarStyleAlive(CustomizeService.ShouldReapplyTaskbarStyle(_settings), _gameOwnsTheWholeScreen))
				{
					_taskbarReapplyTimer?.Stop();
				}
				else
				{
					CustomizeService.ApplyTaskbarStyle(new TaskbarSettings
					{
						Style = CustomizeService.StyleFromName(_settings.TaskbarStyleName),
						ColorArgb = _settings.TaskbarColorArgb,
						UseAccentForTaskbar = _settings.TaskbarFollowsAccent
					}, CustomizeService.ReadFromRegistry().System);
					_taskbarStyledThisSession = true;
				}
			};
		}
		_taskbarReapplyTimer.Start();
	}

	private void StartColourWatchdog()
	{
		ReceiveSystemEventsOnThisThread();
		Func<DateTime> now = () => DateTime.UtcNow;
		_colourWatchdog = new ColourWatchdog(
			() => _engine.ReassertDisplayState(),
			HdrDetection.AllColouredDisplaysInHdr,
			() => _engine.DisplayStateIsNeutral,
			now, null,
			() => _engine.ForgetAppliedState());
		SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
		SystemEvents.PowerModeChanged += OnPowerModeChanged;
		SystemEvents.SessionSwitch += OnSessionSwitch;
		SystemEvents.SessionEnded += OnSessionEnded;
		_colourWatchdogTimer = new System.Windows.Forms.Timer
		{
			Interval = (int)ColourWatchdog.TickInterval.TotalMilliseconds + 1000
		};
		_colourWatchdogTimer.Tick += (s, e2) =>
		{
			RuntimeFacts.NoteFullscreen(CrosshairService.IsExclusiveFullscreen());
			_colourWatchdog?.Tick();
		};
		_colourWatchdogTimer.Start();
		_foregroundListener = new WinEventListener(3u, 3u,
			_ => {
				RuntimeFacts.NoteFullscreen(CrosshairService.IsExclusiveFullscreen());
				_colourWatchdog?.OnForegroundChanged();
				ArmOwedForegroundColourCheck();
			}, now);
	}

	private void ArmOwedForegroundColourCheck()
	{
		TimeSpan? delay = _colourWatchdog?.ForegroundCheckDelay;
		if (!delay.HasValue) { _colourForegroundTimer?.Stop(); return; }
		if (_colourForegroundTimer == null)
		{
			_colourForegroundTimer = new System.Windows.Forms.Timer();
			_colourForegroundTimer.Tick += (_, _) =>
			{
				_colourForegroundTimer.Stop();
				_colourWatchdog?.Tick();
				ArmOwedForegroundColourCheck();
			};
		}
		_colourForegroundTimer.Interval = Math.Max(1, (int)Math.Ceiling(delay.Value.TotalMilliseconds) + 50);
		_colourForegroundTimer.Start();
	}

	internal static void ReceiveSystemEventsOnThisThread()
	{
		if (!(SynchronizationContext.Current is WindowsFormsSynchronizationContext))
			SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
	}

	private void StartColourTrace()
	{
		if (!LaunchOptions.TraceColour) return;
		_colourTraceTimer = new System.Windows.Forms.Timer { Interval = 60000 };
		_colourTraceTimer.Tick += (s, e2) =>
		{
			ReassertTrace.WriteNow(() => new ReassertTrace.Reading(
				_engine.Meter, RuntimeFacts.Overlay, RuntimeFacts.MagInitialized,
				RuntimeFacts.ExclusiveFullscreenSeen, DateTime.Now,
				UpdateService.CurrentVersion.ToString(3), "store", CrashLog.CrashDirectory,
				DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()));
		};
		_colourTraceTimer.Start();
	}

	private void MeasureAgainIfStartupWasRefused()
	{
		try
		{
			MachineCapabilities current = Machine.Current;
			if (RemeasurePolicy.ShouldRemeasure(current, _remeasureTries))
			{
				_remeasureTries++;
				if (Machine.MeasureAndRemember(_engine.DriverAvailable, _settings.OverlayMode, _settings))
					_store.Save(_settings);
				MachineCapabilities next = Machine.Current;
				if (next.GammaRamp != current.GammaRamp || next.RefusedWhy != current.RefusedWhy)
					ShowTheNewMeasurement();
			}
		}
		catch { }
	}

	private void ShowTheNewMeasurement()
	{
		MainWindow? window = _window;
		if (window == null || window.IsDisposed) return;
		if (!window.Visible) { RebuildWindowQuietlyLater(); return; }
		EventHandler? onHidden = null;
		onHidden = (s, e2) =>
		{
			if (!window.Visible)
			{
				window.VisibleChanged -= onHidden;
				if (window == _window) RebuildWindowQuietlyLater();
			}
		};
		window.VisibleChanged += onHidden;
	}

	private void RebuildWindowQuietlyLater()
	{
		System.Windows.Forms.Timer deferred = new System.Windows.Forms.Timer { Interval = 1 };
		deferred.Tick += (s, e2) =>
		{
			deferred.Stop();
			deferred.Dispose();
			MainWindow? window = _window;
			if (window == null || window.IsDisposed) return;
			if (window.Visible) ShowTheNewMeasurement();
			else RebuildWindow(quietly: true);
		};
		deferred.Start();
	}

	private void OnDisplaySettingsChanged(object? sender, EventArgs e)
	{
		MeasureAgainIfStartupWasRefused();
		_driverRedetect?.OnDisplayEvent();
		_monitorSelection?.Refresh();
		_colourWatchdog?.OnDisplayChanged();
		_fullscreen?.PollNow();
		_competitiveWatch?.PollNow();
	}

	private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
	{
		if (e.Mode != PowerModes.Resume) return;
		MeasureAgainIfStartupWasRefused();
		_driverRedetect?.OnDisplayEvent();
		_monitorSelection?.Refresh();
		_colourWatchdog?.OnResume();
	}

	private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
	{
		bool resume = e.Reason is
			SessionSwitchReason.ConsoleConnect or
			SessionSwitchReason.RemoteConnect or
			SessionSwitchReason.RemoteDisconnect or
			SessionSwitchReason.SessionLogon or
			SessionSwitchReason.SessionUnlock;
		if (resume)
		{
			_driverRedetect?.OnDisplayEvent();
			_monitorSelection?.Refresh();
			_colourWatchdog?.OnSessionUnlock();
		}
		_fullscreen?.PollNow();
		_competitiveWatch?.PollNow();
	}

	private void OnSessionEnded(object? sender, SessionEndedEventArgs e)
	{
		try { _vibrancePopup?.Dispose(); } catch { }
		RestoreDisplayNow();
		OnTheUiThreadOrNotAtAll(ExitThreadOnceConstructed);
	}

	private void StopColourWatchdog()
	{
		_colourWatchdog?.Stop();
		if (_colourForegroundTimer != null)
		{
			_colourForegroundTimer.Stop();
			_colourForegroundTimer.Dispose();
			_colourForegroundTimer = null;
		}
		_foregroundListener?.Dispose();
		_foregroundListener = null;
		if (_fullscreenRecheckWindow != null)
		{
			_fullscreenRecheckWindow.Activated -= OnPlexusXActivated;
			_fullscreenRecheckWindow = null;
		}
		_fullscreen?.Dispose();
		_fullscreen = null;
		_competitiveWatch?.Dispose();
		_competitiveWatch = null;
		SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
		SystemEvents.PowerModeChanged -= OnPowerModeChanged;
		SystemEvents.SessionSwitch -= OnSessionSwitch;
		SystemEvents.SessionEnded -= OnSessionEnded;
		if (_colourWatchdogTimer != null)
		{
			_colourWatchdogTimer.Stop();
			_colourWatchdogTimer.Dispose();
			_colourWatchdogTimer = null;
		}
		if (_colourTraceTimer != null)
		{
			_colourTraceTimer.Stop();
			_colourTraceTimer.Dispose();
			_colourTraceTimer = null;
		}
	}

	private void ResetColours()
	{
		_engine.Reset();
		_settings.VibrancePercent = _engine.Vibrance;
		_settings.VibranceChosenByUser = _engine.VibranceChosenByUser;
		_settings.SaturationPercent = _engine.Saturation;
		_settings.BrightnessPercent = _engine.Brightness;
		_settings.GammaPercent = _engine.Gamma;
		_settings.ContrastPercent = _engine.Contrast;
		_settings.Temperature = _engine.Temperature;
		_settings.EyeCare = false;
		_settings.Tone = _engine.Tone;
		_settings.Mix = null;
		_settings.Hue = _engine.Hue;
		_settings.RedGain = _engine.RedGain;
		_settings.GreenGain = _engine.GreenGain;
		_settings.BlueGain = _engine.BlueGain;
		try { _store.Save(_settings); } catch { }
		_window?.OnEngineChanged();
	}

	private void ResetTaskbarStyle()
	{
		CustomizeService.ApplyTaskbarStyle(new TaskbarSettings { Style = TaskbarStyle.Normal },
			CustomizeService.ReadFromRegistry().System);
		_taskbarStyledThisSession = false;
		_settings.TaskbarStyleName = TaskbarStyle.Normal.ToString();
		_store.Save(_settings);
		RefreshTaskbarReapplyTimer();
	}

	private void RegisterPanicHotkey()
	{
		try
		{
			if (!RegisterHotKey(_hotkeyWindow.Handle, PANIC_HOTKEY_ID, PANIC_HOTKEY_MODS, PANIC_HOTKEY_VK))
				CrashLog.Write(new InvalidOperationException("Restore-my-monitor hotkey (Ctrl+Alt+Shift+F12) is taken by another app."));
		}
		catch { }
	}

	private void RestoreMyMonitor()
	{
		if (Interlocked.Exchange(ref _restoringMonitor, 1) == 1) return;
		try { ResetColours(); } catch { }
		SynchronizationContext? ui = SynchronizationContext.Current;
		Task.Run(() =>
		{
			PanicResult? result = null;
			try
			{
				result = MonitorPanicRestore.Run(
					_settings, _store, new Dxva2MonitorWriter(), MonitorProbe.Probe,
					ColdStartRestore.GammaLinearNow(_settings, _store),
					ColdStartRestore.DriverBaselineNow(_settings, _store),
					null, null,
					action => { if (ui != null) ui.Send(_ => action(), null); else action(); },
					userInitiated: true);
			}
			catch { }
			finally { Interlocked.Exchange(ref _restoringMonitor, 0); }
			void Say()
			{
				try { _trayIcon?.ShowBalloonTip(4000, "PlexusX", MonitorPanicRestore.BalloonText(result), ToolTipIcon.Info); }
				catch { }
			}
			if (ui != null) ui.Post(_ => Say(), null); else Say();
		});
	}

	private void RecheckFullscreenWhenWeComeForward(MainWindow window)
	{
		if (_fullscreenRecheckWindow == window) return;
		if (_fullscreenRecheckWindow != null)
			_fullscreenRecheckWindow.Activated -= OnPlexusXActivated;
		_fullscreenRecheckWindow = window;
		window.Activated += OnPlexusXActivated;
	}

	private void OnPlexusXActivated(object? sender, EventArgs e)
	{
		_fullscreen?.PollNow();
		_competitiveWatch?.PollNow();
	}

	internal static void WireFullscreen(FullscreenWatch watch, VibranceEngine engine, Action refreshStatus)
	{
		watch.Changed += (fullscreen, _) => { engine.ColourReachesScreen = !fullscreen; refreshStatus(); };
		watch.LatchChanged += _ => refreshStatus();
	}

	internal static void WireCompetitive(FullscreenWatch watch, VibranceEngine engine, Action refreshStatus)
	{
		watch.Changed += (fullscreen, _) => { engine.CompetitiveHold = fullscreen; refreshStatus(); };
	}

	private void ApplyCompetitiveMode()
	{
		bool on = _settings.CompetitiveMode;
		if (on && _competitiveWatch == null)
		{
			_competitiveWatch = FullscreenWatch.ForAnyFullScreenWindow();
			_competitiveWatch.FullscreenAllowed = () =>
				_engine.CompetitiveMonitorCanCarry(AnyFullScreenWindow.ForegroundMonitorDeviceName());
			WireCompetitive(_competitiveWatch, _engine, ShowColourReachOnPage);
			_competitiveWatch.Start();
		}
		else if (!on && _competitiveWatch != null)
		{
			_competitiveWatch.Dispose();
			_competitiveWatch = null;
			_engine.CompetitiveHold = false;
		}
		ShowColourReachOnPage();
	}

	internal static bool CompetitiveIsOn(AppSettings settings, VibranceEngine engine)
		=> settings.CompetitiveMode && VibranceEngine.CompetitiveModeCanWork(engine);

	internal static ColourReachFacts StatusFacts(FullscreenWatch? watch, ISaturationOverlay? overlay, AppSettings? settings)
		=> VibrancePage.ColourReachFactsFrom(watch?.LatchedGame == null, overlay, settings);

	internal static void NoteGameClosed(FullscreenWatch? watch, string gameId)
	{
		SupportedGame? game = SupportedGames.ById(gameId);
		if (game == null) return;
		foreach (var p in game.AllProcessNames) watch?.GameClosed(p);
	}

	internal static void AskAgainWhenItComesForward(Form window, Func<FullscreenWatch?> watch)
	{
		window.Activated += (_, _) => watch()?.PollNow();
	}

	internal static ContextMenuStrip NewTrayMenu() => SignalMenu.ActionMenu();

	internal static void SetHotkeyRow(ToolStripMenuItem item, string name, string? shortcut)
	{
		item.Text = name;
		item.ShortcutKeyDisplayString = string.IsNullOrEmpty(shortcut) ? null : shortcut;
	}

	internal static (string Title, string Text, ToolTipIcon Icon)? HotkeyBalloon(string lookName, LookNote note)
	{
		if (!string.IsNullOrEmpty(note.Text))
			return (lookName, note.Text, note.Refused ? ToolTipIcon.Warning : ToolTipIcon.Info);
		return null;
	}

	private void SayWhatTheLookSaid(string lookName, LookNote note)
	{
		if (_trayIcon == null) return;
		var tuple = HotkeyBalloon(lookName, note);
		if (!tuple.HasValue) return;
		var (t, b, i) = tuple.GetValueOrDefault();
		try { _trayIcon.ShowBalloonTip(4000, t, b, i); } catch { }
	}

	private void ShowColourReachOnPage()
	{
		try
		{
			MainWindow? window = _window;
			if (window == null || window.IsDisposed) return;
			VibrancePage page = window.VibrancePageForStatus;
			page.ColourReachSource ??= () => StatusFacts(_fullscreen, _overlay, _settings) with
			{
				CompetitiveOn = CompetitiveIsOn(_settings, _engine)
			};
			page.RefreshColourReach();
		}
		catch { }
	}

	public TrayApplicationContext()
	{
		_driverRedetect = new RedetectingVibranceController(CreateVibranceController(), TryCreateVibranceController);
		_controller = _driverRedetect;
		_store = SettingsStore.Default();
		_settings = _store.Load();
		if (GameLookSnapshot.PutBackOwed(_settings)) _store.Save(_settings);
		LoopbackSignIn.UseTheBrowserFrom(() => _settings);
		_overlay = TryCreateOverlay(() => _settings.QualityColour);
		_nightVisionCodeOk = true;
		_gammaRamp = new MultiMonitorGammaRamp(deviceName =>
		{
			MonitorColourSetting? m = _settings.MonitorColours.Find(x => x.DeviceName == deviceName);
			return m != null ? (m.GammaPercent, m.WarmthPercent) : (100, 0);
		});
		_monitorSelection = new MonitorSelection(_settings, ColourMonitorInventory.Read);
		_selectedDriver = new SelectedVibranceController(_controller, _monitorSelection, _settings, () => _store.Save(_settings));
		_settings.OwnedMonitorGamma ??= new Dictionary<string, ushort[]>(StringComparer.OrdinalIgnoreCase);
		_gammaRamp.Selection = _monitorSelection;
		_gammaRamp.AdoptRecoveryBaselines(_settings.OwnedMonitorGamma);
		_gammaRamp.BeforeDeviceColour = (id, ramp) =>
		{
			_settings.OwnedMonitorGamma[id] = ramp;
			_settings.GammaRestorePending = true;
			_store.Save(_settings);
		};
		_gammaRamp.DeviceReleased = id =>
		{
			_settings.OwnedMonitorGamma.Remove(id);
			_settings.GammaRestorePending = _settings.OwnedMonitorGamma.Count > 0;
			_store.Save(_settings);
		};
		_gammaRamp.BeforeFirstColour = () =>
		{
			if (!_settings.GammaRestorePending) { _settings.GammaRestorePending = true; _store.Save(_settings); }
		};
		CrashHandBack.Register(_gammaRamp);
		try
		{
			RememberTheDriverBaseline(_settings, _controller.CurrentLevel, () => _store.Save(_settings));
			RememberEveryDisplayBaseline(_settings, _controller, () => _store.Save(_settings));
		}
		catch { }
		_engine = new VibranceEngine(_selectedDriver, _overlay, _gammaRamp) { MonitorSelection = _monitorSelection };
		_monitorSelection.Changed += () =>
		{
			_selectedDriver.ReleaseExcluded();
			_store.Save(_settings);
			_engine.SelectionMoved();
		};
		_driverRedetect.Changed += () =>
			WhenTheDriverCameBack(_settings, _controller, _engine, () => _store.Save(_settings));
		HandTheEngineTheDriverBaseline(_engine, _settings, () => _store.Save(_settings));
		Application.ApplicationExit += OnApplicationExit;
		RecordOverlayOutcome(_settings, _overlay);
		RuntimeFacts.NoteOverlay(_settings.OverlayMode, (_overlay as IDisplayOverlay)?.EffectIsLive ?? true);
		_store.Save(_settings);
		StartOverlayUpgradeWatch();
		if (RunTheCompositionKeeperMigrations(_settings)) _store.Save(_settings);
		if (AppSettings.ApplyZoomKeyDefault(_settings)) _store.Save(_settings);
		ApplyCompositionKeeper();
		_customTheme = new CustomThemeService(SettingsStore.DefaultDirectory, _settings);
		_customTheme.Restore();
		SlatePreferences.Apply(_settings.SlateGuiOpacityPercent, _settings.SlateMatteBlur, _settings.CustomBackgroundMatteArgb);
		_crosshair.Apply(_settings.ActiveCrosshair);
		if (_settings.CrosshairEnabled) _crosshair.Show();
		_engine.Brightness = _settings.BrightnessPercent;
		_engine.Gamma = _settings.GammaPercent;
		_engine.Contrast = _settings.ContrastPercent;
		_engine.Temperature = _settings.ResolvedTemperature;
		_engine.Hue = _settings.Hue;
		_engine.RedGain = _settings.RedGain;
		_engine.GreenGain = _settings.GreenGain;
		_engine.BlueGain = _settings.BlueGain;
		_engine.Depth = _settings.Depth;
		_engine.EyeCareWarmthPercent = _settings.EyeCareWarmth;
		_engine.EyeCareDim = _settings.EyeCareDim;
		if (_settings.RustResolutionWidth > 0 && _settings.RustResolutionHeight > 0 && MonitorRules.For(_settings.MonitorRules, "rust") == null)
		{
			_settings.MonitorRules = MonitorRules.Set(_settings.MonitorRules, "rust", _settings.RustResolutionWidth, _settings.RustResolutionHeight);
			_store.Save(_settings);
		}
		if (_settings.VibranceScaleVersion < 2)
		{
			if (!_engine.DriverAvailable)
				_settings.VibrancePercent = VibranceEngine.MigrateSoftwareVibrance(_settings.ResolvedVibrance);
			_settings.VibranceScaleVersion = 2;
			_store.Save(_settings);
		}
		_engine.StreamingMode = _settings.StreamingMode && CaptureStatus.ToggleCanHelp(_settings.OverlayMode, _engine.DriverAvailable);
		OutsideDriverChange.AtLaunch(_engine, _settings, _selectedDriver);
		SavedVibranceRestore.Apply(_engine, _settings);
		_engine.Saturation = _settings.ResolvedSaturation;
		_settings.SaturationPercent = _engine.Saturation;
		_store.Save(_settings);
		if (Machine.MeasureOrReuse(_engine.DriverAvailable, _settings.OverlayMode, _settings)) _store.Save(_settings);
		_engine.Tone = ToneCurve.Gate(_settings.ResolvedTone);
		_engine.Mix = _settings.ResolvedMix;
		PushDarkBoostLook();
		_engine.NightVision = global::A.A.Available && _settings.NightVisionOn && _nightVisionCodeOk && _settings.DarkBoostBanRiskAccepted;
		if (ColourModeLook.Settle(_settings)) _store.Save(_settings);
		ThemePalette palette = ThemeCatalog.Resolve(_settings.ThemeName, _settings.LightTheme);
		if (_settings.ThemeName != palette.Name) { _settings.ThemeName = palette.Name; _store.Save(_settings); }
		Theme.Apply(palette);
		if (CustomizeService.ShouldReapplyTaskbarStyle(_settings))
		{
			CustomizeService.ApplyTaskbarStyle(new TaskbarSettings
			{
				Style = CustomizeService.StyleFromName(_settings.TaskbarStyleName),
				ColorArgb = _settings.TaskbarColorArgb,
				UseAccentForTaskbar = _settings.TaskbarFollowsAccent
			}, CustomizeService.ReadFromRegistry().System);
			_taskbarStyledThisSession = true;
		}
		RefreshTaskbarReapplyTimer();
		StartColourWatchdog();
		StartColourTrace();
		_fullscreen = FullscreenWatch.ForThisMachine();
		WireFullscreen(_fullscreen, _engine, ShowColourReachOnPage);
		WireTaskbarKeepAliveToTheFullscreenWatch(_fullscreen, fullscreen =>
		{
			_gameOwnsTheWholeScreen = fullscreen;
			RefreshTaskbarReapplyTimer();
		});
		_fullscreen.Start();
		ApplyCompetitiveMode();
		_audioEdge = CreateAudioEdge();
		if (_audioEdge != null)
		{
			_audioEdge.Threshold = (float)Math.Clamp(_settings.AudioEdgeThresholdPercent, 5, 100) / 100f;
			if (_settings.AudioEdgeRestorePending)
			{
				_audioEdge.ForceRestore(_settings.AudioEdgeRestoreVolume);
				_settings.AudioEdgeRestorePending = false;
				_store.Save(_settings);
			}
			_restorePointSaver = new RestorePointSaver(() => _store.Save(_settings));
			var rp = _restorePointSaver;
			_audioEdge.OnRestorePointCaptured += v => rp.Captured(v, _settings);
			_audioEdge.OnRestoreCompleted += () => rp.Completed(_settings);
			if (_settings.AudioEdgeEnabled) _audioEdge.Start();
		}
		_window = new MainWindow(_engine, _settings, _store, _audioEdge, ApplyTheme,
			_customTheme, _crosshair, BuildProfileCoordinator(),
			onHotkeyChanged: ReRegisterHotkey,
			onMainHotkeyChanged: ReRegisterMainHotkey,
			selection: GameSelectionInstance,
			onLowLatencyChanged: _ => ApplyCompositionKeeper(),
			onScreenshotHotkeyChanged: ReRegisterScreenshotHotkey,
			onNightVisionHotkeyChanged: ReRegisterNightVisionHotkey,
			zoomHooks: MakeZoomHooks(),
			onPresetBindingsChanged: ReRegisterPresetBindings,
			onMonitorColourChanged: () => _gammaRamp.RefreshMonitors(),
			onDisplaySliderChanged: () =>
			{
				try
				{
					VibrancePopup? p = _vibrancePopup;
					if (p != null && !p.IsDisposed) p.SyncFromEngine();
				}
				catch { }
			},
			customModes: CustomModeRegistrar.Real(),
			modeApply: ModeApplier.Real(),
			onBoostNextKeyChanged: () =>
			{
				RegisterDarkBoostNextKeyIfOffered();
				RegisterZoomKey();
			},
			onCompetitiveModeChanged: _ => ApplyCompetitiveMode());
		_window.PresetApplied += n => SetPresenceInBackground(n);
		_window.PhoneWindowOpener = OpenPhoneWindow;
		RecheckFullscreenWhenWeComeForward(_window);
		ShowColourReachOnPage();
		AcceptLinks();
		UpdateService.RecordPending = (release, file) =>
		{
			_settings.PendingUpdateInstaller = file;
			_settings.PendingUpdateVersion = release.Version.ToString();
			_settings.PendingUpdateSha256 = UpdateService.FileSha256(file);
			_settings.PendingUpdateSignedManifest = release.SignedManifest ?? "";
			_store.Save(_settings);
		};
		UpdateService.RecordRefusal = sha => { RefusedUpdate.Record(_settings, sha); _store.Save(_settings); };
		RefusedUpdate.ShowNotice = SayTheUpdateWasRefused;
		SetPresenceInBackground(
			!string.IsNullOrEmpty(_settings.ColourPresetGame) && !string.IsNullOrEmpty(_settings.ColourPresetName)
				? _settings.ColourPresetGame + " · " + _settings.ColourPresetName
				: null);
		_hotkeyWindow = new HotkeyWindow();
		_hotkeyWindow.HotkeyPressed += (s, e3) =>
		{
			if (e3 is HotkeyEventArgs { HotkeyId: MAIN_HOTKEY_ID }) ShowMainWindow();
			else if (e3 is HotkeyEventArgs { HotkeyId: SCREENSHOT_HOTKEY_ID }) TakeColourScreenshot();
			else if (e3 is HotkeyEventArgs { HotkeyId: PANIC_HOTKEY_ID }) RestoreMyMonitor();
			else if (e3 is HotkeyEventArgs { HotkeyId: >= PRESET_BINDING_ID_BASE } e6)
			{
				int idx = e6.HotkeyId - PRESET_BINDING_ID_BASE;
				if (idx < _settings.PresetKeyBindings.Count)
				{
					string name = _settings.PresetKeyBindings[idx].Name;
					SayWhatTheLookSaid(name, _window.ApplyScenePresetByName(name));
				}
			}
			else ShowVibrancePopup();
		};
		nint drainTo = _hotkeyWindow.Handle;
		_darkBoostKeys = new DarkBoostKeys(
			() => global::A.A.Available && _nightVisionCodeOk,
			() => _runningGames.Count > 0,
			() => RawKeyListener.Post(drainTo, 32848),
			relay => new RawKeyListener(relay));
		_darkBoostKeys.Want += OnDarkBoostKey;
		_darkBoostKeys.Next += OnDarkBoostNextKey;
		_hotkeyWindow.KeysWaiting += () => _darkBoostKeys?.Drain();
		_darkBoostKeys.ZoomOffered = ZoomAllowed;
		_darkBoostKeys.Zoom += OnZoomKey;
		_darkBoostKeys.ZoomIgnored += why => ZoomLog.Note("press dropped: " + why);
		HotkeyCaptureGate.Shared.Began += () => _zoom?.Release(smooth: false);
		RegisterZoomKey();
		bool registered = RegisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID, _settings.HotkeyModifierMask, _settings.HotkeyVirtualKey);
		HotkeyStatus.Record(HotkeySlot.QuickVibrance, GetHotkeyDisplay(), registered);
		if (!registered)
		{
			GlassDialog.Show(null, "Shortcut already taken",
				"Couldn't register " + GetHotkeyDisplay() + " - another app is probably already using it.\n\nYou can still open the slider from the tray icon, or pick a different combo on the Display tab.",
				GlassDialogButtons.Ok, GlassDialogTone.Warning);
		}
		if (_settings.MainHotkeyEnabled) TryRegisterMainHotkey();
		RegisterPanicHotkey();
		ReRegisterPresetBindings();
		if (_settings.ScreenshotHotkeyEnabled)
		{
			HotkeySlot? owner = HotkeyConflicts.Owner(_settings, HotkeySlot.Screenshot, _settings.ScreenshotHotkeyModifierMask, _settings.ScreenshotHotkeyVirtualKey);
			if (owner.HasValue)
			{
				GlassDialog.Show(null, "Two shortcuts, one combination",
					GetScreenshotHotkeyDisplay() + " is set for both the screenshot and " + HotkeyConflicts.Describe(owner.Value) +
					", so only one of them can work.\n\nPick a different combination for one of them on the Display tab. The tray menu runs both in the meantime.",
					GlassDialogButtons.Ok, GlassDialogTone.Warning);
			}
			else
			{
				HotkeyStatus.Record(HotkeySlot.Screenshot, GetScreenshotHotkeyDisplay(),
					RegisterHotKey(_hotkeyWindow.Handle, SCREENSHOT_HOTKEY_ID, _settings.ScreenshotHotkeyModifierMask, _settings.ScreenshotHotkeyVirtualKey));
			}
		}
		RegisterNightVisionHotkeyIfOffered();
		RegisterDarkBoostNextKeyIfOffered();
		_darkBoost.Settle();
		HotkeyPicker.DefaultProbe = HotkeyProbe.Real;
		_captureRelease = new HotkeyCaptureRelease(HotkeyCaptureGate.Shared, ReleaseOwnHotkeysForCapture, RestoreOwnHotkeysAfterCapture);
		ContextMenuStrip menu = NewTrayMenu();
		menu.Items.Add("Open", null, (s, e2) => LiveWindow().ShowAndFocus());
		_mainHotkeyMenuItem = (ToolStripMenuItem)menu.Items[menu.Items.Count - 1];
		UpdateMainHotkeyMenuItem();
		_hotkeyMenuItem = (ToolStripMenuItem)menu.Items.Add("Quick vibrance", null, (s, e2) => ShowVibrancePopup());
		SetHotkeyRow(_hotkeyMenuItem, "Quick vibrance", GetHotkeyDisplay());
		_screenshotHotkeyMenuItem = (ToolStripMenuItem)menu.Items.Add("Screenshot with colours", null, (s, e2) => TakeColourScreenshot());
		UpdateScreenshotHotkeyMenuItem();
		menu.Items.Add("Reset colours", null, (s, e2) => ResetColours());
		menu.Items.Add("Restore my monitor", null, (s, e2) => RestoreMyMonitor());
		if (FlavourPolicy.Current.TouchesTheTaskbar)
			menu.Items.Add("Reset taskbar", null, (s, e2) => ResetTaskbarStyle());
		MusicPlayer.AttachToTray(menu, _settings, enabled: true, null, null, () => _store.Save(_settings));
		menu.Items.Add(new ToolStripSeparator());
		menu.Items.Add("Check for updates", null, async (s, e2) => await UpdateService.CheckManuallyAsync());
		menu.Items.Add("Exit", null, (s, e2) => ExitThread());
		_trayIconFrame = new Icon(AppIcon.Value, SystemInformation.SmallIconSize);
		_trayIcon = new NotifyIcon
		{
			Icon = _trayIconFrame,
			Text = "PlexusX",
			Visible = true,
			ContextMenuStrip = menu
		};
		_trayIcon.DoubleClick += (s, e2) => LiveWindow().ShowAndFocus();
		UpdateTrayStateText();
		if (_settings.StartWithWindows && FlavourPolicy.Current.Startup == StartupMechanism.RunKey)
			StartupManager.EnsureCurrentPath();
		_splash = new SplashForm();
		if (LaunchOptions.StartInTray)
		{
			RunStartupAsync().ContinueWith(t =>
			{
				try { CrashLog.Write(t.Exception.GetBaseException()); } catch { }
			}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
		}
		else
		{
			_splash.Shown += async (s, e2) => await RunStartupAsync();
			_splash.Show();
		}
		if (FlavourPolicy.Current.SelfUpdates)
		{
			_backgroundChecker = new BackgroundUpdateChecker(_settings, _store, _trayIcon, _startupUpdateStep.Task);
			_backgroundChecker.Start();
		}
		_constructed = true;
	}

	internal static void RememberTheDriverBaseline(AppSettings settings, int currentLevel, Action save)
	{
		int next = DriverVibranceBaseline.Remember(settings.NvapiRestoreLevel, settings.NvapiRestorePending, currentLevel);
		if (next != settings.NvapiRestoreLevel) { settings.NvapiRestoreLevel = next; save(); }
	}

	internal static void RememberEveryDisplayBaseline(AppSettings settings, IVibranceController driver, Action save)
	{
		var reads = driver.ReadEveryDisplayLevel();
		if (reads.Count == 0) return;
		var next = DriverVibranceBaseline.RememberEach(settings.NvapiRestoreLevels, settings.NvapiRestorePending, reads);
		if (next.Count != settings.NvapiRestoreLevels.Count
			|| !next.All(p => settings.NvapiRestoreLevels.TryGetValue(p.Key, out int v) && v == p.Value))
		{
			settings.NvapiRestoreLevels = next;
			save();
		}
	}

	internal static void WhenTheDriverCameBack(AppSettings settings, IVibranceController driver, VibranceEngine engine, Action save)
	{
		try
		{
			RememberTheDriverBaseline(settings, driver.CurrentLevel, save);
			RememberEveryDisplayBaseline(settings, driver, save);
			engine.DriverBaseline = settings.NvapiRestoreLevel;
		}
		catch { }
	}

	internal static IVibranceController? TryCreateVibranceController()
	{
		try { return new VibranceController(); }
		catch { return null; }
	}

	internal static void HandTheEngineTheDriverBaseline(VibranceEngine engine, AppSettings settings, Action save)
	{
		engine.DriverBaseline = settings.NvapiRestoreLevel;
		engine.BeforeFirstDriverWrite = () =>
		{
			if (!settings.NvapiRestorePending) { settings.NvapiRestorePending = true; save(); }
		};
		engine.DriverLevelWeLastWrote = settings.DriverVibranceWeLastWrote;
		engine.AfterDriverWrite = level =>
		{
			settings.DriverVibranceWeLastWrote = level;
			if (!engine.IsDragging) save();
		};
		engine.WhenTheDriverIsTakenBack = theirs =>
		{
			settings.VibranceChosenByUser = false;
			settings.DriverVibranceWeLastWrote = null;
			settings.NvapiRestorePending = settings.OwnedMonitorVibrance != null && settings.OwnedMonitorVibrance.Count > 0;
			settings.NvapiRestoreLevel = theirs;
			save();
		};
	}

	internal static void RecordTheGoodbyeAsOurs(AppSettings settings, SettingsStore store, int? writtenOnTheFirstDisplay = null)
	{
		try
		{
			int? target = writtenOnTheFirstDisplay ?? DriverVibranceBaseline.RestoreOnExit(settings.NvapiRestoreLevel);
			if (target.HasValue && settings.DriverVibranceWeLastWrote != target)
			{
				settings.DriverVibranceWeLastWrote = target;
				store.Save(settings);
			}
		}
		catch { }
	}

	private static AudioEdgeService? CreateAudioEdge()
	{
		try { return new AudioEdgeService(new WindowsAudioOutput()); }
		catch { return null; }
	}

	internal static IVibranceController CreateVibranceController()
	{
		try { return new VibranceController(); }
		catch (Exception ex) { return new NullVibranceController(SafeStateForFailure(ex)); }
	}

	internal static VibranceDriverState SafeStateForFailure(Exception ex)
	{
		if (ex is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException or TypeInitializationException)
			return VibranceDriverState.NoNvidiaCard;
		try { return StateForFailure(ex); }
		catch { return VibranceDriverState.NoNvidiaCard; }
	}

	internal static VibranceDriverState StateForFailure(Exception ex)
	{
		if (ex is NoNvidiaDisplaysException) return VibranceDriverState.DisplayNotOnNvidia;
		if (ex is NVIDIAApiException) return VibranceDriverState.DisplayNotOnNvidia;
		return VibranceDriverState.NoNvidiaCard;
	}

	private void StartBetaGateWatch()
	{
		CheckBetaGateAsync();
		System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 7200000 };
		t.Tick += (s, e2) => CheckBetaGateAsync();
		t.Start();
		_betaGateTimer = t;
	}

	private async Task CheckBetaGateAsync()
	{
		try
		{
			await RemoteSwitches.RefreshAsync();
			await AppStatusService.RefreshAsync();
			if (!AppStatusService.ThisBuildIsBlocked())
			{
				ShowServerNoticeIfDue();
				return;
			}
			if (FlavourPolicy.Current.SelfUpdates)
				await ForcedUpdate.TryBeforeTheDeadEndAsync(_settings, () => _store.Save(_settings), UpdateService.HandOffCeiling);
			GlassDialog.Show(null, "PlexusX", AppStatusService.CachedMessage(), GlassDialogButtons.Ok, GlassDialogTone.Warning);
			ExitThread();
		}
		catch { }
	}

	private void ShowServerNoticeIfDue()
	{
		try
		{
			if (_trayIcon == null) return;
			var notice = ServerNotice.For(AppStatusService.LastFetched,
				_settings.ServerNoticeShownId ?? "",
				ServerNotice.ParseWhen(_settings.ServerNoticeShownAt),
				DateTime.UtcNow);
			if (!notice.HasValue) return;
			string url = notice.Value.Url;
			if (url.Length > 0)
			{
				EventHandler? onClick = null;
				EventHandler? onClosed = null;
				onClick = (s, e2) =>
				{
					_trayIcon.BalloonTipClicked -= onClick;
					LoopbackSignIn.OpenBrowser(url);
				};
				onClosed = (s, e2) =>
				{
					_trayIcon.BalloonTipClicked -= onClick;
					_trayIcon.BalloonTipClosed -= onClosed;
				};
				_trayIcon.BalloonTipClicked += onClick;
				_trayIcon.BalloonTipClosed += onClosed;
			}
			_trayIcon.ShowBalloonTip(10000, notice.Value.Title, notice.Value.Message, ToolTipIcon.Info);
			_settings.ServerNoticeShownId = notice.Value.Id;
			_settings.ServerNoticeShownAt = ServerNotice.Stamp(DateTime.UtcNow);
			_store.Save(_settings);
		}
		catch { }
	}

	private static ISaturationOverlay? TryCreateDxOverlay(out DxInitFailureKind failure, out string failureMessage, out int failureCode)
	{
		DxOverlay dx;
		try { dx = new DxOverlay(); }
		catch (Exception ex)
		{
			failure = DxInitFailureKind.Unknown;
			failureMessage = ex.GetType().Name + (string.IsNullOrEmpty(ex.Message) ? "" : ": " + ex.Message);
			failureCode = ex.HResult;
			try { CrashLog.Write(ex); } catch { }
			return null;
		}
		if (dx.IsAvailable)
		{
			failure = DxInitFailureKind.None;
			failureMessage = "";
			failureCode = 0;
			return dx;
		}
		failureCode = dx.LastFailureCode;
		failure = dx.LastFailure;
		failureMessage = dx.LastFailureMessage;
		dx.Dispose();
		return null;
	}

	internal static ISaturationOverlay TryCreateOverlay() => TryCreateOverlay(() => false);

	internal static ISaturationOverlay TryCreateOverlay(Func<bool> qualityColour)
		=> ComposeOverlay(qualityColour, TryCreateDxOverlay, () => new MagOverlay());

	internal static void RecordOverlayOutcome(AppSettings settings, ISaturationOverlay overlay)
	{
		var d = overlay as IDisplayOverlay;
		settings.OverlayMode = OverlayModeResolver.Resolve(overlay);
		settings.DxFailure = d?.LastFailure ?? DxInitFailureKind.None;
		settings.DxFailureMessage = d?.LastFailureMessage ?? "";
		settings.DxFailureCode = d?.LastFailureCode ?? 0;
	}

	internal static ISaturationOverlay ComposeOverlay(Func<bool> qualityColour, DxAttempt tryDx, Func<ISaturationOverlay> makeFallback)
		=> ComposeOverlay(new DxOverlayGate(qualityColour), tryDx, makeFallback);

	internal static ISaturationOverlay ComposeOverlay(DxOverlayGate gate, DxAttempt tryDx, Func<ISaturationOverlay> makeFallback)
	{
		ISaturationOverlay? first = Attempt(out var failure, out var message, out var code);
		if (first != null) return first;
		try { return new UpgradingOverlay(makeFallback(), () => Attempt(out _, out _, out _), failure, message, code); }
		catch (Exception ex) { CrashLog.Write(ex); return new NoOverlay(); }

		ISaturationOverlay? Attempt(out DxInitFailureKind f, out string m, out int c)
		{
			if (!gate.IsOpen) { f = DxInitFailureKind.None; m = ""; c = 0; return null; }
			return tryDx(out f, out m, out c);
		}
	}

	internal static ISaturationOverlay NoOverlayForTest() => new NoOverlay();

	private void StartOverlayUpgradeWatch()
	{
		if (_overlay is not UpgradingOverlay upgrading || !upgrading.CanUpgrade || !new DxOverlayGate(() => _settings.QualityColour).IsOpen)
			return;
		int delayMs = 2000, attempts = 0;
		System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = delayMs };
		t.Tick += (s, e2) =>
		{
			attempts++;
			if (upgrading.TryUpgrade())
			{
				t.Stop();
				t.Dispose();
				_engine.ForgetAppliedState();
				_settings.OverlayMode = OverlayMode.Dx;
				_settings.DxFailure = DxInitFailureKind.None;
				_settings.DxFailureMessage = "";
				RuntimeFacts.NoteOverlay(OverlayMode.Dx, (_overlay as IDisplayOverlay)?.EffectIsLive ?? true);
				_store.Save(_settings);
				ShowColourReachOnPage();
			}
			else if (attempts >= 6 || !upgrading.CanUpgrade)
			{
				t.Stop();
				t.Dispose();
			}
			else
			{
				delayMs = Math.Min(delayMs * 2, 30000);
				t.Interval = delayMs;
			}
		};
		t.Start();
	}

	private async Task RunStartupAsync()
	{
		DateTime startedAt = DateTime.UtcNow;
		try
		{
			RestoreOwedDisplayMode();
			if (FlavourPolicy.Current.SelfUpdates) UpdateService.CleanupObsoleteInstallers();
			StartBetaGateWatch();

			string pendingBefore = _settings.PendingUpdateInstaller;
			string pendingVersion = _settings.PendingUpdateVersion;
			if (UpdateService.LaunchLeavesAfter(await UpdateService.HandOffToInstallerAsync(
				_settings, UpdateService.HandOffCeiling, askTheManifest: true,
				() => _splash.SetStatus("Installing PlexusX " + pendingVersion + "…"))))
			{
				Application.Exit();
				return;
			}
			if (pendingBefore != _settings.PendingUpdateInstaller) _store.Save(_settings);

			bool selfUpdates = FlavourPolicy.Current.SelfUpdates;
			if (selfUpdates) _splash.SetStatus("Checking for updates…");

			(bool failed, ReleaseInfo? release) = await UpdateService.StartupChecksTogetherAsync(
				() => Task.FromResult(true),
				selfUpdates ? UpdateService.TryGetUpdateAsync : () => Task.FromResult<ReleaseInfo?>(null));

			if (selfUpdates)
			{
				release = RefusedUpdate.Unless(_settings, release);
				if (release != null && await InstallUpdateAsync(release)) return;
			}
		}
		finally
		{
			_startupUpdateStep.TrySetResult();
		}

		_splash.SetStatus("Starting…");
		string? notes = await WhatsNewNotesAsync();
		TimeSpan elapsed = DateTime.UtcNow - startedAt;
		TimeSpan floor = TimeSpan.FromMilliseconds(1400.0);
		if (elapsed < floor) await Task.Delay(floor - elapsed);
		_splash.Close();
		ShowStagedUpdateNotice();

		if (!_settings.OnboardingComplete)
		{
			string currentName = Theme.CurrentName;
			using (var onboarding = new OnboardingForm(_settings, _store)) onboarding.ShowDialog();
			_settings.LastSeenVersion = UpdateService.CurrentVersion.ToString();
			_store.Save(_settings);
			if (Theme.CurrentName != currentName) RebuildWindow();
			else LiveWindow().ShowAndFocus();
		}
		else
		{
			if (LaunchOptions.StartInTray) return;
			if (notes != null)
			{
				using var whatsNew = new WhatsNewWindow(UpdateService.CurrentVersion, notes);
				whatsNew.ShowDialog();
			}
			LiveWindow();
			_window.ShowAndFocus();
		}
	}

	private void RestoreOwedDisplayMode()
	{
		string owed = _settings.PendingDisplayRestore;
		if (string.IsNullOrEmpty(owed)) return;
		try
		{
			var parsed = DisplayController.ParseOwed(owed);
			if (!parsed.HasValue) return;
			var (device, mode) = parsed.GetValueOrDefault();
			if (!DisplayController.IsAttached(device, DisplayController.Monitors())) return;
			DisplayMode? current = DisplayController.Current(device);
			if (current.HasValue)
			{
				DisplayMode c = current.GetValueOrDefault();
				if (c.Width != mode.Width || c.Height != mode.Height)
					DisplayController.Restore(mode, device);
			}
		}
		catch { }
		finally
		{
			_settings.PendingDisplayRestore = "";
			_store.Save(_settings);
		}
	}

	private bool SayTheUpdateWasRefused(string title, string body)
	{
		try
		{
			if (_trayIcon == null) return false;
			EventHandler? onClick = null;
			EventHandler? onClosed = null;
			onClick = (s, e2) =>
			{
				_trayIcon.BalloonTipClicked -= onClick;
				LoopbackSignIn.OpenBrowser(RefusedUpdate.NoticeLink);
			};
			onClosed = (s, e2) =>
			{
				_trayIcon.BalloonTipClicked -= onClick;
				_trayIcon.BalloonTipClosed -= onClosed;
			};
			_trayIcon.BalloonTipClicked += onClick;
			_trayIcon.BalloonTipClosed += onClosed;
			_trayIcon.ShowBalloonTip(10000, title, body, ToolTipIcon.Info);
			return true;
		}
		catch { return false; }
	}

	private async Task<bool> InstallUpdateAsync(ReleaseInfo update)
	{
		string label = $"Downloading update {update.Version}…";
		_splash.SetStatus(label, 0);
		bool splashWaiting = true;
		Task<string?> download = UpdateService.DownloadAndStageAsync(update, new Progress<int>(p =>
		{
			if (splashWaiting) _splash.SetStatus(label, p);
		}));
		string? file;
		try { file = await download.WaitAsync(UpdateService.SplashDownloadBudget); }
		catch (TimeoutException)
		{
			TaskScheduler sched = SynchronizationContext.Current != null
				? TaskScheduler.FromCurrentSynchronizationContext()
				: TaskScheduler.Default;
			download.ContinueWith(t =>
			{
				if (t.Status == TaskStatus.RanToCompletion && t.Result is string path)
					UpdateService.RecordPending?.Invoke(update, path);
			}, sched);
			return false;
		}
		finally { splashWaiting = false; }

		if (file == null) return false;
		_settings.PendingUpdateInstaller = file;
		_settings.PendingUpdateVersion = update.Version.ToString();
		_settings.PendingUpdateSha256 = UpdateService.FileSha256(file);
		_settings.PendingUpdateSignedManifest = update.SignedManifest ?? "";
		_store.Save(_settings);

		if (!UpdateService.RunInstallerSilently(file))
		{
			_settings.PendingUpdateInstaller = "";
			_settings.PendingUpdateVersion = "";
			_settings.PendingUpdateSha256 = "";
			_settings.PendingUpdateSignedManifest = "";
			_store.Save(_settings);
			return false;
		}

		if (RemoteSwitches.IsOn("updates") && SafeToRestart.IsNow())
		{
			_splash.SetStatus($"Installing PlexusX {update.Version}…");
			await UpdateService.HandOffToInstallerAsync(_settings, UpdateService.HandOffCeiling);
			_store.Save(_settings);
		}
		if (string.IsNullOrEmpty(_settings.PendingUpdateInstaller)) return false;
		_splash.SetStatus($"Restart PlexusX to finish installing v{update.Version}");
		_stagedUpdateVersion = update.Version.ToString();
		return false;
	}

	private void AcceptLinks()
	{
		_uiAnchor = new MessageOnlyAnchor();
		_ = _uiAnchor.Handle;
		LinkInbox.Handler = link =>
		{
			PlexusLink parsed = PlexusLinks.Parse(link);
			if (parsed.Action != LinkAction.Apply) return;
			try
			{
				Control? anchor = _uiAnchor;
				if (anchor == null || anchor.IsDisposed || !anchor.IsHandleCreated) return;
				anchor.BeginInvoke(() =>
				{
					if (_displayRestored) return;
					try
					{
						MainWindow? window = _window;
						if (window != null && !window.IsDisposed) window.ApplySharedSetup(parsed.Code);
					}
					catch { }
				});
			}
			catch { }
		};
		RemoteCommands.OnShow = WhenAskedToShow;
		RemoteCommands.OnQuit = WhenAskedToQuit;
	}

	private void WhenAskedToShow() => OnTheUiThreadOrNotAtAll(() => LiveWindow().ShowAndFocus());
	private void WhenAskedToQuit() => OnTheUiThreadOrNotAtAll(ExitThreadOnceConstructed);

	private void ExitThreadOnceConstructed()
	{
		if (_constructed) { ExitThread(); return; }
		try { _vibrancePopup?.Dispose(); } catch { }
		RestoreDisplayNow();
	}

	private void OnTheUiThreadOrNotAtAll(Action work)
	{
		Control? anchor = _uiAnchor;
		if (anchor == null || anchor.IsDisposed || !anchor.IsHandleCreated) return;
		anchor.BeginInvoke(() => { try { work(); } catch { } });
	}

	private void OpenPhoneWindow()
	{
		if (RemoteSwitches.IsOff("phone"))
		{
			GlassDialog.Show(_window, "Phone control is paused",
				"We have switched phone control off for now while we fix something. It will come back on its own - you do not need to do anything, and nothing else about PlexusX is affected.");
			return;
		}
		PhoneWindow? existing = _phoneWindow;
		if (existing != null && !existing.IsDisposed)
		{
			try
			{
				if (existing.WindowState == FormWindowState.Minimized) existing.WindowState = FormWindowState.Normal;
				existing.Activate();
				return;
			}
			catch { return; }
		}
		_phoneControl ??= new PhoneControl(_engine, PhonePresetNames, ApplyPhonePreset, () =>
		{
			try { _window?.OnEngineChanged(); } catch { }
			try
			{
				VibrancePopup? p = _vibrancePopup;
				if (p != null && !p.IsDisposed) p.SyncFromEngine();
			}
			catch { }
		});
		_phoneListener ??= new PhoneListener(_phoneControl);
		PhoneWindow window = new PhoneWindow(_phoneListener);
		window.FormClosed += (_, _) =>
		{
			if (_phoneWindow == window) _phoneWindow = null;
			try { window.Dispose(); } catch { }
		};
		_phoneWindow = window;
		MainWindow? owner = _window;
		if (owner != null && !owner.IsDisposed) window.Show(owner); else window.Show();
	}

	private IReadOnlyList<string> PhonePresetNames()
	{
		ScenePresetSet set = ServedLooks.ForGame(_settings.ColourPresetGame ?? "");
		List<string> list = new();
		foreach (DisplayPreset p in LookCorrection.OfferableWithoutANote(set.Presets)) list.Add(p.Name);
		return list;
	}

	private void ApplyPhonePreset(int index)
	{
		IReadOnlyList<DisplayPreset> presets = LookCorrection.OfferableWithoutANote(ServedLooks.ForGame(_settings.ColourPresetGame ?? "").Presets);
		if (index < 0 || index >= presets.Count) return;
		DisplayPreset p = presets[index];
		_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Stronger;
		_engine.Saturation = p.Saturation;
		_engine.Vibrance = p.Vibrance;
		_engine.Brightness = p.Brightness;
		_engine.Gamma = p.Gamma;
		_engine.Contrast = p.Contrast;
		_engine.Temperature = p.Temperature;
		_settings.VibrancePercent = _engine.Vibrance;
		_settings.VibranceChosenByUser = true;
		_settings.SaturationPercent = _engine.Saturation;
		_settings.BrightnessPercent = _engine.Brightness;
		_settings.GammaPercent = _engine.Gamma;
		_settings.ContrastPercent = _engine.Contrast;
		_settings.Temperature = _engine.Temperature;
		ColourVisionMode? mode = LookCorrection.GiveTheirsBack(_settings);
		if (mode.HasValue) _engine.ColourVision = mode.GetValueOrDefault();
		_store.Save(_settings);
	}

	private void StopAcceptingLinks()
	{
		LinkInbox.Handler = null;
		RemoteCommands.OnShow = null;
		RemoteCommands.OnQuit = null;
	}

	private void DropTheUiAnchor()
	{
		Control? a = _uiAnchor;
		_uiAnchor = null;
		try { a?.Dispose(); } catch { }
	}

	internal static bool? CrosshairOnTick(bool enabled, bool visible)
	{
		if (!enabled || visible) return null;
		return true;
	}

	private void StopPhoneControl()
	{
		PhoneWindow? w = _phoneWindow;
		if (w != null && !w.IsDisposed)
		{
			_phoneWindow = null;
			try { w.Close(); } catch { }
			try { w.Dispose(); } catch { }
		}
		_phoneWindow = null;
		try { _phoneListener?.Dispose(); } catch { }
		try { _phoneControl?.Dispose(); } catch { }
		_phoneListener = null;
		_phoneControl = null;
	}

	private void ShowStagedUpdateNotice()
	{
		if (string.IsNullOrEmpty(_stagedUpdateVersion) || _trayIcon == null) return;
		try
		{
			_trayIcon.ShowBalloonTip(10000,
				"PlexusX " + _stagedUpdateVersion + " is ready",
				"It installs the next time you open PlexusX. Close it and open it again whenever suits you.",
				ToolTipIcon.Info);
		}
		catch { }
		_stagedUpdateVersion = null;
	}

	private async Task<string?> WhatsNewNotesAsync()
	{
		string v = UpdateService.CurrentVersion.ToString();
		if (_settings.LastSeenVersion == v) return null;
		bool fresh = string.IsNullOrEmpty(_settings.LastSeenVersion);
		_settings.LastSeenVersion = v;
		_store.Save(_settings);
		if (fresh) return null;
		return await UpdateService.GetNotesForVersionAsync(UpdateService.CurrentVersion);
	}

	private void ApplyTheme(string themeName)
	{
		_settings.ThemeName = themeName;
		_store.Save(_settings);
		Theme.Apply(themeName);
		System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 1 };
		t.Tick += (s, e2) =>
		{
			t.Stop();
			t.Dispose();
			RebuildWindow();
		};
		t.Start();
	}

	private void RebuildWindow() => RebuildWindow(quietly: false);

	private void RebuildWindow(bool quietly)
	{
		MainWindow old = _window;
		_window = BuildWindow();
		ShowColourReachOnPage();
		if (quietly) old.HandPendingNoticesTo(_window);
		else _window.ShowAndFocus();
		old.Dispose();
	}

	private MainWindow BuildWindow()
	{
		MainWindow window = new MainWindow(_engine, _settings, _store, _audioEdge, ApplyTheme,
			_customTheme, _crosshair, _profileCoordinator,
			onHotkeyChanged: ReRegisterHotkey,
			onMainHotkeyChanged: ReRegisterMainHotkey,
			selection: GameSelectionInstance,
			onLowLatencyChanged: _ => ApplyCompositionKeeper(),
			onScreenshotHotkeyChanged: ReRegisterScreenshotHotkey,
			onNightVisionHotkeyChanged: ReRegisterNightVisionHotkey,
			zoomHooks: MakeZoomHooks(),
			onPresetBindingsChanged: ReRegisterPresetBindings,
			onMonitorColourChanged: () => _gammaRamp.RefreshMonitors(),
			onDisplaySliderChanged: () =>
			{
				try
				{
					VibrancePopup? p = _vibrancePopup;
					if (p != null && !p.IsDisposed) p.SyncFromEngine();
				}
				catch { }
			},
			customModes: CustomModeRegistrar.Real(),
			modeApply: ModeApplier.Real(),
			onBoostNextKeyChanged: () =>
			{
				RegisterDarkBoostNextKeyIfOffered();
				RegisterZoomKey();
			},
			onCompetitiveModeChanged: _ => ApplyCompetitiveMode());
		window.PresetApplied += n => SetPresenceInBackground(n);
		window.PhoneWindowOpener = OpenPhoneWindow;
		RecheckFullscreenWhenWeComeForward(window);
		return window;
	}

	private void ShowVibrancePopup()
	{
		if (_vibrancePopup != null && !_vibrancePopup.IsDisposed)
		{
			_vibrancePopup.SyncFromEngine();
		}
		else
		{
			_vibrancePopup = new VibrancePopup(_engine, _settings, _store, false, () =>
			{
				try { _window?.OnEngineChanged(); } catch { }
			});
			AskAgainWhenItComesForward(_vibrancePopup, () => _fullscreen);
			AskAgainWhenItComesForward(_vibrancePopup, () => _competitiveWatch);
		}
		_vibrancePopup.Show();
		_vibrancePopup.Activate();
	}

	private void ShowMainWindow() => LiveWindow().ShowAndFocus();

	private MainWindow LiveWindow()
	{
		MainWindow? window = _window;
		MainWindow next = ALiveWindow(window, BuildWindow);
		if (next == window) return next;
		_window = next;
		ShowColourReachOnPage();
		window?.HandPendingNoticesTo(next);
		return next;
	}

	internal static T ALiveWindow<T>(T? current, Func<T> build) where T : Form
		=> (current == null || current.IsDisposed) ? build() : current;

	private void OnTheColourThread(Action work)
	{
		Control? anchor = _uiAnchor;
		if (anchor == null || anchor.IsDisposed || !anchor.IsHandleCreated)
		{
			RunGuarded(work);
			return;
		}
		try
		{
			anchor.BeginInvoke(() => RunGuarded(work));
		}
		catch { RunGuarded(work); }
	}

	private void RunGuarded(Action work)
	{
		if (_displayRestored) return;
		try { work(); } catch { }
	}

	private ProfileEngineCoordinator BuildProfileCoordinator()
	{
		if (_profileCoordinator != null)
		{
			return _profileCoordinator;
		}
		GameProcessWatcher gameProcessWatcher = new GameProcessWatcher(SupportedGames.ProcessNames);
		ProfileApplyEngine engine = new ProfileApplyEngine(_engine);
		ProfileEngineCoordinator profileEngineCoordinator = new ProfileEngineCoordinator(
			gameProcessWatcher,
			engine,
			new GameProfileApplyGate(_settings),
			_settings,
			null,
			OnTheColourThread)
		{
			OnSettingsChanged = () =>
			{
				_store.Save(_settings);
			},
			OnLookChanged = () =>
			{
				try
				{
					_window?.OnEngineChanged();
				}
				catch
				{
				}
			}
		};
		gameProcessWatcher.OnGameClosed += (string id) =>
		{
			OnTheColourThread(() =>
			{
				NoteGameClosed(_fullscreen, id);
			});
		};
		gameProcessWatcher.OnGameLaunched += (string id) =>
		{
			OnTheColourThread(() =>
			{
				_runningGames.Add(id);
				_darkBoostKeys?.Settle();
				ArmTheZoomIfReady();
			});
		};
		gameProcessWatcher.OnGameClosed += (string id) =>
		{
			OnTheColourThread(() =>
			{
				_runningGames.Remove(id);
				_zoom?.Release(smooth: false);
				_darkBoostKeys?.Settle();
				ArmTheZoomIfReady();
			});
		};
		DisplayController.OnRestored = () =>
		{
			if (!string.IsNullOrEmpty(_settings.PendingDisplayRestore))
			{
				_settings.PendingDisplayRestore = "";
				_store.Save(_settings);
			}
		};
		profileEngineCoordinator.Start();
		_profileCoordinator = profileEngineCoordinator;
		return profileEngineCoordinator;
	}

	private void ArmTheZoomIfReady()
	{
		if (!_settings.ZoomEnabled)
		{
			return;
		}
		if (!_settings.HotkeysOnlyInGames || _runningGames.Count > 0)
		{
			_zoom ??= new FullscreenZoom(
				() => CrosshairService.IsExclusiveFullscreen(),
				() => _settings.ZoomSmoothEdges);
			_zoom.Arm();
		}
	}

	private void OnApplicationExit(object? sender, EventArgs e)
	{
		RestoreDisplayNow();
	}

	private void RestoreDisplayNow()
	{
		if (_displayRestored) return;
		_displayRestored = true;
		try { _zoomTimer?.Stop(); _zoomTimer?.Dispose(); _zoomTimer = null; } catch { }
		try { _zoom?.Dispose(); _zoom = null; } catch { }
		try { ZoomEmergency.ResetIfWeZoomed(); } catch { }
		try { _darkBoostKeys?.Dispose(); _darkBoostKeys = null; } catch { }
		try { _captureRelease?.Dispose(); _captureRelease = null; } catch { }
		try { _betaGateTimer?.Stop(); _betaGateTimer?.Dispose(); _betaGateTimer = null; } catch { }
		try { _taskbarReapplyTimer?.Stop(); _taskbarReapplyTimer?.Dispose(); _taskbarReapplyTimer = null; } catch { }
		try { StopColourWatchdog(); } catch { }
		try { _fullscreen?.Dispose(); _fullscreen = null; } catch { }
		try { _competitiveWatch?.Dispose(); _competitiveWatch = null; } catch { }
		try { _compositionKeeper?.Dispose(); _compositionKeeper = null; } catch { }
		try { _audioEdge?.Dispose(); } catch { }
		try { _crosshair?.Dispose(); } catch { }
		try { _phoneListener?.Dispose(); _phoneListener = null; } catch { }
		try { _phoneWindow?.Dispose(); _phoneWindow = null; } catch { }
		try { _backgroundChecker?.Dispose(); _backgroundChecker = null; } catch { }
		try
		{
			if (_hotkeyWindow != null && _hotkeyWindow.Handle != IntPtr.Zero)
			{
				UnregisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID);
				UnregisterHotKey(_hotkeyWindow.Handle, MAIN_HOTKEY_ID);
				UnregisterHotKey(_hotkeyWindow.Handle, SCREENSHOT_HOTKEY_ID);
				UnregisterHotKey(_hotkeyWindow.Handle, PANIC_HOTKEY_ID);
				for (int i = 0; i < _boundPresetBindings; i++)
					UnregisterHotKey(_hotkeyWindow.Handle, PRESET_BINDING_ID_BASE + i);
				_hotkeyWindow.DestroyHandle();
			}
		}
		catch { }
		try { _engine?.HandBack(); } catch { }
		try { RecordTheGoodbyeAsOurs(_settings, _store); } catch { }
		try { _discordPresence?.Dispose(); } catch { }
		try { if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); } } catch { }
		try { _trayIconFrame?.Dispose(); } catch { }
	}

	private void PushDarkBoostLook()
	{
		if (_settings.DarkBoostSetups == null || _settings.DarkBoostSetups.Count == 0)
			return;
		int idx = Math.Clamp(_settings.DarkBoostSelected, 0, _settings.DarkBoostSetups.Count - 1);
		DarkBoostSetup setup = _settings.DarkBoostSetups[idx];
		_engine.SetDarkBoostLook(setup.Tint, setup.Strength, setup.BlackLevel, _settings.DarkBoostFade);
	}

	private string GetHotkeyDisplay()
		=> HotkeyPicker.GetDisplay(_settings.HotkeyModifierMask, _settings.HotkeyVirtualKey);

	private string GetScreenshotHotkeyDisplay()
		=> HotkeyPicker.GetDisplay(_settings.ScreenshotHotkeyModifierMask, _settings.ScreenshotHotkeyVirtualKey);

	private void UpdateMainHotkeyMenuItem()
	{
		if (_mainHotkeyMenuItem == null) return;
		string? shortcut = _settings.MainHotkeyEnabled
			? HotkeyPicker.GetDisplay(_settings.MainHotkeyModifierMask, _settings.MainHotkeyVirtualKey)
			: null;
		SetHotkeyRow(_mainHotkeyMenuItem, "Open", shortcut);
	}

	private void UpdateScreenshotHotkeyMenuItem()
	{
		if (_screenshotHotkeyMenuItem == null) return;
		string? shortcut = _settings.ScreenshotHotkeyEnabled ? GetScreenshotHotkeyDisplay() : null;
		SetHotkeyRow(_screenshotHotkeyMenuItem, "Screenshot with colours", shortcut);
	}

	private void UpdateTrayStateText()
	{
		if (_trayIcon == null) return;
		string text = "PlexusX";
		_trayIcon.Text = text.Length <= 63 ? text : text.Substring(0, 63);
	}

	private bool ReRegisterHotkey(uint mask, uint vk)
	{
		UnregisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID);
		bool ok = RegisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID, mask, vk);
		if (ok)
		{
			_settings.HotkeyModifierMask = mask;
			_settings.HotkeyVirtualKey = vk;
			_store.Save(_settings);
			HotkeyStatus.Record(HotkeySlot.QuickVibrance, GetHotkeyDisplay(), true);
			if (_hotkeyMenuItem != null) SetHotkeyRow(_hotkeyMenuItem, "Quick vibrance", GetHotkeyDisplay());
		}
		else
		{
			RegisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID, _settings.HotkeyModifierMask, _settings.HotkeyVirtualKey);
		}
		return ok;
	}

	private bool TryRegisterMainHotkey()
	{
		UnregisterHotKey(_hotkeyWindow.Handle, MAIN_HOTKEY_ID);
		if (!_settings.MainHotkeyEnabled || _settings.MainHotkeyVirtualKey == 0)
		{
			HotkeyStatus.Forget(HotkeySlot.MainWindow);
			return true;
		}
		bool ok = RegisterHotKey(_hotkeyWindow.Handle, MAIN_HOTKEY_ID, _settings.MainHotkeyModifierMask, _settings.MainHotkeyVirtualKey);
		HotkeyStatus.Record(HotkeySlot.MainWindow, HotkeyPicker.GetDisplay(_settings.MainHotkeyModifierMask, _settings.MainHotkeyVirtualKey), ok);
		return ok;
	}

	private bool ReRegisterMainHotkey(uint mask, uint vk, bool enabled)
	{
		UnregisterHotKey(_hotkeyWindow.Handle, MAIN_HOTKEY_ID);
		if (!enabled || vk == 0)
		{
			_settings.MainHotkeyEnabled = false;
			_store.Save(_settings);
			HotkeyStatus.Forget(HotkeySlot.MainWindow);
			UpdateMainHotkeyMenuItem();
			return true;
		}
		bool ok = RegisterHotKey(_hotkeyWindow.Handle, MAIN_HOTKEY_ID, mask, vk);
		if (ok)
		{
			_settings.MainHotkeyEnabled = true;
			_settings.MainHotkeyModifierMask = mask;
			_settings.MainHotkeyVirtualKey = vk;
			_store.Save(_settings);
			HotkeyStatus.Record(HotkeySlot.MainWindow, HotkeyPicker.GetDisplay(mask, vk), true);
			UpdateMainHotkeyMenuItem();
		}
		else if (_settings.MainHotkeyEnabled)
		{
			RegisterHotKey(_hotkeyWindow.Handle, MAIN_HOTKEY_ID, _settings.MainHotkeyModifierMask, _settings.MainHotkeyVirtualKey);
		}
		return ok;
	}

	private bool ReRegisterScreenshotHotkey(uint mask, uint vk)
	{
		UnregisterHotKey(_hotkeyWindow.Handle, SCREENSHOT_HOTKEY_ID);
		if (vk == 0)
		{
			_settings.ScreenshotHotkeyEnabled = false;
			_store.Save(_settings);
			HotkeyStatus.Forget(HotkeySlot.Screenshot);
			UpdateScreenshotHotkeyMenuItem();
			return true;
		}
		bool ok = RegisterHotKey(_hotkeyWindow.Handle, SCREENSHOT_HOTKEY_ID, mask, vk);
		if (ok)
		{
			_settings.ScreenshotHotkeyEnabled = true;
			_settings.ScreenshotHotkeyModifierMask = mask;
			_settings.ScreenshotHotkeyVirtualKey = vk;
			_store.Save(_settings);
			HotkeyStatus.Record(HotkeySlot.Screenshot, GetScreenshotHotkeyDisplay(), true);
			UpdateScreenshotHotkeyMenuItem();
		}
		else if (_settings.ScreenshotHotkeyEnabled)
		{
			RegisterHotKey(_hotkeyWindow.Handle, SCREENSHOT_HOTKEY_ID, _settings.ScreenshotHotkeyModifierMask, _settings.ScreenshotHotkeyVirtualKey);
		}
		return ok;
	}

	private bool ReRegisterNightVisionHotkey(uint mask, uint vk)
	{
		if (vk == 0)
		{
			_darkBoostKeys?.Unbind();
			_settings.NightVisionHotkeyEnabled = false;
			_settings.NightVisionHotkeyModifierMask = 0;
			_settings.NightVisionHotkeyVirtualKey = 0;
			_store.Save(_settings);
			HotkeyStatus.Forget(HotkeySlot.NightVision);
			return true;
		}
		bool ok = _darkBoostKeys?.Bind(mask, (int)vk, _settings.NightVisionHotkeyHold, _settings.HotkeysOnlyInGames) ?? false;
		if (ok)
		{
			_settings.NightVisionHotkeyEnabled = true;
			_settings.NightVisionHotkeyModifierMask = mask;
			_settings.NightVisionHotkeyVirtualKey = vk;
			_store.Save(_settings);
			HotkeyStatus.Record(HotkeySlot.NightVision, HotkeyPicker.GetDisplay(mask, vk), true);
		}
		return ok;
	}

	private void RegisterNightVisionHotkeyIfOffered()
	{
		if (!global::A.A.Available || !_nightVisionCodeOk || !_settings.NightVisionHotkeyEnabled || _settings.NightVisionHotkeyVirtualKey == 0)
		{
			_darkBoostKeys?.Unbind();
			global::A.A.ForgetIfUnavailable();
			return;
		}
		bool ok = _darkBoostKeys?.Bind(_settings.NightVisionHotkeyModifierMask, (int)_settings.NightVisionHotkeyVirtualKey,
			_settings.NightVisionHotkeyHold, _settings.HotkeysOnlyInGames) ?? false;
		HotkeyStatus.Record(HotkeySlot.NightVision,
			HotkeyPicker.GetDisplay(_settings.NightVisionHotkeyModifierMask, _settings.NightVisionHotkeyVirtualKey), ok);
	}

	private void RegisterDarkBoostNextKeyIfOffered()
	{
		if (!global::A.A.Available || !_nightVisionCodeOk || _settings.DarkBoostNextKeyVirtualKey == 0)
		{
			_darkBoostKeys?.UnbindNext();
			return;
		}
		_darkBoostKeys?.BindNext(_settings.DarkBoostNextKeyModifierMask, (int)_settings.DarkBoostNextKeyVirtualKey, _settings.HotkeysOnlyInGames);
	}

	private bool ReRegisterPresetBindings()
	{
		for (int i = 0; i < _boundPresetBindings; i++)
			UnregisterHotKey(_hotkeyWindow.Handle, PRESET_BINDING_ID_BASE + i);
		_boundPresetBindings = 0;
		bool allOk = true;
		for (int i = 0; i < _settings.PresetKeyBindings.Count; i++)
		{
			PresetKeyBinding b = _settings.PresetKeyBindings[i];
			if (b.VirtualKey == 0) continue;
			bool ok = RegisterHotKey(_hotkeyWindow.Handle, PRESET_BINDING_ID_BASE + i, b.ModifierMask, b.VirtualKey);
			if (!ok) allOk = false;
			_boundPresetBindings = i + 1;
		}
		return allOk;
	}

	private void ReleaseOwnHotkeysForCapture()
	{
		try
		{
			UnregisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID);
			UnregisterHotKey(_hotkeyWindow.Handle, MAIN_HOTKEY_ID);
			UnregisterHotKey(_hotkeyWindow.Handle, SCREENSHOT_HOTKEY_ID);
			for (int i = 0; i < _boundPresetBindings; i++)
				UnregisterHotKey(_hotkeyWindow.Handle, PRESET_BINDING_ID_BASE + i);
			_darkBoostKeys?.Unbind();
			_darkBoostKeys?.UnbindNext();
			_darkBoostKeys?.UnbindZoom();
		}
		catch { }
	}

	private void RestoreOwnHotkeysAfterCapture()
	{
		try
		{
			RegisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID, _settings.HotkeyModifierMask, _settings.HotkeyVirtualKey);
			if (_settings.MainHotkeyEnabled) TryRegisterMainHotkey();
			if (_settings.ScreenshotHotkeyEnabled)
				RegisterHotKey(_hotkeyWindow.Handle, SCREENSHOT_HOTKEY_ID, _settings.ScreenshotHotkeyModifierMask, _settings.ScreenshotHotkeyVirtualKey);
			ReRegisterPresetBindings();
			RegisterNightVisionHotkeyIfOffered();
			RegisterDarkBoostNextKeyIfOffered();
			RegisterZoomKey();
		}
		catch { }
	}

	private void OnDarkBoostKey(bool on)
	{
		if (!_settings.DarkBoostBanRiskAccepted) return;
		bool next = _settings.NightVisionHotkeyHold ? on : !_engine.NightVision;
		if (_engine.NightVision == next) return;
		_engine.NightVision = next;
		_settings.NightVisionOn = next;
		try { _store.Save(_settings); } catch { }
	}

	private void OnDarkBoostNextKey()
	{
		if (_settings.DarkBoostSetups == null || _settings.DarkBoostSetups.Count <= 1) return;
		_settings.DarkBoostSelected = (_settings.DarkBoostSelected + 1) % _settings.DarkBoostSetups.Count;
		PushDarkBoostLook();
		try { _store.Save(_settings); } catch { }
	}

	private bool ZoomAllowed() => _settings.ZoomEnabled;

	private ZoomHooks MakeZoomHooks()
	{
		return new ZoomHooks(
			(mask, vk) =>
			{
				_settings.ZoomHotkeyModifierMask = mask;
				_settings.ZoomHotkeyVirtualKey = vk;
				_store.Save(_settings);
				RegisterZoomKey();
				return vk == 0 || (_zoomKey?.IsBound ?? false);
			},
			() => _zoom?.WhyRefused ?? "",
			() => RegisterZoomKey());
	}

	private void RegisterZoomKey()
	{
		_zoomKey ??= new ZoomKeyBinder(
			ZoomAllowed,
			() => (_settings.ZoomHotkeyModifierMask, _settings.ZoomHotkeyVirtualKey),
			(mask, vk) => HotkeyConflicts.Owner(_settings, HotkeySlot.Zoom, mask, vk).HasValue,
			(mask, vk) => _darkBoostKeys?.BindZoom(mask, (int)vk, _settings.HotkeysOnlyInGames) ?? false,
			() => _darkBoostKeys?.UnbindZoom(),
			() => _zoom?.Release(smooth: false));
		_zoomKey.Apply();
	}

	private void OnZoomKey(bool down)
	{
		if (!ZoomAllowed()) return;
		_zoom ??= new FullscreenZoom(
			() => CrosshairService.IsExclusiveFullscreen(),
			() => _settings.ZoomSmoothEdges);
		if (down)
		{
			_zoom.Press(_settings.ZoomLevel, _settings.ZoomSmooth);
			EnsureZoomTimer();
		}
		else
		{
			_zoom.Release(_settings.ZoomSmooth);
			EnsureZoomTimer();
		}
	}

	private void EnsureZoomTimer()
	{
		if (_zoom == null || !_zoom.NeedsTicks)
		{
			_zoomTimer?.Stop();
			return;
		}
		if (_zoomTimer == null)
		{
			_zoomTimer = new System.Windows.Forms.Timer { Interval = 8 };
			_zoomTimer.Tick += (_, _) =>
			{
				long now = Stopwatch.GetTimestamp();
				double ms = _zoomLastTicks == 0 ? 8.0 : (now - _zoomLastTicks) * 1000.0 / Stopwatch.Frequency;
				_zoomLastTicks = now;
				_zoom?.Tick(TimeSpan.FromMilliseconds(Math.Clamp(ms, 1.0, 50.0)));
				if (_zoom == null || !_zoom.NeedsTicks) _zoomTimer?.Stop();
			};
		}
		_zoomLastTicks = Stopwatch.GetTimestamp();
		if (!_zoomTimer.Enabled) _zoomTimer.Start();
	}

	private void TakeColourScreenshot()
	{
		try
		{
			string path = ColourScreenshot.Take(_engine, DateTime.Now);
			_trayIcon?.ShowBalloonTip(3000, "Screenshot saved", Path.GetFileName(path) + " (copied to clipboard)", ToolTipIcon.Info);
		}
		catch (Exception ex)
		{
			try { CrashLog.Write(ex); } catch { }
		}
	}
}