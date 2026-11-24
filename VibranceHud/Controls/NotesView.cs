using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class NotesView : Control
{
	internal readonly record struct Block(string Lead, string Text, bool Bullet, bool Heading, bool GapBefore);

	private const int LanePx = 8;

	private const int ThumbPx = 4;

	private const int ThumbMinPx = 24;

	private const int ParagraphGapPx = 8;

	private const int LinesPerNotch = 3;

	private string _text = "";

	private int _scroll;

	private bool _thumbHot;

	private bool _dragging;

	private int _dragFrom;

	private int _dragScrollFrom;

	private readonly List<(Point At, string Word, bool Strong)> _laid = new List<(Point, string, bool)>();

	private readonly List<Point> _dots = new List<Point>();

	private int _laidFor = -1;

	private int _contentHeight;

	private int _headingLimit = 48;

	private bool _onCard;

	internal const int DefaultHeadingLimit = 48;

	private static readonly Regex LeadIn = new Regex("^(?<lead>[A-Z][^:.!?\\r\\n]{0,38}:)\\s+(?<rest>\\S.*)$", RegexOptions.Singleline);

	public string NotesText
	{
		get
		{
			return _text;
		}
		set
		{
			_text = value ?? "";
			_laidFor = -1;
			_scroll = 0;
			Invalidate();
		}
	}

	public int HeadingLimit
	{
		get
		{
			return _headingLimit;
		}
		set
		{
			if (value != _headingLimit)
			{
				_headingLimit = value;
				_laidFor = -1;
				Invalidate();
			}
		}
	}

	public bool OnCard
	{
		get
		{
			return _onCard;
		}
		set
		{
			if (value != _onCard)
			{
				_onCard = value;
				if (value)
				{
					BackColor = Color.Transparent;
				}
				else
				{
					ResetBackColor();
				}
				Invalidate();
			}
		}
	}

	private int Dpi => ControlInk.Dpi(this);

	private int TextWidth => Math.Max(1, Width - Px(8));

	private int MaxScroll => Math.Max(0, _contentHeight - Height);

	private int LinePx => Px(20);

	private Rectangle Thumb
	{
		get
		{
			Lay();
			if (_contentHeight <= Height || Height <= 0)
			{
				return Rectangle.Empty;
			}
			int num = Math.Max(Px(24), (int)((long)Height * (long)Height / _contentHeight));
			int num2 = Height - num;
			int y = (int)((MaxScroll != 0) ? ((long)num2 * (long)_scroll / MaxScroll) : 0);
			int num3 = Px(4);
			return new Rectangle(Width - Px(8) + (Px(8) - num3) / 2, y, num3, num);
		}
	}

	private Rectangle Lane => new Rectangle(Width - Px(8), 0, Px(8), Height);

	internal int ContentHeightForTest
	{
		get
		{
			Lay();
			return _contentHeight;
		}
	}

	internal int ScrollForTest => _scroll;

	internal Rectangle ThumbForTest => Thumb;

	public NotesView()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.Selectable, value: false);
		TabStop = false;
		Font = Fonts.Body;
	}

	private int Px(int logical)
	{
		return ControlInk.Px(Dpi, logical);
	}

	private void Lay()
	{
		if (_laidFor == Width)
		{
			return;
		}
		_laid.Clear();
		_dots.Clear();
		int textWidth = TextWidth;
		int num = 0;
		int num2 = Px(8);
		int num3 = Px(4);
		int linePx = LinePx;
		Font body = Fonts.Body;
		Font bodyBold = Fonts.BodyBold;
		int num4 = Math.Max(1, TextRenderer.MeasureText("a a", body, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width - TextRenderer.MeasureText("aa", body, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width);
		int num5 = Px(16);
		foreach (Block item5 in Reflow(_text, _headingLimit))
		{
			if (_laid.Count > 0 || _dots.Count > 0)
			{
				num += (item5.GapBefore ? num2 : num3);
			}
			int num6 = (item5.Bullet ? num5 : 0);
			if (item5.Bullet)
			{
				_dots.Add(new Point(0, num));
			}
			List<(string, bool)> list = new List<(string, bool)>();
			string[] array = item5.Lead.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			foreach (string item in array)
			{
				list.Add((item, true));
			}
			array = item5.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			foreach (string item2 in array)
			{
				list.Add((item2, item5.Heading));
			}
			int num7 = num6;
			foreach (var item6 in list)
			{
				string item3 = item6.Item1;
				bool item4 = item6.Item2;
				int width = TextRenderer.MeasureText(item3, item4 ? bodyBold : body, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
				if (num7 > num6 && num7 + width > textWidth)
				{
					num7 = num6;
					num += linePx;
				}
				_laid.Add((new Point(num7, num), item3, item4));
				num7 += width + num4;
			}
			num += linePx;
		}
		_contentHeight = num;
		_laidFor = Width;
		_scroll = Math.Clamp(_scroll, 0, MaxScroll);
	}

	private void ScrollBy(int pixels)
	{
		Lay();
		int num = Math.Clamp(_scroll + pixels, 0, MaxScroll);
		if (num != _scroll)
		{
			_scroll = num;
			Invalidate();
		}
	}

	public bool ScrollByKey(Keys key)
	{
		switch (key)
		{
		case Keys.Up:
			ScrollBy(-LinePx);
			return true;
		case Keys.Down:
			ScrollBy(LinePx);
			return true;
		case Keys.Prior:
			ScrollBy(-Math.Max(LinePx, Height - LinePx));
			return true;
		case Keys.Next:
			ScrollBy(Math.Max(LinePx, Height - LinePx));
			return true;
		case Keys.Home:
			ScrollBy(-1073741823);
			return true;
		case Keys.End:
			ScrollBy(1073741823);
			return true;
		default:
			return false;
		}
	}

	public void Wheel(int delta)
	{
		ScrollBy(-delta * 3 * LinePx / 120);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		HandledMouseEventArgs e2 = (e as HandledMouseEventArgs) ?? new HandledMouseEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta);
		e2.Handled = true;
		base.OnMouseWheel(e2);
		Wheel(e.Delta);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_dragging)
		{
			Rectangle thumb = Thumb;
			int num = Math.Max(1, Height - thumb.Height);
			int num2 = _dragScrollFrom + (int)((long)(e.Y - _dragFrom) * (long)MaxScroll / num);
			ScrollBy(num2 - _scroll);
			return;
		}
		bool flag = Lane.Contains(e.Location) && !Thumb.IsEmpty;
		if (flag != _thumbHot)
		{
			_thumbHot = flag;
			Invalidate(Lane);
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		if (_thumbHot)
		{
			_thumbHot = false;
			Invalidate(Lane);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		Rectangle thumb = Thumb;
		if (e.Button == MouseButtons.Left && !thumb.IsEmpty && Lane.Contains(e.Location))
		{
			if (e.Y < thumb.Top)
			{
				ScrollByKey(Keys.Prior);
				return;
			}
			if (e.Y >= thumb.Bottom)
			{
				ScrollByKey(Keys.Next);
				return;
			}
			_dragging = true;
			_dragFrom = e.Y;
			_dragScrollFrom = _scroll;
			Capture = true;
			Invalidate(Lane);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (_dragging)
		{
			_dragging = false;
			Capture = false;
			Invalidate(Lane);
		}
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		_laidFor = -1;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Lay();
		Graphics graphics = e.Graphics;
		if (!_onCard)
		{
			using SolidBrush brush = new SolidBrush(ControlInk.Overlay);
			graphics.FillRectangle(brush, ClientRectangle);
		}
		Font body = Fonts.Body;
		Font bodyBold = Fonts.BodyBold;
		int linePx = LinePx;
		int num = Px(4);
		SmoothingMode smoothingMode = graphics.SmoothingMode;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using (SolidBrush brush2 = new SolidBrush(ControlInk.Text2))
		{
			foreach (Point dot in _dots)
			{
				int num2 = dot.Y - _scroll;
				if (num2 + linePx >= 0 && num2 <= Height)
				{
					graphics.FillEllipse(brush2, dot.X + Px(4), (float)num2 + (float)(linePx - num) / 2f, num, num);
				}
			}
		}
		graphics.SmoothingMode = smoothingMode;
		foreach (var item4 in _laid)
		{
			Point item = item4.At;
			string item2 = item4.Word;
			bool item3 = item4.Strong;
			int num3 = item.Y - _scroll;
			if (num3 + linePx >= 0 && num3 <= Height)
			{
				TextRenderer.DrawText(graphics, item2, item3 ? bodyBold : body, new Point(item.X, num3), item3 ? ControlInk.Text1 : ControlInk.Text2, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
			}
		}
		Rectangle thumb = Thumb;
		if (thumb.IsEmpty)
		{
			return;
		}
		Color color;
		if (_dragging)
		{
			color = ControlInk.Text1;
		}
		else
		{
			color = (_thumbHot ? ControlInk.Text2 : ControlInk.Edge);
		}
		SmoothingMode smoothingMode2 = graphics.SmoothingMode;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		using (SolidBrush brush3 = new SolidBrush(color))
		{
			using GraphicsPath path = Glass.RoundedPath(thumb, (float)thumb.Width / 2f);
			graphics.FillPath(brush3, path);
		}
		graphics.SmoothingMode = smoothingMode2;
	}

	internal static List<Block> Reflow(string plain)
	{
		return Reflow(plain, 48);
	}

	internal static List<Block> Reflow(string plain, int headingLimit)
	{
		List<Block> blocks = new List<Block>();
		string[] array = (plain ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		StringBuilder text = new StringBuilder();
		bool bullet = false;
		bool gap = false;
		bool open = false;
		int lineCount = 0;
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length == 0)
			{
				Flush(followedByBlank: true);
				gap = true;
				continue;
			}
			if (text2[0] == '•' || ((text2[0] == '-' || text2[0] == '*') && text2.Length > 1 && char.IsWhiteSpace(text2[1])))
			{
				Flush(followedByBlank: false);
				bullet = true;
				text2 = text2.Substring(1).TrimStart();
			}
			else if (!open)
			{
				bullet = false;
			}
			if (open)
			{
				text.Append(' ');
			}
			text.Append(text2);
			open = true;
			lineCount++;
		}
		Flush(followedByBlank: true);
		return blocks;
		void Flush(bool followedByBlank)
		{
			if (open)
			{
				string text3 = text.ToString().Trim();
				string lead = "";
				if (bullet)
				{
					Match match = LeadIn.Match(text3);
					if (match.Success)
					{
						lead = match.Groups["lead"].Value;
						text3 = match.Groups["rest"].Value;
					}
				}
				int num;
				if (((!bullet && lineCount == 1) & followedByBlank) && text3.Length <= headingLimit && !text3.Contains("://"))
				{
					string text4 = text3;
					num = ((".:!?".IndexOf(text4[text4.Length - 1]) < 0) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				bool heading = (byte)num != 0;
				blocks.Add(new Block(lead, text3, bullet, heading, gap && blocks.Count > 0));
				text.Clear();
				open = false;
				gap = false;
				lineCount = 0;
			}
		}
	}

	internal void WheelForTest(int delta)
	{
		Wheel(delta);
	}
}
