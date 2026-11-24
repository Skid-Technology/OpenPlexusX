using System.Text.Json.Serialization;

namespace VibranceHud.Updates;

public sealed class SignedManifestEnvelope
{
	[JsonPropertyName("manifest")]
	public string Manifest { get; set; } = "";

	[JsonPropertyName("sig")]
	public string Sig { get; set; } = "";

	[JsonPropertyName("sig2")]
	public string? Sig2 { get; set; }
}
