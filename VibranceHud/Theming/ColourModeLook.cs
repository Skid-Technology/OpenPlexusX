using VibranceHud.Design;

namespace VibranceHud.Theming;

public static class ColourModeLook
{
	public const int Version = 1;

	public static bool Settle(AppSettings settings)
	{
		if (settings.ColourModeLookVersion >= 1)
		{
			return false;
		}
		if (!SignatureLooks.TryGet(settings.ThemeName, out var look))
		{
			SignatureLooks.TryGet("Ash", out look);
		}
		settings.ThemeName = look.Theme;
		settings.BackdropLookName = BackdropLooks.Name(look.Backdrop);
		settings.SurfaceStyleName = SurfaceStyles.Name(look.Surface);
		settings.ChromeName = Chromes.Name(look.Chrome);
		settings.ColourModeLookVersion = 1;
		return true;
	}
}
