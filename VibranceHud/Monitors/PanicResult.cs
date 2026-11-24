namespace VibranceHud.Monitors;

public sealed record PanicResult(int PanelsRestored, int PanelsReset, int PanelsRefused, bool GammaLinear, bool DriverBack);
