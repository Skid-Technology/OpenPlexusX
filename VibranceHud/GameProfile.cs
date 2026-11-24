using System;
using System.Text.Json.Serialization;

namespace VibranceHud;

public sealed class GameProfile
{
	[JsonPropertyName("gameId")]
	public string GameId { get; set; } = "";

	[JsonPropertyName("displayName")]
	public string DisplayName { get; set; } = "";

	[JsonPropertyName("vibrance")]
	public int Vibrance { get; set; } = 100;

	[JsonPropertyName("saturation")]
	public int Saturation { get; set; } = 100;

	[JsonPropertyName("brightness")]
	public int Brightness { get; set; } = 100;

	[JsonPropertyName("gamma")]
	public int Gamma { get; set; } = 100;

	[JsonPropertyName("contrast")]
	public int? Contrast { get; set; }

	[JsonPropertyName("temperature")]
	public int? Temperature { get; set; }

	[JsonPropertyName("tone")]
	public ToneSettings? Tone { get; set; }

	[JsonIgnore]
	public int ResolvedContrast => Contrast ?? 100;

	[JsonIgnore]
	public int ResolvedTemperature => Temperature.GetValueOrDefault();

	[JsonIgnore]
	public ToneSettings ResolvedTone
	{
		get
		{
			ToneSettings toneSettings = Tone ?? ToneSettings.Neutral;
			return toneSettings with
			{
				Gamma = Gamma
			};
		}
	}

	[JsonPropertyName("gameHub")]
	public GameHubOptions GameHub { get; set; } = new GameHubOptions();

	[JsonPropertyName("lastUpdated")]
	public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
