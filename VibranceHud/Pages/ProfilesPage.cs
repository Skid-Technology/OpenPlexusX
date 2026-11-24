using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Games;

namespace VibranceHud.Pages;

public sealed class ProfilesPage : GlowPage
{
	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly GameSelection _selection;

	private readonly IVibranceEngine _engine;

	private readonly Action<DisplayPreset>? _applyLook;

	private readonly Func<string?> _runningGameId;

	private readonly Func<string, GameProfile?> _gameProfileLookup;

	private readonly Action<GameProfile> _gameProfileSave;

	private readonly Func<IReadOnlyList<GameProfile>> _gameProfileLoadAll;

	private readonly Action<string> _gameProfileRemove;

	private readonly Label _title;

	private readonly Label _subtitle;

	private CardPanel? _offerCard;

	private Label? _offerLine;

	private GlassButton? _offerYes;

	private GlassButton? _offerNo;

	private CardPanel? _pickCard;

	private Label? _pickTitle;

	private Label? _pickHint;

	private GameChooser? _picker;

	private CardPanel? _profileCard;

	private CardPanel? _listCard;

	private GameProfileManagerSection? _manager;

	private int _cardW;

	private bool _built;

	private readonly EventHandler _onSelectionChanged;

	protected override bool LaysOutInLogicalPixels => false;

	private static int CardPad => S(20);

	private static int CardGap => S(16);

	private static int RowTopPad => S(8);

	private static int RowBottomPad => S(6);

	private static int TitleToNote => S(2);

	private static int PickerW => S(260);

	private static int PickerH => S(32);

	internal GameProfileManagerSection ProfileManagerForTest => _manager ?? throw new InvalidOperationException("no list built");

	internal int RefreshesForTest { get; private set; }

	private static int TitleHeight => Math.Max(Fonts.LineHeight(FontRole.BodyBold), TextRenderer.MeasureText("Ag", Fonts.BodyBold).Height);

	private static int NoteLineHeight => Math.Max(Fonts.LineHeight(FontRole.Caption), TextRenderer.MeasureText("Ag", Fonts.Caption).Height);

	private static int S(int px)
	{
		return Tokens.Scale(px);
	}

