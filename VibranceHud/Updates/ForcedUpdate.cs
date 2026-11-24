using System;
using System.Threading.Tasks;

namespace VibranceHud.Updates;

public static class ForcedUpdate
{
	internal static async Task<UpdateService.HandOff> TryBeforeTheDeadEndAsync(AppSettings settings, Action save, TimeSpan ceiling, Func<Task<ReleaseInfo?>>? fetch = null, Func<ReleaseInfo, IProgress<int>?, Task<string?>>? download = null, IProgress<int>? progress = null, Action<string>? status = null)
	{
		try
		{
			string stagedVersion = settings.PendingUpdateVersion;
			UpdateService.HandOff handOff = await UpdateService.HandOffToInstallerAsync(settings, ceiling, askTheManifest: false, () =>
			{
				status?.Invoke("Installing PlexusX " + stagedVersion + "…");
			});
			Persist(save);
			if (handOff != UpdateService.HandOff.NothingToInstall)
			{
				return handOff;
			}
			if (!RemoteSwitches.IsOn("updates"))
			{
				return UpdateService.HandOff.NothingToInstall;
			}
			ReleaseInfo release = await (fetch ?? new Func<Task<ReleaseInfo>>(UpdateService.TryGetUpdateAsync))();
			release = RefusedUpdate.Unless(settings, release);
			if (release == null)
			{
				return UpdateService.HandOff.NothingToInstall;
			}
			status?.Invoke($"Downloading update {release.Version}…");
			string text = await (download ?? new Func<ReleaseInfo, IProgress<int>, Task<string>>(UpdateService.DownloadAndStageAsync))(release, progress);
			if (string.IsNullOrWhiteSpace(text))
			{
				return UpdateService.HandOff.NothingToInstall;
			}
			settings.PendingUpdateInstaller = text;
			settings.PendingUpdateVersion = release.Version.ToString();
			settings.PendingUpdateSha256 = UpdateService.FileSha256(text);
			settings.PendingUpdateSignedManifest = release.SignedManifest ?? "";
			Persist(save);
			UpdateService.HandOff result = await UpdateService.HandOffToInstallerAsync(settings, ceiling, askTheManifest: false, () =>
			{
				status?.Invoke($"Installing PlexusX {release.Version}…");
			});
			Persist(save);
			return result;
		}
		catch
		{
			return UpdateService.HandOff.NothingToInstall;
		}
	}

	private static void Persist(Action save)
	{
		try
		{
			save();
		}
		catch
		{
		}
	}
}
