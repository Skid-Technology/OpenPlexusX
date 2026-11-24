using System;
using NvAPIWrapper;
using NvAPIWrapper.Native;
using NvAPIWrapper.Native.Display.Structures;

namespace VibranceHud;

internal sealed class NvapiDisplayAccess : INvapiDisplayAccess
{
	private DisplayHandle[] _displays = Array.Empty<DisplayHandle>();

	public int Resolve()
	{
		NVIDIA.Initialize();
		_displays = DisplayApi.EnumNvidiaDisplayHandle();
		return _displays.Length;
	}

	public int ReadCurrentLevel(int display)
	{
		return DisplayApi.GetDVCInfoEx(_displays[display]).CurrentLevel;
	}

	public int ReadDefaultLevel(int display)
	{
		return DisplayApi.GetDVCInfoEx(_displays[display]).DefaultLevel;
	}

	public string DisplayId(int display)
	{
		return DisplayApi.GetAssociatedNvidiaDisplayName(_displays[display]);
	}

	public void WriteLevel(int display, int level)
	{
		DisplayApi.SetDVCLevelEx(_displays[display], level);
	}
}
