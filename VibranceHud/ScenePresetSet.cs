using System.Collections.Generic;

namespace VibranceHud;

public sealed record ScenePresetSet(string Game, IReadOnlyList<DisplayPreset> Presets);
