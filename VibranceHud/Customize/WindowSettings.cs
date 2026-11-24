namespace VibranceHud.Customize;

public sealed record WindowSettings
{
	public int CornerRadiusPx { get; init; }

	public bool SnapAssistSuggestions { get; init; } = true;

	public bool TransparencyEnabled { get; init; } = true;

	public StartLayoutMode StartLayout { get; init; }
}
