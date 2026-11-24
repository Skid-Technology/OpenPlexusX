using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VibranceHud.Games;

public static class GameDetection
{
	public static IReadOnlyList<DetectedGame> DetectInstalled(IEnumerable<string> libraryPaths, IEnumerable<SupportedGame> games, Func<string, bool> fileExists)
	{
		List<string> list = libraryPaths.ToList();
		List<DetectedGame> list2 = new List<DetectedGame>();
		foreach (SupportedGame game in games)
		{
			foreach (string item in list)
			{
				string arg = Path.Combine(item, "steamapps", $"appmanifest_{game.SteamAppId}.acf");
				if (fileExists(arg))
				{
					string installDir = Path.Combine(item, "steamapps", "common", game.InstallFolder);
					list2.Add(new DetectedGame(game, installDir));
					break;
				}
			}
		}
		return list2;
	}
}
