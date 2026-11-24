using System;
using System.IO;
using System.Security;

namespace VibranceHud.Startup;

public static class StartupFailure
{
	public const string MonitorSentence = "Make sure at least one monitor is connected and try again.";

	public const string CarriedOn = "PlexusX hit a problem, but it is still running.\n\nWhat you just did may not have worked. If PlexusX acts strange, close it and open it again.";

	public const string HadToClose = "PlexusX hit a problem and had to close.\n\nOpen PlexusX again. If this keeps happening, email support@plexusx.lol.";

	public const string StuckCopyQuestion = "PlexusX is already open, but it is stuck.\n\nThis can happen when a game or your graphics card crashes.\n\nClose the stuck copy and start PlexusX again?";

	public const string StuckCopyWouldNotClose = "PlexusX couldn't close the stuck copy.\n\nPress Ctrl+Shift+Esc to open Task Manager. Pick PlexusX, press End task, then start PlexusX again.";

	public const string StuckCopyClosed = "The stuck copy is closed. Start PlexusX again.";

	private const string Opening = "PlexusX couldn't start.";

	private const string Closing = "If this keeps happening, email support@plexusx.lol.";

	public static string Describe(Exception? ex)
	{
		if (ex is StartupStepFailed startupStepFailed)
		{
			if (startupStepFailed.Step == "setting up the monitors attached to this PC" && !IsAFileThatWouldNotLoad(startupStepFailed.InnerException))
			{
				return "PlexusX couldn't start.\n\nPlexusX could not set up the monitors attached to this PC. Make sure at least one monitor is connected and try again.\n\nIf this keeps happening, email support@plexusx.lol.";
			}
			return "PlexusX couldn't start.\n\nIt stopped while " + startupStepFailed.Step + ". " + Because(startupStepFailed.InnerException) + "\n\nIf this keeps happening, email support@plexusx.lol.";
		}
		return "PlexusX couldn't start.\n\n" + Because(ex) + "\n\nIf this keeps happening, email support@plexusx.lol.";
	}

	public static Exception? Detail(Exception? ex)
	{
		if (!(ex is StartupStepFailed { InnerException: not null } startupStepFailed))
		{
			return ex;
		}
		return startupStepFailed.InnerException;
	}

	private static bool IsAFileThatWouldNotLoad(Exception? ex)
	{
		if (ex is FileNotFoundException || ex is FileLoadException || ex is BadImageFormatException || ex is TypeLoadException)
		{
			return true;
		}
		return false;
	}

	private static string Because(Exception? ex)
	{
		if (!(ex is UnauthorizedAccessException) && !(ex is SecurityException))
		{
			if (!(ex is FileNotFoundException) && !(ex is FileLoadException) && !(ex is DirectoryNotFoundException) && !(ex is BadImageFormatException) && !(ex is TypeLoadException))
			{
				if (!(ex is IOException))
				{
					if (ex is OutOfMemoryException)
					{
						return "This PC ran out of memory. Closing something else and trying again usually works.";
					}
					return "Something PlexusX needs did not work.";
				}
				return "A file PlexusX needs could not be read.";
			}
			return FlavourWording.MissingFileBecause(FlavourPolicy.Current);
		}
		return "Windows did not give PlexusX permission to do this.";
	}
}
