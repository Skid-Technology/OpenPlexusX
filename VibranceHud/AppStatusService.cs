using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using VibranceHud.Licensing;

namespace VibranceHud;

public static class AppStatusService
{
	private static string StatusUrl => ActivationEndpoint.StatusUrlFor("store");

	private static string CacheDir => AppFolders.Local;

	private static string CachePath => Path.Combine(CacheDir, "app-status-cache.json");

	public static AppStatus? LastFetched { get; private set; }

	private static AppStatus ReadCache()
	{
		try
		{
			if (!File.Exists(CachePath))
			{
				return new AppStatus();
			}
			return VersionGate.Parse(File.ReadAllText(CachePath));
		}
		catch
		{
			return new AppStatus();
		}
	}

	public static Version? CachedMinimum()
	{
		return ReadCache().MinimumVersion;
	}

	public static IReadOnlyList<string> CachedBlockedFlavours()
	{
		return ReadCache().BlockedFlavours;
	}

	public static string CachedMessage()
	{
		return ReadCache().Message;
	}

	public static bool ThisBuildIsBlocked()
	{
		AppStatus appStatus = ReadCache();
		return VersionGate.IsBlocked(UpdateService.CurrentVersion, appStatus.MinimumVersion, "store", appStatus.BlockedFlavours);
	}

	public static bool ThisBuildIsBlockedByVersion()
	{
		return VersionGate.IsBlocked(UpdateService.CurrentVersion, ReadCache().MinimumVersion);
	}

	public static async Task<Version?> RefreshAsync()
	{
		AppStatus cache = ReadCache();
		Version cached = cache.MinimumVersion;
		Version fetched = null;
		IReadOnlyList<string> fetchedFlavours = Array.Empty<string>();
		string message = "";
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(10.0)
			};
			client.DefaultRequestHeaders.UserAgent.ParseAdd(UpdateService.UpdaterUserAgent);
			AppStatus appStatus = VersionGate.Parse(await client.GetStringAsync(StatusUrl));
			fetched = appStatus.MinimumVersion;
			fetchedFlavours = appStatus.BlockedFlavours;
			message = appStatus.Message;
			LastFetched = appStatus;
		}
		catch
		{
		}
		Version version = VersionGate.Resolve(fetched, cached);
		IReadOnlyList<string> readOnlyList = VersionGate.ResolveFlavours(fetchedFlavours, cache.BlockedFlavours);
		bool num = version != null && (cached == null || version > cached);
		bool flag = readOnlyList.Count > cache.BlockedFlavours.Count;
		if (num | flag)
		{
			try
			{
				Directory.CreateDirectory(CacheDir);
				File.WriteAllText(CachePath, VersionGate.Serialize(version, message, readOnlyList));
			}
			catch
			{
			}
		}
		return version;
	}
}
