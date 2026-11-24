using System;
using System.Collections.Generic;

namespace VibranceHud;

public static class SaturationMetrics
{
	public sealed record SaturationMeasurement(SaturationMetric Kind, string Name, int Saturation, double GreySafety, double LumaError, double ClippingPercent, double SkinGain, double HueDriftDegrees, double DeliveredGain, double RedFactor, double GreenFactor, double BlueFactor);

	public static readonly IReadOnlyList<SaturationMetricInfo> All = new SaturationMetricInfo[9]
	{
		new SaturationMetricInfo(SaturationMetric.Rec709, "Rec.709 flat", "Today's Saturation. Lifts every channel equally around Rec.709 luma - the picture never brightens or darkens, only spreads.", "Rec.709", "Every colour gets stronger evenly; brightness stays the same."),
		new SaturationMetricInfo(SaturationMetric.AverageGrey, "Average grey", "Grey = (R+G+B)/3. The formula most tools use. Nothing holds a hue still, so it is the one to beat on brightness honesty rather than the one to trust.", "Average", "Simple average. Some colours look brighter or darker as they get stronger."),
		new SaturationMetricInfo(SaturationMetric.Rec601, "Rec.601 luma", "Grey = television luma. Slightly greener hold than Rec.709 - subtler than the name suggests, but measurable.", "Rec.601", "Like Rec.709, with greens held back a little."),
		new SaturationMetricInfo(SaturationMetric.GreenLocked, "Green-locked", "Grey = green alone. The far end of the grey axis: leaves and skin both move hardest.", "Green-lock", "The strongest pull on leaves and skin."),
		new SaturationMetricInfo(SaturationMetric.Perceptual, "Perceptual (OKLab)", "Grey = OKLab's lightness weights. The only axis here that comes from perceptual science rather than a video standard.", "Perceptual", "Follows how the eye sees brightness. The most even."),
		new SaturationMetricInfo(SaturationMetric.RedGrey, "Red-grey", "Grey = red-weighted. The mirror of green-locked: warm mid-tones and rust sit closest to the grey it holds, cool tones move furthest.", "Red-grey", "Warm tones change least; cool tones change the most."),
		new SaturationMetricInfo(SaturationMetric.BlueGrey, "Blue-grey, deep green", "Grey = blue-weighted, so sky and shadow hold still while firelight and skin move. Foliage is darkened by the Green darken control, which takes light out of the greens and leaves their colour alone.", "Blue-grey", "Sky and shadows hold still; firelight and skin change the most."),
		new SaturationMetricInfo(SaturationMetric.SkinSparing, "Skin-sparing (travel)", "Rec.709, but red travels at 45% of the slider and green and blue at 110%. The app's existing Vibrance idea, applied to the one saturation control.", "Skin-sparing", "Gentler on reds and skin, stronger on greens and blues."),
		new SaturationMetricInfo(SaturationMetric.DeepBlue, "Deep blue (mixed)", "Flat Rec.709 with the blue-range deepen composed on top - saturation and one colour option as one control, to price what mixing them costs.", "Deep blue", "Rec.709, plus deeper blues.")
	};

	public const int MinGreenDarken = 0;

	public const int MaxGreenDarken = 400;

	public const int MinHueTurn = -180;

	public const int MaxHueTurn = 180;

	public const int MinHueTurnAmount = 0;

	public const int MaxHueTurnAmount = 100;

	public const int MinHueComplement = 0;

	public const int MaxHueComplement = 100;

	public const int DeepBlueSaturation = 165;

	public const double GreySafetyBar = 1.0 / 255.0;

	private static readonly (double R, double G, double B) Skin = (R: 224.0 / 255.0, G: 172.0 / 255.0, B: 105.0 / 255.0);

	private static readonly (double R, double G, double B) MidGreen = (R: 77.0 / 255.0, G: 107.0 / 255.0, B: 46.0 / 255.0);

	public const double MinimumChromaForAHue = 0.02;

	public static int Count => All.Count;

	public static (float R, float G, float B) Grey(SaturationMetric kind)
	{
		return GreyOf(kind);
	}

