using System;
using System.Linq;

namespace VibranceHud;

public static class LaunchOptions
{
	public const string TraySwitch = "--tray";

	public const string TraceColourSwitch = "--trace-colour";

	public static bool StartInTray { get; private set; }

	public static bool TraceColour { get; private set; }

	public static void Parse(string[]? args)
	{
		StartInTray = Has(args, "--tray");
		TraceColour = Has(args, "--trace-colour");
	}

	public static string[] WithTraySwitch(string[]? args)
	{
		if (args != null && Has(args, "--tray"))
		{
			return args;
		}
		string[] array = new string[(args?.Length ?? 0) + 1];
		args?.CopyTo(array, 0);
		array[^1] = "--tray";
		return array;
	}

	private static bool Has(string[]? args, string option)
	{
		return args?.Any((string a) => string.Equals(a, option, StringComparison.OrdinalIgnoreCase)) ?? false;
	}

	internal static void ResetForTest()
	{
		StartInTray = false;
		TraceColour = false;
	}
}
