using System;
using System.Collections.Generic;

namespace VibranceHud;

internal static class DisplayLabels
{
	internal static IReadOnlyList<string> For(IEnumerable<(string Adapter, string MonitorName, bool IsPrimary)> monitors)
	{
		List<string> list = new List<string>();
		foreach (var monitor in monitors)
		{
			string item = monitor.Adapter;
			string item2 = monitor.MonitorName;
			bool item3 = monitor.IsPrimary;
			string text = (string.IsNullOrWhiteSpace(item2) ? item : item2);
			text = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
			if (text.Contains("\\\\.\\", StringComparison.Ordinal))
			{
				text = "";
			}
			list.Add($"Display {list.Count + 1}" + ((text.Length > 0) ? (" · " + text) : "") + (item3 ? " (main)" : ""));
		}
		return list;
	}
}
