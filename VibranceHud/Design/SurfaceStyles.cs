using System;

namespace VibranceHud.Design;

public static class SurfaceStyles
{
	public static string Name(SurfaceStyle style)
	{
		return style.ToString();
	}

	public static SurfaceStyle Parse(string? stored)
	{
		string text = (stored ?? "").Trim();
		if (text.Length == 0 || char.IsDigit(text[0]) || text[0] == '-')
		{
			return SurfaceStyle.Glass;
		}
		if (text.Contains(','))
		{
			return SurfaceStyle.Glass;
		}
		if (!Enum.TryParse<SurfaceStyle>(text, ignoreCase: true, out var result) || !Enum.IsDefined(result))
		{
			return SurfaceStyle.Glass;
		}
		return result;
	}
}
