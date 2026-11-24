using System.Threading.Tasks;

namespace VibranceHud;

public sealed class ProfileApplyEngine
{
	private readonly IVibranceEngine _engine;

	private GameProfile? _current;

	private GameProfile? _before;

	private bool _beforeWasChosen;

	private ChannelMix _beforeMix = ChannelMix.Identity;

	private int _beforeHue;

	private int _beforeRed = 100;

	private int _beforeGreen = 100;

	private int _beforeBlue = 100;

	private ColourVisionMode _beforeVision;

	public GameProfile? Current => _current;

	public ProfileApplyEngine(IVibranceEngine engine)
	{
		_engine = engine;
	}

	public void SetCurrent(GameProfile profile)
	{
		_current = profile;
	}

	public void SnapshotDesktop()
	{
		if (_before == null)
		{
			_before = new GameProfile
			{
				GameId = "snapshot",
				Vibrance = _engine.Vibrance,
				Saturation = _engine.Saturation,
				Brightness = _engine.Brightness,
				Gamma = _engine.Gamma,
				Contrast = _engine.Contrast,
				Temperature = _engine.Temperature,
				Tone = _engine.Tone
			};
			_beforeWasChosen = _engine.VibranceChosenByUser;
			_beforeMix = _engine.Mix;
			_beforeHue = _engine.Hue;
			_beforeRed = _engine.RedGain;
			_beforeGreen = _engine.GreenGain;
			_beforeBlue = _engine.BlueGain;
			_beforeVision = _engine.ColourVision;
		}
	}

	public Task ApplyAsync(string gameId)
	{
		if (_current == null || _current.GameId != gameId)
		{
			return Task.CompletedTask;
		}
		SnapshotDesktop();
		_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Stronger;
		SetVibrance(_current.Vibrance);
		_engine.Saturation = _current.Saturation;
		_engine.Brightness = _current.Brightness;
		_engine.Gamma = _current.Gamma;
		_engine.Contrast = _current.ResolvedContrast;
		_engine.Temperature = _current.ResolvedTemperature;
		_engine.Tone = _current.ResolvedTone;
		return Task.CompletedTask;
	}

	public Task RestoreAsync()
	{
		if (_before == null)
		{
			return Task.CompletedTask;
		}
		_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Stronger;
		SetVibrance(_before.Vibrance);
		_engine.Saturation = _before.Saturation;
		_engine.Brightness = _before.Brightness;
		_engine.Gamma = _before.Gamma;
		_engine.Contrast = _before.ResolvedContrast;
		_engine.Temperature = _before.ResolvedTemperature;
		_engine.Tone = _before.ResolvedTone;
		_engine.Mix = _beforeMix;
		_engine.Hue = _beforeHue;
		_engine.RedGain = _beforeRed;
		_engine.GreenGain = _beforeGreen;
		_engine.BlueGain = _beforeBlue;
		_engine.ColourVision = _beforeVision;
		_before = null;
		_current = null;
		return Task.CompletedTask;
	}

	private void SetVibrance(int value)
	{
		if (_beforeWasChosen)
		{
			_engine.Vibrance = value;
		}
		else
		{
			_engine.SeedVibrance(value);
		}
	}
}
