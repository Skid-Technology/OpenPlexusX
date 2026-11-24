using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VibranceHud.Updates;

public static class UpdateManifestVerifier
{
	public static bool TryVerify(string? envelopeJson, byte[] publicKey, out UpdateManifest? manifest)
	{
		manifest = null;
		if (string.IsNullOrWhiteSpace(envelopeJson))
		{
			return false;
		}
		if (publicKey == null || publicKey.Length == 0)
		{
			return false;
		}
		try
		{
			SignedManifestEnvelope signedManifestEnvelope = JsonSerializer.Deserialize<SignedManifestEnvelope>(envelopeJson);
			if (signedManifestEnvelope == null)
			{
				return false;
			}
			if (string.IsNullOrEmpty(signedManifestEnvelope.Manifest) || string.IsNullOrEmpty(signedManifestEnvelope.Sig))
			{
				return false;
			}
			byte[] bytes = Convert.FromBase64String(signedManifestEnvelope.Manifest);
			byte[] signature = Convert.FromBase64String(signedManifestEnvelope.Sig);
			if (!UpdateManifest.TryParse(Encoding.UTF8.GetString(bytes), out UpdateManifest manifest2) || manifest2 == null)
			{
				return false;
			}
			using ECDsa eCDsa = ECDsa.Create();
			eCDsa.ImportSubjectPublicKeyInfo(publicKey, out var _);
			if (!eCDsa.VerifyData(manifest2.CanonicalBytes(), signature, HashAlgorithmName.SHA256))
			{
				return false;
			}
			if (!UpdateManifest.InstallerUrlIsAllowed(manifest2.InstallerUrl))
			{
				return false;
			}
			manifest = manifest2;
			return true;
		}
		catch
		{
			manifest = null;
			return false;
		}
	}

	public static bool TryReadPublishedAssemblyHash(string? envelopeJson, byte[] publicKey, out string? version, out string? assemblySha256)
	{
		version = null;
		assemblySha256 = null;
		if (string.IsNullOrWhiteSpace(envelopeJson))
		{
			return false;
		}
		if (publicKey == null || publicKey.Length == 0)
		{
			return false;
		}
		try
		{
			SignedManifestEnvelope signedManifestEnvelope = JsonSerializer.Deserialize<SignedManifestEnvelope>(envelopeJson);
			if (signedManifestEnvelope == null)
			{
				return false;
			}
			if (string.IsNullOrEmpty(signedManifestEnvelope.Manifest) || string.IsNullOrEmpty(signedManifestEnvelope.Sig2))
			{
				return false;
			}
			byte[] bytes = Convert.FromBase64String(signedManifestEnvelope.Manifest);
			byte[] signature = Convert.FromBase64String(signedManifestEnvelope.Sig2);
			if (!UpdateManifest.TryParse(Encoding.UTF8.GetString(bytes), out UpdateManifest manifest) || manifest == null)
			{
				return false;
			}
			string assemblySha257 = manifest.AssemblySha256;
			if (!LooksLikeASha256(assemblySha257))
			{
				return false;
			}
			using ECDsa eCDsa = ECDsa.Create();
			eCDsa.ImportSubjectPublicKeyInfo(publicKey, out var _);
			if (!eCDsa.VerifyData(manifest.CanonicalBytesV2(), signature, HashAlgorithmName.SHA256))
			{
				return false;
			}
			version = manifest.Version;
			assemblySha256 = assemblySha257.ToLowerInvariant();
			return true;
		}
		catch
		{
			version = null;
			assemblySha256 = null;
			return false;
		}
	}

	private static bool LooksLikeASha256(string? value)
	{
		if (value == null || value.Length != 64)
		{
			return false;
		}
		foreach (char c in value)
		{
			if ((c < '0' || c > '9') && (c < 'a' || c > 'f') && (c < 'A' || c > 'F'))
			{
				return false;
			}
		}
		return true;
	}
}
