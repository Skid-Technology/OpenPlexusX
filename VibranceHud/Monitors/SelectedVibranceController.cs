using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Monitors;

internal sealed class SelectedVibranceController : IVibranceController
{
	private readonly IVibranceController _driver;

	private readonly MonitorSelection _selection;

	private readonly AppSettings _settings;

	private readonly Action _save;

	public int CurrentLevel => ReadEveryDisplayLevel().FirstOrDefault(new KeyValuePair<string, int>("", 50)).Value;

	public int DefaultLevel => _driver.DefaultLevel;

	public bool IsAvailable => _driver.IsAvailable;

	public VibranceDriverState DriverState => _driver.DriverState;

	public SelectedVibranceController(IVibranceController driver, MonitorSelection selection, AppSettings settings, Action save)
	{
		_driver = driver;
		_selection = selection;
		_settings = settings;
		_save = save;
		AppSettings appSettings = settings;
		if (appSettings.OwnedMonitorVibrance == null)
		{
			Dictionary<string, int> dictionary = (appSettings.OwnedMonitorVibrance = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
		}
		appSettings = settings;
		if (appSettings.OwnedMonitorVibranceLastWritten == null)
		{
			Dictionary<string, int> dictionary = (appSettings.OwnedMonitorVibranceLastWritten = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
		}
	}

	public bool EveryDisplayIsAt(int level)
	{
		return ReadEveryDisplayLevel().All((KeyValuePair<string, int> x) => x.Value == level);
	}

	public IReadOnlyList<KeyValuePair<string, int>> ReadEveryDisplayLevel()
	{
		return (from x in _driver.ReadEveryDisplayLevel()
			where _selection.IncludesDevice(x.Key)
			select x).ToArray();
	}

	public void SetLevel(int level)
	{
		ReleaseExcluded();
		foreach (KeyValuePair<string, int> item in ReadEveryDisplayLevel())
		{
			SetLevelFor(item.Key, level);
		}
	}

	public void SetLevelFor(string id, int level)
	{
		if (!_selection.IncludesDevice(id))
		{
			return;
		}
		string key = _selection.KeyFor(id);
		KeyValuePair<string, int> keyValuePair = _driver.ReadEveryDisplayLevel().FirstOrDefault((KeyValuePair<string, int> x) => string.Equals(x.Key, id, StringComparison.OrdinalIgnoreCase));
		if (keyValuePair.Key == null)
		{
			return;
		}
		level = Math.Clamp(level, 0, 100);
		if (keyValuePair.Value != level)
		{
			if (!_settings.OwnedMonitorVibrance.ContainsKey(key) || (_settings.OwnedMonitorVibranceLastWritten.TryGetValue(key, out var value) && keyValuePair.Value != value))
			{
				_settings.OwnedMonitorVibrance[key] = keyValuePair.Value;
			}
			_settings.OwnedMonitorVibranceLastWritten[key] = level;
			_settings.NvapiRestorePending = true;
			_save();
			_driver.SetLevelFor(id, level);
		}
	}

	public void ReleaseExcluded()
	{
		Restore((string g) => !_selection.IncludesDevice(g));
	}

	public void RestoreOwned()
	{
		Restore((string _) => true);
		try
		{
			_save();
		}
		catch
		{
		}
	}

	private void Restore(Func<string, bool> release)
	{
		KeyValuePair<string, int>[] array = _settings.OwnedMonitorVibrance.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			var (text2, num2) = array[i];
			try
			{
				string device = _selection.DeviceFor(text2);
				if (device == null || !release(device))
				{
					continue;
				}
				IReadOnlyList<KeyValuePair<string, int>> source = _driver.ReadEveryDisplayLevel();
				KeyValuePair<string, int> keyValuePair2 = source.FirstOrDefault((KeyValuePair<string, int> x) => string.Equals(x.Key, device, StringComparison.OrdinalIgnoreCase));
				bool flag;
				if (keyValuePair2.Key != null)
				{
					flag = _settings.OwnedMonitorVibranceLastWritten.TryGetValue(text2, out var value) && keyValuePair2.Value != value && keyValuePair2.Value != num2;
					if (flag || keyValuePair2.Value == num2)
					{
						goto IL_0124;
					}
					_driver.SetLevelFor(device, num2);
					KeyValuePair<string, int> keyValuePair3 = _driver.ReadEveryDisplayLevel().FirstOrDefault((KeyValuePair<string, int> x) => string.Equals(x.Key, device, StringComparison.OrdinalIgnoreCase));
					if (keyValuePair3.Key != null && keyValuePair3.Value == num2)
					{
						goto IL_0124;
					}
				}
				continue;
				IL_0124:
				KeyValuePair<string, int> keyValuePair4 = source.FirstOrDefault((KeyValuePair<string, int> x) => _selection.IncludesDevice(x.Key));
				if (string.Equals(device, keyValuePair4.Key, StringComparison.OrdinalIgnoreCase))
				{
					_settings.DriverVibranceWeLastWrote = (flag ? ((int?)null) : new int?(num2));
				}
				_settings.OwnedMonitorVibrance.Remove(text2);
				_settings.OwnedMonitorVibranceLastWritten.Remove(text2);
				_settings.NvapiRestorePending = _settings.OwnedMonitorVibrance.Count > 0;
				_save();
			}
			catch
			{
			}
		}
		_settings.NvapiRestorePending = _settings.OwnedMonitorVibrance.Count > 0;
	}
}
