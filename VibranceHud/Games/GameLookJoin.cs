using System;
using System.Collections.Generic;
using System.Linq;
using VibranceHud.Licensing;

namespace VibranceHud.Games;

internal static class GameLookJoin
{
	private static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["rust"] = "Rust",
		["cs2"] = "CS2",
		["fortnite"] = "Fortnite",
		["valorant"] = "Valorant",
		["r6"] = "Rainbow Six Siege"
	};

	internal static IReadOnlyCollection<string> JoinedIds => Map.Keys.ToList();

	internal static string? LookGameFor(string gameId)
	{
		if (gameId == null || !Map.TryGetValue(gameId, out string value))
		{
			return null;
		}
		return value;
	}

	internal static DisplayPreset? StartingLookFor(string gameId)
	{
		string text = LookGameFor(gameId);
		if (text == null)
		{
			return null;
		}
		ScenePresetSet scenePresetSet = ServedLooks.ForGame(text);
		if (scenePresetSet.Presets.Count <= 0)
		{
			return null;
		}
		return scenePresetSet.Presets[0];
	}
}
