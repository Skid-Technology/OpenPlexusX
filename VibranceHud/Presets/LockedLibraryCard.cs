namespace VibranceHud.Presets;

public sealed record LockedLibraryCard(string Kind, string Name, string Author, string Game, string Blurb)
{
	public string? CommunityId { get; init; }
}
