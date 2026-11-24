using System;

namespace VibranceHud.Monitors;

public readonly record struct PanelRange(int Min, int Current, int Max)
{
	public bool IsUsable
	{
		get
		{
			if (Max > Min && Current >= Min)
			{
				return Current <= Max;
			}
			return false;
		}
	}

	public int FromPercent(int percent)
	{
		return Min + (int)Math.Round((double)((Max - Min) * Math.Clamp(percent, 0, 100)) / 100.0);
	}

	public int ToPercent(int raw)
	{
		if (Max <= Min)
		{
			return 0;
		}
		return Math.Clamp((int)Math.Round((double)(raw - Min) * 100.0 / (double)(Max - Min)), 0, 100);
	}
}
