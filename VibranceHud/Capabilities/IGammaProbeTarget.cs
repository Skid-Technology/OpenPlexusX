namespace VibranceHud.Capabilities;

public interface IGammaProbeTarget
{
	bool TrySet(ushort[] ramp);

	ushort[]? TryGet();
}
