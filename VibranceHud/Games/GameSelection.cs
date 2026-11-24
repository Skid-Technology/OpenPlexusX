using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Games;

public sealed class GameSelection
{
	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly Func<IReadOnlyList<DetectedGame>> _detect;

	private List<DetectedGame> _installed = new List<DetectedGame>();

	private string? _currentId;

	public IReadOnlyList<DetectedGame> Installed => _installed;

	public string? CurrentId => _currentId;

	public SupportedGame? Current
	{
		get
		{
			if (_currentId != null)
			{
				return SupportedGames.All.FirstOrDefault((SupportedGame g) => g.Id == _currentId);
			}
			return null;
		}
	}

	public DetectedGame? Detected
	{
		get
		{
			if (_currentId != null)
			{
				return _installed.FirstOrDefault((DetectedGame d) => d.Game.Id == _currentId);
			}
			return null;
		}
	}

	public bool NothingInstalled => _installed.Count == 0;

	public event EventHandler? Changed;

	public GameSelection(AppSettings settings, SettingsStore store, Func<IReadOnlyList<DetectedGame>>? detect = null)
	{
		_settings = settings;
		_store = store;
		_detect = detect ?? new Func<IReadOnlyList<DetectedGame>>(GameLibrary.DetectInstalled);
		Refresh();
		_currentId = Resolve(_settings.CurrentGameId);
	}

	public void Refresh()
	{
		try
		{
			_installed = _detect().ToList();
		}
		catch
		{
			_installed = new List<DetectedGame>();
		}
		if (_currentId != null && Resolve(_currentId) == null)
		{
			_currentId = null;
			Persist();
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	public void Select(string? gameId)
	{
		string text = Resolve(gameId);
		if (!(text == _currentId))
		{
			_currentId = text;
			Persist();
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	private string? Resolve(string? gameId)
	{
		if (string.IsNullOrWhiteSpace(gameId))
		{
			return null;
		}
		if (!SupportedGames.All.Any((SupportedGame g) => g.Id == gameId))
		{
			return null;
		}
		if (!_installed.Any((DetectedGame d) => d.Game.Id == gameId))
		{
			return null;
		}
		return gameId;
	}

	private void Persist()
	{
		_settings.CurrentGameId = _currentId ?? "";
		_store.Save(_settings);
	}
}