	public static (float R, float G, float B) Travel(SaturationMetric kind)
	{
		return BiasOf(kind);
	}

	public static SaturationMetricInfo Info(SaturationMetric kind)
	{
		return All[(int)kind];
	}

	public static SaturationMetric FromIndex(int index)
	{
		return (SaturationMetric)((index % Count + Count) % Count);
	}

	private static (float R, float G, float B) GreyOf(SaturationMetric kind)
	{
		return kind switch
		{
			SaturationMetric.AverageGrey => (R: 1f / 3f, G: 1f / 3f, B: 1f / 3f), 
			SaturationMetric.Rec601 => (R: 0.299f, G: 0.587f, B: 0.114f), 
			SaturationMetric.GreenLocked => (R: 0f, G: 1f, B: 0f), 
			SaturationMetric.RedGrey => (R: 0.55f, G: 0.35f, B: 0.1f), 
			SaturationMetric.BlueGrey => (R: 0.1f, G: 0.35f, B: 0.55f), 
			SaturationMetric.Perceptual => (R: 0.4122f, G: 0.5363f, B: 0.0515f), 
			_ => (R: 0.2126f, G: 0.7152f, B: 0.0722f), 
		};
	}

	internal static (float R, float G, float B) DarkenTarget(int hueDegrees)
	{
		int num = (hueDegrees % 360 + 360) % 360;
		if (num < 120)
		{
			return (R: (float)(120 - num) / 120f, G: (float)num / 120f, B: 0f);
		}
		if (num < 240)
		{
			return (R: 0f, G: (float)(240 - num) / 120f, B: (float)(num - 120) / 120f);
		}
		return (R: (float)(num - 240) / 120f, G: 0f, B: (float)(360 - num) / 120f);
	}

	private static float[] DarkenMatrix(float k, int hueDegrees)
	{
		(float R, float G, float B) tuple = DarkenTarget(hueDegrees);
		float num = tuple.R - 0.2126f;
		float num2 = tuple.G - 0.7152f;
		float num3 = tuple.B - 0.0722f;
		float[] array = new float[25];
		array[0] = 1f - k * num;
		array[5] = (0f - k) * num2;
		array[10] = (0f - k) * num3;
		array[1] = (0f - k) * num;
		array[6] = 1f - k * num2;
		array[11] = (0f - k) * num3;
		array[2] = (0f - k) * num;
		array[7] = (0f - k) * num2;
		array[12] = 1f - k * num3;
		array[18] = 1f;
		array[24] = 1f;
		return array;
	}

	private static (float R, float G, float B) BiasOf(SaturationMetric kind)
	{
		if (kind == SaturationMetric.SkinSparing)
		{
			return (R: 0.45f, G: 1.1f, B: 1.1f);
		}
		return (R: 1f, G: 1f, B: 1f);
	}

	private static (float R, float G, float B) HoldOf(SaturationMetric kind)
	{
		if (kind == SaturationMetric.SkinSparing)
		{
			return (R: 0.45f, G: 1f, B: 1f);
		}
		return (R: 0.55f, G: 1f, B: 1f);
	}

	public static (float R, float G, float B) Factors(SaturationMetric kind, float saturation, float vibrance)
	{
		(float, float, float) tuple = BiasOf(kind);
		(float, float, float) tuple2 = HoldOf(kind);
		return (R: Factor(saturation, vibrance, tuple.Item1, tuple2.Item1), G: Factor(saturation, vibrance, tuple.Item2, tuple2.Item2), B: Factor(saturation, vibrance, tuple.Item3, tuple2.Item3));
	}

	private static float Factor(float saturation, float vibrance, float travel, float hold)
	{
		if (travel == 1f && hold == 1f)
		{
			return saturation * vibrance;
		}
		float num = ((travel == 1f || saturation < 1f) ? saturation : (1f + (saturation - 1f) * travel));
		float num2 = ((hold == 1f) ? vibrance : (1f + (vibrance - 1f) * hold));
		return num * num2;
	}

