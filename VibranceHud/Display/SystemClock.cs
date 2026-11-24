using System;

namespace VibranceHud.Display;

public sealed class SystemClock : IClock
{
	public DateTime UtcNow => DateTime.UtcNow;
}
