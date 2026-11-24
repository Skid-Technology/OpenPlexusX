using System.Collections.Generic;
using VibranceHud.Crosshair;

namespace VibranceHud.Licensing;

public static class ServedCrosshairs
{
	private static readonly IReadOnlyList<CrosshairGallery.GalleryItem> BuiltInDesigns = BuildDesigns();

	public static IReadOnlyList<CrosshairGallery.GalleryItem> Designs => BuiltInDesigns;

	public static ServedSource Source => ServedSource.Served;

	public static bool OnTheirWay => false;

	public static int Generation => 1;

	internal static void Install()
	{
	}

	private static IReadOnlyList<CrosshairGallery.GalleryItem> BuildDesigns()
	{
		return new CrosshairGallery.GalleryItem[]
		{
			Make("dot-fine", "Fine Dot", CrosshairGallery.Family.Dot, size: 10, thick: 10, gap: 0, top: false, bottom: false, left: false, right: false, diag: false, outline: true, dot: 20, circle: null),
			Make("dot-bold", "Bold Dot", CrosshairGallery.Family.Dot, size: 10, thick: 10, gap: 0, top: false, bottom: false, left: false, right: false, diag: false, outline: true, dot: 35, circle: null),
			Make("cross-classic", "Classic Cross", CrosshairGallery.Family.Cross, size: 55, thick: 15, gap: 25, top: true, bottom: true, left: true, right: true, diag: false, outline: true, dot: null, circle: null),
			Make("cross-tight", "Tight Pro", CrosshairGallery.Family.Cross, size: 35, thick: 15, gap: 15, top: true, bottom: true, left: true, right: true, diag: false, outline: true, dot: null, circle: null),
			Make("cross-t", "Open T", CrosshairGallery.Family.Cross, size: 50, thick: 15, gap: 20, top: false, bottom: true, left: true, right: true, diag: false, outline: true, dot: null, circle: null),
			Make("cross-dot", "Cross + Dot", CrosshairGallery.Family.Cross, size: 45, thick: 15, gap: 30, top: true, bottom: true, left: true, right: true, diag: false, outline: true, dot: 18, circle: null),
			Make("x-clean", "Clean X", CrosshairGallery.Family.X, size: 45, thick: 15, gap: 20, top: true, bottom: true, left: true, right: true, diag: true, outline: true, dot: null, circle: null),
			Make("x-dot", "X + Dot", CrosshairGallery.Family.X, size: 40, thick: 15, gap: 25, top: true, bottom: true, left: true, right: true, diag: true, outline: true, dot: 20, circle: null),
			Make("ring-clean", "Clean Ring", CrosshairGallery.Family.Ring, size: 10, thick: 15, gap: 0, top: false, bottom: false, left: false, right: false, diag: false, outline: true, dot: 18, circle: 60),
			Make("ring-holo", "Holo Ring", CrosshairGallery.Family.Ring, size: 30, thick: 15, gap: 65, top: true, bottom: true, left: true, right: true, diag: false, outline: true, dot: 16, circle: 75),
			Make("funny-wide", "Wide Scope", CrosshairGallery.Family.Funny, size: 90, thick: 20, gap: 45, top: true, bottom: true, left: true, right: true, diag: false, outline: true, dot: 25, circle: 90)
		};
	}

	private static CrosshairGallery.GalleryItem Make(
		string id, string name, CrosshairGallery.Family family,
		int size, int thick, int gap,
		bool top, bool bottom, bool left, bool right,
		bool diag, bool outline, int? dot, int? circle)
	{
		CrosshairConfig c = new CrosshairConfig
		{
			ColourArgb = -1,
			Outline = outline,
			Opacity = 100,
			ArmTop = top,
			ArmBottom = bottom,
			ArmLeft = left,
			ArmRight = right,
			CentreDot = dot.HasValue,
			DotSizeTenths = dot,
			ShowCircle = circle.HasValue,
			CircleRadiusTenths = circle,
			Diagonal = diag
		};
		c.SetSizeTenths(size);
		c.SetThicknessTenths(thick);
		c.SetGapTenths(gap);
		return new CrosshairGallery.GalleryItem(id, name, family, c);
	}
}
