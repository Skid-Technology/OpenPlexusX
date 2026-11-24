using System;
using System.Collections.Generic;

namespace VibranceHud.Display;

public interface ICustomModeDriver
{
	bool Available { get; }

	bool Trial(string? deviceName, int width, int height, int hz);

	bool Trial(string? deviceName, int width, int height, int hz, DisplayMode? native)
	{
		return Trial(deviceName, width, height, hz);
	}

	bool IsOnTheNativeSignal(string? deviceName, int width, int height, int hz, DisplayMode? native)
	{
		return true;
	}

	bool Save(string? deviceName);

	bool Revert(string? deviceName);

	IReadOnlyList<DisplayMode> Saved(string? deviceName)
	{
		return Array.Empty<DisplayMode>();
	}
}
