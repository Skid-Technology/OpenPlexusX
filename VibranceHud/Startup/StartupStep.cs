using System;
using System.IO;

namespace VibranceHud.Startup;

public static class StartupStep
{
	public const string TheMonitors = "setting up the monitors attached to this PC";

	public const string YourSettings = "reading your settings";

	public const string YourLicence = "checking your licence";

	public const string TheStartupList = "remembering whether PlexusX starts with Windows";

	public const string TheBuildCheck = "checking this copy of PlexusX";

	public const string TheFirstRunWizard = "showing the first-run setup";

	public const string TheScreenAfterACrash = "putting your screen back after PlexusX was closed unexpectedly";

	public const string TheRequestChannel = "listening for links, a second launch and the installer";

	public const string TheSecondLaunch = "handing over to the PlexusX that is already running";

	public const string TheTaskbarPin = "pointing your taskbar pin at this PlexusX";

	internal const string SkippedLogName = "startup-skipped.log";

	internal static Action<string, Exception>? Recorder;

	public static bool Skippable(string step, Action work)
	{
		if (work == null)
		{
			return false;
		}
		try
		{
			work();
			return true;
		}
		catch (Exception ex)
		{
			Record(step, ex);
			return false;
		}
	}

	public static void Required(string step, Action work)
	{
		try
		{
			work();
		}
		catch (StartupStepFailed)
		{
			throw;
		}
		catch (Exception inner)
		{
			throw new StartupStepFailed(step, inner);
		}
	}

	public static T Required<T>(string step, Func<T> work)
	{
		try
		{
			return work();
		}
		catch (StartupStepFailed)
		{
			throw;
		}
		catch (Exception inner)
		{
			throw new StartupStepFailed(step, inner);
		}
	}

	internal static void Record(string step, Exception ex)
	{
		Action<string, Exception> recorder = Recorder;
		if (recorder != null)
		{
			recorder(step, ex);
			return;
		}
		try
		{
			File.AppendAllText(Path.Combine(CrashLog.CrashDirectory, "startup-skipped.log"), DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "  skipped: " + step + " - " + CrashLog.Redact(ex.GetType().Name + ": " + ex.Message) + Environment.NewLine);
		}
		catch
		{
		}
	}
}
