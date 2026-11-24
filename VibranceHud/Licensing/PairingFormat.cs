using System;
using System.Globalization;

namespace VibranceHud.Licensing;

public sealed record PairingFormat(string Alphabet, int TokenLength, int TokenBits, string UrlTemplate)
{
	public bool Withheld
	{
		get
		{
			if (Alphabet.Length != 0 && TokenLength > 0 && TokenBits > 0)
			{
				return UrlTemplate.Length == 0;
			}
			return true;
		}
	}

	public static readonly PairingFormat Withholding = new PairingFormat("", 0, 0, "");

	public string Url(string address, int port, string token)
	{
		if (Withheld || string.IsNullOrEmpty(token))
		{
			return "";
		}
		return UrlTemplate.Replace("{address}", address, StringComparison.Ordinal).Replace("{port}", port.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal).Replace("{token}", token, StringComparison.Ordinal);
	}
}
