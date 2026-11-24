using System.Text;
using System.Text.Json.Serialization;

namespace VibranceHud;

public readonly record struct ToneSettings(int Gamma = 100, int Highlights = 0, int Shadows = 0, bool NightVision = false, int Blacks = 0, int Whites = 0, int ShadowTint = 0, int HighlightTint = 0)
{
	[JsonIgnore]
	public int ResolvedGamma
	{
		get
		{
			if (Gamma != 0)
			{
				return Gamma;
			}
			return 100;
		}
	}

	[JsonIgnore]
	public static ToneSettings Neutral => new ToneSettings(100, 0, 0, false, 0, 0, 0, 0);

	[JsonIgnore]
	public bool IsNeutral
	{
		get
		{
			if (ResolvedGamma == 100 && Highlights == 0 && Shadows == 0 && !NightVision && Blacks == 0 && Whites == 0 && ShadowTint == 0)
			{
				return HighlightTint == 0;
			}
			return false;
		}
	}

	[JsonIgnore]
	public bool IsGammaOnly
	{
		get
		{
			if (Highlights == 0 && Shadows == 0 && !NightVision && Blacks == 0 && Whites == 0 && ShadowTint == 0)
			{
				return HighlightTint == 0;
			}
			return false;
		}
	}

	[JsonIgnore]
	public ToneSettings Normalized => this with
	{
		Gamma = ResolvedGamma
	};

	private bool PrintMembers(StringBuilder builder)
	{
		builder.Append("Gamma = ").Append(Gamma).Append(", Highlights = ")
			.Append(Highlights)
			.Append(", Shadows = ")
			.Append(Shadows)
			.Append(", NightVision = ")
			.Append(NightVision)
			.Append(", Blacks = ")
			.Append(Blacks)
			.Append(", Whites = ")
			.Append(Whites)
			.Append(", ShadowTint = ")
			.Append(ShadowTint)
			.Append(", HighlightTint = ")
			.Append(HighlightTint);
		return true;
	}
}
