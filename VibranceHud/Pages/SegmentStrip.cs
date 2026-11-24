using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Pages;

internal sealed class SegmentStrip : Control
{
	private sealed class StripAccessibleObject : ControlAccessibleObject
	{
		private readonly SegmentStrip _owner;

		private readonly SegmentAccessibleObject[] _children;

		public StripAccessibleObject(SegmentStrip owner)
			: base(owner)
		{
			_owner = owner;
			_children = new SegmentAccessibleObject[owner._kinds.Length];
			for (int i = 0; i < _children.Length; i++)
			{
				_children[i] = new SegmentAccessibleObject(owner, this, i);
			}
		}

		public override int GetChildCount()
		{
			return _children.Length;
		}

		public override AccessibleObject? GetChild(int index)
		{
			if (index < 0 || index >= _children.Length)
			{
				return null;
			}
			return _children[index];
		}

		public override AccessibleObject? Navigate(AccessibleNavigation direction)
		{
			return direction switch
			{
				AccessibleNavigation.FirstChild => GetChild(0), 
				AccessibleNavigation.LastChild => GetChild(_children.Length - 1), 
				_ => base.Navigate(direction), 
			};
		}
	}

	private sealed class SegmentAccessibleObject : AccessibleObject
	{
		private readonly SegmentStrip _owner;

		private readonly StripAccessibleObject _parent;

		private readonly int _index;

		public override AccessibleObject? Parent => _parent;

		public override string? Name
		{
			get
			{
				return _owner._kinds[_index];
			}
			set
			{
			}
		}

		public override AccessibleRole Role
		{
			get
			{
				if (!_owner.ArrowsChoose)
				{
					return AccessibleRole.RadioButton;
				}
				return AccessibleRole.PageTab;
			}
		}

		public override AccessibleStates State
		{
			get
			{
				AccessibleStates accessibleStates = AccessibleStates.Selectable;
				accessibleStates = ((!_owner.Enabled) ? (accessibleStates | AccessibleStates.Unavailable) : (accessibleStates | AccessibleStates.Focusable));
				if (_owner._kinds[_index] == _owner._active)
				{
					accessibleStates = (AccessibleStates)((int)accessibleStates | (_owner.ArrowsChoose ? 2 : 16));
				}
				return accessibleStates;
			}
		}

		public override Rectangle Bounds
		{
			get
			{
				Rectangle[] array = _owner.Segments();
				if (_index >= array.Length)
				{
					return Rectangle.Empty;
				}
				return _owner.RectangleToScreen(array[_index]);
			}
		}

		public override string? DefaultAction => "Select";

		public SegmentAccessibleObject(SegmentStrip owner, StripAccessibleObject parent, int index)
		{
			_owner = owner;
			_parent = parent;
			_index = index;
		}

		public override void DoDefaultAction()
		{
			if (_owner.Enabled)
			{
				_owner.TestChoose(_owner._kinds[_index]);
			}
		}

		public override AccessibleObject? Navigate(AccessibleNavigation direction)
		{
			return direction switch
			{
				AccessibleNavigation.Next => _parent.GetChild(_index + 1), 
				AccessibleNavigation.Previous => _parent.GetChild(_index - 1), 
				_ => null, 
			};
		}
	}

	private const int PoPadding = 3;

	private readonly string[] _kinds;

	private string _active;

	private int _hover = -1;

	private int _cursor = -1;

	private readonly OutsideFocusRing _ring;

	private readonly OutsideFocusRing _stripRing;

	private static int Radius => Tokens.Scale(4);

	private static int SegmentRadius => Tokens.Scale(2);

	private static int Inset => Tokens.Scale(4);

	private static int SegmentGap => Tokens.Scale(2);

	internal bool ArrowsChoose { get; set; }

	internal int NaturalWidth
	{
		get
		{
			int num = 2 * Inset + SegmentGap * Math.Max(0, _kinds.Length - 1);
			for (int i = 0; i < _kinds.Length; i++)
			{
				num += TextRenderer.MeasureText(_kinds[i], FontOf(i), Size.Empty, TextFormatFlags.NoPadding).Width + 2 * Tokens.Scale(12);
			}
			return num;
		}
	}

	internal IReadOnlyList<string> Items => _kinds;

	private int CursorIndex
	{
		get
		{
			if (_cursor < 0 || _cursor >= _kinds.Length)
			{
				return Math.Max(0, Array.IndexOf(_kinds, _active));
			}
			return _cursor;
		}
	}

	internal Rectangle TestRingBox => RingBox();

	internal string Active
	{
		get
		{
			return _active;
		}
		set
		{
			if (!(_active == value))
			{
				_active = value;
				Invalidate();
				if (IsHandleCreated)
				{
					AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
				}
			}
		}
	}

	internal Rectangle[] TestSegments => Segments();

	internal event EventHandler<string>? Chosen;

	private Font FontOf(int i)
	{
		if (!(_kinds[i] == _active))
		{
			return Fonts.Body;
		}
		return Fonts.BodyBold;
	}

	private Color Ink(Color c)
	{
		if (!Enabled)
		{
			return ControlInk.Locked(c, Theme.Surface);
		}
		return c;
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Invalidate();
	}

	internal SegmentStrip(string[] kinds)
	{
		_kinds = kinds;
		_active = ((kinds.Length != 0) ? kinds[0] : "");
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		TabStop = true;
		SetStyle(ControlStyles.Selectable, value: true);
		AccessibleRole = AccessibleRole.PageTabList;
		_ring = new OutsideFocusRing(this, () => SegmentRadius, RingBox);
		_stripRing = new OutsideFocusRing(this, () => Radius);
	}

