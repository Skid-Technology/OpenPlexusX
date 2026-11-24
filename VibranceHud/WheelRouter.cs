using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Pages;

namespace VibranceHud;

public sealed class WheelRouter : IMessageFilter
{
	private const int WM_MOUSEWHEEL = 522;

	private static bool _installed;

	public static Func<GlowPage?>? CurrentPage { get; set; }

	[DllImport("user32.dll")]
	private static extern nint WindowFromPoint(Point point);

	public static void Install()
	{
		if (!_installed)
		{
			_installed = true;
			Application.AddMessageFilter(new WheelRouter());
		}
	}

	internal static ScrollableControl? ScrollTargetFor(Control? control, int delta)
	{
		GlowPage glowPage = null;
		for (Control control2 = control; control2 != null; control2 = control2.Parent)
		{
			if (control2 is GlowPage glowPage2 && glowPage == null)
			{
				glowPage = glowPage2;
			}
			if (control2 is ScrollableControl scrollableControl && HasRoom(scrollableControl, delta))
			{
				return scrollableControl;
			}
		}
		return glowPage;
	}

	private static bool HasRoom(ScrollableControl target, int delta)
	{
		if (!target.AutoScroll)
		{
			return false;
		}
		int num = target.DisplayRectangle.Height - target.ClientSize.Height;
		if (num <= 0)
		{
			return false;
		}
		int num2 = -target.AutoScrollPosition.Y;
		if (delta >= 0)
		{
			return num2 > 0;
		}
		return num2 < num;
	}

	private static void Scroll(ScrollableControl target, int delta)
	{
		if (!(target is GlowPage glowPage))
		{
			if (target is QuietScrollPanel quietScrollPanel)
			{
				quietScrollPanel.ScrollByWheel(delta);
			}
		}
		else
		{
			glowPage.ScrollByWheel(delta);
		}
	}

	public bool PreFilterMessage(ref Message m)
	{
		if (m.Msg != 522)
		{
			return false;
		}
		try
		{
			short x = (short)m.LParam;
			int y = (short)((long)m.LParam >> 16);
			int delta = (short)((long)m.WParam >> 16);
			nint num = WindowFromPoint(new Point(x, y));
			if (num == IntPtr.Zero)
			{
				return false;
			}
			Control control = Control.FromChildHandle(num);
			if (control == null || control.FindForm() == null)
			{
				return false;
			}
			ScrollableControl scrollableControl = ScrollTargetFor(control, delta);
			if (scrollableControl == null)
			{
				scrollableControl = CurrentPage?.Invoke();
			}
			if (scrollableControl == null)
			{
				return false;
			}
			Scroll(scrollableControl, delta);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
