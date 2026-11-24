namespace VibranceHud.Presets;

public sealed record LibraryPreset(string Name, string Author, string Game, string Blurb, DisplayPreset Look)
{
	public string? CommunityId { get; init; }

	public string Code => ProfileCode.Encode(new ProfileCode(Look.Vibrance, Look.Saturation, Look.Brightness, Look.Gamma, Look.Contrast, Look.Temperature, Look.Highlights, Look.Shadows, Look.ColourVision).WithDepth(Look.Depth()));
}
