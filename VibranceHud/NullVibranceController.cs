namespace VibranceHud;

public sealed class NullVibranceController : IVibranceController
{
	public int CurrentLevel => 100;

	public int DefaultLevel => 100;

	public bool IsAvailable => false;

	public VibranceDriverState DriverState { get; }

	public NullVibranceController(VibranceDriverState state = VibranceDriverState.NoNvidiaCard)
	{
		DriverState = state;
	}

	public void SetLevel(int level)
	{
	}

	public bool EveryDisplayIsAt(int level)
	{
		return true;
	}
}
