using System.Linq;

namespace VibranceHud;

public sealed class GameLookSnapshot
{
	public int? SaturationPercent { get; set; }

	public int? VibrancePercent { get; set; }

	public bool VibranceChosenByUser { get; set; }

	public int BrightnessPercent { get; set; } = 100;

	public int GammaPercent { get; set; } = 100;

	public int ContrastPercent { get; set; } = 100;

	public int? Temperature { get; set; }

	public ToneSettings? Tone { get; set; }

	public float[]? Mix { get; set; }

	public int Hue { get; set; }

	public int RedGain { get; set; } = 100;

	public int GreenGain { get; set; } = 100;

	public int BlueGain { get; set; } = 100;

	public ColourVisionMode ColourVision { get; set; }

	public bool ColourVisionFromPreset { get; set; }

	public string ColourPresetName { get; set; } = "";

	public static GameLookSnapshot Take(AppSettings s)
	{
		return new GameLookSnapshot
		{
			SaturationPercent = s.SaturationPercent,
			VibrancePercent = s.VibrancePercent,
			VibranceChosenByUser = s.VibranceChosenByUser,
			BrightnessPercent = s.BrightnessPercent,
			GammaPercent = s.GammaPercent,
			ContrastPercent = s.ContrastPercent,
			Temperature = s.Temperature,
			Tone = s.Tone,
			Mix = s.Mix?.ToArray(),
			Hue = s.Hue,
			RedGain = s.RedGain,
			GreenGain = s.GreenGain,
			BlueGain = s.BlueGain,
			ColourVision = s.ColourVision,
			ColourVisionFromPreset = s.ColourVisionFromPreset,
			ColourPresetName = (s.ColourPresetName ?? "")
		};
	}

	public void PutBack(AppSettings s)
	{
		s.SaturationPercent = SaturationPercent;
		s.VibrancePercent = VibrancePercent;
		s.VibranceChosenByUser = VibranceChosenByUser;
		s.BrightnessPercent = BrightnessPercent;
		s.GammaPercent = GammaPercent;
		s.ContrastPercent = ContrastPercent;
		s.Temperature = Temperature;
		s.Tone = Tone;
		s.Mix = Mix?.ToArray();
		s.Hue = Hue;
		s.RedGain = RedGain;
		s.GreenGain = GreenGain;
		s.BlueGain = BlueGain;
		s.ColourVision = ColourVision;
		s.ColourVisionFromPreset = ColourVisionFromPreset;
		s.ColourPresetName = ColourPresetName;
	}

	public static bool PutBackOwed(AppSettings s)
	{
		GameLookSnapshot lookBeforeGame = s.LookBeforeGame;
		if (lookBeforeGame == null)
		{
			return false;
		}
		lookBeforeGame.PutBack(s);
		s.LookBeforeGame = null;
		return true;
	}
}
