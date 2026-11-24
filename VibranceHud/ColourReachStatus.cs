namespace VibranceHud;

public static class ColourReachStatus
{
	public const string FullscreenSentence = "Fullscreen: colour comes from the driver. Borderless gives the full look.";

	public const string CompetitiveSentence = "Colour on the driver: in full-screen games Competitive mode keeps your colour on the NVIDIA driver. Hue, channel mix, colour aids and Eye care pause there.";

	public const string ScreenEffectRefusedSentence = "Colour engine off: Windows could not start the screen effect (Magnification). Restart PlexusX; if it stays off, tell us.";

	public const string DisplayCarriesItSentence = "Full screen: your monitor is holding this look. It is the closest it can get.";

	public const string RampRefusedSentence = "Full screen: Windows refused this look on this monitor. Borderless gives the full look.";

	public const string RampIgnoredHdrSentence = "Full screen: HDR is on, and Windows ignores this look there. Borderless gives the full look.";

	public const string LookShowsInBorderless = "This look shows in Borderless or Windowed mode. In exclusive fullscreen the game only gets the driver's colour.";

	public const string LookNeedsPaidColourControls = "This look needs the paid colour controls, and they are off on this PC right now.";

	public static string DisplayCarriesItButNot(string missing)
	{
		return "Full screen: your monitor is holding this look, but not " + missing + ". Borderless gives the full look.";
	}

	public static string RampSoftenedSentence(string step)
	{
		return "Full screen: Windows only let " + StepWords(step) + " of this look through. Borderless gives the full look.";
	}

	private static string StepWords(string step)
	{
		return step switch
		{
			"1/2" => "half", 
			"1/4" => "a quarter", 
			"1/8" => "an eighth", 
			_ => "part", 
		};
	}

	public static ColourReachLine For(ColourReachFacts facts)
	{
		if (!facts.OverlayLive && (!facts.DxTried || facts.DxFailure == DxInitFailureKind.None))
		{
			return new ColourReachLine(ColourReachSeverity.Refused, "Colour engine off: Windows could not start the screen effect (Magnification). Restart PlexusX; if it stays off, tell us.");
		}
		if (!facts.OverlayLive)
		{
			return new ColourReachLine(ColourReachSeverity.Refused, "Colour engine off: " + Because(facts.DxFailure) + ".");
		}
		if (facts.ExclusiveFullscreen && facts.DisplayIsCarryingTheLook && !facts.RampOutcome.IsFull)
		{
			return new ColourReachLine(ColourReachSeverity.Note, facts.RampOutcome.Kind switch
			{
				GammaDeliveryKind.Softened => RampSoftenedSentence(facts.RampOutcome.Step), 
				GammaDeliveryKind.IgnoredHdr => "Full screen: HDR is on, and Windows ignores this look there. Borderless gives the full look.", 
				_ => "Full screen: Windows refused this look on this monitor. Borderless gives the full look.", 
			});
		}
		if (facts.ExclusiveFullscreen && facts.DisplayIsCarryingTheLook)
		{
			return new ColourReachLine(ColourReachSeverity.Note, string.IsNullOrEmpty(facts.WhatTheDisplayCannotHold) ? "Full screen: your monitor is holding this look. It is the closest it can get." : DisplayCarriesItButNot(facts.WhatTheDisplayCannotHold));
		}
		if (facts.ExclusiveFullscreen)
		{
			return new ColourReachLine(ColourReachSeverity.Note, "Fullscreen: colour comes from the driver. Borderless gives the full look.");
		}
		if (facts.CompetitiveOn)
		{
			return new ColourReachLine(ColourReachSeverity.Note, "Colour on the driver: in full-screen games Competitive mode keeps your colour on the NVIDIA driver. Hue, channel mix, colour aids and Eye care pause there.");
		}
		return new ColourReachLine(ColourReachSeverity.Ok, "");
	}

	public static string Because(DxInitFailureKind kind)
	{
		return kind switch
		{
			DxInitFailureKind.None => "DirectX started but no monitor could be rendered", 
			DxInitFailureKind.NoCompatibleAdapter => "no graphics card here can do DirectX 11", 
			DxInitFailureKind.DeviceCreationFailed => "Windows would not give PlexusX a DirectX device", 
			DxInitFailureKind.NoOutputs => "Windows reported no display", 
			DxInitFailureKind.DriverIssue => "your display driver turned DirectX 11 down", 
			DxInitFailureKind.SdkIssue => "the DirectX 11 runtime is not installed", 
			DxInitFailureKind.OutOfMemory => "your graphics memory is full", 
			DxInitFailureKind.CaptureInUse => "another app is capturing this display", 
			DxInitFailureKind.Unknown => "DirectX 11 would not start on this PC", 
			_ => "DirectX 11 would not start on this PC", 
		};
	}
}
