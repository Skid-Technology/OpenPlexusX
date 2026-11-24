namespace VibranceHud;

public sealed record DisplayPreset(string Name, string Subtitle, int Saturation, int Vibrance, int Brightness, int Gamma, int Contrast, int Temperature, string Glyph, string Art, int Highlights = 0, int Shadows = 0, bool NightVision = false, ColourVisionMode ColourVision = ColourVisionMode.Off, int Blacks = 0, int Whites = 0, int ShadowTint = 0, int HighlightTint = 0, ChannelMix? Mix = null, int Hue = 0, int RedGain = 100, int GreenGain = 100, int BlueGain = 100, SaturationMetric Formula = SaturationMetric.Rec709, int Darken = 0, int DarkenHue = 120, int HueTurn = 0, int HueTurnAmount = 100, int HueComplement = 0)
{
	public ChannelMix MixOrIdentity => Mix ?? ChannelMix.Identity;

	public ColourDepth Depth()
	{
		return new ColourDepth(Formula, Darken, DarkenHue, HueTurn, HueTurnAmount, HueComplement);
	}

	public bool Matches(int saturation, int vibrance, int brightness, int gamma, int contrast, int temperature, int highlights, int shadows, bool nightVision)
	{
		if (Saturation == saturation && Vibrance == vibrance && Brightness == brightness && Gamma == gamma && Contrast == contrast && Temperature == temperature && Highlights == highlights && Shadows == shadows)
		{
			return NightVision == nightVision;
		}
		return false;
	}

	public bool MatchesWithGrade(int saturation, int vibrance, int brightness, int gamma, int contrast, int temperature, int highlights, int shadows, bool nightVision, int blacks, int whites, int shadowTint, int highlightTint, ChannelMix? mix = null, int hue = 0, int redGain = 100, int greenGain = 100, int blueGain = 100, ColourDepth? depth = null)
	{
		if (Matches(saturation, vibrance, brightness, gamma, contrast, temperature, highlights, shadows, nightVision) && Blacks == blacks && Whites == whites && ShadowTint == shadowTint && HighlightTint == highlightTint && MixOrIdentity.Near(mix ?? ChannelMix.Identity) && Hue == hue && RedGain == redGain && GreenGain == greenGain && BlueGain == blueGain)
		{
			return SameDepth(Depth(), depth ?? ColourDepth.Neutral);
		}
		return false;
	}

	private static bool SameDepth(ColourDepth a, ColourDepth b)
	{
		if (!a.IsNeutral || !b.IsNeutral)
		{
			return a.Clamped() == b.Clamped();
		}
		return true;
	}

	public ToneSettings GradeOf(ToneSettings current)
	{
		return current with
		{
			Highlights = Highlights,
			Shadows = Shadows,
			Blacks = Blacks,
			Whites = Whites,
			ShadowTint = ShadowTint,
			HighlightTint = HighlightTint
		};
	}
}
