using System;
using System.Globalization;

namespace VibranceHud;

public readonly record struct ServerNotice(string Title, string Message, string Url, string Id)
{
	public static readonly TimeSpan MinimumGap = TimeSpan.FromDays(7.0);

	public static ServerNotice? For(AppStatus? status, string lastShownId, DateTime? lastShownAt, DateTime nowUtc)
	{
		if (status == null)
		{
			return null;
		}
		string text = (status.Message ?? "").Trim();
		if (text.Length == 0)
		{
			return null;
		}
		string text2 = (status.MessageId ?? "").Trim();
		if (text2.Length == 0)
		{
			return null;
		}
		if (string.Equals(text2, lastShownId, StringComparison.Ordinal))
		{
			return null;
		}
		if (lastShownAt.HasValue && nowUtc - lastShownAt.Value < MinimumGap)
		{
			return null;
		}
		return new ServerNotice("PlexusX", text, SafeLink(status.MessageUrl), text2);
	}

	public static string SafeLink(string? url)
	{
		string text = (url ?? "").Trim();
		if (!text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		if (text.Length <= "https://".Length)
		{
			return "";
		}
		if (text.IndexOf(' ') >= 0)
		{
			return "";
		}
		return text;
	}

	public static string Stamp(DateTime whenUtc)
	{
		return whenUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
	}

	public static DateTime? ParseWhen(string? stored)
	{
		if (string.IsNullOrWhiteSpace(stored))
		{
			return null;
		}
		if (!DateTime.TryParse(stored, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
		{
			return null;
		}
		return (result.Kind == DateTimeKind.Utc) ? result : result.ToUniversalTime();
	}
}
