using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Capabilities;

public static class HdrWords
{
	private const string Method = "PlexusX reads Windows' own advanced colour setting for each display";

	public static string DetectionLine(IReadOnlyList<HdrDisplay> displays, bool coloursLimited = true)
	{
		if (displays == null || displays.Count == 0)
		{
			return "PlexusX couldn't read HDR from Windows on this PC.";
		}
		List<HdrDisplay> list = displays.Where((HdrDisplay d) => d.Enabled).ToList();
		if (list.Count != 0)
		{
			if (coloursLimited)
			{
				return $"HDR is on for {Names(list)}, which is why screen-colour changes are limited there. {"PlexusX reads Windows' own advanced colour setting for each display"}.";
			}
			return $"HDR is on for {Names(list)}. {"PlexusX reads Windows' own advanced colour setting for each display"}.";
		}
		return $"HDR is off on {Names(displays)}. {"PlexusX reads Windows' own advanced colour setting for each display"}.";
	}

	private static string Names(IReadOnlyList<HdrDisplay> displays)
	{
		List<string> list = displays.Select((HdrDisplay d) => (!string.IsNullOrWhiteSpace(d.Device)) ? d.Device : "an unnamed display").ToList();
		if (list.Count == 1)
		{
			return list[0];
		}
		return string.Join(", ", list.Take(list.Count - 1)) + " and " + list[list.Count - 1];
	}
}
