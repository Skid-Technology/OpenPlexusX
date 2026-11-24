using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibranceHud.Music;

internal sealed class WindowsMediaKeys : IMediaKeys
{
	private const int WM_APPCOMMAND = 793;

	private const int APPCOMMAND_MEDIA_NEXTTRACK = 11;

	private const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 12;

	private const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;

	private NativeWindow? _window;

	[DllImport("user32.dll")]
	private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

	internal static int AppCommandFor(MediaKey key)
	{
		return key switch
		{
			MediaKey.Next => 11, 
			MediaKey.Previous => 12, 
			_ => 14, 
		};
	}

	public void Send(MediaKey key)
	{
		if (_window == null)
		{
			_window = new NativeWindow();
			_window.CreateHandle(new CreateParams());
		}
		SendMessage(_window.Handle, 793, _window.Handle, AppCommandFor(key) << 16);
	}
}
