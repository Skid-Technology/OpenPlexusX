using System.Text.Json.Serialization;

namespace VibranceHud;

public sealed class MonitorRule
{
	[JsonPropertyName("gameId")]
	public string GameId { get; set; } = "";

	[JsonPropertyName("width")]
	public int Width { get; set; }

	[JsonPropertyName("height")]
	public int Height { get; set; }

	[JsonPropertyName("device")]
	public string Device { get; set; } = "";

	[JsonIgnore]
	public bool IsSet
	{
		get
		{
			if (Width > 0)
			{
				return Height > 0;
			}
			return false;
		}
	}
}
