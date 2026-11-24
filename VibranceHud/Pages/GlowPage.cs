using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Theming;

namespace VibranceHud.Pages;

public class GlowPage : UserControl
{
	private sealed class HeaderLabel : Label
	{
		protected override void OnPaint(PaintEventArgs e)
		{
			if (!NeedsAHalo || Font.Height < 28)
			{
				base.OnPaint(e);
			}
			else
			{
				PaintHeaderText(e.Graphics, Text, Font, ForeColor, ClientRectangle);
			}
		}
	}

	private bool _bringingIntoView;

	private const int SB_BOTH = 3;

	protected const int HeaderHeight = 86;

	private bool _headerAdded;

	protected const int Gutter = 16;

	internal const int HeaderPanelHeight = 84;

	internal const int HeaderToolbarGap = 12;

	internal const int HeaderToolbarHeight = 32;

	private const int HeaderPadX = 20;

	private const int HeaderPadY = 12;

	private const int ShellEdge = 1;

	private HeaderPanel? _headerCard;

	private Control? _headerTitleControl;

	private Control? _headerSubtitleControl;

	private Control? _headerToolbar;

	private Control[] _headerActions = Array.Empty<Control>();

	private int _headerSubtitleExtra;

	private int _scaledForDpi = 96;

	private HeaderLabel? _headerTitle;

	private HeaderLabel? _headerSubtitle;

	private int _contentWidth;

	private Dictionary<Control, int>? _designLeft;

	private int _centredForWidth = -1;

	private int _lastOffset;

	public IBackdrop? Field { get; set; }

