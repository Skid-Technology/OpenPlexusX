using System;
using System.Drawing;
using VibranceHud.Design;

namespace VibranceHud.Theming;

public static class SlatePreferences
{
	public readonly record struct Snapshot(int GuiOpacityPercent, bool MatteBlur, int MatteArgb);

	public const int NeverChosen = 0;

	public static int DefaultOpacityPercent => (int)Math.Round(66.0);

	public static Snapshot Defaults => new Snapshot(DefaultOpacityPercent, MatteBlur: true, BackgroundMatte.Default.ToArgb());

	public static void Apply(int guiOpacityPercent, bool matteBlur, int matteArgb)
	{
		SlateSurface.UserOpacity = ((guiOpacityPercent == 0) ? 0.66 : ((double)guiOpacityPercent / 100.0));
		SlateSurface.MatteBlur = matteBlur;
		AppBackground.SetMatte((matteArgb == 0) ? BackgroundMatte.Default : Color.FromArgb(matteArgb));
	}

	public static Snapshot Capture()
	{
		return new Snapshot((int)Math.Round(SlateSurface.UserOpacity * 100.0), SlateSurface.MatteBlur, AppBackground.Matte.ToArgb());
	}
}
