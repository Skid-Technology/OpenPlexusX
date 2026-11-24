using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Monitors;

namespace VibranceHud.Controls;

public sealed class MonitorSelectionTile : Control
{
	private sealed class TileAccessible : ControlAccessibleObject
	{
		private readonly MonitorSelectionTile _tile;

		public override AccessibleStates State => (AccessibleStates)((int)base.State | (_tile.Checked ? 16 : 0));

		public override string DefaultAction => "Toggle selection";

		public TileAccessible(MonitorSelectionTile tile)
			: base(tile)
		{
			_tile = tile;
		}

		public override void DoDefaultAction()
		{
			_tile.Request();
		}
	}

	private bool _checked;

	public ColourMonitorGroup Group { get; set; }

	public int Number { get; }

	public bool Checked
	{
		get
		{
			return _checked;
		}
		set
		{
			if (_checked != value)
			{
				_checked = value;
				Invalidate();
				AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
			}
		}
	}

	private string Detail
	{
		get
		{
			if (!Group.IsClone)
			{
				return Group.Bounds.Width + " × " + Group.Bounds.Height + ((Group.RefreshHz > 0) ? (" · " + Group.RefreshHz + " Hz") : "") + (Group.Primary ? " · main" : "");
			}
			return "Mirrored screens — selected together";
		}
	}

	public event Action<bool>? SelectionRequested;

	public MonitorSelectionTile(ColourMonitorGroup group, int number)
	{
		Group = group;
		Number = number;
		Name = "monitor-tile-" + number;
		AccessibleName = group.Name;
		AccessibleRole = AccessibleRole.CheckButton;
		TabStop = true;
		Cursor = Cursors.Hand;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Font = Fonts.BodyBold;
	}

	private int TextWidth(int width)
	{
		return Math.Max(Tokens.Scale(32), width - Tokens.Scale(132));
	}

	private static Size Measure(string text, Font font, int width)
	{
		return TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
	}

	public override Size GetPreferredSize(Size proposedSize)
	{
		int width = ((proposedSize.Width > 0) ? proposedSize.Width : Tokens.Scale(360));
		int width2 = TextWidth(width);
		int num = Measure(Group.Name, Fonts.BodyBold, width2).Height + Tokens.Scale(3) + Measure(Detail, Fonts.Caption, width2).Height;
		return new Size(width, Math.Max(Tokens.Scale(68), num + Tokens.Scale(28)));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		int num = Tokens.Scale(14);
		int num2 = Tokens.Scale(58);
		int num3 = Tokens.Scale(38);
		int num4 = Tokens.Scale(12);
		using SolidBrush brush = new SolidBrush(Checked ? Theme.Surface : Theme.Background);
		using Pen pen = new Pen(Checked ? Theme.Text : Theme.Border);
		Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
		graphics.FillRectangle(brush, rect);
		graphics.DrawRectangle(pen, rect);
		Rectangle rectangle = new Rectangle(num, (Height - num3) / 2, num2, num3);
		graphics.DrawRectangle(pen, rectangle);
		TextRenderer.DrawText(graphics, Number.ToString(CultureInfo.InvariantCulture), Fonts.Title, rectangle, Checked ? Theme.Text : Theme.TextDim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		using SolidBrush brush2 = new SolidBrush(Checked ? Theme.Text : Theme.TextDim);
		graphics.FillRectangle(brush2, rectangle.Left + num2 / 2 - Tokens.Scale(5), rectangle.Bottom, Tokens.Scale(10), Tokens.Scale(6));
		int x = num + num2 + num4;
		int width = TextWidth(Width);
		int height = Measure(Group.Name, Fonts.BodyBold, width).Height;
		TextRenderer.DrawText(graphics, Group.Name, Fonts.BodyBold, new Rectangle(x, num, width, height), Theme.Text, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
		TextRenderer.DrawText(graphics, Detail, Fonts.Caption, new Rectangle(x, num + height + Tokens.Scale(3), width, Height - num * 2 - height - Tokens.Scale(3)), Theme.TextDim, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
		int num5 = Tokens.Scale(20);
		Rectangle rect2 = new Rectangle(Width - num - num5, (Height - num5) / 2, num5, num5);
		if (Checked)
		{
			using SolidBrush brush3 = new SolidBrush(Theme.Text);
			graphics.FillRectangle(brush3, rect2);
			using Pen pen2 = new Pen(Theme.Background, Tokens.Scale(2));
			graphics.DrawLines(pen2, new Point[3]
			{
				new Point(rect2.Left + num5 / 4, rect2.Top + num5 / 2),
				new Point(rect2.Left + num5 * 2 / 5, rect2.Top + num5 * 3 / 4),
				new Point(rect2.Left + num5 * 4 / 5, rect2.Top + num5 / 4)
			});
		}
		else
		{
			graphics.DrawRectangle(pen, rect2);
		}
		if (Focused)
		{
			using (Pen pen3 = new Pen(Theme.Text)
			{
				DashStyle = DashStyle.Dot
			})
			{
				graphics.DrawRectangle(pen3, 2, 2, Width - 5, Height - 5);
				return;
			}
		}
	}

	private void Request()
	{
		if (Enabled)
		{
			SelectionRequested?.Invoke(!Checked);
		}
	}

	protected override void OnClick(EventArgs e)
	{
		base.OnClick(e);
		Focus();
		Request();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Space)
		{
			Request();
			e.Handled = true;
			e.SuppressKeyPress = true;
		}
		base.OnKeyDown(e);
	}

	protected override AccessibleObject CreateAccessibilityInstance()
	{
		return new TileAccessible(this);
	}
}
