using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.DarkBoost;

public static class DarkBoostSetups
{
	public static DarkBoostSetup Default => new DarkBoostSetup("Setup 1", DarkBoostTint.Green, 100, 0);

	public static List<DarkBoostSetup> Normalise(IEnumerable<DarkBoostSetup?>? saved)
	{
		List<DarkBoostSetup> list = (from s in saved ?? Array.Empty<DarkBoostSetup>()
			where s != null
			select s.Clamped()).ToList();
		if (list.Count == 0)
		{
			list.Add(Default);
		}
		return list;
	}

	public static int Selected(int saved, int count)
	{
		if (count > 0)
		{
			return Math.Clamp(saved, 0, count - 1);
		}
		return 0;
	}

	public static int Next(int selected, int count)
	{
		if (count > 0)
		{
			return (Selected(selected, count) + 1) % count;
		}
		return 0;
	}

	public static DarkBoostSetup InUse(AppSettings settings)
	{
		List<DarkBoostSetup> list = Normalise(settings.DarkBoostSetups);
		return list[Selected(settings.DarkBoostSelected, list.Count)];
	}

	public static int AdvanceInSettings(AppSettings settings)
	{
		settings.DarkBoostSelected = Next(count: (settings.DarkBoostSetups = Normalise(settings.DarkBoostSetups)).Count, selected: settings.DarkBoostSelected);
		return settings.DarkBoostSelected;
	}

	public static bool CanDelete(int count)
	{
		return count > 1;
	}

	public static int Delete(List<DarkBoostSetup> setups, int selected)
	{
		if (!CanDelete(setups.Count))
		{
			return Selected(selected, setups.Count);
		}
		int num = Selected(selected, setups.Count);
		setups.RemoveAt(num);
		return Math.Max(0, num - 1);
	}

	public static int SaveNew(List<DarkBoostSetup> setups, int selected)
	{
		DarkBoostSetup darkBoostSetup = ((setups.Count == 0) ? Default : setups[Selected(selected, setups.Count)]);
		int n = setups.Count + 1;
		while (setups.Any((DarkBoostSetup s) => string.Equals(s.Name, "Setup " + n, StringComparison.OrdinalIgnoreCase)))
		{
			n++;
		}
		setups.Add(darkBoostSetup with
		{
			Name = "Setup " + n
		});
		return setups.Count - 1;
	}
}
