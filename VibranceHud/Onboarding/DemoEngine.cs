using System;

namespace VibranceHud.Onboarding;

public static class DemoEngine
{
	internal sealed class NeverTouchesGamma : IGammaRamp
	{
		public void Apply(ushort[] ramp)
		{
			throw new InvalidOperationException("The first-run demonstration must never write a gamma ramp. Windows does not restore ramps when a process dies, so a crash mid-demo would leave a stranger's monitor altered with no PlexusX behind it. Everything the demo shows goes through the colour matrix, which dies with the process.");
		}

		public void Reset()
		{
		}
	}

	public readonly record struct Built(VibranceEngine? Engine, ISaturationOverlay? Overlay);

	public static Built TryBuild(Func<ISaturationOverlay> overlay, Func<IVibranceController> driver, Action<Exception> log)
	{
		if (overlay == null)
		{
			throw new ArgumentNullException("overlay");
		}
		if (driver == null)
		{
			throw new ArgumentNullException("driver");
		}
		ISaturationOverlay saturationOverlay = null;
		try
		{
			saturationOverlay = overlay();
			return new Built(Create(saturationOverlay, driver().DriverState), saturationOverlay);
		}
		catch (Exception obj)
		{
			try
			{
				log?.Invoke(obj);
			}
			catch
			{
			}
			if (saturationOverlay is IDisposable disposable)
			{
				try
				{
					disposable.Dispose();
				}
				catch
				{
				}
			}
			return default;
		}
	}

	public static VibranceEngine Create(ISaturationOverlay overlay, VibranceDriverState driverState)
	{
		if (overlay == null)
		{
			throw new ArgumentNullException("overlay");
		}
		return new VibranceEngine(new NullVibranceController(driverState), overlay, new NeverTouchesGamma())
		{
			PaidControlsUnlocked = true
		};
	}
}
