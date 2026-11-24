using System;

namespace VibranceHud;

public sealed class NoNvidiaDisplaysException : Exception
{
	public NoNvidiaDisplaysException()
		: base("The NVIDIA driver is present but drives no reachable display.")
	{
	}
}
