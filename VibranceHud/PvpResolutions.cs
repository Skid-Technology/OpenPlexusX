using System.Collections.Generic;

namespace VibranceHud;

public static class PvpResolutions
{
	public const string StretchNote = "4:3 only stretches if scaling is set to Full screen — use the Scaling card below to set it. Left on Aspect ratio or No scaling, you'll get black bars down the sides instead.";

	public static IReadOnlyList<PvpResolution> All { get; } = new PvpResolution[3]
	{
		new PvpResolution("Native", 1920, 1080, "16:9", "Sharpest picture and the full field of view.", "Nothing is stretched or cropped.", NeedsStretching: false),
		new PvpResolution("Stretched 4:3", 1440, 1080, "4:3", "Player models come out wider and it runs faster than native.", "You lose some view at the left and right.", NeedsStretching: true),
		new PvpResolution("Low 4:3", 1280, 960, "4:3", "The most frames per second.", "The softest picture of the three.", NeedsStretching: true)
	};
}
