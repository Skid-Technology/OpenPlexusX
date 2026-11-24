using System.Drawing;

namespace VibranceHud;

public sealed record ThemePalette(string Name, bool IsLight, Color Background, Color Surface, Color SurfaceHover, Color Border, Color GlassFill, Color GlassEdge, Color Text, Color TextDim, Color Accent, Color AccentDim, Color PlexusNodeA, Color PlexusNodeB, Color PlexusLine, string? FontFamily = null, double PanelOpacity = 1.0, Color TextFaint = default(Color))
{
	public Color Swatch => Background;

	public bool IsSeeThrough => PanelOpacity < 1.0;
}
