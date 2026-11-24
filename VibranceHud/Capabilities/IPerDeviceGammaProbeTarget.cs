using System.Collections.Generic;

namespace VibranceHud.Capabilities;

public interface IPerDeviceGammaProbeTarget
{
	IReadOnlyList<string> Devices();

	bool TrySet(string device, ushort[] ramp);

	ushort[]? TryGet(string device);
}
