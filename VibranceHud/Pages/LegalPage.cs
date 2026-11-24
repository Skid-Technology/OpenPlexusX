using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;

namespace VibranceHud.Pages;

public sealed class LegalPage : GlowPage
{
	internal static readonly (string Title, string Resource)[] Documents = new (string, string)[4]
	{
		("Terms of use", "EULA.md"),
		("Privacy", "PRIVACY.md"),
		("Licence", "LICENSE.md"),
		("Third-party notices", "THIRD-PARTY-NOTICES.md")
	};

	internal const string CouldNotShow = "This document couldn't be shown. A copy is installed next to PlexusX.exe.";

	private const int HeadingLimit = 90;

	private readonly CardPanel _card;

	private readonly Label _caption;

	private readonly GlassDropdown _picker;

	private readonly NotesView _notes;

	private readonly GlassButton _back;

	private bool _laying;

	private bool _built;

	private static readonly Regex Rule = new Regex("^(-{3,}|\\*{3,}|_{3,})$", RegexOptions.Compiled);

	private static readonly Regex TableSeparator = new Regex("^\\|?[\\s:|\\-]+\\|?$", RegexOptions.Compiled);

	private static readonly Regex Numbered = new Regex("^\\d+[.)]\\s+", RegexOptions.Compiled);

	private static readonly Regex Link = new Regex("\\[([^\\]]+)\\]\\(([^)\\s]+)\\)", RegexOptions.Compiled);

	private static readonly Regex Italic = new Regex("(?<![\\*\\w])\\*(?!\\s)([^*\\n]+?)(?<!\\s)\\*(?![\\*\\w])", RegexOptions.Compiled);

	protected override string? PageTitle => "Legal & licences";

	protected override string PageSubtitle => "The terms of use, the privacy policy and the licences of the parts PlexusX uses.";

	protected override bool UsesHeaderPanel => true;

	protected override Control[] HeaderActions => new Control[1] { _back };

	protected override bool LaysOutInLogicalPixels => false;

	private static int S(int logical)
	{
		return Tokens.Scale(logical);
	}

