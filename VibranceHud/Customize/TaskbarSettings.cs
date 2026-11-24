namespace VibranceHud.Customize;

public sealed record TaskbarSettings
{
	public TaskbarStyle Style { get; init; }

	public uint ColorArgb { get; init; }

	public bool UseAccentForTaskbar { get; init; }

	public TaskbarButtonGlom Buttons { get; init; }

	public TaskbarAlignment Alignment { get; init; } = TaskbarAlignment.Centre;

	public bool SmallButtons { get; init; }

	public SearchBoxMode Search { get; init; } = SearchBoxMode.Box;

	public bool ShowTaskView { get; init; } = true;

	public bool ShowWidgets { get; init; } = true;
}
