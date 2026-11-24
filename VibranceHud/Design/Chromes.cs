using System;

namespace VibranceHud.Design;

public static class Chromes
{
	public static string Name(Chrome chrome)
	{
		return chrome.ToString();
	}

	public static Chrome Parse(string? stored)
	{
		string text = (stored ?? "").Trim();
		if (text.Length == 0 || char.IsDigit(text[0]) || text[0] == '-')
		{
			return Chrome.Plexus;
		}
		if (text.Contains(','))
		{
			return Chrome.Plexus;
		}
		if (!Enum.TryParse<Chrome>(text, ignoreCase: true, out var result) || !Enum.IsDefined(result))
		{
			return Chrome.Plexus;
		}
		return result;
	}
}
