using System;
using System.Collections.Generic;

namespace VibranceHud.Display;

public static class CustomModeLimits
{
	internal const int MinWidth = 640;

	internal const int MinHeight = 480;

	private const int MaxWidth = 15360;

	private const int MaxHeight = 8640;

	private const int MaxHz = 1000;

	public static int NativeRateFor(int typedHz, IReadOnlyList<DisplayMode> reported, int running)
	{
		PanelSize panelSize = CustomModePanel.Native(reported);
		IReadOnlyList<int> readOnlyList2;
		if (!panelSize.Known)
		{
			IReadOnlyList<int> readOnlyList = Array.Empty<int>();
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = DisplayModes.RefreshRatesFor(reported, panelSize.Width, panelSize.Height);
		}
		IReadOnlyList<int> rates = readOnlyList2;
		return SelectNativeRate(typedHz, rates, running);
	}

	internal static int SelectNativeRate(int typedHz, IReadOnlyList<int> rates, int running)
	{
		if (rates.Count == 0)
		{
			if (typedHz <= 0)
			{
				return running;
			}
			return typedHz;
		}
		int num = rates[0];
		foreach (int rate in rates)
		{
			bool num2;
			if (typedHz > 0)
			{
				if (Math.Abs(rate - typedHz) < Math.Abs(num - typedHz))
				{
					goto IL_0062;
				}
				if (Math.Abs(rate - typedHz) != Math.Abs(num - typedHz))
				{
					continue;
				}
				num2 = rate > num;
			}
			else
			{
				num2 = rate > num;
			}
			if (!num2)
			{
				continue;
			}
			goto IL_0062;
			IL_0062:
			num = rate;
		}
		return num;
	}

	public static CustomModeVerdict Judge(int width, int height, int hz, IReadOnlyList<DisplayMode> reported)
	{
		if (width < 640 || height < 480)
		{
			return CustomModeVerdict.TooSmall;
		}
		if (hz > 1000)
		{
			return CustomModeVerdict.RefreshOutOfRange;
		}
		PanelSize panelSize = CustomModePanel.Native(reported);
		if (!panelSize.Known)
		{
			if (width <= 15360 && height <= 8640)
			{
				return CustomModeVerdict.Fine;
			}
			return CustomModeVerdict.BiggerThanThePanel;
		}
		int num = 0;
		foreach (DisplayMode item in reported)
		{
			if (item.RefreshHz > num)
			{
				num = item.RefreshHz;
			}
		}
		if (hz > 0 && num > 0 && hz > num)
		{
			return CustomModeVerdict.RefreshOutOfRange;
		}
		if (width <= panelSize.Width && height <= panelSize.Height)
		{
			return CustomModeVerdict.Fine;
		}
		return CustomModeVerdict.BiggerThanThePanel;
	}

	public static bool NeedsStretching(int width, int height, IReadOnlyList<DisplayMode> reported)
	{
		if (width <= 0 || height <= 0)
		{
			return false;
		}
		PanelSize panelSize = CustomModePanel.Native(reported);
		if (!panelSize.Known)
		{
			return false;
		}
		double num = (double)panelSize.Width / (double)panelSize.Height;
		double num2 = (double)width / (double)height;
		return Math.Abs(num - num2) > 0.01;
	}
}
