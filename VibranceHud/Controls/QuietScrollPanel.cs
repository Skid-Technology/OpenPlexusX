using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibranceHud.Controls;

public sealed class QuietScrollPanel : Panel
{
	private const int SB_BOTH = 3;

	[DllImport("user32.dll")]
	private static extern bool ShowScrollBar(nint hWnd, int wBar, bool bShow);

	public QuietScrollPanel()
	{
		AutoScroll = true;
		BackColor = Color.Transparent;
	}

	internal void ScrollByWheel(int delta)
	{
		OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, 0, 0, delta));
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		int num = DisplayRectangle.Height - ClientSize.Height;
		if (AutoScroll && num > 0)
		{
			int num2 = SystemInformation.MouseWheelScrollLines;
			if (num2 <= 0)
			{
				num2 = 3;
			}
			int num3 = e.Delta * num2 * 22 / 120;
			int y = Math.Clamp(-AutoScrollPosition.Y - num3, 0, num);
			AutoScrollPosition = new Point(-AutoScrollPosition.X, y);
			if (e is HandledMouseEventArgs e2)
			{
				e2.Handled = true;
			}
		}
		else
		{
			base.OnMouseWheel(e);
		}
		Invalidate(invalidateChildren: true);
	}

	internal void TestScrollWheel(int delta)
	{
		OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, 0, 0, delta));
	}

	protected override Point ScrollToControl(Control activeControl)
	{
		return DisplayRectangle.Location;
	}

	private void ForwardWheel(object? sender, MouseEventArgs e)
	{
		OnMouseWheel(e);
	}

	private void Hook(Control root)
	{
		foreach (Control control in root.Controls)
		{
			control.MouseWheel -= ForwardWheel;
			control.MouseWheel += ForwardWheel;
			control.ControlAdded -= OnDescendantAdded;
			control.ControlAdded += OnDescendantAdded;
			Hook(control);
		}
	}

	private void OnDescendantAdded(object? sender, ControlEventArgs e)
	{
		Control control = e.Control;
		if (control != null)
		{
			control.MouseWheel -= ForwardWheel;
			control.MouseWheel += ForwardWheel;
			Hook(control);
		}
	}

	protected override void OnControlAdded(ControlEventArgs e)
	{
		base.OnControlAdded(e);
		OnDescendantAdded(this, e);
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		Hook(this);
		HideBars();
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		base.OnLayout(e);
		HideBars();
	}

	protected override void WndProc(ref Message m)
	{
		base.WndProc(ref m);
		int msg = m.Msg;
		if ((msg == 131 || (uint)(msg - 276) <= 1u) ? true : false)
		{
			HideBars();
		}
	}

	private void HideBars()
	{
		if (IsHandleCreated && !IsDisposed)
		{
			ShowScrollBar(Handle, 3, bShow: false);
		}
	}
}
