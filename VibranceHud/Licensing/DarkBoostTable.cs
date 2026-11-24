namespace VibranceHud.Licensing;

public sealed class DarkBoostTable
{
	private readonly float[] _matrix;

	private readonly ushort[] _ramp;

	private readonly float[]? _fallback;

	public float[] Matrix => (float[])_matrix.Clone();

	public ushort[] Ramp => (ushort[])_ramp.Clone();

	public float[]? FallbackMatrix => (float[])_fallback?.Clone();

	public bool RampIsStraight { get; }

	internal DarkBoostTable(float[] matrix, ushort[] ramp, float[]? fallbackMatrix)
	{
		_matrix = (float[])matrix.Clone();
		_ramp = (ushort[])ramp.Clone();
		_fallback = (float[])fallbackMatrix?.Clone();
		RampIsStraight = DarkBoostPicture.IsStraight(_ramp);
	}
}