	public Point FieldOffset { get; set; }

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 33554432;
			return createParams;
		}
	}

	public int ScrollExtent => HonestExtentHeight();

	public int ScrollViewport => ClientSize.Height;

	public int ScrollOffset => -AutoScrollPosition.Y;

	protected virtual int ScrollBottomPadding => 40;

	protected virtual string? PageTitle => null;

	protected virtual string PageSubtitle => "";

	protected virtual bool UsesHeaderPanel => false;

	protected virtual Control[] HeaderActions => Array.Empty<Control>();

	protected virtual Control? HeaderToolbar => null;

	internal HeaderPanel? HeaderCardForTest => _headerCard;

	internal Control? HeaderTitleForTest => _headerTitleControl;

	internal Control? HeaderSubtitleForTest => _headerSubtitleControl;

	protected bool HasHeaderPanel => _headerCard != null;

	protected static int ContentLeft => S(16);

	protected int ContentRight => Width + 1 - S(16);

	protected int HeaderPanelHeightPx
	{
		get
		{
			if (_headerCard != null)
			{
				return S(84) + _headerSubtitleExtra + ((_headerToolbar != null) ? S(44) : 0);
			}
			return 0;
		}
	}

	protected int ContentTop
	{
		get
		{
			if (_headerCard != null)
			{
				return S(16) + HeaderPanelHeightPx + S(16);
			}
			return S(16);
		}
	}

	protected virtual bool LaysOutInLogicalPixels => true;

	internal static bool NeedsAHalo => SlateSurface.Active;

	protected int ContentWidth
	{
		get
		{
			return _contentWidth;
		}
		set
		{
			_contentWidth = value;
			CentreContent();
		}
	}

	protected float DesignScale => (float)_scaledForDpi / 96f;

	public event EventHandler? ScrollStateChanged;

	protected GlowPage()
	{
		Dock = DockStyle.Fill;
		BackColor = Theme.Background;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void OnScroll(ScrollEventArgs se)
	{
		base.OnScroll(se);
		RaiseScrollState();
		Invalidate(invalidateChildren: true);
	}

	internal void InvalidateBackdropOnly()
	{
		Invalidate(invalidateChildren: true);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		int extent = ScrollExtent - ClientSize.Height;
		ScrollTrace.Write(() => $"{GetType().Name} wheel {e.Delta} autoScroll={AutoScroll} client={ClientSize.Height} min={AutoScrollMinSize.Height} extent={extent} offset={-AutoScrollPosition.Y}");
		int num = -AutoScrollPosition.Y;
		if (AutoScroll && extent > 0)
		{
			int num2 = SystemInformation.MouseWheelScrollLines;
			if (num2 <= 0)
			{
				num2 = 3;
			}
			int num3 = e.Delta * num2 * 22 / 120;
			int target = Math.Clamp(-AutoScrollPosition.Y - num3, 0, extent);
			if (target != -AutoScrollPosition.Y)
			{
				AutoScrollPosition = new Point(-AutoScrollPosition.X, target);
			}
			ScrollTrace.Write(() => $"  -> target={target} landed={-AutoScrollPosition.Y}");
			if (e is HandledMouseEventArgs e2)
			{
				e2.Handled = true;
			}
		}
		else
		{
			ScrollTrace.Write(() => "  -> IGNORED (nothing to scroll)");
			base.OnMouseWheel(e);
		}
		if (-AutoScrollPosition.Y != num)
		{
			RaiseScrollState();
			Invalidate(invalidateChildren: true);
		}
	}

	public void ScrollByWheel(int delta)
	{
		OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, 0, 0, delta));
	}

	internal void TestScrollWheel(int delta)
	{
		ScrollByWheel(delta);
	}

	private void HookWheelForwarding(Control root)
	{
		foreach (Control control in root.Controls)
		{
			control.MouseWheel -= ForwardWheel;
			control.MouseWheel += ForwardWheel;
			control.GotFocus -= OnDescendantGotFocus;
			control.GotFocus += OnDescendantGotFocus;
			control.ControlAdded -= OnDescendantAdded;
			control.ControlAdded += OnDescendantAdded;
			HookWheelForwarding(control);
		}
	}

	private void OnDescendantAdded(object? sender, ControlEventArgs e)
	{
		Control control = e.Control;
		if (control != null)
		{
			control.MouseWheel -= ForwardWheel;
			control.MouseWheel += ForwardWheel;
			control.GotFocus -= OnDescendantGotFocus;
			control.GotFocus += OnDescendantGotFocus;
			control.ControlAdded -= OnDescendantAdded;
			control.ControlAdded += OnDescendantAdded;
			HookWheelForwarding(control);
		}
	}

	private void ForwardWheel(object? sender, MouseEventArgs e)
	{
		OnMouseWheel(e);
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		Control control = ActiveControl;
		while (control is ContainerControl containerControl)
		{
			Control activeControl = containerControl.ActiveControl;
			if (activeControl == null)
			{
				break;
			}
			control = activeControl;
		}
		if (control != null)
		{
			RovingFocus rovingFocus = RovingFocus.Of(control);
			if (rovingFocus != null)
			{
				Control control2 = rovingFocus.Target(control, keyData);
				if (control2 != null)
				{
					if (control2 != control)
					{
						control2.Focus();
						if (rovingFocus.ArrowChooses(control2) && !control2.IsDisposed)
						{
							InvokeOnClick(control2, EventArgs.Empty);
						}
						rovingFocus.Refresh();
					}
					return true;
				}
			}
		}
		return base.ProcessDialogKey(keyData);
	}

	private void OnDescendantGotFocus(object? sender, EventArgs e)
	{
		if (sender is Control focused && KeyboardNavigation.Consume())
		{
			ScrollIntoViewForKeyboard(focused);
		}
	}

	internal void ScrollIntoViewForKeyboard(Control focused)
	{
		if (_bringingIntoView || !IsHandleCreated || focused.IsDisposed || !Contains(focused))
		{
			return;
		}
		_bringingIntoView = true;
		try
		{
			Control target = ((focused is TextBox && focused.Parent is GlassTextBox glassTextBox) ? glassTextBox : focused);
			if (target.Parent == null || !target.IsHandleCreated)
			{
				return;
			}
			int margin = ControlInk.FocusRingReach(ControlInk.Dpi(target)) + S(16);
			Control parent = target.Parent;
			while (parent != null && parent != this)
			{
				if (parent is ScrollableControl { AutoScroll: not false, IsHandleCreated: not false } scrollableControl)
				{
					BringInto(scrollableControl, target, margin);
				}
				parent = parent.Parent;
			}
			int num = ScrollExtent - ClientSize.Height;
			if (AutoScroll && num > 0)
			{
				int before = -AutoScrollPosition.Y;
				int want = OffsetThatShows(before, RectIn(this, target), ClientSize.Height, margin, num);
				ScrollTrace.Write(() => $"{GetType().Name} keyboard focus {target.GetType().Name} '{target.Text}' offset {before} -> {want}");
				if (want != before)
				{
					AutoScrollPosition = new Point(-AutoScrollPosition.X, want);
					RaiseScrollState();
					Invalidate(invalidateChildren: true);
				}
			}
		}
		finally
		{
			_bringingIntoView = false;
		}
	}

	private static void BringInto(ScrollableControl inner, Control target, int margin)
	{
		int num = inner.DisplayRectangle.Height - inner.ClientSize.Height;
		if (num > 0)
		{
			int num2 = -inner.AutoScrollPosition.Y;
			int num3 = OffsetThatShows(num2, RectIn(inner, target), inner.ClientSize.Height, margin, num);
			if (num3 != num2)
			{
				inner.AutoScrollPosition = new Point(-inner.AutoScrollPosition.X, num3);
				inner.Invalidate(invalidateChildren: true);
			}
		}
	}

	private static Rectangle RectIn(Control container, Control target)
	{
		return container.RectangleToClient(target.Parent.RectangleToScreen(target.Bounds));
	}

	internal static int OffsetThatShows(int offset, Rectangle box, int viewport, int margin, int max)
	{
		int num = box.Top - margin;
		int num2 = box.Bottom + margin;
		int value = offset;
		if (num < 0 || num2 - num > viewport)
		{
			value = offset + num;
		}
		else if (num2 > viewport)
		{
			value = offset + (num2 - viewport);
		}
		return Math.Clamp(value, 0, Math.Max(0, max));
	}

	protected override void OnControlAdded(ControlEventArgs e)
	{
		base.OnControlAdded(e);
		OnDescendantAdded(this, e);
	}

	private void RaiseScrollState()
	{
		HideNativeScrollBars();
		ScrollStateChanged?.Invoke(this, EventArgs.Empty);
	}

	public void ScrollTo(int offset)
	{
		ScrollTrace.Write(() => $"{GetType().Name} ScrollTo({offset}) from offset={-AutoScrollPosition.Y}\n" + Environment.StackTrace);
		AutoScrollPosition = new Point(-AutoScrollPosition.X, Math.Max(0, offset));
		Invalidate(invalidateChildren: true);
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		AddHeaderIfWanted();
		ApplyDpi(Tokens.Dpi);
		LayoutHeaderPanel();
		KeyboardNavigation.EnsureInstalled();
		HookWheelForwarding(this);
		RaiseScrollState();
		ScrollTrace.Write(() => $"{GetType().Name} shown: dpi={DeviceDpi} client={ClientSize.Width}x{ClientSize.Height} min={AutoScrollMinSize.Height} autoScroll={AutoScroll}");
	}

	protected override void WndProc(ref Message m)
	{
		base.WndProc(ref m);
		int msg = m.Msg;
		if ((uint)(msg - 276) <= 1u)
		{
			Message msg2 = m;
			ScrollTrace.Write(() => $"{GetType().Name} WM_{((msg2.Msg == 277) ? "V" : "H")}SCROLL wParam=0x{msg2.WParam:X} now offset={-AutoScrollPosition.Y}");
			Invalidate(invalidateChildren: true);
		}
		msg = m.Msg;
		if ((msg == 131 || (uint)(msg - 276) <= 1u) ? true : false)
		{
			HideNativeScrollBars();
		}
	}

	[DllImport("user32.dll")]
	private static extern bool ShowScrollBar(nint hWnd, int wBar, bool bShow);

	protected override void OnLayout(LayoutEventArgs e)
	{
		int before = -AutoScrollPosition.Y;
		base.OnLayout(e);
		int after = -AutoScrollPosition.Y;
		if (before != after)
		{
			ScrollTrace.Write(() => $"{GetType().Name} layout moved the page {before} -> {after} (affected={e.AffectedControl?.GetType().Name} '{e.AffectedProperty}')\n" + Environment.StackTrace);
		}
		RaiseScrollState();
	}

	protected override Point ScrollToControl(Control activeControl)
	{
		ScrollTrace.Write(() => $"{GetType().Name} ScrollToControl({activeControl?.GetType().Name} '{activeControl?.Text}') declined at offset={-AutoScrollPosition.Y}");
		return AutoScrollPosition;
	}

	private int HonestExtentHeight()
	{
		int height = AutoScrollMinSize.Height;
		int num = LowestChildBottom();
		if (num <= 0)
		{
			return height;
		}
		return Math.Min(height, num + ScrollBottomPadding);
	}

	private void AddHeaderIfWanted()
	{
		if (!_headerAdded && PageTitle != null)
		{
			_headerAdded = true;
			AddPageHeader(PageTitle, PageSubtitle);
		}
	}

	private static int S(int logical)
	{
		return Tokens.Scale(logical);
	}

	protected void UseHeaderPanel(Control title, Control subtitle, Control[]? actions = null, Control? toolbar = null)
	{
		if (_headerCard == null)
		{
			_headerCard = new HeaderPanel
			{
				Name = "HeaderPanel",
				AccessibleRole = AccessibleRole.Grouping
			};
			Controls.Add(_headerCard);
			foreach (Control control in Controls)
			{
				if (control != _headerCard)
				{
					control.TabIndex++;
				}
			}
			_headerCard.TabIndex = 0;
		}
		_headerCard.AccessibleName = title.Text;
		_headerTitleControl = title;
		_headerSubtitleControl = subtitle;
		_headerActions = actions ?? Array.Empty<Control>();
		_headerToolbar = toolbar;
		foreach (Control item in new Control[2] { title, subtitle }.Concat(_headerActions).Concat((toolbar == null) ? Array.Empty<Control>() : new Control[1] { toolbar }))
		{
			if (item.Parent != _headerCard)
			{
				item.Parent?.Controls.Remove(item);
				_headerCard.Controls.Add(item);
			}
		}
		LayoutHeaderPanel();
	}

	protected void LayoutHeaderPanel()
	{
		if (_headerCard == null || Width <= 0)
		{
			return;
		}
		int num = Math.Max(S(200), ContentRight - ContentLeft);
		int num2 = ControlInk.Hairline(Tokens.Dpi);
		int num3 = num2 + S(20);
		int num4 = num2 + S(12);
		int num5 = S(36);
		int num6 = S(20);
		int num7 = num4 + (num5 + S(2) + num6) / 2;
		int num8 = num - num3;
		bool flag = false;
		for (int num9 = _headerActions.Length - 1; num9 >= 0; num9--)
		{
			Control control = _headerActions[num9];
			if (control.Visible)
			{
				if (flag)
				{
					num8 -= S(8);
				}
				int num10 = InkInset(control);
				int num11 = Math.Max(0, control.Width - 2 * num10);
				control.Location = new Point(num8 - num11 - num10, num7 - control.Height / 2);
				num8 -= num11;
				flag = true;
			}
		}
		int num12 = (flag ? (num8 - S(16)) : (num - num3));
		int num13 = Math.Max(S(40), num12 - num3);
		Control headerTitleControl = _headerTitleControl;
		if (headerTitleControl != null)
		{
			int num14 = Math.Max(num5, TextRenderer.MeasureText(headerTitleControl.Text, headerTitleControl.Font).Height);
			int num15 = InkInset(headerTitleControl);
			headerTitleControl.SetBounds(num3 - num15, num4 - (num14 - num5) / 2, num13 + num15, num14);
		}
		_headerSubtitleExtra = 0;
		Control headerSubtitleControl = _headerSubtitleControl;
		if (headerSubtitleControl != null)
		{
			int num16 = num6;
			if (headerSubtitleControl.Visible && headerSubtitleControl.Text.Length > 0)
			{
				int height = TextRenderer.MeasureText(headerSubtitleControl.Text, headerSubtitleControl.Font, new Size(num13, int.MaxValue), TextFormatFlags.WordBreak).Height;
				num16 = Math.Max(num6, height);
			}
			_headerSubtitleExtra = num16 - num6;
			int num17 = InkInset(headerSubtitleControl);
			headerSubtitleControl.SetBounds(num3 - num17, num4 + num5 + S(2), num13 + num17, num16);
		}
		if (_headerToolbar != null)
		{
			_headerToolbar.SetBounds(num3, num4 + num5 + S(2) + num6 + _headerSubtitleExtra + S(12), num - 2 * num3, S(32));
		}
		_headerCard.SetBounds(ContentLeft, S(16) + AutoScrollPosition.Y, num, HeaderPanelHeightPx);
	}

	private static int InkInset(Control c)
	{
		if (!(c is Label label) || string.IsNullOrEmpty(label.Text))
		{
			return 0;
		}
		int width = TextRenderer.MeasureText(label.Text, label.Font).Width;
		int width2 = TextRenderer.MeasureText(label.Text, label.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
		return Math.Max(0, (width - width2) / 2);
	}

	public void ApplyDpi(int dpi)
	{
		if (!LaysOutInLogicalPixels || dpi <= 0 || dpi == _scaledForDpi)
		{
			return;
		}
		float num = (float)dpi / (float)_scaledForDpi;
		_scaledForDpi = dpi;
		SuspendLayout();
		foreach (Control control in Controls)
		{
			control.Scale(new SizeF(num, num));
		}
		if (AutoScrollMinSize.Height > 0)
		{
			AutoScrollMinSize = new Size((int)Math.Round((float)AutoScrollMinSize.Width * num), (int)Math.Round((float)AutoScrollMinSize.Height * num));
		}
		ResumeLayout(performLayout: true);
		LayoutHeaderPanel();
		Invalidate(invalidateChildren: true);
	}

	internal static void PaintHeaderText(Graphics g, string text, Font font, Color ink, Rectangle box)
	{
		Color foreColor = Color.FromArgb(150, Theme.IsLight ? Color.White : Color.Black);
		for (int i = 1; i <= 2; i++)
		{
			(int, int)[] array = new (int, int)[8]
			{
				(i, 0),
				(-i, 0),
				(0, i),
				(0, -i),
				(i, i),
				(-i, -i),
				(i, -i),
				(-i, i)
			};
			for (int j = 0; j < array.Length; j++)
			{
				var (num, num2) = array[j];
				TextRenderer.DrawText(g, text, font, new Rectangle(box.X + num, box.Y + num2, box.Width, box.Height), foreColor, TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
			}
		}
		TextRenderer.DrawText(g, text, font, box, ink, TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
	}

	protected void AddPageHeader(string title, string subtitle)
	{
		if (UsesHeaderPanel)
		{
			AddPageHeaderPanel(title, subtitle);
			return;
		}
		int num = 86;
		int num2 = int.MaxValue;
		foreach (Control control in Controls)
		{
			if (control.Visible && control.Left > 0)
			{
				num2 = Math.Min(num2, control.Left);
			}
		}
		if (num2 == int.MaxValue)
		{
			num2 = 24;
		}
		foreach (Control control2 in Controls)
		{
			control2.Top += num;
		}
		_headerTitle = new HeaderLabel
		{
			Text = title,
			Font = Fonts.Display,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(num2, 16, Wide(title, Fonts.Display), 36)
		};
		Controls.Add(_headerTitle);
		_headerSubtitle = new HeaderLabel
		{
			Text = subtitle,
			Font = Fonts.Label,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(num2, 56, Wide(subtitle, Fonts.Label), 20),
			Visible = (subtitle.Length > 0)
		};
		Controls.Add(_headerSubtitle);
		if (AutoScrollMinSize.Height > 0)
		{
			AutoScrollMinSize = new Size(AutoScrollMinSize.Width, AutoScrollMinSize.Height + num);
		}
		static int Wide(string text, Font font)
		{
			return TextRenderer.MeasureText(text, font).Width + 4;
		}
	}

	private void AddPageHeaderPanel(string title, string subtitle)
	{
		Control[] headerActions = HeaderActions;
		Control headerToolbar = HeaderToolbar;
		HashSet<Control> hashSet = new HashSet<Control>(headerActions);
		if (headerToolbar != null)
		{
			hashSet.Add(headerToolbar);
		}
		int num = 116 + ((headerToolbar != null) ? 44 : 0);
		foreach (Control control in Controls)
		{
			if (!hashSet.Contains(control))
			{
				control.Top += num;
			}
		}
		Label title2 = new Label
		{
			Text = title,
			Font = Fonts.Display,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft,
			UseMnemonic = false,
			Name = "PageTitle"
		};
		Label subtitle2 = new Label
		{
			Text = subtitle,
			Font = Fonts.Label,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.TopLeft,
			UseMnemonic = false,
			Visible = (subtitle.Length > 0),
			Name = "PageSubtitle"
		};
		UseHeaderPanel(title2, subtitle2, headerActions, headerToolbar);
		if (AutoScrollMinSize.Height > 0)
		{
			AutoScrollMinSize = new Size(AutoScrollMinSize.Width, AutoScrollMinSize.Height + num);
		}
	}

	private int LowestChildBottom()
	{
		int num = 0;
		int y = AutoScrollPosition.Y;
		foreach (Control control in Controls)
		{
			if (control.Visible)
			{
				num = Math.Max(num, control.Bottom - y);
			}
		}
		return num;
	}

	private void HideNativeScrollBars()
	{
		if (IsHandleCreated && !IsDisposed)
		{
			ShowScrollBar(Handle, 3, bShow: false);
		}
	}

	protected void FitScrollToContent(int bottomPadding = 40)
	{
		int y = AutoScrollPosition.Y;
		int num = 0;
		foreach (Control control in Controls)
		{
			if (control.Visible)
			{
				num = Math.Max(num, control.Bottom - y);
			}
		}
		if (num > 0)
		{
			AutoScrollMinSize = new Size(0, num + bottomPadding);
		}
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		CentreContent();
		LayoutHeaderPanel();
	}

	protected int AtDesignScale(int logical)
	{
		return (int)Math.Round((float)logical * DesignScale);
	}

	protected void ScaleToCurrentDesign(Control c)
	{
		float designScale = DesignScale;
		if (!(Math.Abs(designScale - 1f) < 0.001f))
		{
			c.Scale(new SizeF(designScale, designScale));
		}
	}

	private void CentreContent()
	{
		if (_contentWidth <= 0 || Width <= 0 || Width == _centredForWidth)
		{
			return;
		}
		_centredForWidth = Width;
		float scale = DesignScale;
		if (_designLeft == null)
		{
			_designLeft = Controls.Cast<Control>().ToDictionary((Control c) => c, (Control c) => (int)Math.Round((float)c.Left / scale));
		}
		if (_designLeft.Count > Controls.Count)
		{
			HashSet<Control> live = new HashSet<Control>(Controls.Cast<Control>());
			foreach (Control item in _designLeft.Keys.Where((Control k) => !live.Contains(k)).ToList())
			{
				_designLeft.Remove(item);
			}
		}
		int num = Math.Max(0, (Width - (int)Math.Round((float)_contentWidth * scale)) / 2);
		foreach (Control control in Controls)
		{
			if (control != _headerCard)
			{
				if (!_designLeft.TryGetValue(control, out var value))
				{
					value = (int)Math.Round((float)(control.Left - CurrentOffset()) / scale);
					_designLeft[control] = value;
				}
				control.Left = (int)Math.Round((float)value * scale) + num;
			}
		}
		_lastOffset = num;
	}

	private int CurrentOffset()
	{
		return _lastOffset;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		using (SolidBrush brush = new SolidBrush(Theme.Background))
		{
			e.Graphics.FillRectangle(brush, ClientRectangle);
		}
		Graphics g = e.Graphics;
		PaintGuard.Try(this, () =>
		{
			AppBackground.Paint(g, FieldOffset.X, FieldOffset.Y);
			Field?.Paint(g, FieldOffset.X, FieldOffset.Y);
			PaintHeaderScrim(g);
		});
		base.OnPaint(e);
	}

	private void PaintHeaderScrim(Graphics g)
	{
		if (Width <= 0 || _headerCard != null)
		{
			return;
		}
		Rectangle rect = new Rectangle(0, 0, Width, 102);
		if (rect.Height <= 0)
		{
			return;
		}
		int alpha = (SlateSurface.Active ? 215 : 150);
		using LinearGradientBrush brush = new LinearGradientBrush(rect, Color.FromArgb(alpha, Theme.Background), Color.FromArgb(0, Theme.Background), LinearGradientMode.Vertical);
		g.FillRectangle(brush, rect);
	}
}
