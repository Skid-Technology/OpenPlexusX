namespace VibranceHud.Monitors;

public sealed class Dxva2MonitorWriter : IMonitorWriter
{
	public bool SetBrightnessPercent(int monitorIndex, PanelRange range, int percent)
	{
		return MonitorControl.SetBrightnessPercent(monitorIndex, range, percent);
	}

	public bool SetContrastPercent(int monitorIndex, PanelRange range, int percent)
	{
		return MonitorControl.SetContrastPercent(monitorIndex, range, percent);
	}

	public bool SetLowBlueLight(int monitorIndex, PanelRange gain, int strength)
	{
		return MonitorControl.SetLowBlueLight(monitorIndex, gain, strength);
	}

	public bool SetRedGainPercent(int monitorIndex, PanelRange range, int percent)
	{
		return MonitorControl.SetRedGainPercent(monitorIndex, range, percent);
	}

	public bool SetGreenGainPercent(int monitorIndex, PanelRange range, int percent)
	{
		return MonitorControl.SetGreenGainPercent(monitorIndex, range, percent);
	}

	public bool SetInputSource(int monitorIndex, byte value)
	{
		return MonitorControl.SetInputSource(monitorIndex, value);
	}

	public bool RestoreBrightness(int monitorIndex, int raw)
	{
		return MonitorControl.RestoreBrightness(monitorIndex, raw);
	}

	public bool RestoreContrast(int monitorIndex, int raw)
	{
		return MonitorControl.RestoreContrast(monitorIndex, raw);
	}

	public bool RestoreBlueGain(int monitorIndex, int raw)
	{
		return MonitorControl.RestoreBlueGain(monitorIndex, raw);
	}

	public bool RestoreRedGain(int monitorIndex, int raw)
	{
		return MonitorControl.RestoreRedGain(monitorIndex, raw);
	}

	public bool RestoreGreenGain(int monitorIndex, int raw)
	{
		return MonitorControl.RestoreGreenGain(monitorIndex, raw);
	}

	public (bool Brightness, bool Contrast, bool BlueGain, bool RedGain, bool GreenGain) RestoreAll(int monitorIndex, int? brightness, int? contrast, int? blueGain, int? redGain = null, int? greenGain = null)
	{
		return MonitorControl.RestoreAll(monitorIndex, brightness, contrast, blueGain, redGain, greenGain);
	}

	public int? ReadTrustedBrightness(int monitorIndex)
	{
		return MonitorControl.ReadTrustedBrightness(monitorIndex);
	}

	public bool RestoreColourDefaults(int monitorIndex)
	{
		return MonitorControl.RestoreColourDefaults(monitorIndex);
	}
}
