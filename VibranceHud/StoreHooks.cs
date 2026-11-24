using System;

namespace VibranceHud;

internal static class StoreHooks
{
	internal static Func<IStartupRegistration>? StartupTask { get; set; }

	internal static Func<bool>? LaunchedAtSignIn { get; set; }

	internal static Action? OpenUpdates { get; set; }

	internal static Action? OpenProductPage { get; set; }

	internal static Action? RegisterRestart { get; set; }

	internal static bool TryOpen(Action? door)
	{
		if (door == null)
		{
			return false;
		}
		try
		{
			door();
			return true;
		}
		catch
		{
			return false;
		}
	}
}
