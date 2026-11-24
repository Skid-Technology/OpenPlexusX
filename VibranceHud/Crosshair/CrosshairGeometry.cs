using System;
using System.Collections.Generic;
using System.Drawing;

namespace VibranceHud.Crosshair;

public static class CrosshairGeometry
{
	public const float ChevronDegrees = 45f;

	private const float AntiAliasMargin = 1f;

	public static CrosshairShapes Build(CrosshairConfig c)
	{
		float num = Math.Max(0.5f, c.ResolvedThickness);
		float num2 = Math.Max(0.5f, c.ResolvedSize);
		float num3 = Math.Max(0f, c.ResolvedGap);
		float num4 = num / 2f;
		List<RectangleF> list = new List<RectangleF>();
		List<AngledBar> list2 = new List<AngledBar>();
		RectangleF? circle = null;
		RectangleF rectangleF = new RectangleF(0f - (num3 + num2), 0f - num4, num2, num);
		RectangleF rectangleF2 = new RectangleF(num3, 0f - num4, num2, num);
		if (c.ResolvedArmLeft)
		{
			if (c.Chevron)
			{
				list2.Add(new AngledBar(rectangleF, -45f));
			}
			else
			{
				list.Add(rectangleF);
			}
		}
		if (c.ResolvedArmRight)
		{
			if (c.Chevron)
			{
				list2.Add(new AngledBar(rectangleF2, 45f));
			}
			else
			{
				list.Add(rectangleF2);
			}
		}
		if (c.ResolvedArmBottom)
		{
			list.Add(new RectangleF(0f - num4, num3, num, num2));
		}
		if (c.ResolvedArmTop)
		{
			list.Add(new RectangleF(0f - num4, 0f - (num3 + num2), num, num2));
		}
		if (c.Chevron && c.ResolvedArmLeft && c.ResolvedArmRight && num3 <= 0f)
		{
			list2.Add(new AngledBar(new RectangleF(0f - num4, 0f - num4, num, num), 45f));
		}
		if (c.ResolvedShowCircle)
		{
			float num5 = Math.Max(0.5f, c.ResolvedCircleRadius);
			circle = new RectangleF(0f - num5, 0f - num5, num5 * 2f, num5 * 2f);
		}
		int dotIndex = -1;
		if (c.ResolvedCentreDot)
		{
			float num6 = Math.Max(0.5f, c.ResolvedDotSize);
			list.Add(new RectangleF((0f - num6) / 2f, (0f - num6) / 2f, num6, num6));
			dotIndex = list.Count - 1;
		}
		int resolvedOffsetX = c.ResolvedOffsetX;
		int resolvedOffsetY = c.ResolvedOffsetY;
		if (resolvedOffsetX != 0 || resolvedOffsetY != 0)
		{
			for (int i = 0; i < list.Count; i++)
			{
				list[i] = Offset(list[i], resolvedOffsetX, resolvedOffsetY);
			}
			for (int j = 0; j < list2.Count; j++)
			{
				list2[j] = list2[j] with
				{
					Rect = Offset(list2[j].Rect, resolvedOffsetX, resolvedOffsetY)
				};
			}
			if (circle.HasValue)
			{
				RectangleF valueOrDefault = circle.GetValueOrDefault();
				circle = Offset(valueOrDefault, resolvedOffsetX, resolvedOffsetY);
			}
		}
		PointF pivot = new PointF(resolvedOffsetX, resolvedOffsetY);
		float num7 = c.ResolvedRotationDegrees;
		return new CrosshairShapes(list, circle, Bounds(list, circle, list2, pivot, 0f, 0f))
		{
			Angled = list2,
			DotIndex = dotIndex,
			RotationDegrees = num7,
			Pivot = pivot,
			DrawnBounds = Bounds(list, circle, list2, pivot, num7, 0f)
		};
	}

