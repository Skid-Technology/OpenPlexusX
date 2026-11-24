namespace VibranceHud;

public sealed record CaptureProbe(bool Ran, double MedianRatio, bool ReachesCapture, string Note, string Detail = "")
{
	public static CaptureProbe Failed(string why, string detail = "")
	{
		return new CaptureProbe(Ran: false, 0.0, ReachesCapture: false, why, detail);
	}
}
