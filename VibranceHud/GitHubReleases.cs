using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VibranceHud;

public static class GitHubReleases
{
	public static ReleaseInfo? ParseLatest(string json)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			if (!rootElement.TryGetProperty("tag_name", out var value))
			{
				return null;
			}
			string tag = value.GetString() ?? "";
			Version version = ParseVersion(tag);
			if (version == null)
			{
				return null;
			}
			string pageUrl = (rootElement.TryGetProperty("html_url", out var value2) ? (value2.GetString() ?? "") : "");
			string notes = (rootElement.TryGetProperty("body", out var value3) ? (value3.GetString() ?? "") : "");
			if (!rootElement.TryGetProperty("assets", out var value4) || value4.ValueKind != JsonValueKind.Array)
			{
				return null;
			}
			string text = null;
			string filename = "";
			Version version2 = null;
			foreach (JsonElement item in value4.EnumerateArray())
			{
				string text2 = (item.TryGetProperty("name", out var value5) ? (value5.GetString() ?? "") : "");
				if (!text2.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string text3 = (item.TryGetProperty("browser_download_url", out var value6) ? value6.GetString() : null);
				if (text3 != null)
				{
					Version version3 = ParseVersionFromFilename(text2);
					if (text == null || (version3 != null && (version2 == null || version3 > version2)))
					{
						text = text3;
						filename = text2;
						version2 = version3;
					}
				}
			}
			if (text == null)
			{
				return null;
			}
			Version version4 = ParseVersionFromFilename(filename);
			if (version4 != null && version4 > version)
			{
				version = version4;
			}
			return new ReleaseInfo(version, tag, text, pageUrl, notes);
		}
		catch
		{
			return null;
		}
	}

	public static Version? ParseVersion(string tag)
	{
		if (string.IsNullOrWhiteSpace(tag))
		{
			return null;
		}
		if (!Version.TryParse(tag.Trim().TrimStart('v', 'V'), out Version result))
		{
			return null;
		}
		return Normalize(result);
	}

	public static Version? ParseVersionFromFilename(string filename)
	{
		MatchCollection matchCollection = Regex.Matches(filename, "\\d+\\.\\d+(?:\\.\\d+)?");
		if (matchCollection.Count <= 0)
		{
			return null;
		}
		return ParseVersion(matchCollection[matchCollection.Count - 1].Value);
	}

	public static bool IsNewer(Version latest, Version current)
	{
		return Normalize(latest) > Normalize(current);
	}

	private static Version Normalize(Version v)
	{
		return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
	}

	public static Version NormalizeForCompare(Version v)
	{
		return Normalize(v);
	}
}
