using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibranceHud;

internal static class WindowDrag
{
	private const int WM_NCLBUTTONDOWN = 161;

	private const int HTCAPTION = 2;

	[DllImport("user32.dll")]
	private static extern bool ReleaseCapture();

	[DllImport("user32.dll")]
	private static extern nint SendMessage(nint hWnd, int msg, int wParam, int lParam);

	public static void Enable(Control source, Form form, Func<Point, bool>? except = null)
	{
		source.MouseDown += (object? _, MouseEventArgs e) =>
		{
			if (e.Button == MouseButtons.Left && (except == null || !except(e.Location)))
			{
				ReleaseCapture();
				SendMessage(form.Handle, 161, 2, 0);
			}
		};
	}
}
