using System.Collections.Generic;

namespace VibranceHud.Crosshair;

public static class CrosshairShapeKinds
{
	public static IReadOnlyList<CrosshairShapeKind> All { get; } = new CrosshairShapeKind[5]
	{
		CrosshairShapeKind.Cross,
		CrosshairShapeKind.T,
		CrosshairShapeKind.Circle,
		CrosshairShapeKind.Dot,
		CrosshairShapeKind.Chevron
	};

	public static void Apply(CrosshairConfig config, CrosshairShapeKind kind)
	{
		config.CentreDot = config.ResolvedCentreDot;
		switch (kind)
		{
		case CrosshairShapeKind.Cross:
			SetParts(config, top: true, bottom: true, left: true, right: true, ring: false, chevron: false);
			config.Shape = CrosshairShape.Cross;
			break;
		case CrosshairShapeKind.T:
			SetParts(config, top: false, bottom: true, left: true, right: true, ring: false, chevron: false);
			config.Shape = CrosshairShape.T;
			break;
		case CrosshairShapeKind.Circle:
			SetParts(config, top: false, bottom: false, left: false, right: false, ring: true, chevron: false);
			config.Shape = CrosshairShape.Circle;
			break;
		case CrosshairShapeKind.Dot:
			SetParts(config, top: false, bottom: false, left: false, right: false, ring: false, chevron: false);
			config.CentreDot = true;
			config.Shape = CrosshairShape.Dot;
			break;
		case CrosshairShapeKind.Chevron:
			SetParts(config, top: false, bottom: false, left: true, right: true, ring: false, chevron: true);
			config.Shape = CrosshairShape.Cross;
			break;
		}
	}

	public static CrosshairShapeKind? Of(CrosshairConfig c)
	{
		bool resolvedArmTop = c.ResolvedArmTop;
		bool resolvedArmBottom = c.ResolvedArmBottom;
		bool resolvedArmLeft = c.ResolvedArmLeft;
		bool resolvedArmRight = c.ResolvedArmRight;
		bool resolvedShowCircle = c.ResolvedShowCircle;
		if (c.Chevron)
		{
			if (!(resolvedArmLeft & resolvedArmRight) || resolvedArmTop || resolvedArmBottom || resolvedShowCircle)
			{
				return null;
			}
			return CrosshairShapeKind.Chevron;
		}
		bool flag = !resolvedArmTop && !resolvedArmBottom && !resolvedArmLeft && !resolvedArmRight;
		if (resolvedShowCircle)
		{
			if (!flag)
			{
				return null;
			}
			return CrosshairShapeKind.Circle;
		}
		if (resolvedArmTop & resolvedArmBottom & resolvedArmLeft & resolvedArmRight)
		{
			return CrosshairShapeKind.Cross;
		}
		if (!resolvedArmTop & resolvedArmBottom & resolvedArmLeft & resolvedArmRight)
		{
			return CrosshairShapeKind.T;
		}
		if (flag && c.ResolvedCentreDot)
		{
			return CrosshairShapeKind.Dot;
		}
		return null;
	}

	private static void SetParts(CrosshairConfig c, bool top, bool bottom, bool left, bool right, bool ring, bool chevron)
	{
		c.ArmTop = top;
		c.ArmBottom = bottom;
		c.ArmLeft = left;
		c.ArmRight = right;
		c.ShowCircle = ring;
		c.Chevron = chevron;
	}
}
