using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace VibranceHud.Controls;

internal sealed class RovingFocus
{
	private static readonly ConditionalWeakTable<Control, RovingFocus> ByItem = new ConditionalWeakTable<Control, RovingFocus>();

	[ThreadStatic]
	private static List<WeakReference<RovingFocus>>? _groups;

	private readonly Control[] _items;

	private readonly Func<Control, bool> _isSelected;

	private readonly Func<Control, bool> _arrowChooses;

	internal IReadOnlyList<Control> Items => _items;

	private RovingFocus(Control[] items, Func<Control, bool> isSelected, Func<Control, bool> arrowChooses)
	{
		_items = items;
		_isSelected = isSelected;
		_arrowChooses = arrowChooses;
	}

	internal static RovingFocus Attach(IEnumerable<Control> items, Func<Control, bool> isSelected, Func<Control, bool>? arrowChooses = null)
	{
		RovingFocus rovingFocus = new RovingFocus(items.ToArray(), isSelected, arrowChooses ?? ((Func<Control, bool>)((Control _) => true)));
		Control[] items2 = rovingFocus._items;
		foreach (Control key in items2)
		{
			ByItem.AddOrUpdate(key, rovingFocus);
		}
		(_groups ?? (_groups = new List<WeakReference<RovingFocus>>())).Add(new WeakReference<RovingFocus>(rovingFocus));
		rovingFocus.Refresh();
		return rovingFocus;
	}

	internal static RovingFocus? Of(Control item)
	{
		if (!ByItem.TryGetValue(item, out RovingFocus value))
		{
			return null;
		}
		return value;
	}

	private static bool Usable(Control c)
	{
		if (!c.IsDisposed && c.Enabled)
		{
			return c.Visible;
		}
		return false;
	}

	internal Control? Current()
	{
		Control control = null;
		Control control2 = null;
		Control[] items = _items;
		foreach (Control control3 in items)
		{
			if (Usable(control3))
			{
				if (_isSelected(control3))
				{
					return control3;
				}
				if (control == null)
				{
					control = control3;
				}
				if (control3.Focused)
				{
					control2 = control3;
				}
			}
		}
		return control2 ?? control;
	}

	internal void Refresh()
	{
		Control control = Current();
		if (control == null)
		{
			return;
		}
		Control[] items = _items;
		foreach (Control control2 in items)
		{
			if (!control2.IsDisposed)
			{
				control2.TabStop = control2 == control;
			}
		}
	}

	internal static void RefreshAll()
	{
		if (_groups == null)
		{
			return;
		}
		_groups.RemoveAll((WeakReference<RovingFocus> w) => !w.TryGetTarget(out RovingFocus target2) || target2._items.All((Control c) => c.IsDisposed));
		foreach (WeakReference<RovingFocus> group in _groups)
		{
			if (group.TryGetTarget(out var target))
			{
				target.Refresh();
			}
		}
	}

	internal Control? Target(Control from, Keys keyData)
	{
		if ((keyData & Keys.Modifiers) != 0)
		{
			return null;
		}
		int num = Array.IndexOf(_items, from);
		if (num < 0)
		{
			return null;
		}
		int num2 = _items.Length;
		switch (keyData & Keys.KeyCode)
		{
		case Keys.Left:
		case Keys.Up:
		{
			for (int j = 1; j <= num2; j++)
			{
				if (Usable(_items[(num - j + num2) % num2]))
				{
					return _items[(num - j + num2) % num2];
				}
			}
			return from;
		}
		case Keys.Right:
		case Keys.Down:
		{
			for (int i = 1; i <= num2; i++)
			{
				if (Usable(_items[(num + i) % num2]))
				{
					return _items[(num + i) % num2];
				}
			}
			return from;
		}
		case Keys.Home:
			return _items.FirstOrDefault(Usable) ?? from;
		case Keys.End:
			return _items.LastOrDefault(Usable) ?? from;
		default:
			return null;
		}
	}

	internal bool ArrowChooses(Control item)
	{
		return _arrowChooses(item);
	}
}
