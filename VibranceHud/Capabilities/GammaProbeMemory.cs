using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Capabilities;

public sealed record GammaProbeMemory(string Key, GammaSupport Ramp, GammaRefusal Why, List<DeviceGamma> Devices)
{
	internal const string Version = "g1";

	internal static string KeyFor(IEnumerable<string> devices, bool elevated, bool hdr, string appVersion)
	{
		return "g1|" + appVersion + "|" + (elevated ? "E" : "e") + (hdr ? "H" : "h") + "|" + string.Join(";", devices.OrderBy((string d) => d, StringComparer.OrdinalIgnoreCase));
	}

	internal bool Reusable(string key)
	{
		bool flag = Key == key;
		if (flag)
		{
			GammaSupport ramp = Ramp;
			bool flag2 = (uint)(ramp - 1) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			return Why != GammaRefusal.ScreenUnreadable;
		}
		return false;
	}
}