	public static Size OverlaySize(CrosshairConfig c)
	{
		Bitmap bitmap = ImageCrosshair.Load(c.ImagePath);
		if (bitmap != null)
		{
			float num = (float)Math.Max(1, c.ImageScalePercent) / 100f;
			float num2 = (float)bitmap.Width * num + (float)(Math.Abs(c.ResolvedOffsetX) * 2);
			float num3 = (float)bitmap.Height * num + (float)(Math.Abs(c.ResolvedOffsetY) * 2);
			float num4 = c.ResolvedOutlineThickness * 2f + 4f;
			return new Size(Math.Max(8, (int)Math.Ceiling(num2 + num4)), Math.Max(8, (int)Math.Ceiling(num3 + num4)));
		}
		CrosshairShapes crosshairShapes = Build(c);
		float num5 = crosshairShapes.Bounds.Width;
		float num6 = crosshairShapes.Bounds.Height;
		if (c.Diagonal)
		{
			num5 *= 1.4142f;
			num6 *= 1.4142f;
		}
		num5 += (float)(Math.Abs(c.ResolvedOffsetX) * 2);
		num6 += (float)(Math.Abs(c.ResolvedOffsetY) * 2);
		float num7 = c.ResolvedOutlineThickness * 2f + 4f;
		int val = (int)Math.Ceiling(num5 + num7);
		int val2 = (int)Math.Ceiling(num6 + num7);
		float resolvedOutlineThickness = c.ResolvedOutlineThickness;
		float ringGrow = ((!crosshairShapes.Circle.HasValue) ? 0f : (Math.Max(0.5f, c.ResolvedThickness) / 2f + resolvedOutlineThickness));
		RectangleF rectangleF = Bounds(crosshairShapes.Bars, crosshairShapes.Circle, crosshairShapes.Angled, crosshairShapes.Pivot, crosshairShapes.RotationDegrees, resolvedOutlineThickness, ringGrow);
		float num8 = Math.Max(Math.Abs(rectangleF.Left), Math.Abs(rectangleF.Right)) + 1f;
		float num9 = Math.Max(Math.Abs(rectangleF.Top), Math.Abs(rectangleF.Bottom)) + 1f;
		return new Size(Math.Max(8, Math.Max(val, (int)Math.Ceiling(num8 * 2f))), Math.Max(8, Math.Max(val2, (int)Math.Ceiling(num9 * 2f))));
	}

	private static RectangleF Offset(RectangleF r, float dx, float dy)
	{
		return new RectangleF(r.X + dx, r.Y + dy, r.Width, r.Height);
	}

	private static RectangleF Bounds(IReadOnlyList<RectangleF> bars, RectangleF? circle, IReadOnlyList<AngledBar> angled, PointF pivot, float rotation, float grow, float ringGrow = 0f)
	{
		if (bars.Count == 0 && !circle.HasValue && angled.Count == 0)
		{
			return RectangleF.Empty;
		}
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		foreach (RectangleF bar in bars)
		{
			Take(bar, rotation);
		}
		foreach (AngledBar item3 in angled)
		{
			Take(item3.Rect, rotation + item3.Degrees);
		}
		if (circle.HasValue)
		{
			RectangleF valueOrDefault = circle.GetValueOrDefault();
			RectangleF rectangleF = ((ringGrow > 0f) ? RectangleF.Inflate(valueOrDefault, ringGrow, ringGrow) : valueOrDefault);
			TakePoint(rectangleF.Left, rectangleF.Top);
			TakePoint(rectangleF.Right, rectangleF.Bottom);
		}
		return new RectangleF(minX, minY, maxX - minX, maxY - minY);
		void Take(RectangleF r, float degrees)
		{
			RectangleF rectangleF2 = ((grow > 0f) ? RectangleF.Inflate(r, grow, grow) : r);
			if (degrees == 0f)
			{
				TakePoint(rectangleF2.Left, rectangleF2.Top);
				TakePoint(rectangleF2.Right, rectangleF2.Bottom);
			}
			else
			{
				double num = (double)degrees * Math.PI / 180.0;
				double num2 = Math.Cos(num);
				double num3 = Math.Sin(num);
				(float, float)[] array = new (float, float)[4]
				{
					(rectangleF2.Left, rectangleF2.Top),
					(rectangleF2.Right, rectangleF2.Top),
					(rectangleF2.Right, rectangleF2.Bottom),
					(rectangleF2.Left, rectangleF2.Bottom)
				};
				for (int i = 0; i < array.Length; i++)
				{
					(float, float) tuple = array[i];
					float item = tuple.Item1;
					float item2 = tuple.Item2;
					double num4 = item - pivot.X;
					double num5 = item2 - pivot.Y;
					TakePoint((float)((double)pivot.X + num4 * num2 - num5 * num3), (float)((double)pivot.Y + num4 * num3 + num5 * num2));
				}
			}
		}
		void TakePoint(float x, float y)
		{
			minX = Math.Min(minX, x);
			minY = Math.Min(minY, y);
			maxX = Math.Max(maxX, x);
			maxY = Math.Max(maxY, y);
		}
	}
}
