using System;
using A;
using VibranceHud.Music;

namespace VibranceHud;

public static class HotkeyConflicts
{
	public static HotkeySlot? Owner(AppSettings settings, HotkeySlot target, uint mask, uint vk)
	{
		if (vk == 0)
		{
			return null;
		}
		HotkeySlot[] slots = MusicHotkey.Slots;
		foreach (HotkeySlot hotkeySlot in slots)
		{
			(bool, uint, uint) tuple = MusicHotkey.Binding(settings, hotkeySlot);
			if (target != hotkeySlot && tuple.Item1 && tuple.Item2 == mask && tuple.Item3 == vk)
			{
				return hotkeySlot;
			}
		}
		if (target != HotkeySlot.QuickVibrance && settings.HotkeyVirtualKey == vk && settings.HotkeyModifierMask == mask)
		{
			return HotkeySlot.QuickVibrance;
		}
		if (target != HotkeySlot.MainWindow && settings.MainHotkeyEnabled && settings.MainHotkeyVirtualKey == vk && settings.MainHotkeyModifierMask == mask)
		{
			return HotkeySlot.MainWindow;
		}
		if (target != HotkeySlot.Screenshot && settings.ScreenshotHotkeyEnabled && settings.ScreenshotHotkeyVirtualKey == vk && settings.ScreenshotHotkeyModifierMask == mask)
		{
			return HotkeySlot.Screenshot;
		}
		if (target != HotkeySlot.NightVision && settings.NightVisionHotkeyEnabled && global::A.A.Available && settings.NightVisionHotkeyVirtualKey == vk && settings.NightVisionHotkeyModifierMask == mask)
		{
			return HotkeySlot.NightVision;
		}
		if (target == HotkeySlot.Zoom && global::A.A.Available && settings.HoldsTheNextSetupKey(mask, vk))
		{
			return HotkeySlot.NightVision;
		}
		if (target != HotkeySlot.Zoom && settings.ZoomEnabled && global::A.A.Available && settings.ZoomHotkeyVirtualKey == vk && settings.ZoomHotkeyModifierMask == mask)
		{
			return HotkeySlot.Zoom;
		}
		return null;
	}

	public static HotkeySlot? DarkBoostKeyOwner(AppSettings settings, uint mask, uint vk)
	{
		HotkeySlot? hotkeySlot = Owner(settings, HotkeySlot.NightVision, mask, vk);
		if (hotkeySlot.HasValue)
		{
			HotkeySlot valueOrDefault = hotkeySlot.GetValueOrDefault();
			if (valueOrDefault != HotkeySlot.Zoom)
			{
				return valueOrDefault;
			}
		}
		return null;
	}

	public static HotkeySlot? DarkBoostRebindOwner(AppSettings settings, uint mask, uint vk)
	{
		if (!settings.NightVisionHotkeyEnabled || vk == 0 || vk != settings.NightVisionHotkeyVirtualKey || mask != settings.NightVisionHotkeyModifierMask)
		{
			return Owner(settings, HotkeySlot.NightVision, mask, vk);
		}
		return DarkBoostKeyOwner(settings, mask, vk);
	}

	public static string? BindingOwner(AppSettings settings, string presetName, uint mask, uint vk)
	{
		if (vk == 0)
		{
			return null;
		}
		HotkeySlot[] slots = MusicHotkey.Slots;
		foreach (HotkeySlot slot in slots)
		{
			(bool, uint, uint) tuple = MusicHotkey.Binding(settings, slot);
			if (tuple.Item1 && tuple.Item2 == mask && tuple.Item3 == vk)
			{
				return Describe(slot);
			}
		}
		if (settings.HotkeyVirtualKey == vk && settings.HotkeyModifierMask == mask)
		{
			return Describe(HotkeySlot.QuickVibrance);
		}
		if (settings.MainHotkeyEnabled && settings.MainHotkeyVirtualKey == vk && settings.MainHotkeyModifierMask == mask)
		{
			return Describe(HotkeySlot.MainWindow);
		}
		if (settings.ScreenshotHotkeyEnabled && settings.ScreenshotHotkeyVirtualKey == vk && settings.ScreenshotHotkeyModifierMask == mask)
		{
			return Describe(HotkeySlot.Screenshot);
		}
		if (settings.NightVisionHotkeyEnabled && global::A.A.Available && settings.NightVisionHotkeyVirtualKey == vk && settings.NightVisionHotkeyModifierMask == mask)
		{
			return Describe(HotkeySlot.NightVision);
		}
		if (global::A.A.Available && settings.HoldsTheNextSetupKey(mask, vk))
		{
			return Describe(HotkeySlot.NightVision);
		}
		if (settings.ZoomEnabled && global::A.A.Available && settings.ZoomHotkeyVirtualKey == vk && settings.ZoomHotkeyModifierMask == mask)
		{
			return Describe(HotkeySlot.Zoom);
		}
		foreach (PresetKeyBinding presetKeyBinding in settings.PresetKeyBindings)
		{
			if (!string.Equals(presetKeyBinding.Name, presetName, StringComparison.OrdinalIgnoreCase) && presetKeyBinding.VirtualKey == vk && presetKeyBinding.ModifierMask == mask)
			{
				return "the '" + presetKeyBinding.Name + "' preset";
			}
		}
		return null;
	}

	public static string Describe(HotkeySlot slot)
	{
		return slot switch
		{
			HotkeySlot.QuickVibrance => "Quick vibrance", 
			HotkeySlot.MainWindow => "Open main window", 
			HotkeySlot.Screenshot => "Screenshot with colours", 
			HotkeySlot.NightVision => "Dark Boost", 
			HotkeySlot.MusicMini => "Music mini player", 
			HotkeySlot.MusicPlayPause => "Play / pause", 
			HotkeySlot.MusicNext => "Next track", 
			HotkeySlot.MusicPrevious => "Previous track", 
			HotkeySlot.Zoom => "Hold to zoom", 
			_ => throw new ArgumentOutOfRangeException("slot", slot, "Every HotkeySlot needs a name the user can look for."), 
		};
	}
}
