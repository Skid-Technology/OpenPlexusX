using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class GlassDropdown : Control
{
	private sealed class DropdownAccessibleObject : ControlAccessibleObject
	{
		private readonly GlassDropdown _owner;

		public override string? Value
		{
			get
			{
				return _owner.SelectedItem;
			}
			set
			{
			}
		}

		public override AccessibleStates State => (AccessibleStates)((int)base.State | (_owner._open ? 512 : 1024));

		public DropdownAccessibleObject(GlassDropdown owner)
			: base(owner)
		{
			_owner = owner;
		}
	}

	private readonly List<string> _items = new List<string>();

	private int _selectedIndex = -1;

	private bool _open;

	private readonly HoverFade _fade;

	private readonly OutsideFocusRing _ring;

	private long _closedAtTicks;

	internal static int LivePopupCount;

	private ToolStripDropDown? _menu;

	public string Placeholder { get; set; } = "Select...";

	public IReadOnlyList<string> Items => _items;

	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			int num = ((_items.Count == 0) ? (-1) : Math.Clamp(value, -1, _items.Count - 1));
			if (num != _selectedIndex)
			{
				_selectedIndex = num;
				Invalidate();
				if (IsHandleCreated)
				{
					AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
				}
				SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public string SelectedItem
	{
		get
		{
			if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
			{
				return "";
			}
			return _items[_selectedIndex];
		}
	}

	public event EventHandler? SelectedIndexChanged;

	public GlassDropdown()
	{
		_fade = new HoverFade(this);
		_ring = new OutsideFocusRing(this, () => Radius(ControlInk.Dpi(this)));
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Height = 32;
		Font = Fonts.Body;
		TabStop = true;
		SetStyle(ControlStyles.Selectable, value: true);
		AccessibleRole = AccessibleRole.ComboBox;
	}

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new DropdownAccessibleObject(this);
	}

	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		Invalidate();
	}

	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);
		Invalidate();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (!Enabled || _items.Count == 0)
		{
			return;
		}
		switch (e.KeyCode)
		{
		case Keys.Return:
		case Keys.Space:
		case Keys.F4:
			Open();
			e.Handled = true;
			break;
		case Keys.Down:
			if (e.Alt)
			{
				Open();
				e.Handled = true;
				break;
			}
			goto case Keys.Right;
		case Keys.Right:
			SelectedIndex = Math.Min(_selectedIndex + 1, _items.Count - 1);
			e.Handled = true;
			break;
		case Keys.Left:
		case Keys.Up:
			SelectedIndex = Math.Max(_selectedIndex - 1, 0);
			e.Handled = true;
			break;
		case Keys.Home:
			SelectedIndex = 0;
			e.Handled = true;
			break;
		case Keys.End:
			SelectedIndex = _items.Count - 1;
			e.Handled = true;
			break;
		case Keys.D0:
		case Keys.D1:
		case Keys.D2:
		case Keys.D3:
		case Keys.D4:
		case Keys.D5:
		case Keys.D6:
		case Keys.D7:
		case Keys.D8:
		case Keys.D9:
		case Keys.A:
		case Keys.B:
		case Keys.C:
		case Keys.D:
		case Keys.E:
		case Keys.F:
		case Keys.G:
		case Keys.H:
		case Keys.I:
		case Keys.J:
		case Keys.K:
		case Keys.L:
		case Keys.M:
		case Keys.N:
		case Keys.O:
		case Keys.P:
		case Keys.Q:
		case Keys.R:
		case Keys.S:
		case Keys.T:
		case Keys.U:
		case Keys.V:
		case Keys.W:
		case Keys.X:
		case Keys.Y:
		case Keys.Z:
			JumpToItemStartingWith((char)e.KeyValue);
			e.Handled = true;
			break;
		}
	}

	private void JumpToItemStartingWith(char key)
	{
		for (int i = 1; i <= _items.Count; i++)
		{
			int num = (((_selectedIndex < 0) ? (-1) : _selectedIndex) + i) % _items.Count;
			if (_items[num].Length > 0 && char.ToUpperInvariant(_items[num][0]) == char.ToUpperInvariant(key))
			{
				SelectedIndex = num;
				break;
			}
		}
	}

	protected override bool IsInputKey(Keys keyData)
	{
		Keys keys = keyData & ~Keys.Alt;
		if ((uint)(keys - 35) <= 5u || keys == Keys.F4)
		{
			return true;
		}
		return base.IsInputKey(keyData);
	}

	internal bool TestClaimsKey(Keys key)
	{
		return IsInputKey(key);
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	internal void TestSetHover(bool on)
	{
		_fade.Snap(on);
	}

	internal void TestSetOpen(bool on)
	{
		_open = on;
	}

	internal ToolStripDropDown BuildMenuForTest()
	{
		int rowH;
		return BuildMenu(out rowH);
	}

	public void SetItems(IEnumerable<string> items)
	{
		_items.Clear();
		_items.AddRange(items);
		_selectedIndex = ((_items.Count <= 0) ? (-1) : 0);
		Invalidate();
		SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		_fade.Set(hot: true);
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_fade.Set(hot: false);
		Invalidate();
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = (Enabled ? Cursors.Hand : Cursors.Default);
		_fade.Snap(hot: false);
		Invalidate();
	}

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		_ring.Rehook();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_ring.Release();
		}
		base.Dispose(disposing);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left && Environment.TickCount64 - _closedAtTicks >= 200)
		{
			Open();
		}
	}

	internal void TestCloseMenu()
	{
		_menu?.Close();
	}

	private ToolStripDropDown BuildMenu(out int rowH)
	{
		rowH = SignalMenu.RowHeight(ControlInk.Dpi(this), Font);
		ToolStripDropDown toolStripDropDown = SignalMenu.SelectList(this);
		int width = SignalMenu.RowWidth(this);
		for (int i = 0; i < _items.Count; i++)
		{
			int index = i;
			ToolStripMenuItem toolStripMenuItem = SignalMenu.SelectItem(_items[i], index == _selectedIndex, width, rowH);
			toolStripMenuItem.Font = Font;
			toolStripMenuItem.Click += (object? _, EventArgs _) =>
			{
				SelectedIndex = index;
			};
			toolStripDropDown.Items.Add(toolStripMenuItem);
		}
		return toolStripDropDown;
	}

	private void Open()
	{
		if (_items.Count == 0)
		{
			return;
		}
		ToolStripDropDown menu = BuildMenu(out var _);
		_menu = menu;
		LivePopupCount++;
		menu.Disposed += (object? _, EventArgs _) =>
		{
			LivePopupCount--;
		};
		menu.Closed += (object? _, ToolStripDropDownClosedEventArgs _) =>
		{
			_open = false;
			_closedAtTicks = Environment.TickCount64;
			if (_menu == menu)
			{
				_menu = null;
			}
			Invalidate();
			if (IsHandleCreated)
			{
				AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
			}
			BeginInvoke(menu.Dispose);
		};
		_open = true;
		Invalidate();
		if (IsHandleCreated)
		{
			AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
		}
		SignalMenu.ShowUnder(menu, this);
	}

	private static float Radius(int dpi)
	{
		return ControlInk.PxF(dpi, 4f);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		bool flag = _selectedIndex >= 0;
		SignalMenu.PaintTrigger(e.Graphics, ClientSize, ControlInk.Dpi(this), Font, flag ? SelectedItem : Placeholder, !flag, _open, _fade.Value, Enabled);
		if (Focused && ShowFocusCues)
		{
			_ring.Show(show: true);
		}
		else
		{
			_ring.Show(show: false);
		}
	}
}
