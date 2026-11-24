namespace VibranceHud.Display;

public static class ScalingModeNames
{
	public static string Label(this ScalingMode mode)
	{
		return mode switch
		{
			ScalingMode.AspectRatio => "Aspect ratio", 
			ScalingMode.NoScaling => "No scaling", 
			ScalingMode.FullScreen => "Full screen", 
			ScalingMode.Integer => "Integer", 
			_ => mode.ToString(), 
		};
	}
}