	public ProfilesPage(AppSettings settings, SettingsStore store, GameSelection selection, IVibranceEngine engine, Action<DisplayPreset>? applyLook = null, Func<string?>? runningGameId = null, Func<string, GameProfile?>? gameProfileLookup = null, Action<GameProfile>? gameProfileSave = null, Func<IReadOnlyList<GameProfile>>? gameProfileLoadAll = null, Action<string>? gameProfileRemove = null)
	{
		_settings = settings;
		_store = store;
		_selection = selection;
		_engine = engine;
		_applyLook = applyLook;
		_runningGameId = runningGameId ?? (() => SupportedGames.CurrentlyRunning()?.Id);
		_gameProfileLookup = gameProfileLookup ?? new Func<string, GameProfile?>(GameProfileStore.Get);
		_gameProfileSave = gameProfileSave ?? new Action<GameProfile>(GameProfileStore.Set);
		_gameProfileLoadAll = gameProfileLoadAll ?? new Func<IReadOnlyList<GameProfile>>(GameProfileStore.Load);
		_gameProfileRemove = gameProfileRemove ?? new Action<string>(GameProfileStore.Remove);
		AutoScroll = true;
		Font = Fonts.Body;
		_title = PageLabel("Profiles", Fonts.Display, Theme.Text);
		_subtitle = PageLabel("A look for each game — saved here, applied when it opens, put back when it closes.", Fonts.Label, Theme.TextDim);
		BuildSections();
		_onSelectionChanged = (object? _, EventArgs _) =>
		{
			RefreshProfileCard();
			LayoutContent();
		};
		_selection.Changed += _onSelectionChanged;
		Resize += (object? _, EventArgs _) =>
		{
			LayoutContent();
		};
		ParentChanged += (object? _, EventArgs _) =>
		{
			if (Parent != null)
			{
				Refresh();
			}
		};
		_subtitle.TextAlign = ContentAlignment.TopLeft;
		UseHeaderPanel(_title, _subtitle);
		_built = true;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_selection.Changed -= _onSelectionChanged;
		}
		base.Dispose(disposing);
	}

	private void BuildSections()
	{
		_offerCard = new CardPanel
		{
			Visible = false
		};
		Controls.Add(_offerCard);
		_offerLine = new Label
		{
			Text = "",
			Font = Fonts.Body,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			AutoSize = true,
			TextAlign = ContentAlignment.TopLeft,
			UseMnemonic = false
		};
		_offerCard.Controls.Add(_offerLine);
		_offerYes = new GlassButton
		{
			Text = "Yes, set it up",
			Kind = GlassButtonKind.Primary
		};
		_offerYes.Click += (object? _, EventArgs _) =>
		{
			AcceptSetupOffer();
		};
		_offerCard.Controls.Add(_offerYes);
		_offerNo = new GlassButton
		{
			Text = "No thanks",
			Kind = GlassButtonKind.Secondary
		};
		_offerNo.Click += (object? _, EventArgs _) =>
		{
			DeclineSetupOffer();
		};
		_offerCard.Controls.Add(_offerNo);
		_pickCard = new CardPanel();
		_pickTitle = RowTitle(_pickCard, "Pick a game");
		_picker = new GameChooser(_selection);
		_pickCard.Controls.Add(_picker);
		_pickHint = RowNote(_pickCard, "Choose a game to give it its own colours.");
		Controls.Add(_pickCard);
		_listCard = new CardPanel();
		Controls.Add(_listCard);
		_manager = new GameProfileManagerSection(_listCard, Fonts.BodyBold, Fonts.Caption, _gameProfileLoadAll, _gameProfileSave, _gameProfileRemove, (string id) => !string.IsNullOrEmpty(id) && _runningGameId() == id, () =>
		{
			string text = _runningGameId();
			return (text != null) ? SupportedGames.ById(text)?.DisplayName : null;
		}, SaveCurrentLookForRunningGame);
		Retitle(_listCard, "SAVED GAME PROFILES", "Saved game profiles");
		_manager.Changed += () =>
		{
			if (_built)
			{
				LayoutContent();
				Invalidate(invalidateChildren: true);
			}
		};
		RefreshProfileCard();
	}

	private static void Retitle(Control card, string oldText, string newText)
	{
		string value = oldText.Replace(" ", "");
		foreach (Label item in card.Controls.OfType<Label>())
		{
			if ((item.Text ?? "").Replace(" ", "").Equals(value, StringComparison.OrdinalIgnoreCase))
			{
				item.Text = newText;
				item.Font = Fonts.BodyBold;
				item.ForeColor = Theme.Text;
				item.UseMnemonic = false;
				break;
			}
		}
	}

	private void RefreshProfileCard()
	{
		if (_listCard == null)
		{
			return;
		}
		if (_profileCard != null)
		{
			Controls.Remove(_profileCard);
			_profileCard.Dispose();
			_profileCard = null;
		}
		SupportedGame current = _selection.Current;
		if (!(current == null))
		{
			_profileCard = GameProfileSection.BuildCard(current.Id, current.DisplayName, _engine, 0, 0, (_cardW > 0) ? _cardW : S(700), _settings, _gameProfileLookup, _gameProfileSave, _gameProfileRemove, () =>
			{
				_manager?.Refresh();
			});
			Retitle(_profileCard, "PROFILE", "Profile");
			Controls.Add(_profileCard);
		}
	}

	private void RefreshSetupOffer()
	{
		if (_offerCard != null && _offerLine != null)
		{
			string text = _settings.PendingGameOfferId ?? "";
			bool flag = text.Length > 0 && GameLookJoin.LookGameFor(text) != null;
			if (flag)
			{
				_offerLine.Text = GameSetupOffer.Sentence(text);
			}
			_offerCard.Visible = flag;
			LayoutContent();
		}
	}

	private void AcceptSetupOffer()
	{
		string text = _settings.PendingGameOfferId ?? "";
		DisplayPreset displayPreset = GameLookJoin.StartingLookFor(text);
		if (displayPreset == null)
		{
			ClearSetupOffer();
			return;
		}
		_applyLook?.Invoke(displayPreset);
		string gameName = GameLookJoin.LookGameFor(text) ?? text;
		_gameProfileSave(GameProfileSection.CaptureFromPreset(text, gameName, displayPreset, _gameProfileLookup(text)));
		new GameProfileApplyGate(_settings).UserOptedBackIn();
		ClearSetupOffer();
		_manager?.Refresh();
		RefreshProfileCard();
		LayoutContent();
	}

	private void DeclineSetupOffer()
	{
		string text = _settings.PendingGameOfferId ?? "";
		if (text.Length > 0 && !_settings.DeclinedGameOffers.Contains(text, StringComparer.OrdinalIgnoreCase))
		{
			_settings.DeclinedGameOffers.Add(text);
		}
		ClearSetupOffer();
	}

	private void ClearSetupOffer()
	{
		_settings.PendingGameOfferId = "";
		_store.Save(_settings);
		RefreshSetupOffer();
	}

	private void SaveCurrentLookForRunningGame()
	{
		string text = _runningGameId();
		if (text != null)
		{
			SupportedGame supportedGame = SupportedGames.ById(text);
			if (!(supportedGame == null))
			{
				_gameProfileSave(GameProfileSection.Capture(text, supportedGame.DisplayName, _engine, _gameProfileLookup(text)));
				new GameProfileApplyGate(_settings).UserOptedBackIn();
				RefreshProfileCard();
			}
		}
	}

	public new void Refresh()
	{
		RefreshesForTest++;
		if (_built)
		{
			_manager?.Refresh();
			if (_listCard != null)
			{
				Retitle(_listCard, "SAVED GAME PROFILES", "Saved game profiles");
			}
			RefreshProfileCard();
			RefreshSetupOffer();
		}
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		base.OnVisibleChanged(e);
		if (Visible)
		{
			Refresh();
		}
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		LayoutContent();
		Refresh();
	}

	private Label PageLabel(string text, Font font, Color colour)
	{
		Label label = new Label
		{
			Text = text,
			Font = font,
			ForeColor = colour,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft,
			UseMnemonic = false
		};
		Controls.Add(label);
		return label;
	}

	private static Label RowTitle(Control card, string text)
	{
		Label label = new Label
		{
			Text = text,
			Font = Fonts.BodyBold,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft,
			UseMnemonic = false
		};
		card.Controls.Add(label);
		return label;
	}

	private static Label RowNote(Control card, string text)
	{
		Label label = new Label
		{
			Text = text,
			Font = Fonts.Caption,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = true,
			TextAlign = ContentAlignment.TopLeft,
			UseMnemonic = false
		};
		card.Controls.Add(label);
		return label;
	}

	private static int WidthOf(Label l)
	{
		return TextRenderer.MeasureText(l.Text ?? "", l.Font).Width + S(4);
	}

	private static int PlaceNote(Label note, int x, int y, int width)
	{
		note.MaximumSize = new Size(Math.Max(1, width), 0);
		note.Location = new Point(x, y);
		return Math.Max(NoteLineHeight, note.Height);
	}

	private void LayoutContent()
	{
		if (!_built || Width <= 0)
		{
			return;
		}
		int num = ContentRight - GlowPage.ContentLeft;
		if (num <= 0)
		{
			return;
		}
		int num2 = Math.Clamp(num, S(520), S(980));
		int num3 = GlowPage.ContentLeft + Math.Max(0, (num - num2) / 2);
		int num4 = num2 - 2 * CardPad;
		int y = AutoScrollPosition.Y;
		int num5 = ContentTop + y;
		if (_pickCard != null && _listCard != null && _manager != null && _offerCard != null && _offerLine != null && _offerYes != null && _offerNo != null && _pickTitle != null && _picker != null && _pickHint != null)
		{
			bool flag = _cardW != num2;
			_cardW = num2;
			int num6 = LayoutOfferCard(num3, num5, num2, num4);
			if (_offerCard.Visible)
			{
				num5 += num6 + CardGap;
			}
			_picker.Size = new Size(PickerW, PickerH);
			int num7 = ControlRow(_pickTitle, _pickHint, new Control[1] { _picker }, 0, num4) + RowBottomPad;
			_pickCard.SetBounds(num3, num5, num2, num7);
			num5 += num7 + CardGap;
			if ((_profileCard != null) & flag)
			{
				RefreshProfileCard();
			}
			if (_profileCard != null)
			{
				_profileCard.Location = new Point(num3, num5);
				num5 += _profileCard.Height + CardGap;
			}
			int num8 = _manager.PreferredHeight + 2 * CardPad;
			_listCard.SetBounds(num3, num5, num2, num8);
			_manager.Place(CardPad, CardPad, num4);
			num5 += num8;
		}
		AutoScrollMinSize = new Size(0, num5 - y + S(40));
	}

	private int LayoutOfferCard(int left, int y, int cardW, int innerW)
	{
		int num = 32;
		int num2 = ((num > 0) ? S(num) : S(32));
		_offerYes.Size = new Size(S(140), num2);
		_offerNo.Size = new Size(S(110), num2);
		int num3 = S(8);
		int num4 = _offerYes.Width + num3 + _offerNo.Width;
		bool flag = innerW - num4 - S(16) >= S(180);
		int num5 = PlaceNote(width: flag ? (innerW - num4 - S(16)) : innerW, note: _offerLine, x: CardPad, y: RowTopPad);
		int num6;
		if (flag)
		{
			num6 = Math.Max(RowTopPad + num5 + RowBottomPad, RowTopPad + num2 + RowBottomPad);
			_offerYes.Location = new Point(CardPad + innerW - num4, (num6 - num2) / 2);
			_offerNo.Location = new Point(_offerYes.Right + num3, _offerYes.Top);
		}
		else
		{
			int num7 = RowTopPad + num5 + S(10);
			_offerYes.Location = new Point(CardPad, num7);
			_offerNo.Location = new Point(_offerYes.Right + num3, num7);
			num6 = num7 + num2 + RowBottomPad;
		}
		_offerCard.SetBounds(left, y, cardW, num6);
		return num6;
	}

	private int ControlRow(Label title, Label? note, Control[] controls, int cy, int inner)
	{
		int num = S(8);
		int num2 = controls.Sum((Control control3) => control3.Width) + num * (controls.Length - 1);
		int num3 = controls.Max((Control control3) => control3.Height);
		bool flag = inner - num2 - S(16) >= WidthOf(title);
		int width = Math.Max(S(40), flag ? (inner - num2 - S(16)) : inner);
		int num4 = RowTopPad + TitleHeight;
		title.SetBounds(CardPad, cy + RowTopPad, width, TitleHeight);
		if (note != null)
		{
			num4 += TitleToNote + PlaceNote(note, CardPad, cy + num4 + TitleToNote, width);
		}
		Control[] array;
		if (flag)
		{
			int num5 = Math.Max(num4 + RowBottomPad, RowTopPad + num3 + RowBottomPad);
			int num6 = CardPad + inner - num2;
			array = controls;
			foreach (Control control in array)
			{
				control.Location = new Point(num6, cy + (num5 - control.Height) / 2);
				num6 += control.Width + num;
			}
			return cy + num5;
		}
		int num8 = cy + num4 + S(10);
		int num9 = CardPad;
		array = controls;
		foreach (Control control2 in array)
		{
			control2.Location = new Point(num9, num8);
			num9 += control2.Width + num;
		}
		return num8 + num3 + RowBottomPad;
	}
}