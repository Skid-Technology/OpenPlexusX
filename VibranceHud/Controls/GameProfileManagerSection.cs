using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class GameProfileManagerSection
{
	private sealed class Row
	{
		public GameProfile Profile;

		public Label Name;

		public Label Detail;

		public GlassButton Rename;

		public GlassButton Delete;
	}

	private readonly Control _parent;

	private readonly Font _bodyFont;

	private readonly Font _captionFont;

	private readonly Func<string, bool>? _isRunning;

	private readonly Func<IReadOnlyList<GameProfile>> _load;

	private readonly Action<GameProfile> _save;

	private readonly Action<string> _remove;

	private readonly Func<string?>? _runningGameName;

	private readonly Action? _saveCurrentLook;

	private readonly Label _caption;

	private readonly Label _empty;

	private readonly GlassButton _saveNow;

	private readonly Label _saveHint;

	private readonly List<Row> _rows = new List<Row>();

	public const string RowNameControl = "profileRowName";

	public const string RowDetailControl = "profileRowDetail";

	public const string SaveNowControl = "profileSaveNow";

	public const string SaveHintControl = "profileSaveHint";

	private bool _visible = true;

	private static int RowHeight => Tokens.Scale(54);

	private static int HeaderHeight => Math.Max(Tokens.Scale(22), Tokens.Scale(28));

	private static int HintHeight => Tokens.Scale(20);

	public int PreferredHeight
	{
		get
		{
			int num = HeaderHeight + Tokens.Scale(6);
			if (_saveHint.Visible)
			{
				num += HintHeight + Tokens.Scale(6);
			}
			return num + ((_rows.Count == 0) ? Tokens.Scale(36) : (_rows.Count * RowHeight));
		}
	}

	public event Action? Changed;

	public GameProfileManagerSection(Control parent, Font captionFont, Font bodyFont, Func<IReadOnlyList<GameProfile>>? load = null, Action<GameProfile>? save = null, Action<string>? remove = null, Func<string, bool>? isRunning = null, Func<string?>? runningGameName = null, Action? saveCurrentLook = null)
	{
		_parent = parent;
		_bodyFont = bodyFont;
		_captionFont = captionFont;
		_isRunning = isRunning;
		_runningGameName = runningGameName;
		_saveCurrentLook = saveCurrentLook;
		_load = load ?? new Func<IReadOnlyList<GameProfile>>(GameProfileStore.Load);
		_save = save ?? new Action<GameProfile>(GameProfileStore.Set);
		_remove = remove ?? new Action<string>(GameProfileStore.Remove);
		_caption = new Label
		{
			Text = UiHelpers.Spaced("SAVED GAME PROFILES"),
			Font = captionFont,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft
		};
		_parent.Controls.Add(_caption);
		_empty = new Label
		{
			Text = "No saved profiles yet. Pick a game above and save the look on screen, and it will show up here.",
			Font = bodyFont,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.TopLeft
		};
		_parent.Controls.Add(_empty);
		_saveNow = new GlassButton
		{
			Text = "Save my colours",
			Kind = GlassButtonKind.Primary,
			Name = "profileSaveNow",
			Visible = false
		};
		_saveNow.Click += (object? _, EventArgs _) =>
		{
			SaveCurrentLookFlow();
		};
		_parent.Controls.Add(_saveNow);
		_saveHint = new Label
		{
			Text = "Open a game and this is where you save the colours you're looking at.",
			Font = bodyFont,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft,
			Name = "profileSaveHint",
			Visible = false
		};
		_parent.Controls.Add(_saveHint);
		Refresh();
	}

	public void Refresh()
	{
		foreach (Row row2 in _rows)
		{
			_parent.Controls.Remove(row2.Name);
			_parent.Controls.Remove(row2.Detail);
			_parent.Controls.Remove(row2.Rename);
			_parent.Controls.Remove(row2.Delete);
			row2.Name.Dispose();
			row2.Detail.Dispose();
			row2.Rename.Dispose();
			row2.Delete.Dispose();
		}
		_rows.Clear();
		foreach (GameProfile item in (from p in _load()
			orderby IsRunningNow(p) descending, ChangesSomething(p) descending
			select p).ThenBy((GameProfile p) => p.DisplayName, StringComparer.OrdinalIgnoreCase).ToList())
		{
			GameProfile captured = item;
			Row row = new Row
			{
				Profile = captured
			};
			bool flag = IsRunningNow(captured);
			row.Name = new Label
			{
				Text = (flag ? (captured.DisplayName + "   ● running now") : captured.DisplayName),
				Font = _bodyFont,
				ForeColor = (flag ? Theme.Accent : Theme.Text),
				BackColor = Color.Transparent,
				AutoSize = false,
				TextAlign = ContentAlignment.MiddleLeft,
				Visible = _visible
			};
			row.Name.Name = "profileRowName";
			_parent.Controls.Add(row.Name);
			row.Detail = new Label
			{
				Text = (ChangesSomething(captured) ? GameProfileSection.Describe(captured) : "Changes nothing yet — set your colours with this game open, then save."),
				Font = _captionFont,
				ForeColor = Theme.TextDim,
				BackColor = Color.Transparent,
				AutoSize = false,
				TextAlign = ContentAlignment.MiddleLeft,
				Visible = _visible
			};
			row.Detail.Name = "profileRowDetail";
			_parent.Controls.Add(row.Detail);
			row.Rename = new GlassButton
			{
				Text = "Rename",
				Kind = GlassButtonKind.Secondary,
				Visible = _visible
			};
			row.Rename.Click += (object? _, EventArgs _) =>
			{
				RenameFlow(captured);
			};
			_parent.Controls.Add(row.Rename);
			row.Delete = new GlassButton
			{
				Text = "Delete",
				Kind = GlassButtonKind.Secondary,
				Visible = _visible
			};
			row.Delete.Click += (object? _, EventArgs _) =>
			{
				DeleteFlow(captured);
			};
			_parent.Controls.Add(row.Delete);
			_rows.Add(row);
		}
		RefreshSaveAffordance();
		Changed?.Invoke();
	}

	private void RefreshSaveAffordance()
	{
		string text = null;
		if (_saveCurrentLook != null)
		{
			try
			{
				text = _runningGameName?.Invoke();
			}
			catch
			{
				text = null;
			}
		}
		bool flag = _visible && _saveCurrentLook != null && !string.IsNullOrWhiteSpace(text);
		_saveNow.Visible = flag;
		if (flag)
		{
			_saveNow.Text = "Save my colours for " + GameProfileSection.Shorten(text);
		}
		bool flag2 = _visible && _saveCurrentLook != null && !flag;
		_saveHint.Visible = flag2;
		_saveHint.Text = ((_rows.Count == 0) ? "No game profiles yet. Open a game, set your colours, then save them here." : "Open a game and this is where you save the colours you're looking at.");
		_empty.Visible = _visible && _rows.Count == 0 && !flag2;
	}

	private void SaveCurrentLookFlow()
	{
		_saveCurrentLook?.Invoke();
		Refresh();
	}

	public void SetVisible(bool visible)
	{
		_visible = visible;
		_caption.Visible = visible;
		RefreshSaveAffordance();
		foreach (Row row in _rows)
		{
			row.Name.Visible = visible;
			row.Rename.Visible = visible;
			row.Delete.Visible = visible;
		}
	}

	private void RenameFlow(GameProfile profile)
	{
		string text = Prompt.Ask("Rename profile", profile.DisplayName);
		if (!string.IsNullOrWhiteSpace(text))
		{
			Rename(profile.GameId, text);
		}
	}

	private void DeleteFlow(GameProfile profile)
	{
		if (GlassDialog.Show(_parent.FindForm(), "Delete this profile?", $"\"{profile.DisplayName}\" will be removed. PlexusX will stop switching to it when {profile.DisplayName} opens. There is no undo - saving a new " + "profile for it is the only way back.", GlassDialogButtons.Destructive, GlassDialogTone.Warning, "Delete profile", "Keep it") == DialogResult.Yes)
		{
			Delete(profile.GameId);
		}
	}

	private void Rename(string gameId, string newName)
	{
		GameProfile gameProfile = _load().FirstOrDefault((GameProfile p) => p.GameId == gameId);
		if (gameProfile != null)
		{
			gameProfile.DisplayName = newName;
			_save(gameProfile);
			Refresh();
		}
	}

	private void Delete(string gameId)
	{
		_remove(gameId);
		Refresh();
	}

	internal void TestRename(string gameId, string newName)
	{
		Rename(gameId, newName);
	}

	internal void TestDelete(string gameId)
	{
		Delete(gameId);
	}

	private bool IsRunningNow(GameProfile p)
	{
		try
		{
			return _isRunning?.Invoke(p.GameId) ?? false;
		}
		catch
		{
			return false;
		}
	}

	private static bool ChangesSomething(GameProfile p)
	{
		return GameProfileSection.ChangesAnything(p);
	}

	public void Place(int x, int y, int width)
	{
		int num = Tokens.Scale(22);
		int headerHeight = HeaderHeight;
		int num2 = Tokens.Scale(8);
		int num3 = Math.Min(Tokens.Scale(196), Math.Max(Tokens.Scale(120), width / 2));
		int num4 = Tokens.Scale(28);
		_caption.SetBounds(x, y + (headerHeight - num) / 2, Math.Max(Tokens.Scale(40), width - num3 - num2), num);
		_saveNow.SetBounds(x + width - num3, y + (headerHeight - num4) / 2, num3, num4);
		y += headerHeight + Tokens.Scale(6);
		_saveHint.SetBounds(x, y, width, HintHeight);
		if (_saveHint.Visible)
		{
			y += HintHeight + Tokens.Scale(6);
		}
		_empty.SetBounds(x, y, width, Tokens.Scale(36));
		if (_rows.Count == 0)
		{
			return;
		}
		int num5 = Tokens.Scale(76);
		int num6 = Tokens.Scale(28);
		int num7 = Tokens.Scale(8);
		int rowHeight = RowHeight;
		int num8 = width - 2 * num5 - 2 * num7;
		int num9 = Tokens.Scale(20);
		int num10 = Math.Max(Tokens.Scale(18), TextRenderer.MeasureText("Hg", Fonts.Body).Height);
		int num11 = (rowHeight - (num9 + num10)) / 2;
		foreach (Row row in _rows)
		{
			row.Name.SetBounds(x, y + num11, num8, num9);
			row.Detail.SetBounds(x, y + num11 + num9, num8, num10);
			row.Rename.SetBounds(x + num8 + num7, y + (rowHeight - num6) / 2, num5, num6);
			row.Delete.SetBounds(x + num8 + 2 * num7 + num5, y + (rowHeight - num6) / 2, num5, num6);
			y += rowHeight;
		}
	}
}
