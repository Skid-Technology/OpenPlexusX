using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Presets;

public static class CrosshairLibrary
{
	public static readonly IReadOnlyList<CrosshairLibraryEntry> All = Array.Empty<CrosshairLibraryEntry>();

	public static IReadOnlyList<string> Games => new string[1] { "All" }.Concat(All.Select((CrosshairLibraryEntry e) => e.Game).Distinct()).ToList();

	public static IReadOnlyList<CrosshairLibraryEntry> ForGame(string game)
	{
		if (!string.Equals(game, "All", StringComparison.OrdinalIgnoreCase))
		{
			return All.Where((CrosshairLibraryEntry e) => string.Equals(e.Game, game, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		return All;
	}
}
