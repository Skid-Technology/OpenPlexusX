namespace VibranceHud.Display;

public readonly record struct PanelSize(int Width, int Height)
{
	public bool Known
	{
		get
		{
			if (Width > 0)
			{
				return Height > 0;
			}
			return false;
		}
	}

	public override string ToString()
	{
		return $"{Width} x {Height}";
	}
}
