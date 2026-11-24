namespace VibranceHud;

public sealed class GameProfileApplyGate
{
	private readonly AppSettings _settings;

	public GameProfileApplyGate(AppSettings settings)
	{
		_settings = settings;
	}

	public bool ShouldAutoApply(string gameId)
	{
		if (_settings.ManualOverrideActive)
		{
			return false;
		}
		return true;
	}

	public void UserOptedBackIn()
	{
		_settings.ManualOverrideActive = false;
	}
}
