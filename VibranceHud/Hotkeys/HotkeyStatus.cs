using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Hotkeys;

public static class HotkeyStatus
{
	private static readonly object Gate = new object();

	private static readonly Dictionary<HotkeySlot, string> Refused = new Dictionary<HotkeySlot, string>();

	public static void Record(HotkeySlot slot, string combo, bool bound)
	{
		lock (Gate)
		{
			if (bound)
			{
				Refused.Remove(slot);
			}
			else
			{
				Refused[slot] = combo;
			}
		}
	}

	public static void Forget(HotkeySlot slot)
	{
		lock (Gate)
		{
			Refused.Remove(slot);
		}
	}

	public static string Line()
	{
		List<(HotkeySlot, string)> refused;
		lock (Gate)
		{
			refused = (from p in Refused
				orderby p.Key
				select (Key: p.Key, Value: p.Value)).ToList();
		}
		return Line(refused);
	}

	public static string Line(IReadOnlyList<(HotkeySlot Slot, string Combo)> refused)
	{
		if (refused == null || refused.Count == 0)
		{
			return "";
		}
		if (refused.Count == 1)
		{
			return $"Another app owns {refused[0].Combo}, so “{DisplayPageName(refused[0].Slot)}” does nothing. " + "Pick a different combination on the Display tab.";
		}
		List<string> list = refused.Select(((HotkeySlot Slot, string Combo) r) => r.Combo + " (“" + DisplayPageName(r.Slot) + "”)").ToList();
		string text = string.Join(", ", list.Take(list.Count - 1)) + " and " + list[list.Count - 1];
		return "Another app owns " + text + ", so they do nothing. Pick different combinations on the Display tab.";
	}

	private static string DisplayPageName(HotkeySlot slot)
	{
		if (slot != HotkeySlot.QuickVibrance)
		{
			return HotkeyConflicts.Describe(slot);
		}
		return "Quick colour popup";
	}

	internal static void ResetForTest()
	{
		lock (Gate)
		{
			Refused.Clear();
		}
	}
}
