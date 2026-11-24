using System;

namespace VibranceHud.Licensing;

public static class ServedNightVision
{
	private static readonly float[] DefaultMatrix = new float[25]
	{
		0.20f, 0.70f, 0.10f, 0f, 0f,
		0.35f, 1.45f, 0.25f, 0f, 0f,
		0.15f, 0.45f, 0.15f, 0f, 0f,
		0f,    0f,    0f,    1f, 0f,
		-0.01f, -0.02f, -0.01f, 0f, 1f
	};

	private static readonly DarkBoostTable DefaultDarkBoost = BuildDefaultDarkBoost();

	public static bool Live => true;

	public static float[]? Matrix => (float[])DefaultMatrix.Clone();

	public static int Generation => 1;

	public static DarkBoostTable? DarkBoost => DefaultDarkBoost;

	internal static void Install()
	{
	}

	private static DarkBoostTable BuildDefaultDarkBoost()
	{
		ushort[] ramp = new ushort[768];
		for (int c = 0; c < 3; c++)
		{
			int offset = c * 256;
			for (int i = 0; i < 256; i++)
			{
				if (i == 255)
				{
					ramp[offset + i] = ushort.MaxValue;
					continue;
				}
				double x = (double)i / 255.0;
				double lifted = Math.Pow(x, 0.62);
				int target = (int)Math.Round(Math.Clamp(lifted, 0.0, 1.0) * 65535.0);
				int maxAllowed = Math.Min(65535, (i + 120) << 8);
				int minAllowed = Math.Max(0, (i - 120) << 8);
				int clamped = Math.Clamp(target, minAllowed, maxAllowed);
				if (i > 0 && clamped < ramp[offset + i - 1])
				{
					clamped = ramp[offset + i - 1];
				}
				ramp[offset + i] = (ushort)clamped;
			}
		}
		return new DarkBoostTable((float[])DefaultMatrix.Clone(), ramp, (float[])DefaultMatrix.Clone());
	}
}
