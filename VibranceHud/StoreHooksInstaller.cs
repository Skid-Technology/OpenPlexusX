using System.Runtime.CompilerServices;

namespace VibranceHud;

internal static class StoreHooksInstaller
{
	[ModuleInitializer]
	internal static void Install()
	{
		StoreHooks.StartupTask = () => new StoreStartupTask();
		StoreHooks.LaunchedAtSignIn = StoreActivation.LaunchedAtSignIn;
		StoreHooks.OpenUpdates = StoreLinks.OpenUpdates;
		StoreHooks.OpenProductPage = StoreLinks.OpenProductPage;
		StoreHooks.RegisterRestart = StoreRestart.Register;
	}
}
