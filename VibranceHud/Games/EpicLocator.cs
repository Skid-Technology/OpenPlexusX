using System;
using System.IO;
using System.Text.Json;

namespace VibranceHud.Games;

public static class EpicLocator
{
	public static string? ParseInstallLocation(string manifestJson, string appName)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(manifestJson);
			if (!jsonDocument.RootElement.TryGetProperty("AppName", out var value))
			{
				return null;
			}
			if (!string.Equals(value.GetString(), appName, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			JsonElement value2;
			return jsonDocument.RootElement.TryGetProperty("InstallLocation", out value2) ? value2.GetString() : null;
		}
		catch
		{
			return null;
		}
	}

	public static string? FindGameInstall(string appName)
	{
		try
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
			if (!Directory.Exists(path))
			{
				return null;
			}
			foreach (string item in Directory.EnumerateFiles(path, "*.item"))
			{
				string text = ParseInstallLocation(File.ReadAllText(item), appName);
				if (text != null && Directory.Exists(text))
				{
					return text;
				}
			}
			return null;
		}
		catch
		{
			return null;
		}
	}
}
