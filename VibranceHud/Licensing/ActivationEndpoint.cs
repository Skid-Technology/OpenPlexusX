using System;

namespace VibranceHud.Licensing;

internal static class ActivationEndpoint
{
	internal const string Origin = "https://plexusx.lol";

	internal const string SiteOrigin = "https://plexusx.lol";

	internal static string SwitchesUrl => Origin + "/api/app/switches";

	internal static string LibraryUrl => Origin + "/api/presets/library";

	internal static string StatusUrlFor(string channel)
	{
		return Origin + "/api/app/status?channel=" + Uri.EscapeDataString(channel ?? "");
	}
}
