namespace VibranceHud.Capabilities;

public sealed record DeviceGamma(string Device, GammaSupport Result, GammaRefusal Why);
