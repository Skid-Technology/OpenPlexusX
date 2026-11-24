using System;

namespace VibranceHud.Licensing;

internal sealed class ServedVibranceCurve
{
	private readonly float[][] _points;

	internal ServedVibranceCurve(float[][] points)
	{
		_points = points;
	}

	internal ServedVibrancePoint At(int level, bool driver, bool streaming)
	{
		float[] array = _points[Math.Clamp(level, 0, _points.Length - 1)];
		bool flag = driver && !streaming;
		return new ServedVibrancePoint((int)array[0], array[(!flag) ? 1 : 3], array[flag ? 4 : 2]);
	}
}
