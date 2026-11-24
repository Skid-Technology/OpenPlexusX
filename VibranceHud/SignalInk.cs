using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

internal static class SignalInk
{
	private const float BaselineRatio = 0.87f;

	private static bool Slate => Theme.CurrentName == "Slate";

	internal static Color Faint => Theme.TextFaint;

	internal static Color WindowEdge
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.WindowEdge;
			}
			return SignalTokens.Slate.WindowEdge;
		}
	}

	internal static Color AccentHover
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.AccentHover;
			}
			return SignalTokens.Slate.AccentHover;
		}
	}

	internal static Color PreviewWell
	{
		get
		{
			if (!Slate)
			{
				return SignalTokens.Ash.PreviewWell;
			}
			return SignalTokens.Slate.PreviewWell;
		}
	}

	internal static int DpiFor(Control c)
	{
		return Math.Max((c.DeviceDpi > 0) ? c.DeviceDpi : 96, Tokens.Dpi);
	}

	internal static int TopForBaseline(Graphics g, Font font, int boxTop, int boxHeight)
	{
		FontFamily fontFamily = font.FontFamily;
		FontStyle style = font.Style;
		float num = fontFamily.GetEmHeight(style);
		float num2 = fontFamily.GetLineSpacing(style);
		if (num <= 0f || num2 <= 0f)
		{
			return boxTop;
		}
		float num3 = font.GetHeight(g) * num / num2;
		float num4 = (float)fontFamily.GetCellAscent(style) / num * num3;
		return (int)Math.Round((float)boxTop + ((float)boxHeight - num3) / 2f + 0.87f * num3 - num4);
	}
}
