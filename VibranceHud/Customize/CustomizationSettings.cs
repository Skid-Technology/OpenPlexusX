using System.Text.Json.Serialization;

namespace VibranceHud.Customize;

public sealed record CustomizationSettings
{
	public TaskbarSettings Taskbar { get; init; } = new TaskbarSettings();

	public SystemSettings System { get; init; } = new SystemSettings();

	public WindowSettings Windows { get; init; } = new WindowSettings();

	public ExplorerSettings Explorer { get; init; } = new ExplorerSettings();

	[JsonIgnore]
	public TaskbarStyleSupport TaskbarStylerSupport { get; init; } = TaskbarStyleSupport.Unknown;

	public static CustomizationSettings Default()
	{
		return new CustomizationSettings();
	}
}
