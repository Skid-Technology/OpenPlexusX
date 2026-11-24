using System;

namespace VibranceHud;

public static class AnimationClockPolicy
{
	public static int IntervalFor(TimeSpan sinceLastInput, bool movingPicture)
	{
		if (!movingPicture && !(sinceLastInput < TimeSpan.FromSeconds(3.0)))
		{
			return 200;
		}
		return 47;
	}

	public static bool ShouldRun(bool visible, bool minimized, bool movingOrSizing, bool goingAway, bool active)
	{
		return (visible && !minimized && !movingOrSizing && !goingAway) & active;
	}
}
