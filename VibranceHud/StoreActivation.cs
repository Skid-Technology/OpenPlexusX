using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;

namespace VibranceHud;

internal static class StoreActivation
{
	internal static bool LaunchedAtSignIn()
	{
		try
		{
			return AskWindows();
		}
		catch
		{
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool AskWindows()
	{
		IActivatedEventArgs activatedEventArgs = AppInstance.GetActivatedEventArgs();
		if (activatedEventArgs != null)
		{
			return activatedEventArgs.Kind == ActivationKind.StartupTask;
		}
		return false;
	}
}
