using System.Runtime.InteropServices;

namespace VibranceHud.Updates;

public static class SafeToRestart
{
	private const int QUNS_NOT_PRESENT = 1;

	private const int QUNS_BUSY = 2;

	private const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;

	private const int QUNS_PRESENTATION_MODE = 4;

	private const int QUNS_ACCEPTS_NOTIFICATIONS = 5;

	private const int QUNS_QUIET_TIME = 6;

	private const int QUNS_APP = 7;

	[DllImport("shell32.dll")]
	private static extern int SHQueryUserNotificationState(out int state);

	internal static bool ForShellState(int state)
	{
		return state switch
		{
			1 => true, 
			5 => true, 
			6 => true, 
			_ => false, 
		};
	}

	public static bool IsNow()
	{
		try
		{
			int state;
			return SHQueryUserNotificationState(out state) == 0 && ForShellState(state);
		}
		catch
		{
			return false;
		}
	}
}
