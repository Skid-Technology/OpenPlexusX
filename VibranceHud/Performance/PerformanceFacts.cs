using System;

namespace VibranceHud.Performance;

public readonly struct PerformanceFacts
{
	public int DriverWritesPerMinute { get; init; }

	public int GammaWritesPerMinute { get; init; }

	public int OverlayWritesPerMinute { get; init; }

	public double? LastReassertMs { get; init; }

	public OverlayMode Overlay { get; init; }

	public bool HoldingCompositionKeeper { get; init; }

	public static PerformanceFacts From(ColourWriteMeter meter, TimeSpan sessionLength, OverlayMode overlay, bool holdingCompositionKeeper, double? lastReassertMs)
	{
		double minutes = sessionLength.TotalMinutes;
		return new PerformanceFacts
		{
			DriverWritesPerMinute = PerMinute(meter.Driver),
			GammaWritesPerMinute = PerMinute(meter.Gamma),
			OverlayWritesPerMinute = PerMinute(meter.Overlay),
			LastReassertMs = lastReassertMs,
			Overlay = overlay,
			HoldingCompositionKeeper = holdingCompositionKeeper
		};
		int PerMinute(int count)
		{
			if (!(minutes < 1.0))
			{
				return (int)((double)count / minutes);
			}
			return 0;
		}
	}
}
