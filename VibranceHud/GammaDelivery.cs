namespace VibranceHud;

public readonly record struct GammaDelivery(GammaDeliveryKind Kind, int Displays = 0, string Step = "")
{
	public static GammaDelivery Full => new GammaDelivery(GammaDeliveryKind.Full);

	public bool IsFull => Kind == GammaDeliveryKind.Full;

	public string ReportValue => Kind.ToString();
}
