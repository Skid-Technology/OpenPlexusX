namespace VibranceHud.Capabilities;

public readonly record struct HdrDisplay(string Device, bool Supported, bool Enabled, int BitsPerChannel);
