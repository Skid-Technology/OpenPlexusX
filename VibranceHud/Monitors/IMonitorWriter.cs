namespace VibranceHud.Monitors;

public interface IMonitorWriter
{
	bool SetBrightnessPercent(int monitorIndex, PanelRange range, int percent);

	bool SetContrastPercent(int monitorIndex, PanelRange range, int percent);

	bool SetLowBlueLight(int monitorIndex, PanelRange gain, int strength);

	bool SetRedGainPercent(int monitorIndex, PanelRange range, int percent);

	bool SetGreenGainPercent(int monitorIndex, PanelRange range, int percent);

	bool SetInputSource(int monitorIndex, byte value);

	bool RestoreBrightness(int monitorIndex, int raw);

	bool RestoreContrast(int monitorIndex, int raw);

	bool RestoreBlueGain(int monitorIndex, int raw);

	bool RestoreRedGain(int monitorIndex, int raw);

	bool RestoreGreenGain(int monitorIndex, int raw);

	(bool Brightness, bool Contrast, bool BlueGain, bool RedGain, bool GreenGain) RestoreAll(int monitorIndex, int? brightness, int? contrast, int? blueGain, int? redGain = null, int? greenGain = null);

	int? ReadTrustedBrightness(int monitorIndex);

	bool RestoreColourDefaults(int monitorIndex);
}
