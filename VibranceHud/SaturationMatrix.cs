namespace VibranceHud;

public static class SaturationMatrix
{
	private const float Lr = 0.2126f;

	private const float Lg = 0.7152f;

	private const float Lb = 0.0722f;

	public static float[] Build(float saturation)
	{
		return Build(saturation, saturation, saturation);
	}

	public static float[] Build(float sR, float sG, float sB)
	{
		return Build(sR, sG, sB, 0.2126f, 0.7152f, 0.0722f);
	}

	public static float[] Build(float sR, float sG, float sB, float gr, float gg, float gb)
	{
		float num = 1f - sR;
		float num2 = 1f - sG;
		float num3 = 1f - sB;
		return new float[25]
		{
			gr * num + sR,
			gr * num2,
			gr * num3,
			0f,
			0f,
			gg * num,
			gg * num2 + sG,
			gg * num3,
			0f,
			0f,
			gb * num,
			gb * num2,
			gb * num3 + sB,
			0f,
			0f,
			0f,
			0f,
			0f,
			1f,
			0f,
			0f,
			0f,
			0f,
			0f,
			1f
		};
	}
}
