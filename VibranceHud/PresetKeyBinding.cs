namespace VibranceHud;

public sealed class PresetKeyBinding
{
	public string Name { get; set; } = "";

	public uint ModifierMask { get; set; }

	public uint VirtualKey { get; set; }
}
