using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VibranceHud;

public static class VersionGate
{
	private sealed class RawStatus
	{
		[JsonPropertyName("minimumVersion")]
		public string? MinimumVersion { get; set; }

		[JsonPropertyName("blockedFlavours")]
		public string[]? BlockedFlavours { get; set; }

		[JsonPropertyName("message")]
		public string? Message { get; set; }

		[JsonPropertyName("messageId")]
		public string? MessageId { get; set; }

		[JsonPropertyName("messageUrl")]
		public string? MessageUrl { get; set; }
	}

	public static bool IsBlocked(Version current, Version? minimum)
	{
		if (minimum == null)
		{
			return false;
		}
		return Normalize(current) < Normalize(minimum);
	}

	public static bool IsFlavourBlocked(string flavour, IEnumerable<string>? blocked)
	{
		if (blocked == null)
		{
			return false;
		}
		if (string.Equals(flavour, "public", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return blocked.Any((string b) => string.Equals(b?.Trim(), flavour, StringComparison.OrdinalIgnoreCase));
	}

	public static bool IsBlocked(Version current, Version? minimum, string flavour, IEnumerable<string>? blockedFlavours)
	{
		if (!IsBlocked(current, minimum))
		{
			return IsFlavourBlocked(flavour, blockedFlavours);
		}
		return true;
	}

	public static Version? Resolve(Version? fetched, Version? cached)
	{
		if (fetched == null)
		{
			return cached;
		}
		if (cached == null)
		{
			return fetched;
		}
		if (!(Normalize(fetched) >= Normalize(cached)))
		{
			return cached;
		}
		return fetched;
	}

	public static IReadOnlyList<string> ResolveFlavours(IEnumerable<string>? fetched, IEnumerable<string>? cached)
	{
		List<string> list = new List<string>();
		foreach (string item in (cached ?? Array.Empty<string>()).Concat(fetched ?? Array.Empty<string>()))
		{
			string text = (item ?? "").Trim().ToLowerInvariant();
			if (text.Length != 0 && !(text == "public") && !list.Contains(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	public static AppStatus Parse(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return new AppStatus();
		}
		try
		{
			RawStatus rawStatus = JsonSerializer.Deserialize<RawStatus>(json);
			if (rawStatus == null)
			{
				return new AppStatus();
			}
			Version minimumVersion = null;
			if (!string.IsNullOrWhiteSpace(rawStatus.MinimumVersion) && Version.TryParse(rawStatus.MinimumVersion.Trim(), out Version result))
			{
				minimumVersion = result;
			}
			return new AppStatus
			{
				MinimumVersion = minimumVersion,
				BlockedFlavours = ResolveFlavours(rawStatus.BlockedFlavours, null),
				Message = (rawStatus.Message ?? ""),
				MessageId = (rawStatus.MessageId ?? ""),
				MessageUrl = (rawStatus.MessageUrl ?? "")
			};
		}
		catch
		{
			return new AppStatus();
		}
	}

	public static string Serialize(Version? minimum, string message, IEnumerable<string>? blockedFlavours = null)
	{
		return JsonSerializer.Serialize(new RawStatus
		{
			MinimumVersion = minimum?.ToString(),
			Message = message,
			BlockedFlavours = ResolveFlavours(blockedFlavours, null).ToArray()
		}, new JsonSerializerOptions
		{
			WriteIndented = true
		});
	}

	private static Version Normalize(Version v)
	{
		return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
	}
}
