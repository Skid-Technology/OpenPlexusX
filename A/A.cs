using VibranceHud;
using VibranceHud.Hotkeys;
using VibranceHud.Licensing;

namespace A;

internal static class A
{
	internal static bool Available
	{
		get
		{
			try
			{
				return ServedNightVision.Live;
			}
			catch
			{
				return false;
			}
		}
	}

	internal static void ForgetIfUnavailable()
	{
		if (!Available)
		{
			HotkeyStatus.Forget(HotkeySlot.NightVision);
		}
	}
}