	public static bool IsIdentityAtRest(SaturationMetric kind)
	{
		return IsIdentityAtRest(new ColourDepth(kind));
	}

	public static bool IsIdentityAtRest(ColourDepth depth)
	{
		if (depth.Formula != SaturationMetric.DeepBlue && !HueIsTurned(depth))
		{
			return depth.Darken == 0;
		}
		return false;
	}

	public static bool HueIsTurned(ColourDepth depth)
	{
		if (depth.HueComplement <= 0)
		{
			if (depth.HueTurn != 0)
			{
				return depth.HueTurnAmount > 0;
			}
			return false;
		}
		return true;
	}

	private static float[] HueMatrix(float degrees)
	{
		double num = (double)degrees * Math.PI / 180.0;
		float num2 = (float)Math.Cos(num);
		float num3 = (float)Math.Sin(num);
		float num4 = 0.213f + num2 * 0.787f - num3 * 0.213f;
		float num5 = 0.715f - num2 * 0.715f - num3 * 0.715f;
		float num6 = 0.072f - num2 * 0.072f + num3 * 0.928f;
		float num7 = 0.213f - num2 * 0.213f + num3 * 0.143f;
		float num8 = 0.715f + num2 * 0.285f + num3 * 0.14f;
		float num9 = 0.072f - num2 * 0.072f - num3 * 0.283f;
		float num10 = 0.213f - num2 * 0.213f - num3 * 0.787f;
		float num11 = 0.715f - num2 * 0.715f + num3 * 0.715f;
		float num12 = 0.072f + num2 * 0.928f + num3 * 0.072f;
		float[] array = new float[25];
		array[0] = num4;
		array[5] = num5;
		array[10] = num6;
		array[1] = num7;
		array[6] = num8;
		array[11] = num9;
		array[2] = num10;
		array[7] = num11;
		array[12] = num12;
		array[18] = 1f;
		array[24] = 1f;
		return array;
	}

	private static float[] BlendWithIdentity(float[] turned, float amount, float[] identity)
	{
		float[] array = new float[25];
		for (int i = 0; i < 25; i++)
		{
			array[i] = identity[i] + (turned[i] - identity[i]) * amount;
		}
		return array;
	}

	private static float[] IdentityMatrix()
	{
		float[] array = new float[25];
		for (int i = 0; i < 5; i++)
		{
			array[i * 5 + i] = 1f;
		}
		return array;
	}

	private static float[]? HueSlidersMatrix(ColourDepth depth)
	{
		if (!HueIsTurned(depth))
		{
			return null;
		}
		float[] array = IdentityMatrix();
		float[] array2 = array;
		if (depth.HueTurn != 0 && depth.HueTurnAmount > 0)
		{
			array2 = BlendWithIdentity(HueMatrix(depth.HueTurn), (float)depth.HueTurnAmount / 100f, array);
		}
		if (depth.HueComplement > 0)
		{
			float[] turned = HueMatrix(180f);
			array2 = ColourBalance.Multiply(array2, BlendWithIdentity(turned, (float)depth.HueComplement / 100f, array));
		}
		return array2;
	}

	public static float[] Build(SaturationMetric kind, float saturation, float vibrance)
	{
		return Build(kind, saturation, vibrance, new ColourDepth(kind));
	}

	public static float[] Build(SaturationMetric kind, float saturation, float vibrance, ColourDepth depth)
	{
		return Finish(kind, Factors(kind, saturation, vibrance), depth);
	}

	internal static float[] BuildWithVibranceNumbers(SaturationMetric kind, float saturation, float red, float chroma, ColourDepth depth)
	{
		(float, float, float) tuple = BiasOf(kind);
		float num = ((kind == SaturationMetric.SkinSparing) ? (1f + (chroma - 1f) * HoldOf(kind).R) : red);
		return Finish(kind, (R: Travelled(saturation, tuple.Item1) * num, G: Travelled(saturation, tuple.Item2) * chroma, B: Travelled(saturation, tuple.Item3) * chroma), depth);
	}

