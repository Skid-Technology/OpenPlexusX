using System;

namespace VibranceHud;

public readonly record struct PipeRequest(PipeVerb Verb, string Payload)
{
	public const string ShowToken = "plexusx-control:show";

	public const string QuitToken = "plexusx-control:quit";

	public static PipeRequest Parse(string? raw)
	{
		string text = (raw ?? "").Trim();
		if (string.Equals(text, "plexusx-control:show", StringComparison.OrdinalIgnoreCase))
		{
			return new PipeRequest(PipeVerb.Show, "");
		}
		if (string.Equals(text, "plexusx-control:quit", StringComparison.OrdinalIgnoreCase))
		{
			return new PipeRequest(PipeVerb.Quit, "");
		}
		return new PipeRequest(PipeVerb.Link, text);
	}
}
