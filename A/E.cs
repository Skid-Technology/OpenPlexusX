using System;
using VibranceHud;
using VibranceHud.Licensing;

namespace A;

internal static class e
{
	private static readonly Lazy<ServedVibranceCurve> FallbackCurve = new Lazy<ServedVibranceCurve>(BuildFallback);

	internal static ServedVibranceCurve Fallback => FallbackCurve.Value;

	internal static ServedVibrancePoint A(int P_0, bool P_1, bool P_2)
	{
		return Fallback.At(P_0, P_1, P_2);
	}

	private static ServedVibranceCurve BuildFallback()
	{
		float[][] array = new float[351][];
		for (int i = 0; i < array.Length; i++)
		{
			float num = VibranceEngine.SoftwareVibranceFactor(i, driverAvailable: false);
			float num2 = VibranceEngine.SoftwareVibranceFactor(i, driverAvailable: true);
			array[i] = new float[5]
			{
				Math.Min(i, 100),
				1f + (num - 1f) * 0.55f,
				num,
				1f + (num2 - 1f) * 0.55f,
				num2
			};
		}
		return new ServedVibranceCurve(array);
	}
}
