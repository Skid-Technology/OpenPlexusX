using System;

namespace VibranceHud.Hotkeys;

public interface IRawInputSource : IDisposable
{
	void Apply(bool keyboard, bool mouse);
}
