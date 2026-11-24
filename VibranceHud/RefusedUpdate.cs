using System;
using System.Globalization;

namespace VibranceHud;

internal static class RefusedUpdate
{
	internal static readonly TimeSpan BlockFor = TimeSpan.FromDays(7.0);

	internal const string NoticeBody = "Windows blocked the PlexusX update. Get it from plexusx.lol.";

	internal const string NoticeTitle = "PlexusX";

	internal static string NoticeLink => "https://plexusx.lol";

	internal static Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

	internal static Func<string, string, bool>? ShowNotice { get; set; }

	private static string Normal(string? sha)
	{
		return (sha ?? "").Trim().ToLowerInvariant();
	}

	internal static void Record(AppSettings settings, string? sha256)
	{
		string text = Normal(sha256);
		if (text.Length == 0)
		{
			return;
		}
		settings.UpdateRefusedSha256 = text;
		settings.UpdateRefusedAtUtc = UtcNow().ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
		if (Normal(settings.UpdateRefusedNoticedSha256) == text)
		{
			return;
		}
		try
		{
			Func<string, string, bool>? showNotice = ShowNotice;
			if (showNotice != null && showNotice("PlexusX", "Windows blocked the PlexusX update. Get it from plexusx.lol."))
			{
				settings.UpdateRefusedNoticedSha256 = text;
			}
		}
		catch
		{
		}
	}

	internal static bool IsBlocked(AppSettings settings, ReleaseInfo? release)
	{
		if (release == null)
		{
			return false;
		}
		string text = Normal(settings.UpdateRefusedSha256);
		if (text.Length == 0 || text != Normal(release.Sha256))
		{
			return false;
		}
		if (!DateTime.TryParse(settings.UpdateRefusedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
		{
			return false;
		}
		TimeSpan timeSpan = UtcNow().ToUniversalTime() - result;
		if (timeSpan >= TimeSpan.Zero)
		{
			return timeSpan < BlockFor;
		}
		return false;
	}

	internal static ReleaseInfo? Unless(AppSettings settings, ReleaseInfo? release)
	{
		if (!IsBlocked(settings, release))
		{
			return release;
		}
		return null;
	}
}
