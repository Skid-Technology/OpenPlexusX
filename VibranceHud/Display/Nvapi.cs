using NvAPIWrapper;

namespace VibranceHud.Display;

internal static class Nvapi
{
	private static bool? _ready;

	internal static bool Ready()
	{
		bool? ready = _ready;
		if (ready.HasValue)
		{
			return ready == true;
		}
		try
		{
			NVIDIA.Initialize();
			_ready = true;
		}
		catch
		{
			_ready = false;
		}
		return _ready.Value;
	}
}
