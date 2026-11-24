using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VibranceHud;

internal static class Timing
{
	internal static readonly bool Enabled;

	private static readonly Stopwatch Since;

	private static readonly object Gate;

	private static string Path => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "plexusx-timing.log");

	internal static void Mark(string what)
	{
		if (Enabled)
		{
			Write($"{Since.Elapsed.TotalMilliseconds,9:F1} ms   {what}");
		}
	}

	internal static double Measure(string what, Action work)
	{
		if (!Enabled)
		{
			work();
			return 0.0;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			work();
		}
		finally
		{
			stopwatch.Stop();
			Write($"{Since.Elapsed.TotalMilliseconds,9:F1} ms   {what}  took {stopwatch.Elapsed.TotalMilliseconds:F1} ms");
		}
		return stopwatch.Elapsed.TotalMilliseconds;
	}

	private static void Write(string line)
	{
		try
		{
			lock (Gate)
			{
				File.AppendAllText(Path, line + Environment.NewLine, Encoding.UTF8);
			}
		}
		catch
		{
		}
	}

	static Timing()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("PLEXUSX_TIMING");
		bool enabled = ((environmentVariable == "1" || environmentVariable == "selftest") ? true : false);
		Enabled = enabled;
		Since = Stopwatch.StartNew();
		Gate = new object();
	}
}
