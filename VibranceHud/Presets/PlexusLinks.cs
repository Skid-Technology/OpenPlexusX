using System;

namespace VibranceHud.Presets;

public static class PlexusLinks
{
	public const string Scheme = "plexusx";

	internal const int MaxCodeChars = 2048;

	private const string ApplyPrefix = "apply/";

	public static PlexusLink Parse(string? link)
	{
		PlexusLink result = new PlexusLink(LinkAction.None, "");
		if (string.IsNullOrWhiteSpace(link))
		{
			return result;
		}
		string text = link.Trim();
		if (text.Length > 2080)
		{
			return result;
		}
		if (!text.StartsWith("plexusx://", StringComparison.OrdinalIgnoreCase))
		{
			return result;
		}
		string text2 = text.Substring("plexusx".Length + 3);
		text2 = text2.TrimEnd('/');
		if (!text2.StartsWith("apply/", StringComparison.OrdinalIgnoreCase))
		{
			return result;
		}
		string stringToUnescape = text2.Substring("apply/".Length);
		try
		{
			stringToUnescape = Uri.UnescapeDataString(stringToUnescape);
		}
		catch
		{
			return result;
		}
		stringToUnescape = stringToUnescape.Trim();
		if (stringToUnescape.Length == 0 || stringToUnescape.Length > 2048)
		{
			return result;
		}
		return new PlexusLink(LinkAction.Apply, stringToUnescape);
	}

	public static string For(string code)
	{
		return "plexusx://apply/" + Uri.EscapeDataString(code ?? "");
	}
}
