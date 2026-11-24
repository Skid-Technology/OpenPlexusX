using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VibranceHud.Games;

public static class GameLibrary
{
	public static IReadOnlyList<DetectedGame> DetectInstalled()
	{
		List<DetectedGame> list = new List<DetectedGame>();
		list.AddRange(DetectSteam());
		list.AddRange(DetectEpic());
		return list;
	}

	public static IReadOnlyList<(SupportedGame Game, DetectedGame? Detected)> OrderForHub(IReadOnlyList<DetectedGame> detected)
	{
		Dictionary<SupportedGame, DetectedGame> byGame = detected.ToDictionary((DetectedGame d) => d.Game);
		return (from g in SupportedGames.All
			select (Game: g, Detected: byGame.TryGetValue(g, out var value) ? value : null) into x
			orderby x.Detected == null
			select x).ToList();
	}

	private static IReadOnlyList<DetectedGame> DetectSteam()
	{
		try
		{
			string text = SteamLocator.FindSteamPath();
			if (text == null)
			{
				return Array.Empty<DetectedGame>();
			}
			IReadOnlyList<string> libraries = SteamLocator.GetLibraries(text);
			IEnumerable<SupportedGame> games = SupportedGames.All.Where((SupportedGame g) => g.SteamAppId > 0);
			return GameDetection.DetectInstalled(libraries, games, File.Exists);
		}
		catch
		{
			return Array.Empty<DetectedGame>();
		}
	}

	private static IReadOnlyList<DetectedGame> DetectEpic()
	{
		try
		{
			List<DetectedGame> list = new List<DetectedGame>();
			foreach (SupportedGame item in SupportedGames.All.Where((SupportedGame g) => g.EpicAppName != null))
			{
				string text = EpicLocator.FindGameInstall(item.EpicAppName);
				if (text != null)
				{
					list.Add(new DetectedGame(item, text));
				}
			}
			return list;
		}
		catch
		{
			return Array.Empty<DetectedGame>();
		}
	}
}
