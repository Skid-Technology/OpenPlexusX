namespace VibranceHud;

public static class ColourVision
{
	private static readonly double[,] RgbToLms = new double[3, 3]
	{
		{ 17.8824, 43.5161, 4.11935 },
		{ 3.45565, 27.1554, 3.86714 },
		{ 0.0299566, 0.184309, 1.46709 }
	};

	private static readonly double[,] LmsToRgb = new double[3, 3]
	{
		{ 0.0809444479, -0.130504409, 0.116721066 },
		{ -0.0102485335, 0.0540193266, -0.113614708 },
		{ -0.000365296938, -0.00412161469, 0.693511405 }
	};

	private static readonly double[,] ErrorShift = new double[3, 3]
	{
		{ 0.0, 0.0, 0.0 },
		{ 0.7, 1.0, 0.0 },
		{ 0.7, 0.0, 1.0 }
	};

	private static double[,] Simulation(ColourVisionMode mode)
	{
		return mode switch
		{
			ColourVisionMode.RedWeak => new double[3, 3]
			{
				{ 0.0, 2.02344, -2.52581 },
				{ 0.0, 1.0, 0.0 },
				{ 0.0, 0.0, 1.0 }
			}, 
			ColourVisionMode.GreenWeak => new double[3, 3]
			{
				{ 1.0, 0.0, 0.0 },
				{ 0.494207, 0.0, 1.24827 },
				{ 0.0, 0.0, 1.0 }
			}, 
			ColourVisionMode.BlueWeak => new double[3, 3]
			{
				{ 1.0, 0.0, 0.0 },
				{ 0.0, 1.0, 0.0 },
				{ -0.395913, 0.801109, 0.0 }
			}, 
			_ => Identity3(), 
		};
	}

	public static float[] Matrix(ColourVisionMode mode)
	{
		float[] array = Identity5();
		switch (mode)
		{
		case ColourVisionMode.Off:
			return array;
		case ColourVisionMode.HoloBlack:
			return HoloBlack.GreenSubtracted();
		default:
		{
			double[,] b = Multiply3(LmsToRgb, Multiply3(Simulation(mode), RgbToLms));
			double[,] b2 = Subtract3(Identity3(), b);
			double[,] array2 = Add3(Identity3(), Multiply3(ErrorShift, b2));
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					array[j * 5 + i] = (float)array2[i, j];
				}
			}
			return array;
		}
		}
	}

	public static float[] Compose(float[] first, float[] second)
	{
		float[] array = new float[25];
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				float num = 0f;
				for (int k = 0; k < 5; k++)
				{
					num += first[i * 5 + k] * second[k * 5 + j];
				}
				array[i * 5 + j] = num;
			}
		}
		return array;
	}

	private static double[,] Identity3()
	{
		return new double[3, 3]
		{
			{ 1.0, 0.0, 0.0 },
			{ 0.0, 1.0, 0.0 },
			{ 0.0, 0.0, 1.0 }
		};
	}

	private static float[] Identity5()
	{
		float[] array = new float[25];
		for (int i = 0; i < 5; i++)
		{
			array[i * 5 + i] = 1f;
		}
		return array;
	}

	private static double[,] Multiply3(double[,] a, double[,] b)
	{
		double[,] array = new double[3, 3];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				double num = 0.0;
				for (int k = 0; k < 3; k++)
				{
					num += a[i, k] * b[k, j];
				}
				array[i, j] = num;
			}
		}
		return array;
	}

	private static double[,] Add3(double[,] a, double[,] b)
	{
		double[,] array = new double[3, 3];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				array[i, j] = a[i, j] + b[i, j];
			}
		}
		return array;
	}

	private static double[,] Subtract3(double[,] a, double[,] b)
	{
		double[,] array = new double[3, 3];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				array[i, j] = a[i, j] - b[i, j];
			}
		}
		return array;
	}

	public static (double R, double G, double B) Simulate(ColourVisionMode mode, double r, double g, double b)
	{
		if (mode == ColourVisionMode.Off)
		{
			return (R: r, G: g, B: b);
		}
		double[,] array = Multiply3(LmsToRgb, Multiply3(Simulation(mode), RgbToLms));
		return (R: array[0, 0] * r + array[0, 1] * g + array[0, 2] * b, G: array[1, 0] * r + array[1, 1] * g + array[1, 2] * b, B: array[2, 0] * r + array[2, 1] * g + array[2, 2] * b);
	}
}