	private static float Travelled(float saturation, float travel)
	{
		if (travel != 1f && !(saturation < 1f))
		{
			return 1f + (saturation - 1f) * travel;
		}
		return saturation;
	}

	private static float[] Finish(SaturationMetric kind, (float R, float G, float B) f, ColourDepth depth)
	{
		depth = depth.Clamped();
		(float R, float G, float B) tuple = GreyOf(kind);
		float item = tuple.R;
		float item2 = tuple.G;
		float item3 = tuple.B;
		float[] array = SaturationMatrix.Build(f.R, f.G, f.B, item, item2, item3);
		float[] array2 = HueSlidersMatrix(depth);
		if (array2 != null)
		{
			array = ColourBalance.Multiply(array2, array);
		}
		if (kind == SaturationMetric.DeepBlue)
		{
			array = ColourBalance.Multiply(array, DeepenBlueMatrix(165));
		}
		int num = Math.Clamp(depth.Darken, 0, 400);
		if (num != 0)
		{
			return ColourBalance.Multiply(array, DarkenMatrix((float)num / 100f, depth.DarkenHue));
		}
		return array;
	}

	private static float[] DeepenBlueMatrix(int blueSaturation)
	{
		float num = (float)(Math.Clamp(blueSaturation, 0, 200) - 100) / 100f;
		float num2 = -0.2126f;
		float num3 = -0.7152f;
		float num4 = 0.9278f;
		float num5 = 0f - num;
		float num6 = num;
		float[] array = new float[25];
		array[0] = 1f + num5 * num2;
		array[5] = num5 * num3;
		array[10] = num5 * num4;
		array[1] = num5 * num2;
		array[6] = 1f + num5 * num3;
		array[11] = num5 * num4;
		array[2] = num6 * num2;
		array[7] = num6 * num3;
		array[12] = 1f + num6 * num4;
		array[18] = 1f;
		array[24] = 1f;
		return array;
	}

	public static SaturationMeasurement Measure(SaturationMetric kind, int saturation)
	{
		double num = (double)saturation / 100.0;
		float[] m = Build(kind, (float)num, 1f);
		(float, float, float) tuple = Factors(kind, (float)num, 1f);
		double num2 = 0.0;
		for (int i = 0; i <= 255; i++)
		{
			double num3 = (double)i / 255.0;
			(double R, double G, double B) tuple2 = Transform(m, num3, num3, num3);
			double item = tuple2.R;
			double item2 = tuple2.G;
			double item3 = tuple2.B;
			num2 = Math.Max(num2, Math.Max(Math.Abs(item - num3), Math.Max(Math.Abs(item2 - num3), Math.Abs(item3 - num3))));
		}
		double num4 = 0.0;
		double num5 = 0.0;
		double num6 = 0.0;
		double num7 = 0.0;
		foreach (var item7 in SampleColours())
		{
			double item4 = item7.R;
			double item5 = item7.G;
			double item6 = item7.B;
			(double, double, double) tuple3 = Transform(m, item4, item5, item6);
			num7++;
			int num8;
			if (!(tuple3.Item1 < -1E-06) && !(tuple3.Item1 > 1.000001) && !(tuple3.Item2 < -1E-06) && !(tuple3.Item2 > 1.000001) && !(tuple3.Item3 < -1E-06))
			{
				num8 = ((tuple3.Item3 > 1.000001) ? 1 : 0);
				if (num8 == 0)
				{
					goto IL_019f;
				}
			}
			else
			{
				num8 = 1;
			}
			num6++;
			goto IL_019f;
			IL_019f:
			num4 = Math.Max(num4, Math.Abs(Luma(tuple3.Item1, tuple3.Item2, tuple3.Item3) - Luma(item4, item5, item6)));
			if (num8 == 0 && !(Chroma((R: item4, G: item5, B: item6)) < 0.02))
			{
				double num9 = Math.Abs(Hue(tuple3.Item1, tuple3.Item2, tuple3.Item3) - Hue(item4, item5, item6));
				if (num9 > 180.0)
				{
					num9 = 360.0 - num9;
				}
				num5 = Math.Max(num5, num9);
			}
		}
		double skinGain = Chroma(Clamp01(Transform(m, Skin.R, Skin.G, Skin.B))) / Chroma(Skin);
		double deliveredGain = Chroma(Transform(m, MidGreen.R, MidGreen.G, MidGreen.B)) / Chroma(MidGreen);
		return new SaturationMeasurement(kind, Info(kind).Name, saturation, num2, num4, 100.0 * num6 / num7, skinGain, num5, deliveredGain, tuple.Item1, tuple.Item2, tuple.Item3);
	}

