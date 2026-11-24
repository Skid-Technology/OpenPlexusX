using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Hotkeys;

namespace VibranceHud;

public sealed class HotkeyPicker : Control
{
	private sealed class PickerAccessible : ControlAccessibleObject
	{
		private readonly HotkeyPicker _owner;

		public override string? Value
		{
			get
			{
				return GetSpacedDisplay(_owner._modifierMask, _owner._virtualKey);
			}
			set
			{
			}
		}

		public override string? Description
		{
			get
			{
				string text = _owner.AccessibleDescription ?? "";
				string captureError = _owner._captureError;
				if (captureError.Length == 0)
				{
					if (text.Length != 0)
					{
						return text;
					}
					return null;
				}
				if (text.Length != 0)
				{
					return text + " " + captureError;
				}
				return captureError;
			}
		}

		public PickerAccessible(HotkeyPicker owner)
			: base(owner)
		{
			_owner = owner;
		}
	}

	private const int ButtonWidth = 64;

	private const int ButtonHeight = 28;

	private const int ChipPadding = 8;

	public static readonly Size PickerDefaultSize = new Size(300, 40);

	public static readonly Size PickerMinimumSize = new Size(240, 36);

	private Rectangle _setBtnRect;

	private uint _modifierMask;

	private uint _virtualKey;

	private bool _capturing;

	private bool _hoverSet;

	private string _captureError = "";

	private bool _bindingFailed;

	private readonly HashSet<Keys> _held = new HashSet<Keys>();

	private bool _sawModifier;

	internal const string UsedByAnotherApp = "Used by another app - pick another";

	private HotkeyCaptureGate _gate = HotkeyCaptureGate.Shared;

	private readonly OutsideFocusRing _ring;

	private bool _dimmed;

	private static Font ComboFont => Fonts.BodyBold;

	private static Font ButtonFont => Fonts.BodyBold;

	internal HotkeyCaptureGate Gate
	{
		set
		{
			_gate = value;
		}
	}

	internal static Func<nint, uint, uint, int>? DefaultProbe { get; set; }

	internal Func<uint, uint, int>? ComboProbe { get; set; }

	internal string CaptureMessage => _captureError;

	public uint ModifierMask
	{
		get
		{
			return _modifierMask;
		}
		set
		{
			_modifierMask = value;
			StopListening();
			_captureError = "";
			Invalidate();
			NotifyValueChanged();
		}
	}

	public uint VirtualKey
	{
		get
		{
			return _virtualKey;
		}
		set
		{
			_virtualKey = value;
			StopListening();
			_captureError = "";
			Invalidate();
			NotifyValueChanged();
		}
	}

	public bool Dimmed
	{
		get
		{
			return _dimmed;
		}
		set
		{
			if (_dimmed != value)
			{
				_dimmed = value;
				Invalidate();
			}
		}
	}

	internal Rectangle SetButtonBoundsForTest => _setBtnRect;

	internal Color ComboInkForTest => Combo().Ink;

	internal bool ComboIsBoldForTest => Combo().Bold;

	public bool IsCapturing => _capturing;

	public bool AllowMouseButtons { get; set; }

	public event Action<uint, uint>? HotkeyChanged;

	public void ReportBindingResult(bool succeeded)
	{
		_bindingFailed = !succeeded;
		Invalidate();
	}

