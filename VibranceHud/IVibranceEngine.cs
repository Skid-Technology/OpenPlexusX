namespace VibranceHud;

public interface IVibranceEngine
{
	int Vibrance { get; set; }

	bool VibranceChosenByUser => false;

	bool StreamingMode
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	bool DriverAvailable => false;

	bool PaidControlsUnlocked => false;

	bool ScreenEffectIsLive => true;

	int Saturation { get; set; }

	int Brightness { get; set; }

	int Gamma { get; set; }

	int Contrast
	{
		get
		{
			return 100;
		}
		set
		{
		}
	}

	int Temperature
	{
		get
		{
			return 0;
		}
		set
		{
		}
	}

	ToneSettings Tone
	{
		get
		{
			return ToneSettings.Neutral;
		}
		set
		{
		}
	}

	DisplayOnlyLook.Knob DriverKnobFollows
	{
		get
		{
			return DisplayOnlyLook.Knob.Stronger;
		}
		set
		{
		}
	}

	int Hue
	{
		get
		{
			return 0;
		}
		set
		{
		}
	}

	int RedGain
	{
		get
		{
			return 100;
		}
		set
		{
		}
	}

	int GreenGain
	{
		get
		{
			return 100;
		}
		set
		{
		}
	}

	int BlueGain
	{
		get
		{
			return 100;
		}
		set
		{
		}
	}

	ColourVisionMode ColourVision
	{
		get
		{
			return ColourVisionMode.Off;
		}
		set
		{
		}
	}

	ChannelMix Mix
	{
		get
		{
			return ChannelMix.Identity;
		}
		set
		{
		}
	}

	void SeedVibrance(int value)
	{
		Vibrance = value;
	}

	CaptureProbe RunCaptureProbe()
	{
		return CaptureProbe.Failed("not supported by this engine");
	}

	void BeginDrag();

	void EndDrag();

	void SuspendOverlay();

	void ResumeOverlay();
}
