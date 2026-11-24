using System;
using System.Text;

namespace VibranceHud.Crosshair;

public static class CrosshairCode
{
	private const string Prefix = "PXC-";

	private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ#";

	private const int ShortValueCount = 10;

	private const int LongValueCount = 13;

	private const int LongestValueCount = 18;

	private const int ShortBodyLength = 21;

	private const int LongBodyLength = 27;

	private const int LongestBodyLength = 37;

	private const int ArmTop = 1;

	private const int ArmBottom = 2;

	private const int ArmLeft = 4;

	private const int ArmRight = 8;

	private const int Circle = 16;

	private const int Dot = 32;

	private const int Outline = 64;

	private const int DiagonalArms = 128;

	private const int ChevronArms = 256;

	private const int OffsetBias = 200;

	public static string Encode(CrosshairConfig c)
	{
		int resolvedRotationDegrees = c.ResolvedRotationDegrees;
		int num = 0;
		if (c.ResolvedArmTop)
		{
			num |= 1;
		}
		if (c.ResolvedArmBottom)
		{
			num |= 2;
		}
		if (c.ResolvedArmLeft)
		{
			num |= 4;
		}
		if (c.ResolvedArmRight)
		{
			num |= 8;
		}
		if (c.ResolvedShowCircle)
		{
			num |= 0x10;
		}
		if (c.ResolvedCentreDot)
		{
			num |= 0x20;
		}
		if (c.Outline)
		{
			num |= 0x40;
		}
		if (resolvedRotationDegrees == 45)
		{
			num |= 0x80;
		}
		if (c.Chevron)
		{
			num |= 0x100;
		}
		int colourArgb = c.ColourArgb;
		int num2 = c.OutlineThicknessTenths ?? 10;
		int resolvedOffsetX = c.ResolvedOffsetX;
		int resolvedOffsetY = c.ResolvedOffsetY;
		int resolvedDotOpacity = c.ResolvedDotOpacity;
		int num3 = c.ResolvedOutlineColourArgb & 0xFFFFFF;
		bool flag = c.Chevron || (resolvedRotationDegrees != 0 && resolvedRotationDegrees != 45) || resolvedDotOpacity != 100 || num3 != 0;
		bool flag2 = flag || resolvedRotationDegrees == 45 || num2 != 10 || resolvedOffsetX != 0 || resolvedOffsetY != 0;
		int[] array = new int[18]
		{
			num,
			Tenths(c.ResolvedSize),
			Tenths(c.ResolvedThickness),
			Tenths(c.ResolvedGap),
			Tenths(c.ResolvedDotSize),
			Tenths(c.ResolvedCircleRadius),
			Math.Clamp(c.Opacity, 0, 100),
			(colourArgb >> 16) & 0xFF,
			(colourArgb >> 8) & 0xFF,
			colourArgb & 0xFF,
			Math.Clamp(num2, 0, 40),
			resolvedOffsetX + 200,
			resolvedOffsetY + 200,
			resolvedRotationDegrees,
			resolvedDotOpacity,
			(num3 >> 16) & 0xFF,
			(num3 >> 8) & 0xFF,
			num3 & 0xFF
		};
		int num4;
		if (flag)
		{
			num4 = 18;
		}
		else
		{
			num4 = (flag2 ? 13 : 10);
		}
		StringBuilder stringBuilder = new StringBuilder(num4 * 2 + 1);
		int[] subArray = array[..num4];
		for (int i = 0; i < subArray.Length; i++)
		{
			int num5 = Math.Clamp(subArray[i], 0, 1023);
			stringBuilder.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[num5 / 32]);
			stringBuilder.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[num5 % 32]);
		}
		stringBuilder.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[Checksum(stringBuilder.ToString())]);
		return "PXC-" + stringBuilder;
	}

	public static bool TryDecode(string? code, out CrosshairConfig crosshair)
	{
		crosshair = new CrosshairConfig();
		if (string.IsNullOrWhiteSpace(code))
		{
			return false;
		}
		string text = ShareCodeText.Normalize(code).Trim().ToUpperInvariant();
		if (!text.StartsWith("PXC-", StringComparison.Ordinal))
		{
			return false;
		}
		string text2 = text.Substring("PXC-".Length);
		int num = text2.Length switch
		{
			21 => 10, 
			27 => 13, 
			37 => 18, 
			_ => 0, 
		};
		if (num == 0)
		{
			return false;
		}
		int[] array = new int[text2.Length];
		for (int i = 0; i < text2.Length; i++)
		{
			array[i] = "23456789ABCDEFGHJKMNPQRSTUVWXYZ#".IndexOf(text2[i]);
			if (array[i] < 0)
			{
				return false;
			}
		}
		if (array[num * 2] != Checksum(text2.Substring(0, num * 2)))
		{
			return false;
		}
		int[] array2 = new int[num];
		for (int j = 0; j < num; j++)
		{
			array2[j] = array[j * 2] * 32 + array[j * 2 + 1];
		}
		int num2 = array2[0];
		crosshair = new CrosshairConfig
		{
			ArmTop = ((num2 & 1) != 0),
			ArmBottom = ((num2 & 2) != 0),
			ArmLeft = ((num2 & 4) != 0),
			ArmRight = ((num2 & 8) != 0),
			ShowCircle = ((num2 & 0x10) != 0),
			CentreDot = ((num2 & 0x20) != 0),
			Outline = ((num2 & 0x40) != 0),
			Diagonal = ((num2 & 0x80) != 0),
			Opacity = Math.Clamp(array2[6], 10, 100),
			ColourArgb = (-16777216 | ((array2[7] & 0xFF) << 16) | ((array2[8] & 0xFF) << 8) | (array2[9] & 0xFF))
		};
		crosshair.SetSizeTenths(Math.Clamp(array2[1], 5, 300));
		crosshair.SetThicknessTenths(Math.Clamp(array2[2], 5, 100));
		crosshair.SetGapTenths(Math.Clamp(array2[3], 0, 300));
		crosshair.DotSizeTenths = Math.Clamp(array2[4], 5, 100);
		crosshair.CircleRadiusTenths = Math.Clamp(array2[5], 10, 400);
		if (num >= 13)
		{
			crosshair.OutlineThicknessTenths = Math.Clamp(array2[10], 0, 40);
			crosshair.OffsetXPx = Math.Clamp(array2[11] - 200, -200, 200);
			crosshair.OffsetYPx = Math.Clamp(array2[12] - 200, -200, 200);
		}
		if (num == 18)
		{
			crosshair.Chevron = (num2 & 0x100) != 0;
			crosshair.SetRotationDegrees(Math.Clamp(array2[13], 0, 90));
			crosshair.DotOpacity = Math.Clamp(array2[14], 10, 100);
			crosshair.OutlineColourRgb = ((array2[15] & 0xFF) << 16) | ((array2[16] & 0xFF) << 8) | (array2[17] & 0xFF);
		}
		return true;
	}

	private static int Tenths(float value)
	{
		return Math.Clamp((int)Math.Round(value * 10f), 0, 1023);
	}

	private static int Checksum(string body)
	{
		int num = 0;
		for (int i = 0; i < body.Length; i++)
		{
			num += ("23456789ABCDEFGHJKMNPQRSTUVWXYZ#".IndexOf(body[i]) + 1) * (2 * i + 1);
		}
		return num % 32;
	}
}
