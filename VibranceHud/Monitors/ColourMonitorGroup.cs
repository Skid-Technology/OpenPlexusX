using System.Drawing;

namespace VibranceHud.Monitors;

public sealed record ColourMonitorGroup(string DeviceName, string[] Identities, string Name, Rectangle Bounds, bool Primary, int RefreshHz)
{
	public string Identity => string.Join("|", Identities);

	public bool IsClone => Identities.Length > 1;
}
