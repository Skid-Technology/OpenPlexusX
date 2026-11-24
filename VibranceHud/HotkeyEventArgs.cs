using System;

namespace VibranceHud;

internal sealed class HotkeyEventArgs : EventArgs
{
	public int HotkeyId { get; }

	public HotkeyEventArgs(int id)
	{
		HotkeyId = id;
	}
}
