using VibranceHud.Crosshair;
using VibranceHud.Display;

namespace VibranceHud.Presets;

public sealed record ConfigLibraryEntry(string Name, string Author, string Game, string Blurb, ConfigCode.Parts Setup)
{
	public string? CommunityId { get; init; }

	public string Code => ConfigCode.Encode(Setup.Look, Setup.Crosshair, Setup.Mode);

	public DisplayPreset? Look
	{
		get
		{
			ProfileCode? look = Setup.Look;
			if (look.HasValue)
			{
				ProfileCode valueOrDefault = look.GetValueOrDefault();
				return CommunityCatalogue.LookFrom(valueOrDefault, Name, Blurb);
			}
			return null;
		}
	}

	public CrosshairConfig? Crosshair => Setup.Crosshair;

	public MonitorCode? Mode => Setup.Mode;

	public bool ChangesTheScreenMode => Setup.Mode.HasValue;

	public string Summary()
	{
		return string.Join("  ·  ", ConfigCode.Describe(Setup));
	}
}
