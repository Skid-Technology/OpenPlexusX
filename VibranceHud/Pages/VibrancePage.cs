using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using A;
using VibranceHud.Capabilities;
using VibranceHud.Controls;
using VibranceHud.Crosshair;
using VibranceHud.Design;
using VibranceHud.Display;
using VibranceHud.Licensing;
using VibranceHud.Monitors;
using VibranceHud.Presets;
using VibranceHud.Support;

namespace VibranceHud.Pages;

public sealed class VibrancePage : GlowPage
{
	internal sealed class MixChannel
	{
		private readonly int _min;

		private readonly int _max;

		private int _value;

		internal int Value
		{
			get
			{
				return _value;
			}
			set
			{
				int num = Math.Clamp(value, _min, _max);
				if (num != _value)
				{
					_value = num;
					Changed?.Invoke();
				}
			}
		}

		internal event Action? Changed;

		internal MixChannel(int min, int max, int value)
		{
			_min = min;
			_max = max;
			_value = Math.Clamp(value, min, max);
		}
	}

	private const int SaveDebounceMs = 500;

	private readonly MonitorSelectionCard _monitorCard;

	private readonly MonitorSelection _monitorSelection;

	private readonly VibranceEngine _engine;

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly DebouncedAction _saveDebounce;

	private readonly CardPanel _card;

	private readonly CardPanel _headlineCard = new CardPanel();

	private readonly CardPanel _fineCard = new CardPanel();

	private readonly CardPanel _presetsCard = new CardPanel();

	private readonly CardPanel _scenesCard = new CardPanel
	{
		Visible = false
	};

	private bool _scenesShown;

	internal const int ScenesPerRow = 10;

	internal const int GameScenesPerRow = 4;

	private int _gameTileCount;

	private readonly Label _headlineRule;

	private bool _locked;

	private SliderRow _eyeWarmth;

	private SliderRow _eyeDim;

	private Label _eyeCaption;

	private Label _eyeHint;

	private readonly GlassButton _phone;

	private Action? _onOpenPhone;

	private readonly Label _title;

	private readonly Label _subtitle;

	private readonly Label _liveTick;

	private readonly Label _liveWord;

	private readonly Label _liveDetail;

	private readonly Label _presetsLabel;

	private readonly Label _primaryLabel;

	private readonly Label _fineLabel;

	private readonly Label _shortcutsLabel;

	private readonly Label _fineHint;

	private readonly Label _popupHotkeyLabel;

	private readonly Label _mainHotkeyLabel;

	private readonly Label _screenshotHotkeyLabel;

	private readonly SliderRow _saturation;

	private readonly SliderRow _vibrance;

	private readonly SliderRow _brightness;

	private readonly SliderRow _gamma;

	private readonly SliderRow _contrast;

	private readonly SliderRow _temperature;

	private readonly HotkeyPicker _hotkeyPicker;

	private readonly HotkeyPicker _mainHotkeyPicker;

	private readonly HotkeyPicker _screenshotHotkeyPicker;

	private readonly GlassButton _resetFine;

	private readonly GlassButton _saveLook;

	private readonly GlassButton _deleteAllLooks;

	private readonly GlassButton _compare;

	private readonly AbCompare _ab = new AbCompare();

	private readonly SliderRow _highlights;

	private readonly SliderRow _shadows;

	private readonly SliderRow _blacks;

	private readonly SliderRow _whites;

	private readonly SliderRow _shadowTint;

	private readonly SliderRow _highlightTint;

	private readonly MixChannel _hue;

	private readonly MixChannel _red;

	private readonly MixChannel _green;

	private readonly MixChannel _blue;

	private readonly CardPanel _depthCard = new CardPanel
	{
		Visible = false
	};

	private bool _depthShown;

	private Label _depthLabel;

	private Label _depthHint;

	private Label _formulaLabel;

	private Label _hueWheelLabel;

	private Label _hueWheelHint;

	private Label _darkenWheelLabel;

	private Label _darkenWheelHint;

	private Label _darkenCaption;

	private InUseTag _depthInUse;

	private readonly List<GlassButton> _formulaChips = new List<GlassButton>();

	private HueWheel _hueWheel;

	private HueWheel _darkenWheel;

	private SliderRow _turnRow;

	private SliderRow _darkenRow;

	private SliderRow _amountRow;

	private SliderRow _complementRow;

	internal const string DepthSubtitle = "Which saturation PlexusX uses, and how far the colours are pushed. The formula travels with the look, so a preset can bring its own.";

	private readonly Label _visionLabel;

	private readonly Label _visionHint;

	private readonly List<ChipButton> _visionChips = new List<ChipButton>();

	private readonly Label _colourReach;

	private readonly Label _lookNote;

	private readonly Label _saturationHint;

	private readonly Label _vibranceHint;

	private readonly Label[] _saturationTicks;

	private readonly Label[] _vibranceTicks;

	private static readonly (ColourVisionMode Mode, string Label, string Tip)[] VisionModes = new (ColourVisionMode, string, string)[4]
	{
		(ColourVisionMode.Off, "Normal", "No correction."),
		(ColourVisionMode.GreenWeak, "Green-weak", "Deuteranopia - by far the most common, around 6% of men."),
		(ColourVisionMode.RedWeak, "Red-weak", "Protanopia - reds look darker and closer to green."),
		(ColourVisionMode.BlueWeak, "Blue-weak", "Tritanopia - rare, and affects blue against green.")
	};

	private bool _built;

	private readonly GlassDropdown _gamePicker;

	private readonly Label _shareLabel;

	private readonly Label _shareHint;

	private readonly Label _shareStatus;

	private readonly Label _setupHint;

	private readonly GlassTextBox _codeBox;

	private readonly GlassButton _copySetup;

	private readonly GlassButton _copyCode;

	private readonly GlassButton _applyCode;

	private readonly GlassButton _keepCode;

	private readonly List<SceneTile> _chips = new List<SceneTile>();

	private readonly List<DisplayPreset> _presets = new List<DisplayPreset>();

	private int _looksGeneration;

	private readonly Label _looksOnTheirWay;

	private bool _showLooksOnTheirWay;

	private readonly SignalToolTip _presetTips = new SignalToolTip
	{
		InitialDelay = 350,
		ReshowDelay = 120
	};

	private bool _applyingPreset;

	private bool _dragging;

	private bool _syncingFromEngine;

	private Dictionary<Font, FontRole> _ramp = new Dictionary<Font, FontRole>(ReferenceEqualityComparer.Instance);

	private string? _monitorLine;

	[ThreadStatic]
	internal static Action? ReloadingTheLooksForTest;

	internal const int MaxCustomPresets = 10;

	private bool _laying;

	private Size _laidOutFor = Size.Empty;

	private int _saturationRowBottom;

	private bool _paidSectionsHidden;

	private static readonly float[] VibranceTickFractions = new float[3] { 0f, 0.2857143f, 1f };

	internal const string CodesNotReadyWords = "Share codes aren't working on this PC right now. Nothing was copied, changed or saved.";

	private LookNote _lastLookNote = LookNote.None;

	private const string MatrixMonitorHint = "Needs every monitor ticked";

	protected override bool LaysOutInLogicalPixels => false;

	private static int PagePadBottom => Tokens.Scale(16);

	private static int CardPad => Tokens.Scale(21);

	private static int CardPadTop => Tokens.Scale(21);

	private static int HeadlinePadX => Tokens.Scale(21);

	private static int HeadlinePadTop => Tokens.Scale(21);

	private static int HeadlinePadBottom => Tokens.Scale(21);

	private static int CardGap => Tokens.Scale(16);

	private static int FineColGap => Tokens.Scale(40);

	private static int PresetsCardW => Tokens.Scale(332);

	private static int HeaderHeightHere => Math.Max(Tokens.Scale(56), Fonts.LineHeight(FontRole.Display) + Tokens.Scale(2) + Fonts.LineHeight(FontRole.Body));

	private static int SectionGap => Tokens.Scale(26);

	private static int ColGap => Tokens.Scale(28);

	private static int SectionLabelH => Tokens.Scale(22);

	private static int ResetH => Tokens.Scale(28);

	private bool ActiveIsSavedLook => _chips.FindIndex((SceneTile sceneTile) => sceneTile.Active) >= _gameTileCount;

	private static int SceneGap => Tokens.Scale(8);

	internal Action? PhoneWindowOpener
	{
		get
		{
			return _onOpenPhone;
		}
		set
		{
			_onOpenPhone = value;
			_phone.Visible = value != null && !_locked;
		}
	}

	internal Func<ColourReachFacts>? ColourReachSource { get; set; }

	internal Func<CrosshairConfig?>? CrosshairForSharing { get; set; }

	internal Func<MonitorCode?>? ModeForSharing { get; set; }

	internal IReadOnlyList<string> TestPickerGames => _gamePicker.Items.ToList();

	internal IReadOnlyList<string> TestChipCaptions => _chips.Select((SceneTile sceneTile) => sceneTile.Caption).ToList();

	internal int TestGameTileCount => _gameTileCount;

	internal CardPanel PresetsCardForTest => _presetsCard;

	internal CardPanel ScenesCardForTest => _scenesCard;

	internal bool ScenesCardShownForTest => _scenesShown;

	internal string? TestCodeBoxTip
	{
		get
		{
			string toolTip = _presetTips.GetToolTip(_codeBox);
			if (toolTip == null || toolTip.Length <= 0)
			{
				return null;
			}
			return toolTip;
		}
	}

	internal string? TestLooksOnTheirWay
	{
		get
		{
			if (!_showLooksOnTheirWay)
			{
				return null;
			}
			return _looksOnTheirWay.Text;
		}
	}

	internal bool TestPickerShown
	{
		get
		{
			if (!_locked)
			{
				return !_showLooksOnTheirWay;
			}
			return false;
		}
	}

	internal IReadOnlyList<DisplayPreset> TestCustomPresets => _settings.CustomPresets;

	internal IReadOnlyList<PresetKeyBinding> TestPresetBindings => _settings.PresetKeyBindings;

	internal int TestHighlights => _highlights.Slider.Value;

	internal IReadOnlyDictionary<string, MixChannel> TestColourMixChannels => new Dictionary<string, MixChannel>
	{
		["Hue"] = _hue,
		["Red"] = _red,
		["Green"] = _green,
		["Blue"] = _blue
	};

	internal int TestShadows => _shadows.Slider.Value;

	internal int TestBlacks => _blacks.Slider.Value;

	internal ChannelMix TestMix => _engine.Mix;

	internal int TestWhites => _whites.Slider.Value;

	internal int TestShadowTint => _shadowTint.Slider.Value;

	internal int TestHighlightTint => _highlightTint.Slider.Value;

	internal string? TestActiveChipName => _chips.FirstOrDefault((SceneTile sceneTile) => sceneTile.Active)?.Caption;

	internal bool TestGradeRowsVisible
	{
		get
		{
			if (_blacks.Visible && _whites.Visible && _shadowTint.Visible)
			{
				return _highlightTint.Visible;
			}
			return false;
		}
	}

	internal int TestSaturation => _saturation.Slider.Value;

	internal int TestVibrance => _vibrance.Slider.Value;

	internal bool PaidSectionsShownForTest => _vibrance.Visible;

	internal string SubtitleForTest => _subtitle.Text;

	internal CardPanel DepthCardForTest => _depthCard;

	internal HueWheel HueWheelForTest => _hueWheel;

	internal HueWheel DarkenWheelForTest => _darkenWheel;

	internal IReadOnlyList<GlassButton> FormulaChipsForTest => _formulaChips;

	internal InUseTag DepthInUseForTest => _depthInUse;

	internal string DarkenCaptionForTest => _darkenCaption.Text;

	internal IReadOnlyDictionary<string, SliderRow> DepthRowsForTest => new Dictionary<string, SliderRow>
	{
		["Hue turn"] = _turnRow,
		["Darken"] = _darkenRow,
		["Turn amount"] = _amountRow,
		["Complement"] = _complementRow
	};

	internal string TestShareCode => _codeBox.Text;

	internal string TestShareStatus => _shareStatus.Text;

	private bool PaidLookAllowed => _engine.PaidControlsUnlocked;

	internal string TestColourReachText => _colourReach.Text;

	internal bool TestColourReachVisible => _colourReach.Visible;

	internal bool TestColourReachIsRed => _colourReach.ForeColor == Theme.Negative;

	internal Label TestColourReachLabel => _colourReach;

	internal string TestLookNoteText => _lookNote.Text;

	internal bool TestLookNoteVisible => _lookNote.Visible;

	internal bool TestLookNoteIsRed => _lookNote.ForeColor == Theme.Negative;

	internal Label TestLookNoteLabel => _lookNote;

	internal Label TestShareStatusLabel => _shareStatus;

	internal ColourVisionMode? TestActiveCorrectionChip
	{
		get
		{
			for (int i = 0; i < _visionChips.Count; i++)
			{
				if (_visionChips[i].Active)
				{
					return VisionModes[i].Mode;
				}
			}
			return null;
		}
	}

	internal int TestCardBottom => _headlineCard.Bottom;

	internal int TestSaturationRowBottom => _saturationRowBottom;

	internal int TestCardClientHeight => _headlineCard.ClientSize.Height;

	public event Action<string>? PresetApplied;

	public event Action? SliderMovedByUser;

	public event Func<uint, uint, bool>? HotkeyChanged;

	public event Func<uint, uint, bool, bool>? MainHotkeyChanged;

	public event Func<uint, uint, bool>? ScreenshotHotkeyChanged;

	public event Func<uint, uint, bool>? NightVisionHotkeyChanged
	{
		add
		{
		}
		remove
		{
		}
	}

	public event Func<bool>? PresetBindingsChanged;

	internal static string GammaHelpFor(MachineCapabilities caps)
	{
		string toneLimitation = caps.ToneLimitation;
		if (toneLimitation.Length != 0)
		{
			return "Midtones only — brightens shadows without washing out the highlights.\r\n\r\n" + toneLimitation;
		}
		return "Midtones only — brightens shadows without washing out the highlights.";
	}

	public void SyncNightVisionSwitch()
	{
	}

	internal void ReloadServedNightVision()
	{
	}

