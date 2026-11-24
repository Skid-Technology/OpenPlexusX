namespace VibranceHud;

public static class OverlayModeResolver
{
	public static OverlayMode Resolve(ISaturationOverlay overlay)
	{
		return (overlay as IDisplayOverlay)?.ActiveMode ?? OverlayMode.Dx;
	}
}
