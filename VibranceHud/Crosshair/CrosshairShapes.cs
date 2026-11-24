using System;
using System.Collections.Generic;
using System.Drawing;

namespace VibranceHud.Crosshair;

public sealed record CrosshairShapes(IReadOnlyList<RectangleF> Bars, RectangleF? Circle, RectangleF Bounds)
{
	public IReadOnlyList<AngledBar> Angled { get; init; } = Array.Empty<AngledBar>();

	public int DotIndex { get; init; } = -1;

	public float RotationDegrees { get; init; }

	public PointF Pivot { get; init; }

	public RectangleF DrawnBounds { get; init; }
}