	private Rectangle RingBox()
	{
		Rectangle[] array = Segments();
		if (array.Length != 0)
		{
			return array[Math.Min(CursorIndex, array.Length - 1)];
		}
		return ClientRectangle;
	}

	internal bool TestClaimsKey(Keys key)
	{
		return IsInputKey(key);
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	protected override bool IsInputKey(Keys keyData)
	{
		bool flag = (keyData & Keys.Modifiers) == 0;
		if (flag)
		{
			Keys keys = keyData & Keys.KeyCode;
			bool flag2 = (uint)(keys - 35) <= 5u;
			flag = flag2;
		}
		if (!flag)
		{
			return base.IsInputKey(keyData);
		}
		return true;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (_kinds.Length == 0 || e.Modifiers != Keys.None)
		{
			return;
		}
		int cursorIndex = CursorIndex;
		int num;
		switch (e.KeyCode)
		{
		case Keys.Left:
		case Keys.Up:
			num = (cursorIndex - 1 + _kinds.Length) % _kinds.Length;
			break;
		case Keys.Right:
		case Keys.Down:
			num = (cursorIndex + 1) % _kinds.Length;
			break;
		case Keys.Home:
			num = 0;
			break;
		case Keys.End:
			num = _kinds.Length - 1;
			break;
		case Keys.Return:
		case Keys.Space:
			e.Handled = true;
			_cursor = -1;
			if (_kinds[cursorIndex] != _active)
			{
				Chosen?.Invoke(this, _kinds[cursorIndex]);
			}
			Invalidate();
			return;
		default:
			return;
		}
		e.Handled = true;
		if (num != cursorIndex)
		{
			if (ArrowsChoose)
			{
				_cursor = -1;
				Chosen?.Invoke(this, _kinds[num]);
			}
			else
			{
				_cursor = num;
			}
			Invalidate();
		}
	}

	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		_cursor = -1;
		Invalidate();
	}

	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);
		_cursor = -1;
		Invalidate();
	}

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		_ring.Rehook();
		_stripRing.Rehook();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_ring.Release();
			_stripRing.Release();
		}
		base.Dispose(disposing);
	}

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new StripAccessibleObject(this);
	}

	private Rectangle[] Segments()
	{
		Rectangle[] array = new Rectangle[_kinds.Length];
		if (_kinds.Length == 0)
		{
			return array;
		}
		int inset = Inset;
		int segmentGap = SegmentGap;
		int num = Math.Max(1, Width - 2 * inset - segmentGap * (_kinds.Length - 1));
		int num2 = 0;
		int[] array2 = new int[_kinds.Length];
		for (int i = 0; i < _kinds.Length; i++)
		{
			array2[i] = TextRenderer.MeasureText(_kinds[i], FontOf(i), Size.Empty, TextFormatFlags.NoPadding).Width;
			num2 += array2[i];
		}
		int num3 = Math.Max(0, num - num2);
		int num4 = inset;
		for (int j = 0; j < _kinds.Length; j++)
		{
			int num5 = num3 * (j + 1) / _kinds.Length - num3 * j / _kinds.Length;
			int num6 = array2[j] + num5;
			array[j] = new Rectangle(num4, inset, num6, Math.Max(1, Height - 2 * inset));
			num4 += num6 + segmentGap;
		}
		return array;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		Rectangle[] array = Segments();
		int hover = _hover;
		_hover = -1;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Contains(e.Location))
			{
				_hover = i;
			}
		}
		if (_hover != hover)
		{
			Invalidate();
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		if (_hover >= 0)
		{
			_hover = -1;
			Invalidate();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (e.Button != MouseButtons.Left)
		{
			return;
		}
		Rectangle[] array = Segments();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Contains(e.Location))
			{
				if (!(_kinds[i] == _active))
				{
					Chosen?.Invoke(this, _kinds[i]);
				}
				break;
			}
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
		using (Pen pen = new Pen(Ink(Theme.GlassEdge), Tokens.Scale(1)))
		{
			using GraphicsPath path = Glass.RoundedPath(rect, Radius);
			graphics.DrawPath(pen, path);
		}
		Rectangle[] array = Segments();
		for (int i = 0; i < array.Length; i++)
		{
			bool flag = _kinds[i] == _active;
			RectangleF rect2 = new RectangleF(array[i].X, array[i].Y, array[i].Width, array[i].Height);
			if (flag)
			{
				using SolidBrush brush = new SolidBrush(Ink(Theme.Accent));
				using GraphicsPath path2 = Glass.RoundedPath(rect2, SegmentRadius);
				graphics.FillPath(brush, path2);
			}
			else if (_hover == i && Enabled)
			{
				using SolidBrush brush2 = new SolidBrush(Theme.SurfaceHover);
				using GraphicsPath path3 = Glass.RoundedPath(rect2, SegmentRadius);
				graphics.FillPath(brush2, path3);
			}
			TextRenderer.DrawText(graphics, _kinds[i], FontOf(i), array[i], Ink(flag ? Theme.OnAccent : Theme.TextDim), TextFormatFlags.HorizontalCenter | TextFormatFlags.NoClipping | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
		bool flag2 = Focused && ShowFocusCues && Enabled;
		if (flag2)
		{
			ControlInk.PaintFocusRing(graphics, RingBox(), SegmentRadius, ControlInk.Dpi(this));
		}
		_ring.Show(flag2);
		_stripRing.Show(flag2);
	}

	internal void TestChoose(string item)
	{
		if (item != _active && Array.IndexOf(_kinds, item) >= 0)
		{
			Chosen?.Invoke(this, item);
		}
	}
}
