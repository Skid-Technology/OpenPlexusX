using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class GlassTextBox : Control
{
	private sealed class NamedTextBox : TextBox
	{
		private sealed class Accessible : ControlAccessibleObject
		{
			private readonly NamedTextBox _box;

			public override string? Name
			{
				get
				{
					_box._sync();
					return base.Name;
				}
				set
				{
					base.Name = value;
				}
			}

			public override string? Description
			{
				get
				{
					_box._sync();
					return base.Description;
				}
			}

			public Accessible(NamedTextBox box)
				: base(box)
			{
				_box = box;
			}
		}

		private readonly Action _sync;

		public NamedTextBox(Action sync)
		{
			_sync = sync;
		}

		protected override AccessibleObject CreateAccessibilityInstance()
		{
			return new Accessible(this);
		}
	}

	private readonly TextBox _inner;

	private readonly OutsideFocusRing _ring;

	private const int PadX = 12;

	private const int IconPadLeft = 8;

	private const int IconSize = 16;

	private const int IconGap = 8;

	private SignalIcon _leadingIcon;

	private bool _focusOnEdge;

	private string _placeholder = "";

	private bool _innerNameIsOurs;

	private bool _innerDescriptionIsOurs;

	public TextBox Inner => _inner;

	public bool FocusOnEdge
	{
		get
		{
			return _focusOnEdge;
		}
		set
		{
			if (_focusOnEdge != value)
			{
				_focusOnEdge = value;
				if (value)
				{
					_ring.Show(show: false);
				}
				Invalidate();
			}
		}
	}

	internal Color EdgeInk
	{
		get
		{
			if (Enabled)
			{
				if (!_focusOnEdge || !ContainsFocus)
				{
					return ControlInk.Edge;
				}
				return ControlInk.Text3;
			}
			return ControlInk.TextDisabled;
		}
	}

	public SignalIcon LeadingIcon
	{
		get
		{
			return _leadingIcon;
		}
		set
		{
			if (_leadingIcon != value)
			{
				_leadingIcon = value;
				PerformLayout();
				Invalidate();
			}
		}
	}

	[Browsable(true)]
	public override string Text
	{
		get
		{
			return _inner.Text ?? "";
		}
		[param: AllowNull]
		set
		{
			_inner.Text = value ?? "";
		}
	}

	public string PlaceholderText
	{
		get
		{
			return _placeholder;
		}
		set
		{
			if (value == null)
			{
				value = "";
			}
			if (!(_placeholder == value))
			{
				_placeholder = value;
				SyncAccessibleNames();
				SyncHint();
			}
		}
	}

	public bool ReadOnly
	{
		get
		{
			return _inner.ReadOnly;
		}
		set
		{
			_inner.ReadOnly = value;
		}
	}

	public int MaxLength
	{
		get
		{
			return _inner.MaxLength;
		}
		set
		{
			_inner.MaxLength = value;
		}
	}

	public CharacterCasing CharacterCasing
	{
		get
		{
			return _inner.CharacterCasing;
		}
		set
		{
			_inner.CharacterCasing = value;
		}
	}

	public bool Multiline
	{
		get
		{
			return _inner.Multiline;
		}
		set
		{
			_inner.Multiline = value;
			if (value)
			{
				_inner.ScrollBars = ScrollBars.None;
			}
			PerformLayout();
		}
	}

	private Color InnerGround
	{
		get
		{
			if (ControlInk.FieldFill.A != byte.MaxValue)
			{
				return GroundBehind;
			}
			return ControlInk.FieldFill;
		}
	}

	private Color GroundBehind
	{
		get
		{
			if (!(FindForm() is SignalDialogWindow))
			{
				return Theme.Surface;
			}
			return ControlInk.Overlay;
		}
	}

	internal bool FocusRingShownForTest => _ring.Shown;

	private bool HintShowing
	{
		get
		{
			if (_placeholder.Length > 0 && _inner.Text.Length == 0)
			{
				return !ContainsFocus;
			}
			return false;
		}
	}

	public GlassTextBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		_inner = new NamedTextBox(SyncAccessibleNames)
		{
			BorderStyle = BorderStyle.None,
			BackColor = InnerGround,
			ForeColor = Theme.Text,
			Font = Fonts.Body
		};
		_inner.GotFocus += (object? s, EventArgs e) =>
		{
			_ring.Show(Enabled && !_focusOnEdge);
			Invalidate();
			SyncHint();
		};
		_inner.LostFocus += (object? s, EventArgs e) =>
		{
			_ring.Show(show: false);
			Invalidate();
			SyncHint();
		};
		_inner.TextChanged += (object? s, EventArgs e) =>
		{
			SyncHint();
			OnTextChanged(EventArgs.Empty);
		};
		Controls.Add(_inner);
		_ring = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 4f));
		Height = 32;
	}

	public void SetPlaceholder(string text)
	{
		PlaceholderText = text;
	}

	private void SyncAccessibleNames()
	{
		if (_inner == null)
		{
			return;
		}
		string text = ((!string.IsNullOrEmpty(AccessibleName)) ? AccessibleName : _placeholder);
		if (string.IsNullOrEmpty(_inner.AccessibleName) || _innerNameIsOurs)
		{
			string text2 = ((text.Length > 0) ? text : null);
			if (_inner.AccessibleName != text2)
			{
				_inner.AccessibleName = text2;
			}
			_innerNameIsOurs = text2 != null;
		}
		string accessibleDescription = AccessibleDescription;
		if (string.IsNullOrEmpty(_inner.AccessibleDescription) || _innerDescriptionIsOurs)
		{
			string text3 = (string.IsNullOrEmpty(accessibleDescription) ? null : accessibleDescription);
			if (_inner.AccessibleDescription != text3)
			{
				_inner.AccessibleDescription = text3;
			}
			_innerDescriptionIsOurs = text3 != null;
		}
	}

	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		if (!_inner.Visible)
		{
			_inner.Visible = true;
		}
		_inner.Focus();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left && CanFocus && !ContainsFocus)
		{
			Focus();
		}
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

	private int TextLeft(int dpi)
	{
		if (_leadingIcon != SignalIcon.None)
		{
			return ControlInk.Px(dpi, 32);
		}
		return ControlInk.Px(dpi, 12);
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		base.OnLayout(e);
		if (_inner != null)
		{
			int dpi = ControlInk.Dpi(this);
			int num = TextLeft(dpi);
			int width = Math.Max(1, Width - num - ControlInk.Px(dpi, 12));
			if (_inner.Multiline)
			{
				int num2 = ControlInk.Px(dpi, 6);
				_inner.SetBounds(num, num2, width, Math.Max(1, Height - 2 * num2));
			}
			else
			{
				int num3 = Math.Min(_inner.PreferredHeight, Math.Max(1, Height - ControlInk.Px(dpi, 6)));
				_inner.SetBounds(num, Math.Max(0, (Height - num3) / 2), width, num3);
				SyncHint();
			}
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		int dpi = ControlInk.Dpi(this);
		Color innerGround = InnerGround;
		Color color = (Enabled ? Theme.Text : ControlInk.TextDisabledInk);
		if (_inner.BackColor != innerGround)
		{
			_inner.BackColor = innerGround;
		}
		if (_inner.ForeColor != color)
		{
			_inner.ForeColor = color;
		}
		ControlInk.PaintBox(graphics, ClientSize, ControlInk.PxF(dpi, 4f), ControlInk.Hairline(dpi), ControlInk.FieldFill, EdgeInk);
		if (_leadingIcon != SignalIcon.None)
		{
			int num = ControlInk.Px(dpi, 16);
			SignalIcons.Draw(graphics, _leadingIcon, new RectangleF(ControlInk.Px(dpi, 8), (float)(Height - num) / 2f, num, num), Enabled ? ControlInk.Text3 : ControlInk.TextDisabledInk);
		}
		if (HintShowing)
		{
			TextRenderer.DrawText(graphics, _placeholder, _inner.Font, _inner.Bounds, Enabled ? ControlInk.Text3 : ControlInk.TextDisabledInk, (TextFormatFlags)(0x10000800 | ((!_inner.Multiline) ? 4 : 0)));
		}
		_ring.Show(ContainsFocus && Enabled && !_focusOnEdge);
	}

	private void SyncHint()
	{
		if (_inner != null)
		{
			_inner.Visible = !HintShowing;
			Invalidate();
		}
	}
}
