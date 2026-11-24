using System;

namespace VibranceHud.Display;

public interface IClock
{
	DateTime UtcNow { get; }
}
