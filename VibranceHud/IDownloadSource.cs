using System;
using System.Threading.Tasks;

namespace VibranceHud;

internal interface IDownloadSource
{
	string Name { get; }

	Task<string?> DownloadAsync(IProgress<int>? progress);
}
