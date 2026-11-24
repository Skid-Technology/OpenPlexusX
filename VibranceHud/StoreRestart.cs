using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VibranceHud;

internal static class StoreRestart
{
	private const int RESTART_NO_CRASH = 1;

	private const int RESTART_NO_HANG = 2;

	private const int RESTART_NO_REBOOT = 8;

	internal static void Register()
	{
		try
		{
			Ask();
		}
		catch
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void Ask()
	{
		RegisterApplicationRestart("--tray", 11);
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
	private static extern int RegisterApplicationRestart(string pwzCommandline, int dwFlags);
}
