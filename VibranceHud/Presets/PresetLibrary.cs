using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Presets;

public static class PresetLibrary
{
	public const string Everything = "All";

	public static readonly IReadOnlyList<LibraryPreset> All = Array.Empty<LibraryPreset>();

	public static IReadOnlyList<string> Games => new string[1] { "All" }.Concat(All.Select((LibraryPreset p) => p.Game).Distinct()).ToList();

	public static IReadOnlyList<LibraryPreset> ForGame(string game)
	{
		if (!string.Equals(game, "All", StringComparison.OrdinalIgnoreCase))
		{
			return All.Where((LibraryPreset p) => string.Equals(p.Game, game, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		return All;
	}
}
