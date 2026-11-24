using System;
using System.Text.Json.Serialization;

namespace VibranceHud;

public sealed class GameHubOptions
{
	[JsonPropertyName("graphicsQuality")]
	public string GraphicsQuality { get; set; } = "";

	[JsonPropertyName("fpsCap")]
	public int FpsCap { get; set; }

	[JsonPropertyName("effectToggles")]
	public string[] EffectToggles { get; set; } = Array.Empty<string>();

	[JsonPropertyName("tools")]
	public string[] Tools { get; set; } = Array.Empty<string>();
}
