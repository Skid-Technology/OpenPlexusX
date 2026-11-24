using VibranceHud.Display;

namespace VibranceHud.Presets;

public sealed record DisplayLibraryEntry(string Name, string Author, string Game, string Blurb, MonitorCode Mode)
{
	public string? CommunityId { get; init; }

	public string Code => MonitorCode.Encode(Mode);

	public string Summary()
	{
		return Mode.Label() + "  ·  " + Mode.Scaling.Label();
	}
}
