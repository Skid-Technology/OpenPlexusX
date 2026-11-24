using System;
using System.Diagnostics;

namespace VibranceHud;

internal static class Links
{
	internal static bool TryOpen(string? url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
			return true;
		}
		catch (Exception ex)
		{
			CrashLog.Write(ex);
			return false;
		}
	}

	internal static void Open(string url)
	{
		TryOpen(url);
	}
}
