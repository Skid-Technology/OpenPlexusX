using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using A;
using VibranceHud.Licensing;

namespace VibranceHud.Presets;

public static class CommunityPresetService
{
	internal sealed record LibraryAsker(string Serial, string HardwareId, string? BuildId);

	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8.0);

	internal static string PublicWallUrl => ActivationEndpoint.Origin + "/api/presets/public?version=" + Uri.EscapeDataString(AppVersion);

	internal static string AppVersion { get; } = ReadAppVersion();

	internal static string CachePath => Path.Combine(SettingsStore.DefaultDirectory, "community-presets.json");

	private static string ReadAppVersion()
	{
		Assembly assembly = typeof(CommunityPresetService).Assembly;
		return PlainVersion(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion, assembly.GetName().Version);
	}

	internal static string PlainVersion(string? informational, Version? assemblyVersion)
	{
		Match match = Regex.Match(informational?.Trim() ?? "", "^(\\d+)\\.(\\d+)\\.(\\d+)");
		if (match.Success)
		{
			return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) + "." + int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) + "." + int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
		}
		Version version = assemblyVersion ?? new Version(0, 0, 0);
		return version.Major + "." + version.Minor + "." + Math.Max(version.Build, 0);
	}

	public static CommunityCatalogue Cached()
	{
		return Cached(CachePath);
	}

	internal static CommunityCatalogue Cached(string cachePath)
	{
		try
		{
			return File.Exists(cachePath) ? CommunityCatalogue.Parse(File.ReadAllText(cachePath)) : CommunityCatalogue.Empty;
		}
		catch
		{
			return CommunityCatalogue.Empty;
		}
	}

	public static Task<CommunityCatalogue?> FetchAsync(CancellationToken cancel = default(CancellationToken))
	{
		return FetchAsync(null, CachePath, WhoIsAskingNow(), cancel);
	}

	private static LibraryAsker? WhoIsAskingNow()
	{
		return null;
	}

	internal static LibraryAsker? WhoIsAsking(string? serial)
	{
		return null;
	}

	internal static LibraryAsker? WhoIsAsking(string? serial, string? hardwareId, string? buildId)
	{
		if (!string.IsNullOrWhiteSpace(serial) && !string.IsNullOrWhiteSpace(hardwareId))
		{
			return new LibraryAsker(serial, hardwareId, buildId);
		}
		return null;
	}

	internal static string PaidWallUrl(LibraryAsker who)
	{
		return ActivationEndpoint.LibraryUrl + "?serial=" + Uri.EscapeDataString(who.Serial) + "&hwid=" + Uri.EscapeDataString(who.HardwareId) + (!string.IsNullOrWhiteSpace(who.BuildId) ? ("&build_id=" + Uri.EscapeDataString(who.BuildId)) : "") + "&version=" + Uri.EscapeDataString(AppVersion);
	}

	internal static Task<CommunityCatalogue?> FetchAsync(HttpMessageHandler? transport, string cachePath, CancellationToken cancel = default(CancellationToken))
	{
		return FetchAsync(transport, cachePath, null, cancel);
	}

	internal static async Task<CommunityCatalogue?> FetchAsync(HttpMessageHandler? transport, string cachePath, LibraryAsker? who, CancellationToken cancel = default(CancellationToken), Func<Action, bool>? acceptResponse = null)
	{
		string json = null;
		try
		{
			using HttpClient client = ((transport == null) ? new HttpClient() : new HttpClient(transport, disposeHandler: false));
			client.Timeout = Timeout;
			client.DefaultRequestHeaders.UserAgent.ParseAdd("PlexusX");
			if ((object)who != null)
			{
				using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, PaidWallUrl(who));
				using HttpResponseMessage answer = await client.SendAsync(request, cancel).ConfigureAwait(continueOnCapturedContext: false);
				int statusCode = (int)answer.StatusCode;
				if (answer.IsSuccessStatusCode)
				{
					json = await answer.Content.ReadAsStringAsync(cancel).ConfigureAwait(continueOnCapturedContext: false);
				}
				else if (statusCode != 403 && statusCode != 401)
				{
					return null;
				}
			}
			if (json == null)
			{
				json = await client.GetStringAsync(PublicWallUrl, cancel).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch
		{
			return null;
		}
		CommunityCatalogue catalogue = CommunityCatalogue.Parse(json);
		if (catalogue.Source == null)
		{
			return null;
		}
		if (acceptResponse != null)
		{
			if (!acceptResponse(Remember))
			{
				return null;
			}
		}
		else
		{
			Remember();
		}
		return catalogue;
		void Remember()
		{
			if (catalogue.Looks.Count > 0 || catalogue.Crosshairs.Count > 0)
			{
				Save(json, cachePath);
			}
		}
	}

	private static void Save(string json, string cachePath)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
			string text = cachePath + ".tmp";
			File.WriteAllText(text, json);
			File.Move(text, cachePath, overwrite: true);
		}
		catch
		{
		}
	}
}
