namespace VibranceHud;

public static class SavedVibranceRestore
{
	public static void Apply(IVibranceEngine engine, AppSettings settings)
	{
		AppSettings.MigrateVibranceOwnership(settings);
		if (settings.VibranceChosenByUser)
		{
			engine.Vibrance = settings.ResolvedVibrance;
		}
		else
		{
			engine.SeedVibrance(settings.ResolvedVibrance);
		}
		settings.VibrancePercent = engine.Vibrance;
		settings.VibranceChosenByUser = engine.VibranceChosenByUser;
	}
}
