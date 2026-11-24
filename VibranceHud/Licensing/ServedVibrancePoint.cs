namespace VibranceHud.Licensing;

internal readonly record struct ServedVibrancePoint(int Driver, float Red, float Chroma)
{
	internal static readonly ServedVibrancePoint Neutral = new ServedVibrancePoint(50, 1f, 1f);
}
