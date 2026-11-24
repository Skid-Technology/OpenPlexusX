using System.Collections.Generic;

namespace VibranceHud.Display;

public interface IDesktopLayout
{
	IReadOnlyList<MonitorPlacement> Read();

	bool Place(IReadOnlyList<MonitorPlacement> placements);
}
