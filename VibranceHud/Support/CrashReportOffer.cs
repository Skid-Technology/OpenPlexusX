using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VibranceHud.Support;

public static class CrashReportOffer
{
	public const string Question = "Send this crash report to PlexusX so it can be fixed?\n\nIt holds the error and where it happened. It has no licence key, no account name and no contact details. Nothing is sent unless you press Yes.";

	internal const string Heading = "Crash report, sent by the user after a crash.\n";

	public static string BuildBody(string logText, string version)
	{
		string text = CrashLog.Redact(logText ?? string.Empty).Trim();
		string text2 = "Crash report, sent by the user after a crash.\n" + text;
		Dictionary<string, string> facts = new Dictionary<string, string>
		{
			["flavour"] = "store",
			["version"] = version ?? string.Empty,
			["windows"] = SafeWindows()
		};
		return BugReportClient.BuildBody(version ?? string.Empty, text2, null, facts);
	}

	public static async Task<bool> OfferAsync(string logPath, string version, Func<bool> userConsents, Func<string, Task<bool>> send)
	{
		try
		{
			if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
			{
				return false;
			}
			if (!userConsents())
			{
				return false;
			}
			string arg = BuildBody(File.ReadAllText(logPath), version);
			return await send(arg).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			return false;
		}
	}

	private static string SafeWindows()
	{
		try
		{
			return Environment.OSVersion.Version.ToString();
		}
		catch
		{
			return "unknown";
		}
	}
}
