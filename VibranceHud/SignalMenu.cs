using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;

namespace VibranceHud;

internal static class SignalMenu
{
	internal sealed record RowArt(Action<Graphics, RectangleF, Color> Icon, bool Active);

	internal sealed class Renderer : ToolStripRenderer
	{
		private readonly Func<int> _dpi;

		private readonly bool _checkColumn;

		private readonly bool _square;

		internal bool HasCheckColumn => _checkColumn;

		internal bool IsSquare => _square;

		public Renderer(Func<int> dpi, bool checkColumn, bool square = false)
		{
			_dpi = dpi;
			_checkColumn = checkColumn;
			_square = square;
		}

		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			using SolidBrush brush = new SolidBrush(ControlInk.Overlay);
			e.Graphics.FillRectangle(brush, e.AffectedBounds);
		}

		protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
		{
		}

		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			Graphics graphics = e.Graphics;
			if (_square)
			{
				int width = e.ToolStrip.Width;
				int height = e.ToolStrip.Height;
				int num = ControlInk.Hairline(_dpi());
				using SolidBrush brush = new SolidBrush(SignalDialogWindow.EdgeInk);
				graphics.FillRectangle(brush, 0, 0, width, num);
				graphics.FillRectangle(brush, 0, height - num, width, num);
				graphics.FillRectangle(brush, 0, 0, num, height);
				graphics.FillRectangle(brush, width - num, 0, num, height);
				return;
			}
			SmoothingMode smoothingMode = graphics.SmoothingMode;
			PixelOffsetMode pixelOffsetMode = graphics.PixelOffsetMode;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			int dpi = _dpi();
			int num2 = ControlInk.Hairline(dpi);
			float num3 = (float)num2 / 2f;
			using (GraphicsPath path = Glass.RoundedPath(new RectangleF(num3, num3, e.ToolStrip.Width - num2, e.ToolStrip.Height - num2), ControlInk.PxF(dpi, 8f) - num3))
			{
				using Pen pen = new Pen(SignalDialogWindow.EdgeInk, num2);
				graphics.DrawPath(pen, path);
			}
			graphics.SmoothingMode = smoothingMode;
			graphics.PixelOffsetMode = pixelOffsetMode;
		}

		protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
		{
			if (_square)
			{
				bool flag = e.Item.Tag is RowArt rowArt && rowArt.Active;
				if (e.Item.Enabled && (e.Item.Selected || e.Item.Pressed || flag))
				{
					using (SolidBrush brush = new SolidBrush(e.Item.Pressed ? ControlInk.Pressed : ControlInk.Hover))
					{
						e.Graphics.FillRectangle(brush, 0, 0, e.Item.Width, e.Item.Height);
					}
				}
			}
			else
			{
				if (!e.Item.Enabled || (!e.Item.Selected && !e.Item.Pressed))
				{
					return;
				}
				Graphics graphics = e.Graphics;
				SmoothingMode smoothingMode = graphics.SmoothingMode;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using (GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, e.Item.Width, e.Item.Height), ControlInk.PxF(_dpi(), 4f)))
				{
					using SolidBrush brush2 = new SolidBrush(e.Item.Pressed ? ControlInk.Pressed : ControlInk.Hover);
					graphics.FillPath(brush2, path);
				}
				graphics.SmoothingMode = smoothingMode;
			}
		}

		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			int dpi = _dpi();
			if (_square && e.Item.Tag is RowArt rowArt)
			{
				int num = ControlInk.Px(dpi, 16);
				int num2 = ControlInk.Px(dpi, 12);
				Color color;
				if (rowArt.Active)
				{
					color = Color.White;
				}
				else
				{
					color = (e.Item.Enabled ? ControlInk.Text1 : ControlInk.TextDisabledInk);
				}
				Graphics graphics = e.Graphics;
				SmoothingMode smoothingMode = graphics.SmoothingMode;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				rowArt.Icon(graphics, new RectangleF(num2, (float)(e.Item.Height - num) / 2f, num, num), color);
				graphics.SmoothingMode = smoothingMode;
				int item = SquareRowXs(dpi).LabelX;
				TextRenderer.DrawText(graphics, e.Text, rowArt.Active ? Fonts.BodyBold : Fonts.Body, new Rectangle(item, 0, Math.Max(0, e.Item.Width - item - ControlInk.Px(dpi, 12)), e.Item.Height), color, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
				return;
			}
			int num3 = ControlInk.Px(dpi, 12);
			if (_checkColumn)
			{
				object tag = e.Item.Tag;
				if (tag is bool && (bool)tag)
				{
					int num4 = ControlInk.Px(dpi, 16);
					SignalIcons.Draw(e.Graphics, SignalIcon.Check, new RectangleF(num3, (float)(e.Item.Height - num4) / 2f, num4, num4), ControlInk.Text1);
				}
				num3 += ControlInk.Px(dpi, 16) + ControlInk.Px(dpi, 8);
			}
			string text = ShortcutOf(e.Item);
			if (text == null || !(e.Text == text) || !(e.Text != e.Item.Text))
			{
				Color foreColor = (e.Item.Enabled ? ControlInk.Text1 : ControlInk.TextDisabledInk);
				int num5 = e.Item.Width - ControlInk.Px(dpi, 12);
				if (text != null)
				{
					Font caption = Fonts.Caption;
					int width = TextRenderer.MeasureText(e.Graphics, text, caption, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
					Color foreColor2 = (e.Item.Enabled ? ControlInk.Text3 : ControlInk.TextDisabledInk);
					TextRenderer.DrawText(e.Graphics, text, caption, new Rectangle(num5 - width, 0, width, e.Item.Height), foreColor2, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
					num5 -= width + ControlInk.Px(dpi, 24);
				}
				TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont ?? Fonts.Body, new Rectangle(num3, 0, Math.Max(0, num5 - num3), e.Item.Height), foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			}
		}

		protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
		{
			int dpi = _dpi();
			int height = ControlInk.Hairline(dpi);
			using SolidBrush brush = new SolidBrush(ControlInk.Border);
			e.Graphics.FillRectangle(brush, 0, ControlInk.Px(dpi, 4), e.Item.Width, height);
		}

		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
		{
		}
	}

	internal const int Row = 32;

	internal const int Inset = 4;

	internal const int PadX = 12;

	internal const int CheckSize = 16;

	internal const int CheckGap = 8;

	internal const int MinWidth = 160;

	internal const int Gap = 4;

	internal const int SeparatorAir = 4;

	internal const int ShortcutGap = 24;

	internal const int SquareRow = 36;

	internal const int SquareMinWidth = 180;

	internal const int SquareIcon = 16;

	internal const int SquareIconGap = 8;

	private static readonly PointF[] Cog = new PointF[32]
	{
		new PointF(15.7f, 8.47f),
		new PointF(18f, 8.73f),
		new PointF(18f, 11.27f),
		new PointF(15.7f, 11.53f),
		new PointF(15.11f, 12.95f),
		new PointF(16.55f, 14.76f),
		new PointF(14.76f, 16.55f),
		new PointF(12.95f, 15.11f),
		new PointF(11.53f, 15.7f),
		new PointF(11.27f, 18f),
		new PointF(8.73f, 18f),
		new PointF(8.47f, 15.7f),
		new PointF(7.05f, 15.11f),
		new PointF(5.24f, 16.55f),
		new PointF(3.45f, 14.76f),
		new PointF(4.89f, 12.95f),
		new PointF(4.3f, 11.53f),
		new PointF(2f, 11.27f),
		new PointF(2f, 8.73f),
		new PointF(4.3f, 8.47f),
		new PointF(4.89f, 7.05f),
		new PointF(3.45f, 5.24f),
		new PointF(5.24f, 3.45f),
		new PointF(7.05f, 4.89f),
		new PointF(8.47f, 4.3f),
		new PointF(8.73f, 2f),
		new PointF(11.27f, 2f),
		new PointF(11.53f, 4.3f),
		new PointF(12.95f, 4.89f),
		new PointF(14.76f, 3.45f),
		new PointF(16.55f, 5.24f),
		new PointF(15.11f, 7.05f)
	};

	internal static string? ShortcutOf(ToolStripItem item)
	{
		if (item is ToolStripMenuItem toolStripMenuItem)
		{
			string shortcutKeyDisplayString = toolStripMenuItem.ShortcutKeyDisplayString;
			if (shortcutKeyDisplayString != null && shortcutKeyDisplayString.Length > 0)
			{
				return shortcutKeyDisplayString;
			}
		}
		return null;
	}

	internal static int RowHeight(int dpi, Font font)
	{
		return Math.Max(ControlInk.Px(dpi, 32), font.Height + ControlInk.Px(dpi, 8));
	}

	internal static ToolStripDropDown SelectList(Control owner)
	{
		int dpi = ControlInk.Dpi(owner);
		ToolStripDropDown toolStripDropDown = new ToolStripDropDown
		{
			AutoClose = true,
			DropShadowEnabled = false,
			Padding = new Padding(ControlInk.Px(dpi, 4)),
			BackColor = ControlInk.Overlay,
			Renderer = new Renderer(() => ControlInk.Dpi(owner), checkColumn: true),
			Opacity = 1.0,
			Font = Fonts.Body,
			AccessibleRole = AccessibleRole.List
		};
		RoundWhenOpened(toolStripDropDown, () => ControlInk.Dpi(owner));
		return toolStripDropDown;
	}

	internal static int RowWidth(Control trigger)
	{
		int dpi = ControlInk.Dpi(trigger);
		return Math.Max(trigger.Width, ControlInk.Px(dpi, 160)) - 2 * ControlInk.Px(dpi, 4);
	}

	internal static ToolStripMenuItem SelectItem(string text, bool chosen, int width, int rowHeight)
	{
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(text)
		{
			Font = Fonts.Body,
			ForeColor = ControlInk.Text1,
			AutoSize = false,
			Size = new Size(width, rowHeight),
			TextAlign = ContentAlignment.MiddleLeft,
			Tag = chosen,
			AccessibleRole = AccessibleRole.ListItem
		};
		if (chosen)
		{
			toolStripMenuItem.AccessibleDescription = "Selected";
		}
		return toolStripMenuItem;
	}

	internal static ToolStripSeparator Separator(int width, int dpi)
	{
		return new ToolStripSeparator
		{
			AutoSize = false,
			Size = new Size(width, 2 * ControlInk.Px(dpi, 4) + ControlInk.Hairline(dpi)),
			Margin = Padding.Empty
		};
	}

	internal static void ShowUnder(ToolStripDropDown menu, Control trigger)
	{
		int num = ControlInk.Px(ControlInk.Dpi(trigger), 4);
		int height = menu.GetPreferredSize(Size.Empty).Height;
		Point point = trigger.PointToScreen(new Point(0, trigger.Height + num));
		Rectangle workingArea = Screen.FromControl(trigger).WorkingArea;
		bool flag = point.Y + height <= workingArea.Bottom;
		menu.Show(trigger, flag ? new Point(0, trigger.Height + num) : new Point(0, -height - num));
	}

	internal static ContextMenuStrip ActionMenu(bool square = false)
	{
		ContextMenuStrip menu = new ContextMenuStrip
		{
			ShowImageMargin = false,
			ShowCheckMargin = false,
			DropShadowEnabled = false,
			BackColor = ControlInk.Overlay,
			Font = Fonts.Body,
			Opacity = 1.0
		};
		menu.Padding = new Padding(ControlInk.Px(Dpi(), 4));
		menu.Renderer = new Renderer(Dpi, checkColumn: false, square);
		menu.ItemAdded += (object? _, ToolStripItemEventArgs _) =>
		{
			SizeRows(menu, Dpi(), square);
		};
		menu.Opening += (object? _, CancelEventArgs _) =>
		{
			SizeRows(menu, Dpi(), square);
		};
		if (!square)
		{
			RoundWhenOpened(menu, Dpi);
		}
		return menu;
		int Dpi()
		{
			return SignalInk.DpiFor(menu);
		}
	}

	internal static (int IconRight, int LabelX) SquareRowXs(int dpi)
	{
		int num = ControlInk.Px(dpi, 12);
		int num2 = ControlInk.Px(dpi, 16);
		return (IconRight: num + num2, LabelX: num + num2 + ControlInk.Px(dpi, 8));
	}

	internal static void SizeRows(ToolStripDropDown menu, int dpi, bool square = false)
	{
		int num = ControlInk.Px(dpi, 12);
		int logical = (square ? 180 : 160);
		int num2 = ControlInk.Px(dpi, logical) - 2 * ControlInk.Px(dpi, 4);
		int num3 = (square ? (ControlInk.Px(dpi, 16) + ControlInk.Px(dpi, 8)) : 0);
		foreach (ToolStripItem item in menu.Items)
		{
			if (!(item is ToolStripSeparator))
			{
				int num4 = TextRenderer.MeasureText(item.Text ?? "", square ? Fonts.BodyBold : Fonts.Body).Width + 2 * num + num3;
				string text = ShortcutOf(item);
				if (text != null)
				{
					num4 += ControlInk.Px(dpi, 24) + TextRenderer.MeasureText(text, Fonts.Caption).Width;
				}
				num2 = Math.Max(num2, num4);
			}
		}
		int height = (square ? Math.Max(ControlInk.Px(dpi, 36), Fonts.Body.Height + ControlInk.Px(dpi, 8)) : RowHeight(dpi, Fonts.Body));
		foreach (ToolStripItem item2 in menu.Items)
		{
			item2.AutoSize = false;
			item2.Margin = ((!square) ? Padding.Empty : new Padding(ControlInk.Px(dpi, 4), (item2 == menu.Items[0]) ? Math.Max(0, ControlInk.Px(dpi, 4) - ControlInk.Px(dpi, 2)) : 0, 0, 0));
			if (item2 is ToolStripSeparator)
			{
				item2.Size = new Size(num2, 2 * ControlInk.Px(dpi, 4) + ControlInk.Hairline(dpi));
				continue;
			}
			item2.Font = Fonts.Body;
			item2.Size = new Size(num2, height);
		}
		if (!square)
		{
			return;
		}
		int num5 = ControlInk.Px(dpi, 4);
		int num6 = 0;
		foreach (ToolStripItem item3 in menu.Items)
		{
			num6 += item3.Height;
		}
		menu.AutoSize = false;
		menu.Size = new Size(num2 + 2 * num5, num6 + 2 * num5);
	}

	internal static void DrawSettingsIcon(Graphics g, RectangleF box, Color ink)
	{
		DrawOnGrid(g, box, ink, (Pen pen) =>
		{
			g.DrawPolygon(pen, Cog);
			g.DrawEllipse(pen, 7.6f, 7.6f, 4.8f, 4.8f);
		});
	}

	internal static void DrawAccountIcon(Graphics g, RectangleF box, Color ink)
	{
		DrawOnGrid(g, box, ink, (Pen pen) =>
		{
			g.DrawEllipse(pen, 6.8f, 3.8f, 6.4f, 6.4f);
			g.DrawArc(pen, 3.8f, 10.8f, 12.4f, 12.4f, 180f, 180f);
		});
	}

	private static void DrawOnGrid(Graphics g, RectangleF box, Color ink, Action<Pen> draw)
	{
		GraphicsState gstate = g.Save();
		try
		{
			g.TranslateTransform(box.X, box.Y);
			float num = box.Width / 20f;
			g.ScaleTransform(num, num);
			using Pen obj = new Pen(ink, 1.5f)
			{
				StartCap = LineCap.Round,
				EndCap = LineCap.Round,
				LineJoin = LineJoin.Round
			};
			draw(obj);
		}
		finally
		{
			g.Restore(gstate);
		}
	}

	private static void RoundWhenOpened(ToolStripDropDown menu, Func<int> dpi)
	{
		menu.Opened += (object? _, EventArgs _) =>
		{
			ApplyRound(menu, dpi());
		};
	}

	internal static void ApplyRound(ToolStripDropDown menu, int dpi)
	{
		using GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, menu.Width, menu.Height), ControlInk.PxF(dpi, 8f));
		Region? region = menu.Region;
		menu.Region = new Region(path);
		region?.Dispose();
	}

	internal static void PaintTrigger(Graphics g, Size size, int dpi, Font font, string text, bool placeholder, bool open, float hot, bool enabled, bool chevron = true)
	{
		Color fill;
		Color edge;
		Color foreColor;
		Color ink;
		if (!enabled)
		{
			fill = Color.Transparent;
			edge = ControlInk.TextDisabled;
			foreColor = (ink = ControlInk.TextDisabledInk);
		}
		else
		{
			Color hover = ControlInk.Hover;
			fill = (open ? ControlInk.Pressed : ControlInk.Mix(Color.FromArgb(0, hover), hover, hot));
			edge = ControlInk.Edge;
			foreColor = (placeholder ? ControlInk.Text3 : ControlInk.Text1);
			ink = (open ? ControlInk.Text1 : ControlInk.Mix(ControlInk.Text2, ControlInk.Text1, hot));
		}
		ControlInk.PaintBox(g, size, ControlInk.PxF(dpi, 4f), ControlInk.Hairline(dpi), fill, edge);
		int num = ControlInk.Px(dpi, 12);
		int num2 = ControlInk.Px(dpi, 8);
		int num3 = ControlInk.Px(dpi, 16);
		int num4 = ControlInk.Px(dpi, 12);
		TextRenderer.DrawText(g, text, font, new Rectangle(num, 0, Math.Max(0, size.Width - num - num4 - num3 - num2), size.Height), foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		if (chevron)
		{
			SignalIcons.Draw(g, SignalIcon.Chevron, new RectangleF(size.Width - num2 - num3, (float)(size.Height - num3) / 2f, num3, num3), ink);
		}
	}
}
