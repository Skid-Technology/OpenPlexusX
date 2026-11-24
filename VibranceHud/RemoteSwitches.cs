using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VibranceHud.Licensing;

namespace VibranceHud;

public static class RemoteSwitches
{
	private sealed class RawSwitches
	{
		[JsonPropertyName("disabled")]
		public string[]? Disabled { get; set; }
	}

	public const string DiscordPresence = "discord";

	public const string PhoneRemote = "phone";

	public const string Overlay = "overlay";

	public const string BackgroundUpdates = "updates";

	public const string FreeTier = "free";

	public static readonly IReadOnlyCollection<string> Known = new string[5] { "discord", "phone", "overlay", "updates", "free" };

	private static readonly object Gate = new object();

	private static HashSet<string> _off = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static string CacheDir => AppFolders.Local;

	private static string CachePath => Path.Combine(CacheDir, "switches-cache.json");

	public static bool IsOff(string feature)
	{
		lock (Gate)
		{
			return _off.Contains(feature);
		}
	}

	public static bool IsOn(string feature)
	{
		return !IsOff(feature);
	}

	public static IReadOnlySet<string> Parse(string? json)
	{
		HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(json))
		{
			return result;
		}
		try
		{
			RawSwitches rawSwitches = JsonSerializer.Deserialize<RawSwitches>(json);
			if (rawSwitches?.Disabled == null)
			{
				return result;
			}
			return new HashSet<string>(from name in rawSwitches.Disabled
				where !string.IsNullOrWhiteSpace(name)
				select name.Trim() into name
				where Known.Contains(name, StringComparer.OrdinalIgnoreCase)
				select name, StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
			return result;
		}
	}

	public static void LoadCached()
	{
		try
		{
			if (File.Exists(CachePath))
			{
				Adopt(Parse(File.ReadAllText(CachePath)));
			}
		}
		catch
		{
		}
	}

	public static async Task RefreshAsync()
	{
		string json;
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(10.0)
			};
			client.DefaultRequestHeaders.UserAgent.ParseAdd("PlexusX-Switches");
			json = await client.GetStringAsync(ActivationEndpoint.SwitchesUrl);
		}
		catch
		{
			return;
		}
		IReadOnlySet<string> off = Parse(json);
		Adopt(off);
		try
		{
			Directory.CreateDirectory(CacheDir);
			File.WriteAllText(CachePath, Serialize(off));
		}
		catch
		{
		}
	}

	internal static string Serialize(IReadOnlySet<string> off)
	{
		return JsonSerializer.Serialize(new RawSwitches
		{
			Disabled = off.ToArray()
		}, new JsonSerializerOptions
		{
			WriteIndented = true
		});
	}

	internal static void Adopt(IReadOnlySet<string> off)
	{
		lock (Gate)
		{
			_off = new HashSet<string>(off, StringComparer.OrdinalIgnoreCase);
		}
	}

	internal static void ResetForTest()
	{
		Adopt(new HashSet<string>());
	}
}
