using System;

namespace VibranceHud.Customize;

public static class WindowsRelease
{
	public const int FirstElevenBuild = 22000;

	public static bool IsElevenOrLater { get; } = IsElevenBuild(Environment.OSVersion.Version.Build);

	public static bool IsElevenBuild(int build)
	{
		return build >= 22000;
	}

	public static bool IsSupported(CustomizeTweak tweak)
	{
		return IsSupported(tweak, IsElevenOrLater);
	}

	public static bool IsSupported(CustomizeTweak tweak, bool elevenOrLater)
	{
		return UnsupportedReason(tweak, elevenOrLater) == null;
	}

	public static string? UnsupportedReason(CustomizeTweak tweak)
	{
		return UnsupportedReason(tweak, IsElevenOrLater);
	}

	public static string? UnsupportedReason(CustomizeTweak tweak, bool elevenOrLater)
	{
		switch (tweak)
		{
		case CustomizeTweak.TaskbarAlignment:
		case CustomizeTweak.WidgetsButton:
		case CustomizeTweak.StartLayout:
			return elevenOrLater ? null : "Windows 11 only — your Windows 10 has no such setting.";
		case CustomizeTweak.SmallTaskbarButtons:
			return elevenOrLater ? "Windows 10 only — Windows 11 removed this setting." : null;
		case CustomizeTweak.SearchBox:
		case CustomizeTweak.TaskViewButton:
		case CustomizeTweak.FileExtensions:
		case CustomizeTweak.HiddenFiles:
			return null;
		default:
			throw new ArgumentOutOfRangeException("tweak", tweak, "no support rule recorded for this tweak");
		}
	}
}
