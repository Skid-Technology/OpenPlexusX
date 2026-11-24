using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Licensing;
using VibranceHud.Updates;

namespace VibranceHud;

public static class UpdateService
{
	internal enum CheckOutcome
	{
		UpdateAvailable,
		UpToDate,
		CouldNotCheck
	}

	internal interface IInstallerProcess
	{
		Task WaitForExitAsync(CancellationToken token);
	}

	private sealed class StartedInstaller : IInstallerProcess
	{
		private readonly Process _process;

		public StartedInstaller(Process process)
		{
			_process = process;
		}

		public Task WaitForExitAsync(CancellationToken token)
		{
			return _process.WaitForExitAsync(token);
		}
	}

	internal enum HandOff
	{
		NothingToInstall,
		AlreadyStarted,
		CouldNotStart,
		InstallerEnded,
		CeilingReached
	}

	private const string Repo = "matej4Y8Y/VibranceHud";

	private const string LatestApi = "https://api.github.com/repos/matej4Y8Y/VibranceHud/releases/latest";

	private const string TagApi = "https://api.github.com/repos/matej4Y8Y/VibranceHud/releases/tags/";

	private const string ManifestUrl = "https://plexusx.lol/api/update/latest";

	private static readonly object _downloadsLock = new object();

	private static readonly Dictionary<string, Task<string?>> _downloads = new Dictionary<string, Task<string>>();

	internal static readonly TimeSpan SplashDownloadBudget = TimeSpan.FromSeconds(90.0);

	internal static Func<ProcessStartInfo, IInstallerProcess> StartInstallerProcess = (ProcessStartInfo psi) => new StartedInstaller(Process.Start(psi) ?? throw new Win32Exception("the installer did not start"));

	private static int _installerStarted;

	internal static readonly TimeSpan HandOffCeiling = TimeSpan.FromSeconds(90.0);

	private static SplashForm? _splash;

	private static readonly object _checkLock = new object();

	private static bool _isChecking;

