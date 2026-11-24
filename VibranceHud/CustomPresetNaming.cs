using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VibranceHud;

public static class CustomPresetNaming
{
	private const int MaxNameLength = 40;

	public const string Fallback = "My look";

	public static string Unique(string? wanted, IEnumerable<string> existing)
	{
		string text = (string.IsNullOrWhiteSpace(wanted) ? "My look" : wanted.Trim());
		text = text.Substring(0, Math.Min(text.Length, 40));
		HashSet<string> hashSet = new HashSet<string>(existing ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		if (!hashSet.Contains(text))
		{
			return text;
		}
		for (int i = 2; i < int.MaxValue; i++)
		{
			string text2 = " " + i.ToString(CultureInfo.InvariantCulture);
			string text3 = text.Substring(0, Math.Min(text.Length, 40 - text2.Length)) + text2;
			if (!hashSet.Contains(text3))
			{
				return text3;
			}
		}
		return text;
	}
}
