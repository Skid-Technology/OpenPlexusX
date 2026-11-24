namespace VibranceHud.Licensing;

public sealed record GradeCurve(double RegionRange, int RegionSharpness, double PointRange, int PointSharpness, double TintRange)
{
	public static readonly GradeCurve Withheld = new GradeCurve(0.0, 1, 0.0, 1, 0.0);
}
