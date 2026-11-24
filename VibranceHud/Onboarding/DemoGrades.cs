using System;
using System.Linq;

namespace VibranceHud.Onboarding;

public static class DemoGrades
{
	public readonly record struct Grade(int Saturation, int Vibrance, int Brightness, int Contrast, int Temperature)
	{
		public void ApplyTo(VibranceEngine engine)
		{
			if (engine == null)
			{
				throw new ArgumentNullException("engine");
			}
			engine.Saturation = Saturation;
			engine.Vibrance = Vibrance;
			engine.Brightness = Brightness;
			engine.Contrast = Contrast;
			engine.Temperature = Temperature;
		}
	}

	private const int UntouchedVibrance = 50;

	public static Grade Opening => new Grade(205, 100, 100, 104, -6);

	public static Grade ForGame(string? game)
	{
		if (string.IsNullOrWhiteSpace(game) || game == "Something else")
		{
			return Opening;
		}
		if (!SetupAnswers.Games.Any((string g) => string.Equals(g, game, StringComparison.OrdinalIgnoreCase)))
		{
			return Opening;
		}
		return new Grade(SetupAnswers.SaturationFor(game), 50, 100, 100, 0);
	}

	public static Grade ForComplaint(Complaint complaint)
	{
		return complaint switch
		{
			Complaint.WashedOut => new Grade(245, 100, 100, 110, -8), 
			Complaint.TooDark => new Grade(215, 100, 108, 92, -12), 
			Complaint.EyeStrain => new Grade(150, 90, 96, 98, 18), 
			Complaint.FlatRecordings => new Grade(230, 100, 100, 104, -6), 
			_ => Opening, 
		};
	}

	public static string AnswerLine(Complaint complaint, VibranceDriverState driver)
	{
		return complaint switch
		{
			Complaint.WashedOut => "More colour and more separation between shades. Grey stops being one shade of grey.", 
			Complaint.TooDark => "The dark end opened up and contrast eased off, so shade stops being a solid mass.", 
			Complaint.EyeStrain => "Warmer, and less colour rather than more. This one asks for less - it is the only setting here that does.", 
			Complaint.FlatRecordings => (driver == VibranceDriverState.Available) ? "Digital Vibrance is added after the picture is put together, so no recorder ever sees it. Everything PlexusX just did is in the picture itself, which is why a recording keeps it." : "All of this is in the picture itself, so what you record is what you see.", 
			_ => "", 
		};
	}
}
