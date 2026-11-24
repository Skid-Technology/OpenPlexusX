using System;

namespace VibranceHud.Crosshair;

public sealed class CrosshairConfig
{
	public const int MaxOffsetPx = 200;

	public string Name { get; set; } = "New crosshair";

	public CrosshairShape Shape { get; set; }

	public bool? ArmTop { get; set; }

	public bool? ArmBottom { get; set; }

	public bool? ArmLeft { get; set; }

	public bool? ArmRight { get; set; }

	public bool? ShowCircle { get; set; }

	public bool Diagonal { get; set; }

	public int? DotSizeTenths { get; set; }

	public int? CircleRadiusTenths { get; set; }

	public int Opacity { get; set; } = 100;

	private bool LegacyHasArms
	{
		get
		{
			CrosshairShape shape = Shape;
			if (shape == CrosshairShape.Cross || shape == CrosshairShape.T)
			{
				return true;
			}
			return false;
		}
	}

	public bool ResolvedArmTop => ArmFrom(ArmTop, Shape == CrosshairShape.Cross);

	public bool ResolvedArmBottom => ArmFrom(ArmBottom, LegacyHasArms);

	public bool ResolvedArmLeft => ArmFrom(ArmLeft, LegacyHasArms);

	public bool ResolvedArmRight => ArmFrom(ArmRight, LegacyHasArms);

	public bool ResolvedShowCircle => ShowCircle ?? (Shape == CrosshairShape.Circle);

	public bool ResolvedCentreDot
	{
		get
		{
			if (!CentreDot)
			{
				return Shape == CrosshairShape.Dot;
			}
			return true;
		}
	}

	public float ResolvedDotSize => (float)(DotSizeTenths ?? ((int)Math.Round(ResolvedThickness * 10f))) / 10f;

	public float ResolvedCircleRadius => (float)(CircleRadiusTenths ?? ((int)Math.Round((ResolvedGap + ResolvedSize) * 10f))) / 10f;

	public int ResolvedColourArgb
	{
		get
		{
			int num = (byte)((ColourArgb >> 24) & 0xFF) * Math.Clamp(Opacity, 0, 100) / 100;
			return (ColourArgb & 0xFFFFFF) | (num << 24);
		}
	}

	public int ColourArgb { get; set; } = -16711834;

	public int Size { get; set; } = 8;

	public int Thickness { get; set; } = 2;

	public int Gap { get; set; } = 4;

	public int? SizeTenths { get; set; }

	public int? ThicknessTenths { get; set; }

	public int? GapTenths { get; set; }

	public float ResolvedSize => (float)(SizeTenths ?? (Size * 10)) / 10f;

	public float ResolvedThickness => (float)(ThicknessTenths ?? (Thickness * 10)) / 10f;

	public float ResolvedGap => (float)(GapTenths ?? (Gap * 10)) / 10f;

	public bool Outline { get; set; } = true;

	public int? OutlineThicknessTenths { get; set; }

	public float ResolvedOutlineThickness
	{
		get
		{
			if (Outline)
			{
				return (float)(OutlineThicknessTenths ?? 10) / 10f;
			}
			return 0f;
		}
	}

	public int? OffsetXPx { get; set; }

	public int? OffsetYPx { get; set; }

	public int ResolvedOffsetX => Clamp(OffsetXPx);

	public int ResolvedOffsetY => Clamp(OffsetYPx);

	public bool CentreDot { get; set; }

	public bool Chevron { get; set; }

	public int? RotationDegrees { get; set; }

	public int ResolvedRotationDegrees => Math.Clamp(RotationDegrees ?? (Diagonal ? 45 : 0), 0, 90);

	public int? DotOpacity { get; set; }

	public int ResolvedDotOpacity => Math.Clamp(DotOpacity ?? 100, 10, 100);

	public int ResolvedDotColourArgb
	{
		get
		{
			int resolvedColourArgb = ResolvedColourArgb;
			int num = ((resolvedColourArgb >> 24) & 0xFF) * ResolvedDotOpacity / 100;
			return (resolvedColourArgb & 0xFFFFFF) | (num << 24);
		}
	}

	public int? OutlineColourRgb { get; set; }

	public int ResolvedOutlineColourArgb => -1107296256 | (OutlineColourRgb.GetValueOrDefault() & 0xFFFFFF);

	public string? ImagePath { get; set; }

	public int ImageScalePercent { get; set; } = 100;

	public bool UsesImage => ImageCrosshair.Load(ImagePath) != null;

	private bool ArmFrom(bool? saved, bool legacyDefault)
	{
		return saved ?? legacyDefault;
	}

	public void SetSizeTenths(int tenths)
	{
		SizeTenths = tenths;
		Size = LegacyPixels(tenths, 1);
	}

	public void SetThicknessTenths(int tenths)
	{
		ThicknessTenths = tenths;
		Thickness = LegacyPixels(tenths, 1);
	}

	public void SetGapTenths(int tenths)
	{
		GapTenths = tenths;
		Gap = LegacyPixels(tenths, 0);
	}

	private static int LegacyPixels(int tenths, int floor)
	{
		return Math.Max(floor, (int)Math.Round((double)tenths / 10.0, MidpointRounding.AwayFromZero));
	}

	private static int Clamp(int? value)
	{
		return Math.Clamp(value.GetValueOrDefault(), -200, 200);
	}

	public void SetRotationDegrees(int degrees)
	{
		int num = Math.Clamp(degrees, 0, 90);
		RotationDegrees = num;
		Diagonal = num > 22 && num < 68;
	}

	public CrosshairConfig Clone()
	{
		return (CrosshairConfig)MemberwiseClone();
	}
}
