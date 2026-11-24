using System;
using System.Runtime.InteropServices;

namespace VibranceHud.Music;

internal sealed class MusicHotkey : IDisposable
{
	private const int Id = 1;

	private readonly HotkeyWindow _window;

	private bool _disposed;

	internal static readonly HotkeySlot[] Slots = new HotkeySlot[4]
	{
		HotkeySlot.MusicMini,
		HotkeySlot.MusicPlayPause,
		HotkeySlot.MusicNext,
		HotkeySlot.MusicPrevious
	};

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UnregisterHotKey(nint hWnd, int id);

	private MusicHotkey(HotkeyWindow window)
	{
		_window = window;
	}

	internal static MusicHotkey? TryRegister(AppSettings settings, Action onPressed)
	{
		return TryRegister(settings, HotkeySlot.MusicMini, onPressed);
	}

	internal static (bool Enabled, uint Mask, uint Key) Binding(AppSettings s, HotkeySlot slot)
	{
		return slot switch
		{
			HotkeySlot.MusicMini => (Enabled: s.MusicHotkeyEnabled, Mask: s.MusicHotkeyModifierMask, Key: s.MusicHotkeyVirtualKey), 
			HotkeySlot.MusicPlayPause => (Enabled: s.MusicPlayPauseHotkeyEnabled, Mask: s.MusicPlayPauseHotkeyModifierMask, Key: s.MusicPlayPauseHotkeyVirtualKey), 
			HotkeySlot.MusicNext => (Enabled: s.MusicNextHotkeyEnabled, Mask: s.MusicNextHotkeyModifierMask, Key: s.MusicNextHotkeyVirtualKey), 
			HotkeySlot.MusicPrevious => (Enabled: s.MusicPreviousHotkeyEnabled, Mask: s.MusicPreviousHotkeyModifierMask, Key: s.MusicPreviousHotkeyVirtualKey), 
			_ => throw new ArgumentOutOfRangeException("slot"), 
		};
	}

	internal static void SetBinding(AppSettings s, HotkeySlot slot, bool enabled, uint mask, uint key)
	{
		switch (slot)
		{
		case HotkeySlot.MusicMini:
			s.MusicHotkeyEnabled = enabled;
			s.MusicHotkeyModifierMask = mask;
			s.MusicHotkeyVirtualKey = key;
			break;
		case HotkeySlot.MusicPlayPause:
			s.MusicPlayPauseHotkeyEnabled = enabled;
			s.MusicPlayPauseHotkeyModifierMask = mask;
			s.MusicPlayPauseHotkeyVirtualKey = key;
			break;
		case HotkeySlot.MusicNext:
			s.MusicNextHotkeyEnabled = enabled;
			s.MusicNextHotkeyModifierMask = mask;
			s.MusicNextHotkeyVirtualKey = key;
			break;
		case HotkeySlot.MusicPrevious:
			s.MusicPreviousHotkeyEnabled = enabled;
			s.MusicPreviousHotkeyModifierMask = mask;
			s.MusicPreviousHotkeyVirtualKey = key;
			break;
		default:
			throw new ArgumentOutOfRangeException("slot");
		}
	}

	internal static MusicHotkey? TryRegister(AppSettings settings, HotkeySlot slot, Action onPressed)
	{
		(bool, uint, uint) tuple = Binding(settings, slot);
		if (!tuple.Item1 || tuple.Item3 == 0)
		{
			return null;
		}
		if (Clashes(settings, slot, tuple.Item2, tuple.Item3))
		{
			return null;
		}
		HotkeyWindow hotkeyWindow = new HotkeyWindow();
		if (!RegisterHotKey(hotkeyWindow.Handle, 1, tuple.Item2 | 0x4000, tuple.Item3))
		{
			hotkeyWindow.DestroyHandle();
			return null;
		}
		hotkeyWindow.HotkeyPressed += (object? _, EventArgs e) =>
		{
			if (e is HotkeyEventArgs { HotkeyId: 1 })
			{
				onPressed();
			}
		};
		return new MusicHotkey(hotkeyWindow);
	}

	internal static bool ClashesWithAnAppKey(AppSettings s)
	{
		return Clashes(s, HotkeySlot.MusicMini, s.MusicHotkeyModifierMask, s.MusicHotkeyVirtualKey);
	}

	internal static bool Clashes(AppSettings s, HotkeySlot slot, uint mask, uint vk)
	{
		if (HotkeyConflicts.Owner(s, slot, mask, vk).HasValue)
		{
			return true;
		}
		foreach (PresetKeyBinding presetKeyBinding in s.PresetKeyBindings)
		{
			if (vk != 0 && presetKeyBinding.ModifierMask == mask && presetKeyBinding.VirtualKey == vk)
			{
				return true;
			}
		}
		return false;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		try
		{
			UnregisterHotKey(_window.Handle, 1);
		}
		catch
		{
		}
		try
		{
			_window.DestroyHandle();
		}
		catch
		{
		}
	}
}
