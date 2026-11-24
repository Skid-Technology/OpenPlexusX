using System;
using System.Windows.Forms;

namespace VibranceHud.Controls;

internal static class TheClipboard
{
	internal static Action<string>? InsteadOfTheRealOneForTest { get; set; }

	internal static bool Put(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		Action<string> insteadOfTheRealOneForTest = InsteadOfTheRealOneForTest;
		if (insteadOfTheRealOneForTest != null)
		{
			insteadOfTheRealOneForTest(text);
			return true;
		}
		try
		{
			Clipboard.SetText(text);
			return true;
		}
		catch (Exception ex)
		{
			CrashLog.Write(ex);
			return false;
		}
	}
}
