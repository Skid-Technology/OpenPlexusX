using System;
using System.Threading.Tasks;

namespace VibranceHud;

internal sealed class GitHubReleasesSource : IDownloadSource
{
	private readonly ReleaseInfo _release;

	public string Name => "GitHub Releases (" + _release.InstallerUrl + ")";

	public GitHubReleasesSource(ReleaseInfo release)
	{
		_release = release;
	}

	public async Task<string?> DownloadAsync(IProgress<int>? progress)
	{
		if (string.IsNullOrEmpty(_release.InstallerUrl))
		{
			return null;
		}
		return await DownloadHelper.DownloadToTempAsync(new Uri(_release.InstallerUrl), UpdateService.PartialPathFor(_release), progress);
	}
}
