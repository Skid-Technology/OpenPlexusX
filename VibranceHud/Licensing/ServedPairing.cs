namespace VibranceHud.Licensing;

public static class ServedPairing
{
	private static readonly PairingFormat DefaultFormat = new PairingFormat(
		Alphabet: "23456789ABCDEFGHJKLMNPQRSTUVWXYZ",
		TokenLength: 26,
		TokenBits: 128,
		UrlTemplate: "http://{address}:{port}/?k={token}");

	public static PairingFormat Current => DefaultFormat;

	public static ServedSource Source => ServedSource.Served;

	internal static void Install()
	{
	}
}
