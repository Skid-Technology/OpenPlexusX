using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class LibraryToast : Control
{
	private const int PadLeft = 16;

	private const int PadRight = 8;

	private const int Gap = 8;

	private const int IconSize = 20;

	private const int CloseBox = 28;

	private const int UndoPad = 8;

	private const int ButtonH = 28;

	private const int MinWidth = 360;

	private const int MaxWidth = 560;

	private readonly GlassButton _undo;

	private readonly GlassButton _close;

	public string Message { get; private set; } = "";

	internal (string Lead, string Tail) Parts
	{
		get
		{
			int num = Message.IndexOf(" · ", StringComparison.Ordinal);
			if (num <= 0)
			{
				return (Lead: "", Tail: Message);
			}
			return (Lead: Message.Substring(0, num), Tail: Message.Substring(num));
		}
	}

	private static int UndoWidth => Advance("Undo", Fonts.BodyBold) + 2 * S(8);

	internal int PreferredWidth
	{
		get
		{
			(string Lead, string Tail) parts = Parts;
			string item = parts.Lead;
			string item2 = parts.Tail;
			int num = ((item.Length > 0) ? Advance(item, Fonts.BodyBold) : 0) + Advance(item2, Fonts.Body);
			return Math.Clamp(S(16) + S(20) + S(8) + num + S(8) + UndoWidth + S(8) + S(28) + S(8), S(360), S(560));
		}
	}

	public event EventHandler? UndoRequested;

	public event EventHandler? CloseRequested;

	private static int S(int logical)
	{
		return Tokens.Scale(logical);
	}

	public LibraryToast()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Height = 48;
		Visible = false;
		_undo = new GlassButton
		{
			Kind = GlassButtonKind.Quiet,
			Text = "Undo",
			AccessibleName = "Undo",
			TabIndex = 0
		};
		_undo.Click += (object? _, EventArgs _) =>
		{
			UndoRequested?.Invoke(this, EventArgs.Empty);
		};
		_close = new GlassButton
		{
			Kind = GlassButtonKind.Icon,
			Icon = SignalIcon.Close,
			AccessibleName = "Close",
			TabIndex = 1
		};
		_close.Click += (object? _, EventArgs _) =>
		{
			CloseRequested?.Invoke(this, EventArgs.Empty);
		};
		Controls.Add(_undo);
		Controls.Add(_close);
		LayOut();
	}

	public void Show(string message)
	{
		Message = message;
		Visible = true;
		LayOut();
		BringToFront();
		Invalidate();
	}

	protected override void OnLayout(LayoutEventArgs levent)
	{
		base.OnLayout(levent);
		LayOut();
	}

	public void ReassertOnTop()
	{
		BringToFront();
	}

	internal void TestPressUndo()
	{
		UndoRequested?.Invoke(this, EventArgs.Empty);
	}

	internal void TestPressClose()
	{
		CloseRequested?.Invoke(this, EventArgs.Empty);
	}

	private void LayOut()
	{
		if (_undo != null && _close != null)
		{
			int num = S(28);
			_close.SetBounds(Width - S(8) - num, (Height - num) / 2, num, num);
			int undoWidth = UndoWidth;
			int num2 = S(28);
			_undo.SetBounds(_close.Left - S(8) - undoWidth, (Height - num2) / 2, undoWidth, num2);
		}
	}

	private static int Advance(string text, Font font)
	{
		return TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		LayOut();
		RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
		float radius = S(8);
		if (SlateSurface.Active)
		{
			Glass.PaintPanel(graphics, rect, radius, Glass.BackdropOffsetOf(this), 255, 200, exactAlpha: true);
		}
		else
		{
			using GraphicsPath path = Glass.RoundedPath(rect, radius);
			using (SolidBrush brush = new SolidBrush(SignalTokens.Ash.Overlay))
			{
				graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen(SignalTokens.Ash.WindowEdge, S(1));
			graphics.DrawPath(pen, path);
		}
		int num = S(20);
		int num2 = S(16);
		LineIcons.Success(graphics, new Rectangle(num2, (Height - num) / 2, num, num), Theme.Positive);
		num2 += num + S(8);
		int num3 = Math.Max(0, _undo.Left - S(8) - num2);
		var (text, text2) = Parts;
		if (text.Length > 0)
		{
			int num4 = Math.Min(num3, Advance(text, Fonts.BodyBold));
			TextRenderer.DrawText(graphics, text, Fonts.BodyBold, new Rectangle(num2, 0, num4, Height), Theme.Text, TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			num2 += num4;
			num3 -= num4;
		}
		TextRenderer.DrawText(graphics, text2, Fonts.Body, new Rectangle(num2, 0, Math.Max(0, num3), Height), Theme.TextDim, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		base.OnPaint(e);
	}
}
