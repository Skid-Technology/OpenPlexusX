using System;
using System.Linq;
using System.Text;
using VibranceHud.Licensing;

namespace VibranceHud;

public readonly record struct ProfileCode(int Vibrance, int Saturation, int Brightness, int Gamma, int Contrast = 100, int Temperature = 0, int Highlights = 0, int Shadows = 0, ColourVisionMode ColourVision = ColourVisionMode.Off, int Hue = 0, int RedGain = 100, int GreenGain = 100, int BlueGain = 100, SaturationMetric Formula = SaturationMetric.Rec709, int Darken = 0, int DarkenHue = 120, int HueTurn = 0, int HueTurnAmount = 100, int HueComplement = 0)
{
	internal const int DepthBodyLength = 39;

	internal const int DepthFields = 19;

	internal const int BalanceBodyLength = 27;

	internal const int BalanceFields = 13;

	public ColourDepth Depth()
	{
		return new ColourDepth(Formula, Darken, DarkenHue, HueTurn, HueTurnAmount, HueComplement);
	}

	private bool PrintMembers(StringBuilder builder)
	{
		builder.Append("Vibrance = ").Append(Vibrance.ToString()).Append(", Saturation = ")
			.Append(Saturation.ToString())
			.Append(", Brightness = ")
			.Append(Brightness.ToString())
			.Append(", Gamma = ")
			.Append(Gamma.ToString())
			.Append(", Contrast = ")
			.Append(Contrast.ToString())
			.Append(", Temperature = ")
			.Append(Temperature.ToString())
			.Append(", Highlights = ")
			.Append(Highlights.ToString())
			.Append(", Shadows = ")
			.Append(Shadows.ToString())
			.Append(", ColourVision = ")
			.Append(ColourVision.ToString())
			.Append(", Hue = ")
			.Append(Hue.ToString())
			.Append(", RedGain = ")
			.Append(RedGain.ToString())
			.Append(", GreenGain = ")
			.Append(GreenGain.ToString())
			.Append(", BlueGain = ")
			.Append(BlueGain.ToString());
		if (!Depth().IsNeutral)
		{
			builder.Append(", Formula = ").Append(Formula.ToString()).Append(", Darken = ")
				.Append(Darken.ToString())
				.Append(", DarkenHue = ")
				.Append(DarkenHue.ToString())
				.Append(", HueTurn = ")
				.Append(HueTurn.ToString())
				.Append(", HueTurnAmount = ")
				.Append(HueTurnAmount.ToString())
				.Append(", HueComplement = ")
				.Append(HueComplement.ToString());
		}
		return true;
	}

	public ProfileCode WithDepth(ColourDepth depth)
	{
		return this with
		{
			Formula = depth.Formula,
			Darken = depth.Darken,
			DarkenHue = depth.DarkenHue,
			HueTurn = depth.HueTurn,
			HueTurnAmount = depth.HueTurnAmount,
			HueComplement = depth.HueComplement
		};
	}

	private static int ClampVibrance(int v, ShareCodeFormat f)
	{
		return Math.Clamp(v, 0, f.MaxVibrance);
	}

	private static int ClampSaturation(int v, ShareCodeFormat f)
	{
		return Math.Clamp(v, 0, f.MaxSaturation);
	}

	private static int ClampBrightness(int v, ShareCodeFormat f)
	{
		return Math.Clamp(v, f.MinBrightness, f.MaxBrightness);
	}

	private static int ClampGamma(int v, ShareCodeFormat f)
	{
		return Math.Clamp(v, f.MinGamma, f.MaxGamma);
	}

	private static int ClampContrast(int v, ShareCodeFormat f)
	{
		return Math.Clamp(v, f.MinContrast, f.MaxContrast);
	}

	private static int ClampTemperature(int v, ShareCodeFormat f)
	{
		return Math.Clamp(v, f.MinTemperature, f.MaxTemperature);
	}

	private static int ClampGrade(int v, ShareCodeFormat f)
	{
		return Math.Clamp(v, f.MinGrade, f.MaxGrade);
	}

	public static string Encode(ProfileCode profile)
	{
		return Encode(profile, ServedCodes.Current);
	}

	internal static string Encode(ProfileCode profile, ShareCodeFormat format)
	{
		if (format == null || format.Withheld)
		{
			return "";
		}
		int[] array = new int[8]
		{
			ClampVibrance(profile.Vibrance, format),
			ClampSaturation(profile.Saturation, format),
			ClampBrightness(profile.Brightness, format) - format.MinBrightness,
			ClampGamma(profile.Gamma, format) - format.MinGamma,
			ClampContrast(profile.Contrast, format) - format.MinContrast,
			ClampTemperature(profile.Temperature, format) - format.MinTemperature,
			ClampGrade(profile.Highlights, format) - format.MinGrade,
			ClampGrade(profile.Shadows, format) - format.MinGrade
		};
		ColourDepth colourDepth = profile.Depth().Clamped();
		if (!colourDepth.IsNeutral && format.FieldsFor(39) == 19)
		{
			array = array.Concat(new int[11]
			{
				(int)profile.ColourVision,
				ClampHue(profile.Hue) - -180,
				ClampGain(profile.RedGain) - 50,
				ClampGain(profile.GreenGain) - 50,
				ClampGain(profile.BlueGain) - 50,
				(int)colourDepth.Formula,
				colourDepth.Darken,
				colourDepth.DarkenHue,
				colourDepth.HueTurn - -180,
				colourDepth.HueTurnAmount,
				colourDepth.HueComplement
			}).ToArray();
			return Assemble(array, format);
		}
		if (HasBalance(profile) && format.FieldsFor(27) == 13)
		{
			array = array.Concat(new int[5]
			{
				(int)profile.ColourVision,
				ClampHue(profile.Hue) - -180,
				ClampGain(profile.RedGain) - 50,
				ClampGain(profile.GreenGain) - 50,
				ClampGain(profile.BlueGain) - 50
			}).ToArray();
			return Assemble(array, format);
		}
		if (profile.ColourVision != ColourVisionMode.Off)
		{
			array = array.Append((int)profile.ColourVision).ToArray();
		}
		return Assemble(array, format);
	}

	internal static string EncodeEightValuesForTest(ProfileCode profile)
	{
		ShareCodeFormat current = ServedCodes.Current;
		return Assemble(new int[8]
		{
			ClampVibrance(profile.Vibrance, current),
			ClampSaturation(profile.Saturation, current),
			ClampBrightness(profile.Brightness, current) - current.MinBrightness,
			ClampGamma(profile.Gamma, current) - current.MinGamma,
			ClampContrast(profile.Contrast, current) - current.MinContrast,
			ClampTemperature(profile.Temperature, current) - current.MinTemperature,
			ClampGrade(profile.Highlights, current) - current.MinGrade,
			ClampGrade(profile.Shadows, current) - current.MinGrade
		}, current);
	}

	internal static string EncodeRawForTest(int[] payload)
	{
		return Assemble(payload, ServedCodes.Current);
	}

	internal static string EncodeSixValuesForTest(ProfileCode profile)
	{
		ShareCodeFormat current = ServedCodes.Current;
		return Assemble(new int[6]
		{
			ClampVibrance(profile.Vibrance, current),
			ClampSaturation(profile.Saturation, current),
			ClampBrightness(profile.Brightness, current) - current.MinBrightness,
			ClampGamma(profile.Gamma, current) - current.MinGamma,
			ClampContrast(profile.Contrast, current) - current.MinContrast,
			ClampTemperature(profile.Temperature, current) - current.MinTemperature
		}, current);
	}

	private static string Assemble(int[] payload, ShareCodeFormat format)
	{
		if (format == null || format.Withheld)
		{
			return "";
		}
		int length = format.Alphabet.Length;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (int num in payload)
		{
			if (num < 0 || num >= length * length)
			{
				return "";
			}
			stringBuilder.Append(format.Alphabet[num / length]);
			stringBuilder.Append(format.Alphabet[num % length]);
		}
		int num2 = format.Checksum(stringBuilder.ToString());
		if (num2 < 0 || num2 >= length)
		{
			return "";
		}
		stringBuilder.Append(format.Alphabet[num2]);
		return format.Prefix + stringBuilder;
	}

	public static bool TryDecode(string? code, out ProfileCode profile)
	{
		return TryDecode(code, ServedCodes.Current, out profile);
	}

	internal static bool TryDecode(string? code, ShareCodeFormat format, out ProfileCode profile)
	{
		profile = default;
		if (format == null || format.Withheld)
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(code))
		{
			return false;
		}
		string text = ShareCodeText.Normalize(code).Trim().ToUpperInvariant();
		if (!text.StartsWith(format.Prefix, StringComparison.Ordinal))
		{
			return false;
		}
		string text2 = text.Substring(format.Prefix.Length);
		int num = format.FieldsFor(text2.Length);
		if (num == 0)
		{
			return false;
		}
		int length = format.Alphabet.Length;
		int[] array = new int[text2.Length];
		for (int i = 0; i < text2.Length; i++)
		{
			array[i] = format.Alphabet.IndexOf(text2[i]);
			if (array[i] < 0)
			{
				return false;
			}
		}
		int[] array2 = new int[num];
		for (int j = 0; j < num; j++)
		{
			array2[j] = array[j * 2] * length + array[j * 2 + 1];
		}
		if (array[num * 2] != format.Checksum(text2.Substring(0, num * 2)))
		{
			return false;
		}
		profile = new ProfileCode(ClampVibrance(array2[0], format), ClampSaturation(array2[1], format), ClampBrightness(array2[2] + format.MinBrightness, format), ClampGamma(array2[3] + format.MinGamma, format), (num >= 6) ? ClampContrast(array2[4] + format.MinContrast, format) : 100, (num >= 6) ? ClampTemperature(array2[5] + format.MinTemperature, format) : 0, (num >= 8) ? ClampGrade(array2[6] + format.MinGrade, format) : 0, (num >= 8) ? ClampGrade(array2[7] + format.MinGrade, format) : 0, (num >= 9) ? ReadCorrection(array2[8]) : ColourVisionMode.Off, (num >= 13) ? ClampHue(array2[9] + -180) : 0, (num >= 13) ? ClampGain(array2[10] + 50) : 100, (num >= 13) ? ClampGain(array2[11] + 50) : 100, (num >= 13) ? ClampGain(array2[12] + 50) : 100);
		if (num >= 19)
		{
			profile = profile.WithDepth(new ColourDepth((SaturationMetric)array2[13], array2[14], array2[15], array2[16] + -180, array2[17], array2[18]).Clamped());
		}
		return true;
	}

	private static int ClampHue(int v)
	{
		return Math.Clamp(v, -180, 180);
	}

	private static int ClampGain(int v)
	{
		return Math.Clamp(v, 50, 150);
	}

	private static bool HasBalance(ProfileCode p)
	{
		if (p.Hue == 0 && p.RedGain == 100 && p.GreenGain == 100)
		{
			return p.BlueGain != 100;
		}
		return true;
	}

	private static ColourVisionMode ReadCorrection(int value)
	{
		if (!Enum.IsDefined(typeof(ColourVisionMode), value))
		{
			return ColourVisionMode.Off;
		}
		return (ColourVisionMode)value;
	}
}
