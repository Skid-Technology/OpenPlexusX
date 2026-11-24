using System;
using System.Globalization;
using System.IO;

namespace VibranceHud.Support;

public static class ReassertTrace
{
	public sealed record Reading(ReassertMeter? Meter, string OverlayMode, bool MagInitialized, bool ExclusiveFullscreenSeen, DateTime Now, string Version, string Flavour, string Directory, TimeSpan? SessionLength = null);

	public const int IntervalMs = 60000;

	public const string FileName = "colour-trace.txt";

	public static string Text(Reading r)
	{
		if (r == null)
		{
			return "(no reading)" + Environment.NewLine;
		}
		string newLine = Environment.NewLine;
		return string.Format(CultureInfo.InvariantCulture, "PlexusX {0} ({1})  {2:yyyy-MM-dd HH:mm:ss}", r.Version ?? "", r.Flavour ?? "", r.Now) + newLine + "overlayMode: " + r.OverlayMode + newLine + "magInitialized: " + YesNo(r.MagInitialized) + newLine + "exclusiveFullscreenSeen: " + YesNo(r.ExclusiveFullscreenSeen) + newLine + Rates(r) + ((r.Meter == null) ? "(no meter)" : r.Meter.Describe()) + newLine;
	}

	private static string Rates(Reading r)
	{
		TimeSpan? sessionLength = r.SessionLength;
		if (sessionLength.HasValue)
		{
			TimeSpan valueOrDefault = sessionLength.GetValueOrDefault();
			if (!(valueOrDefault.TotalMinutes < 1.0))
			{
				string newLine = Environment.NewLine;
				double totalMinutes = valueOrDefault.TotalMinutes;
				string text = "sessionMinutes: " + totalMinutes.ToString("0.0", CultureInfo.InvariantCulture) + newLine;
				if (r.Meter == null)
				{
					return text;
				}
				text = text + "reassertsPerMinute: " + ((double)r.Meter.Reasserts / totalMinutes).ToString("0.0", CultureInfo.InvariantCulture) + newLine;
				string[] array = new string[3] { "driver", "gamma", "overlay" };
				foreach (string text2 in array)
				{
					int count = r.Meter.For(text2).Count;
					if (count != 0)
					{
						text = text + text2 + "WritesPerMinute: " + ((double)count / totalMinutes).ToString("0.0", CultureInfo.InvariantCulture) + newLine;
					}
				}
				return text;
			}
		}
		return "";
	}

	private static string YesNo(bool value)
	{
		if (!value)
		{
			return "no";
		}
		return "yes";
	}

	public static void Write(Reading r)
	{
		try
		{
			Directory.CreateDirectory(r.Directory);
			File.WriteAllText(Path.Combine(r.Directory, "colour-trace.txt"), Text(r));
		}
		catch
		{
		}
	}

	public static void WriteNow(Func<Reading> read)
	{
		try
		{
			if (read != null)
			{
				Write(read());
			}
		}
		catch
		{
		}
	}
}
