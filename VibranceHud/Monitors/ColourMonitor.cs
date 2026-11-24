using System.Drawing;

namespace VibranceHud.Monitors;

public sealed record ColourMonitor(string Identity, string DeviceName, string Name, Rectangle Bounds, bool Primary = false, int RefreshHz = 0);
