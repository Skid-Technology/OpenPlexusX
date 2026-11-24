using System;

namespace VibranceHud;

public static class OutsideDriverChange
{
	public static void AtLaunch(VibranceEngine engine, AppSettings settings, IVibranceController driver)
	{
		AppSettings.MigrateVibranceOwnership(settings);
		try
		{
			if (settings.VibranceChosenByUser && driver.IsAvailable)
			{
				int? driverVibranceWeLastWrote = settings.DriverVibranceWeLastWrote;
				if (driverVibranceWeLastWrote.HasValue)
				{
					int valueOrDefault = driverVibranceWeLastWrote.GetValueOrDefault();
					if (HoldsALevelOtherThan(driver, valueOrDefault))
					{
						LetGo(settings);
					}
				}
				else
				{
					int num = Math.Min(settings.ResolvedVibrance, 100);
					if (HoldsALevelOtherThan(driver, num))
					{
						LetGo(settings);
					}
					else
					{
						settings.DriverVibranceWeLastWrote = num;
					}
				}
			}
		}
		catch
		{
		}
		engine.DriverLevelWeLastWrote = settings.DriverVibranceWeLastWrote;
	}

	internal static bool HoldsALevelOtherThan(IVibranceController driver, int level)
	{
		if (!driver.EveryDisplayIsAt(level))
		{
			return driver.CurrentLevel != level;
		}
		return false;
	}

	private static void LetGo(AppSettings settings)
	{
		settings.VibranceChosenByUser = false;
		settings.DriverVibranceWeLastWrote = null;
	}
}
