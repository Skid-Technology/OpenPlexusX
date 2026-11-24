using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Games;

public sealed record SupportedGame(string Id, string DisplayName, int SteamAppId, string InstallFolder, string ProcessName, string? EpicAppName = null, IReadOnlyList<string>? AlsoProcessNames = null)
{
	public IReadOnlyList<string> AllProcessNames
	{
		get
		{
			if (AlsoProcessNames == null || AlsoProcessNames.Count == 0)
			{
				return new string[1] { ProcessName };
			}
			return new string[1] { ProcessName }.Concat(AlsoProcessNames).ToArray();
		}
	}
}
