using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Pages;
using VibranceHud.Theming;

namespace VibranceHud.Customize;

public sealed class CustomizePage : GlowPage
{
	private sealed class Rule : Control
	{
		public Rule()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint, value: true);
			BackColor = Color.Transparent;
			TabStop = false;
			Height = 1;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			using SolidBrush brush = new SolidBrush(ControlInk.Border);
			e.Graphics.FillRectangle(brush, ClientRectangle);
		}
	}

	private sealed class PictureThumb : Control
	{
		public PictureThumb()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			TabStop = false;
			AccessibleRole = AccessibleRole.Graphic;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			if (Width < 2 || Height < 2)
			{
				return;
			}
			float radius = Tokens.Scale(4);
			GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, Width, Height), radius);
			try
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				using (SolidBrush brush = new SolidBrush(SlateSurface.Active ? SignalTokens.Slate.PreviewWell : SignalTokens.Ash.PreviewWell))
				{
					g.FillPath(brush, path);
				}
				if (!AppBackground.IsSet)
				{
					return;
				}
				PaintGuard.Try(this, () =>
				{
					using Region clip = g.Clip;
					try
					{
						g.SetClip(path, CombineMode.Intersect);
						AppBackground.PaintInto(g, Width, Height);
					}
					finally
					{
						g.Clip = clip;
					}
				});
			}
			finally
			{
				if (path != null)
				{
					((IDisposable)path).Dispose();
				}
			}
		}
	}

	private sealed class NoteIcon : Control
	{
		public NoteIcon()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			TabStop = false;
			AccessibleRole = AccessibleRole.Graphic;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			SignalIcons.Draw(e.Graphics, SignalIcon.Info, new RectangleF(0f, 0f, Width, Height), ControlInk.Info);
		}
	}

	private const int NarrowColumn = 332;

	private const string NoneSegment = "None";

	private const string PictureSegment = "Image / GIF";

	private readonly Label _title;

	private readonly Label _subtitle;

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly Action<string> _onThemeChanged;

	private readonly Action _onBackgroundChanged;

	private readonly Action<int> _onOpacityChanged;

	private readonly CustomThemeService? _custom;

	private readonly List<LookTile> _lookTiles = new List<LookTile>();

	private readonly bool _slateLive;

	private readonly List<ChipButton> _backdropButtons = new List<ChipButton>();

	private readonly DebouncedAction _saveDebounce;

	private const int SaveDebounceMs = 500;

	internal const string PictureFilter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*";

	private readonly CardPanel _lookCard;

	private readonly Label _lookTitle;

	private readonly Label _modeCaption;

	private readonly Label _opacityRowTitle;

	private readonly Label _opacityRowNote;

	private readonly Label _opacityRowValue;

	private readonly FlatSlider _slateOpacity;

	private readonly Label _matteBlurTitle;

	private readonly Label _matteBlurNote;

	private readonly ToggleSwitch _matteBlurToggle;

	private readonly Rule _slateRule1;

	private readonly CardPanel _pictureCard;

	private readonly Label _pictureTitle;

	private readonly SegmentStrip _pictureSwitch;

	private readonly PictureThumb _thumb;

	private readonly Label _pictureName;

	private readonly Label _pictureDetail;

	private readonly Label _noPicture;

	private readonly Label _pictureError;

	private readonly GlassButton _chooseBtn;

	private readonly GlassButton _clearBtn;

	private readonly Rule _noteRule;

	private readonly NoteIcon _noteIcon;

	private readonly Label _gifNote;

	private readonly CardPanel _checkCard;

	private readonly Label _checkTitle;

	private readonly Label _checkRatio;

	private readonly Label _checkNote;

	private readonly StatusPill _checkStatus;

	private readonly CardPanel _keptCard;

	private readonly Label _backdropTitle;

	private readonly Label _backdropNote;

	private readonly Label? _dimTitle;

	private readonly Label? _dimValue;

	private readonly Label? _dimNote;

	private readonly FlatSlider? _dimSlider;

	private readonly Label _blurTitle;

	private readonly Label _blurValue;

	private readonly Label _blurNote;

	private readonly FlatSlider _blurSlider;

	private readonly Label _opacityTitle;

	private readonly Label _opacityValue;

	private readonly Label _opacityHint;

	private readonly FlatSlider _opacitySlider;

	private readonly Label _savedNote;

	private readonly Rule _keptRuleDim;

	private readonly Rule _keptRuleBlur;

	private readonly Rule _keptRuleMatte;

	private readonly Rule _keptRuleOpacity;

	private readonly Rule _keptRuleSaved;

	private readonly Label _matteLabel;

	private readonly ChipButton _blackChip;

	private readonly ChipButton _whiteChip;

	private readonly ChipButton _pickChip;

	private bool _built;

	protected override bool LaysOutInLogicalPixels => false;

	private static int Inset => S(21);

	private static int ListPadY => S(4);

	private static int CardGap => S(16);

	private static int ColumnGap => S(16);

	private static int RowMin => S(52);

	private static Size ThumbSize => new Size(S(112), S(64));

	private static int RowSliderW => S(200);

	private bool APictureIsSet => _custom?.HasImage ?? AppBackground.IsSet;

	private static int TitleLine => S(20);

	private static int CaptionLine => S(16);

	private static int S(int px)
	{
		return Tokens.Scale(px);
	}

	public CustomizePage(AppSettings settings, SettingsStore store, Action<string>? onThemeChanged = null, Action? onBackgroundChanged = null, Action<int>? onOpacityChanged = null, CustomThemeService? custom = null, Action<BackdropLook>? onBackdropChanged = null)
	{
		CustomizePage customizePage = this;
		_settings = settings;
		_store = store;
		_onThemeChanged = onThemeChanged ?? ((Action<string>)((string name) =>
		{
			Theme.Apply(name);
		}));
		_onBackgroundChanged = onBackgroundChanged ?? ((Action)(() =>
		{
		}));
		_onOpacityChanged = onOpacityChanged ?? ((Action<int>)((int _) =>
		{
		}));
		_custom = custom;
		_saveDebounce = new DebouncedAction(() =>
		{
			customizePage._store.Save(customizePage._settings);
		}, 500);
		_slateLive = Theme.IsSeeThrough;
		Dock = DockStyle.Fill;
		BackColor = Theme.Background;
		AutoScroll = true;
		Font = Fonts.Body;
		_title = PageLabel("Customize", Fonts.Display, Theme.Text);
		_subtitle = PageLabel("How PlexusX itself looks. The colour of your games is set on Display.", Fonts.Label, Theme.TextDim);
		_subtitle.TextAlign = ContentAlignment.TopLeft;
		_lookCard = NewCard();
		_lookTitle = Heading(_lookCard, "Colour mode");
		_modeCaption = Words(_lookCard, _slateLive ? "Slate (see-through)" : "Default", Fonts.Caption, Theme.TextFaint, ContentAlignment.MiddleRight);
		(string, string, string)[] array = new (string, string, string)[2]
		{
			("Ash", "Default", "Solid panels. Your picture shows around them."),
			("Slate", "Slate (see-through)", "Dark matte panels. Your picture shows through.")
		};
		for (int num = 0; num < array.Length; num++)
		{
			(string, string, string) tuple = array[num];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string item3 = tuple.Item3;
			ThemePalette themePalette = ThemeCatalog.ByName(item);
			LookTile tile = new LookTile(themePalette, item3, item2)
			{
				Active = (themePalette.Name == Theme.CurrentName)
			};
			tile.Click += (object? s, EventArgs e) =>
			{
				foreach (LookTile lookTile in customizePage._lookTiles)
				{
					lookTile.Active = lookTile == tile;
				}
				if (SignatureLooks.TryGet(tile.Palette.Name, out var look))
				{
					customizePage._settings.BackdropLookName = BackdropLooks.Name(look.Backdrop);
					customizePage._settings.SurfaceStyleName = SurfaceStyles.Name(look.Surface);
					customizePage._settings.ChromeName = Chromes.Name(look.Chrome);
					customizePage._store.Save(customizePage._settings);
					foreach (ChipButton backdropButton in customizePage._backdropButtons)
					{
						backdropButton.Active = backdropButton.Tag is BackdropLook backdropLook2 && backdropLook2 == look.Backdrop;
					}
				}
				customizePage._onThemeChanged(tile.Palette.Name);
			};
			_lookTiles.Add(tile);
			_lookCard.Controls.Add(tile);
		}
		Color colour = (_slateLive ? Theme.Text : ControlInk.TextDisabledInk);
		Color colour2 = (_slateLive ? Theme.TextFaint : ControlInk.TextDisabledInk);
		_opacityRowTitle = Words(_lookCard, "GUI opacity", Fonts.Body, colour);
		_opacityRowNote = Note(_lookCard, _slateLive ? "How solid the Slate panels are. The picture itself never dims." : "Slate only.", colour2);
		int value = (int)Math.Round(SlateSurface.UserOpacity * 100.0);
		_opacityRowValue = Words(_lookCard, $"{value}%", Fonts.BodyBold, colour, ContentAlignment.MiddleRight);
		_slateOpacity = new FlatSlider
		{
			AccessibleName = "GUI opacity",
			Minimum = (int)Math.Round(45.0),
			Maximum = (int)Math.Round(85.0),
			Value = value,
			Enabled = _slateLive
		};
		_slateOpacity.ValueChanged += (object? s, EventArgs e) =>
		{
			if (customizePage._slateLive)
			{
				SlateSurface.UserOpacity = (double)customizePage._slateOpacity.Value / 100.0;
				customizePage._opacityRowValue.Text = $"{customizePage._slateOpacity.Value}%";
				customizePage._settings.SlateGuiOpacityPercent = customizePage._slateOpacity.Value;
				customizePage._saveDebounce.Trigger();
				customizePage._onBackgroundChanged();
				customizePage.RefreshTextCheck();
			}
		};
		_lookCard.Controls.Add(_slateOpacity);
		_slateRule1 = NewRule(_lookCard);
		_matteBlurTitle = Words(_lookCard, "Matte blur", Fonts.Body, colour);
		_matteBlurNote = Note(_lookCard, _slateLive ? "Softens the picture behind the panels only." : "Slate only.", colour2);
		_matteBlurToggle = new ToggleSwitch
		{
			AccessibleName = "Matte blur",
			Checked = (_slateLive && SlateSurface.MatteBlur),
			Enabled = _slateLive
		};
		_matteBlurToggle.CheckedChanged += (object? s, EventArgs e) =>
		{
			if (customizePage._slateLive)
			{
				SlateSurface.MatteBlur = customizePage._matteBlurToggle.Checked;
				customizePage._settings.SlateMatteBlur = customizePage._matteBlurToggle.Checked;
				customizePage._store.Save(customizePage._settings);
				customizePage._onBackgroundChanged();
				customizePage.RefreshTextCheck();
			}
		};
		_lookCard.Controls.Add(_matteBlurToggle);
		_pictureCard = NewCard();
		_pictureTitle = Heading(_pictureCard, "Background");
		_pictureSwitch = new SegmentStrip(new string[2] { "None", "Image / GIF" })
		{
			AccessibleName = "Backdrop"
		};
		_pictureSwitch.Chosen += (object? _, string segment) =>
		{
			if (segment == "None")
			{
				customizePage.RemovePicture();
			}
			else
			{
				customizePage.ChoosePicture();
			}
		};
		_pictureCard.Controls.Add(_pictureSwitch);
		_thumb = new PictureThumb();
		_pictureCard.Controls.Add(_thumb);
		_pictureName = Words(_pictureCard, "", Fonts.BodyBold, Theme.Text);
		_pictureName.AutoEllipsis = true;
		_pictureDetail = Words(_pictureCard, "", Fonts.Caption, Theme.TextFaint);
		_noPicture = Words(_pictureCard, "No picture. The window is plain.", Fonts.Body, Theme.TextDim);
		_pictureError = Note(_pictureCard, "", Theme.TextDim);
		_chooseBtn = new GlassButton
		{
			Text = "Choose image or GIF",
			Kind = GlassButtonKind.Secondary,
			Icon = SignalIcon.Upload
		};
		_clearBtn = new GlassButton
		{
			Text = "Remove",
			Kind = GlassButtonKind.Quiet
		};
		_chooseBtn.Click += (object? s, EventArgs e) =>
		{
			customizePage.ChoosePicture();
		};
		_clearBtn.Click += (object? s, EventArgs e) =>
		{
			customizePage.RemovePicture();
		};
		_pictureCard.Controls.Add(_chooseBtn);
		_pictureCard.Controls.Add(_clearBtn);
		_noteRule = NewRule(_pictureCard);
		_noteIcon = new NoteIcon();
		_pictureCard.Controls.Add(_noteIcon);
		_gifNote = Note(_pictureCard, "A GIF plays only while PlexusX is open and in front. It stops while a game runs. Only you see it; nothing is uploaded.", Theme.TextFaint);
		_checkCard = NewCard();
		_checkTitle = Heading(_checkCard, "Text check");
		_checkStatus = new StatusPill();
		_checkCard.Controls.Add(_checkStatus);
		_checkRatio = Words(_checkCard, "", Fonts.Body, Theme.TextDim);
		_checkNote = Note(_checkCard, _slateLive ? "The dimmest text against the brightest part of your picture behind the panels. 4.5 : 1 is the bar." : "Solid panels: your picture never sits behind text.", Theme.TextFaint);
		_keptCard = NewCard();
		_backdropTitle = Words(_keptCard, "Backdrop", Fonts.Body, Theme.Text);
		_backdropNote = Note(_keptCard, "What is drawn behind the app.", Theme.TextFaint);
		BackdropLook backdropLook = BackdropLooks.Parse(settings.BackdropLookName);
		(BackdropLook, string)[] array2 = new (BackdropLook, string)[3]
		{
			(BackdropLook.Blossom, "Petals"),
			(BackdropLook.Soft, "Soft"),
			(BackdropLook.Off, "Off")
		};
		for (int num = 0; num < array2.Length; num++)
		{
			(BackdropLook, string) tuple2 = array2[num];
			BackdropLook item4 = tuple2.Item1;
			string item5 = tuple2.Item2;
			BackdropLook chosen = item4;
			ChipButton btn = new ChipButton
			{
				Text = item5,
				Tag = chosen,
				Active = (item4 == backdropLook)
			};
			btn.Click += (object? s, EventArgs e) =>
			{
				foreach (ChipButton backdropButton2 in customizePage._backdropButtons)
				{
					backdropButton2.Active = backdropButton2 == btn;
				}
				onBackdropChanged?.Invoke(chosen);
			};
			_backdropButtons.Add(btn);
			_keptCard.Controls.Add(btn);
		}
		RovingFocus.Attach(_backdropButtons, (Control c) => c is ChipButton chipButton && chipButton.Active);
		_keptRuleDim = NewRule(_keptCard);
		if (!_slateLive)
		{
			_dimTitle = Words(_keptCard, "Dim", Fonts.Body, Theme.Text);
			_dimNote = Note(_keptCard, "", ControlInk.TextDisabledInk);
			_dimValue = Words(_keptCard, $"{_settings.CustomBackgroundDim}%", Fonts.BodyBold, Theme.Text, ContentAlignment.MiddleRight);
			_dimSlider = new FlatSlider
			{
				AccessibleName = "Dim",
				Minimum = 0,
				Maximum = 80,
				Value = Math.Clamp(_settings.CustomBackgroundDim, 0, 80)
			};
			FlatSlider dim = _dimSlider;
			Label dimValue = _dimValue;
			dim.ValueChanged += (object? s, EventArgs e) =>
			{
				customizePage._custom?.SetDim(dim.Value);
				dimValue.Text = $"{dim.Value}%";
				customizePage._saveDebounce.Trigger();
				customizePage._onBackgroundChanged();
			};
			_keptCard.Controls.Add(dim);
		}
		_keptRuleBlur = NewRule(_keptCard);
		_blurTitle = Words(_keptCard, "Blur", Fonts.Body, Theme.Text);
		_blurNote = Note(_keptCard, "", ControlInk.TextDisabledInk);
		_blurValue = Words(_keptCard, $"{_settings.CustomBackgroundBlur}%", Fonts.BodyBold, Theme.Text, ContentAlignment.MiddleRight);
		_blurSlider = new FlatSlider
		{
			AccessibleName = "Blur",
			Minimum = 0,
			Maximum = 100,
			Value = Math.Clamp(_settings.CustomBackgroundBlur, 0, 100)
		};
		_blurSlider.ValueChanged += (object? s, EventArgs e) =>
		{
			customizePage._custom?.SetBlur(customizePage._blurSlider.Value);
			customizePage._blurValue.Text = $"{customizePage._blurSlider.Value}%";
			customizePage._saveDebounce.Trigger();
			customizePage._onBackgroundChanged();
		};
		_keptCard.Controls.Add(_blurSlider);
		_keptRuleMatte = NewRule(_keptCard);
		_matteLabel = Words(_keptCard, "Behind the picture", Fonts.Body, Theme.Text);
		_matteLabel.Visible = false;
		_blackChip = new ChipButton
		{
			Text = "Black",
			Visible = false
		};
		_whiteChip = new ChipButton
		{
			Text = "White",
			Visible = false
		};
		_pickChip = new ChipButton
		{
			Text = "Pick a colour…",
			Visible = false
		};
		_blackChip.Click += (object? s, EventArgs e) =>
		{
			ChooseMatte(BackgroundMatte.Black);
		};
		_whiteChip.Click += (object? s, EventArgs e) =>
		{
			ChooseMatte(BackgroundMatte.White);
		};
		_pickChip.Click += (object? s, EventArgs e) =>
		{
			using ColorDialog colorDialog = new ColorDialog
			{
				Color = AppBackground.Matte,
				FullOpen = true
			};
			if (colorDialog.ShowDialog(customizePage) == DialogResult.OK)
			{
				ChooseMatte(colorDialog.Color);
			}
		};
		_keptCard.Controls.Add(_blackChip);
		_keptCard.Controls.Add(_whiteChip);
		_keptCard.Controls.Add(_pickChip);
		_keptRuleOpacity = NewRule(_keptCard);
		_opacityTitle = Words(_keptCard, "Window opacity", Fonts.Body, Theme.Text);
		_opacityValue = Words(_keptCard, $"{Clamp(_settings.OpacityPercent)}%", Fonts.BodyBold, Theme.Text, ContentAlignment.MiddleRight);
		_opacitySlider = new FlatSlider
		{
			AccessibleName = "Window opacity",
			Minimum = 50,
			Maximum = 100,
			Value = Clamp(_settings.OpacityPercent)
		};
		_keptCard.Controls.Add(_opacitySlider);
		_opacityHint = Note(_keptCard, "", Theme.TextFaint);
		RefreshOpacityHint();
		_opacitySlider.ValueChanged += (object? s, EventArgs e) =>
		{
			customizePage._onOpacityChanged(customizePage._opacitySlider.Value);
			customizePage._settings.OpacityPercent = customizePage._opacitySlider.Value;
			customizePage._opacityValue.Text = $"{customizePage._opacitySlider.Value}%";
			customizePage.RefreshOpacityHint();
			customizePage.LayoutContent();
			customizePage._saveDebounce.Trigger();
		};
		_keptRuleSaved = NewRule(_keptCard);
		_savedNote = Note(_keptCard, "All settings are saved automatically.", Theme.TextFaint);
		RefreshPictureControls();
		UseHeaderPanel(_title, _subtitle);
		Resize += (object? _, EventArgs _) =>
		{
			customizePage.LayoutContent();
		};
		_built = true;
		LayoutContent();
		void ChooseMatte(Color matte)
		{
			AppBackground.SetMatte(matte);
			customizePage._settings.CustomBackgroundMatteArgb = matte.ToArgb();
			customizePage._store.Save(customizePage._settings);
			customizePage._blackChip.Active = matte.ToArgb() == BackgroundMatte.Black.ToArgb();
			customizePage._whiteChip.Active = matte.ToArgb() == BackgroundMatte.White.ToArgb();
			customizePage._pickChip.Active = !customizePage._blackChip.Active && !customizePage._whiteChip.Active;
			customizePage._onBackgroundChanged();
			customizePage.RefreshTextCheck();
		}
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		LayoutContent();
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		base.OnVisibleChanged(e);
		if (Visible)
		{
			LayoutContent();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_saveDebounce.Dispose();
		}
		base.Dispose(disposing);
	}

	private static int Clamp(int pct)
	{
		return Math.Clamp(pct, 50, 100);
	}

	private void RefreshOpacityHint()
	{
		_opacityHint.Text = ((_opacitySlider.Value >= 100) ? "Fully opaque. This is the smoothest the window gets." : "Below 100% the window is translucent, and Windows redraws the whole thing every frame instead of copying it. The background animation gets choppier. Set this to 100% if you want it smooth.");
	}

	internal static string ThemeAfterImagePicked(string currentTheme)
	{
		if (!SignatureLooks.TryGet(currentTheme, out var _))
		{
			return "Ash";
		}
		return currentTheme;
	}

	internal static string ThemeAfterImageRemoved(string currentTheme)
	{
		if (!SignatureLooks.TryGet(currentTheme, out var _))
		{
			return "Ash";
		}
		return currentTheme;
	}

	private void ChoosePicture()
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Choose a background image",
			Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*"
		};
		if (openFileDialog.ShowDialog(this) != DialogResult.OK || _custom == null)
		{
			return;
		}
		if (!_custom.SetImage(openFileDialog.FileName))
		{
			_pictureError.Text = "That file couldn't be read as an image.";
			_pictureError.Visible = true;
			LayoutContent();
			return;
		}
		_pictureError.Text = "";
		_pictureError.Visible = false;
		if (_dimSlider != null)
		{
			_dimSlider.Value = _settings.CustomBackgroundDim;
			_dimValue.Text = $"{_settings.CustomBackgroundDim}%";
		}
		_blurSlider.Value = _settings.CustomBackgroundBlur;
		_blurValue.Text = $"{_settings.CustomBackgroundBlur}%";
		_store.Save(_settings);
		RefreshPictureControls();
		_onThemeChanged(ThemeAfterImagePicked(Theme.CurrentName));
	}

	private void RemovePicture()
	{
		if (!APictureIsSet)
		{
			RefreshPictureControls();
			return;
		}
		_custom?.Remove();
		_store.Save(_settings);
		RefreshPictureControls();
		_onThemeChanged(ThemeAfterImageRemoved(Theme.CurrentName));
	}

	private (string Name, string Detail) DescribePicture()
	{
		CustomThemeService custom = _custom;
		string text = ((custom != null && custom.HasImage) ? _custom.ImagePath : null);
		string text2 = (((text != null) ? string.Equals(Path.GetExtension(text), ".gif", StringComparison.OrdinalIgnoreCase) : AppBackground.HasMotion) ? "GIF" : "Image");
		if (text == null)
		{
			return (Name: text2, Detail: "");
		}
		string text3 = SizeOf(text);
		return (Name: Path.GetFileName(text), Detail: (text3 == null) ? text2 : (text2 + " · " + text3));
	}

	private static string? SizeOf(string path)
	{
		try
		{
			using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using Image image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);
			return image.Width.ToString(CultureInfo.InvariantCulture) + " × " + image.Height.ToString(CultureInfo.InvariantCulture);
		}
		catch
		{
			return null;
		}
	}

	private void RefreshPictureControls()
	{
		bool aPictureIsSet = APictureIsSet;
		Color foreColor = (aPictureIsSet ? Theme.Text : ControlInk.TextDisabledInk);
		if (_dimSlider != null)
		{
			_dimSlider.Enabled = aPictureIsSet;
			_dimTitle.ForeColor = foreColor;
			_dimValue.ForeColor = foreColor;
			_dimNote.Text = (aPictureIsSet ? "" : "Needs a background picture.");
			_dimNote.Visible = !aPictureIsSet;
		}
		_blurSlider.Enabled = aPictureIsSet;
		_blurTitle.ForeColor = foreColor;
		_blurValue.ForeColor = foreColor;
		_blurNote.Text = (aPictureIsSet ? "" : "Needs a background picture.");
		_blurNote.Visible = !aPictureIsSet;
		_pictureSwitch.Active = (aPictureIsSet ? "Image / GIF" : "None");
		_clearBtn.Visible = aPictureIsSet;
		_clearBtn.Enabled = aPictureIsSet;
		string text;
		string text2;
		if (aPictureIsSet)
		{
			(text, text2) = DescribePicture();
		}
		else
		{
			text = "";
			text2 = "";
		}
		_pictureName.Text = text;
		_pictureDetail.Text = text2;
		_thumb.Visible = aPictureIsSet;
		_pictureName.Visible = aPictureIsSet;
		_pictureDetail.Visible = aPictureIsSet && text2.Length > 0;
		_noPicture.Visible = !aPictureIsSet;
		_thumb.Invalidate();
		bool needsMatte = AppBackground.NeedsMatte;
		_matteLabel.Visible = needsMatte;
		_blackChip.Visible = needsMatte;
		_whiteChip.Visible = needsMatte;
		_pickChip.Visible = needsMatte;
		int num = AppBackground.Matte.ToArgb();
		_blackChip.Active = num == BackgroundMatte.Black.ToArgb();
		_whiteChip.Active = num == BackgroundMatte.White.ToArgb();
		_pickChip.Active = needsMatte && !_blackChip.Active && !_whiteChip.Active;
		LayoutContent();
	}

	internal void RefreshMatteControlForTest()
	{
		RefreshPictureControls();
	}

	private void RefreshTextCheck()
	{
		double num = TextCheck.Ratio();
		bool flag = num >= 4.5;
		string text = TextCheck.Format(num);
		_checkStatus.Text = (flag ? "Readable" : "Hard to read");
		_checkStatus.Status = ((!flag) ? StatusKind.Warning : StatusKind.Success);
		_checkRatio.Text = text;
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
			UseMnemonic = false,
			UseCompatibleTextRendering = false
		};
		Controls.Add(label);
		return label;
	}

	private static Label Heading(Control card, string text)
	{
		return Words(card, text, Fonts.BodyBold, Theme.Text);
	}

	private static Label Words(Control card, string text, Font font, Color colour, ContentAlignment align = ContentAlignment.MiddleLeft)
	{
		Label label = new Label
		{
			Text = text,
			Font = font,
			ForeColor = colour,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = align,
			UseMnemonic = false,
			UseCompatibleTextRendering = false
		};
		card.Controls.Add(label);
		return label;
	}

	private static Label Note(Control card, string text, Color colour)
	{
		Label label = new Label
		{
			Text = text,
			Font = Fonts.Caption,
			ForeColor = colour,
			BackColor = Color.Transparent,
			AutoSize = true,
			TextAlign = ContentAlignment.TopLeft,
			UseMnemonic = false,
			UseCompatibleTextRendering = false
		};
		card.Controls.Add(label);
		return label;
	}

	private CardPanel NewCard()
	{
		CardPanel cardPanel = new CardPanel();
		Controls.Add(cardPanel);
		return cardPanel;
	}

	private static Rule NewRule(Control card)
	{
		Rule rule = new Rule();
		card.Controls.Add(rule);
		return rule;
	}

	private static int BoxFor(Font font, int line)
	{
		return Math.Max(line, TextRenderer.MeasureText("Ag", font).Height);
	}

	private static int InkInset(Font font)
	{
		int width = TextRenderer.MeasureText("Ag", font).Width;
		int width2 = TextRenderer.MeasureText("Ag", font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
		return Math.Max(0, (width - width2) / 2);
	}

	private static int InkWidth(Label l)
	{
		return TextRenderer.MeasureText(l.Text ?? "", l.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
	}

	private static void PlaceText(Label l, int inkX, int y, int inkW, int h)
	{
		int num = InkInset(l.Font);
		int num2 = BoxFor(l.Font, h);
		int num3 = ((l.Font.SizeInPoints >= Fonts.Body.SizeInPoints - 0.01f) ? S(1) : 0);
		l.SetBounds(inkX - num, y - (num2 - h) / 2 - num3, Math.Max(1, inkW) + 2 * num + 1, num2);
	}

	private static void PlaceValue(Label l, int right, int y, int h)
	{
		int num = BoxFor(l.Font, h);
		y -= (num - h) / 2;
		h = num;
		int num2 = Math.Max(TextRenderer.MeasureText(l.Text ?? "", l.Font).Width, TextRenderer.MeasureText("100%", l.Font).Width);
		l.SetBounds(right - num2, y, num2, h);
	}

	private static int LinesOf(Label note, int inkW)
	{
		if (string.IsNullOrEmpty(note.Text))
		{
			return 0;
		}
		int num = InkInset(note.Font);
		note.MinimumSize = Size.Empty;
		note.MaximumSize = Size.Empty;
		int num2 = Math.Max(1, note.GetPreferredSize(Size.Empty).Height);
		note.MaximumSize = new Size(Math.Max(1, inkW + 2 * num), 0);
		int num3 = (note.AutoSize ? note.Height : note.GetPreferredSize(note.MaximumSize).Height);
		return Math.Max(1, (int)Math.Round((double)num3 / (double)num2));
	}

	private static int PlaceNote(Label note, int inkX, int y, int inkW)
	{
		int num = InkInset(note.Font);
		note.MaximumSize = new Size(Math.Max(1, inkW + 2 * num), 0);
		note.Location = new Point(inkX - num, y + S(1));
		return CaptionHeight(note, inkW);
	}

	private static int CaptionHeight(Label note, int inkW)
	{
		int num = LinesOf(note, inkW);
		if (num == 0)
		{
			return 0;
		}
		int result = Math.Max(num * CaptionLine, note.Height);
		note.MinimumSize = new Size(0, num * CaptionLine);
		return result;
	}

	private static void PlaceRule(Rule rule, int y, int cardW)
	{
		rule.Visible = true;
		rule.SetBounds(Inset, y, cardW - 2 * Inset, ControlInk.Hairline(Tokens.Dpi));
	}

	private void LayoutContent()
	{
		if (!_built || Width <= 0)
		{
			return;
		}
		int contentLeft = GlowPage.ContentLeft;
		int num = ContentRight - contentLeft;
		if (num > 0)
		{
			RefreshTextCheck();
			bool flag = num >= S(760);
			int num2 = (flag ? S(332) : num);
			int num3 = (flag ? (num - ColumnGap - num2) : num);
			int x = (flag ? (contentLeft + num3 + ColumnGap) : contentLeft);
			int y = AutoScrollPosition.Y;
			int y2 = ContentTop + y;
			int num4;
			if (flag)
			{
				int y3 = LayoutLookCard(contentLeft, y2, num3) + CardGap;
				y3 = LayoutKeptCard(contentLeft, y3, num3);
				int y4 = LayoutPictureCard(x, y2, num2) + CardGap;
				y4 = LayoutTextCheckCard(x, y4, num2);
				num4 = Math.Max(y3, y4);
			}
			else
			{
				int y5 = LayoutLookCard(contentLeft, y2, num) + CardGap;
				y5 = LayoutPictureCard(contentLeft, y5, num) + CardGap;
				y5 = LayoutTextCheckCard(contentLeft, y5, num) + CardGap;
				num4 = LayoutKeptCard(contentLeft, y5, num);
			}
			Size size = new Size(0, num4 - y + S(16));
			if (AutoScrollMinSize != size)
			{
				AutoScrollMinSize = size;
			}
		}
	}

	private int LayoutLookCard(int x, int y, int w)
	{
		int num = w - 2 * Inset;
		int inset = Inset;
		int num2 = S(24);
		int width = TextRenderer.MeasureText(_modeCaption.Text ?? "", _modeCaption.Font).Width;
		int num3 = BoxFor(_modeCaption.Font, CaptionLine);
		int num4 = Inset + num + InkInset(_modeCaption.Font) - width - S(8);
		_modeCaption.SetBounds(num4, inset + (num2 - num3) / 2, width + S(8), num3);
		int val = num4 - Inset - 2 * InkInset(_lookTitle.Font) - 1 - S(2);
		PlaceText(_lookTitle, Inset, inset + (num2 - TitleLine) / 2, Math.Max(S(40), val), TitleLine);
		inset += num2 + S(12);
		int num5 = S(12);
		int tileW = (num - num5) / 2;
		int num6 = _lookTiles.Select((LookTile t) => Math.Max(LookTile.HeightFor(tileW, t.Caption, Tokens.Dpi), TileHeightByTheDesign(tileW, t.Caption))).DefaultIfEmpty(S(196)).Max();
		for (int num7 = 0; num7 < _lookTiles.Count; num7++)
		{
			_lookTiles[num7].SetBounds(Inset + num7 * (tileW + num5), inset, tileW, num6);
		}
		inset += num6 + S(12);
		inset = SliderRow(w, inset, null, _opacityRowTitle, _opacityRowNote, _opacityRowValue, _slateOpacity);
		inset = ToggleRow(w, inset, _slateRule1, _matteBlurTitle, _matteBlurNote, _matteBlurToggle);
		inset += S(20);
		_lookCard.SetBounds(x, y, w, inset + 1);
		return _lookCard.Bottom;
	}

	private int LayoutPictureCard(int x, int y, int w)
	{
		int num = w - 2 * Inset;
		int inset = Inset;
		int num2 = S(32);
		int num3 = Math.Min(num, _pictureSwitch.NaturalWidth);
		_pictureSwitch.SetBounds(Inset + num - num3, inset, num3, num2);
		PlaceText(_pictureTitle, Inset, inset + (num2 - TitleLine) / 2, Math.Max(S(40), num - num3 - S(8)), TitleLine);
		inset += num2 + S(12);
		Size thumbSize = ThumbSize;
		_thumb.SetBounds(Inset, inset, thumbSize.Width, thumbSize.Height);
		int inkX = Inset + thumbSize.Width + S(12);
		int inkW = Math.Max(S(40), num - thumbSize.Width - S(12));
		int num4 = TitleLine + S(2) + CaptionLine;
		int num5 = inset + (thumbSize.Height - num4) / 2;
		PlaceText(_pictureName, inkX, num5, inkW, TitleLine);
		PlaceText(_pictureDetail, inkX, num5 + TitleLine + S(2), inkW, CaptionLine);
		PlaceText(_noPicture, Inset, inset, num, TitleLine);
		inset += (_thumb.Visible ? thumbSize.Height : TitleLine);
		int num6 = PlaceNote(_pictureError, Inset, inset + S(8), num);
		if (_pictureError.Visible && _pictureError.Text.Length > 0)
		{
			inset += S(8) + num6;
		}
		inset += S(12);
		int num7 = S(28);
		_chooseBtn.Height = num7;
		_clearBtn.Height = num7;
		_chooseBtn.Width = _chooseBtn.GetPreferredSize(Size.Empty).Width;
		_clearBtn.Width = _clearBtn.GetPreferredSize(Size.Empty).Width;
		_chooseBtn.Location = new Point(Inset, inset);
		_clearBtn.Location = new Point(_chooseBtn.Right + S(8), inset);
		inset += num7;
		inset += S(16);
		PlaceRule(_noteRule, inset, w);
		inset += ControlInk.Hairline(Tokens.Dpi) + S(12);
		_noteIcon.SetBounds(Inset, inset, S(16), S(16));
		inset += PlaceNote(_gifNote, Inset + S(16) + S(8), inset, num - S(16) - S(8));
		inset += S(20);
		_pictureCard.SetBounds(x, y, w, inset + 1);
		return _pictureCard.Bottom;
	}

	private int LayoutTextCheckCard(int x, int y, int w)
	{
		int num = w - 2 * Inset;
		int inset = Inset;
		int num2 = S(24);
		PlaceText(_checkTitle, Inset, inset + (num2 - TitleLine) / 2, num, TitleLine);
		inset += num2 + S(12);
		int num3 = S(23);
		Size size = TextRenderer.MeasureText(_checkStatus.Text ?? "", Fonts.BodyBold, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
		_checkStatus.Size = new Size(S(16) + S(8) + size.Width + S(2), Math.Max(S(20), size.Height));
		_checkStatus.Location = new Point(Inset, inset + (num3 - _checkStatus.Height) / 2 - S(1));
		int num4 = _checkStatus.Right + S(8);
		PlaceText(_checkRatio, num4, inset + (num3 - TitleLine) / 2, Math.Max(S(40), Inset + num - num4), TitleLine);
		inset += num3 + S(4);
		inset += PlaceNote(_checkNote, Inset, inset, num);
		inset += S(20);
		_checkCard.SetBounds(x, y, w, inset + 1);
		return _checkCard.Bottom;
	}

	private int LayoutKeptCard(int x, int y, int w)
	{
		int inkW = w - 2 * Inset;
		int top = ListPadY + ControlInk.Hairline(Tokens.Dpi);
		int height = S(32);
		foreach (ChipButton backdropButton in _backdropButtons)
		{
			backdropButton.Size = new Size(ChipWidth(backdropButton, S(72)), height);
		}
		top = ControlRow(w, top, null, _backdropTitle, _backdropNote, _backdropButtons.Cast<Control>().ToArray());
		if (_dimSlider != null && _dimTitle != null && _dimValue != null)
		{
			top = SliderRow(w, top, _keptRuleDim, _dimTitle, _dimNote, _dimValue, _dimSlider);
		}
		else
		{
			_keptRuleDim.Visible = false;
		}
		top = SliderRow(w, top, _keptRuleBlur, _blurTitle, _blurNote, _blurValue, _blurSlider);
		_blackChip.Size = new Size(ChipWidth(_blackChip, S(72)), height);
		_whiteChip.Size = new Size(ChipWidth(_whiteChip, S(72)), height);
		_pickChip.Size = new Size(ChipWidth(_pickChip, S(140)), height);
		Control[] controls = new Control[3] { _blackChip, _whiteChip, _pickChip };
		if (_matteLabel.Visible)
		{
			top = ControlRow(w, top, _keptRuleMatte, _matteLabel, null, controls);
		}
		else
		{
			_keptRuleMatte.Visible = false;
			ControlRow(w, top, null, _matteLabel, null, controls);
		}
		top = SliderRow(w, top, _keptRuleOpacity, _opacityTitle, _opacityHint, _opacityValue, _opacitySlider);
		PlaceRule(_keptRuleSaved, top, w);
		int num = top + ControlInk.Hairline(Tokens.Dpi);
		int num2 = Math.Max(S(44), CaptionLine + S(8));
		PlaceNote(_savedNote, Inset, num + (num2 - CaptionLine) / 2, inkW);
		top += num2;
		top += ListPadY;
		_keptCard.SetBounds(x, y, w, top + 1);
		return _keptCard.Bottom;
	}

	private static int TileHeightByTheDesign(int tileW, string caption)
	{
		int width = Math.Max(1, tileW - 2 * S(13));
		int height = TextRenderer.MeasureText(caption, Fonts.Caption, new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
		int height2 = TextRenderer.MeasureText("Ag", Fonts.Caption, Size.Empty, TextFormatFlags.NoPadding).Height;
		int num = Math.Max(1, (int)Math.Round((double)height / (double)Math.Max(1, height2)));
		return S(151) + num * CaptionLine + S(13);
	}

	private static int ChipWidth(ChipButton chip, int atLeast)
	{
		return Math.Max(atLeast, TextRenderer.MeasureText(chip.Text ?? "", Fonts.BodyBold).Width + S(24));
	}

	private int RowFrame(int cardW, int top, Rule? rule, Label title, Label? note, int rightW, out int centre)
	{
		int num = cardW - 2 * Inset;
		int inkW = Math.Max(S(60), num - ((rightW > 0) ? (rightW + S(16)) : 0));
		int num2 = ((note != null && !string.IsNullOrEmpty(note.Text)) ? CaptionHeight(note, inkW) : 0);
		int num3 = TitleLine + ((num2 > 0) ? (S(2) + num2) : 0);
		int num4 = Math.Max(RowMin, S(8) + num3);
		int num5 = top;
		if (rule != null)
		{
			PlaceRule(rule, top, cardW);
			num5 = top + ControlInk.Hairline(Tokens.Dpi);
		}
		int num6 = num5 + (num4 - num3) / 2;
		PlaceText(title, Inset, num6, inkW, TitleLine);
		if (note != null)
		{
			PlaceNote(note, Inset, num6 + TitleLine + S(2), inkW);
		}
		centre = num5 + num4 / 2;
		return top + num4;
	}

	private int SliderRow(int cardW, int top, Rule? rule, Label title, Label? note, Label value, FlatSlider slider)
	{
		int num = cardW - 2 * Inset;
		int num2 = Math.Min(RowSliderW, num / 2);
		int rightW = TextRenderer.MeasureText("100%", value.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + S(12) + num2;
		int result = RowFrame(cardW, top, rule, title, note, rightW, out var centre);
		int x = Inset + num - num2;
		slider.SetTrackBounds(x, centre - S(16), num2, S(32));
		PlaceValue(value, slider.Left, centre - TitleLine / 2, TitleLine);
		return result;
	}

	private int ToggleRow(int cardW, int top, Rule? rule, Label title, Label? note, ToggleSwitch toggle)
	{
		int num = cardW - 2 * Inset;
		int result = RowFrame(cardW, top, rule, title, note, S(40), out var centre);
		toggle.SetBounds(Inset + num - S(40), centre - S(10), S(40), S(20));
		return result;
	}

	private int ControlRow(int cardW, int top, Rule? rule, Label title, Label? note, Control[] controls)
	{
		int num = cardW - 2 * Inset;
		int num2 = S(8);
		int num3 = controls.Sum((Control c) => c.Width) + num2 * (controls.Length - 1);
		int num4 = controls.Max((Control c) => c.Height);
		Control[] array;
		if (num - num3 - S(16) >= Math.Max(InkWidth(title), S(60)))
		{
			int result = RowFrame(cardW, top, rule, title, note, num3, out var centre);
			int num5 = Inset + num - num3;
			array = controls;
			foreach (Control control in array)
			{
				control.Location = new Point(num5, centre - control.Height / 2);
				num5 += control.Width + num2;
			}
			return result;
		}
		int num7 = RowFrame(cardW, top, rule, title, note, 0, out var _);
		int num8 = Inset;
		array = controls;
		foreach (Control control2 in array)
		{
			control2.Location = new Point(num8, num7);
			num8 += control2.Width + num2;
		}
		return num7 + num4 + S(12);
	}
}
