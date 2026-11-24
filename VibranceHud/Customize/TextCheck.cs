using System.Drawing;
using System.Globalization;
using VibranceHud.Design;
using VibranceHud.Theming;

namespace VibranceHud.Customize;

internal static class TextCheck
{
	internal static double Ratio()
	{
		if (!SlateSurface.Active)
		{
			return Readability.Contrast(Theme.TextFaint, Theme.Surface);
		}
		Color[] array = AppBackground.SampleBehind(AppBackground.WindowBoundsForSampling, SlateSurface.MatteBlur);
		Color ground = ((array.Length != 0) ? Readability.BrightestFivePercent(array) : Theme.Background);
		Color b = Readability.Over(SlateSurface.Base, SlateSurface.PanelAlpha, ground);
		return Readability.Contrast(SignalTokens.Slate.Text3, b);
	}

	internal static string Format(double ratio)
	{
		return ratio.ToString("0.0", CultureInfo.InvariantCulture) + " : 1";
	}
}
