namespace VibranceHud;

public sealed class ColourWriteMeter
{
	public int Driver { get; private set; }

	public int Gamma { get; private set; }

	public int Overlay { get; private set; }

	public void CountDriver()
	{
		Driver++;
	}

	internal void CountGamma()
	{
		Gamma++;
	}

	public void CountOverlay()
	{
		Overlay++;
	}

	public void AddGamma(int uploads)
	{
		if (uploads > 0)
		{
			Gamma += uploads;
		}
	}
}