	public LegalPage(Action? onBack = null)
	{
		LegalPage legalPage = this;
		AutoScroll = false;
		SetStyle(ControlStyles.Selectable, value: true);
		TabStop = false;
		_back = new GlassButton
		{
			Text = "Back",
			Kind = GlassButtonKind.Secondary
		};
		_back.Click += (object? _, EventArgs _) =>
		{
			onBack?.Invoke();
		};
		SizeBack();
		_card = new CardPanel
		{
			Name = "LegalCard"
		};
		_caption = UiHelpers.Caption("Document", 0, 0, 100);
		_caption.UseMnemonic = false;
		_card.Controls.Add(_caption);
		_picker = new GlassDropdown
		{
			AccessibleName = "Document"
		};
		_picker.SetItems(Documents.Select(((string Title, string Resource) d) => d.Title));
		_card.Controls.Add(_picker);
		_notes = new NotesView
		{
			OnCard = true,
			HeadingLimit = 90,
			AccessibleName = "Document text",
			AccessibleRole = AccessibleRole.Document
		};
		_notes.MouseDown += (object? _, MouseEventArgs _) =>
		{
			if (!legalPage.ContainsFocus)
			{
				legalPage.Focus();
			}
		};
		_card.Controls.Add(_notes);
		_picker.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			legalPage.Show(legalPage._picker.SelectedIndex);
		};
		Controls.Add(_card);
		Show(0);
		_built = true;
	}

	private void SizeBack()
	{
		int height = S(32);
		_back.Height = height;
		_back.Width = Math.Max(S(64), _back.GetPreferredSize(Size.Empty).Width);
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		LayoutContent();
		BeginInvoke(() =>
		{
			if (!IsDisposed && Visible && !ContainsFocus)
			{
				Focus();
			}
		});
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		base.OnLayout(e);
		LayoutContent();
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		LayoutContent();
	}

	private void LayoutContent()
	{
		if (!_built || _laying || IsDisposed || ClientSize.Width <= 0 || ClientSize.Height <= 0)
		{
			return;
		}
		_laying = true;
		try
		{
			int num = _back.Width * 4096 + _back.Height;
			SizeBack();
			if (HasHeaderPanel && num != _back.Width * 4096 + _back.Height)
			{
				LayoutHeaderPanel();
			}
			int contentLeft = GlowPage.ContentLeft;
			int contentTop = ContentTop;
			int num2 = ClientSize.Height + 1 - S(16);
			int num3 = Math.Max(S(240), ContentRight - GlowPage.ContentLeft);
			int num4 = Math.Max(S(120), num2 - contentTop);
			_card.SetBounds(contentLeft, contentTop, num3, num4);
			int num5 = S(20);
			int num6 = S(16);
			int num7 = Math.Max(S(120), num3 - 2 * num5);
			int num8 = Fonts.LineHeight(FontRole.CaptionBold);
			_caption.SetBounds(num5, num6, num7, num8);
			int num9 = num6 + num8 + S(8);
			_picker.SetBounds(num5, num9, Math.Min(S(280), num7), S(32));
			int num10 = num9 + S(32) + S(16);
			_notes.SetBounds(num5, num10, num7, Math.Max(S(40), num4 - num10 - num6));
		}
		finally
		{
			_laying = false;
		}
	}

	private void Show(int index)
	{
		if (index < 0 || index >= Documents.Length)
		{
			index = 0;
		}
		_notes.NotesText = LoadDocument(Documents[index].Resource);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		if (!(e is HandledMouseEventArgs { Handled: not false }))
		{
			_notes.Wheel(e.Delta);
			if (e is HandledMouseEventArgs e3)
			{
				e3.Handled = true;
			}
		}
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
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
		if (ScrollKey(keyData, control))
		{
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	private bool ScrollKey(Keys keyData, Control? focused)
	{
		if ((keyData & Keys.Modifiers) != 0)
		{
			return false;
		}
		bool flag;
		switch (keyData)
		{
		case Keys.End:
		case Keys.Home:
		case Keys.Up:
		case Keys.Down:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag && focused is GlassDropdown)
		{
			return false;
		}
		return _notes.ScrollByKey(keyData);
	}

	internal bool PressKeyForTest(Keys key, Control? focused = null)
	{
		return ScrollKey(key, focused);
	}

	internal static string LoadDocument(string resource)
	{
		return LoadDocument(resource, (string name) => Assembly.GetExecutingAssembly().GetManifestResourceStream(name));
	}

	internal static string LoadDocument(string resource, Func<string, Stream?> open)
	{
		try
		{
			using Stream stream = open(resource);
			if (stream == null)
			{
				return "This document couldn't be shown. A copy is installed next to PlexusX.exe.";
			}
			using StreamReader streamReader = new StreamReader(stream);
			return Normalise(streamReader.ReadToEnd());
		}
		catch (Exception ex)
		{
			try
			{
				CrashLog.Write(ex);
			}
			catch
			{
			}
			return "This document couldn't be shown. A copy is installed next to PlexusX.exe.";
		}
	}

	internal static string Normalise(string markdown)
	{
		List<string> lines = new List<string>();
		bool flag = false;
		bool flag2 = false;
		string[] array = markdown.Replace("\r\n", "\n").Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.StartsWith("```"))
			{
				flag = !flag;
				Blank();
				continue;
			}
			if (flag)
			{
				if (text.Length > 0)
				{
					lines.Add(text);
					lines.Add("");
				}
				continue;
			}
			bool flag3 = text.StartsWith("|");
			if (flag2 && !flag3)
			{
				Blank();
				flag2 = false;
			}
			if (text.Length == 0)
			{
				Blank();
			}
			else if (Rule.IsMatch(text))
			{
				Blank();
			}
			else if (text.StartsWith("#"))
			{
				Blank();
				lines.Add(Inline(text.TrimStart('#').Trim()));
				lines.Add("");
			}
			else if (flag3)
			{
				flag2 = true;
				if (!TableSeparator.IsMatch(text))
				{
					IEnumerable<string> values = from c in text.Trim('|').Split('|')
						select Inline(c.Trim()) into c
						where c.Length > 0
						select c;
					lines.Add("• " + string.Join(" — ", values));
				}
			}
			else if (text.StartsWith("> ") || text == ">")
			{
				lines.Add(Inline(text.TrimStart('>').Trim()));
			}
			else if ((text[0] == '-' || text[0] == '*' || text[0] == '+') && text.Length > 1 && char.IsWhiteSpace(text[1]))
			{
				lines.Add("• " + Inline(text.Substring(1).Trim()));
			}
			else if (Numbered.IsMatch(text))
			{
				Blank();
				lines.Add(Inline(text));
			}
			else
			{
				lines.Add(Inline(text));
			}
		}
		return string.Join("\n", lines).Trim('\n');
		void Blank()
		{
			if (lines.Count > 0)
			{
				List<string> list = lines;
				if (list[list.Count - 1].Length != 0)
				{
					lines.Add("");
				}
			}
		}
	}

	private static string Inline(string s)
	{
		s = Link.Replace(s, (Match m) => (!(m.Groups[1].Value == m.Groups[2].Value)) ? (m.Groups[1].Value + " (" + m.Groups[2].Value + ")") : m.Groups[1].Value);
		s = s.Replace("**", "").Replace("`", "");
		s = Italic.Replace(s, "$1");
		return s;
	}
}
