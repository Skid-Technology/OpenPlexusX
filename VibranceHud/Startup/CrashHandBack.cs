using System.Threading;

namespace VibranceHud.Startup;

internal static class CrashHandBack
{
	private static MultiMonitorGammaRamp? _ramp;

	internal static void Register(MultiMonitorGammaRamp ramp)
	{
		Volatile.Write(ref _ramp, ramp);
	}

	internal static void Forget()
	{
		Volatile.Write(ref _ramp, null);
	}

	internal static void Run()
	{
		MultiMonitorGammaRamp multiMonitorGammaRamp = Interlocked.Exchange(ref _ramp, null);
		if (multiMonitorGammaRamp == null)
		{
			return;
		}
		try
		{
			multiMonitorGammaRamp.Dispose();
		}
		catch
		{
		}
	}
}
