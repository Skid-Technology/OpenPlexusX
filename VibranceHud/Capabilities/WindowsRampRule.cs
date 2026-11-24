using System;
using Microsoft.Win32;

namespace VibranceHud.Capabilities;

public static class WindowsRampRule
{
	public const int DefaultRange = 128;

	public const int UnlockedRange = 256;

	public static bool WindowsAcceptsRamp(ushort[] ramp, int range = 128)
	{
		if (ramp.Length != 768)
		{
			return false;
		}
		if (range >= 256)
		{
			return true;
		}
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				if (Math.Abs((ramp[i * 256 + j] >> 8) - j) > range)
				{
					return false;
				}
			}
		}
		return true;
	}

	public static int? ReadGammaRange()
	{
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ICM");
			return (registryKey?.GetValue("GdiIcmGammaRange") is int value) ? new int?(value) : ((int?)null);
		}
		catch
		{
			return null;
		}
	}

	public static int WorstDeviation(ushort[] ramp)
	{
		int num = 0;
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				num = Math.Max(num, Math.Abs((ramp[i * 256 + j] >> 8) - j));
			}
		}
		return num;
	}
}
