using System;
using VibranceHud.Licensing;

namespace VibranceHud;

public static class ToneCurve
{
	private readonly record struct CurveParams(double InvGamma, double Shadows, double Highlights, double Blacks = 0.0, double Whites = 0.0, double ShadowTint = 0.0, double HighlightTint = 0.0);

	internal const bool Unlocked = false;

	public static ToneSettings Gate(ToneSettings t)
	{
		return t with
		{
			NightVision = false
		};
	}

	public static ushort[] Build(ToneSettings t)
	{
		return Build(t, ServedLooks.CurrentCurve);
	}

	public static ushort[] Build(ToneSettings t, GradeCurve? grade)
	{
		return BuildNormal(t, grade ?? GradeCurve.Withheld);
	}

	private static ushort[] BuildNormal(ToneSettings t, GradeCurve grade)
	{
		ushort[] array = new ushort[768];
		CurveParams c = Params(t);
		for (int i = 0; i < 256; i++)
		{
			double x = (double)i / 255.0;
			array[i] = Q(Channel(x, c, grade, 1));
			array[256 + i] = Q(Channel(x, c, grade, 0));
			array[512 + i] = Q(Channel(x, c, grade, -1));
		}
		EnforceMonotonic(array);
		return array;
	}

	private static CurveParams Params(ToneSettings t)
	{
		double num = (double)Math.Clamp(t.ResolvedGamma, 50, 150) / 100.0;
		return new CurveParams(1.0 / ((num <= 0.0) ? 1.0 : num), Norm(t.Shadows), Norm(t.Highlights), Norm(t.Blacks), Norm(t.Whites), Norm(t.ShadowTint), Norm(t.HighlightTint));
	}

	private static double Curve(double x, CurveParams c, GradeCurve grade)
	{
		double num = Math.Pow(x, c.InvGamma);
		double num2 = Weight(1.0 - num, grade.RegionSharpness);
		double num3 = Weight(num, grade.RegionSharpness);
		num += c.Shadows * num2 * grade.RegionRange;
		num += c.Highlights * num3 * grade.RegionRange;
		double num4 = Weight(1.0 - num, grade.PointSharpness);
		double num5 = Weight(num, grade.PointSharpness);
		num += c.Blacks * num4 * grade.PointRange;
		num += c.Whites * num5 * grade.PointRange;
		return Math.Clamp(num, 0.0, 1.0);
	}

	private static double Weight(double v, int sharpness)
	{
		if (sharpness != 2)
		{
			return Math.Pow(v, sharpness);
		}
		return v * v;
	}

	private static double Channel(double x, CurveParams c, GradeCurve grade, int lean)
	{
		double num = Curve(x, c, grade);
		if (lean == 0 || (c.ShadowTint == 0.0 && c.HighlightTint == 0.0))
		{
			return num;
		}
		double num2 = Weight(1.0 - num, grade.RegionSharpness);
		double num3 = Weight(num, grade.RegionSharpness);
		num += (double)lean * (c.ShadowTint * num2 + c.HighlightTint * num3) * grade.TintRange;
		return Math.Clamp(num, 0.0, 1.0);
	}

	private static double Norm(int slider)
	{
		return (double)Math.Clamp(slider, -100, 100) / 100.0;
	}

	private static ushort Q(double v)
	{
		return (ushort)Math.Clamp(Math.Round(Math.Clamp(v, 0.0, 1.0) * 65535.0), 0.0, 65535.0);
	}

	private static void EnforceMonotonic(ushort[] ramp)
	{
		for (int i = 0; i < 3; i++)
		{
			int num = i * 256;
			for (int j = 1; j < 256; j++)
			{
				if (ramp[num + j] < ramp[num + j - 1])
				{
					ramp[num + j] = ramp[num + j - 1];
				}
			}
		}
	}
}
