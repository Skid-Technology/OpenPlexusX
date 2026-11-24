using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VibranceHud.Updates;

public sealed record UpdateManifest([property: JsonPropertyName("version")] string Version, [property: JsonPropertyName("installerUrl")] string InstallerUrl, [property: JsonPropertyName("sha256")] string Sha256, [property: JsonPropertyName("notes")] string? Notes = null, [property: JsonPropertyName("assemblySha256")] string? AssemblySha256 = null)
{
	public const string Domain = "plexusx-update-v1|";

	public const string DomainV2 = "plexusx-update-v2|";

	public byte[] CanonicalBytes()
	{
		return Encoding.UTF8.GetBytes($"{"plexusx-update-v1|"}{Version}\n{InstallerUrl}\n{Sha256.ToLowerInvariant()}\n{Notes ?? ""}");
	}

	public byte[] CanonicalBytesV2()
	{
		return Encoding.UTF8.GetBytes($"{"plexusx-update-v2|"}{Version}\n{InstallerUrl}\n{Sha256.ToLowerInvariant()}\n{Notes ?? ""}\n{(AssemblySha256 ?? "").ToLowerInvariant()}");
	}

	public static bool InstallerUrlIsAllowed(string? url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}
		if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

	public static bool TryParse(string? json, out UpdateManifest? manifest)
	{
		manifest = null;
		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}
		try
		{
			manifest = JsonSerializer.Deserialize<UpdateManifest>(json);
			return manifest != null && !string.IsNullOrWhiteSpace(manifest.Version) && !string.IsNullOrWhiteSpace(manifest.InstallerUrl) && !string.IsNullOrWhiteSpace(manifest.Sha256);
		}
		catch
		{
			manifest = null;
			return false;
		}
	}
}
