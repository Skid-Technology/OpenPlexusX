using System;
using System.Globalization;
using System.IO;

namespace VibranceHud.Pages;

internal static class ScrollTrace
{
	private static readonly bool Enabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PLEXUSX_SCROLL_TRACE"));

	private static readonly string Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "plexusx-scroll.log");

	private static readonly object Gate = new object();

	public static void Write(Func<string> message)
	{
		if (!Enabled)
		{
			return;
		}
		try
		{
			string text = string.Format(CultureInfo.InvariantCulture, "{0:HH:mm:ss.fff} {1}", DateTime.Now, message());
			lock (Gate)
			{
				File.AppendAllText(Path, text + Environment.NewLine);
			}
		}
		catch
		{
		}
	}
}
