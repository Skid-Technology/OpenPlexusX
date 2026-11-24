namespace VibranceHud.Display;

public sealed class NoCustomModeDriver : ICustomModeDriver
{
	public bool Available => false;

	public bool Trial(string? deviceName, int width, int height, int hz)
	{
		return false;
	}

	public bool Save(string? deviceName)
	{
		return false;
	}

	public bool Revert(string? deviceName)
	{
		return false;
	}
}
