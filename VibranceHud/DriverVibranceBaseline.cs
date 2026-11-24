using System.Collections.Generic;
using VibranceHud.Monitors;

namespace VibranceHud;

public static class DriverVibranceBaseline
{
	public const int None = -1;

	public static int Remember(int stored, bool pending, int currentLevel)
	{
		if (!pending || stored < 0)
		{
			return Clamp(currentLevel);
		}
		return stored;
	}

	public static int? RestoreOnExit(int stored)
	{
		if (stored < 0)
		{
			return null;
		}
		return Clamp(stored);
	}

	public static Dictionary<string, int> RememberEach(IReadOnlyDictionary<string, int> stored, bool pending, IEnumerable<KeyValuePair<string, int>> current)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(stored);
		foreach (var (key, currentLevel) in current)
		{
			dictionary[key] = Remember(stored.TryGetValue(key, out var value) ? value : (-1), pending, currentLevel);
		}
		return dictionary;
	}

	public static int? RestoreOnExit(string displayId, IReadOnlyDictionary<string, int> stored, int legacyLevel)
	{
		if (!stored.TryGetValue(displayId, out var value))
		{
			if (stored.Count != 0)
			{
				return null;
			}
			return RestoreOnExit(legacyLevel);
		}
		return RestoreOnExit(value);
	}

	public static int? RestoreEveryDisplay(IVibranceController driver, IReadOnlyDictionary<string, int> stored, int legacyLevel)
	{
		if (driver is SelectedVibranceController selectedVibranceController)
		{
			selectedVibranceController.RestoreOwned();
			return null;
		}
		IReadOnlyList<KeyValuePair<string, int>> readOnlyList = driver.ReadEveryDisplayLevel();
		if (readOnlyList.Count == 0)
		{
			int? result = RestoreOnExit(legacyLevel);
			if (result.HasValue)
			{
				driver.SetLevel(result.Value);
			}
			return result;
		}
		int? result2 = null;
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			int? num = RestoreOnExit(readOnlyList[i].Key, stored, legacyLevel);
			if (num.HasValue)
			{
				driver.SetLevelFor(readOnlyList[i].Key, num.Value);
				if (i == 0)
				{
					result2 = num;
				}
			}
		}
		return result2;
	}

	private static int Clamp(int level)
	{
		if (level >= 0)
		{
			if (level <= 100)
			{
				return level;
			}
			return 100;
		}
		return 0;
	}
}
