using System.Collections.Generic;

namespace VibranceHud.Licensing;

public static class ServedCodes
{
	private static readonly ShareCodeFormat DefaultFormat = new ShareCodeFormat(
		Alphabet: "23456789ABCDEFGHJKMNPQRSTUVWXYZ#",
		Prefix: "PX-",
		ChecksumModulus: 32,
		ChecksumWeightStep: 2,
		ChecksumWeightOffset: 1,
		ChecksumIndexOffset: 0,
		BodyLengths: new Dictionary<int, int>
		{
			[13] = 6,
			[17] = 8,
			[19] = 9,
			[27] = 13,
			[39] = 19
		},
		MaxVibrance: 350,
		MaxSaturation: 200,
		MinBrightness: 50,
		MaxBrightness: 150,
		MinGamma: 50,
		MaxGamma: 150,
		MinContrast: 50,
		MaxContrast: 150,
		MinTemperature: -100,
		MaxTemperature: 100,
		MinGrade: -100,
		MaxGrade: 100);

	public static ShareCodeFormat Current => DefaultFormat;

	public static ServedSource Source => ServedSource.Served;

	internal static void Install()
	{
	}
}
