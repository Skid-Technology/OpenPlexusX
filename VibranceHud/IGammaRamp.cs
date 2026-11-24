namespace VibranceHud;

public interface IGammaRamp
{
	int DeviceWrites => 0;

	bool LastApplyFailed => false;

	GammaDelivery LastDelivery => GammaDelivery.Full;

	void Apply(ushort[] ramp);

	void Reset();

	string? ApplyAllOrNothing(ushort[] ramp)
	{
		Apply(ramp);
		if (!LastApplyFailed)
		{
			return null;
		}
		return "Windows refused the Dark Boost ramp";
	}
}
