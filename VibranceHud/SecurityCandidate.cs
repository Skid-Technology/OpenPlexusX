using System;
using System.IO;

namespace VibranceHud;

internal static class SecurityCandidate
{
	internal const string ProcessName = "PlexusX";

	internal const string ChannelPrefix = "PlexusX";

	internal static bool IsEnabled => false;

	internal static string? StorageRoot => null;

	internal static string? DownloadRoot
	{
		get
		{
			if (StorageRoot != null)
			{
				return Path.Combine(Path.GetTempPath(), "PlexusX");
			}
			return null;
		}
	}

	internal static byte[] ReadPublicKey(string name)
	{
		using Stream stream = typeof(SecurityCandidate).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Security candidate public key is missing.");
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	internal static string ValidateOrigin(string input)
	{
		if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out Uri result) || (result.Scheme != Uri.UriSchemeHttps && (!(result.Scheme == Uri.UriSchemeHttp) || !result.IsLoopback)) || result.UserInfo.Length != 0 || result.Query.Length != 0 || result.Fragment.Length != 0 || result.AbsolutePath != "/" || result.Host.Equals("plexusx.lol", StringComparison.OrdinalIgnoreCase) || result.Host.Equals("www.plexusx.lol", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Security candidate requires an isolated HTTPS origin or loopback rehearsal.");
		}
		return result.GetLeftPart(UriPartial.Authority);
	}

	internal static string ReadOrigin()
	{
		using Stream stream = typeof(SecurityCandidate).Assembly.GetManifestResourceStream("security-origin.txt");
		if (stream == null)
		{
			return "http://127.0.0.1:9761";
		}
		using StreamReader streamReader = new StreamReader(stream);
		return ValidateOrigin(streamReader.ReadToEnd());
	}
}