	public HotkeyPicker()
	{
		_ring = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 4f));
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Size = PickerDefaultSize;
		MinimumSize = PickerMinimumSize;
		TabStop = true;
		AccessibleName = "Shortcut";
		AccessibleRole = AccessibleRole.Text;
		_modifierMask = 3u;
		_virtualKey = 86u;
		_setBtnRect = new Rectangle(0, 0, 64, 28);
	}

	private void NotifyValueChanged()
	{
		AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
	}

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new PickerAccessible(this);
	}

	public static string GetDisplay(uint modifierMask, uint virtualKey)
	{
		return Join(modifierMask, virtualKey, "+");
	}

	private static string Join(uint modifierMask, uint virtualKey, string glue)
	{
		if (virtualKey == 0)
		{
			return "Not set";
		}
		List<string> list = new List<string>(5);
		if ((modifierMask & 2) != 0)
		{
			list.Add("Ctrl");
		}
		if ((modifierMask & 1) != 0)
		{
			list.Add("Alt");
		}
		if ((modifierMask & 4) != 0)
		{
			list.Add("Shift");
		}
		if ((modifierMask & 8) != 0)
		{
			list.Add("Win");
		}
		list.Add(KeyName(virtualKey));
		return string.Join(glue, list);
	}

	public static string GetSpacedDisplay(uint modifierMask, uint virtualKey)
	{
		return Join(modifierMask, virtualKey, " + ");
	}

	public static string KeyName(uint vk)
	{
		if (vk >= 65 && vk <= 90)
		{
			return ((char)vk).ToString();
		}
		if (vk >= 48 && vk <= 57)
		{
			return ((char)vk).ToString();
		}
		if (vk >= 112 && vk <= 135)
		{
			return "F" + (vk - 111);
		}
		if (vk >= 96 && vk <= 105)
		{
			return "Num" + (vk - 96);
		}
		return vk switch
		{
			33u => "PageUp", 
			34u => "PageDown", 
			35u => "End", 
			36u => "Home", 
			45u => "Insert", 
			46u => "Delete", 
			37u => "Left", 
			38u => "Up", 
			39u => "Right", 
			40u => "Down", 
			4u => "Middle mouse", 
			5u => "Mouse 4", 
			6u => "Mouse 5", 
			32u => "Space", 
			9u => "Tab", 
			13u => "Enter", 
			8u => "Backspace", 
			27u => "Esc", 
			20u => "CapsLock", 
			145u => "ScrollLock", 
			144u => "NumLock", 
			19u => "Pause", 
			44u => "PrintScreen", 
			106u => "Num*", 
			107u => "Num+", 
			109u => "Num-", 
			110u => "Num.", 
			111u => "Num/", 
			186u => ";", 
			187u => "=", 
			188u => ",", 
			189u => "-", 
			190u => ".", 
			191u => "/", 
			192u => "`", 
			219u => "[", 
			220u => "\\", 
			221u => "]", 
			222u => "'", 
			173u => "Mute", 
			174u => "VolumeDown", 
			175u => "VolumeUp", 
			176u => "NextTrack", 
			177u => "PrevTrack", 
			178u => "StopMedia", 
			179u => "PlayPause", 
			0u => "(none)", 
			_ => "0x" + vk.ToString("X"), 
		};
	}

	public static bool IsBindable(uint modifierMask, uint virtualKey, out string error)
	{
		error = "";
		if (virtualKey == 0)
		{
			error = "Press a key to bind.";
			return false;
		}
		if (IsModifierOnly(virtualKey))
		{
			error = "Now press the key.";
			return false;
		}
		if (modifierMask == 0 && virtualKey == 27)
		{
			error = "Esc cancels - pick another key.";
			return false;
		}
		if (IsReservedCombo(modifierMask, virtualKey))
		{
			error = "That combination is reserved by Windows.";
			return false;
		}
		return true;
	}

	public void CancelCapture()
	{
		if (_capturing)
		{
			ExitCapture();
		}
	}

	internal void TestBeginCapture()
	{
		EnterCapture();
	}

	private void EnterCapture()
	{
		_held.Clear();
		_sawModifier = false;
		_captureError = "";
		_bindingFailed = false;
		if (!_capturing)
		{
			_capturing = true;
			_gate.Enter(this);
		}
		Focus();
		Invalidate();
	}

	private void ExitCapture()
	{
		StopListening();
		_captureError = "";
		Invalidate();
	}

	private void StopListening()
	{
		_held.Clear();
		_sawModifier = false;
		if (_capturing)
		{
			_capturing = false;
			_gate.Leave(this);
		}
	}

	protected override void OnHandleDestroyed(EventArgs e)
	{
		StopListening();
		base.OnHandleDestroyed(e);
	}

	internal static uint MouseButtonKey(MouseButtons b)
	{
		return b switch
		{
			MouseButtons.Middle => 4u, 
			MouseButtons.XButton1 => 5u, 
			MouseButtons.XButton2 => 6u, 
			_ => 0u, 
		};
	}

	internal bool TestMouseCapture(MouseButtons b, Keys modifiers)
	{
		return TryCaptureMouse(b, modifiers);
	}

	private bool TryCaptureMouse(MouseButtons b, Keys modifiers)
	{
		if (!_capturing || !AllowMouseButtons)
		{
			return false;
		}
		uint num = MouseButtonKey(b);
		if (num == 0)
		{
			return false;
		}
		uint num2 = 0u;
		if ((modifiers & Keys.Control) != 0)
		{
			num2 |= 2;
		}
		if ((modifiers & Keys.Alt) != 0)
		{
			num2 |= 1;
		}
		if ((modifiers & Keys.Shift) != 0)
		{
			num2 |= 4;
		}
		_modifierMask = num2;
		_virtualKey = num;
		ExitCapture();
		NotifyValueChanged();
		HotkeyChanged?.Invoke(_modifierMask, _virtualKey);
		return true;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (!TryCaptureMouse(e.Button, Control.ModifierKeys) && e.Button == MouseButtons.Left && _setBtnRect.Contains(e.Location))
		{
			if (_capturing)
			{
				ExitCapture();
			}
			else
			{
				EnterCapture();
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		bool flag = _setBtnRect.Contains(e.Location);
		Cursor = (flag ? Cursors.Hand : Cursors.Default);
		if (flag != _hoverSet)
		{
			_hoverSet = flag;
			Invalidate(Rectangle.Inflate(_setBtnRect, 2, 2));
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		Cursor = Cursors.Default;
		if (_hoverSet)
		{
			_hoverSet = false;
			Invalidate(Rectangle.Inflate(_setBtnRect, 2, 2));
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (!_capturing)
		{
			return;
		}
		if (e.KeyCode == Keys.Escape)
		{
			ExitCapture();
			e.Handled = true;
			e.SuppressKeyPress = true;
			return;
		}
		uint num = 0u;
		if (e.Control)
		{
			num |= 2;
		}
		if (e.Alt)
		{
			num |= 1;
		}
		if (e.Shift)
		{
			num |= 4;
		}
		if ((Control.ModifierKeys & Keys.LWin) != 0 || (Control.ModifierKeys & Keys.RWin) != 0 || IsKeyDown(Keys.LWin) || IsKeyDown(Keys.RWin))
		{
			num |= 8;
		}
		uint keyCode = (uint)e.KeyCode;
		if (IsModifierOnly(keyCode))
		{
			_held.Add(e.KeyCode);
			_sawModifier = true;
		}
		else
		{
			_sawModifier = false;
		}
		TryBind(num, keyCode);
		e.Handled = true;
		e.SuppressKeyPress = true;
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		base.OnKeyUp(e);
		if (!_capturing)
		{
			return;
		}
		if (e.KeyCode == Keys.Snapshot)
		{
			uint num = 0u;
			if (e.Control)
			{
				num |= 2;
			}
			if (e.Alt)
			{
				num |= 1;
			}
			if (e.Shift)
			{
				num |= 4;
			}
			if (_held.Contains(Keys.LWin) || _held.Contains(Keys.RWin))
			{
				num |= 8;
			}
			TryBind(num, 44u);
			e.Handled = true;
			e.SuppressKeyPress = true;
		}
		else if (IsModifierOnly((uint)e.KeyCode))
		{
			_held.Remove(e.KeyCode);
			if (_held.Count == 0 && _sawModifier)
			{
				_sawModifier = false;
				_captureError = "Used by another app - pick another";
				Invalidate();
			}
		}
	}

	private void TryBind(uint mods, uint vk)
	{
		if (!IsBindable(mods, vk, out string error))
		{
			_captureError = (IsModifierOnly(vk) ? "" : error);
			Invalidate();
			return;
		}
		if (ComboTaken(mods, vk))
		{
			_captureError = "Used by another app - pick another";
			Invalidate();
			return;
		}
		_modifierMask = mods;
		_virtualKey = vk;
		ExitCapture();
		NotifyValueChanged();
		HotkeyChanged?.Invoke(_modifierMask, _virtualKey);
	}

	private bool ComboTaken(uint mods, uint vk)
	{
		try
		{
			int num;
			if (ComboProbe != null)
			{
				num = ComboProbe(mods, vk);
			}
			else
			{
				if (DefaultProbe == null || AllowMouseButtons || !IsHandleCreated)
				{
					return false;
				}
				num = DefaultProbe(Handle, mods, vk);
			}
			return num == 1409;
		}
		catch
		{
			return false;
		}
	}

	internal void TestKeyDown(Keys keyData)
	{
		OnKeyDown(new KeyEventArgs(keyData));
	}

	internal void TestKeyUp(Keys keyData)
	{
		OnKeyUp(new KeyEventArgs(keyData));
	}

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int vKey);

	private static bool IsKeyDown(Keys key)
	{
		return (GetAsyncKeyState((int)key) & 0x8000) != 0;
	}

	private static bool IsModifierOnly(uint vk)
	{
		if (vk != 17 && vk != 16 && vk != 18 && vk != 164 && vk != 165 && vk != 162 && vk != 163 && vk != 160 && vk != 161 && vk != 91)
		{
			return vk == 92;
		}
		return true;
	}

	private static bool IsReservedCombo(uint mask, uint vk)
	{
		bool flag = (mask & 2) != 0;
		bool flag2 = (mask & 1) != 0;
		bool flag3 = (mask & 8) != 0;
		if ((flag & flag2) && vk == 46)
		{
			return true;
		}
		if (flag && vk == 27)
		{
			return true;
		}
		if (flag2 && (vk == 9 || vk == 27 || vk == 115))
		{
			return true;
		}
		if (flag3 && vk == 76)
		{
			return true;
		}
		return false;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		int dpi = ControlInk.Dpi(this);
		int num = ControlInk.Px(dpi, 64);
		int num2 = ControlInk.Px(dpi, 28);
		int y = (Height - num2) / 2;
		int num3 = ControlInk.Px(dpi, 8);
		RectangleF value = new RectangleF(num3, 0f, Math.Max(60, Width - num - num3 * 2), Height);
		var (text, foreColor, flag) = Combo();
		TextRenderer.DrawText(graphics, text, flag ? ComboFont : Fonts.Body, Rectangle.Round(value), foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
		_setBtnRect.X = Width - num;
		_setBtnRect.Y = y;
		_setBtnRect.Width = num;
		_setBtnRect.Height = num2;
		PaintSecondaryButton(graphics, _setBtnRect, _capturing ? "Cancel" : "Set", _hoverSet, dpi, Enabled);
		if (Focused && ShowFocusCues)
		{
			_ring.Show(Enabled);
		}
		else
		{
			_ring.Show(show: false);
		}
	}

	private (string Text, Color Ink, bool Bold) Combo()
	{
		bool item = true;
		string item2;
		Color item3;
		int num;
		Color color;
		if (_capturing)
		{
			item2 = ((_captureError.Length > 0) ? _captureError : "Press any key (Esc to cancel)");
			item3 = ((_captureError.Length > 0) ? ControlInk.Danger : Theme.Text);
		}
		else
		{
			if (!_bindingFailed)
			{
				item2 = GetSpacedDisplay(_modifierMask, _virtualKey);
				if (_virtualKey != 0)
				{
					num = (_dimmed ? 1 : 0);
					if (num == 0)
					{
						color = Theme.Text;
						goto IL_00a1;
					}
				}
				else
				{
					num = 1;
				}
				color = ControlInk.Text3;
				goto IL_00a1;
			}
			item2 = GetSpacedDisplay(_modifierMask, _virtualKey) + "  - already in use";
			item3 = ControlInk.Warning;
		}
		goto IL_00a6;
		IL_00a6:
		if (!Enabled)
		{
			item3 = ControlInk.TextDisabledInk;
		}
		return (Text: item2, Ink: item3, Bold: item);
		IL_00a1:
		item3 = color;
		item = num == 0;
		goto IL_00a6;
	}

	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		Invalidate();
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Invalidate();
	}

	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);
		if (_capturing)
		{
			ExitCapture();
		}
		else
		{
			Invalidate();
		}
	}

	protected override void OnLeave(EventArgs e)
	{
		base.OnLeave(e);
		if (_capturing)
		{
			ExitCapture();
		}
	}

	internal void TestLoseFocus()
	{
		OnLostFocus(EventArgs.Empty);
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
			StopListening();
			_ring.Release();
		}
		base.Dispose(disposing);
	}

	private static void PaintSecondaryButton(Graphics g, Rectangle rect, string text, bool hover, int dpi, bool enabled)
	{
		Color fill = ((hover & enabled) ? ControlInk.Hover : Color.Transparent);
		GraphicsState gstate = g.Save();
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.TranslateTransform(rect.X, rect.Y);
		ControlInk.PaintBox(g, rect.Size, ControlInk.PxF(dpi, 4f), ControlInk.Hairline(dpi), fill, enabled ? ControlInk.Edge : ControlInk.TextDisabled);
		g.Restore(gstate);
		TextRenderer.DrawText(g, text, ButtonFont, rect, enabled ? ControlInk.Text1 : ControlInk.TextDisabledInk, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
	}

	protected override bool IsInputKey(Keys keyData)
	{
		if (!_capturing)
		{
			return base.IsInputKey(keyData);
		}
		return true;
	}

	internal bool TestClaimsKey(Keys key)
	{
		return IsInputKey(key);
	}
}