	private static (double R, double G, double B) Clamp01((double R, double G, double B) c)
	{
		return (R: Math.Clamp(c.R, 0.0, 1.0), G: Math.Clamp(c.G, 0.0, 1.0), B: Math.Clamp(c.B, 0.0, 1.0));
	}

	private static (double R, double G, double B) Clamp01(double r, double g, double b)
	{
		return (R: Math.Clamp(r, 0.0, 1.0), G: Math.Clamp(g, 0.0, 1.0), B: Math.Clamp(b, 0.0, 1.0));
	}

	private static IEnumerable<(double R, double G, double B)> SampleColours()
	{
		for (int i = 0; i < 36; i++)
		{
			double h = (double)i * 10.0;
			yield return FromHue(h, 0.35, 0.7);
			yield return FromHue(h, 0.6, 0.62);
		}
		for (int i = 0; i < 12; i++)
		{
			yield return FromHue((double)i * 30.0, 0.9, 0.92);
		}
		for (int i = 0; i <= 255; i += 17)
		{
			double num = (double)i / 255.0;
			yield return (R: num, G: num, B: num);
		}
		yield return (R: 0.3, G: 0.55, B: 0.9);
		yield return (R: 0.3, G: 0.42, B: 0.18);
		yield return (R: 0.95, G: 0.8, B: 0.45);
		yield return (R: 0.85, G: 0.1, B: 0.1);
		yield return (R: 0.12, G: 0.35, B: 0.2);
		yield return (R: 0.55, G: 0.5, B: 0.45);
	}

	public static (double R, double G, double B) Transform(float[] m, double r, double g, double b)
	{
		return (R: r * (double)m[0] + g * (double)m[5] + b * (double)m[10] + (double)m[20], G: r * (double)m[1] + g * (double)m[6] + b * (double)m[11] + (double)m[21], B: r * (double)m[2] + g * (double)m[7] + b * (double)m[12] + (double)m[22]);
	}

	private static double Luma(double r, double g, double b)
	{
		return 0.2126 * r + 0.7152 * g + 0.0722 * b;
	}

	private static double Chroma((double R, double G, double B) c)
	{
		return Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B));
	}

	private static double Hue(double r, double g, double b)
	{
		double num = Math.Max(r, Math.Max(g, b));
		double num2 = Math.Min(r, Math.Min(g, b));
		double num3 = num - num2;
		if (num3 < 0.02)
		{
			return 0.0;
		}
		return (((num == r) ? ((g - b) / num3 % 6.0) : ((num == g) ? ((b - r) / num3 + 2.0) : ((r - g) / num3 + 4.0))) * 60.0 + 360.0) % 360.0;
	}

	private static (double R, double G, double B) FromHue(double hue, double saturation, double value)
	{
		double num = hue / 60.0;
		double num2 = 1.0 - Math.Abs(num % 2.0 - 1.0);
		(double, double, double) tuple = (int)num switch
		{
			0 => (1.0, num2, 0.0), 
			1 => (num2, 1.0, 0.0), 
			2 => (0.0, 1.0, num2), 
			3 => (0.0, num2, 1.0), 
			4 => (num2, 0.0, 1.0), 
			_ => (1.0, 0.0, num2), 
		};
		double num3 = 1.0 - saturation;
		return (R: value * (num3 + saturation * tuple.Item1), G: value * (num3 + saturation * tuple.Item2), B: value * (num3 + saturation * tuple.Item3));
	}
}
