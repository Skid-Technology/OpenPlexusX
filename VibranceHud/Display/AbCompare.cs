using System;

namespace VibranceHud.Display;

public sealed class AbCompare
{
	private readonly IClock _clock;

	private readonly TimeSpan _cooldown;

	private DateTime _lastToggle = DateTime.MinValue;

	public bool ShowingNeutral { get; private set; }

	public AbCompare(IClock? clock = null, TimeSpan? cooldown = null)
	{
		_clock = clock ?? new SystemClock();
		_cooldown = cooldown ?? TimeSpan.FromMilliseconds(250.0);
	}

	public bool TryToggle()
	{
		DateTime utcNow = _clock.UtcNow;
		if (utcNow - _lastToggle < _cooldown)
		{
			return false;
		}
		_lastToggle = utcNow;
		ShowingNeutral = !ShowingNeutral;
		return true;
	}

	public void Reset()
	{
		ShowingNeutral = false;
	}
}
