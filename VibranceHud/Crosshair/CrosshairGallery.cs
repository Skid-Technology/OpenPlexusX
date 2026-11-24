using System;
using System.Collections.Generic;
using System.Linq;
using VibranceHud.Licensing;

namespace VibranceHud.Crosshair;

public static class CrosshairGallery
{
	public enum Family
	{
		Dot,
		X,
		Ring,
		Cross,
		Funny
	}

	public sealed record GalleryItem(string Id, string Name, Family Group, CrosshairConfig Config);

	public static IReadOnlyList<GalleryItem> All => ServedCrosshairs.Designs;

	public static void Apply(CrosshairConfig target, GalleryItem item)
	{
		CrosshairConfig config = item.Config;
		target.ArmTop = config.ResolvedArmTop;
		target.ArmBottom = config.ResolvedArmBottom;
		target.ArmLeft = config.ResolvedArmLeft;
		target.ArmRight = config.ResolvedArmRight;
		target.CentreDot = config.ResolvedCentreDot;
		target.DotSizeTenths = config.DotSizeTenths;
		target.ShowCircle = config.ResolvedShowCircle;
		target.CircleRadiusTenths = config.CircleRadiusTenths;
		target.Diagonal = config.Diagonal;
		target.RotationDegrees = config.RotationDegrees;
		target.Chevron = config.Chevron;
		target.SetSizeTenths(config.SizeTenths ?? 80);
		target.SetThicknessTenths(config.ThicknessTenths ?? 20);
		target.SetGapTenths(config.GapTenths ?? 40);
		bool flag = !config.ResolvedArmTop && !config.ResolvedArmBottom && !config.ResolvedArmLeft && !config.ResolvedArmRight;
		target.Shape = ((config.ResolvedShowCircle && !config.ResolvedArmLeft) ? CrosshairShape.Circle : (flag ? CrosshairShape.Dot : ((!config.ResolvedArmTop) ? CrosshairShape.T : CrosshairShape.Cross)));
	}

	public static GalleryItem? Matching(CrosshairConfig c)
	{
		return All.FirstOrDefault((GalleryItem item) => item.Config.ResolvedArmTop == c.ResolvedArmTop && item.Config.ResolvedArmBottom == c.ResolvedArmBottom && item.Config.ResolvedArmLeft == c.ResolvedArmLeft && item.Config.ResolvedArmRight == c.ResolvedArmRight && item.Config.ResolvedCentreDot == c.ResolvedCentreDot && item.Config.ResolvedShowCircle == c.ResolvedShowCircle && item.Config.ResolvedRotationDegrees == c.ResolvedRotationDegrees && item.Config.Chevron == c.Chevron && Near(item.Config.ResolvedSize, c.ResolvedSize) && Near(item.Config.ResolvedThickness, c.ResolvedThickness) && Near(item.Config.ResolvedGap, c.ResolvedGap) && (!c.ResolvedCentreDot || Near(item.Config.ResolvedDotSize, c.ResolvedDotSize)) && (!c.ResolvedShowCircle || Near(item.Config.ResolvedCircleRadius, c.ResolvedCircleRadius)));
	}

	private static bool Near(float a, float b)
	{
		return Math.Abs(a - b) < 0.05f;
	}
}
