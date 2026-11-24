using System;
using System.Drawing;

namespace VibranceHud.Design;

public static class SlateSurface
{
	public const double MinOpacity = 0.45;

	public const double MaxOpacity = 0.85;

	public const double ChromeCeiling = 0.95;

	public const double ChromeBoost = 0.08;

	private static double _userOpacity = 0.66;

	public static double UserOpacity
	{
		get
		{
			return _userOpacity;
		}
		set
		{
			_userOpacity = Math.Clamp(value, 0.45, 0.85);
		}
	}

	public static bool MatteBlur { get; set; } = true;

	public static bool Active => Theme.IsSeeThrough;

	public static Color Base => SignalTokens.Slate.SlateBase;

	public static double PanelOpacity => Math.Clamp(_userOpacity, 0.45, 0.85);

	public static double ChromeOpacity => Math.Min(0.95, PanelOpacity + 0.08);

	public static int PanelAlpha => Alpha(PanelOpacity);

	public static int ChromeAlpha => Alpha(ChromeOpacity);

	internal static int Alpha(double opacity)
	{
		return (int)Math.Round(Math.Clamp(opacity, 0.0, 1.0) * 255.0);
	}

	internal static void ResetForTests()
	{
		_userOpacity = 0.66;
		MatteBlur = true;
	}
}
