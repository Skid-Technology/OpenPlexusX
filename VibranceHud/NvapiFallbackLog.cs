using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace VibranceHud;

internal static class NvapiFallbackLog
{
	internal const string FileName = "nvapi-fail.log";

	internal const string CustomModeFileName = "custom-mode.log";

	internal static string CustomModeLine(DateTime whenUtc, string step, string status)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss} UTC  NVIDIA custom resolution {1} refused: {2}.", whenUtc, step, status);
	}

	internal static void NoteCustomMode(string step, string status)
	{
		try
		{
			if (ThisIsTheRealApp())
			{
				string crashDirectory = CrashLog.CrashDirectory;
				if (!string.IsNullOrEmpty(crashDirectory))
				{
					File.AppendAllText(Path.Combine(crashDirectory, "custom-mode.log"), CrashLog.Redact(CustomModeLine(DateTime.UtcNow, step, status)) + Environment.NewLine, Encoding.UTF8);
				}
			}
		}
		catch
		{
		}
	}

	internal static string CustomModeStepLine(DateTime whenUtc, string what, string status)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss} UTC  custom resolution {1}: {2}", whenUtc, what, status);
	}

	internal static void NoteCustomModeStep(string what, string status)
	{
		try
		{
			if (ThisIsTheRealApp())
			{
				string crashDirectory = CrashLog.CrashDirectory;
				if (!string.IsNullOrEmpty(crashDirectory))
				{
					File.AppendAllText(Path.Combine(crashDirectory, "custom-mode.log"), CrashLog.Redact(CustomModeStepLine(DateTime.UtcNow, what, status)) + Environment.NewLine, Encoding.UTF8);
				}
			}
		}
		catch
		{
		}
	}

	internal static string Line(DateTime whenUtc, Exception cause)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss} UTC  NVIDIA stopped answering ({1}: {2}). Re-resolving the display handles did not help, so vibrance continues in software for the rest of this session.", whenUtc, cause?.GetType().Name ?? "unknown", cause?.Message ?? "no message");
	}

	internal static void Note(Exception cause)
	{
		try
		{
			if (ThisIsTheRealApp())
			{
				string crashDirectory = CrashLog.CrashDirectory;
				if (!string.IsNullOrEmpty(crashDirectory))
				{
					File.AppendAllText(Path.Combine(crashDirectory, "nvapi-fail.log"), CrashLog.Redact(Line(DateTime.UtcNow, cause)) + Environment.NewLine, Encoding.UTF8);
				}
			}
		}
		catch
		{
		}
	}

	private static bool ThisIsTheRealApp()
	{
		try
		{
			return string.Equals(Path.GetFileName(Application.ExecutablePath), "PlexusX.exe", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}
}
