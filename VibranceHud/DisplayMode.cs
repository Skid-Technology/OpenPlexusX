namespace VibranceHud;

public readonly record struct DisplayMode(int Width, int Height, int RefreshHz)
{
	public override string ToString()
	{
		return $"{Width}x{Height}";
	}
}
