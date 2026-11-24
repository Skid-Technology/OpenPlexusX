using VibranceHud.Crosshair;

namespace VibranceHud.Presets;

public sealed record CrosshairLibraryEntry(string Name, string Author, string Game, string Blurb, CrosshairConfig Crosshair)
{
	public string? CommunityId { get; init; }

	public string Code => CrosshairCode.Encode(Crosshair);
}
