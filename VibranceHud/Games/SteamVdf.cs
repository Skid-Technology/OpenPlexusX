using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VibranceHud.Games;

public static class SteamVdf
{
	private static readonly Regex KeyValue = new Regex("^\\s*\"([^\"]+)\"\\s+\"(.+?)\"\\s*$", RegexOptions.Compiled);

	public static IReadOnlyList<string> ParseLibraryPaths(string vdfContent)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrEmpty(vdfContent))
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] array = vdfContent.Split('\n');
		foreach (string input in array)
		{
			Match match = KeyValue.Match(input);
			if (!match.Success)
			{
				continue;
			}
			string value = match.Groups[1].Value;
			string value2 = match.Groups[2].Value;
			if ((value.Equals("path", StringComparison.OrdinalIgnoreCase) || IsAllDigits(value)) && (value2.Contains('\\') || value2.Contains('/')))
			{
				string item = value2.Replace("\\\\", "\\").Trim();
				if (hashSet.Add(item))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	private static bool IsAllDigits(string s)
	{
		if (s.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (!char.IsDigit(s[i]))
			{
				return false;
			}
		}
		return true;
	}
}
