using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Pages;

namespace VibranceHud;

internal sealed class OutsideFocusRing
{
	private readonly Control _owner;

	private readonly Func<float> _radius;

	private readonly Func<Rectangle>? _box;

	private readonly List<Control> _hosts = new List<Control>();

	private bool _shown;

	private Rectangle _band;

	internal bool Shown => _shown;

	internal IReadOnlyList<Control> HostsForTest => _hosts;

	public OutsideFocusRing(Control owner, Func<float> radius, Func<Rectangle>? box = null)
	{
		_owner = owner;
		_radius = radius;
		_box = box;
	}

	public void Rehook()
	{
		if (_shown)
		{
			Invalidate(_band);
			Hook();
			_band = RingBand();
			Invalidate(_band);
		}
	}

	public void Release()
	{
		Unhook();
		_shown = false;
	}

	public void Show(bool show)
	{
		if (show)
		{
			Rectangle rectangle = RingBand();
			if (!_shown || !(rectangle == _band))
			{
				if (_shown)
				{
					Invalidate(_band);
				}
				_shown = true;
				Hook();
				_band = rectangle;
				Invalidate(rectangle);
			}
		}
		else if (_shown)
		{
			Invalidate(_band);
			_shown = false;
			Unhook();
		}
	}

	private static Point Origin(Control c)
	{
		int num = 0;
		int num2 = 0;
		Control control = c;
		while (control != null && control.Parent != null)
		{
			num += control.Left;
			num2 += control.Top;
			control = control.Parent;
		}
		return new Point(num, num2);
	}

	private static Rectangle ClientInRoot(Control c)
	{
		return new Rectangle(Origin(c), c.ClientSize);
	}

	private Rectangle BoxInRoot()
	{
		Rectangle result = _box?.Invoke() ?? new Rectangle(Point.Empty, _owner.Size);
		Point pos = Origin(_owner);
		result.Offset(pos);
		return result;
	}

	private Rectangle RingBand()
	{
		int num = ControlInk.FocusRingReach(ControlInk.Dpi(_owner)) + 1;
		return Rectangle.Inflate(BoxInRoot(), num, num);
	}

	private void Hook()
	{
		Unhook();
		if (_owner.Parent == null)
		{
			return;
		}
		Rectangle rect = RingBand();
		Control control = _owner;
		Control parent = _owner.Parent;
		while (parent != null)
		{
			Add(parent);
			foreach (Control control2 in parent.Controls)
			{
				if (control2 != control && control2.Visible && ClientInRoot(control2).IntersectsWith(rect))
				{
					Add(control2);
				}
			}
			if (!(parent is Form) && !(parent is GlowPage) && parent.Parent != null && !ClientInRoot(parent).Contains(rect))
			{
				control = parent;
				parent = parent.Parent;
				continue;
			}
			break;
		}
	}

	private void Add(Control host)
	{
		if (!_hosts.Contains(host))
		{
			_hosts.Add(host);
			host.Paint += OnHostPaint;
		}
	}

	private void Unhook()
	{
		foreach (Control host in _hosts)
		{
			host.Paint -= OnHostPaint;
		}
		_hosts.Clear();
	}

	private void Invalidate(Rectangle band)
	{
		foreach (Control host in _hosts)
		{
			if (!host.IsDisposed && host.IsHandleCreated)
			{
				Rectangle rc = band;
				Point point = Origin(host);
				rc.Offset(-point.X, -point.Y);
				host.Invalidate(rc);
			}
		}
	}

	private bool WhollyShownBelow(Control host)
	{
		Rectangle rect = BoxInRoot();
		Control parent = _owner.Parent;
		while (parent != null && parent != host && !parent.Contains(host))
		{
			if (parent is ScrollableControl { AutoScroll: not false } && !ClientInRoot(parent).Contains(rect))
			{
				return false;
			}
			parent = parent.Parent;
		}
		return true;
	}

	private void OnHostPaint(object? sender, PaintEventArgs e)
	{
		if (_shown && sender is Control control && !_owner.IsDisposed && _owner.Visible && _owner.Parent != null && WhollyShownBelow(control))
		{
			Rectangle bounds = BoxInRoot();
			Point point = Origin(control);
			bounds.Offset(-point.X, -point.Y);
			ControlInk.PaintFocusRing(e.Graphics, bounds, _radius(), ControlInk.Dpi(_owner));
		}
	}
}
