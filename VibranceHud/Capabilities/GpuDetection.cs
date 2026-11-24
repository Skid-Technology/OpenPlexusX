using SharpDX.DXGI;

namespace VibranceHud.Capabilities;

internal static class GpuDetection
{
	public static GpuVendor PrimaryVendor()
	{
		using Factory1 factory = new Factory1();
		using Adapter1 adapter = factory.GetAdapter1(0);
		switch (adapter.Description1.VendorId)
		{
		case 4318:
			return GpuVendor.Nvidia;
		case 4098:
		case 4130:
			return GpuVendor.Amd;
		case 32902:
			return GpuVendor.Intel;
		default:
			return GpuVendor.Other;
		}
	}
}