	public VibrancePage(VibranceEngine engine, AppSettings settings, SettingsStore store, Func<string?>? runningGameId = null, Func<string, GameProfile?>? gameProfileLookup = null, Action<GameProfile>? gameProfileSave = null, MonitorIdentifier? monitorIdentifier = null)
	{
		_engine = engine;
		_settings = settings;
		_store = store;
		MonitorSelection monitorSelection = (_monitorSelection = engine.MonitorSelection ?? new MonitorSelection(settings, () => Array.Empty<ColourMonitor>()));
		_monitorCard = new MonitorSelectionCard(monitorSelection, monitorIdentifier ?? new MonitorIdentifier(), () =>
		{
			_store.Save(_settings);
		});
		if (monitorSelection.WorthShowing)
		{
			Controls.Add(_monitorCard.View);
		}
		monitorSelection.Changed += SyncMonitorCard;
		_saveDebounce = new DebouncedAction(() =>
		{
			_store.Save(_settings);
		}, 500);
		AutoScroll = true;
		Font = Fonts.Label;
		_title = HeaderText("Display", Fonts.Display, Theme.Text);
		_subtitle = HeaderText("Scenes are starting points. The sliders work whatever you play.", Fonts.Label, Theme.TextDim);
		_subtitle.TextAlign = ContentAlignment.TopLeft;
		_phone = new GlassButton
		{
			Text = "Phone",
			Icon = SignalIcon.Phone
		};
		_phone.Click += (object? s, EventArgs e2) =>
		{
			_onOpenPhone?.Invoke();
		};
		Controls.Add(_phone);
		_liveTick = PageLabel("", Fonts.Caption, Theme.Positive);
		_liveTick.Paint += (object? _, PaintEventArgs e2) =>
		{
			PaintLiveTick(e2.Graphics, _liveTick.ClientRectangle);
		};
		_liveWord = HeaderText("Live", Fonts.BodyBold, Theme.Text);
		_liveDetail = HeaderText(TheMonitorLine(), Fonts.Body, Theme.TextDim);
		_card = new CardPanel();
		Controls.Add(_card);
		Controls.Add(_headlineCard);
		Controls.Add(_fineCard);
		Controls.Add(_presetsCard);
		Controls.Add(_scenesCard);
		Controls.Add(_depthCard);
		_headlineRule = new Label
		{
			AutoSize = false,
			BackColor = (SlateSurface.Active ? SignalTokens.Slate.Border : Theme.Border),
			Text = "",
			TabStop = false
		};
		_headlineCard.Controls.Add(_headlineRule);
		_eyeCaption = SectionLabel("Eye care");
		_eyeHint = CardLabel("Warmer and dimmer for long sessions. Free on every account, and it sits on top of whatever look you are using.");
		_eyeHint.Font = Fonts.Caption;
		_eyeHint.ForeColor = Theme.TextDim;
		_eyeWarmth = Row("Warmth", 0, 100, 0, _engine.EyeCareWarmthPercent, (int v) =>
		{
			_engine.EyeCareWarmthPercent = v;
			_settings.EyeCareWarmth = v;
		}, SliderRowShape.Standard, SliderPalette.Accent(), (int v) => (v != 0) ? $"{v}%" : "Off");
		_eyeDim = Row("Screen dim", 0, 80, 0, _engine.EyeCareDim, (int v) =>
		{
			_engine.EyeCareDim = v;
			_settings.EyeCareDim = v;
		}, SliderRowShape.Standard, SliderPalette.Luminance, (int v) => (v != 0) ? $"{v}%" : "Off");
		_highlights = ToneRow("Highlights", (int v) =>
		{
			_engine.Tone = _engine.Tone with
			{
				Highlights = v
			};
		}, _engine.Tone.Highlights, _fineCard);
		_shadows = ToneRow("Shadows", (int v) =>
		{
			_engine.Tone = _engine.Tone with
			{
				Shadows = v
			};
		}, _engine.Tone.Shadows, _fineCard);
		Explain(_highlights, "The brightest parts only. Pull it down to recover detail in a blown-out sky.");
		Explain(_shadows, "The darkest parts only. Lift it to see into corners without washing the whole picture out.");
		_blacks = ToneRow("Blacks", (int v) =>
		{
			_engine.Tone = _engine.Tone with
			{
				Blacks = v
			};
		}, _engine.Tone.Blacks, _fineCard);
		_whites = ToneRow("Whites", (int v) =>
		{
			_engine.Tone = _engine.Tone with
			{
				Whites = v
			};
		}, _engine.Tone.Whites, _fineCard);
		_shadowTint = ToneRow("Shadow tint", (int v) =>
		{
			_engine.Tone = _engine.Tone with
			{
				ShadowTint = v
			};
		}, _engine.Tone.ShadowTint, _fineCard);
		_highlightTint = ToneRow("Highlight tint", (int v) =>
		{
			_engine.Tone = _engine.Tone with
			{
				HighlightTint = v
			};
		}, _engine.Tone.HighlightTint, _fineCard);
		Explain(_blacks, "Where black sits. Pull it down for punch, lift it for a faded, filmic base.");
		Explain(_whites, "Where white sits. Push it up so highlights read hot instead of grey.");
		Explain(_shadowTint, "Warm or cool, in the dark parts only. Left is cool, right is warm.");
		Explain(_highlightTint, "Warm or cool, in the bright parts only. A cool sky is a left pull here.");
		_hue = Mix(-180, 180, _engine.Hue, (int v) =>
		{
			_engine.Hue = v;
			_settings.Hue = v;
		});
		_red = Mix(50, 150, _engine.RedGain, (int v) =>
		{
			_engine.RedGain = v;
			_settings.RedGain = v;
		});
		_green = Mix(50, 150, _engine.GreenGain, (int v) =>
		{
			_engine.GreenGain = v;
			_settings.GreenGain = v;
		});
		_blue = Mix(50, 150, _engine.BlueGain, (int v) =>
		{
			_engine.BlueGain = v;
			_settings.BlueGain = v;
		});
		BuildColourDepthCard();
		_visionLabel = SectionLabel("Colour vision");
		_visionHint = CardLabel("Around one man in twelve can't separate some colours. This rotates the ones you can't tell apart into ones you can.");
		_visionHint.Font = Fonts.Caption;
		_visionHint.ForeColor = Theme.TextDim;
		_engine.SetAsideCorrection = LookCorrection.SetAside(_settings);
		_engine.ColourVision = _settings.ColourVision;
		(ColourVisionMode, string, string)[] visionModes = VisionModes;
		for (int num = 0; num < visionModes.Length; num++)
		{
			(ColourVisionMode, string, string) tuple = visionModes[num];
			ColourVisionMode item = tuple.Item1;
			string item2 = tuple.Item2;
			string item3 = tuple.Item3;
			ColourVisionMode captured = item;
			ChipButton chipButton = new ChipButton
			{
				Text = item2,
				Active = (_settings.ColourVision == item)
			};
			chipButton.Click += (object? _, EventArgs _) =>
			{
				ApplyColourVision(captured);
			};
			_presetTips.SetToolTip(chipButton, item3);
			_visionChips.Add(chipButton);
			_card.Controls.Add(chipButton);
		}
		_presetsLabel = SectionLabel("Scene presets", _presetsCard);
		_gamePicker = new GlassDropdown
		{
			AccessibleName = "Game",
			Size = new Size(200, Tokens.Scale(32))
		};
		_looksGeneration = ServedLooks.Generation;
		_gamePicker.SetItems(ServedLooks.Games);
		string wanted = ((!string.IsNullOrEmpty(_settings.ColourPresetGame)) ? _settings.ColourPresetGame : (_settings.FavoriteGame ?? ""));
		int num2 = ServedLooks.Games.ToList().FindIndex((string g) => string.Equals(g, wanted, StringComparison.OrdinalIgnoreCase));
		if (num2 >= 0)
		{
			_gamePicker.SelectedIndex = num2;
		}
		_gamePicker.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			RebuildSceneChips();
		};
		_presetsCard.Controls.Add(_gamePicker);
		_looksOnTheirWay = CardLabel("", _presetsCard);
		DecideWhereTheLooksAre();
		_saveLook = new GlassButton
		{
			Text = "Save look",
			Kind = GlassButtonKind.Quiet,
			Icon = SignalIcon.Save
		};
		_saveLook.Click += (object? _, EventArgs _) =>
		{
			PromptAndSaveCustom();
		};
		_presetsCard.Controls.Add(_saveLook);
		_deleteAllLooks = new GlassButton
		{
			Text = "",
			Kind = GlassButtonKind.Icon,
			Icon = SignalIcon.Trash
		};
		_deleteAllLooks.AccessibleName = "Delete all saved looks";
		_presetTips.SetToolTip(_deleteAllLooks, "Delete all saved looks");
		_deleteAllLooks.Click += (object? _, EventArgs _) =>
		{
			ConfirmAndDeleteAllCustomPresets();
		};
		_presetsCard.Controls.Add(_deleteAllLooks);
		RebuildSceneChips();
		_primaryLabel = SectionLabel("Primary");
		_primaryLabel.Visible = false;
		_saturation = Row("Saturation", 0, 300, 100, _engine.Saturation, (int v) =>
		{
			if (!_applyingPreset)
			{
				_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Saturation;
			}
			_engine.Saturation = v;
			_settings.SaturationPercent = v;
		}, SliderRowShape.Headline, null, (int v) => $"{v}%", _headlineCard);
		_saturation.Slider.Fill = SliderFill.Signal;
		_vibrance = Row("Vibrance", 0, 350, 100, _engine.Vibrance, (int v) =>
		{
			if (!_applyingPreset)
			{
				_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Vibrance;
			}
			_engine.Vibrance = v;
			_settings.VibrancePercent = v;
			_settings.VibranceChosenByUser = true;
		}, SliderRowShape.Headline, null, (int v) => VibranceStatus.Readout(_engine.DriverState, v), _headlineCard);
		_saturationHint = CardLabel("100 is the game as it ships", _headlineCard);
		_saturationHint.TextAlign = ContentAlignment.MiddleRight;
		_saturationHint.Font = Fonts.Caption;
		_saturationTicks = BuildTicks(_headlineCard, "0", "100", "200", 300.ToString(CultureInfo.InvariantCulture));
		_vibranceHint = CardLabel("Past 100 it continues in software", _headlineCard);
		_vibranceHint.TextAlign = ContentAlignment.MiddleRight;
		_vibranceHint.Font = Fonts.Caption;
		_vibranceTicks = BuildTicks(_headlineCard, "0", $"{100} · driver", 350.ToString(CultureInfo.InvariantCulture));
		_compare = new GlassButton
		{
			Text = "Compare",
			Kind = GlassButtonKind.Secondary,
			Icon = SignalIcon.Compare
		};
		_compare.Click += (object? _, EventArgs _) =>
		{
			ToggleCompare();
		};
		Controls.Add(_compare);
		_presetTips.SetToolTip(_compare, "Hold your look against neutral, so you can see what it is actually doing.");
		_resetFine = new GlassButton
		{
			Text = "Reset",
			Icon = SignalIcon.Reset
		};
		_resetFine.AccessibleName = "Reset all sliders";
		_resetFine.Click += (object? _, EventArgs _) =>
		{
			ResetFineTune();
		};
		Controls.Add(_resetFine);
		UseHeaderPanel(_title, _subtitle, new Control[6] { _liveTick, _liveWord, _liveDetail, _compare, _resetFine, _phone });
		_fineLabel = SectionLabel("Fine tune", _fineCard);
		string toneLimitation = Machine.Current.ToneLimitation;
		_fineHint = CardLabel((toneLimitation.Length == 0) ? "Contrast, highlights and shadows also change how dark it looks, not just gamma." : toneLimitation, _fineCard);
		_fineHint.Font = Fonts.Caption;
		_fineHint.ForeColor = ((toneLimitation.Length > 0) ? Theme.Accent : Theme.TextFaint);
		_brightness = Row("Brightness", 50, 170, 100, _engine.Brightness, (int v) =>
		{
			_engine.Brightness = v;
			_settings.BrightnessPercent = v;
		}, SliderRowShape.Standard, SliderPalette.Luminance, (int v) => $"{v}%", _fineCard);
		_gamma = Row("Gamma", 50, 150, 100, _engine.Gamma, (int v) =>
		{
			_engine.Gamma = v;
			_settings.GammaPercent = v;
		}, SliderRowShape.Standard, SliderPalette.Luminance, (int v) => ((float)v / 100f).ToString("0.00", CultureInfo.InvariantCulture), _fineCard, 0.01m);
		_contrast = Row("Contrast", 50, 150, 100, _engine.Contrast, (int v) =>
		{
			_engine.Contrast = v;
			_settings.ContrastPercent = v;
		}, SliderRowShape.Standard, SliderPalette.Contrast, (int v) => $"{v}%", _fineCard);
		_temperature = Row("Temperature", -100, 100, 0, _engine.Temperature, (int v) =>
		{
			_engine.Temperature = v;
			_settings.Temperature = v;
		}, SliderRowShape.Standard, SliderPalette.Temperature, TemperatureText, _fineCard);
		_shortcutsLabel = SectionLabel("Shortcuts");
		_popupHotkeyLabel = CardLabel("Quick colour popup");
		_mainHotkeyLabel = CardLabel("Open main window");
		_screenshotHotkeyLabel = CardLabel("Screenshot with colours");
		_hotkeyPicker = new HotkeyPicker
		{
			AccessibleName = _popupHotkeyLabel.Text,
			ModifierMask = _settings.HotkeyModifierMask,
			VirtualKey = _settings.HotkeyVirtualKey
		};
		_hotkeyPicker.HotkeyChanged += (uint mask, uint vk) =>
		{
			_settings.HotkeyModifierMask = mask;
			_settings.HotkeyVirtualKey = vk;
			_store.Save(_settings);
			_hotkeyPicker.ReportBindingResult(HotkeyChanged?.Invoke(mask, vk) ?? true);
		};
		_card.Controls.Add(_hotkeyPicker);
		_mainHotkeyPicker = new HotkeyPicker
		{
			AccessibleName = _mainHotkeyLabel.Text,
			ModifierMask = _settings.MainHotkeyModifierMask,
			VirtualKey = (_settings.MainHotkeyEnabled ? _settings.MainHotkeyVirtualKey : 0u)
		};
		_mainHotkeyPicker.HotkeyChanged += (uint mask, uint vk) =>
		{
			_settings.MainHotkeyModifierMask = mask;
			_settings.MainHotkeyVirtualKey = vk;
			_settings.MainHotkeyEnabled = true;
			_store.Save(_settings);
			_mainHotkeyPicker.ReportBindingResult(MainHotkeyChanged?.Invoke(mask, vk, arg3: true) ?? true);
		};
		_card.Controls.Add(_mainHotkeyPicker);
		_screenshotHotkeyPicker = new HotkeyPicker
		{
			AccessibleName = _screenshotHotkeyLabel.Text,
			ModifierMask = _settings.ScreenshotHotkeyModifierMask,
			VirtualKey = (_settings.ScreenshotHotkeyEnabled ? _settings.ScreenshotHotkeyVirtualKey : 0u)
		};
		_screenshotHotkeyPicker.HotkeyChanged += (uint mask, uint vk) =>
		{
			_settings.ScreenshotHotkeyModifierMask = mask;
			_settings.ScreenshotHotkeyVirtualKey = vk;
			_settings.ScreenshotHotkeyEnabled = true;
			_store.Save(_settings);
			_screenshotHotkeyPicker.ReportBindingResult(ScreenshotHotkeyChanged?.Invoke(mask, vk) ?? true);
		};
		_card.Controls.Add(_screenshotHotkeyPicker);
		_shareLabel = SectionLabel("Share");
		_shareHint = CardLabel("Paste a friend's code to get their exact look, or copy yours to send.");
		_codeBox = new GlassTextBox
		{
			AccessibleName = "Share code",
			CharacterCasing = CharacterCasing.Upper,
			PlaceholderText = "PX-XXXXXXXXXXXXXXXXX"
		};
		_codeBox.Inner.Font = Fonts.Mono;
		_card.Controls.Add(_codeBox);
		_copyCode = new GlassButton
		{
			Text = "Copy my code",
			Kind = GlassButtonKind.Secondary
		};
		_copyCode.Click += (object? _, EventArgs _) =>
		{
			CopyMyCode();
		};
		_card.Controls.Add(_copyCode);
		_applyCode = new GlassButton
		{
			Text = "Apply",
			Kind = GlassButtonKind.Primary
		};
		_applyCode.Click += (object? _, EventArgs _) =>
		{
			ApplyCode();
		};
		_card.Controls.Add(_applyCode);
		_keepCode = new GlassButton
		{
			Text = "Keep it",
			Kind = GlassButtonKind.Secondary
		};
		_keepCode.Click += (object? _, EventArgs _) =>
		{
			SaveSharedCodeAsLook(_codeBox.Text);
		};
		_card.Controls.Add(_keepCode);
		_codeBox.TextChanged += (object? _, EventArgs _) =>
		{
			SyncShareActions();
		};
		SyncShareActions();
		_codeBox.Inner.KeyDown += (object? _, KeyEventArgs e2) =>
		{
			if (e2.KeyCode == Keys.Return && _applyCode.Enabled)
			{
				ApplyCode();
				e2.SuppressKeyPress = true;
			}
		};
		_setupHint = CardLabel("Or copy everything at once - these colours, your crosshair and your screen mode.");
		_copySetup = new GlassButton
		{
			Text = "Copy whole setup",
			Kind = GlassButtonKind.Secondary
		};
		_copySetup.Click += (object? _, EventArgs _) =>
		{
			CopyWholeSetup();
		};
		_card.Controls.Add(_copySetup);
		_shareStatus = CardLabel("");
		_colourReach = CardLabel("", _headlineCard);
		_colourReach.Name = "ColourReach";
		_colourReach.TextAlign = ContentAlignment.TopLeft;
		_colourReach.Visible = false;
		_lookNote = CardLabel("", _scenesCard);
		_lookNote.Name = "LookNote";
		_lookNote.TextAlign = ContentAlignment.TopLeft;
		_lookNote.Visible = false;
		Explain(_saturation, "How strong every colour is. Push it too far and detail turns to blocks.");
		string text = "Boosts dull colours and mostly leaves skin tones alone. Subtler than saturation.";
		string text2 = VibranceStatus.Qualifier(_engine.DriverState);
		Explain(_vibrance, (text2.Length == 0) ? text : (text + "\r\n\r\n" + text2));
		Explain(_brightness, "Lifts or lowers the whole picture. Bright areas clip to white if you push it.");
		Explain(_gamma, GammaHelpFor(Machine.Current));
		Explain(_contrast, "Distance between the darkest and lightest parts.");
		Explain(_temperature, "Warm pushes toward orange, cool toward blue. Neutral is untouched.");
		SetCodeBoxTip();
		_presetTips.SetToolTip(_copyCode, "Puts your exact look on the clipboard as a short code.");
		_presetTips.SetToolTip(_applyCode, "Switch to the look in the box. A wrong code changes nothing.");
		_presetTips.SetToolTip(_keepCode, "Saves the code in the box to your looks, so you still have it later.");
		_presetTips.SetToolTip(_resetFine, "Put everything on this page back: vibrance, saturation, brightness, gamma, contrast, temperature, highlights, shadows, blacks, whites, both tints, hue, red, green, blue, Colour depth, Eye care and Colour vision.");
		RefreshReadouts();
		UpdateActiveChip();
		_vibrance.Slider.ValueChanged += SyncVibranceMatrixHint;
		SyncMatrixRows();
		RememberTheRamp();
		Resize += (object? _, EventArgs _) =>
		{
			LayoutIfTheSizeChanged();
		};
		HandleCreated += (object? _, EventArgs _) =>
		{
			LayoutContent();
		};
		_built = true;
		void SyncShareActions()
		{
			GlassButton applyCode = _applyCode;
			bool enabled = (_keepCode.Enabled = _codeBox.Text.Trim().Length > 0);
			applyCode.Enabled = enabled;
		}
	}

	private void RememberTheRamp()
	{
		_ramp = new Dictionary<Font, FontRole>(ReferenceEqualityComparer.Instance);
		FontRole[] values = Enum.GetValues<FontRole>();
		foreach (FontRole fontRole in values)
		{
			_ramp.TryAdd(Fonts.For(fontRole), fontRole);
		}
	}

	private bool TheRampWasRebuilt()
	{
		return Enum.GetValues<FontRole>().Any((FontRole r) => !_ramp.ContainsKey(Fonts.For(r)));
	}

	private void TakeTheRebuiltRamp()
	{
		if (!TheRampWasRebuilt())
		{
			return;
		}
		Dictionary<FontRole, Font> dictionary = new Dictionary<FontRole, Font>();
		FontRole[] values = Enum.GetValues<FontRole>();
		foreach (FontRole fontRole in values)
		{
			dictionary[fontRole] = Fonts.For(fontRole);
		}
		if (_ramp.TryGetValue(Font, out var value))
		{
			Font = dictionary[value];
		}
		foreach (Control item in Everything(this))
		{
			if (_ramp.TryGetValue(item.Font, out var value2) && item.Font != dictionary[value2])
			{
				item.Font = dictionary[value2];
			}
		}
		SliderRow[] array = new SliderRow[14]
		{
			_saturation, _vibrance, _brightness, _gamma, _contrast, _temperature, _highlights, _shadows, _blacks, _whites,
			_shadowTint, _highlightTint, _eyeWarmth, _eyeDim
		};
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Restyle();
		}
		RememberTheRamp();
	}

	private static IEnumerable<Control> Everything(Control root)
	{
		foreach (Control child in root.Controls)
		{
			yield return child;
			foreach (Control item in Everything(child))
			{
				yield return item;
			}
		}
	}

	private static void OrderTabsByReading(Control? container)
	{
		if (container == null || container.Controls.Count < 2)
		{
			return;
		}
		List<List<Control>> list = new List<List<Control>>();
		foreach (Control item in from Control control in container.Controls
			orderby control.Top, control.Left
			select control)
		{
			object obj;
			if (list.Count != 0)
			{
				obj = list[list.Count - 1];
			}
			else
			{
				obj = null;
			}
			List<Control> list2 = (List<Control>)obj;
			int val = ((list2 != null) ? (Math.Min(list2[0].Height, item.Height) / 2) : 0);
			if (list2 != null && item.Top < list2[0].Top + Math.Max(1, val))
			{
				list2.Add(item);
				continue;
			}
			list.Add(new List<Control> { item });
		}
		int num = 0;
		foreach (List<Control> item2 in list)
		{
			foreach (Control item3 in item2.OrderBy((Control k) => k.Left))
			{
				if (item3.TabIndex != num)
				{
					item3.TabIndex = num;
				}
				num++;
			}
		}
	}

	private Label HeaderText(string text, Font font, Color colour)
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

	private Label PageLabel(string text, Font font, Color colour)
	{
		Label label = new Label
		{
			Text = text,
			Font = font,
			ForeColor = colour,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft
		};
		Controls.Add(label);
		return label;
	}

	private void Explain(SliderRow row, string text)
	{
		_presetTips.SetToolTip(row.Slider, text);
		row.SetToolTip(_presetTips, text);
	}

	private Label SectionLabel(string text, Control? parent = null)
	{
		Label label = new Label
		{
			Text = text,
			Font = Fonts.Heading,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft
		};
		(parent ?? _card).Controls.Add(label);
		return label;
	}

	private Label[] BuildTicks(Control parent, params string[] texts)
	{
		Label[] array = new Label[texts.Length];
		for (int i = 0; i < texts.Length; i++)
		{
			Label label = new Label
			{
				Text = texts[i],
				Font = Fonts.Caption,
				ForeColor = Theme.TextDim,
				BackColor = Color.Transparent,
				AutoSize = false,
				TextAlign = ((i == 0) ? ContentAlignment.MiddleLeft : ((i == texts.Length - 1) ? ContentAlignment.MiddleRight : ContentAlignment.MiddleCenter))
			};
			parent.Controls.Add(label);
			array[i] = label;
		}
		return array;
	}

	private Label CardLabel(string text, Control? parent = null)
	{
		Label label = new Label
		{
			Text = text,
			Font = Fonts.Label,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft
		};
		(parent ?? _card).Controls.Add(label);
		return label;
	}

	private SliderRow ToneRow(string caption, Action<int> apply, int value, Control? parent = null)
	{
		return Row(caption, -100, 100, 0, value, (int v) =>
		{
			apply(v);
			_settings.Tone = _engine.Tone;
		}, SliderRowShape.Standard, SliderPalette.Luminance, (int v) => (v <= 0) ? v.ToString(CultureInfo.InvariantCulture) : ("+" + v), parent);
	}

	private void ToggleCompare()
	{
		if (_ab.TryToggle())
		{
			_engine.PreviewNeutral(_ab.ShowingNeutral);
			_compare.Text = (_ab.ShowingNeutral ? "Showing off" : "Compare");
		}
	}

	private void RestoreFromCompare()
	{
		_ab.Reset();
		_engine.PreviewNeutral(on: false);
		_compare.Text = "Compare";
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		base.OnVisibleChanged(e);
		if (!Visible && _engine.IsPreviewingNeutral)
		{
			RestoreFromCompare();
		}
		if (Visible && _built)
		{
			RefreshColourReach();
		}
		if (Visible && _built)
		{
			RefreshTheMonitorLine();
		}
	}

	private static string TheMonitorLine()
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		try
		{
			DisplayMode? displayMode = DisplayController.Current();
			if (displayMode.HasValue)
			{
				DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
				if (valueOrDefault.Width > 0)
				{
					return string.Format(invariantCulture, "Display 1 · {0} × {1}, {2} Hz", valueOrDefault.Width, valueOrDefault.Height, valueOrDefault.RefreshHz);
				}
			}
		}
		catch
		{
		}
		Screen primaryScreen = Screen.PrimaryScreen;
		if (primaryScreen != null)
		{
			return string.Format(invariantCulture, "Display 1 · {0} × {1}", primaryScreen.Bounds.Width, primaryScreen.Bounds.Height);
		}
		return "Display 1";
	}

	private void RefreshTheMonitorLine()
	{
		string text = TheMonitorLine();
		if (!((_monitorLine ?? _liveDetail.Text) == text))
		{
			_monitorLine = text;
			_liveDetail.Text = text;
			LayoutContent();
		}
	}

	private static void PaintLiveTick(Graphics g, Rectangle box)
	{
		if (box.Width >= 6 && box.Height >= 6)
		{
			SignalIcons.Draw(g, SignalIcon.Success, box, ControlInk.Success);
		}
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		if (Environment.GetEnvironmentVariable("PLEXUSX_TIMING") == "selftest")
		{
			BeginInvoke(RunPresetSelfTest);
		}
	}

	private void RunPresetSelfTest()
	{
		try
		{
			ScenePresetSet scenePresetSet = ServedLooks.ForGame(_gamePicker.SelectedItem ?? "");
			Timing.Mark($"selftest: {scenePresetSet.Presets.Count} presets for '{_gamePicker.SelectedItem}'");
			for (int i = 1; i <= 2; i++)
			{
				foreach (DisplayPreset preset in scenePresetSet.Presets)
				{
					ApplyPreset(preset);
					Application.DoEvents();
				}
				Timing.Mark($"selftest: pass {i} done");
			}
			Timing.Mark("selftest: finished - closing");
		}
		catch (Exception ex)
		{
			Timing.Mark("selftest FAILED: " + ex.Message);
		}
		finally
		{
			Application.Exit();
		}
	}

	private void SetCodeBoxTip()
	{
		string text = ProfileCode.Encode(new ProfileCode(120, 230, 104, 112, 98, -14, -20, 30));
		_presetTips.SetToolTip(_codeBox, (text.Length == 0) ? null : ("A code looks like " + text + ". Paste one and press Apply."));
	}

	internal void ReloadServedLooks()
	{
		int generation = ServedLooks.Generation;
		if (generation == _looksGeneration)
		{
			if (_showLooksOnTheirWay && _looksOnTheirWay.Text != PcClock.WaitingWords())
			{
				DecideWhereTheLooksAre();
				if (_built)
				{
					LayoutContent();
				}
				Invalidate(invalidateChildren: true);
			}
			return;
		}
		ReloadingTheLooksForTest?.Invoke();
		DecideWhereTheLooksAre();
		string selectedItem = _gamePicker.SelectedItem;
		string wanted;
		if (string.IsNullOrEmpty(selectedItem))
		{
			wanted = ((!string.IsNullOrEmpty(_settings.ColourPresetGame)) ? _settings.ColourPresetGame : (_settings.FavoriteGame ?? ""));
		}
		else
		{
			wanted = selectedItem;
		}
		IReadOnlyList<string> games = ServedLooks.Games;
		_gamePicker.SetItems(games);
		int num = games.ToList().FindIndex((string g) => string.Equals(g, wanted, StringComparison.OrdinalIgnoreCase));
		if (num >= 0)
		{
			_gamePicker.SelectedIndex = num;
		}
		SetCodeBoxTip();
		if (_built)
		{
			LayoutContent();
		}
		_looksGeneration = generation;
		Invalidate(invalidateChildren: true);
	}

	private void DecideWhereTheLooksAre()
	{
		_showLooksOnTheirWay = !_locked && ServedLooks.OnTheirWay && ServedLooks.Games.Count == 0;
		_gamePicker.Visible = !_locked && !_showLooksOnTheirWay;
		_looksOnTheirWay.Visible = _showLooksOnTheirWay;
		_looksOnTheirWay.Text = (_showLooksOnTheirWay ? PcClock.WaitingWords() : "");
	}

	private void RebuildSceneChips()
	{
		foreach (SceneTile chip in _chips)
		{
			_presetTips.SetToolTip(chip, null);
			chip.Parent?.Controls.Remove(chip);
			chip.Dispose();
		}
		_chips.Clear();
		_presets.Clear();
		_gameTileCount = 0;
		IReadOnlyList<string> games = ServedLooks.Games;
		List<DisplayPreset> list = new List<DisplayPreset>();
		Image image = null;
		if (games.Count > 0)
		{
			int index = Math.Clamp(_gamePicker.SelectedIndex, 0, games.Count - 1);
			IReadOnlyList<DisplayPreset> presets = ServedLooks.ForGame(games[index]).Presets;
			list.AddRange(presets);
			_gameTileCount = presets.Count;
			DisplayPreset displayPreset = presets.FirstOrDefault((DisplayPreset p) => p.Glyph == "balanced") ?? presets.FirstOrDefault();
			if (displayPreset != null)
			{
				image = BrandAssets.PresetChip(displayPreset.Art);
			}
		}
		list.AddRange(_settings.CustomPresets);
		foreach (DisplayPreset item in list)
		{
			DisplayPreset captured = item;
			bool flag = item.Glyph == "custom";
			SceneTile sceneTile = new SceneTile
			{
				Caption = item.Name,
				Subtitle = item.Subtitle,
				Kind = item.Glyph,
				Reading = item.Saturation,
				Photo = (flag ? image : BrandAssets.PresetChip(item.Art)),
				ShowsTheLookOnThePhoto = flag,
				Matrix = ColourBalance.ComposeInto(ColorAdjust.BuildWithVibranceNumbers((float)item.Saturation / 100f, e.A(item.Vibrance, _engine.DriverState == VibranceDriverState.Available, false).Red, e.A(item.Vibrance, _engine.DriverState == VibranceDriverState.Available, false).Chroma, (float)item.Contrast / 100f, (float)item.Brightness / 100f, (float)item.Temperature / 100f, item.Depth().Clamped()), item.Hue, item.RedGain, item.GreenGain, item.BlueGain)
			};
			sceneTile.Click += (object? _, EventArgs _) =>
			{
				ApplyPreset(captured);
			};
			if (captured.Glyph == "custom")
			{
				sceneTile.Deletable = true;
				sceneTile.DeleteRequested += (object? _, EventArgs _) =>
				{
					ConfirmAndDeleteCustomPreset(captured);
				};
			}
			PresetKeyBinding presetKeyBinding = BindingFor(captured.Name);
			sceneTile.Bindable = true;
			sceneTile.BoundHotkey = ((presetKeyBinding == null) ? null : HotkeyPicker.GetDisplay(presetKeyBinding.ModifierMask, presetKeyBinding.VirtualKey));
			sceneTile.BindRequested += (object? _, EventArgs _) =>
			{
				PromptAndBindPreset(captured);
			};
			string text = item.Subtitle;
			if (sceneTile.BoundHotkey != null)
			{
				text = text + "  —  " + sceneTile.BoundHotkey;
			}
			if (captured.Glyph == "custom")
			{
				text += " - right-click to delete";
			}
			_presetTips.SetToolTip(sceneTile, text);
			_presets.Add(item);
			_chips.Add(sceneTile);
			((_chips.Count <= _gameTileCount) ? _presetsCard : _scenesCard).Controls.Add(sceneTile);
		}
		_deleteAllLooks.Enabled = _settings.CustomPresets.Count > 0;
		_presetTips.SetToolTip(_deleteAllLooks, _deleteAllLooks.Enabled ? "Delete all saved looks" : "No saved looks yet");
		if (_built)
		{
			UpdateActiveChip();
			LayoutContent();
		}
	}

	private void SaveCurrentAsCustom(string? wantedName)
	{
		if (_settings.CustomPresets.Count < 10)
		{
			string name = CustomPresetNaming.Unique(wantedName, _settings.CustomPresets.Select((DisplayPreset p) => p.Name));
			_settings.CustomPresets.Add(new DisplayPreset(name, "Your own look", _saturation.Slider.Value, _vibrance.Slider.Value, _brightness.Slider.Value, _gamma.Slider.Value, _contrast.Slider.Value, _temperature.Slider.Value, "custom", "", _highlights.Slider.Value, _shadows.Slider.Value, NightVision: false, ColourVisionMode.Off, _blacks.Slider.Value, _whites.Slider.Value, _shadowTint.Slider.Value, _highlightTint.Slider.Value, _engine.Mix.IsIdentity ? null : _engine.Mix, _hue.Value, _red.Value, _green.Value, _blue.Value, _engine.Depth.Formula, _engine.Depth.Darken, _engine.Depth.DarkenHue, _engine.Depth.HueTurn, _engine.Depth.HueTurnAmount, _engine.Depth.HueComplement));
			_store.Save(_settings);
			RebuildSceneChips();
		}
	}

	internal void SaveSharedCodeAsLook(string? code, string? wantedName = null)
	{
		if (ServedCodes.Current.Withheld)
		{
			SetShareStatus("Share codes aren't working on this PC right now. Nothing was copied, changed or saved.", ok: false);
			return;
		}
		if (!ProfileCode.TryDecode(code, out var profile))
		{
			SetShareStatus("That code isn't valid. Nothing changed.", ok: false);
			return;
		}
		if (_settings.CustomPresets.Count >= 10)
		{
			SetShareStatus($"You already have {10} saved looks - the most this page " + "holds. Delete one first.", ok: false);
			return;
		}
		string text = CustomPresetNaming.Unique(wantedName ?? "Their look", _settings.CustomPresets.Select((DisplayPreset p) => p.Name));
		_settings.CustomPresets.Add(new DisplayPreset(text, "From a shared code", profile.Saturation, profile.Vibrance, profile.Brightness, profile.Gamma, profile.Contrast, profile.Temperature, "custom", "", profile.Highlights, profile.Shadows, NightVision: false, ColourVisionMode.Off, 0, 0, 0, 0, null, profile.Hue, profile.RedGain, profile.GreenGain, profile.BlueGain, profile.Formula, profile.Darken, profile.DarkenHue, profile.HueTurn, profile.HueTurnAmount, profile.HueComplement));
		_store.Save(_settings);
		RebuildSceneChips();
		SetShareStatus("Saved as \"" + text + "\" — it's in your looks now.", ok: true);
	}

	private void ConfirmAndDeleteCustomPreset(DisplayPreset preset)
	{
		if (GlassDialog.Show(FindForm(), "Delete this look?", "\"" + preset.Name + "\" will be removed. There is no undo - saving it again is the only way back.", GlassDialogButtons.Destructive, GlassDialogTone.Warning, "Delete look", "Keep it") == DialogResult.Yes)
		{
			DeleteCustomPreset(preset);
		}
	}

	private void DeleteCustomPreset(DisplayPreset preset)
	{
		int num = _settings.CustomPresets.FindIndex((DisplayPreset p) => string.Equals(p.Name, preset.Name, StringComparison.OrdinalIgnoreCase));
		if (num >= 0)
		{
			_settings.CustomPresets.RemoveAt(num);
			bool flag = _settings.PresetKeyBindings.RemoveAll((PresetKeyBinding presetKeyBinding) => string.Equals(presetKeyBinding.Name, preset.Name, StringComparison.OrdinalIgnoreCase)) > 0;
			_store.Save(_settings);
			if (flag)
			{
				PresetBindingsChanged?.Invoke();
			}
			RebuildSceneChips();
		}
	}

	private void PromptAndSaveCustom()
	{
		if (_settings.CustomPresets.Count >= 10)
		{
			GlassDialog.Show(FindForm(), "Presets full", $"You already have {10} saved looks - the most this page " + "keeps. Right-click a saved look and choose Delete look to make room.");
		}
		else
		{
			string text = Prompt.Ask("Save this look as", "My look", "Give this look a name.");
			if (!string.IsNullOrWhiteSpace(text))
			{
				SaveCurrentAsCustom(text);
			}
		}
	}

	private void ConfirmAndDeleteAllCustomPresets()
	{
		if (_settings.CustomPresets.Count != 0 && GlassDialog.Show(FindForm(), "Delete all saved looks?", (_settings.CustomPresets.Count == 1) ? ("This removes your saved look \"" + _settings.CustomPresets[0].Name + "\". Scene presets are not affected.") : $"This removes all {_settings.CustomPresets.Count} of your saved looks. Scene presets are not affected.", GlassDialogButtons.Destructive, GlassDialogTone.Warning, "Delete all looks", "Keep them") == DialogResult.Yes)
		{
			_settings.CustomPresets.Clear();
			_store.Save(_settings);
			RebuildSceneChips();
		}
	}

	internal void TestSaveCurrentAsCustom(string name)
	{
		SaveCurrentAsCustom(name);
	}

	internal void TestDeleteAllCustomPresets()
	{
		_settings.CustomPresets.Clear();
		_store.Save(_settings);
		RebuildSceneChips();
	}

	internal void TestDeleteCustomPreset(string name)
	{
		DeleteCustomPreset(new DisplayPreset(name, "", 100, 50, 100, 100, 100, 0, "custom", ""));
	}

	internal void TestSetGrade(int blacks, int whites, int shadowTint, int highlightTint)
	{
		_blacks.Slider.Value = blacks;
		_whites.Slider.Value = whites;
		_shadowTint.Slider.Value = shadowTint;
		_highlightTint.Slider.Value = highlightTint;
	}

	internal void TestSetSlidersForTest(int saturation, int vibrance, int brightness, int gamma, int contrast, int temperature, int highlights, int shadows)
	{
		_applyingPreset = true;
		try
		{
			_saturation.Slider.Value = saturation;
			_vibrance.Slider.Value = vibrance;
			_brightness.Slider.Value = brightness;
			_gamma.Slider.Value = gamma;
			_contrast.Slider.Value = contrast;
			_temperature.Slider.Value = temperature;
			_highlights.Slider.Value = highlights;
			_shadows.Slider.Value = shadows;
		}
		finally
		{
			_applyingPreset = false;
		}
	}

	internal void TestApplyPresetByName(string name)
	{
		DisplayPreset displayPreset = _presets.FirstOrDefault((DisplayPreset p) => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
		if (displayPreset != null)
		{
			ApplyPreset(displayPreset);
		}
	}

	private void ApplyColourVision(ColourVisionMode mode)
	{
		LookCorrection.ChosenByThem(_settings, mode);
		SetLookNote("", ok: true);
		WriteColourVision(mode);
		UpdateActiveChip();
		_store.Save(_settings);
	}

	private void WriteColourVision(ColourVisionMode mode)
	{
		_engine.SetAsideCorrection = LookCorrection.SetAside(_settings);
		_engine.ColourVision = mode;
		_settings.ColourVision = mode;
		ShowCorrectionOnTheChips(mode);
	}

	private void ShowCorrectionOnTheChips(ColourVisionMode mode)
	{
		for (int i = 0; i < _visionChips.Count; i++)
		{
			_visionChips[i].Active = VisionModes[i].Mode == mode;
		}
	}

	private void BringLookCorrection(ColourVisionMode mode)
	{
		LookCorrection.BroughtByALook(_settings, mode);
		WriteColourVision(mode);
	}

	private void ReturnTheirCorrection()
	{
		ColourVisionMode? colourVisionMode = LookCorrection.GiveTheirsBack(_settings);
		if (colourVisionMode.HasValue)
		{
			ColourVisionMode valueOrDefault = colourVisionMode.GetValueOrDefault();
			WriteColourVision(valueOrDefault);
		}
	}

	private void SyncAdvancedRows()
	{
		ToneSettings tone = _engine.Tone;
		_highlights.Slider.Value = Math.Clamp(tone.Highlights, -100, 100);
		_shadows.Slider.Value = Math.Clamp(tone.Shadows, -100, 100);
		_blacks.Slider.Value = Math.Clamp(tone.Blacks, -100, 100);
		_whites.Slider.Value = Math.Clamp(tone.Whites, -100, 100);
		_shadowTint.Slider.Value = Math.Clamp(tone.ShadowTint, -100, 100);
		_highlightTint.Slider.Value = Math.Clamp(tone.HighlightTint, -100, 100);
	}

	private SliderRow Row(string caption, int min, int max, int? notch, int value, Action<int> apply, SliderRowShape shape = SliderRowShape.Standard, SliderPalette? palette = null, Func<int, string>? format = null, Control? parent = null, decimal valuePerStep = 1m)
	{
		SliderRow row = new SliderRow(parent ?? _card, caption, min, max, notch, value, shape, palette, format, valuePerStep);
		row.Slider.AccessibleName = caption;
		row.Slider.ValueChanged += (object? _, EventArgs _) =>
		{
			if (!_syncingFromEngine)
			{
				if (_engine.IsPreviewingNeutral && !_applyingPreset)
				{
					RestoreFromCompare();
				}
				apply(row.Slider.Value);
				_saveDebounce.Trigger();
				SliderMovedByUser?.Invoke();
				if (!_dragging && !_applyingPreset)
				{
					UpdateActiveChip();
				}
			}
		};
		row.Slider.DragBegin += (object? _, EventArgs _) =>
		{
			_dragging = true;
			_engine.BeginDrag();
		};
		row.Slider.DragEnd += (object? _, EventArgs _) =>
		{
			_dragging = false;
			_engine.EndDrag();
			UpdateActiveChip();
		};
		return row;
	}

	private MixChannel Mix(int min, int max, int value, Action<int> apply)
	{
		MixChannel channel = new MixChannel(min, max, value);
		channel.Changed += () =>
		{
			if (!_syncingFromEngine)
			{
				if (_engine.IsPreviewingNeutral && !_applyingPreset)
				{
					RestoreFromCompare();
				}
				apply(channel.Value);
				_saveDebounce.Trigger();
				SliderMovedByUser?.Invoke();
				if (!_dragging && !_applyingPreset)
				{
					UpdateActiveChip();
				}
			}
		};
		return channel;
	}

	private void LayoutIfTheSizeChanged()
	{
		if (!(Size == _laidOutFor) || TheRampWasRebuilt())
		{
			LayoutContent();
		}
	}

	private void LayoutContent()
	{
		if (Width <= 0 || _laying)
		{
			return;
		}
		_laying = true;
		try
		{
			LayoutContentCore();
			_laidOutFor = Size;
		}
		finally
		{
			_laying = false;
		}
	}

	private void ShowPaidSections(bool show)
	{
		_vibrance.Visible = show;
		_vibranceHint.Visible = show;
		Label[] vibranceTicks = _vibranceTicks;
		for (int i = 0; i < vibranceTicks.Length; i++)
		{
			vibranceTicks[i].Visible = show;
		}
		Control[] array = new Control[15]
		{
			_fineLabel, _fineHint, _resetFine, _presetsLabel, _saveLook, _deleteAllLooks, _shareLabel, _shareHint, _codeBox, _copyCode,
			_applyCode, _keepCode, _copySetup, _setupHint, _shareStatus
		};
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Visible = show;
		}
		DecideWhereTheLooksAre();
		SliderRow[] array2 = new SliderRow[14]
		{
			_brightness, _gamma, _contrast, _temperature, _highlights, _shadows, _blacks, _whites, _shadowTint, _highlightTint,
			_turnRow, _darkenRow, _amountRow, _complementRow
		};
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Visible = show;
		}
		foreach (SceneTile chip in _chips)
		{
			chip.Visible = show;
		}
	}

	internal void Unlock()
	{
		if (_paidSectionsHidden)
		{
			_paidSectionsHidden = false;
			ShowPaidSections(show: true);
			_phone.Visible = _onOpenPhone != null;
			SyncMatrixRows();
			LayoutContent();
			Invalidate(invalidateChildren: true);
		}
	}

	private int PlaceColourReach(int x, int y, int width)
	{
		return PlaceWrappedLine(_colourReach, x, y, width);
	}

	private int PlaceLookNote(int x, int y, int width)
	{
		return PlaceWrappedLine(_lookNote, x, y, width);
	}

	private static int PlaceWrappedLine(Label line, int x, int y, int width)
	{
		if (line.Text.Length == 0)
		{
			line.SetBounds(x, y, width, 0);
			return y;
		}
		int num = y + Tokens.Scale(4);
		int num2 = Math.Max(Tokens.Scale(20), TextRenderer.MeasureText(line.Text, line.Font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak).Height);
		line.SetBounds(x, num, width, num2);
		return num + num2;
	}

	private int LayoutHeadlineCard(int width)
	{
		int headlinePadX = HeadlinePadX;
		int headlinePadTop = HeadlinePadTop;
		int num = (_locked ? width : (width / 2));
		int x = headlinePadX;
		int width2 = num - 2 * headlinePadX;
		int x2 = num + headlinePadX;
		int width3 = width - num - 2 * headlinePadX;
		int width4 = LayoutHeadlineColumn(_saturation, _saturationHint, x, headlinePadTop, width2);
		_headlineRule.Visible = true;
		int width5 = LayoutHeadlineColumn(_vibrance, _vibranceHint, x2, headlinePadTop, width3);
		int num2 = (_saturationRowBottom = headlinePadTop + SliderRow.HeadlineRowHeight + Math.Max(_saturation.AvailabilityHintHeight, _vibrance.AvailabilityHintHeight));
		int num3 = PlaceColourReach(x, num2, width2);
		if (num3 == num2)
		{
			num3 -= 2;
			_colourReach.Top = num3;
		}
		int num4 = PlaceTicks(_saturationTicks, x, num3, width4);
		num4 = Math.Max(num4, PlaceTicks(_vibranceTicks, x2, num3, width5, VibranceTickFractions));
		int num5 = num4 + HeadlinePadBottom;
		int num6 = 1;
		_headlineRule.SetBounds(num, num6, Math.Max(1, Tokens.Scale(1)), Math.Max(1, num5 - 2 * num6));
		return num5;
	}

	private int LayoutHeadlineColumn(SliderRow row, Label hint, int x, int y, int width)
	{
		Fonts.LineHeight(FontRole.Heading);
		Size size = TextRenderer.MeasureText(hint.Text, hint.Font);
		int num = Math.Min(width / 2, size.Width + Tokens.Scale(12));
		hint.SetBounds(x + width - num, y + Tokens.Scale(2), num, Math.Max(Fonts.LineHeight(FontRole.Caption), size.Height));
		int num2 = Math.Max(Tokens.Scale(120), width);
		row.PlaceHeadline(x, y, num2, width - num - Tokens.Scale(8));
		return num2;
	}

	private static int PlaceTicks(Label[] ticks, int x, int y, int width, float[]? fractions = null)
	{
		if (ticks.Length == 0)
		{
			return y;
		}
		int num = Math.Max(Fonts.LineHeight(FontRole.Caption), ticks.Max((Label t) => TextRenderer.MeasureText(t.Text, t.Font).Height));
		for (int num2 = 0; num2 < ticks.Length; num2++)
		{
			float num3;
			if (fractions != null && num2 < fractions.Length)
			{
				num3 = fractions[num2];
			}
			else
			{
				num3 = ((ticks.Length == 1) ? 0f : ((float)num2 / (float)(ticks.Length - 1)));
			}
			int num4 = x + (int)Math.Round(num3 * (float)width);
			int num5 = Math.Max(Tokens.Scale(36), TextRenderer.MeasureText(ticks[num2].Text, ticks[num2].Font).Width + Tokens.Scale(6));
			int x2 = Math.Clamp(num4 - num5 / 2, x, x + width - num5);
			ticks[num2].SetBounds(x2, y, num5, num);
		}
		return y + num;
	}

	private int LayoutFineCard(int width)
	{
		int cardPad = CardPad;
		int num = width - 2 * cardPad;
		int num2 = cardPad;
		int cardPadTop = CardPadTop;
		int num3 = Fonts.LineHeight(FontRole.Heading);
		_fineLabel.SetBounds(num2, cardPadTop + (ResetH - num3) / 2, num, num3);
		cardPadTop += ResetH + Tokens.Scale(2);
		int num4 = Fonts.LineHeight(FontRole.Caption);
		int num5 = Math.Max(num4, TextRenderer.MeasureText(_fineHint.Text, _fineHint.Font, new Size(num, int.MaxValue), TextFormatFlags.WordBreak).Height);
		int num6 = Math.Max(1, (int)Math.Round((double)num5 / (double)_fineHint.Font.Height));
		_fineHint.SetBounds(num2, cardPadTop, num, num5);
		cardPadTop += Math.Max(num6 * num4 + Tokens.Scale(12), num5 + Tokens.Scale(4));
		int num7 = (num - FineColGap) / 2;
		int x = num2 + num7 + FineColGap;
		(SliderRow, SliderRow)[] array = new (SliderRow, SliderRow)[5]
		{
			(_brightness, _gamma),
			(_contrast, _temperature),
			(_highlights, _shadows),
			(_blacks, _whites),
			(_shadowTint, _highlightTint)
		};
		for (int i = 0; i < array.Length; i++)
		{
			int y = cardPadTop;
			array[i].Item1.PlaceCompact(num2, y, num7);
			array[i].Item2.PlaceCompact(x, y, num7);
			cardPadTop += ((i == array.Length - 1) ? SliderRow.CompactRowHeight : SliderRow.CompactRowPitch) + Math.Max(array[i].Item1.AvailabilityHintHeight, array[i].Item2.AvailabilityHintHeight);
		}
		return cardPadTop + CardPadTop;
	}

	private void ShowDepthCard(bool show)
	{
		if (_depthShown != show)
		{
			_depthCard.Visible = show;
			_depthShown = show;
		}
	}

	private int LayoutDepthCard(int width)
	{
		int cardPad = CardPad;
		int num = width - 2 * cardPad;
		int num2 = cardPad;
		int cardPadTop = CardPadTop;
		int num3 = Fonts.LineHeight(FontRole.Heading);
		int num4 = Fonts.LineHeight(FontRole.Caption);
		int num5 = Fonts.LineHeight(FontRole.Label);
		Size naturalSize = _depthInUse.NaturalSize;
		_depthInUse.SetBounds(num2 + num - naturalSize.Width, cardPadTop + (ResetH - naturalSize.Height) / 2, naturalSize.Width, naturalSize.Height);
		_depthLabel.SetBounds(num2, cardPadTop + (ResetH - num3) / 2, num - naturalSize.Width - S(8), num3);
		cardPadTop += ResetH + S(2);
		cardPadTop = PlaceWrappedLine(_depthHint, num2, cardPadTop - S(4), num) + S(14);
		_formulaLabel.SetBounds(num2, cardPadTop, num, num5);
		cardPadTop += num5 + S(8);
		int num6 = S(8);
		int num7 = S(32);
		int num8 = 5;
		int num9 = (num - (num8 - 1) * num6) / num8;
		for (int i = 0; i < _formulaChips.Count; i++)
		{
			int num10 = i / num8;
			int num11 = i % num8;
			_formulaChips[i].SetBounds(num2 + num11 * (num9 + num6), cardPadTop + num10 * (num7 + num6), num9, num7);
		}
		int num12 = (_formulaChips.Count + num8 - 1) / num8;
		cardPadTop += num12 * num7 + (num12 - 1) * num6 + S(20);
		int num13 = S(24);
		int num14 = (num - num13) / 2;
		int num15 = num2 + num14 + num13;
		_hueWheelLabel.SetBounds(num2, cardPadTop, num14, num5 + S(2));
		_darkenWheelLabel.SetBounds(num15, cardPadTop, num14, num5 + S(2));
		cardPadTop += num5 + S(8);
		int num16 = Math.Min(S(260), num14);
		double num17 = (double)(num - num13) / 2.0;
		_hueWheel.SetBounds(num2 + (int)Math.Round(num17 / 2.0 - (double)num16 / 2.0, MidpointRounding.AwayFromZero), cardPadTop, num16, num16);
		_darkenWheel.SetBounds(num2 + (int)Math.Round(num17 + (double)num13 + num17 / 2.0 - (double)num16 / 2.0, MidpointRounding.AwayFromZero), cardPadTop, num16, num16);
		cardPadTop += num16 + S(4);
		_hueWheelHint.SetBounds(num2, cardPadTop, num14, num4 + S(2));
		_darkenWheelHint.SetBounds(num15, cardPadTop, num14, num4 + S(2));
		cardPadTop += num4 + S(16);
		int num18 = (Math.Max(num13, 2 * TwoColorSlider.EdgeInset) - num13 + 1) / 2;
		_turnRow.PlaceCompact(num2, cardPadTop, num14 - num18);
		_darkenRow.PlaceCompact(num15 + num18, cardPadTop, num14 - num18);
		cardPadTop += SliderRow.CompactRowPitch + Math.Max(_turnRow.AvailabilityHintHeight, _darkenRow.AvailabilityHintHeight);
		_amountRow.PlaceCompact(num2, cardPadTop, num14 - num18);
		_complementRow.PlaceCompact(num15 + num18, cardPadTop, num14 - num18);
		cardPadTop += SliderRow.CompactRowHeight + Math.Max(_amountRow.AvailabilityHintHeight, _complementRow.AvailabilityHintHeight);
		return cardPadTop + CardPadTop;
		static int S(int px)
		{
			return Tokens.Scale(px);
		}
	}

	internal static string DarkenCaptionFor(int darkenHue)
	{
		return HueWheel.NearestName(darkenHue) + " darken";
	}

	private void BuildColourDepthCard()
	{
		ColourDepth depth = _engine.Depth;
		_depthLabel = SectionLabel("Colour depth", _depthCard);
		_depthInUse = new InUseTag("In this look")
		{
			Visible = false
		};
		_depthCard.Controls.Add(_depthInUse);
		_depthHint = CardLabel("Which saturation PlexusX uses, and how far the colours are pushed. The formula travels with the look, so a preset can bring its own.", _depthCard);
		_depthHint.Font = Fonts.Caption;
		_depthHint.ForeColor = Theme.TextDim;
		_formulaLabel = CardLabel("Formula", _depthCard);
		_formulaLabel.Font = Fonts.BodyBold;
		_formulaLabel.ForeColor = Theme.Text;
		foreach (SaturationMetricInfo item in SaturationMetrics.All)
		{
			SaturationMetric kind = item.Kind;
			GlassButton glassButton = new GlassButton
			{
				Text = item.Short,
				Kind = GlassButtonKind.Secondary
			};
			glassButton.AccessibleName = "Formula " + item.Short;
			glassButton.Click += (object? _, EventArgs _) =>
			{
				SetDepth((ColourDepth colourDepth) => colourDepth with
				{
					Formula = kind
				});
			};
			_presetTips.SetToolTip(glassButton, item.Tip);
			_formulaChips.Add(glassButton);
			_depthCard.Controls.Add(glassButton);
		}
		_hueWheelLabel = CardLabel("Hue turn", _depthCard);
		_hueWheelLabel.Font = Fonts.BodyBold;
		_hueWheelLabel.ForeColor = Theme.Text;
		_hueWheelLabel.TextAlign = ContentAlignment.MiddleCenter;
		_hueWheel = new HueWheel
		{
			Hue = depth.HueTurn,
			Ring = true
		};
		_hueWheel.AccessibleName = "Hue turn";
		_depthCard.Controls.Add(_hueWheel);
		_hueWheelHint = CardLabel("Drag or scroll. Double-click resets.", _depthCard);
		_hueWheelHint.Font = Fonts.Caption;
		_hueWheelHint.ForeColor = Theme.TextFaint;
		_hueWheelHint.TextAlign = ContentAlignment.MiddleCenter;
		_darkenWheelLabel = CardLabel("Darken a colour", _depthCard);
		_darkenWheelLabel.Font = Fonts.BodyBold;
		_darkenWheelLabel.ForeColor = Theme.Text;
		_darkenWheelLabel.TextAlign = ContentAlignment.MiddleCenter;
		_darkenWheel = new HueWheel
		{
			DarkenMode = true,
			Ring = true,
			DarkenHue = depth.DarkenHue,
			Darken = depth.Darken
		};
		_darkenWheel.AccessibleName = "Darken a colour";
		_darkenWheel.DarkenChanged += (object? _, EventArgs _) =>
		{
			if (!_syncingFromEngine)
			{
				if (_engine.IsPreviewingNeutral)
				{
					RestoreFromCompare();
				}
				SetDepth((ColourDepth colourDepth) => colourDepth with
				{
					DarkenHue = _darkenWheel.DarkenHue,
					Darken = _darkenWheel.Darken
				});
				_syncingFromEngine = true;
				try
				{
					_darkenRow.Slider.Value = _darkenWheel.Darken;
				}
				finally
				{
					_syncingFromEngine = false;
				}
				_darkenRow.SyncValueText();
			}
		};
		_depthCard.Controls.Add(_darkenWheel);
		_darkenWheelHint = CardLabel("Pick the colour, then set how much.", _depthCard);
		_darkenWheelHint.Font = Fonts.Caption;
		_darkenWheelHint.ForeColor = Theme.TextFaint;
		_darkenWheelHint.TextAlign = ContentAlignment.MiddleCenter;
		_turnRow = Row("Hue turn", -180, 180, 0, depth.HueTurn, (int v) =>
		{
			SetDepth((ColourDepth colourDepth) => colourDepth with
			{
				HueTurn = v
			});
		}, SliderRowShape.Standard, SliderPalette.Luminance, TurnText, _depthCard);
		_turnRow.Slider.ValueChanged += (object? _, EventArgs _) =>
		{
			_hueWheel.Hue = _turnRow.Slider.Value;
		};
		_hueWheel.HueChanged += (object? _, EventArgs _) =>
		{
			_turnRow.Slider.Value = _hueWheel.Hue;
		};
		int count = _depthCard.Controls.Count;
		_darkenRow = Row(DarkenCaptionFor(depth.DarkenHue), 0, 400, 0, depth.Darken, (int v) =>
		{
			SetDepth((ColourDepth colourDepth) => colourDepth with
			{
				Darken = v
			});
		}, SliderRowShape.Standard, SliderPalette.Luminance, (int v) => $"{v}%", _depthCard);
		_darkenCaption = (Label)_depthCard.Controls[count];
		_darkenRow.Slider.ValueChanged += (object? _, EventArgs _) =>
		{
			_darkenWheel.Darken = _darkenRow.Slider.Value;
		};
		_amountRow = Row("Turn amount", 0, 100, 100, depth.HueTurnAmount, (int v) =>
		{
			SetDepth((ColourDepth colourDepth) => colourDepth with
			{
				HueTurnAmount = v
			});
		}, SliderRowShape.Standard, SliderPalette.Accent(), (int v) => $"{v}%", _depthCard);
		_complementRow = Row("Complement", 0, 100, 0, depth.HueComplement, (int v) =>
		{
			SetDepth((ColourDepth colourDepth) => colourDepth with
			{
				HueComplement = v
			});
		}, SliderRowShape.Standard, SliderPalette.Accent(), (int v) => $"{v}%", _depthCard);
		ShowTheLiveFormula();
	}

	internal static string TurnText(int value)
	{
		return ((value > 0) ? "+" : "") + value.ToString(CultureInfo.InvariantCulture) + "°";
	}

	private void SetDepth(Func<ColourDepth, ColourDepth> change)
	{
		if (!_syncingFromEngine)
		{
			ColourDepth colourDepth = change(_engine.Depth).Clamped();
			if (colourDepth != _engine.Depth)
			{
				_engine.Depth = colourDepth;
			}
			_settings.Depth = _engine.Depth;
			_depthInUse.Visible = false;
			ShowTheLiveFormula();
			_darkenCaption.Text = DarkenCaptionFor(_engine.Depth.DarkenHue);
			_saveDebounce.Trigger();
			if (!_dragging && !_applyingPreset)
			{
				UpdateActiveChip();
			}
		}
	}

	private void ShowTheLiveFormula()
	{
		SaturationMetric formula = _engine.Depth.Formula;
		for (int i = 0; i < _formulaChips.Count; i++)
		{
			GlassButtonKind glassButtonKind = ((SaturationMetrics.All[i].Kind != formula) ? GlassButtonKind.Secondary : GlassButtonKind.Primary);
			if (_formulaChips[i].Kind != glassButtonKind)
			{
				_formulaChips[i].Kind = glassButtonKind;
			}
		}
	}

	private void SyncDepthControls()
	{
		ColourDepth depth = _engine.Depth;
		bool syncingFromEngine = _syncingFromEngine;
		_syncingFromEngine = true;
		try
		{
			_turnRow.Slider.Value = depth.HueTurn;
			_darkenRow.Slider.Value = depth.Darken;
			_amountRow.Slider.Value = depth.HueTurnAmount;
			_complementRow.Slider.Value = depth.HueComplement;
			_hueWheel.Hue = depth.HueTurn;
			_darkenWheel.DarkenHue = depth.DarkenHue;
			_darkenWheel.Darken = depth.Darken;
		}
		finally
		{
			_syncingFromEngine = syncingFromEngine;
		}
		_darkenCaption.Text = DarkenCaptionFor(depth.DarkenHue);
		SliderRow[] array = new SliderRow[4] { _turnRow, _darkenRow, _amountRow, _complementRow };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SyncValueText();
		}
		ShowTheLiveFormula();
	}

	private void MarkDepthInTheLook()
	{
		_depthInUse.Visible = !_engine.Depth.IsNeutral && !_locked;
	}

	private int LayoutPresetsCard(int width)
	{
		int cardPad = CardPad;
		int num = width - 2 * cardPad;
		int num2 = cardPad;
		int cardPadTop = CardPadTop;
		int num3 = Fonts.LineHeight(FontRole.Heading);
		int resetH = ResetH;
		int width2 = _saveLook.GetPreferredSize(Size.Empty).Width;
		int num4 = width2 + Tokens.Scale(8) + resetH + Tokens.Scale(8);
		_saveLook.SetBounds(num2 + num - width2, cardPadTop, width2, ResetH);
		_deleteAllLooks.SetBounds(_saveLook.Left - resetH - Tokens.Scale(8), cardPadTop, resetH, ResetH);
		_presetsLabel.SetBounds(num2, cardPadTop + (ResetH - num3) / 2, num - num4, num3);
		cardPadTop += ResetH;
		cardPadTop += Tokens.Scale(12);
		int num5 = Tokens.Scale(32);
		_gamePicker.SetBounds(num2, cardPadTop, num, num5);
		if (_showLooksOnTheirWay)
		{
			cardPadTop = PlaceWrappedLine(_looksOnTheirWay, num2, cardPadTop - Tokens.Scale(4), num);
		}
		else
		{
			_looksOnTheirWay.SetBounds(num2, cardPadTop, num, 0);
			cardPadTop += num5;
		}
		if (_gameTileCount > 0)
		{
			cardPadTop = PlaceTileGrid(_presetsCard, 0, _gameTileCount, 4, num2, cardPadTop + Tokens.Scale(12), num);
		}
		if (!ActiveIsSavedLook)
		{
			if (_lookNote.Parent != _presetsCard)
			{
				_presetsCard.Controls.Add(_lookNote);
			}
			cardPadTop = PlaceLookNote(num2, cardPadTop, num);
		}
		return cardPadTop + CardPadTop;
	}

	private int PlaceTileGrid(CardPanel host, int from, int count, int perRow, int x0, int y, int innerW)
	{
		if (count <= 0)
		{
			return y;
		}
		int sceneGap = SceneGap;
		int num = (innerW - (perRow - 1) * sceneGap) / perRow;
		int num2 = innerW - (perRow - 1) * sceneGap - num * perRow;
		int num3 = SceneTile.BoxHeight + sceneGap;
		for (int i = 0; i < count; i++)
		{
			int num4 = i / perRow;
			int num5 = i % perRow;
			int x1 = x0 + num5 * (num + sceneGap) + Math.Min(num5, num2);
			int width = num + ((num5 < num2) ? 1 : 0);
			SceneTile sceneTile = _chips[from + i];
			if (sceneTile.Parent != host)
			{
				host.Controls.Add(sceneTile);
			}
			sceneTile.SetBounds(x1, y + num4 * num3, width, SceneTile.ControlHeight);
		}
		int num6 = (count + perRow - 1) / perRow;
		return y + num6 * SceneTile.BoxHeight + (num6 - 1) * sceneGap;
	}

	private int LayoutScenesCard(int width)
	{
		bool flag = !_locked && _chips.Count > _gameTileCount;
		if (_scenesShown != flag)
		{
			_scenesCard.Visible = flag;
			_scenesShown = flag;
		}
		if (!flag)
		{
			return 0;
		}
		int cardPad = CardPad;
		int num = width - 2 * cardPad;
		int cardPadTop = CardPadTop;
		cardPadTop = PlaceTileGrid(_scenesCard, _gameTileCount, _chips.Count - _gameTileCount, 10, cardPad, cardPadTop, num);
		if (ActiveIsSavedLook)
		{
			if (_lookNote.Parent != _scenesCard)
			{
				_scenesCard.Controls.Add(_lookNote);
			}
			cardPadTop = PlaceLookNote(cardPad, cardPadTop, num);
		}
		return cardPadTop + CardPadTop;
	}

	private int LayoutTrailingContent(int y, int leftX, int rightX, int colW, int innerW, bool includeShare)
	{
		_card.SuspendLayout();
		try
		{
			_visionLabel.SetBounds(leftX, y, innerW, SectionLabelH);
			y += SectionLabelH + Tokens.Scale(4);
			_visionHint.SetBounds(leftX, y, innerW, Tokens.Scale(18));
			y += Tokens.Scale(22);
			int num = Tokens.Scale(8);
			int num2 = (innerW - 3 * num) / 4;
			int num3 = Tokens.Scale(28);
			for (int i = 0; i < _visionChips.Count; i++)
			{
				_visionChips[i].SetBounds(leftX + i * (num2 + num), y, num2, num3);
			}
			y += num3 + SectionGap;
			_shortcutsLabel.SetBounds(leftX, y, innerW, SectionLabelH);
			y += SectionLabelH + Tokens.Scale(6);
			int num4 = Tokens.Scale(HotkeyPicker.PickerDefaultSize.Height);
			_popupHotkeyLabel.SetBounds(leftX, y, colW, Fonts.LineHeight(FontRole.Label));
			_mainHotkeyLabel.SetBounds(rightX, y, colW, Fonts.LineHeight(FontRole.Label));
			y += Tokens.Scale(20);
			_hotkeyPicker.SetBounds(leftX, y, colW, num4);
			_mainHotkeyPicker.SetBounds(rightX, y, colW, num4);
			y += num4 + Tokens.Scale(10);
			_screenshotHotkeyLabel.SetBounds(leftX, y, colW, Fonts.LineHeight(FontRole.Label));
			y += Tokens.Scale(20);
			_screenshotHotkeyPicker.SetBounds(leftX, y, colW, num4);
			y += num4 + SectionGap;
			if (includeShare)
			{
				_shareLabel.SetBounds(leftX, y, innerW, SectionLabelH);
				y += SectionLabelH + Tokens.Scale(4);
				_shareHint.SetBounds(leftX, y, innerW, Tokens.Scale(20));
				y += Tokens.Scale(26);
				int num5 = Tokens.Scale(30);
				int num6 = Tokens.Scale(84);
				int num7 = Tokens.Scale(84);
				int num8 = Tokens.Scale(120);
				int num9 = Tokens.Scale(8);
				int num10 = innerW - num6 - num7 - num8 - 3 * num9;
				int num11 = Tokens.Scale(32);
				_codeBox.SetBounds(leftX, y + (num5 - num11) / 2, num10, num11);
				_copyCode.SetBounds(leftX + num10 + num9, y, num8, num5);
				_applyCode.SetBounds(leftX + num10 + num8 + 2 * num9, y, num6, num5);
				_keepCode.SetBounds(leftX + num10 + num8 + num6 + 3 * num9, y, num7, num5);
				y += num5 + Tokens.Scale(10);
				int num12 = Tokens.Scale(148);
				_copySetup.SetBounds(leftX, y, num12, num5);
				_setupHint.SetBounds(leftX + num12 + num9, y + (num5 - Tokens.Scale(20)) / 2, innerW - num12 - num9, Tokens.Scale(20));
				y += num5;
				if (_shareStatus.Text.Length == 0)
				{
					_shareStatus.SetBounds(leftX, y, innerW, 0);
					_shareStatus.Visible = false;
				}
				else
				{
					_shareStatus.Visible = true;
					y += Tokens.Scale(6);
					int num13 = Math.Max(Tokens.Scale(20), TextRenderer.MeasureText(_shareStatus.Text, _shareStatus.Font, new Size(innerW, int.MaxValue), TextFormatFlags.WordBreak).Height);
					_shareStatus.SetBounds(leftX, y, innerW, num13);
					y += num13;
				}
				y += SectionGap;
			}
			_eyeCaption.SetBounds(leftX, y, innerW, Tokens.Scale(20));
			y += Tokens.Scale(22);
			_eyeHint.SetBounds(leftX, y, innerW, Tokens.Scale(18));
			y += Tokens.Scale(22);
			_eyeWarmth.Place(leftX, y, innerW);
			y += SliderRow.RowHeight + _eyeWarmth.AvailabilityHintHeight + Tokens.Scale(4);
			_eyeDim.Place(leftX, y, innerW);
			y += SliderRow.RowHeight + _eyeDim.AvailabilityHintHeight;
			return y;
		}
		finally
		{
			_card.ResumeLayout(performLayout: false);
		}
	}

	private void LayoutContentCore()
	{
		TakeTheRebuiltRamp();
		int num = ContentRight - GlowPage.ContentLeft;
		int num2 = Math.Clamp(num, Tokens.Scale(420), Tokens.Scale(980));
		int num3 = GlowPage.ContentLeft + Math.Max(0, (num - num2) / 2);
		int num4 = num2 - 2 * CardPad;
		int num5 = (num4 - ColGap) / 2;
		int cardPad = CardPad;
		int rightX = CardPad + num5 + ColGap;
		int y = AutoScrollPosition.Y;
		int cardGap = CardGap;
		int height = Tokens.Scale(32);
		int num6 = Fonts.LineHeight(FontRole.Body);
		int num7 = Tokens.Scale(16);
		_phone.Size = new Size(_phone.GetPreferredSize(Size.Empty).Width, height);
		_resetFine.Size = new Size(_resetFine.GetPreferredSize(Size.Empty).Width, height);
		_compare.Size = new Size(_compare.GetPreferredSize(Size.Empty).Width, height);
		_liveWord.Size = new Size(TextRenderer.MeasureText(_liveWord.Text, _liveWord.Font).Width, num6);
		_liveTick.Size = new Size(num7, num7);
		FitTheLiveCue(num6);
		int num8 = ContentTop + y;
		int num9 = _monitorCard.Layout(num2);
		_monitorCard.View.SetBounds(num3, num8, num2, num9);
		if (num9 > 0)
		{
			num8 += num9 + cardGap;
		}
		_primaryLabel.SetBounds(num3, num8, TextRenderer.MeasureText(_primaryLabel.Text, _primaryLabel.Font).Width + Tokens.Scale(4), SectionLabelH);
		int num10 = LayoutHeadlineCard(num2);
		_headlineCard.SetBounds(num3, num8, num2, num10);
		int num11;
		int num12;
		if (_locked)
		{
			if (_fineCard.Visible)
			{
				_fineCard.Visible = false;
			}
			if (_presetsCard.Visible)
			{
				_presetsCard.Visible = false;
			}
			LayoutScenesCard(0);
			ShowDepthCard(show: false);
			num11 = num8 + num10 + cardGap;
			num12 = LayoutTrailingContent(CardPad, cardPad, rightX, num5, num4, includeShare: false);
		}
		else
		{
			if (!_fineCard.Visible)
			{
				_fineCard.Visible = true;
			}
			if (!_presetsCard.Visible)
			{
				_presetsCard.Visible = true;
			}
			int num13 = num8 + num10 + cardGap;
			int num14 = Tokens.Scale(932);
			int val = PresetsCardW + Math.Max(0, num2 - num14) * 332 / 932;
			int val2 = Math.Max(Tokens.Scale(220), (num2 - cardGap) / 2);
			int num15 = Math.Min(val, val2);
			int num16 = num2 - cardGap - num15;
			int val3 = LayoutFineCard(num16);
			int val4 = LayoutPresetsCard(num15);
			int num17 = Math.Max(val3, val4);
			int x = num3 + num16 + cardGap;
			_fineCard.SetBounds(num3, num13, num16, num17);
			_presetsCard.SetBounds(x, num13, num15, num17);
			int num18 = num13 + num17;
			int num19 = LayoutScenesCard(num2);
			if (num19 > 0)
			{
				_scenesCard.SetBounds(num3, num18 + cardGap, num2, num19);
				num18 = _scenesCard.Bottom;
			}
			else
			{
				_scenesCard.SetBounds(num3, num18 + cardGap, num2, 0);
			}
			ShowDepthCard(show: true);
			int height2 = LayoutDepthCard(num2);
			_depthCard.SetBounds(num3, num18 + cardGap, num2, height2);
			num11 = _depthCard.Bottom + cardGap;
			num12 = LayoutTrailingContent(CardPad, cardPad, rightX, num5, num4, includeShare: true);
		}
		_card.SetBounds(num3, num11, num2, num12 + CardPad);
		Size size = new Size(0, num11 - y + _card.Height + PagePadBottom);
		if (AutoScrollMinSize != size)
		{
			AutoScrollMinSize = size;
		}
		OrderTabsByReading(this);
		OrderTabsByReading(_compare.Parent);
		Control[] array = new Control[6] { _headlineCard, _fineCard, _presetsCard, _scenesCard, _depthCard, _card };
		for (int i = 0; i < array.Length; i++)
		{
			OrderTabsByReading(array[i]);
		}
	}

	private static int ButtonWidth(GlassButton button, int fromThePicture)
	{
		return Math.Max(fromThePicture, TextRenderer.MeasureText(button.Text, Fonts.BodyBold).Width + Tokens.Scale(24));
	}

	private void FitTheLiveCue(int cueLine)
	{
		if (_monitorLine == null)
		{
			_monitorLine = _liveDetail.Text;
		}
		string monitorLine = _monitorLine;
		string text = (monitorLine.StartsWith("Display 1 · ", StringComparison.Ordinal) ? monitorLine.Substring("Display 1 · ".Length) : monitorLine);
		if (!_locked)
		{
			string[] array = new string[3] { monitorLine, text, "" };
			foreach (string text2 in array)
			{
				Show(text2);
				if (HeaderPanelHeightPx <= Tokens.Scale(84))
				{
					return;
				}
			}
		}
		Show(monitorLine);
		void Show(string text3)
		{
			_liveDetail.Text = text3;
			_liveDetail.Visible = text3.Length > 0;
			_liveDetail.Size = new Size((text3.Length > 0) ? TextRenderer.MeasureText(text3, _liveDetail.Font).Width : 0, cueLine);
			LayoutHeaderPanel();
		}
	}

	private void ApplyPreset(DisplayPreset preset)
	{
		if (Timing.Enabled)
		{
			Timing.Measure("ApplyPreset(" + preset.Name + ")", () =>
			{
				ApplyPresetCore(preset);
			});
		}
		else
		{
			ApplyPresetCore(preset);
		}
	}

	private void ApplyPresetCore(DisplayPreset preset)
	{
		_applyingPreset = true;
		_engine.BeginDrag();
		try
		{
			_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Stronger;
			_saturation.Slider.Value = Math.Clamp(preset.Saturation, _saturation.Slider.Minimum, _saturation.Slider.Maximum);
			_vibrance.Slider.Value = Math.Clamp(preset.Vibrance, _vibrance.Slider.Minimum, _vibrance.Slider.Maximum);
			_brightness.Slider.Value = preset.Brightness;
			_gamma.Slider.Value = preset.Gamma;
			_contrast.Slider.Value = preset.Contrast;
			_temperature.Slider.Value = preset.Temperature;
			_highlights.Slider.Value = Math.Clamp(preset.Highlights, -100, 100);
			_shadows.Slider.Value = Math.Clamp(preset.Shadows, -100, 100);
			_blacks.Slider.Value = Math.Clamp(preset.Blacks, -100, 100);
			_whites.Slider.Value = Math.Clamp(preset.Whites, -100, 100);
			_shadowTint.Slider.Value = Math.Clamp(preset.ShadowTint, -100, 100);
			_highlightTint.Slider.Value = Math.Clamp(preset.HighlightTint, -100, 100);
			_hue.Value = Math.Clamp(preset.Hue, -180, 180);
			_red.Value = Math.Clamp(preset.RedGain, 50, 150);
			_green.Value = Math.Clamp(preset.GreenGain, 50, 150);
			_blue.Value = Math.Clamp(preset.BlueGain, 50, 150);
			_engine.Tone = preset.GradeOf(_engine.Tone);
			_engine.Mix = preset.MixOrIdentity;
			_settings.Mix = (_engine.Mix.IsIdentity ? null : _engine.Mix.ToArray());
			_engine.Depth = preset.Depth().Clamped();
			_settings.Depth = _engine.Depth;
		}
		finally
		{
			_applyingPreset = false;
			_engine.EndDrag();
		}
		SyncDepthControls();
		MarkDepthInTheLook();
		ColourReachFacts facts = LookFacts();
		bool paidLookAllowed = PaidLookAllowed;
		LookOutcome lookOutcome = LookRequirement.Decide(preset.ColourVision, facts.OverlayLive, paidLookAllowed);
		switch (lookOutcome)
		{
		case LookOutcome.Ready:
			BringLookCorrection(preset.ColourVision);
			SetLookNote("This look shows in Borderless or Windowed mode. In exclusive fullscreen the game only gets the driver's colour.", ok: true);
			break;
		case LookOutcome.Refused:
			ReturnTheirCorrection();
			SetLookNote(LookRequirement.WhyRefused(preset.ColourVision, facts, paidLookAllowed), ok: false);
			break;
		default:
			ReturnTheirCorrection();
			SetLookNote("", ok: true);
			break;
		}
		_settings.ColourPresetGame = _gamePicker.SelectedItem;
		bool flag = lookOutcome == LookOutcome.Refused;
		if (!flag)
		{
			_settings.ColourPresetName = preset.Name;
		}
		RefreshReadouts();
		UpdateActiveChip();
		_store.Save(_settings);
		if (!flag)
		{
			PresetApplied?.Invoke(_gamePicker.SelectedItem + " · " + preset.Name);
		}
	}

	private PresetKeyBinding? BindingFor(string name)
	{
		return _settings.PresetKeyBindings.Find((PresetKeyBinding presetKeyBinding) => string.Equals(presetKeyBinding.Name, name, StringComparison.OrdinalIgnoreCase));
	}

	internal string? SetPresetBinding(string name, uint mask, uint vk)
	{
		string text = HotkeyConflicts.BindingOwner(_settings, name, mask, vk);
		if (text != null)
		{
			return text;
		}
		PresetKeyBinding presetKeyBinding = BindingFor(name);
		if (presetKeyBinding != null)
		{
			presetKeyBinding.ModifierMask = mask;
			presetKeyBinding.VirtualKey = vk;
		}
		else
		{
			_settings.PresetKeyBindings.Add(new PresetKeyBinding
			{
				Name = name,
				ModifierMask = mask,
				VirtualKey = vk
			});
		}
		_store.Save(_settings);
		PresetBindingsChanged?.Invoke();
		RebuildSceneChips();
		return null;
	}

	internal void RemovePresetBinding(string name)
	{
		if (_settings.PresetKeyBindings.RemoveAll((PresetKeyBinding presetKeyBinding) => string.Equals(presetKeyBinding.Name, name, StringComparison.OrdinalIgnoreCase)) != 0)
		{
			_store.Save(_settings);
			PresetBindingsChanged?.Invoke();
			RebuildSceneChips();
		}
	}

	internal LookNote ApplyLibraryPreset(DisplayPreset preset)
	{
		ApplyPreset(preset);
		return _lastLookNote;
	}

	internal void ApplyStartingLook(DisplayPreset preset)
	{
		ApplyPreset(preset);
	}

	internal void TestCopyWholeSetup()
	{
		CopyWholeSetup();
	}

	internal LookNote ApplyPresetByBindingName(string name)
	{
		DisplayPreset displayPreset = _presets.Find((DisplayPreset p) => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
		if (displayPreset == null)
		{
			return LookNote.None;
		}
		ApplyPreset(displayPreset);
		return _lastLookNote;
	}

	private void PromptAndBindPreset(DisplayPreset preset)
	{
		PresetKeyBinding presetKeyBinding = BindingFor(preset.Name);
		GlassShellForm form = new GlassShellForm("Hotkey for '" + preset.Name + "'")
		{
			ClientSize = new Size(Tokens.Scale(380), Tokens.Scale(168))
		};
		try
		{
			uint mask = presetKeyBinding?.ModifierMask ?? 3;
			uint vk = presetKeyBinding?.VirtualKey ?? 0;
			int num = Tokens.Scale(24);
			HotkeyPicker hotkeyPicker = new HotkeyPicker
			{
				ModifierMask = mask,
				VirtualKey = vk,
				Location = new Point(num, Tokens.Scale(58)),
				Size = new Size(form.ClientSize.Width - num * 2, Tokens.Scale(40))
			};
			hotkeyPicker.HotkeyChanged += (uint m, uint k) =>
			{
				mask = m;
				vk = k;
			};
			form.Controls.Add(hotkeyPicker);
			int num2 = Tokens.Scale(96);
			int num3 = Tokens.Scale(32);
			int y = form.ClientSize.Height - num3 - Tokens.Scale(20);
			GlassButton glassButton = new GlassButton
			{
				Text = "Save",
				Kind = GlassButtonKind.Primary,
				Size = new Size(num2, num3),
				Location = new Point(form.ClientSize.Width - num2 - num, y)
			};
			glassButton.Click += (object? _, EventArgs _) =>
			{
				Finish(DialogResult.OK);
			};
			form.Controls.Add(glassButton);
			GlassButton glassButton2 = new GlassButton
			{
				Text = "Cancel",
				Kind = GlassButtonKind.Secondary,
				Size = new Size(num2, num3),
				Location = new Point(glassButton.Left - num2 - Tokens.Scale(8), y)
			};
			glassButton2.Click += (object? _, EventArgs _) =>
			{
				Finish(DialogResult.Cancel);
			};
			form.Controls.Add(glassButton2);
			if (presetKeyBinding != null)
			{
				GlassButton glassButton3 = new GlassButton
				{
					Text = "Remove",
					Kind = GlassButtonKind.Secondary,
					Size = new Size(num2, num3),
					Location = new Point(num, y)
				};
				glassButton3.Click += (object? _, EventArgs _) =>
				{
					Finish(DialogResult.No);
				};
				form.Controls.Add(glassButton3);
			}
			switch (form.ShowDialog(FindForm()))
			{
			case DialogResult.No:
				RemovePresetBinding(preset.Name);
				break;
			case DialogResult.OK:
				if (vk != 0)
				{
					string text = SetPresetBinding(preset.Name, mask, vk);
					if (text != null)
					{
						GlassDialog.Show(FindForm(), "That combination is taken", HotkeyPicker.GetDisplay(mask, vk) + " already belongs to " + text + ". Pick a different one.", GlassDialogButtons.Ok, GlassDialogTone.Warning);
					}
				}
				break;
			}
		}
		finally
		{
			if (form != null)
			{
				((IDisposable)form).Dispose();
			}
		}
		void Finish(DialogResult r)
		{
			form.DialogResult = r;
			form.Close();
		}
	}

	private void ResetFineTune()
	{
		Refresh();
		_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Stronger;
		_applyingPreset = true;
		try
		{
			_vibrance.Slider.Value = 100;
			_eyeWarmth.Slider.Value = 0;
			_eyeDim.Slider.Value = 0;
			_saturation.Slider.Value = 100;
			_hue.Value = 0;
			_red.Value = 100;
			_green.Value = 100;
			_blue.Value = 100;
			_brightness.Slider.Value = 100;
			_gamma.Slider.Value = 100;
			_contrast.Slider.Value = 100;
			_temperature.Slider.Value = 0;
			_highlights.Slider.Value = 0;
			_shadows.Slider.Value = 0;
			_blacks.Slider.Value = 0;
			_whites.Slider.Value = 0;
			_shadowTint.Slider.Value = 0;
			_highlightTint.Slider.Value = 0;
			_engine.Mix = ChannelMix.Identity;
			_settings.Mix = null;
			_engine.Depth = ColourDepth.Neutral;
			_settings.Depth = _engine.Depth;
			LookCorrection.ChosenByThem(_settings, ColourVisionMode.Off);
			WriteColourVision(ColourVisionMode.Off);
		}
		finally
		{
			_applyingPreset = false;
		}
		SyncDepthControls();
		_depthInUse.Visible = false;
		SetLookNote("", ok: true);
		RefreshReadouts();
		UpdateActiveChip();
		_store.Save(_settings);
	}

	private ProfileCode CurrentLook()
	{
		return new ProfileCode(_engine.Vibrance, _engine.Saturation, _engine.Brightness, _engine.Gamma, _engine.Contrast, _engine.Temperature, _engine.Tone.Highlights, _engine.Tone.Shadows, _engine.ColourVision, _engine.Hue, _engine.RedGain, _engine.GreenGain, _engine.BlueGain).WithDepth(_engine.Depth);
	}

	private void CopyMyCode()
	{
		if (ServedCodes.Current.Withheld)
		{
			SetShareStatus("Share codes aren't working on this PC right now. Nothing was copied, changed or saved.", ok: false);
			return;
		}
		string text = ProfileCode.Encode(CurrentLook());
		_codeBox.Text = text;
		if (TheClipboard.Put(text))
		{
			SetShareStatus(text + "  —  copied, paste it anywhere", ok: true);
		}
		else
		{
			SetShareStatus("Couldn't reach the clipboard. Copy the code from the box above.", ok: false);
		}
	}

	private void CopyWholeSetup()
	{
		if (ServedCodes.Current.Withheld)
		{
			SetShareStatus("Share codes aren't working on this PC right now. Nothing was copied, changed or saved.", ok: false);
			return;
		}
		CrosshairConfig crosshair = CrosshairForSharing?.Invoke();
		MonitorCode? mode = ((ModeForSharing != null) ? ModeForSharing() : CurrentScreenMode());
		string text = ConfigCode.Encode(CurrentLook(), crosshair, mode);
		_codeBox.Text = text;
		string text2 = SetupSummary(crosshair, mode);
		if (TheClipboard.Put(text))
		{
			SetShareStatus("Copied — " + text2 + ".", ok: true);
		}
		else
		{
			SetShareStatus(text2 + " — copy the code above, the clipboard is busy.", ok: false);
		}
	}

	internal static string SetupSummary(CrosshairConfig? crosshair, MonitorCode? mode)
	{
		List<string> list = new List<string> { "colour" };
		if (crosshair != null)
		{
			list.Add("crosshair");
		}
		if (mode.HasValue)
		{
			MonitorCode valueOrDefault = mode.GetValueOrDefault();
			list.Add($"{valueOrDefault.Width} × {valueOrDefault.Height}");
		}
		return string.Join(", ", list);
	}

	private static MonitorCode? CurrentScreenMode()
	{
		try
		{
			DisplayMode? displayMode = DisplayController.Current();
			if (displayMode.HasValue)
			{
				DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
				ScalingMode scaling = ScalingModeController.Current() ?? ScalingMode.FullScreen;
				return new MonitorCode(valueOrDefault.Width, valueOrDefault.Height, valueOrDefault.RefreshHz, scaling);
			}
			return null;
		}
		catch
		{
			return null;
		}
	}

	private void ApplyCode()
	{
		if (ServedCodes.Current.Withheld)
		{
			SetShareStatus("Share codes aren't working on this PC right now. Nothing was copied, changed or saved.", ok: false);
			return;
		}
		if (!ProfileCode.TryDecode(_codeBox.Text, out var profile))
		{
			SetShareStatus("That code isn't valid. Nothing changed.", ok: false);
			return;
		}
		_applyingPreset = true;
		try
		{
			_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Stronger;
			_saturation.Slider.Value = profile.Saturation;
			_vibrance.Slider.Value = profile.Vibrance;
			_brightness.Slider.Value = profile.Brightness;
			_gamma.Slider.Value = profile.Gamma;
			_contrast.Slider.Value = profile.Contrast;
			_temperature.Slider.Value = profile.Temperature;
			_highlights.Slider.Value = profile.Highlights;
			_shadows.Slider.Value = profile.Shadows;
			_blacks.Slider.Value = 0;
			_whites.Slider.Value = 0;
			_shadowTint.Slider.Value = 0;
			_highlightTint.Slider.Value = 0;
			_engine.Mix = ChannelMix.Identity;
			_settings.Mix = null;
			_hue.Value = Math.Clamp(profile.Hue, -180, 180);
			_red.Value = Math.Clamp(profile.RedGain, 50, 150);
			_green.Value = Math.Clamp(profile.GreenGain, 50, 150);
			_blue.Value = Math.Clamp(profile.BlueGain, 50, 150);
			_engine.Depth = profile.Depth().Clamped();
			_settings.Depth = _engine.Depth;
		}
		finally
		{
			_applyingPreset = false;
		}
		SyncDepthControls();
		MarkDepthInTheLook();
		string text = "Applied — that's their exact look.";
		bool flag = false;
		ColourReachFacts facts = LookFacts();
		bool paidLookAllowed = PaidLookAllowed;
		if (LookCorrection.IsALook(profile.ColourVision))
		{
			SetLookNote("", ok: true);
			if (LookRequirement.Decide(profile.ColourVision, facts.OverlayLive, paidLookAllowed) == LookOutcome.Ready)
			{
				BringLookCorrection(profile.ColourVision);
				text += " This look shows in Borderless or Windowed mode. In exclusive fullscreen the game only gets the driver's colour.";
			}
			else
			{
				ReturnTheirCorrection();
				text = LookRequirement.WhyRefused(profile.ColourVision, facts, paidLookAllowed);
				flag = true;
			}
		}
		else if (profile.ColourVision == ColourVisionMode.Off)
		{
			SetLookNote("", ok: true);
			ReturnTheirCorrection();
		}
		else
		{
			ApplyColourVision(profile.ColourVision);
			text = text + " " + (facts.OverlayLive ? "This look shows in Borderless or Windowed mode. In exclusive fullscreen the game only gets the driver's colour." : ColourReachStatus.For(facts).Text);
		}
		RefreshReadouts();
		UpdateActiveChip();
		_store.Save(_settings);
		SetShareStatus(text, !flag, flag);
	}

	internal void ShowColourReach(ColourReachFacts facts)
	{
		ColourReachLine colourReachLine = ColourReachStatus.For(facts);
		Color color = ((colourReachLine.Severity == ColourReachSeverity.Refused) ? Theme.Negative : Theme.TextDim);
		bool flag = colourReachLine.Text.Length > 0;
		if (!(_colourReach.Text == colourReachLine.Text) || !(_colourReach.ForeColor == color) || _colourReach.Visible != flag)
		{
			_colourReach.ForeColor = color;
			_colourReach.Text = colourReachLine.Text;
			_colourReach.Visible = flag;
			if (_built)
			{
				LayoutContent();
			}
		}
	}

	internal void RefreshColourReach()
	{
		Func<ColourReachFacts> colourReachSource = ColourReachSource;
		if (colourReachSource == null)
		{
			return;
		}
		ColourReachFacts facts;
		try
		{
			facts = colourReachSource();
		}
		catch
		{
			facts = new ColourReachFacts(ExclusiveFullscreen: false, OverlayLive: true, DxInitFailureKind.None, DxTried: false);
		}
		try
		{
			ShowColourReach(facts);
		}
		catch
		{
		}
	}

	private ColourReachFacts LookFacts()
	{
		ColourReachFacts result = new ColourReachFacts(ExclusiveFullscreen: false, OverlayLive: true, DxInitFailureKind.None, DxTried: false);
		Func<ColourReachFacts> colourReachSource = ColourReachSource;
		if (colourReachSource == null)
		{
			return result;
		}
		try
		{
			return colourReachSource();
		}
		catch
		{
			return result;
		}
	}

	private void SetLookNote(string text, bool ok)
	{
		_lastLookNote = new LookNote(text, !ok && text.Length > 0);
		Color color = (ok ? Theme.TextDim : Theme.Negative);
		bool flag = text.Length > 0 && !_locked;
		if (_lookNote.Text != text || _lookNote.ForeColor != color || _lookNote.Visible != flag)
		{
			_lookNote.ForeColor = color;
			_lookNote.Text = text;
			_lookNote.Visible = flag;
			if (_built)
			{
				LayoutContent();
			}
		}
		if (flag && _built)
		{
			try
			{
				RevealLookNote();
			}
			catch
			{
			}
		}
	}

	private void RevealLookNote()
	{
		if (!_lookNote.Visible || !Visible || !IsHandleCreated || _chips.Count == 0 || _lookNote.Parent == null)
		{
			return;
		}
		int height = ClientSize.Height;
		if (_chips.Max((SceneTile sceneTile) => TopInPage(sceneTile) + sceneTile.Height) > 0 && ((IEnumerable<SceneTile>)_chips).Min((Func<SceneTile, int>)TopInPage) < height)
		{
			int num = TopInPage(_lookNote) + _lookNote.Height + Tokens.Scale(8) - height;
			if (num > 0)
			{
				ScrollTo(ScrollOffset + num);
				PerformLayout();
			}
		}
		int TopInPage(Control control)
		{
			return PointToClient(control.PointToScreen(Point.Empty)).Y;
		}
	}

	internal static ColourReachFacts ColourReachFactsFrom(bool colourReachesScreen, ISaturationOverlay? overlay, AppSettings? settings)
	{
		return ColourReachFactsFromEffect(colourReachesScreen, (overlay as IDisplayOverlay)?.EffectIsLive, settings);
	}

	internal static ColourReachFacts ColourReachFactsFromEffect(bool colourReachesScreen, bool? effectIsLive, AppSettings? settings)
	{
		DxInitFailureKind dxInitFailureKind = settings?.DxFailure ?? DxInitFailureKind.None;
		bool dxTried = settings != null && (dxInitFailureKind != DxInitFailureKind.None || (settings.OverlayMode == OverlayMode.Dx && effectIsLive == true));
		return new ColourReachFacts(!colourReachesScreen, effectIsLive ?? true, dxInitFailureKind, dxTried, RuntimeFacts.DisplayIsCarryingTheLook, RuntimeFacts.WhatTheDisplayCannotHold, RuntimeFacts.RampDelivery);
	}

	private void SetShareStatus(string text, bool ok, bool refusal = false)
	{
		Label shareStatus = _shareStatus;
		Color foreColor;
		if (refusal)
		{
			foreColor = Theme.Negative;
		}
		else
		{
			foreColor = (ok ? Theme.TextDim : Theme.Accent);
		}
		shareStatus.ForeColor = foreColor;
		if (!(_shareStatus.Text == text))
		{
			_shareStatus.Text = text;
			if (_built)
			{
				LayoutContent();
			}
		}
	}

	private void UpdateActiveChip()
	{
		ToneSettings tone = _engine.Tone;
		int num = -1;
		for (int i = 0; i < _chips.Count; i++)
		{
			if (_presets[i].MatchesWithGrade(_saturation.Slider.Value, _vibrance.Slider.Value, _brightness.Slider.Value, _gamma.Slider.Value, _contrast.Slider.Value, _temperature.Slider.Value, _highlights.Slider.Value, _shadows.Slider.Value, tone.NightVision, _blacks.Slider.Value, _whites.Slider.Value, _shadowTint.Slider.Value, _highlightTint.Slider.Value, _engine.Mix, _hue.Value, _red.Value, _green.Value, _blue.Value, _engine.Depth) && CorrectionIsOnScreen(_presets[i]))
			{
				if (num == -1)
				{
					num = i;
				}
				if (_presets[i].Name == _settings.ColourPresetName)
				{
					num = i;
					break;
				}
			}
		}
		for (int j = 0; j < _chips.Count; j++)
		{
			_chips[j].Active = j == num;
		}
	}

	private bool CorrectionIsOnScreen(DisplayPreset preset)
	{
		if (!LookRequirement.NeedsColourMatrix(preset.ColourVision))
		{
			return !_settings.ColourVisionFromPreset;
		}
		if (_settings.ColourVisionFromPreset)
		{
			return _engine.ColourVision == preset.ColourVision;
		}
		return false;
	}

	private void RefreshReadouts()
	{
		SliderRow[] array = new SliderRow[6] { _saturation, _vibrance, _brightness, _gamma, _contrast, _temperature };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SyncValueText();
		}
		SyncMatrixRows();
	}

	internal static string HueText(int value)
	{
		return ((value > 0) ? "+" : "") + value.ToString(CultureInfo.InvariantCulture) + "°";
	}

	internal static string TemperatureText(int value)
	{
		if (value >= 0)
		{
			if (value == 0)
			{
				return "0";
			}
			return "+" + value.ToString(CultureInfo.InvariantCulture);
		}
		return "−" + (-value).ToString(CultureInfo.InvariantCulture);
	}

	public new void Refresh()
	{
		_syncingFromEngine = true;
		try
		{
			_saturation.Slider.Value = _engine.Saturation;
			_vibrance.Slider.Value = _engine.Vibrance;
			_brightness.Slider.Value = _engine.Brightness;
			_gamma.Slider.Value = _engine.Gamma;
			_contrast.Slider.Value = _engine.Contrast;
			_temperature.Slider.Value = _engine.Temperature;
			_hue.Value = _engine.Hue;
			_red.Value = _engine.RedGain;
			_green.Value = _engine.GreenGain;
			_blue.Value = _engine.BlueGain;
			_eyeWarmth.Slider.Value = _engine.EyeCareWarmthPercent;
			_eyeDim.Slider.Value = _engine.EyeCareDim;
			SyncAdvancedRows();
		}
		finally
		{
			_syncingFromEngine = false;
		}
		SyncDepthControls();
		ShowCorrectionOnTheChips(_engine.ColourVision);
		if (!_settings.ColourVisionFromPreset && !_lastLookNote.Refused && _lookNote.Text.Length > 0)
		{
			SetLookNote("", ok: true);
		}
		_hotkeyPicker.ModifierMask = _settings.HotkeyModifierMask;
		_hotkeyPicker.VirtualKey = _settings.HotkeyVirtualKey;
		_mainHotkeyPicker.ModifierMask = _settings.MainHotkeyModifierMask;
		_mainHotkeyPicker.VirtualKey = (_settings.MainHotkeyEnabled ? _settings.MainHotkeyVirtualKey : 0u);
		_screenshotHotkeyPicker.ModifierMask = _settings.ScreenshotHotkeyModifierMask;
		_screenshotHotkeyPicker.VirtualKey = (_settings.ScreenshotHotkeyEnabled ? _settings.ScreenshotHotkeyVirtualKey : 0u);
		RefreshReadouts();
		UpdateActiveChip();
		Invalidate();
	}

	private void SyncMonitorCard()
	{
		if (_monitorSelection.WorthShowing)
		{
			if (_monitorCard.View.Parent == null)
			{
				Controls.Add(_monitorCard.View);
			}
		}
		else
		{
			Controls.Remove(_monitorCard.View);
		}
		SyncMatrixRows();
		LayoutContent();
	}

	private void SyncMatrixRows()
	{
		bool flag = _monitorSelection.Groups.Count > 0 && !_monitorSelection.AllowsMatrix;
		SliderRow[] array = new SliderRow[3] { _brightness, _contrast, _temperature };
		foreach (SliderRow obj in array)
		{
			obj.Slider.Enabled = true;
			obj.SetAvailabilityHint("");
		}
		array = new SliderRow[7] { _saturation, _turnRow, _darkenRow, _amountRow, _complementRow, _eyeWarmth, _eyeDim };
		foreach (SliderRow sliderRow in array)
		{
			bool flag2 = sliderRow != _saturation && sliderRow != _eyeWarmth && sliderRow != _eyeDim;
			sliderRow.Slider.Enabled = !flag;
			sliderRow.SetAvailabilityHint((flag && (!flag2 || !_locked)) ? "Needs every monitor ticked" : "");
		}
		_vibrance.Slider.Enabled = !flag || _engine.DriverAvailable;
		_vibrance.SetAvailabilityHint((flag && !_locked && (!_engine.DriverAvailable || _vibrance.Slider.Value > 100)) ? "Needs every monitor ticked" : "");
	}

	private void SyncVibranceMatrixHint(object? sender, EventArgs e)
	{
		int availabilityHintHeight = _vibrance.AvailabilityHintHeight;
		SyncMatrixRows();
		if (_built && availabilityHintHeight != _vibrance.AvailabilityHintHeight)
		{
			LayoutContent();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_saveDebounce.Dispose();
			_monitorCard.Dispose();
			_monitorSelection.Changed -= SyncMonitorCard;
			_vibrance.Slider.ValueChanged -= SyncVibranceMatrixHint;
			if (_monitorCard.View.Parent == null)
			{
				_monitorCard.View.Dispose();
			}
			_presetTips.Dispose();
		}
		base.Dispose(disposing);
	}
}
