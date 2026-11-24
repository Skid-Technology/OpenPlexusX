using System;
using System.Diagnostics;
using System.IO;

namespace VibranceHud.Licensing;

internal static class LoopbackSignIn
{
	private static Func<AppSettings>? _settingsSource;

	internal static void UseTheBrowserFrom(Func<AppSettings> settingsSource)
	{
		_settingsSource = settingsSource;
	}

	internal static void OpenBrowser(string url)
	{
		TryOpenBrowser(url);
	}

	internal static bool TryOpenBrowser(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}
		try
		{
			string? browserPath = _settingsSource?.Invoke()?.SignInBrowserPath;
			if (!string.IsNullOrWhiteSpace(browserPath) && File.Exists(browserPath))
			{
				Process.Start(new ProcessStartInfo(browserPath, url)
				{
					UseShellExecute = false
				});
				return true;
			}
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
			return true;
		}
		catch
		{
			return false;
		}
	}
}
