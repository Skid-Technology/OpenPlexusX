namespace VibranceHud.Zoom;

public static class ZoomEmergency
{
	internal static volatile bool WeZoomed;

	public static void Reset()
	{
		try
		{
			MagRuntime.Native.MagSetFullscreenTransform(1f, 0, 0);
		}
		catch
		{
		}
	}

	public static void ResetIfWeZoomed()
	{
		if (WeZoomed)
		{
			Reset();
		}
	}
}
