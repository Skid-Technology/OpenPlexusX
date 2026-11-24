using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace VibranceHud;

public static class WindowBounds
{
	private const int MinVisibleWidth = 120;

	private const int MinVisibleHeight = 60;

	public static Rectangle ClampToVisible(Rectangle saved, IEnumerable<Rectangle> screens)
	{
		if (saved.Width <= 0 || saved.Height <= 0)
		{
			return Rectangle.Empty;
		}
		List<Rectangle> list = screens?.Where((Rectangle s) => s.Width > 0 && s.Height > 0).ToList() ?? new List<Rectangle>();
		if (list.Count == 0)
		{
			return Rectangle.Empty;
		}
		(Rectangle, Rectangle) tuple = (from s in list
			select (screen: s, overlap: Overlap(s, saved)) into x
			orderby (long)x.overlap.Width * (long)x.overlap.Height descending
			select x).First();
		int num;
		Rectangle rectangle;
		if (tuple.Item2.Width >= 120)
		{
			num = ((tuple.Item2.Height >= 60) ? 1 : 0);
			if (num != 0)
			{
				(rectangle, _) = tuple;
				goto IL_00eb;
			}
		}
		else
		{
			num = 0;
		}
		rectangle = list[0];
		goto IL_00eb;
		IL_00eb:
		Rectangle rectangle2 = rectangle;
		int num2 = Math.Min(saved.Width, rectangle2.Width);
		int num3 = Math.Min(saved.Height, rectangle2.Height);
		if (num != 0 && num2 == saved.Width && num3 == saved.Height)
		{
			return saved;
		}
		return new Rectangle(rectangle2.X + (rectangle2.Width - num2) / 2, rectangle2.Y + (rectangle2.Height - num3) / 2, num2, num3);
	}

	public static Size PinnedSize(Size design, Rectangle workingArea)
	{
		if (workingArea.Height <= 0)
		{
			return design;
		}
		return new Size(design.Width, Math.Min(design.Height, workingArea.Height));
	}

	private static Rectangle Overlap(Rectangle a, Rectangle b)
	{
		Rectangle result = a;
		result.Intersect(b);
		return result;
	}
}
