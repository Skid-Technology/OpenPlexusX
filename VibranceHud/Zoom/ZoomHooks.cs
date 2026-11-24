using System;

namespace VibranceHud.Zoom;

public sealed record ZoomHooks(Func<uint, uint, bool> BindKey, Func<string> WhyRefused, Action Changed);
