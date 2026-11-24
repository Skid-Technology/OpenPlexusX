using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Games;

internal static class GameSetupOffer
{
	internal static bool ShouldOffer(string gameId, bool profileExists, IEnumerable<string> declined, string alreadyPending)
	{
		if (string.IsNullOrEmpty(gameId))
		{
			return false;
		}
		if (GameLookJoin.LookGameFor(gameId) == null)
		{
			return false;
		}
		if (profileExists)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(alreadyPending))
		{
			return false;
		}
		return !(declined ?? Enumerable.Empty<string>()).Any((string d) => string.Equals(d, gameId, StringComparison.OrdinalIgnoreCase));
	}

	internal static string Sentence(string gameId)
	{
		string value = GameLookJoin.LookGameFor(gameId) ?? gameId;
		return $"Playing {value}? PlexusX can use the {value} look every time you launch it.";
	}
}
