namespace VibranceHud;

public static class HoloBlack
{
	public static float[] GreenSubtracted()
	{
		float[] array = Identity();
		array[0] = 1.5f;
		array[1] = 1f;
		array[2] = 0.5f;
		array[5] = -1f;
		array[6] = -1f;
		array[7] = -1f;
		array[10] = 0.5f;
		array[11] = 1f;
		array[12] = 1.5f;
		return array;
	}

	private static float[] Identity()
	{
		float[] array = new float[25];
		for (int i = 0; i < 5; i++)
		{
			array[i * 5 + i] = 1f;
		}
		return array;
	}
}