	public static Version CurrentVersion
	{
		get
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
			return new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
		}
	}

	public static string? LastDownloadError { get; internal set; }

	public static string? LastCheckError { get; internal set; }

	internal static string UpdaterUserAgent => $"PlexusX-Updater/{CurrentVersion}";

	internal static Func<Task<ReleaseInfo?>>? SignedUpdateForTest { get; set; }

	internal static Action<string?>? RecordRefusal { get; set; }

	internal static string DownloadFolder => DownloadFolderForTest ?? SecurityCandidate.DownloadRoot ?? Path.GetTempPath();

	internal static string? DownloadFolderForTest { get; set; }

	internal static TimeSpan BackoffUnit { get; set; } = TimeSpan.FromSeconds(5.0);

	internal static Func<ReleaseInfo, IEnumerable<IDownloadSource>>? DownloadSourcesForTest { get; set; }

	internal static bool InstallerStartedThisRun => Volatile.Read(in _installerStarted) != 0;

	public static Action<ReleaseInfo, string>? RecordPending { get; set; }

	internal static CheckOutcome OutcomeOf(ReleaseInfo? update, string? checkError)
	{
		if (update != null)
		{
			return CheckOutcome.UpdateAvailable;
		}
		if (!string.IsNullOrEmpty(checkError))
		{
			return CheckOutcome.CouldNotCheck;
		}
		return CheckOutcome.UpToDate;
	}

	private static HttpClient NewClient(TimeSpan? timeout = null)
	{
		HttpClient httpClient = new HttpClient();
		httpClient.Timeout = timeout ?? TimeSpan.FromSeconds(8.0);
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UpdaterUserAgent);
		return httpClient;
	}

	public static async Task<ReleaseInfo?> TryGetUpdateAsync()
	{
		if (!SecurityCandidate.IsEnabled)
		{
		}
		return null;
	}

	internal static async Task<ReleaseInfo?> TryGetSignedUpdateAsync()
	{
		if (SignedUpdateForTest != null)
		{
			return await SignedUpdateForTest();
		}
		try
		{
			using HttpClient client = NewClient();
			string json = await client.GetStringAsync("https://plexusx.lol/api/update/latest");
			LastCheckError = null;
			return ReleaseFromSignedManifest(json);
		}
		catch (Exception ex)
		{
			LastCheckError = ex.Message;
			return null;
		}
	}

	internal static async Task<(bool RevocationRefreshed, ReleaseInfo? Update)> StartupChecksTogetherAsync(Func<Task<bool>> refreshRevocation, Func<Task<ReleaseInfo?>> getUpdate)
	{
		Task<bool> revocation = Guarded<bool>(refreshRevocation, failed: false);
		Task<ReleaseInfo> update = Guarded<ReleaseInfo>(getUpdate, null);
		await Task.WhenAll(revocation, update);
		return (RevocationRefreshed: revocation.Result, Update: update.Result);
		static async Task<T> Guarded<T>(Func<Task<T>> ask, T failed)
		{
			try
			{
				return await ask();
			}
			catch
			{
				return failed;
			}
		}
	}

	internal static ReleaseInfo? ReleaseFromSignedManifest(string json)
	{
		if (!UpdateManifestVerifier.TryVerify(json, UpdateKeys.Verification, out UpdateManifest manifest) || manifest == null)
		{
			LastDownloadError = "Update manifest failed signature check - ignoring it.";
			LastCheckError = "Update manifest failed its signature check.";
			return null;
		}
		if (!Version.TryParse(manifest.Version, out Version result))
		{
			return null;
		}
		if (!GitHubReleases.IsNewer(result, CurrentVersion))
		{
			return null;
		}
		return new ReleaseInfo(result, "v" + manifest.Version, manifest.InstallerUrl, "https://plexusx.lol", manifest.Notes ?? "", manifest.Sha256, null, null, json);
	}

	internal static ReleaseInfo? ParseMirrorJson(string json)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			if (!rootElement.TryGetProperty("version", out var value))
			{
				return null;
			}
			if (!Version.TryParse(value.GetString(), out Version result))
			{
				return null;
			}
			if (!rootElement.TryGetProperty("installer_url", out var value2))
			{
				return null;
			}
			string sha = (rootElement.TryGetProperty("sha256", out var value3) ? value3.GetString() : null);
			string mirrorUrl = (rootElement.TryGetProperty("mirror_url", out var value4) ? value4.GetString() : null);
			string rawMirrorUrl = (rootElement.TryGetProperty("raw_url", out var value5) ? value5.GetString() : null);
			string notes = (rootElement.TryGetProperty("notes", out var value6) ? (value6.GetString() ?? "") : "");
			return new ReleaseInfo(result, $"v{result}", value2.GetString() ?? "", "", notes, sha, mirrorUrl, rawMirrorUrl);
		}
		catch
		{
			return null;
		}
	}

	public static async Task<string> GetNotesForVersionAsync(Version version)
	{
		string text = LoadEmbeddedNotes(version);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		try
		{
			using HttpClient client = NewClient();
			ReleaseInfo releaseInfo = GitHubReleases.ParseLatest(await client.GetStringAsync("https://api.github.com/repos/matej4Y8Y/VibranceHud/releases/tags/" + $"v{version}"));
			if (releaseInfo != null && !string.IsNullOrWhiteSpace(releaseInfo.Notes))
			{
				return releaseInfo.Notes;
			}
		}
		catch
		{
		}
		return $"PlexusX {version.Major}.{version.Minor}.{Math.Max(version.Build, 0)} is installed.\r\n\r\n" + "This update has no release notes.\r\n\r\nCome say hi on Discord for changes, help and to report anything broken:\r\nhttps://plexusx.lol/discord";
	}

	private static string? LoadEmbeddedNotes(Version version)
	{
		try
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			string name = $"VibranceHud.UpdateNotes.RELEASE_NOTES_v{version.Major}_{version.Minor}_{((version.Build >= 0) ? version.Build : 0)}.md";
			using Stream stream = executingAssembly.GetManifestResourceStream(name);
			if (stream == null)
			{
				return null;
			}
			using StreamReader streamReader = new StreamReader(stream);
			return streamReader.ReadToEnd();
		}
		catch
		{
			return null;
		}
	}

	public static async Task<(string? Etag, string? Url)> HeadLatestReleaseAsync()
	{
		try
		{
			using HttpClient client = NewClient();
			using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Head, "https://api.github.com/repos/matej4Y8Y/VibranceHud/releases/latest");
			using HttpResponseMessage httpResponseMessage = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return (Etag: null, Url: null);
			}
			string text = httpResponseMessage.Headers.ETag?.Tag;
			if (text == null && httpResponseMessage.Headers.TryGetValues("ETag", out IEnumerable<string> values))
			{
				text = string.Join("", values);
			}
			string item = null;
			if (httpResponseMessage.Headers.Location != null)
			{
				item = httpResponseMessage.Headers.Location.ToString();
			}
			return (Etag: text, Url: item);
		}
		catch
		{
			return (Etag: null, Url: null);
		}
	}

	public static Task<string?> DownloadAndStageAsync(ReleaseInfo release, IProgress<int>? progress = null)
	{
		string key = $"{release.Version}|{(release.Sha256 ?? "").Trim().ToLowerInvariant()}";
		lock (_downloadsLock)
		{
			if (_downloads.TryGetValue(key, out Task<string> value))
			{
				return value;
			}
			Task<string?> download = DownloadAndStageOnceAsync(release, progress);
			_downloads[key] = download;
			download.ContinueWith((Task<string?> _) =>
			{
				lock (_downloadsLock)
				{
					if (_downloads.TryGetValue(key, out Task<string> value2) && value2 == download)
					{
						_downloads.Remove(key);
					}
				}
			}, TaskScheduler.Default);
			return download;
		}
	}

	private static async Task<string?> DownloadAndStageOnceAsync(ReleaseInfo release, IProgress<int>? progress)
	{
		LastDownloadError = null;
		string alreadyStaged = Path.Combine(DownloadFolder, $"PlexusX-Setup-{release.Version}.exe");
		if (File.Exists(alreadyStaged))
		{
			if (await VerifyAsync(alreadyStaged, release))
			{
				progress?.Report(100);
				return alreadyStaged;
			}
			LastDownloadError = null;
		}
		string partial = PartialPathFor(release);
		IEnumerable<IDownloadSource> enumerable = ResolveDownloadSources(release);
		foreach (IDownloadSource source in enumerable)
		{
			int barren = 0;
			while (barren < 3)
			{
				long before = LengthOf(partial);
				string landedAt = null;
				try
				{
					string downloaded = await source.DownloadAsync(progress);
					landedAt = downloaded;
					if (downloaded != null)
					{
						if (!File.Exists(downloaded))
						{
							return WindowsRemovedIt(release);
						}
						if (await VerifyAsync(downloaded, release))
						{
							string text = StageInstaller(downloaded, release.Version);
							if (text != null)
							{
								return text;
							}
							if (!File.Exists(downloaded))
							{
								return WindowsRemovedIt(release);
							}
							barren++;
							await BackoffAsync(barren);
						}
						else
						{
							SafeDelete(downloaded);
							barren++;
							await BackoffAsync(barren);
						}
						continue;
					}
				}
				catch (Exception ex)
				{
					LastDownloadError = "Source=" + source.Name + ": " + ex.Message;
					if (landedAt != null && !File.Exists(landedAt))
					{
						return WindowsRemovedIt(release);
					}
				}
				if (LengthOf(partial) > before)
				{
					barren = 0;
					continue;
				}
				barren++;
				await BackoffAsync(barren);
			}
		}
		if (LastDownloadError == null)
		{
			LastDownloadError = "All download sources and retries exhausted.";
		}
		return null;
	}

	private static string? WindowsRemovedIt(ReleaseInfo release)
	{
		LastDownloadError = "Windows removed the downloaded installer (antivirus or SmartScreen).";
		try
		{
			RecordRefusal?.Invoke(release.Sha256);
		}
		catch
		{
		}
		return null;
	}

	private static long LengthOf(string path)
	{
		try
		{
			return File.Exists(path) ? new FileInfo(path).Length : 0;
		}
		catch
		{
			return 0L;
		}
	}

	internal static string PartialPathFor(ReleaseInfo release)
	{
		string text = (release.Sha256 ?? "").Trim().ToLowerInvariant();
		string value = ((text.Length >= 12 && text.All(Uri.IsHexDigit)) ? text.Substring(0, 12) : "unsigned");
		return Path.Combine(DownloadFolder, $"PlexusX-Setup-{release.Version}-{value}.partial");
	}

	private static IEnumerable<IDownloadSource> ResolveDownloadSources(ReleaseInfo release)
	{
		if (DownloadSourcesForTest != null)
		{
			foreach (IDownloadSource item in DownloadSourcesForTest(release))
			{
				yield return item;
			}
			yield break;
		}
		yield return new GitHubReleasesSource(release);
		if (!string.IsNullOrEmpty(release.MirrorUrl))
		{
			yield return new GitHubReleasesSource(release with
			{
				InstallerUrl = release.MirrorUrl
			});
		}
		if (!string.IsNullOrEmpty(release.RawMirrorUrl))
		{
			yield return new GitHubReleasesSource(release with
			{
				InstallerUrl = release.RawMirrorUrl
			});
		}
	}

	private static async Task BackoffAsync(int attempt)
	{
		await Task.Delay(BackoffUnit * Math.Pow(2.0, attempt - 1));
	}

	internal static Task<bool> VerifyForTest(string path, ReleaseInfo release)
	{
		return VerifyAsync(path, release);
	}

	private static async Task<bool> VerifyAsync(string path, ReleaseInfo release)
	{
		if (!IsValidInstaller(path))
		{
			LastDownloadError = "Downloaded file is not a valid Windows executable (missing MZ header).";
			return false;
		}
		if (string.IsNullOrWhiteSpace(release.Sha256))
		{
			LastDownloadError = "This update did not come with a checksum, so PlexusX cannot confirm the installer is the one we published. Nothing was installed.";
			return false;
		}
		using (SHA256 sha = SHA256.Create())
		{
			using FileStream stream = File.OpenRead(path);
			string text = Convert.ToHexString(await sha.ComputeHashAsync(stream)).ToLowerInvariant();
			if (!string.Equals(text, release.Sha256, StringComparison.OrdinalIgnoreCase))
			{
				LastDownloadError = $"SHA256 mismatch: expected {release.Sha256}, got {text}.";
				return false;
			}
		}
		Version version = ReadInstallerVersion(path);
		if (version == null)
		{
			LastDownloadError = "Could not read installer version from PE resource.";
			return false;
		}
		if (version < release.Version)
		{
			LastDownloadError = $"Installer reports version {version} which is older than the release's {release.Version}.";
			return false;
		}
		return true;
	}

	private static string? StageInstaller(string sourcePath, Version version)
	{
		try
		{
			Directory.CreateDirectory(DownloadFolder);
			string text = Path.Combine(DownloadFolder, $"PlexusX-Setup-{version}.exe");
			File.Move(sourcePath, text, overwrite: true);
			return text;
		}
		catch (Exception ex)
		{
			LastDownloadError = "Failed to stage installer: " + ex.Message;
			return null;
		}
	}

	internal static Version? ReadInstallerVersion(string path)
	{
		try
		{
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);
			if (!string.IsNullOrEmpty(versionInfo.FileVersion) && Version.TryParse(versionInfo.FileVersion, out Version result))
			{
				return GitHubReleases.NormalizeForCompare(result);
			}
			if (!string.IsNullOrEmpty(versionInfo.ProductVersion) && Version.TryParse(versionInfo.ProductVersion, out Version result2))
			{
				return GitHubReleases.NormalizeForCompare(result2);
			}
		}
		catch
		{
		}
		return GitHubReleases.ParseVersionFromFilename(Path.GetFileName(path));
	}

	internal static bool IsValidInstaller(string path)
	{
		try
		{
			using FileStream fileStream = File.OpenRead(path);
			if (fileStream.Length < 2)
			{
				return false;
			}
			Span<byte> buffer = stackalloc byte[2];
			return fileStream.Read(buffer) == 2 && buffer[0] == 77 && buffer[1] == 90;
		}
		catch
		{
			return false;
		}
	}

	public static bool RunInstallerSilently(string installerPath)
	{
		if (SecurityCandidate.IsEnabled)
		{
			return false;
		}
		try
		{
			if (!IsValidInstaller(installerPath))
			{
				SafeDelete(installerPath);
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static Task<bool> RunPendingUpdateIfAnyAsync(AppSettings settings)
	{
		return RunPendingUpdateIfAnyAsync(settings, FlavourPolicy.Current);
	}

	internal static async Task<bool> RunPendingUpdateIfAnyAsync(AppSettings settings, FlavourPolicy policy)
	{
		if (SecurityCandidate.IsEnabled)
		{
			return false;
		}
		if (!policy.SelfUpdates)
		{
			return false;
		}
		try
		{
			string text = await PendingInstallerWorthRunningAsync(settings, askTheManifest: true);
			if (text == null)
			{
				return false;
			}
			try
			{
				using FileStream fileStream = LockVerifiedPendingInstaller(settings, text);
				if (fileStream == null)
				{
					ClearPending(settings, SafeToDelete(text));
					return false;
				}
				if (StartOnce(text) == null)
				{
					return false;
				}
			}
			catch
			{
				NoteRefused(settings, text);
				throw;
			}
			ClearPending(settings, text);
			return true;
		}
		catch
		{
			ClearPending(settings, settings.PendingUpdateInstaller);
			return false;
		}
	}

	private static async Task<string?> PendingInstallerWorthRunningAsync(AppSettings settings, bool askTheManifest)
	{
		string pendingPath = ResolvePendingInstaller(settings);
		if (string.IsNullOrEmpty(pendingPath))
		{
			if (!string.IsNullOrEmpty(settings.PendingUpdateInstaller))
			{
				ClearPending(settings, SafeToDelete(settings.PendingUpdateInstaller));
			}
			return null;
		}
		if (!IsValidInstaller(pendingPath))
		{
			ClearPending(settings, pendingPath);
			return null;
		}
		Version pendingVersion = Version.Parse(settings.PendingUpdateVersion);
		if (!ShouldRunPendingInstaller(pendingVersion, CurrentVersion, null))
		{
			ClearPending(settings, pendingPath);
			return null;
		}
		if (askTheManifest)
		{
			ReleaseInfo releaseInfo = await TryGetUpdateAsync();
			if (!ShouldRunPendingInstaller(pendingVersion, CurrentVersion, releaseInfo?.Version))
			{
				ClearPending(settings, pendingPath);
				return null;
			}
		}
		return pendingPath;
	}

	internal static void ResetInstallerLatchForTest()
	{
		Interlocked.Exchange(ref _installerStarted, 0);
	}

	private static IInstallerProcess? StartOnce(string installerPath)
	{
		if (SecurityCandidate.IsEnabled)
		{
			return null;
		}
		if (Interlocked.CompareExchange(ref _installerStarted, 1, 0) != 0)
		{
			return null;
		}
		try
		{
			return StartInstallerProcess(new ProcessStartInfo
			{
				FileName = installerPath,
				Arguments = "/VERYSILENT /NORESTART /SUPPRESSMSGBOXES /SP- /FORCECLOSEAPPLICATIONS /CLOSEAPPLICATIONS",
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Path.GetTempPath()
			});
		}
		catch
		{
			Interlocked.Exchange(ref _installerStarted, 0);
			throw;
		}
	}

	internal static bool LaunchLeavesAfter(HandOff handOff)
	{
		return handOff == HandOff.CeilingReached;
	}

	internal static async Task<HandOff> HandOffToInstallerAsync(AppSettings settings, TimeSpan ceiling, bool askTheManifest = false, Action? beforeStart = null, FlavourPolicy? policy = null)
	{
		if (SecurityCandidate.IsEnabled || !(policy ?? FlavourPolicy.Current).SelfUpdates)
		{
			return HandOff.NothingToInstall;
		}
		string text;
		try
		{
			text = await PendingInstallerWorthRunningAsync(settings, askTheManifest);
		}
		catch
		{
			ClearPending(settings, settings.PendingUpdateInstaller);
			return HandOff.NothingToInstall;
		}
		if (text == null)
		{
			return HandOff.NothingToInstall;
		}
		try
		{
			beforeStart?.Invoke();
		}
		catch
		{
		}
		IInstallerProcess installerProcess;
		try
		{
			using FileStream fileStream = LockVerifiedPendingInstaller(settings, text);
			if (fileStream == null)
			{
				ClearPending(settings, SafeToDelete(text));
				return HandOff.NothingToInstall;
			}
			installerProcess = StartOnce(text);
		}
		catch
		{
			NoteRefused(settings, text);
			ClearPending(settings, text);
			return HandOff.CouldNotStart;
		}
		if (installerProcess == null)
		{
			return HandOff.AlreadyStarted;
		}
		settings.PendingUpdateInstaller = "";
		settings.PendingUpdateVersion = "";
		settings.PendingUpdateSha256 = "";
		settings.PendingUpdateSignedManifest = "";
		using CancellationTokenSource ceilingPassed = new CancellationTokenSource(ceiling);
		_ = 1;
		try
		{
			await installerProcess.WaitForExitAsync(ceilingPassed.Token);
			return HandOff.InstallerEnded;
		}
		catch (OperationCanceledException)
		{
			return HandOff.CeilingReached;
		}
		catch
		{
			return HandOff.InstallerEnded;
		}
	}

	public static bool RunPendingUpdateIfAny(AppSettings settings)
	{
		return RunPendingUpdateIfAnyAsync(settings).GetAwaiter().GetResult();
	}

	internal static bool ShouldRunPendingInstaller(Version? pendingVersion, Version currentVersion, Version? latestOnline)
	{
		if (pendingVersion == null)
		{
			return false;
		}
		if (Normalize(pendingVersion) <= Normalize(currentVersion))
		{
			return false;
		}
		if (latestOnline != null && Normalize(latestOnline) > Normalize(pendingVersion))
		{
			return false;
		}
		return true;
	}

	private static Version Normalize(Version v)
	{
		return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
	}

	internal static void CleanupObsoleteInstallers(string directory, Version currentVersion)
	{
		try
		{
			if (!Directory.Exists(directory))
			{
				return;
			}
			foreach (string item in Directory.EnumerateFiles(directory, "PlexusX-Setup-*.exe"))
			{
				try
				{
					Match match = Regex.Match(Path.GetFileName(item), "^PlexusX-Setup-(\\d+\\.\\d+(?:\\.\\d+)?)\\.exe$", RegexOptions.IgnoreCase);
					if (match.Success && Version.TryParse(match.Groups[1].Value, out Version result) && !(Normalize(result) > Normalize(currentVersion)))
					{
						File.Delete(item);
					}
				}
				catch
				{
				}
			}
			foreach (string item2 in Directory.EnumerateFiles(directory, "PlexusX-Setup-*.partial"))
			{
				try
				{
					Match match2 = Regex.Match(Path.GetFileName(item2), "^PlexusX-Setup-(\\d+\\.\\d+(?:\\.\\d+)?)-[0-9a-z]+\\.partial$", RegexOptions.IgnoreCase);
					if (match2.Success && Version.TryParse(match2.Groups[1].Value, out Version result2) && !(Normalize(result2) > Normalize(currentVersion)))
					{
						File.Delete(item2);
					}
				}
				catch
				{
				}
			}
			foreach (string item3 in Directory.EnumerateFiles(directory, "PlexusX-dl-*.partial"))
			{
				try
				{
					if (DateTime.UtcNow - File.GetLastWriteTimeUtc(item3) > TimeSpan.FromDays(1.0))
					{
						File.Delete(item3);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	public static void CleanupObsoleteInstallers()
	{
		CleanupObsoleteInstallers(DownloadFolder, CurrentVersion);
	}

	internal static string? ResolvePendingInstaller(AppSettings settings)
	{
		try
		{
			if (string.IsNullOrEmpty(settings.PendingUpdateInstaller) || !File.Exists(settings.PendingUpdateInstaller))
			{
				return null;
			}
			byte[] verification = UpdateKeys.Verification;
			if (!UpdateManifestVerifier.TryVerify(settings.PendingUpdateSignedManifest, verification, out UpdateManifest manifest) || manifest == null)
			{
				return null;
			}
			if (!Version.TryParse(manifest.Version, out Version result) || !Version.TryParse(settings.PendingUpdateVersion, out Version result2) || Normalize(result) != Normalize(result2))
			{
				return null;
			}
			string text = manifest.Sha256.Trim().ToLowerInvariant();
			if (text.Length != 64 || !string.Equals(text, settings.PendingUpdateSha256.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			return (FileSha256(settings.PendingUpdateInstaller) == text) ? settings.PendingUpdateInstaller : null;
		}
		catch
		{
			return null;
		}
	}

	private static FileStream? LockVerifiedPendingInstaller(AppSettings settings, string path)
	{
		FileStream fileStream = null;
		try
		{
			fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			if (string.Equals(path, ResolvePendingInstaller(settings), StringComparison.OrdinalIgnoreCase))
			{
				return fileStream;
			}
		}
		catch
		{
		}
		fileStream?.Dispose();
		return null;
	}

	internal static string FileSha256(string path)
	{
		try
		{
			using FileStream inputStream = File.OpenRead(path);
			using SHA256 sHA = SHA256.Create();
			return Convert.ToHexString(sHA.ComputeHash(inputStream)).ToLowerInvariant();
		}
		catch
		{
			return "";
		}
	}

	internal static string? SafeToDelete(string? path)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return null;
			}
			string fullPath = Path.GetFullPath(path);
			string value = Path.GetFullPath(DownloadFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			if (!fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			return Regex.IsMatch(Path.GetFileName(fullPath), "^PlexusX-Setup-\\d+\\.\\d+(?:\\.\\d+)?\\.exe$", RegexOptions.IgnoreCase) ? fullPath : null;
		}
		catch
		{
			return null;
		}
	}

	private static void NoteRefused(AppSettings settings, string installerPath)
	{
		try
		{
			string text = settings.PendingUpdateSha256;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = FileSha256(installerPath);
			}
			RefusedUpdate.Record(settings, text);
		}
		catch
		{
		}
	}

	private static void ClearPending(AppSettings settings, string? path)
	{
		settings.PendingUpdateInstaller = "";
		settings.PendingUpdateVersion = "";
		settings.PendingUpdateSha256 = "";
		settings.PendingUpdateSignedManifest = "";
		try
		{
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	internal static string? WhatTheOldTempSweepWouldHaveFound(string? dir = null)
	{
		try
		{
			string? path = dir ?? Path.GetTempPath();
			Version currentVersion = CurrentVersion;
			string result = null;
			Version version = null;
			foreach (string item in Directory.EnumerateFiles(path, "PlexusX-Setup-*.exe"))
			{
				Match match = Regex.Match(Path.GetFileName(item), "PlexusX-Setup-(\\d+\\.\\d+(?:\\.\\d+)?)\\.exe$");
				if (match.Success && IsValidInstaller(item) && Version.TryParse(match.Groups[1].Value, out Version result2) && !(result2 <= currentVersion) && (version == null || result2 > version))
				{
					result = item;
					version = result2;
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	public static Task CheckManuallyAsync()
	{
		return CheckManuallyAsync(FlavourPolicy.Current, StoreHooks.OpenUpdates);
	}

	public static Task CheckManuallyAsync(IWin32Window? owner)
	{
		return CheckManuallyAsync(FlavourPolicy.Current, StoreHooks.OpenUpdates, owner);
	}

	private static string? CustomerReason(string? lastError)
	{
		if (string.IsNullOrWhiteSpace(lastError))
		{
			return null;
		}
		if (lastError.Contains("Windows removed the downloaded installer", StringComparison.Ordinal))
		{
			return "Your antivirus or SmartScreen removed the downloaded installer.";
		}
		if (lastError.StartsWith("This update did not come with a checksum", StringComparison.Ordinal))
		{
			return lastError;
		}
		string text = lastError.ToLowerInvariant();
		if (text.Contains("signature"))
		{
			return "The update could not be verified.";
		}
		if (text.Contains("host") || text.Contains("network") || text.Contains("name resolution") || text.Contains("internet") || text.Contains("dns"))
		{
			return "You seem to be offline.";
		}
		if (text.Contains("timed out") || text.Contains("timeout") || text.Contains("connection") || text.Contains("unreachable") || text.Contains("refused") || text.Contains("404") || text.Contains("response status"))
		{
			return "The update server could not be reached.";
		}
		return null;
	}

	internal static async Task CheckManuallyAsync(FlavourPolicy policy, Action? openStoreUpdates, IWin32Window? owner = null)
	{
		if (!policy.SelfUpdates)
		{
			StoreHooks.TryOpen(openStoreUpdates);
			return;
		}
		lock (_checkLock)
		{
			if (_isChecking)
			{
				return;
			}
			_isChecking = true;
		}
		try
		{
			ReleaseInfo update = await TryGetUpdateAsync();
			if (update == null)
			{
				if (OutcomeOf(update, LastCheckError) == CheckOutcome.CouldNotCheck)
				{
					string text = CustomerReason(LastCheckError);
					GlassDialog.Show(owner, "Couldn't check for updates", "PlexusX couldn't reach the update server, so it doesn't know " + $"whether there's a newer version than {CurrentVersion}.\n\n" + "Check your internet connection and try again, or download the latest version from " + ActivationEndpoint.SiteOrigin + "." + ((text == null) ? "" : ("\n\n" + text)), GlassDialogButtons.Ok, GlassDialogTone.Warning);
				}
				else
				{
					GlassDialog.Show(owner, "You're up to date", $"PlexusX {CurrentVersion} is the latest version.");
				}
			}
			else
			{
				if (GlassDialog.Show(owner, "Update available", $"PlexusX {update.Version} is ready to install - you're on {CurrentVersion}.\n\n" + "PlexusX will close, and finish installing the next time you open it.", GlassDialogButtons.YesNo, GlassDialogTone.Info, "Install update") != DialogResult.Yes)
				{
					return;
				}
				Progress<int> progress = new Progress<int>((int p) =>
				{
					_splash?.SetStatus($"Downloading update {update.Version}…", p);
				});
				string text2 = await DownloadAndStageAsync(update, progress);
				if (text2 == null || !RunInstallerSilently(text2))
				{
					if (!string.IsNullOrEmpty(LastDownloadError))
					{
						CrashLog.Write(new InvalidOperationException(LastDownloadError));
					}
					string text3 = CustomerReason(LastDownloadError);
					GlassDialog.Show(owner, "Update didn't download", "The update couldn't be downloaded. You can get it from " + ActivationEndpoint.SiteOrigin + " instead." + ((text3 == null) ? "" : ("\n\n" + text3)), GlassDialogButtons.Ok, GlassDialogTone.Warning);
				}
				else
				{
					RecordPending?.Invoke(update, text2);
					GlassDialog.Show(owner, "Ready to install", $"PlexusX {update.Version} has been downloaded. It installs the next time " + "you open PlexusX, which is about to close.");
					Application.Exit();
				}
			}
		}
		finally
		{
			_isChecking = false;
		}
	}

	public static void SetSplashForm(SplashForm? splash)
	{
		_splash = splash;
	}

	internal static void SafeDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}
}
