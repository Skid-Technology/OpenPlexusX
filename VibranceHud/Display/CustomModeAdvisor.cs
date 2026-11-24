using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Display;

public static class CustomModeAdvisor
{
	public static string PanelLine(PanelSize panel)
	{
		if (!panel.Known)
		{
			return "Windows didn't report a size for this monitor.";
		}
		return $"Your monitor is {panel.Width} x {panel.Height}.";
	}

	public static CustomModeAdvice Advise(string widthText, string heightText, string rateText, IReadOnlyList<DisplayMode> reported, bool canRegister, string vendorHelp)
	{
		PanelSize panel = CustomModePanel.Native(reported);
		if (!TryRead(widthText, out var value) || !TryRead(heightText, out var value2))
		{
			return new CustomModeAdvice(CustomModeStanding.NothingTyped, Hint(canRegister), 0, 0, 0);
		}
		int result = 0;
		string text = (rateText ?? "").Trim();
		if (text.Length > 0 && (!int.TryParse(text, out result) || result <= 0))
		{
			return new CustomModeAdvice(CustomModeStanding.RefreshTooHigh, "Refresh rate should be a plain number, like 144 - or leave the box blank.", value, value2, 0);
		}
		switch (CustomModeLimits.Judge(value, value2, result, reported))
		{
		case CustomModeVerdict.TooSmall:
			return new CustomModeAdvice(CustomModeStanding.TooSmall, $"{value} x {value2} is too small to run a desktop in - try something like 1440 and 1080.", value, value2, result);
		case CustomModeVerdict.RefreshOutOfRange:
			return new CustomModeAdvice(CustomModeStanding.RefreshTooHigh, TooFast(result, reported), value, value2, result);
		case CustomModeVerdict.BiggerThanThePanel:
			return new CustomModeAdvice(CustomModeStanding.Impossible, CannotBeShown(value, value2, panel), value, value2, result);
		default:
			if (DisplayModes.IsSupported(reported, value, value2))
			{
				return new CustomModeAdvice(CustomModeStanding.AlreadyListed, $"{value} x {value2} is on your monitor's list - press Apply to switch to it.", value, value2, result);
			}
			if (canRegister)
			{
				return new CustomModeAdvice(CustomModeStanding.NeedsRegistering, $"Windows doesn't list {value} x {value2}, but your monitor can show it - press Apply " + "and PlexusX adds it to your graphics driver.", value, value2, result);
			}
			return new CustomModeAdvice(CustomModeStanding.CannotRegister, Join($"Windows doesn't list {value} x {value2} for this monitor, so PlexusX won't force it - " + "that is how people end up on a black screen.", vendorHelp, "Then come back and press Apply."), value, value2, result);
		}
	}

	public static string CannotBeShown(int w, int h, PanelSize panel)
	{
		if (panel.Known)
		{
			bool flag = w > panel.Width;
			bool flag2 = h > panel.Height;
			string value;
			if (flag & flag2)
			{
				value = $"Your monitor is {panel.Width} x {panel.Height}, so {w} x {h} cannot be shown.";
			}
			else
			{
				value = (flag2 ? $"Your monitor is {panel.Height} tall, so {h} tall cannot be shown." : $"Your monitor is {panel.Width} wide, so {w} wide cannot be shown.");
			}
			DisplayMode displayMode = CustomModePanel.FitInside(w, h, panel);
			return $"{value} The closest that fits is {displayMode.Width} x {displayMode.Height}.";
		}
		return $"{w} x {h} is bigger than any monitor - nothing could show it.";
	}

	private static string TooFast(int hz, IReadOnlyList<DisplayMode> reported)
	{
		int num = ((reported.Count != 0) ? reported.Max((DisplayMode m) => m.RefreshHz) : 0);
		if (num <= 0)
		{
			return $"{hz} Hz is faster than any monitor runs - leave the box blank for its own rate.";
		}
		return $"{hz} Hz is faster than this monitor goes - it tops out at {num} Hz. " + "Leave the box blank for its own rate.";
	}

	private static string Hint(bool canRegister)
	{
		if (!canRegister)
		{
			return "Anything your monitor reports. If it refuses one, nothing changes.";
		}
		return "Type any size that fits - if Windows doesn't list it, PlexusX adds it to your graphics driver.";
	}

	private static string Join(params string[] parts)
	{
		return string.Join(" ", from p in parts
			where !string.IsNullOrWhiteSpace(p)
			select p.Trim());
	}

	private static bool TryRead(string? text, out int value)
	{
		value = 0;
		string text2 = (text ?? "").Trim();
		if (text2.Length > 0 && int.TryParse(text2, out value))
		{
			return value > 0;
		}
		return false;
	}
}
