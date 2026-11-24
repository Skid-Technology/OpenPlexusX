using System.Collections.Generic;

namespace VibranceHud;

public static class DisplayModes
{
	public static int RefreshForSharedMode(IEnumerable<DisplayMode> supported, int width, int height, int sharedHz)
	{
		return MaxRefreshFor(supported, width, height);
	}

	public static IReadOnlyList<DisplayMode> BestPerResolution(IEnumerable<DisplayMode> modes)
	{
		Dictionary<(int, int), int> dictionary = new Dictionary<(int, int), int>();
		foreach (DisplayMode mode in modes)
		{
			if (mode.Width > 0 && mode.Height > 0)
			{
				(int, int) key = (mode.Width, mode.Height);
				if (!dictionary.TryGetValue(key, out var value) || mode.RefreshHz > value)
				{
					dictionary[key] = mode.RefreshHz;
				}
			}
		}
		List<DisplayMode> list = new List<DisplayMode>();
		foreach (var (tuple2, num2) in dictionary)
		{
			int item = tuple2.Item1;
			int item2 = tuple2.Item2;
			int refreshHz = num2;
			list.Add(new DisplayMode(item, item2, refreshHz));
		}
		list.Sort((DisplayMode a, DisplayMode b) => (a.Width == b.Width) ? b.Height.CompareTo(a.Height) : b.Width.CompareTo(a.Width));
		return list;
	}

	public static int MaxRefreshFor(IEnumerable<DisplayMode> modes, int width, int height)
	{
		int num = 0;
		foreach (DisplayMode mode in modes)
		{
			if (mode.Width == width && mode.Height == height && mode.RefreshHz > num)
			{
				num = mode.RefreshHz;
			}
		}
		return num;
	}

	public static IReadOnlyList<int> RefreshRatesFor(IEnumerable<DisplayMode> modes, int width, int height)
	{
		SortedSet<int> sortedSet = new SortedSet<int>();
		foreach (DisplayMode mode in modes)
		{
			if (mode.Width == width && mode.Height == height && mode.RefreshHz > 0)
			{
				sortedSet.Add(mode.RefreshHz);
			}
		}
		List<int> list = new List<int>(sortedSet);
		list.Reverse();
		return list;
	}

	public static bool IsSupported(IEnumerable<DisplayMode> modes, int width, int height, int hz)
	{
		foreach (DisplayMode mode in modes)
		{
			if (mode.Width == width && mode.Height == height && mode.RefreshHz == hz)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsSupported(IEnumerable<DisplayMode> modes, int width, int height)
	{
		return MaxRefreshFor(modes, width, height) > 0;
	}
}
