using System;

namespace VibranceHud;

public static class AppInfo
{
	public const string ProductName = "PlexusX";

	public const string Tagline = "Sharper colours. Every game.";

	public const string DiscordUrl = "https://plexusx.lol/discord";

	public const string SiteUrl = "https://plexusx.lol";

	public const string PresetUploadUrl = "https://plexusx.lol/dashboard?tab=presets";

	public const string SupportEmail = "support@plexusx.lol";

	public static string VersionText => FormatVersion(UpdateService.CurrentVersion);

	internal static string PresetReportUrl(string id)
	{
		return "https://plexusx.lol/presets?report=" + Uri.EscapeDataString(id);
	}

	public static string FormatVersion(Version v)
	{
		return $"v{v.Major}.{v.Minor}.{Math.Max(v.Build, 0)}";
	}
}
