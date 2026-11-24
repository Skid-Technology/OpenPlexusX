using System;
using System.Linq;
using System.Threading;
using VibranceHud.Monitors;

namespace VibranceHud.Startup;

internal static class RestoreMonitorSwitch
{
	internal const string Switch = "--restore-monitor";

	internal const int DeadlineMs = 10000;

	internal static int? TryRun(string[] args, Action? work = null, int timeoutMs = 10000)
	{
		if (args == null || !args.Any((string a) => string.Equals(a, "--restore-monitor", StringComparison.OrdinalIgnoreCase)))
		{
			return null;
		}
		try
		{
			Action job = work ?? new Action(RealWork);
			Thread thread = new Thread(() =>
			{
				try
				{
					job();
				}
				catch
				{
				}
			});
			thread.IsBackground = true;
			thread.Name = "restore-monitor";
			thread.Start();
			thread.Join(timeoutMs);
		}
		catch
		{
		}
		return 0;
	}

	private static void RealWork()
	{
		SettingsStore settingsStore = SettingsStore.Default();
		AppSettings appSettings = settingsStore.Load();
		MonitorPanicRestore.Run(appSettings, settingsStore, new Dxva2MonitorWriter(), MonitorProbe.Probe, ColdStartRestore.GammaLinearNow(appSettings, settingsStore), ColdStartRestore.DriverBaselineNow(appSettings, settingsStore));
	}
}
