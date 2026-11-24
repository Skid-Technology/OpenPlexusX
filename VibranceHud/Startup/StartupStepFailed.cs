using System;

namespace VibranceHud.Startup;

public sealed class StartupStepFailed : Exception
{
	public string Step { get; }

	public StartupStepFailed(string step, Exception inner)
		: base("PlexusX failed while " + step + ".", inner)
	{
		Step = step ?? "";
	}
}
