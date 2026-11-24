using System.Collections.Generic;

namespace VibranceHud.Display;

public static class EdidLink
{
	public static int MaxVerticalHz(byte[]? edid)
	{
		if (edid == null || edid.Length < 128)
		{
			return 0;
		}
		for (int i = 54; i <= 108; i += 18)
		{
			if (edid[i] == 0 && edid[i + 1] == 0 && edid[i + 3] == 253)
			{
				return edid[i + 6] + (((edid[i + 4] & 2) != 0) ? 255 : 0);
			}
		}
		return 0;
	}

	public static bool IsDisplayPort(byte[]? edid)
	{
		if (edid != null && edid.Length >= 21 && edid[18] == 1 && edid[19] >= 4 && (edid[20] & 0x80) != 0)
		{
			return (edid[20] & 0xF) == 5;
		}
		return false;
	}

	public static string? CeilingHint(int fastestListed, byte[]? current, IReadOnlyList<byte[]> sameModel)
	{
		if (fastestListed <= 0 || current == null)
		{
			return null;
		}
		int num = 0;
		bool flag = false;
		foreach (byte[] item in sameModel)
		{
			int num2 = MaxVerticalHz(item);
			if (num2 > num)
			{
				num = num2;
				flag = IsDisplayPort(item);
			}
		}
		if (num <= fastestListed || num <= MaxVerticalHz(current))
		{
			return null;
		}
		if (flag && !IsDisplayPort(current))
		{
			return $"Your monitor does {num} Hz over DisplayPort. On this cable it stops at {fastestListed} Hz - plug in a DisplayPort cable to get {num} Hz.";
		}
		return $"Your monitor has run {num} Hz on another port. On this one it stops at {fastestListed} Hz.";
	}
}
