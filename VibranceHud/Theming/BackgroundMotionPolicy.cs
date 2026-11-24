namespace VibranceHud.Theming;

public static class BackgroundMotionPolicy
{
	public static bool ShouldPlay(bool pictureMoves, bool windowVisible, bool minimized, bool inForeground, bool gameRunning, bool goingAway, bool reducedMotion, bool movingOrSizing = false)
	{
		if ((((pictureMoves & windowVisible) && !minimized) & inForeground) && !gameRunning && !goingAway && !reducedMotion)
		{
			return !movingOrSizing;
		}
		return false;
	}
}
