using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using VibranceHud.Support;

namespace VibranceHud;

internal static class ColourTrace
{
	internal static readonly bool Enabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PLEXUSX_SAT_TRACE"));

	private static readonly string Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "plexusx-sat.log");

	private static readonly object Gate = new object();

	private const int Frames = 12;

	public static void Value(string name, int was, int now)
	{
		if (Enabled && was != now)
		{
			Write(string.Format(CultureInfo.InvariantCulture, "{0} {1}->{2}  {3}", name, was, now, Caller()));
		}
	}

	public static void Screen(bool cleared, int saturation)
	{
		if (Enabled)
		{
			Write(string.Format(CultureInfo.InvariantCulture, "SCREEN {0}  (saturation still reads {1})  {2}", cleared ? "CLEARED - picture now neutral" : "applied", saturation, Caller()));
		}
	}

	public static void Meter(ReassertMeter meter)
	{
		if (Enabled && meter != null)
		{
			Write("REASSERT" + Environment.NewLine + meter.Describe());
		}
	}

	public static void Refused(string layer, string why)
	{
		if (Enabled)
		{
			Write(string.Format(CultureInfo.InvariantCulture, "REFUSED {0}: {1}  {2}", layer, why, Caller()));
		}
	}

	public static void Fallback(string what)
	{
		if (Enabled)
		{
			Write(string.Format(CultureInfo.InvariantCulture, "FALLBACK {0}  {1}", what, Caller()));
		}
	}

	private static string Caller()
	{
		try
		{
			StackTrace stackTrace = new StackTrace(2, fNeedFileInfo: false);
			StringBuilder stringBuilder = new StringBuilder();
			int num = Math.Min(12, stackTrace.FrameCount);
			for (int i = 0; i < num; i++)
			{
				MethodBase methodBase = stackTrace.GetFrame(i)?.GetMethod();
				if (!(methodBase == null))
				{
					stringBuilder.Append(methodBase.DeclaringType?.Name).Append('.').Append(methodBase.Name)
						.Append(" <- ");
				}
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return "(no stack)";
		}
	}

	private static void Write(string message)
	{
		try
		{
			string text = string.Format(CultureInfo.InvariantCulture, "{0:HH:mm:ss.fff} {1}", DateTime.Now, message);
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
