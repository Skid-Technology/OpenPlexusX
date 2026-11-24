using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class BrowserWontOpenDialog : SignalDialogWindow
{
	public const string Title = "We couldn't open your browser";

	public const string Body = "PlexusX asked Windows to open your browser and nothing happened. Copy the link below, paste it into your browser, and carry on there.";

	public const string ShortSentence = "We couldn't open your browser. Copy the link and open it yourself.";

	public const string CopyLabel = "Copy link";

	public const string CloseLabel = "Close";

	public const string Copied = "Copied. Paste it into your browser.";

	private const int WidthLogical = 560;

	private const int LinkHeight = 32;

	private const int HintHeight = 16;

	private const int HintGap = 8;

	internal const int Chrome = 208;

	private readonly string _url;

	private readonly string _displayUrl;

	private readonly int _linkHeight;

	private readonly GlassButton _copyButton;

	private readonly Action<string> _copy;

	private string _hint = "";

	internal string HintForTest => _hint;

	internal static Action<string>? InsteadOfShowingForTest { get; set; }

	internal static int MeasureHeight(string body, int width, string url = "")
	{
		int num = Math.Max(80, width - 48);
		int val = (int)Math.Ceiling((double)(SignalDialogWindow.MeasureBody(body, Tokens.Scale(num)) * 96) / (double)Tokens.Dpi);
		int num2 = (int)Math.Ceiling((double)(MeasureLink(url, Tokens.Scale(num - 24), Tokens.Scale(16), out string _) * 96) / (double)Tokens.Dpi);
		return 208 + Math.Min(val, 200) + Math.Max(0, num2 - 32);
	}

	internal BrowserWontOpenDialog(string url, Action<string> copy)
	{
		_url = url ?? "";
		_copy = copy ?? throw new ArgumentNullException("copy");
		ShowInTaskbar = true;
		TopMost = true;
		Text = "PlexusX";
		AccessibleName = "We couldn't open your browser";
		AccessibleDescription = "PlexusX asked Windows to open your browser and nothing happened. Copy the link below, paste it into your browser, and carry on there. " + _url;
		ChromeTitle = "We couldn't open your browser";
		ChromeBody = "PlexusX asked Windows to open your browser and nothing happened. Copy the link below, paste it into your browser, and carry on there.";
		int num = S(560);
		_linkHeight = Math.Max(S(32), MeasureLink(_url, num - 2 * S(24) - S(24), S(16), out _displayUrl));
		ChromeBodyHeight = Math.Min(SignalDialogWindow.MeasureBody("PlexusX asked Windows to open your browser and nothing happened. Copy the link below, paste it into your browser, and carry on there.", num - 2 * S(24)), S(200));
		ClientSize = new Size(num, BodyTop + ChromeBodyHeight + S(16) + _linkHeight + S(8) + S(16) + S(24) + S(32) + S(20));
		GlassButton glassButton = FooterButton("Close", GlassButtonKind.Secondary, "BrowserWontOpenClose");
		glassButton.Click += (object? _, EventArgs _) =>
		{
			Close();
		};
		_copyButton = FooterButton("Copy link", GlassButtonKind.Primary, "BrowserWontOpenCopy");
		_copyButton.Click += (object? _, EventArgs _) =>
		{
			CopyTheLink();
		};
		Controls.Add(glassButton);
		Controls.Add(_copyButton);
		PlaceFooter(glassButton, _copyButton);
		_copyButton.TabIndex = 0;
		glassButton.TabIndex = 1;
		ActiveControl = _copyButton;
		WindowDrag.Enable(this, this);
	}

	private static int MeasureLink(string url, int width, int padding, out string display)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num;
		for (int i = 0; i < url.Length; i += num)
		{
			num = 1;
			int num2 = url.Length - i;
			while (num < num2)
			{
				int num3 = (num + num2 + 1) / 2;
				if (TextRenderer.MeasureText(url.Substring(i, num3), Fonts.Body, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width <= width)
				{
					num = num3;
				}
				else
				{
					num2 = num3 - 1;
				}
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(url, i, num);
		}
		display = stringBuilder.ToString();
		Size proposedSize = new Size(Math.Max(1, width), int.MaxValue);
		return Math.Max(TextRenderer.MeasureText(display, Fonts.Body, proposedSize, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak).Height, TextRenderer.MeasureText(url, Fonts.Body, proposedSize, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak).Height) + padding;
	}

	private void CopyTheLink()
	{
		try
		{
			_copy(_url);
			_hint = "Copied. Paste it into your browser.";
		}
		catch (Exception ex)
		{
			CrashLog.Write(ex);
			_hint = "Windows would not let us copy it. Type the link above into your browser.";
		}
		Invalidate();
	}

	internal void PressCopyForTest()
	{
		CopyTheLink();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Escape)
		{
			e.Handled = true;
			Close();
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		int x = S(24);
		int textWidth = TextWidth;
		Rectangle rect = new Rectangle(x, ContentTop, textWidth, _linkHeight);
		GraphicsState gstate = graphics.Save();
		graphics.TranslateTransform(rect.X, rect.Y);
		ControlInk.PaintBox(graphics, rect.Size, S(4), ControlInk.Hairline(ChromeDpi), ControlInk.FieldFill, ControlInk.Edge);
		graphics.Restore(gstate);
		TextRenderer.DrawText(graphics, _displayUrl, Fonts.Body, Rectangle.Inflate(rect, -S(12), -S(8)), ControlInk.Text1, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak);
		if (_hint.Length > 0)
		{
			TextRenderer.DrawText(graphics, _hint, Fonts.Caption, new Rectangle(x, rect.Bottom + S(8), textWidth, S(16)), ControlInk.Text2, TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
	}

	public static void ShowFor(IWin32Window? owner, string url)
	{
		Action<string> insteadOfShowingForTest = InsteadOfShowingForTest;
		if (insteadOfShowingForTest != null)
		{
			insteadOfShowingForTest(url);
			return;
		}
		using BrowserWontOpenDialog browserWontOpenDialog = new BrowserWontOpenDialog(url, ToTheClipboard);
		if (owner != null)
		{
			browserWontOpenDialog.ShowDialog(owner);
		}
		else
		{
			browserWontOpenDialog.ShowDialog();
		}
	}

	private static void ToTheClipboard(string text)
	{
		if (string.IsNullOrEmpty(text) || TheClipboard.Put(text))
		{
			return;
		}
		throw new InvalidOperationException("Windows would not let go of the clipboard.");
	}
}
