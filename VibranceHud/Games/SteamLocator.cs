using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace VibranceHud.Games;

public static class SteamLocator
{
	public static string? FindSteamPath()
	{
		string[] array = new string[3]
		{
			Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", null) as string,
			Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath", null) as string,
			Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) as string
		};
		foreach (string text in array)
		{
			if (!string.IsNullOrWhiteSpace(text) && Directory.Exists(text))
			{
				return Path.GetFullPath(text);
			}
		}
		return null;
	}

	public static IReadOnlyList<string> GetLibraries(string steamPath)
	{
		List<string> list = new List<string> { steamPath };
		string[] array = new string[2] { "steamapps\\libraryfolders.vdf", "config\\libraryfolders.vdf" };
		foreach (string path in array)
		{
			string path2 = Path.Combine(steamPath, path);
			if (File.Exists(path2))
			{
				list.AddRange(SteamVdf.ParseLibraryPaths(File.ReadAllText(path2)));
				break;
			}
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> list2 = new List<string>();
		foreach (string item in list)
		{
			if (Directory.Exists(item))
			{
				string fullPath = Path.GetFullPath(item);
				if (hashSet.Add(fullPath))
				{
					list2.Add(fullPath);
				}
			}
		}
		return list2;
	}
}
