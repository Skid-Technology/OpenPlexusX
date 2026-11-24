using System;
using System.Windows.Forms;

namespace VibranceHud;

internal sealed class HotkeyWindow : NativeWindow
{
	private const int WM_HOTKEY = 786;

	internal const int WM_DRAIN_KEYS = 32848;

	public event EventHandler? HotkeyPressed;

	public event Action? KeysWaiting;

	public HotkeyWindow()
	{
		CreateHandle(new CreateParams());
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 786)
		{
			int id = ((IntPtr)m.WParam).ToInt32();
			HotkeyPressed?.Invoke(this, new HotkeyEventArgs(id));
		}
		else if (m.Msg == 32848)
		{
			try
			{
				KeysWaiting?.Invoke();
				return;
			}
			catch
			{
				return;
			}
		}
		base.WndProc(ref m);
	}
}
