using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Games;

namespace VibranceHud;

public sealed class GameChooser : Control
{
	private readonly GameSelection _selection;

	private bool _open;

	private readonly HoverFade _fade;

	private readonly OutsideFocusRing _ring;

	private readonly SignalToolTip _tip = new SignalToolTip();

	private long _closedAtTicks;

	internal const int TriggerHeight = 32;

	private readonly EventHandler _onSelectionChanged;

	public GameChooser(GameSelection selection)
	{
		_selection = selection;
		_fade = new HoverFade(this);
		_ring = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 4f));
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		RefreshAvailability();
		Height = Tokens.Scale(32);
		Font = Fonts.Body;
		TabStop = true;
		AccessibleRole = AccessibleRole.ComboBox;
		AccessibleName = "Game";
		_onSelectionChanged = (object? _, EventArgs _) =>
		{
			Invalidate();
		};
		_selection.Changed += _onSelectionChanged;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_selection.Changed -= _onSelectionChanged;
			_tip.Dispose();
			_ring.Release();
		}
		base.Dispose(disposing);
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

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		_ring.Rehook();
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

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left && Environment.TickCount64 - _closedAtTicks >= 200)
		{
			Open();
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Space || e.KeyCode == Keys.Down || e.KeyCode == Keys.F4)
		{
			Open();
			e.Handled = true;
		}
	}

	private void Open()
	{
		if (!_selection.NothingInstalled)
		{
			ToolStripDropDown menu = BuildMenu();
			menu.Closed += (object? _, ToolStripDropDownClosedEventArgs _) =>
			{
				_open = false;
				_closedAtTicks = Environment.TickCount64;
				Invalidate();
				BeginInvoke(menu.Dispose);
			};
			_open = true;
			Invalidate();
			SignalMenu.ShowUnder(menu, this);
		}
	}

	internal ToolStripDropDown BuildMenuForTest()
	{
		return BuildMenu();
	}

	private ToolStripDropDown BuildMenu()
	{
		ToolStripDropDown toolStripDropDown = SignalMenu.SelectList(this);
		int dpi = ControlInk.Dpi(this);
		int width = SignalMenu.RowWidth(this);
		int rowH = SignalMenu.RowHeight(dpi, Fonts.Body);
		foreach (DetectedGame item in _selection.Installed)
		{
			SupportedGame game = item.Game;
			toolStripDropDown.Items.Add(Item(game.DisplayName, game.Id, _selection.CurrentId == game.Id, width, rowH));
		}
		toolStripDropDown.Items.Add(SignalMenu.Separator(width, dpi));
		toolStripDropDown.Items.Add(Item("Desktop", null, _selection.CurrentId == null, width, rowH));
		return toolStripDropDown;
	}

	private ToolStripMenuItem Item(string text, string? id, bool chosen, int width, int rowH)
	{
		ToolStripMenuItem toolStripMenuItem = SignalMenu.SelectItem(text, chosen, width, rowH);
		toolStripMenuItem.Click += (object? _, EventArgs _) =>
		{
			_selection.Select(id);
		};
		return toolStripMenuItem;
	}

	private void RefreshAvailability()
	{
		bool nothingInstalled = _selection.NothingInstalled;
		Cursor = (nothingInstalled ? Cursors.Default : Cursors.Hand);
		string text = (nothingInstalled ? "No supported game found on this PC. PlexusX finds supported games by itself once they are installed." : "");
		if (_tip.GetToolTip(this) != text)
		{
			_tip.SetToolTip(this, text);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		RefreshAvailability();
		string text;
		bool placeholder;
		if (!_selection.NothingInstalled)
		{
			SupportedGame current = _selection.Current;
			if ((object)current == null)
			{
				text = "Desktop";
				placeholder = false;
			}
			else
			{
				string displayName = current.DisplayName;
				placeholder = false;
				text = displayName;
			}
		}
		else
		{
			text = "No games found";
			placeholder = true;
		}
		SignalMenu.PaintTrigger(e.Graphics, ClientSize, ControlInk.Dpi(this), Font, text, placeholder, _open, _fade.Value, Enabled, !_selection.NothingInstalled);
		if (Focused && ShowFocusCues)
		{
			_ring.Show(Enabled);
		}
		else
		{
			_ring.Show(show: false);
		}
	}
}
