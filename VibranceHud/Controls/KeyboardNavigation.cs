using System;
using System.Windows.Forms;

namespace VibranceHud.Controls;

internal sealed class KeyboardNavigation : IMessageFilter
{
	[ThreadStatic]
	private static KeyboardNavigation? _installed;

	[ThreadStatic]
	private static bool _moving;

	private const int WM_KEYDOWN = 256;

	private const int WM_SYSKEYDOWN = 260;

	internal static bool InProgress => _moving;

	internal static void EnsureInstalled()
	{
		if (_installed == null)
		{
			_installed = new KeyboardNavigation();
			Application.AddMessageFilter(_installed);
		}
	}

	internal static bool Consume()
	{
		bool moving = _moving;
		_moving = false;
		return moving;
	}

	internal static bool IsNavigationKey(Keys key)
	{
		Keys keys = key & Keys.KeyCode;
		if (keys == Keys.Tab || (uint)(keys - 33) <= 7u)
		{
			return true;
		}
		return false;
	}

	public bool PreFilterMessage(ref Message m)
	{
		int msg = m.Msg;
		bool flag = ((msg == 256 || msg == 260) ? true : false);
		bool flag2 = flag;
		Keys keys = (flag2 ? ((Keys)((long)m.WParam & 0xFFFFL)) : Keys.None);
		_moving = flag2 && IsNavigationKey(keys);
		if (_moving && keys == Keys.Tab)
		{
			try
			{
				RovingFocus.RefreshAll();
			}
			catch (Exception)
			{
			}
		}
		return false;
	}
}
