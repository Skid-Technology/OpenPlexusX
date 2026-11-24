namespace VibranceHud.Hotkeys;

public static class RawInputPolicy
{
	public static (bool Keyboard, bool Mouse) Wanted(bool offered, bool bound, int vk, uint modifiers, bool gameOnly, bool gameRunning)
	{
		if (!offered || !bound || (gameOnly && !gameRunning))
		{
			return (Keyboard: false, Mouse: false);
		}
		bool flag = IsMouseButton(vk);
		return (Keyboard: !flag || modifiers != 0, Mouse: flag);
	}

	public static bool IsMouseButton(int vk)
	{
		if ((uint)(vk - 4) <= 2u)
		{
			return true;
		}
		return false;
	}
}
