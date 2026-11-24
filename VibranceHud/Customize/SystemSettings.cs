namespace VibranceHud.Customize;

public sealed record SystemSettings
{
	public bool DarkMode { get; init; }

	public uint AccentColorArgb { get; init; }

	public bool TitleBarsUseAccent { get; init; }

	public bool BordersUseAccent { get; init; }
}
