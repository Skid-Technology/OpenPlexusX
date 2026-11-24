using System;

namespace VibranceHud;

public readonly record struct ColourDepth(SaturationMetric Formula = SaturationMetric.Rec709, int Darken = 0, int DarkenHue = 120, int HueTurn = 0, int HueTurnAmount = 100, int HueComplement = 0)
{
	public static ColourDepth Neutral => new ColourDepth(SaturationMetric.Rec709, 0, 120, 0, 100, 0);

	public bool IsNeutral
	{
		get
		{
			if (Formula == SaturationMetric.Rec709 && Darken == 0 && HueComplement == 0)
			{
				if (HueTurn != 0)
				{
					return HueTurnAmount == 0;
				}
				return true;
			}
			return false;
		}
	}

	public const int MinDarken = 0;

	public const int MaxDarken = 400;

	public const int MinDarkenHue = 0;

	public const int MaxDarkenHue = 359;

	public const int MinHueTurn = -180;

	public const int MaxHueTurn = 180;

	public const int MinHueTurnAmount = 0;

	public const int MaxHueTurnAmount = 100;

	public const int MinHueComplement = 0;

	public const int MaxHueComplement = 100;

	public ColourDepth Clamped()
	{
		return new ColourDepth(Enum.IsDefined(typeof(SaturationMetric), Formula) ? Formula : SaturationMetric.Rec709, Math.Clamp(Darken, 0, 400), Math.Clamp(DarkenHue, 0, 359), Math.Clamp(HueTurn, -180, 180), Math.Clamp(HueTurnAmount, 0, 100), Math.Clamp(HueComplement, 0, 100));
	}
}
