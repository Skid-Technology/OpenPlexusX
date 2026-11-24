using System;
using System.Collections.Generic;

namespace VibranceHud.Monitors;

public sealed record MonitorCapability(string Description, bool SupportsBrightness, bool SupportsContrast, bool SupportsRgbGain, int BrightnessMin, int BrightnessCurrent, int BrightnessMax, string Refusal)
{
	public PanelRange Brightness { get; init; } = new PanelRange(BrightnessMin, BrightnessCurrent, BrightnessMax);

	public PanelRange Contrast { get; init; }

	public PanelRange BlueGain { get; init; }

	public bool SupportsRedGain { get; init; }

	public bool SupportsGreenGain { get; init; }

	public PanelRange RedGain { get; init; }

	public PanelRange GreenGain { get; init; }

	public IReadOnlyList<byte> InputSourceValues { get; init; } = Array.Empty<byte>();

	public bool SupportsInputSource => InputSourceValues.Count > 0;

	public byte? CurrentInputSource { get; init; }

	public int Index { get; init; }
}
