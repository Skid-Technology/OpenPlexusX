using System;
using System.Collections.Generic;
using System.Linq;
using VibranceHud.Games;

namespace VibranceHud;

public sealed class ProfileEngineCoordinator
{
	internal sealed record RuleDisplay(
		Func<IReadOnlyList<MonitorDescriptor>> Monitors,
		Func<string?, DisplayMode?> Current,
		Func<int, int, string?, bool> Apply,
		Action<string, DisplayMode, string?> RestoreWhenGameExits);

	private readonly GameProcessWatcher _watcher;
	private readonly ProfileApplyEngine _engine;
	private readonly GameProfileApplyGate _gate;
	private readonly AppSettings? _settings;
	private readonly Func<string, GameProfile?> _lookup;
	private Action? _onSettingsChanged;
	private readonly Action<Action> _marshal;
	private readonly Action<string> _applyMonitorRule;
	private volatile bool _stopped;

	internal static readonly RuleDisplay RealRuleDisplay = new RuleDisplay(
		() => DisplayController.Monitors(),
		(string? d) => DisplayController.Current(d),
		(int w, int h, string? d) => DisplayController.Apply(w, h, 0, d),
		(string process, DisplayMode original, string? d) =>
		{
			DisplayController.RestoreWhenGameExits(
				process,
				original,
				TimeSpan.FromMinutes(5.0),
				d,
				SupportedGames.All.FirstOrDefault((SupportedGame g) =>
					string.Equals(g.ProcessName, process, StringComparison.OrdinalIgnoreCase))?.AlsoProcessNames);
		});

	internal Action? OnLookChanged { get; set; }

	internal Action? OnSettingsChanged
	{
		get => _onSettingsChanged;
		set => _onSettingsChanged = value;
	}

	public bool IsRunning => _watcher.IsRunning;

	public ProfileEngineCoordinator(
		GameProcessWatcher watcher,
		ProfileApplyEngine engine,
		GameProfileApplyGate gate,
		AppSettings? settings = null,
		Func<string, GameProfile?>? lookup = null,
		Action<Action>? marshal = null,
		Action<string>? applyMonitorRule = null)
	{
		_watcher = watcher ?? throw new ArgumentNullException("watcher");
		_engine = engine ?? throw new ArgumentNullException("engine");
		_gate = gate ?? throw new ArgumentNullException("gate");
		_settings = settings;
		_lookup = lookup ?? new Func<string, GameProfile?>(GameProfileStore.Get);
		_marshal = marshal ?? (work => work());
		_applyMonitorRule = applyMonitorRule ?? ApplyMonitorRule;

		_watcher.OnGameLaunched += OnLaunched;
		_watcher.OnGameClosed += id => _marshal(() => OnClosed(id));
	}

	public void Start()
	{
		_stopped = false;
		_watcher.Start();
	}

	public void Stop()
	{
		_stopped = true;
		_watcher.Stop();
	}

	private void OnLaunched(string gameId)
	{
		if (_stopped) return;
		_applyMonitorRule(gameId);
		_marshal(() => ApplyTheProfile(gameId));
	}

	private void ApplyTheProfile(string gameId)
	{
		RememberTheLookBeforeTheGame();
		if (_gate.ShouldAutoApply(gameId))
		{
			GameProfile? gameProfile = _lookup(gameId);
			if (gameProfile == null)
			{
				NoteSetupOffer(gameId);
				return;
			}
			_engine.SetCurrent(gameProfile);
			_engine.ApplyAsync(gameId).GetAwaiter().GetResult();
			OnLookChanged?.Invoke();
		}
	}

	private void NoteSetupOffer(string gameId)
	{
		if (_settings != null
			&& GameSetupOffer.ShouldOffer(gameId, profileExists: false,
				_settings.DeclinedGameOffers, _settings.PendingGameOfferId))
		{
			_settings.PendingGameOfferId = gameId;
		}
	}

	private void ApplyMonitorRule(string gameId)
	{
		if (_settings != null)
		{
			ApplyTheRule(_settings, gameId,
				SupportedGames.ById(gameId)?.ProcessName,
				_onSettingsChanged, RealRuleDisplay);
		}
	}

	internal static void ApplyTheRule(
		AppSettings settings,
		string gameId,
		string? processName,
		Action? settingsChanged,
		RuleDisplay display)
	{
		MonitorRule? monitorRule = MonitorRules.For(settings.MonitorRules, gameId);
		if (monitorRule == null) return;

		string? device = string.IsNullOrWhiteSpace(monitorRule.Device) ? null : monitorRule.Device;
		if (device != null && !DisplayController.IsAttached(device, display.Monitors()))
			return;

		DisplayMode? current = display.Current(device);
		if (!current.HasValue) return;

		DisplayMode original = current.GetValueOrDefault();
		if (original.Width == monitorRule.Width && original.Height == monitorRule.Height)
			return;

		settings.PendingDisplayRestore = DisplayController.DescribeOwed(original, device);
		settingsChanged?.Invoke();

		if (!display.Apply(monitorRule.Width, monitorRule.Height, device)
			&& display.Current(device) == original)
		{
			settings.PendingDisplayRestore = "";
			settingsChanged?.Invoke();
		}
		else if (!string.IsNullOrWhiteSpace(processName))
		{
			display.RestoreWhenGameExits(processName, original, device);
		}
	}

	private void OnClosed(string gameId)
	{
		_engine.RestoreAsync().GetAwaiter().GetResult();
		PutBackTheLookBeforeTheGame();
		OnLookChanged?.Invoke();
	}

	private void RememberTheLookBeforeTheGame()
	{
		_engine.SnapshotDesktop();
		if (_settings != null && _settings.LookBeforeGame == null)
		{
			_settings.LookBeforeGame = GameLookSnapshot.Take(_settings);
			_onSettingsChanged?.Invoke();
		}
	}

	private void PutBackTheLookBeforeTheGame()
	{
		if (_settings != null && GameLookSnapshot.PutBackOwed(_settings))
			_onSettingsChanged?.Invoke();
	}
}