using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Updates;

namespace VibranceHud;

public sealed class WhatsNewWindow : SignalDialogWindow
{
	private static class Shape
	{
		public const int Width = 560;

		public const int Pad = 24;

		public const int Radius = 8;

		public const int TitleY = 23;

		public const int BodyY = 60;

		public const int BodyToContent = 16;

		public const int ContentToFooter = 24;

		public const int ButtonMinWidth = 96;

		public const int ButtonPad = 20;

		public const int FooterBottom = 20;

		public const int VoteRow = 100;
	}

	internal const bool VotingIsShown = false;

	private const int HeightWithVote = 520;

	private const int HeightWithoutVote = 420;

	private const int MinHeight = 240;

	private const double MaxShareOfWorkingArea = 0.7;

	private readonly VoteButton? _thumbUp;

	private readonly VoteButton? _thumbDown;

	private readonly GlassTextBox? _note;

	private readonly NotesView _notes;

	private readonly string _version;

	private readonly int _dpi;

	private bool? _liked;

	internal static int? WorkingAreaHeightForTest { get; set; }

	protected override bool Square => true;

	internal bool? ChosenVoteForTest => _liked;

	internal string NotesTextForTest => _notes.NotesText;

	internal Func<string, bool, string?, Task> Sender { get; set; } = (string version2, bool liked, string? note) => UpdateVoteClient.SendAsync(version2, liked, note);

	private static int WorkingAreaHeight()
	{
		return WorkingAreaHeightForTest ?? Screen.PrimaryScreen?.WorkingArea.Height ?? 1080;
	}

	private new int S(int logical)
	{
		return Tokens.ScaleAt(_dpi, logical);
	}

	public WhatsNewWindow(Version version, string notes)
	{
		_version = version?.ToString() ?? string.Empty;
		_dpi = SignalInk.DpiFor(this);
		StartPosition = FormStartPosition.CenterScreen;
		ShowInTaskbar = true;
		TopMost = true;
		Text = $"What's new in {version}";
		AccessibleName = Text;
		int num = S(560);
		int num2 = S(24);
		int width = num - 2 * num2;
		ClientSize = new Size(num, S(240));
		Label value = SignalDialogWindow.TitleLabel($"What's new in {version}", num2 + S(2), S(23), width);
		Controls.Add(value);
		int height = Math.Max(S(20), TextRenderer.MeasureText("PlexusX updated itself in the background - nothing for you to do.", Fonts.Body, new Size(width, int.MaxValue), TextFormatFlags.WordBreak).Height);
		Label label = SignalDialogWindow.BodyLabel("PlexusX updated itself in the background - nothing for you to do.", num2 + S(2), S(60), width, height);
		Controls.Add(label);
		GlassButton glassButton = new GlassButton
		{
			Text = "OK",
			Kind = GlassButtonKind.Primary,
			Name = "WhatsNewOk"
		};
		int num3 = SignalDialogWindow.TwinButtonWidth(glassButton.Text, glassButton.Font, S(20), S(96));
		int num4 = S(32);
		int num5 = label.Bottom + S(16);
		_notes = new NotesView
		{
			NotesText = (string.IsNullOrWhiteSpace(notes) ? "Thanks for updating.\r\n\r\nNo notes were published for this version. Everything from previous releases carries over - your profiles, hotkeys and settings are untouched.\r\n\r\nFor changes, help or to report something broken, join the Discord:\r\nhttps://plexusx.lol/discord" : ReleaseNotesText.ToPlainText(notes))
		};
		int num6 = S(24) + num4 + S(20);
		int num7 = MeasureNotes(_notes.NotesText, width, ControlInk.Dpi(_notes));
		int num8 = S(240);
		int max = Math.Max(num8, (int)((double)WorkingAreaHeight() * 0.7));
		int num9 = Math.Clamp(num5 + num7 + num6, num8, max);
		ClientSize = new Size(num, num9);
		glassButton.Bounds = new Rectangle(num - num2 - num3, num9 - S(20) - num4, num3, num4);
		glassButton.Click += (object? s, EventArgs e) =>
		{
			SendVoteIfChosen();
			Close();
		};
		int num10 = glassButton.Top - S(24);
		_notes.Bounds = new Rectangle(num2, num5, width, Math.Max(S(40), num10 - num5));
		Controls.Add(_notes);
		Controls.Add(glassButton);
		ActiveControl = glassButton;
		KeyDown += (object? s, KeyEventArgs e) =>
		{
			Keys keyCode = e.KeyCode;
			if ((keyCode == Keys.Return || keyCode == Keys.Escape) ? true : false)
			{
				Close();
			}
			else if (_notes.ScrollByKey(e.KeyCode))
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		};
		WindowDrag.Enable(this, this);
	}

	internal static int MeasureNotes(string plainNotes, int width, int dpi)
	{
		int num = Math.Max(1, width - Px(8));
		int num2 = Px(8);
		int num3 = Px(4);
		int num4 = Px(20);
		int num5 = Px(16);
		Font body = Fonts.Body;
		Font bodyBold = Fonts.BodyBold;
		int num6 = Math.Max(1, TextRenderer.MeasureText("a a", body, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width - TextRenderer.MeasureText("aa", body, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width);
		int num7 = 0;
		bool flag = false;
		foreach (NotesView.Block item5 in NotesView.Reflow(plainNotes))
		{
			if (flag)
			{
				num7 += (item5.GapBefore ? num2 : num3);
			}
			int num8 = (item5.Bullet ? num5 : 0);
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
			if (item5.Bullet || list.Count > 0)
			{
				flag = true;
			}
			int num9 = num8;
			foreach (var item6 in list)
			{
				string item3 = item6.Item1;
				bool item4 = item6.Item2;
				int width2 = TextRenderer.MeasureText(item3, item4 ? bodyBold : body, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
				if (num9 > num8 && num9 + width2 > num)
				{
					num9 = num8;
					num7 += num4;
				}
				num9 += width2 + num6;
			}
			num7 += num4;
		}
		return num7;
		int Px(int logical)
		{
			return ControlInk.Px(dpi, logical);
		}
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		_notes.Wheel(e.Delta);
	}

	internal void ChooseVoteForTest(bool liked)
	{
		ChooseVote(liked);
	}

	private void ChooseVote(bool liked)
	{
	}

	internal void SendVoteIfChosen()
	{
	}
}
