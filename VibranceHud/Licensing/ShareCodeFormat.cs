using System.Collections.Generic;

namespace VibranceHud.Licensing;

public sealed record ShareCodeFormat(string Alphabet, string Prefix, int ChecksumModulus, int ChecksumWeightStep, int ChecksumWeightOffset, int ChecksumIndexOffset, IReadOnlyDictionary<int, int> BodyLengths, int MaxVibrance, int MaxSaturation, int MinBrightness, int MaxBrightness, int MinGamma, int MaxGamma, int MinContrast, int MaxContrast, int MinTemperature, int MaxTemperature, int MinGrade, int MaxGrade)
{
	public bool Withheld
	{
		get
		{
			if (Alphabet.Length != 0 && Prefix.Length != 0)
			{
				return BodyLengths.Count == 0;
			}
			return true;
		}
	}

	public static readonly ShareCodeFormat Withholding = new ShareCodeFormat("", "", 32, 2, 1, 0, new Dictionary<int, int>(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

	public int FieldsFor(int bodyLength)
	{
		if (!BodyLengths.TryGetValue(bodyLength, out var value))
		{
			return 0;
		}
		return value;
	}

	public int Checksum(string body)
	{
		int num = 0;
		for (int i = 0; i < body.Length; i++)
		{
			int num2 = Alphabet.IndexOf(body[i]);
			if (num2 < 0)
			{
				return -1;
			}
			num += (num2 + ChecksumIndexOffset) * (ChecksumWeightStep * i + ChecksumWeightOffset);
		}
		if (ChecksumModulus > 0)
		{
			return num % ChecksumModulus;
		}
		return -1;
	}
}
