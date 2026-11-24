using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using VibranceHud.Capabilities;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Display;
using VibranceHud.Games;

namespace VibranceHud.Pages;

public sealed class MonitorPage : GlowPage
{
	private static class Shape
	{
		public const int PageLeft = 16;

		public const int LeftW = 580;

		public const int ColGap = 16;

		public const int RightX = 612;

		public const int RightW = 336;

		public const int FullW = 932;

		public const int ContentW = 968;

		public const int CardPad = 21;

		public const int TopCardY = 116;

		public const int NowCardH = 371;

		public const int PvpCardH = 508;

		public const int CustomCardY = 503;

		public const int CustomCardH = 74;

		public const int Gap = 16;

		public const int BelowTheFoldY = 656;
	}

	private sealed record PvpRow(PvpResolution Preset, Label Name, Label Size, Label Caption, GlassButton Use, StatusPill InUse, Panel? Divider);

	private sealed class LiveDot : Control
	{
		public LiveDot()
		{
			TabStop = false;
			SetStyle(ControlStyles.Selectable, value: false);
			SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Enabled = false;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			SignalIcons.Draw(e.Graphics, SignalIcon.Success, new RectangleF(0f, 0f, Width, Height), ControlInk.Success);
		}
	}

	private sealed class NoteRing : Control
	{
		public NoteRing()
		{
			TabStop = false;
			SetStyle(ControlStyles.Selectable, value: false);
			SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Enabled = false;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			SignalIcons.Draw(e.Graphics, SignalIcon.Info, new RectangleF(0f, 0f, Width, Height), ControlInk.Info);
		}
	}

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly GameSelection _selection;

	private readonly List<ChipButton> _modeChips = new List<ChipButton>();

	private readonly List<DisplayMode> _modes = new List<DisplayMode>();

	private readonly List<ChipButton> _ruleChips = new List<ChipButton>();

	private Label _currentLabel;

	private Label _resolutionLabel;

	private Label _resolutionCountLabel;

	private Label _nowValueLabel;

	private Label _liveLabel;

	private LiveDot _liveDot;

	private Panel _aspectBox;

	private Label _aspectRatioLabel;

	private Label _aspectMpLabel;

	private Label _aspectCaptionLabel;

	private Panel? _rateRow;

	private GlassTextBox _customWidth;

	private GlassTextBox _customHeight;

	private GlassTextBox _customRate;

	private Label _status;

	private Label _customStatus;

	private Label? _customPanelLine;

	private Label? _customHint;

	private Panel? _offerRow;

	private Label _ruleCaption;

	private ToggleSwitch? _ruleToggle;

	private Label? _ruleSummary;

	private bool _syncingRuleToggle;

	private static readonly TimeSpan RevertGrace = TimeSpan.FromSeconds(8.0);

	private string? _activeDeviceName;

	private CardPanel? _nowCard;

	private CardPanel? _pvpCard;

	private CardPanel? _customCard;

	private Panel _modeGrid;

	private Control[] _underTheColumns = Array.Empty<Control>();

	private int _pvpMeasuredAt;

	private CustomModeRegistrar _customModes = CustomModeRegistrar.Disabled();

	private readonly Func<IReadOnlyList<HdrDisplay>> _hdrReport;

	private readonly Func<IReadOnlyList<MonitorDescriptor>> _monitors;

	private readonly Func<GpuVendor> _gpuVendor;

	private ModeApplier _modeApply = ModeApplier.Dead();

	private Func<string?, IReadOnlyList<DisplayMode>> _reportedModes = (string? d) => DisplayController.SupportedModes(d);

	private Func<string?, DisplayMode?> _currentMode = (string? d) => DisplayController.Current(d);

	private IReadOnlyList<DisplayMode>? _reportedCache;

	private bool? _canRegisterCache;

	private readonly EventHandler _onSelectionChanged;

	private Label _liveWord;

	private bool _liveHasMode;

	private bool _scaled;

	private Label? _nowOnLabel;

	private readonly List<PvpRow> _pvpRows = new List<PvpRow>();

	private Label? _ruleTitle;

	private Panel? _ruleDivider;

	private Panel? _noteDivider;

	private NoteRing? _noteRing;

	private Label? _noteLabel;

	private readonly List<ChipButton> _scalingChips = new List<ChipButton>();

	private Label _scalingStatus;

	private Label _hdrStatus;

	private Label _ruleStatus;

	private CardPanel? _scalingCard;

	private CardPanel? _hdrCard;

	private Label? _scalingDescription;

	private Label? _scalingNote;

	private Label? _hdrExplain;

	private Label? _hdrDetection;

	private GlassTextBox _modeCodeBox;

	private Label _modeShareStatus;

	private string? _ruleGridDevice;

	internal CustomModeRegistrar CustomModes
	{
		get
		{
			return _customModes;
		}
		set
		{
			_customModes = value;
			RefreshCustomArea();
		}
	}

	internal Func<string?, ScalingMode?> ReadScaling { get; set; } = ScalingModeController.Current;

	internal Func<ScalingMode, TimeSpan, string?, ScalingOutcome> ChangeScaling { get; set; } = ScalingModeController.TrySetWithRevert;

	internal ModeApplier ModeApply
	{
		get
		{
			return _modeApply;
		}
		set
		{
			_modeApply = value;
		}
	}

	internal Func<string?, IReadOnlyList<DisplayMode>> ReportedModes
	{
		get
		{
			return _reportedModes;
		}
		set
		{
			_reportedModes = value;
			RefreshCustomArea();
			RebuildModeChips();
			SyncCurrent();
		}
	}

	internal Func<string?, DisplayMode?> CurrentMode
	{
		get
		{
			return _currentMode;
		}
		set
		{
			_currentMode = value;
			SyncCurrent();
		}
	}

	internal string TestModeCode => _modeCodeBox.Text;

	internal string TestModeShareStatus => _modeShareStatus.Text;

	internal ToggleSwitch? TestRuleToggle => _ruleToggle;

	internal string TestRuleSummary => _ruleSummary?.Text ?? "";

	private IReadOnlyList<DisplayMode> Reported()
	{
		IReadOnlyList<DisplayMode> reportedCache = _reportedCache;
		if (reportedCache != null)
		{
			return reportedCache;
		}
		try
		{
			_reportedCache = _reportedModes(_activeDeviceName);
		}
		catch
		{
			_reportedCache = Array.Empty<DisplayMode>();
		}
		return _reportedCache;
	}

	private DisplayMode? Now()
	{
		try
		{
			return _currentMode(_activeDeviceName);
		}
		catch
		{
			return null;
		}
	}

	private bool CanRegisterModes()
	{
		bool valueOrDefault = _canRegisterCache == true;
		if (!_canRegisterCache.HasValue)
		{
			valueOrDefault = CustomModes.Available;
			_canRegisterCache = valueOrDefault;
		}
		return _canRegisterCache.Value;
	}

	private void ForgetWhatTheDisplayAndDriverSaid()
	{
		_reportedCache = null;
		_canRegisterCache = null;
	}

	public MonitorPage(AppSettings settings, SettingsStore store, GameSelection selection, CustomModeRegistrar? customModes = null, ModeApplier? modeApply = null, Func<IReadOnlyList<HdrDisplay>>? hdrReport = null, Func<string?, IReadOnlyList<DisplayMode>>? reportedModes = null, Func<IReadOnlyList<MonitorDescriptor>>? monitors = null, Func<GpuVendor>? gpuVendor = null)
	{
		_settings = settings;
		_store = store;
		_selection = selection;
		_hdrReport = hdrReport ?? new Func<IReadOnlyList<HdrDisplay>>(HdrDetection.Report);
		_monitors = monitors ?? new Func<IReadOnlyList<MonitorDescriptor>>(DisplayController.Monitors);
		_gpuVendor = gpuVendor ?? new Func<GpuVendor>(GpuDetection.PrimaryVendor);
		if (reportedModes != null)
		{
			_reportedModes = reportedModes;
		}
		ModeApply = modeApply ?? ModeApplier.Dead();
		CustomModes = customModes ?? CustomModeRegistrar.Disabled();
		AutoScroll = true;
		Font = Fonts.Body;
		BuildHeader();
		int num = BuildNowCard(116);
		int val = BuildPvpCard(116);
		num = BuildCustomCard(num + 16);
		int y = Math.Max(656, Math.Max(num, val) + 32);
		int count = Controls.Count;
		y = BuildCustomOffersCard(y);
		y = BuildMonitorPickerCard(y);
		y = BuildScalingCard(y);
		y = BuildShareCard(y);
		y = BuildHdrCard(y);
		y = BuildPerGameCard(y);
		_status = new Label
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(16, y),
			AutoSize = true,
			MaximumSize = new Size(932, 0)
		};
		Controls.Add(_status);
		_underTheColumns = Controls.Cast<Control>().Skip(count).ToArray();
		AutoScrollMinSize = new Size(0, y + 60);
		_onSelectionChanged = (object? _, EventArgs _) =>
		{
			RefreshRuleSection();
		};
		_selection.Changed += _onSelectionChanged;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_selection.Changed -= _onSelectionChanged;
		}
		base.Dispose(disposing);
	}

	private void BuildHeader()
	{
		Label title = new Label
		{
			Text = "Resolution",
			ForeColor = Theme.Text,
			Font = Fonts.Display,
			TextAlign = ContentAlignment.MiddleLeft,
			AutoSize = false,
			BackColor = Color.Transparent,
			UseMnemonic = false
		};
		Label subtitle = new Label
		{
			Text = "Change resolution and refresh rate without leaving PlexusX.",
			ForeColor = Theme.TextDim,
			Font = Fonts.Body,
			TextAlign = ContentAlignment.TopLeft,
			AutoSize = false,
			BackColor = Color.Transparent,
			UseMnemonic = false
		};
		_liveDot = new LiveDot
		{
			Size = new Size(16, 16),
			Visible = false
		};
		_liveWord = new Label
		{
			Text = "Live",
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Font = Fonts.BodyBold,
			TextAlign = ContentAlignment.MiddleLeft,
			Size = new Size(40, 20),
			AutoSize = false,
			UseMnemonic = false,
			Visible = false
		};
		_liveLabel = new Label
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			TextAlign = ContentAlignment.MiddleLeft,
			Size = new Size(336, 20),
			AutoSize = false,
			UseMnemonic = false
		};
		UseHeaderPanel(title, subtitle, new Control[3] { _liveDot, _liveWord, _liveLabel });
	}

	private void SyncLiveBadge(DisplayMode? now)
	{
		if (_liveLabel != null && _liveDot != null)
		{
			string text = DisplayLabel();
			Label liveLabel = _liveLabel;
			string text2;
			if (now.HasValue)
			{
				DisplayMode valueOrDefault = now.GetValueOrDefault();
				text2 = $"{text} · {FormatSize(valueOrDefault.Width, valueOrDefault.Height)}, {valueOrDefault.RefreshHz} Hz";
			}
			else
			{
				text2 = text + " · Windows didn't report a mode";
			}
			liveLabel.Text = text2;
			_liveHasMode = now.HasValue;
			PlaceLiveBadge();
		}
	}

	private void PlaceLiveBadge()
	{
		if (_scaled && _liveLabel != null && _liveDot != null)
		{
			int height = Fonts.LineHeight(FontRole.Body);
			_liveLabel.Size = new Size(TextRenderer.MeasureText(_liveLabel.Text, _liveLabel.Font).Width, height);
			_liveWord.Size = new Size(TextRenderer.MeasureText(_liveWord.Text, _liveWord.Font).Width, height);
			_liveDot.Size = new Size(S(16), S(16));
			_liveDot.Visible = _liveHasMode;
			_liveWord.Visible = _liveHasMode;
			LayoutHeaderPanel();
		}
		static int S(int logical)
		{
			return Tokens.Scale(logical);
		}
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		_scaled = true;
		PlaceLiveBadge();
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		PlaceLiveBadge();
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		base.OnLayout(e);
		if (_pvpCard != null && _pvpMeasuredAt != 0 && AtDesignScale(1000) != _pvpMeasuredAt)
		{
			LayoutPvpCard();
			LayoutCustomCard();
			LayoutScalingCard();
			LayoutHdrCard();
			SettleUnderTheColumns();
			for (int i = 1; i < _underTheColumns.Length; i++)
			{
				_underTheColumns[i].Top = _underTheColumns[i - 1].Bottom + AtDesignScale(16);
			}
			FitScrollToContent();
		}
	}

	private string DisplayLabel()
	{
		if (_activeDeviceName == null)
		{
			return "Display 1";
		}
		try
		{
			IReadOnlyList<MonitorDescriptor> readOnlyList = _monitors();
			for (int i = 0; i < readOnlyList.Count; i++)
			{
				if (string.Equals(readOnlyList[i].DeviceName, _activeDeviceName, StringComparison.Ordinal))
				{
					return "Display " + (i + 1).ToString(CultureInfo.InvariantCulture);
				}
			}
		}
		catch
		{
		}
		return "Display 1";
	}

	private int BuildMonitorPickerCard(int y)
	{
		IReadOnlyList<MonitorDescriptor> monitors = _monitors();
		if (monitors.Count < 2)
		{
			return y;
		}
		CardPanel cardPanel = NewCard(16, y, 932, 98);
		cardPanel.Controls.Add(SectionHeading("Display", 21, 21));
		GlassDropdown picker = new GlassDropdown
		{
			AccessibleName = "Display",
			Location = new Point(21, 45),
			Size = new Size(890, 32)
		};
		picker.SetItems(DisplayLabels.For(monitors.Select((MonitorDescriptor m) => ("", FriendlyName: m.FriendlyName, IsPrimary: m.IsPrimary))));
		picker.SelectedIndex = 0;
		picker.SelectedIndexChanged += (object? _, EventArgs _) =>
		{
			if (picker.SelectedIndex >= 0 && picker.SelectedIndex < monitors.Count)
			{
				SwitchDevice(monitors[picker.SelectedIndex].DeviceName);
			}
		};
		cardPanel.Controls.Add(picker);
		Controls.Add(cardPanel);
		return y + cardPanel.Height + 16;
	}

	private void SwitchDevice(string deviceName)
	{
		_activeDeviceName = deviceName;
		if (_noteLabel != null)
		{
			_noteLabel.Text = HdrNoteForThisDisplay();
		}
		RefreshCustomArea();
		RebuildModeChips();
		SyncCurrent();
		SyncScaling();
	}

	private int BuildNowCard(int y)
	{
		CardPanel cardPanel = (_nowCard = NewCard(16, y, 580, 371));
		int num = 538;
		_nowOnLabel = new Label
		{
			Text = "Now on " + DisplayLabel(),
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(Ink(21, Fonts.Caption), 20),
			Size = new Size(260, 16),
			AutoSize = false,
			UseMnemonic = false
		};
		cardPanel.Controls.Add(_nowOnLabel);
		_nowValueLabel = new Label
		{
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Font = Fonts.Value,
			Location = new Point(Ink(21, Fonts.Value), 36),
			Size = new Size(num - 152 - 16, Fonts.Step(FontRole.Value).LinePx + 6),
			AutoSize = false,
			UseMnemonic = false
		};
		cardPanel.Controls.Add(_nowValueLabel);
		_currentLabel = new Label
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(Ink(21, Fonts.Body), 90),
			Size = new Size(num - 152 - 16, 20),
			AutoSize = false,
			UseMnemonic = false
		};
		cardPanel.Controls.Add(_currentLabel);
		_aspectBox = new Panel
		{
			Location = new Point(21 + num - 152, 21),
			Size = new Size(152, 86),
			BackColor = Color.Transparent
		};
		_aspectBox.Paint += (object? _, PaintEventArgs e) =>
		{
			using (SolidBrush brush = new SolidBrush(Theme.GlassFill))
			{
				e.Graphics.FillRectangle(brush, 1, 1, _aspectBox.Width - 2, _aspectBox.Height - 2);
			}
			using Pen pen = new Pen(Theme.Text, 1f);
			e.Graphics.DrawRectangle(pen, 0, 0, _aspectBox.Width - 1, _aspectBox.Height - 1);
		};
		cardPanel.Controls.Add(_aspectBox);
		_aspectRatioLabel = new Label
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			TextAlign = ContentAlignment.TopLeft,
			Location = new Point(Ink(8, Fonts.Caption), 5),
			Size = new Size(136, 16)
		};
		_aspectBox.Controls.Add(_aspectRatioLabel);
		_aspectMpLabel = new Label
		{
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Font = Fonts.BodyBold,
			TextAlign = ContentAlignment.MiddleCenter,
			Location = new Point(0, 33),
			Size = new Size(152, 20)
		};
		_aspectBox.Controls.Add(_aspectMpLabel);
		_aspectCaptionLabel = new Label
		{
			Text = "fills the panel",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			TextAlign = ContentAlignment.TopCenter,
			Location = new Point(21 + num - 152, 108),
			Size = new Size(152, 16)
		};
		cardPanel.Controls.Add(_aspectCaptionLabel);
		cardPanel.Controls.Add(Hairline(21, 141, num));
		_resolutionLabel = SectionHeading("Resolution", 21, 158);
		cardPanel.Controls.Add(_resolutionLabel);
		_resolutionCountLabel = new Label
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			TextAlign = ContentAlignment.TopRight,
			Location = new Point(21 + num / 2, 159),
			Size = new Size(num - num / 2 + GlyphPad(Fonts.Caption), 16),
			AutoSize = false
		};
		cardPanel.Controls.Add(_resolutionCountLabel);
		_modeGrid = new Panel
		{
			Location = new Point(21, 194),
			Size = new Size(num, 72),
			BackColor = Color.Transparent
		};
		cardPanel.Controls.Add(_modeGrid);
		cardPanel.Controls.Add(SectionHeading("Refresh rate", 21, 282));
		cardPanel.Controls.Add(new Label
		{
			Text = "Highest first",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			TextAlign = ContentAlignment.TopRight,
			Location = new Point(21 + num / 2, 283),
			Size = new Size(num - num / 2 + GlyphPad(Fonts.Caption), 16),
			AutoSize = false
		});
		_rateRow = new Panel
		{
			Location = new Point(21, 318),
			Size = new Size(num, 32),
			BackColor = Color.Transparent
		};
		cardPanel.Controls.Add(_rateRow);
		RebuildModeChips();
		Controls.Add(cardPanel);
		SyncCurrent();
		return y + cardPanel.Height;
	}

	private void RebuildModeChips()
	{
		if (_modeGrid == null)
		{
			return;
		}
		_modeGrid.Controls.Clear();
		_modeChips.Clear();
		_modes.Clear();
		List<DisplayMode> list = (from m in DisplayModes.BestPerResolution(Reported())
			orderby m.Width descending, m.Height descending
			select m).ToList();
		List<DisplayMode> list2 = list.Take(12).ToList();
		_resolutionCountLabel.Text = ((list.Count > list2.Count) ? $"{list2.Count} of {list.Count}" : $"{list.Count}") + " mode" + ((list.Count == 1) ? "" : "s") + " this display supports";
		int width = _modeGrid.Width;
		int height = AtDesignScale(32);
		int num = AtDesignScale(40);
		int gap = AtDesignScale(8);
		for (int num2 = 0; num2 < list2.Count; num2++)
		{
			DisplayMode mode = list2[num2];
			int col = num2 % 4;
			int num3 = num2 / 4;
			ChipButton chipButton = new ChipButton
			{
				Text = FormatSize(mode.Width, mode.Height),
				Suffix = ((AspectRatioLabel(mode.Width, mode.Height) == "4:3") ? "4:3" : ""),
				Font = Fonts.Body,
				Size = new Size(ColumnWidth(width, col, 4, gap), height),
				Location = new Point(ColumnLeft(width, col, 4, gap), num3 * num)
			};
			chipButton.Click += (object? _, EventArgs _) =>
			{
				ApplyNow(mode);
			};
			_modes.Add(mode);
			_modeChips.Add(chipButton);
			_modeGrid.Controls.Add(chipButton);
		}
		int gridRows = Math.Max(1, (list2.Count + 3) / 4);
		SetGridRows(gridRows);
	}

	private void SetGridRows(int rows)
	{
		if (_modeGrid == null || _nowCard == null)
		{
			return;
		}
		int num = rows * AtDesignScale(40) - AtDesignScale(8);
		int num2 = num - _modeGrid.Height;
		if (num2 == 0)
		{
			return;
		}
		_modeGrid.Height = num;
		foreach (Control control in _nowCard.Controls)
		{
			if (control != _modeGrid && control.Top > _modeGrid.Top)
			{
				control.Top += num2;
			}
		}
		GrowNowCard(num2);
	}

	private void ApplyNow(DisplayMode mode)
	{
		ApplyWithRevertAndReport(mode.Width, mode.Height, 0, "Switched to " + FormatSize(mode.Width, mode.Height) + ".", "Your monitor refused " + FormatSize(mode.Width, mode.Height) + " - nothing changed.");
		EchoBesideTheCustomBox();
	}

	private void ApplyRate(int hz)
	{
		DisplayMode? displayMode = Now();
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			ApplyWithRevertAndReport(valueOrDefault.Width, valueOrDefault.Height, hz, $"Switched to {hz} Hz.", $"Your monitor refused {hz} Hz at this resolution - nothing changed.");
			EchoBesideTheCustomBox();
		}
	}

	private string ApplyWithRevertAndReport(int width, int height, int hz, string ok, string refused)
	{
		DispChangeResult dispChangeResult = ModeApply.Apply(width, height, hz, _activeDeviceName, RevertGrace);
		string text = dispChangeResult switch
		{
			DispChangeResult.Successful => ok, 
			DispChangeResult.Restart => "That mode needs a restart before it takes effect - nothing changed yet.", 
			DispChangeResult.Reverted => "Reverted - your display is back to how it was.", 
			_ => refused, 
		};
		SetStatus(text, (dispChangeResult == DispChangeResult.Successful) ? Theme.TextDim : Theme.Accent);
		SyncCurrent();
		return text;
	}

	internal static string AspectRatioLabel(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			return "";
		}
		int num = width;
		int num2 = height;
		while (num2 != 0)
		{
			int num3 = num2;
			num2 = num % num2;
			num = num3;
		}
		int num4 = Math.Max(1, num);
		return $"{width / num4}:{height / num4}";
	}

	private void SyncCurrent()
	{
		if (_nowOnLabel != null)
		{
			_nowOnLabel.Text = "Now on " + DisplayLabel();
		}
		DisplayMode? displayMode = Now();
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			string text = AspectRatioLabel(valueOrDefault.Width, valueOrDefault.Height);
			_nowValueLabel.Text = FormatSize(valueOrDefault.Width, valueOrDefault.Height);
			_currentLabel.Text = (IsNative(valueOrDefault) ? $"{valueOrDefault.RefreshHz} Hz · {text} · native" : $"{valueOrDefault.RefreshHz} Hz · {text} stretched to fill");
			_aspectRatioLabel.Text = text;
			_aspectMpLabel.Text = ((double)((long)valueOrDefault.Width * (long)valueOrDefault.Height) / 1000000.0).ToString("0.00", CultureInfo.InvariantCulture) + " MP";
			_aspectCaptionLabel.Text = (IsNative(valueOrDefault) ? "fills the panel" : (text + ", stretched to fill"));
		}
		else
		{
			_nowValueLabel.Text = "";
			_currentLabel.Text = "Windows didn't report a current mode.";
			_aspectRatioLabel.Text = "";
			_aspectMpLabel.Text = "";
		}
		ChipButton chipButton;
		int active;
		for (int i = 0; i < _modeChips.Count; chipButton.Active = (byte)active != 0, i++)
		{
			chipButton = _modeChips[i];
			if (displayMode.HasValue)
			{
				DisplayMode valueOrDefault2 = displayMode.GetValueOrDefault();
				if (_modes[i].Width == valueOrDefault2.Width)
				{
					active = ((_modes[i].Height == valueOrDefault2.Height) ? 1 : 0);
					continue;
				}
			}
			active = 0;
		}
		SyncLiveBadge(displayMode);
		RebuildRateChips(displayMode);
		SyncPvpRows(displayMode);
	}

	private bool IsNative(DisplayMode now)
	{
		PanelSize panelSize = CustomModePanel.Native(Reported());
		if (panelSize.Width == now.Width)
		{
			return panelSize.Height == now.Height;
		}
		return false;
	}

	private void RebuildRateChips(DisplayMode? current)
	{
		if (_rateRow == null)
		{
			return;
		}
		_rateRow.Controls.Clear();
		if (!current.HasValue)
		{
			return;
		}
		DisplayMode valueOrDefault = current.GetValueOrDefault();
		IReadOnlyList<int> readOnlyList = DisplayModes.RefreshRatesFor(Reported(), valueOrDefault.Width, valueOrDefault.Height);
		if (readOnlyList.Count < 2)
		{
			_rateRow.Controls.Add(new Label
			{
				Text = $"{valueOrDefault.RefreshHz} Hz — the only rate this monitor offers at {FormatSize(valueOrDefault.Width, valueOrDefault.Height)}.",
				ForeColor = Theme.TextDim,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(0, AtDesignScale(8)),
				AutoSize = true
			});
			SetRateRowHeight(AtDesignScale(32));
			return;
		}
		int num = Math.Max(1, Math.Min(5, readOnlyList.Count));
		int num2 = 0;
		int num3 = AtDesignScale(32);
		int num4 = AtDesignScale(40);
		int gap = AtDesignScale(8);
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			int num5 = i % num;
			if (i > 0 && num5 == 0)
			{
				num2 += num4;
			}
			int num6 = readOnlyList[i];
			ChipButton chipButton = new ChipButton
			{
				Text = $"{num6} Hz",
				Font = Fonts.Body,
				Size = new Size(ColumnWidth(_rateRow.Width, num5, num, gap), num3),
				Location = new Point(ColumnLeft(_rateRow.Width, num5, num, gap), num2),
				Active = (num6 == valueOrDefault.RefreshHz)
			};
			int captured = num6;
			chipButton.Click += (object? _, EventArgs _) =>
			{
				ApplyRate(captured);
			};
			_rateRow.Controls.Add(chipButton);
		}
		int num7 = num2 + num3;
		string text = _customModes.LinkHint(_activeDeviceName);
		if (text != null)
		{
			Label label = new Label
			{
				Text = text,
				ForeColor = Theme.Accent,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(0, num7 + AtDesignScale(8)),
				MaximumSize = new Size(_rateRow.Width, 0),
				AutoSize = true
			};
			_rateRow.Controls.Add(label);
			num7 = label.Bottom;
		}
		SetRateRowHeight(num7);
	}

	private void SetRateRowHeight(int wanted)
	{
		if (_rateRow != null && _nowCard != null)
		{
			int num = wanted - _rateRow.Height;
			if (num != 0)
			{
				_rateRow.Height = wanted;
				GrowNowCard(num);
			}
		}
	}

	private void GrowNowCard(int delta)
	{
		if (_nowCard == null || delta == 0)
		{
			return;
		}
		_nowCard.Height += delta;
		foreach (Control control in Controls)
		{
			if (control != _nowCard && control != _pvpCard && Array.IndexOf(_underTheColumns, control) < 0 && control.Top > _nowCard.Top)
			{
				control.Top += delta;
			}
		}
		SettleUnderTheColumns();
		FitScrollToContent();
	}

	private void SettleUnderTheColumns()
	{
		if (_underTheColumns.Length == 0 || _nowCard == null || _pvpCard == null)
		{
			return;
		}
		int num = Math.Max(Math.Max(_nowCard.Bottom, _customCard?.Bottom ?? 0), _pvpCard.Bottom);
		int num2 = Math.Max(AtDesignScale(656) + AutoScrollPosition.Y, num + AtDesignScale(32)) - _underTheColumns.Min((Control c) => c.Top);
		if (num2 != 0)
		{
			Control[] underTheColumns = _underTheColumns;
			for (int num3 = 0; num3 < underTheColumns.Length; num3++)
			{
				underTheColumns[num3].Top += num2;
			}
		}
	}

	private int BuildPvpCard(int y)
	{
		CardPanel cardPanel = (_pvpCard = NewCard(612, y, 336, 508));
		int num = 294;
		cardPanel.Controls.Add(SectionHeading("PvP presets", 21, 23));
		cardPanel.Controls.Add(new Label
		{
			Text = "Rust, CS2, Fortnite",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			TextAlign = ContentAlignment.TopRight,
			Location = new Point(21 + num / 2, 24),
			Size = new Size(num - num / 2 + GlyphPad(Fonts.Caption), 16),
			AutoSize = false
		});
		foreach (PvpResolution item in PvpResolutions.All)
		{
			Panel panel = ((_pvpRows.Count == 0) ? null : Hairline(21, 0, num));
			if (panel != null)
			{
				cardPanel.Controls.Add(panel);
			}
			Label label = RowWords(item.Name, Fonts.BodyBold, Theme.Text);
			Label label2 = RowWords("", Fonts.Body, Theme.TextDim);
			Label label3 = RowCaption(item.Why + " " + item.TradeOff);
			GlassButton glassButton = new GlassButton
			{
				Text = "Use",
				AccessibleName = "Use " + item.Name,
				Kind = GlassButtonKind.Secondary,
				Size = new Size(56, 28)
			};
			PvpResolution captured = item;
			glassButton.Click += (object? _, EventArgs _) =>
			{
				ApplyPvp(captured);
			};
			StatusPill statusPill = new StatusPill
			{
				Text = "In use",
				Visible = false
			};
			cardPanel.Controls.Add(label);
			cardPanel.Controls.Add(label2);
			cardPanel.Controls.Add(label3);
			cardPanel.Controls.Add(glassButton);
			cardPanel.Controls.Add(statusPill);
			_pvpRows.Add(new PvpRow(item, label, label2, label3, glassButton, statusPill, panel));
		}
		_ruleDivider = Hairline(21, 0, num);
		cardPanel.Controls.Add(_ruleDivider);
		_ruleTitle = RowWords("Per-game rules", Fonts.BodyBold, Theme.Text);
		cardPanel.Controls.Add(_ruleTitle);
		_ruleToggle = new ToggleSwitch
		{
			Size = new Size(40, 20),
			AccessibleName = "Per-game rules"
		};
		_ruleToggle.CheckedChanged += (object? _, EventArgs _) =>
		{
			RuleToggled();
		};
		cardPanel.Controls.Add(_ruleToggle);
		_ruleSummary = RowCaption("");
		cardPanel.Controls.Add(_ruleSummary);
		_noteDivider = Hairline(21, 0, num);
		cardPanel.Controls.Add(_noteDivider);
		_noteRing = new NoteRing
		{
			Size = new Size(16, 16)
		};
		cardPanel.Controls.Add(_noteRing);
		_noteLabel = RowCaption(HdrNoteForThisDisplay());
		cardPanel.Controls.Add(_noteLabel);
		Controls.Add(cardPanel);
		_pvpMeasuredAt = AtDesignScale(1000);
		SyncPvpRows(Now());
		return y + cardPanel.Height;
	}

	private static Label RowWords(string text, Font font, Color ink)
	{
		return new Label
		{
			Text = text,
			ForeColor = ink,
			BackColor = Color.Transparent,
			Font = font,
			TextAlign = ContentAlignment.TopLeft,
			AutoSize = false,
			AutoEllipsis = false,
			UseMnemonic = false
		};
	}

	private static Label RowCaption(string text)
	{
		return new Label
		{
			Text = text,
			ForeColor = Theme.TextFaint,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			TextAlign = ContentAlignment.TopLeft,
			AutoSize = false,
			UseMnemonic = false
		};
	}

	private void LayoutPvpCard()
	{
		if (_pvpCard == null || _ruleToggle == null || _ruleSummary == null || _ruleTitle == null || _ruleDivider == null || _noteDivider == null || _noteRing == null || _noteLabel == null)
		{
			return;
		}
		_pvpMeasuredAt = AtDesignScale(1000);
		CardPanel pvpCard = _pvpCard;
		int num = AtDesignScale(21);
		int num2 = pvpCard.Width - 2 * num;
		int num3 = Math.Max(1, AtDesignScale(1));
		int num4 = AtDesignScale(20);
		pvpCard.SuspendLayout();
		try
		{
			int num5 = AtDesignScale(49);
			foreach (PvpRow pvpRow in _pvpRows)
			{
				num5 = PlaceDivider(pvpRow.Divider, num, num5, num2, num3);
				int num6 = num5 + AtDesignScale(4);
				int num7 = FitStatus(pvpRow.InUse);
				int num8 = Math.Max(AtDesignScale(56), num7);
				int width = num2 - num8 - AtDesignScale(16);
				int num9 = PlaceWords(pvpRow.Name, Fonts.BodyBold, num, num6, num4);
				PlaceWords(pvpRow.Size, Fonts.Body, num9 + AtDesignScale(8), num6, num4);
				int num10 = PlaceCaption(pvpRow.Caption, num, num6 + num4 + AtDesignScale(2), width);
				int num11 = Math.Max(num5 + AtDesignScale(52), num6 + num4 + AtDesignScale(2) + num10 + AtDesignScale(4));
				int num12 = (num6 + num11 - AtDesignScale(4)) / 2;
				pvpRow.Use.SetBounds(num + num2 - AtDesignScale(56), num12 - AtDesignScale(28) / 2, AtDesignScale(56), AtDesignScale(28));
				pvpRow.InUse.SetBounds(num + num2 - num7, num12 - AtDesignScale(20) / 2, num7, AtDesignScale(20));
				num5 = num11;
			}
			num5 = PlaceDivider(_ruleDivider, num, num5, num2, num3);
			int num13 = num5 + AtDesignScale(4);
			int width2 = num2 - AtDesignScale(40) - AtDesignScale(16);
			PlaceWords(_ruleTitle, Fonts.BodyBold, num, num13, num4);
			int num14 = PlaceCaption(_ruleSummary, num, num13 + num4 + AtDesignScale(2), width2);
			int num15 = Math.Max(num5 + AtDesignScale(52), num13 + num4 + AtDesignScale(2) + num14 + AtDesignScale(4));
			int num16 = (num13 + num15 - AtDesignScale(4)) / 2;
			_ruleToggle.SetBounds(num + num2 - AtDesignScale(40), num16 - AtDesignScale(20) / 2, AtDesignScale(40), AtDesignScale(20));
			num5 = num15;
			int width3 = num2 - AtDesignScale(24);
			int num17 = MeasureCaption(_noteLabel.Text, width3);
			int num18 = num3 + AtDesignScale(12) + num17;
			int num19 = AtDesignScale(508);
			int num20 = Math.Max(num5 + AtDesignScale(16), num19 - num - num18);
			_noteDivider.SetBounds(num, num20, num2, num3);
			_noteRing.SetBounds(num, num20 + num3 + AtDesignScale(12), AtDesignScale(16), AtDesignScale(16));
			PlaceCaption(_noteLabel, num + AtDesignScale(24), num20 + num3 + AtDesignScale(12), width3);
			pvpCard.Height = Math.Max(num19, num20 + num18 + num);
		}
		finally
		{
			pvpCard.ResumeLayout(performLayout: false);
		}
		SettleUnderTheColumns();
		FitScrollToContent();
	}

	private static int PlaceDivider(Panel? divider, int x, int y, int width, int hair)
	{
		if (divider == null)
		{
			return y;
		}
		divider.SetBounds(x, y, width, hair);
		return y + hair;
	}

	private int Current(int devicePixels)
	{
		float num = (float)Tokens.Dpi / 96f;
		float num2 = (float)AtDesignScale(1000) / 1000f;
		return (int)Math.Ceiling((float)devicePixels * num2 / num);
	}

	private int Device(int currentPixels)
	{
		float num = (float)Tokens.Dpi / 96f;
		float num2 = (float)AtDesignScale(1000) / 1000f;
		return (int)Math.Floor((float)currentPixels * num / num2);
	}

	private int PlaceWords(Label label, Font font, int inkX, int top, int line)
	{
		if (label.Font != font)
		{
			label.Font = font;
		}
		Size size = TextRenderer.MeasureText(label.Text, font);
		int width = TextRenderer.MeasureText(label.Text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
		int num = Current(GlyphPad(font));
		label.SetBounds(inkX - num, top, Current(size.Width) + 1, Math.Max(line, Current(size.Height)));
		return inkX + Current(width);
	}

	private int MeasureCaption(string text, int width)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		int height = TextRenderer.MeasureText(text, Fonts.Caption, new Size(Math.Max(20, Device(width)), int.MaxValue), TextFormatFlags.WordBreak).Height;
		return Current(height);
	}

	private int PlaceCaption(Label label, int inkX, int top, int width)
	{
		Font caption = Fonts.Caption;
		if (label.Font != caption)
		{
			label.Font = caption;
		}
		int num = Current(GlyphPad(caption));
		int num2 = MeasureCaption(label.Text, width);
		label.SetBounds(inkX - num, top, width + 2 * num + Current(2), (num2 != 0) ? (num2 + AtDesignScale(4)) : 0);
		return num2;
	}

	private int FitStatus(StatusPill status)
	{
		int width = TextRenderer.MeasureText(status.Text, Fonts.BodyBold, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
		return AtDesignScale(26) + Current(width);
	}

	private (int Width, int Height) PvpSize(PvpResolution preset)
	{
		if (!preset.NeedsStretching)
		{
			PanelSize panelSize = CustomModePanel.Native(Reported());
			if (panelSize.Known)
			{
				return (Width: panelSize.Width, Height: panelSize.Height);
			}
		}
		return (Width: preset.Width, Height: preset.Height);
	}

	internal static string HdrNote(IReadOnlyList<HdrDisplay> report, string? device)
	{
		if (device == null || report == null || report.Where((HdrDisplay d) => string.Equals(d.Device, device, StringComparison.OrdinalIgnoreCase)).Select((Func<HdrDisplay, bool?>)((HdrDisplay d) => !d.Enabled)).FirstOrDefault() != true)
		{
			return "Note · A changed mode asks before it stays.";
		}
		return "Note · HDR is off on this display. A changed mode asks before it stays.";
	}

	private string HdrNoteForThisDisplay()
	{
		string text = _activeDeviceName;
		if (text == null)
		{
			try
			{
				text = (from m in _monitors()
					where m.IsPrimary
					select m.DeviceName).FirstOrDefault();
			}
			catch
			{
				text = null;
			}
		}
		IReadOnlyList<HdrDisplay> report;
		try
		{
			report = _hdrReport();
		}
		catch
		{
			report = Array.Empty<HdrDisplay>();
		}
		return HdrNote(report, text);
	}

	private void SyncPvpRows(DisplayMode? current)
	{
		using (List<PvpRow>.Enumerator enumerator = _pvpRows.GetEnumerator())
		{
			bool flag;
			int num;
			PvpRow current2;
			for (; enumerator.MoveNext(); flag = (byte)num != 0, current2.Use.Visible = !flag, current2.InUse.Visible = flag)
			{
				current2 = enumerator.Current;
				(int Width, int Height) tuple = PvpSize(current2.Preset);
				int item = tuple.Width;
				int item2 = tuple.Height;
				string text = (current2.Preset.NeedsStretching ? FormatSize(item, item2) : (FormatSize(item, item2) + " · " + AspectRatioLabel(item, item2)));
				if (current2.Size.Text != text)
				{
					current2.Size.Text = text;
				}
				if (current.HasValue)
				{
					DisplayMode valueOrDefault = current.GetValueOrDefault();
					if (valueOrDefault.Width == item)
					{
						num = ((valueOrDefault.Height == item2) ? 1 : 0);
						continue;
					}
				}
				num = 0;
			}
		}
		LayoutPvpCard();
	}

	private void RuleToggled()
	{
		if (_syncingRuleToggle || _ruleToggle == null)
		{
			return;
		}
		if ((object)_selection.Current == null)
		{
			RefreshRuleSection();
			return;
		}
		if (!_ruleToggle.Checked)
		{
			SetRule(0, 0, _activeDeviceName);
			return;
		}
		DisplayMode? displayMode = Now();
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			SetRule(valueOrDefault.Width, valueOrDefault.Height, _activeDeviceName);
		}
		else
		{
			SetStatus("Windows didn't report a current mode, so there is no size to save.", Theme.Accent);
			RefreshRuleSection();
		}
	}

	private int BuildCustomCard(int y)
	{
		CardPanel cardPanel = (_customCard = NewCard(16, y, 580, 74));
		int num = 538;
		cardPanel.Controls.Add(SectionHeading("Custom", 21, 27));
		int num2 = 21 + num - 64;
		int num3 = num2 - 8 - 88;
		int num4 = num3 - 28;
		int num5 = num4 - 84;
		int num6 = num5 - 24;
		int x = num6 - 84;
		_customWidth = NumberBox(x, 21, "Width", 84);
		_customHeight = NumberBox(num5, 21, "Height", 84);
		_customRate = NumberBox(num3, 21, "Hz", 88);
		GlassTextBox customWidth = _customWidth;
		string accessibleName = (_customWidth.Inner.AccessibleName = "Custom width");
		customWidth.AccessibleName = accessibleName;
		GlassTextBox customHeight = _customHeight;
		accessibleName = (_customHeight.Inner.AccessibleName = "Custom height");
		customHeight.AccessibleName = accessibleName;
		GlassTextBox customRate = _customRate;
		accessibleName = (_customRate.Inner.AccessibleName = "Custom refresh rate");
		customRate.AccessibleName = accessibleName;
		cardPanel.Controls.Add(_customWidth);
		cardPanel.Controls.Add(_customHeight);
		cardPanel.Controls.Add(_customRate);
		cardPanel.Controls.Add(Between("×", num6, 21, 24));
		cardPanel.Controls.Add(Between("at", num4, 21, 28));
		GlassTextBox[] array = new GlassTextBox[3] { _customWidth, _customHeight, _customRate };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].TextChanged += (object? _, EventArgs _) =>
			{
				AdviseNow();
			};
		}
		array = new GlassTextBox[3] { _customWidth, _customHeight, _customRate };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Inner.KeyDown += (object? _, KeyEventArgs e) =>
			{
				if (e.KeyCode == Keys.Return)
				{
					bool handled = (e.SuppressKeyPress = true);
					e.Handled = handled;
					ApplyCustom();
				}
			};
		}
		GlassButton glassButton = new GlassButton
		{
			Text = "Apply",
			Kind = GlassButtonKind.Secondary,
			Location = new Point(num2, 21),
			Size = new Size(64, 32)
		};
		glassButton.Click += (object? _, EventArgs _) =>
		{
			ApplyCustom();
		};
		cardPanel.Controls.Add(glassButton);
		_customStatus = new Label
		{
			Text = "",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(21, 61),
			Size = new Size(num, 0),
			UseMnemonic = false,
			Visible = false
		};
		cardPanel.Controls.Add(_customStatus);
		Controls.Add(cardPanel);
		AdviseNow();
		return y + cardPanel.Height;
	}

	private void LayoutCustomCard()
	{
		if (_customCard != null && _customStatus != null)
		{
			int num = AtDesignScale(21);
			int width = _customCard.Width - 2 * num;
			int num2 = AtDesignScale(61);
			int num3;
			if (_customStatus.Text.Length == 0)
			{
				_customStatus.Visible = false;
				num3 = AtDesignScale(74);
			}
			else
			{
				int num4 = PlaceCaption(_customStatus, num, num2, width);
				_customStatus.Visible = true;
				num3 = num2 + num4 + num;
			}
			if (_customCard.Height != num3)
			{
				_customCard.Height = num3;
				SettleUnderTheColumns();
				FitScrollToContent();
			}
		}
	}

	private void SayBesideTheCustomBox(string text, Color colour)
	{
		if (_customStatus != null)
		{
			_customStatus.ForeColor = colour;
			if (_customStatus.Text != text)
			{
				_customStatus.Text = text;
			}
			LayoutCustomCard();
		}
	}

	internal void TestSayBesideTheCustomBox(string text)
	{
		SayBesideTheCustomBox(text, Theme.TextDim);
	}

	private int BuildCustomOffersCard(int y)
	{
		int inner = 890;
		CardPanel cardPanel = NewCard(16, y, 932, 0);
		cardPanel.Controls.Add(SectionHeading("Sizes for Custom", 21, 21));
		int num = 49;
		_customPanelLine = new Label
		{
			Text = CustomModeAdvisor.PanelLine(CustomModePanel.Native(Reported())),
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			TextAlign = ContentAlignment.MiddleLeft,
			Location = new Point(Ink(21, Fonts.Caption), num),
			Size = new Size(200 + GlyphPad(Fonts.Caption), 32),
			AutoSize = false,
			UseMnemonic = false
		};
		cardPanel.Controls.Add(_customPanelLine);
		_offerRow = new Panel
		{
			Location = new Point(231, num),
			Size = new Size(inner - 210, 32),
			BackColor = Color.Transparent
		};
		cardPanel.Controls.Add(_offerRow);
		FillOfferRow();
		int num2 = num + 32 + 8;
		int num3 = new string[2]
		{
			AtRestHint(canRegister: true),
			AtRestHint(canRegister: false)
		}.Max((string s) => LogicalTextHeight(s, Fonts.Caption, inner));
		_customHint = new Label
		{
			Text = AtRestHint(CanRegisterModes()),
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(Ink(21, Fonts.Caption), num2),
			Size = new Size(inner, num3),
			UseMnemonic = false
		};
		cardPanel.Controls.Add(_customHint);
		cardPanel.Height = num2 + num3 + 21;
		Controls.Add(cardPanel);
		return y + cardPanel.Height + 16;
	}

	private string AtRestHint(bool canRegister)
	{
		if (!canRegister)
		{
			return "Pick one to fill the Custom boxes, or type any size your monitor reports. If it refuses one, nothing changes.";
		}
		return CustomModeAdvisor.Advise("", "", "", Reported(), canRegister: true, DisplayHelp.HowToAddACustomMode()).Message;
	}

	private static Label Between(string text, int x, int y, int width)
	{
		return new Label
		{
			Text = text,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			TextAlign = ContentAlignment.MiddleCenter,
			Location = new Point(x, y),
			Size = new Size(width, 32),
			AutoSize = false,
			UseMnemonic = false
		};
	}

	private int BuildScalingCard(int y)
	{
		CardPanel cardPanel = (_scalingCard = NewCard(16, y, 932, 152));
		cardPanel.Controls.Add(SectionHeading("Scaling", 21, 21));
		_scalingDescription = new Label
		{
			Text = "How a resolution below the monitor's native one fills the screen. Applies to the display picked above.",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(Ink(21, Fonts.Body), 47),
			AutoSize = false
		};
		cardPanel.Controls.Add(_scalingDescription);
		(ScalingMode, string)[] array = new (ScalingMode, string)[4]
		{
			(ScalingMode.AspectRatio, "Aspect ratio"),
			(ScalingMode.NoScaling, "No scaling"),
			(ScalingMode.FullScreen, "Full screen"),
			(ScalingMode.Integer, "Integer")
		};
		bool flag = _gpuVendor() == GpuVendor.Nvidia;
		int num = 890;
		for (int i = 0; i < array.Length; i++)
		{
			(ScalingMode, string) tuple = array[i];
			ScalingMode mode = tuple.Item1;
			string item = tuple.Item2;
			bool flag2 = mode == ScalingMode.Integer && !flag;
			ChipButton chipButton = new ChipButton
			{
				Text = (flag2 ? (item + " (NVIDIA only)") : item),
				Font = Fonts.Body,
				Size = new Size(ColumnWidth(num, i, 4, 8), 32),
				Location = new Point(21 + ColumnLeft(num, i, 4, 8), 76),
				Enabled = !flag2
			};
			chipButton.Click += (object? _, EventArgs _) =>
			{
				ApplyScaling(mode);
			};
			_scalingChips.Add(chipButton);
			cardPanel.Controls.Add(chipButton);
		}
		int num2 = LogicalTextHeight("Windows reports Integer back as Aspect ratio, so its chip won't light up - check NVIDIA Control Panel to see it.", Fonts.Caption, num);
		if (flag)
		{
			_scalingNote = new Label
			{
				Text = "Windows reports Integer back as Aspect ratio, so its chip won't light up - check NVIDIA Control Panel to see it.",
				ForeColor = Theme.TextDim,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(Ink(21, Fonts.Caption), 116),
				Size = new Size(num, num2)
			};
			cardPanel.Controls.Add(_scalingNote);
			cardPanel.Height = 116 + num2 + 21;
		}
		else
		{
			cardPanel.Height = 129;
		}
		_scalingStatus = AddCardAnswer(cardPanel);
		LayoutScalingCard();
		SyncScaling();
		Controls.Add(cardPanel);
		return y + cardPanel.Height + 16;
	}

	private void LayoutScalingCard()
	{
		if (_scalingCard == null || _scalingDescription == null || _scalingStatus == null)
		{
			return;
		}
		int num = AtDesignScale(21);
		int num2 = _scalingCard.Width - 2 * num;
		FitCardLabel(_scalingDescription, Fonts.Body, num, AtDesignScale(47), num2);
		int num3 = Math.Max(AtDesignScale(76), _scalingDescription.Bottom + AtDesignScale(8));
		foreach (ChipButton scalingChip in _scalingChips)
		{
			scalingChip.Top = num3;
		}
		int num4 = num3 + AtDesignScale(32);
		if (_scalingNote != null)
		{
			FitCardLabel(_scalingNote, Fonts.Caption, num, num4 + AtDesignScale(8), num2);
			num4 = _scalingNote.Bottom;
		}
		FitCardAnswer(_scalingCard, _scalingStatus, num4, num, num2);
	}

	private void LayoutHdrCard()
	{
		if (_hdrCard != null && _hdrExplain != null && _hdrDetection != null && _hdrStatus != null)
		{
			int num = AtDesignScale(21);
			int num2 = _hdrCard.Width - 2 * num;
			FitCardLabel(_hdrExplain, Fonts.Body, num, AtDesignScale(47), num2 - AtDesignScale(70));
			int top = Math.Max(AtDesignScale(97), _hdrExplain.Bottom + AtDesignScale(8));
			FitCardLabel(_hdrDetection, Fonts.Caption, num, top, num2);
			FitCardAnswer(_hdrCard, _hdrStatus, _hdrDetection.Bottom, num, num2);
		}
	}

	private void FitCardLabel(Label label, Font font, int x, int top, int width)
	{
		label.Font = font;
		int num = Current(GlyphPad(font));
		int num2 = width + num;
		int height = Current(DeviceTextHeight(label.Text, font, Device(num2)));
		label.SetBounds(x - num, top, num2, height);
	}

	private void FitCardAnswer(CardPanel card, Label answer, int bottom, int pad, int inner)
	{
		FitCardLabel(answer, Fonts.Caption, pad, bottom + AtDesignScale(8), inner);
		answer.Height = Math.Max(answer.Height, Current(DeviceTextHeight("Ag", Fonts.Caption, Device(answer.Width))));
		card.Height = answer.Bottom + pad;
	}

	private void ApplyScaling(ScalingMode mode)
	{
		string text = mode.Label();
		ShapeFit.Shared.Forget(_activeDeviceName);
		ScalingOutcome scalingOutcome = ChangeScaling(mode, RevertGrace, _activeDeviceName);
		Label scalingStatus = _scalingStatus;
		string text2 = scalingOutcome switch
		{
			ScalingOutcome.Applied => "Scaling set to " + text + ".", 
			ScalingOutcome.SavedForLater => text + " saved. You're at your monitor's own resolution, so nothing is being scaled yet - it kicks in as soon as anything drops below it, including a game that changes resolution itself.", 
			ScalingOutcome.Unconfirmed => text + " sent to the driver. Windows reports it back as Aspect ratio, so PlexusX can't confirm it took - check NVIDIA Control Panel.", 
			ScalingOutcome.Unreadable => text + " was sent, but Windows didn't report the display back, so PlexusX can't tell you whether it took.", 
			ScalingOutcome.Reverted => "Reverted - your display is back to how it was.", 
			ScalingOutcome.RevertFailed => "You said no, but PlexusX couldn't put the old setting back - the display is still on " + text + ". Pick the one you want above, or set it in " + ((_gpuVendor() == GpuVendor.Nvidia) ? "NVIDIA Control Panel." : "your graphics driver's control panel."), 
			_ => "This display wouldn't take " + text + " - nothing changed.", 
		};
		bool flag = (uint)scalingOutcome <= 1u;
		SetCardStatus(scalingStatus, text2, flag ? Theme.TextDim : Theme.Accent);
		SyncScaling(mode, scalingOutcome);
	}

	private void SyncScaling(ScalingMode? justPicked = null, ScalingOutcome? outcome = null)
	{
		ScalingMode? scalingMode = ChipToLight(ReadScaling(_activeDeviceName), justPicked, outcome);
		foreach (ChipButton scalingChip in _scalingChips)
		{
			int active;
			if (scalingMode.HasValue)
			{
				ScalingMode valueOrDefault = scalingMode.GetValueOrDefault();
				active = (scalingChip.Text.StartsWith(valueOrDefault.Label()) ? 1 : 0);
			}
			else
			{
				active = 0;
			}
			scalingChip.Active = (byte)active != 0;
		}
	}

	internal static ScalingMode? ChipToLight(ScalingMode? readBack, ScalingMode? justPicked, ScalingOutcome? outcome)
	{
		if (justPicked == ScalingMode.Integer)
		{
			return null;
		}
		if (outcome == ScalingOutcome.SavedForLater && justPicked.HasValue)
		{
			return justPicked.GetValueOrDefault();
		}
		return readBack;
	}

	private static GlassTextBox NumberBox(int x, int y, string placeholder, int width)
	{
		GlassTextBox glassTextBox = new GlassTextBox
		{
			Location = new Point(x, y),
			Size = new Size(width, 32),
			PlaceholderText = placeholder,
			MaxLength = 5
		};
		AttachDigitOnlyFilter(glassTextBox);
		return glassTextBox;
	}

	internal static void AttachDigitOnlyFilter(GlassTextBox box)
	{
		box.Inner.KeyPress += (object? _, KeyPressEventArgs e) =>
		{
			if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
			{
				e.Handled = true;
			}
		};
		box.Inner.TextChanged += (object? _, EventArgs _) =>
		{
			string text = new string(box.Inner.Text.Where(char.IsDigit).ToArray());
			if (!(text == box.Inner.Text))
			{
				int selectionStart = box.Inner.SelectionStart;
				int num = box.Inner.Text.Substring(0, Math.Min(selectionStart, box.Inner.Text.Length)).Count((char ch) => !char.IsDigit(ch));
				box.Inner.Text = text;
				box.Inner.SelectionStart = Math.Clamp(selectionStart - num, 0, text.Length);
			}
		};
	}

	private void ApplyPvp(PvpResolution preset)
	{
		var (num, num2) = PvpSize(preset);
		ApplyWithRevertAndReport(num, num2, 0, preset.NeedsStretching ? ("Switched to " + FormatSize(num, num2) + ", stretched to fill your screen.") : $"Switched to {num} x {num2}.", $"Your monitor doesn't offer {num} x {num2} - nothing changed.");
		EchoBesideTheCustomBox();
	}

	private void ApplyCustom()
	{
		ForgetWhatTheDisplayAndDriverSaid();
		IReadOnlyList<DisplayMode> readOnlyList = Reported();
		CustomModeAdvice customModeAdvice = CustomModeAdvisor.Advise(_customWidth.Text, _customHeight.Text, _customRate.Text, readOnlyList, CanRegisterModes(), DisplayHelp.HowToAddACustomMode());
		int width = customModeAdvice.Width;
		int height = customModeAdvice.Height;
		int hz = customModeAdvice.Hz;
		switch (customModeAdvice.Standing)
		{
		case CustomModeStanding.NothingTyped:
			SetCustomStatus("Enter a width and height - something like 1440 and 1080.", Theme.Accent);
			return;
		case CustomModeStanding.RefreshTooHigh:
		{
			string text = _customModes.LinkHint(_activeDeviceName);
			if (text != null)
			{
				SetCustomStatus(text, ColourFor(customModeAdvice.Standing));
				return;
			}
			goto case CustomModeStanding.TooSmall;
		}
		case CustomModeStanding.TooSmall:
		case CustomModeStanding.Impossible:
		case CustomModeStanding.CannotRegister:
			SetCustomStatus(customModeAdvice.Message, ColourFor(customModeAdvice.Standing));
			return;
		case CustomModeStanding.NeedsRegistering:
			RegisterAndApplyCustom(width, height, hz);
			return;
		}
		if (hz > 0 && !DisplayModes.IsSupported(readOnlyList, width, height, hz))
		{
			IReadOnlyList<int> values = DisplayModes.RefreshRatesFor(readOnlyList, width, height);
			SetCustomStatus($"{width} x {height} doesn't do {hz} Hz on this monitor. It offers {string.Join(", ", values)} - or leave the box blank for the highest.", Theme.Accent);
			return;
		}
		ApplyWithRevertAndReport(width, height, hz, (hz > 0) ? $"Switched to {width} x {height} at {hz} Hz." : $"Switched to {width} x {height}.", $"Your monitor refused {width} x {height} - nothing changed.");
		EchoBesideTheCustomBox();
	}

	private void RegisterAndApplyCustom(int w, int h, int hz)
	{
		CustomModeAttempt attempt = CustomModes.TryApply(w, h, hz, _activeDeviceName, RevertGrace);
		var (text, colour) = attempt.Outcome switch
		{
			CustomModeOutcome.Applied => ($"Switched to {w} x {h} at {attempt.Hz} Hz. Your monitor didn't offer it, so " + "PlexusX added it to your graphics driver - it's there from now on." + StretchNote(attempt), Theme.TextDim), 
			CustomModeOutcome.AppliedButNotSaved => ($"Switched to {w} x {h} at {attempt.Hz} Hz, but your driver wouldn't keep it - " + "it'll be gone after a restart." + StretchNote(attempt), Theme.Accent), 
			CustomModeOutcome.ScalingFailed => ($"Your driver saved {w} x {h}, but couldn't confirm full-screen scaling. " + "Check scaling in your graphics driver's control panel or pick your old resolution above.", Theme.Accent), 
			CustomModeOutcome.Reverted => ("Reverted - your display is back to how it was.", Theme.Accent), 
			CustomModeOutcome.RevertFailed => ("You said no, but PlexusX couldn't put your old resolution back - you're " + $"still on {w} x {h}. Pick one from Resolution above.", Theme.Accent), 
			CustomModeOutcome.DriverRefused => ($"Your graphics driver wouldn't add {w} x {h} - nothing changed.", Theme.Accent), 
			CustomModeOutcome.OutOfRange => (attempt.Verdict switch
			{
				CustomModeVerdict.TooSmall => $"{w} x {h} is too small to run a desktop in - try something like 1440 and 1080.", 
				CustomModeVerdict.RefreshOutOfRange => $"{hz} Hz is faster than this monitor goes - leave the box blank for its own rate.", 
				_ => CustomModeAdvisor.CannotBeShown(w, h, CustomModePanel.Native(Reported())), 
			}, Theme.Accent), 
			_ => ($"Windows doesn't list {w} x {h} for this monitor, so PlexusX won't force it - that is how people end up on a black screen. {DisplayHelp.HowToAddACustomMode()} " + "Then come back and press Apply.", Theme.Accent), 
		};
		SetCustomStatus(text, colour);
		SyncCurrent();
	}

	private static string StretchNote(CustomModeAttempt attempt)
	{
		if (!attempt.Stretched)
		{
			return "";
		}
		return " It isn't your monitor's shape, so PlexusX stretched it to fill the screen.";
	}

	private void AdviseNow()
	{
		if (_customStatus != null && _customWidth != null)
		{
			CustomModeAdvice customModeAdvice = CustomModeAdvisor.Advise(_customWidth.Text, _customHeight.Text, _customRate.Text, Reported(), CanRegisterModes(), DisplayHelp.HowToAddACustomMode());
			SayBesideTheCustomBox((customModeAdvice.Standing == CustomModeStanding.NothingTyped) ? "" : customModeAdvice.Message, ColourFor(customModeAdvice.Standing));
		}
	}

	private static Color ColourFor(CustomModeStanding standing)
	{
		return standing switch
		{
			CustomModeStanding.NothingTyped => Theme.TextDim, 
			CustomModeStanding.AlreadyListed => Theme.TextDim, 
			CustomModeStanding.NeedsRegistering => Theme.TextDim, 
			_ => Theme.Accent, 
		};
	}

	private void RefreshCustomArea()
	{
		ForgetWhatTheDisplayAndDriverSaid();
		if (_customPanelLine != null)
		{
			_customPanelLine.Text = CustomModeAdvisor.PanelLine(CustomModePanel.Native(Reported()));
		}
		if (_customHint != null)
		{
			_customHint.Text = AtRestHint(CanRegisterModes());
		}
		FillOfferRow();
		AdviseNow();
	}

	private void FillOfferRow()
	{
		if (_offerRow == null)
		{
			return;
		}
		_offerRow.Controls.Clear();
		List<DisplayMode> list = CustomModeOffers.For(CustomModePanel.Native(Reported())).ToList();
		if (list.Count == 0)
		{
			return;
		}
		int num = AtDesignScale(8);
		int num2 = Math.Clamp((_offerRow.Width - num * (list.Count - 1)) / list.Count, AtDesignScale(60), AtDesignScale(112));
		for (int i = 0; i < list.Count; i++)
		{
			DisplayMode captured = list[i];
			ChipButton chipButton = new ChipButton
			{
				Text = FormatSize(captured.Width, captured.Height),
				Font = Fonts.Body,
				Size = new Size(num2, AtDesignScale(32)),
				Location = new Point(i * (num2 + num), 0)
			};
			chipButton.Click += (object? _, EventArgs _) =>
			{
				if (_customWidth != null && _customHeight != null)
				{
					_customWidth.Text = captured.Width.ToString(CultureInfo.InvariantCulture);
					_customHeight.Text = captured.Height.ToString(CultureInfo.InvariantCulture);
					AdviseNow();
					ScrollControlIntoView(_customCard);
					_customWidth.Focus();
				}
			};
			_offerRow.Controls.Add(chipButton);
		}
	}

	private int BuildShareCard(int y)
	{
		CardPanel cardPanel = NewCard(16, y, 932, 138);
		cardPanel.Controls.Add(SectionHeading("Share", 21, 21));
		cardPanel.Controls.Add(new Label
		{
			Text = "Paste a code to use somebody's exact setup, or copy yours to send.",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(Ink(21, Fonts.Body), 47),
			Size = new Size(890 + GlyphPad(Fonts.Body), 20)
		});
		_modeCodeBox = new GlassTextBox
		{
			AccessibleName = "Share code",
			Location = new Point(21, 76),
			Size = new Size(240, 32),
			CharacterCasing = CharacterCasing.Upper,
			PlaceholderText = "PXM-XXXXXXXXXXX",
			MaxLength = 20
		};
		_modeCodeBox.Inner.Font = Fonts.Mono;
		_modeCodeBox.Inner.AccessibleName = "Share code";
		cardPanel.Controls.Add(_modeCodeBox);
		GlassButton glassButton = new GlassButton
		{
			Text = "Copy screen code",
			Kind = GlassButtonKind.Quiet,
			Location = new Point(271, 76),
			Size = new Size(130, 32)
		};
		glassButton.Click += (object? _, EventArgs _) =>
		{
			CopyMyMode();
		};
		cardPanel.Controls.Add(glassButton);
		GlassButton glassButton2 = new GlassButton
		{
			Text = "Apply",
			Kind = GlassButtonKind.Secondary,
			Location = new Point(411, 76),
			Size = new Size(90, 32)
		};
		glassButton2.Click += (object? _, EventArgs _) =>
		{
			ApplySharedMode();
		};
		cardPanel.Controls.Add(glassButton2);
		_modeCodeBox.Inner.KeyDown += (object? _, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Return)
			{
				bool handled = (e.SuppressKeyPress = true);
				e.Handled = handled;
				ApplySharedMode();
			}
		};
		_modeShareStatus = new Label
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(Ink(21, Fonts.Caption), 116),
			Size = new Size(890 + GlyphPad(Fonts.Caption), 20)
		};
		cardPanel.Controls.Add(_modeShareStatus);
		Controls.Add(cardPanel);
		return y + cardPanel.Height + 16;
	}

	private void CopyMyMode()
	{
		DisplayMode? displayMode = Now();
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			ScalingMode scaling = ScalingModeController.Current(_activeDeviceName) ?? ScalingMode.FullScreen;
			string text = MonitorCode.Encode(new MonitorCode(valueOrDefault.Width, valueOrDefault.Height, valueOrDefault.RefreshHz, scaling));
			_modeCodeBox.Text = text;
			if (TheClipboard.Put(text))
			{
				SetModeShareStatus(text + "  —  copied, paste it anywhere", ok: true);
			}
			else
			{
				SetModeShareStatus("Couldn't reach the clipboard — copy the code above.", ok: false);
			}
		}
		else
		{
			SetModeShareStatus("Windows didn't report a current mode, so there's nothing to copy.", ok: false);
		}
	}

	private void ApplySharedMode()
	{
		if (!MonitorCode.TryDecode(_modeCodeBox.Text, out var mode))
		{
			SetModeShareStatus("That code isn't valid. Nothing changed.", ok: false);
		}
		else
		{
			ApplyIncomingMode(mode);
		}
	}

	internal string ApplyLibraryMode(MonitorCode incoming)
	{
		_modeCodeBox.Text = MonitorCode.Encode(incoming);
		return ApplyIncomingMode(incoming);
	}

	private string ApplyIncomingMode(MonitorCode incoming)
	{
		IReadOnlyList<DisplayMode> readOnlyList = Reported();
		if (!DisplayModes.IsSupported(readOnlyList, incoming.Width, incoming.Height))
		{
			string text = "Your monitor doesn't do " + FormatSize(incoming.Width, incoming.Height) + " - that code is for a different screen.";
			SetModeShareStatus(text, ok: false);
			return text;
		}
		int num = DisplayModes.RefreshForSharedMode(readOnlyList, incoming.Width, incoming.Height, incoming.RefreshHz);
		int hz = 0;
		string result = ApplyWithRevertAndReport(incoming.Width, incoming.Height, hz, (num > 0) ? ($"Switched to {FormatSize(incoming.Width, incoming.Height)} at {num} Hz - the " + "fastest your monitor does at that size.") : ("Switched to " + FormatSize(incoming.Width, incoming.Height) + "."), "Your monitor refused " + FormatSize(incoming.Width, incoming.Height) + " - nothing changed.");
		DisplayMode? displayMode = Now();
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			if (valueOrDefault.Width == incoming.Width && valueOrDefault.Height == incoming.Height)
			{
				ApplyScaling(incoming.Scaling);
			}
		}
		return result;
	}

	private void SetModeShareStatus(string text, bool ok)
	{
		_modeShareStatus.ForeColor = (ok ? Theme.TextDim : Theme.Negative);
		_modeShareStatus.Text = text;
	}

	internal void TestSetModeCode(string code)
	{
		_modeCodeBox.Text = code;
	}

	internal void TestCopyMyMode()
	{
		CopyMyMode();
	}

	internal void TestApplySharedModeForTest()
	{
		ApplySharedMode();
	}

	private int BuildHdrCard(int y)
	{
		MachineCapabilities current = Machine.Current;
		if (!current.HdrActive && current.GammaRamp != GammaSupport.Refused && current.GammaRamp != GammaSupport.Clamped)
		{
			return y;
		}
		CardPanel cardPanel = NewCard(16, y, 932, 138);
		cardPanel.Controls.Add(SectionHeading("HDR", 21, 21));
		_hdrCard = cardPanel;
		ToggleSwitch toggle = new ToggleSwitch
		{
			Size = new Size(40, 20),
			Location = new Point(871, 21),
			Checked = current.HdrActive
		};
		string toneLimitation = current.ToneLimitation;
		bool flag = toneLimitation.Length > 0;
		Label value = (_hdrExplain = new Label
		{
			ForeColor = (flag ? Theme.Accent : Theme.TextDim),
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(Ink(21, Fonts.Body), 47),
			Size = new Size(820 + GlyphPad(Fonts.Body), 46),
			Text = (flag ? toneLimitation : (current.HdrActive ? "HDR is on, and your colour controls still work on this PC." : "HDR is off. Your colour controls all work."))
		});
		bool settingBack = false;
		toggle.CheckedChanged += (object? _, EventArgs _) =>
		{
			if (!settingBack)
			{
				bool flag2 = toggle.Checked;
				switch (ModeApply.SetHdr(flag2, _activeDeviceName, RevertGrace))
				{
				case HdrSwitchOutcome.Reverted:
					settingBack = true;
					toggle.Checked = !flag2;
					settingBack = false;
					SetCardStatus(_hdrStatus, "Reverted - HDR is back to how it was.", Theme.Accent);
					break;
				case HdrSwitchOutcome.Failed:
					settingBack = true;
					toggle.Checked = !flag2;
					settingBack = false;
					SetCardStatus(_hdrStatus, "Windows wouldn't change HDR from here — try Display settings.", Theme.Accent);
					break;
				default:
					SetCardStatus(_hdrStatus, flag2 ? "HDR on. Restart PlexusX so it re-checks what your colour controls can do." : "HDR off. Restart PlexusX so it re-checks what your colour controls can do.", Theme.TextDim);
					break;
				}
			}
		};
		cardPanel.Controls.Add(value);
		cardPanel.Controls.Add(toggle);
		string text = HdrWords.DetectionLine(_hdrReport(), flag);
		int num = LogicalTextHeight(text, Fonts.Caption, 890);
		_hdrDetection = new Label
		{
			Text = text,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(Ink(21, Fonts.Caption), 97),
			Size = new Size(890 + GlyphPad(Fonts.Caption), num),
			UseMnemonic = false
		};
		cardPanel.Controls.Add(_hdrDetection);
		cardPanel.Height = 97 + num + 21;
		_hdrStatus = AddCardAnswer(cardPanel);
		LayoutHdrCard();
		Controls.Add(cardPanel);
		return y + cardPanel.Height + 16;
	}

	private int BuildPerGameCard(int y)
	{
		_ruleGridDevice = _activeDeviceName;
		List<DisplayMode> list = (from m in DisplayModes.BestPerResolution(Reported())
			orderby m.Width descending, m.Height descending
			select m).Take(11).ToList();
		foreach (DisplayMode saved in CustomModes.SavedSizes(_ruleGridDevice))
		{
			if (!list.Any((DisplayMode m) => m.Width == saved.Width && m.Height == saved.Height))
			{
				list.Add(saved);
			}
		}
		int num = (list.Count + 1 + 3) / 4;
		CardPanel cardPanel = NewCard(16, y, 932, 122 + num * 40);
		cardPanel.Controls.Add(SectionHeading("When a game launches", 21, 21));
		GameChooser value = new GameChooser(_selection)
		{
			Location = new Point(21, 47),
			Size = new Size(260, 32)
		};
		cardPanel.Controls.Add(value);
		_ruleCaption = new Label
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(Ink(21, Fonts.Body), 103),
			AutoSize = true,
			MaximumSize = new Size(890 + GlyphPad(Fonts.Body), 0)
		};
		cardPanel.Controls.Add(_ruleCaption);
		int num2 = 137;
		int span = 890;
		ChipButton chipButton = new ChipButton
		{
			Text = "Don't change",
			Font = Fonts.Body,
			Size = new Size(ColumnWidth(span, 0, 4, 8), 32),
			Location = new Point(21, num2)
		};
		chipButton.Click += (object? _, EventArgs _) =>
		{
			SetRule(0, 0, _ruleGridDevice);
		};
		_ruleChips.Add(chipButton);
		cardPanel.Controls.Add(chipButton);
		for (int num3 = 0; num3 < list.Count; num3++)
		{
			DisplayMode mode = list[num3];
			int num4 = num3 + 1;
			ChipButton chipButton2 = new ChipButton
			{
				Text = FormatSize(mode.Width, mode.Height),
				Font = Fonts.Body,
				Size = new Size(ColumnWidth(span, num4 % 4, 4, 8), 32),
				Location = new Point(21 + ColumnLeft(span, num4 % 4, 4, 8), num2 + num4 / 4 * 40)
			};
			chipButton2.Click += (object? _, EventArgs _) =>
			{
				SetRule(mode.Width, mode.Height, _ruleGridDevice);
			};
			_ruleChips.Add(chipButton2);
			cardPanel.Controls.Add(chipButton2);
		}
		cardPanel.Height = num2 + num * 40 + 21;
		_ruleStatus = AddCardAnswer(cardPanel);
		Controls.Add(cardPanel);
		RefreshRuleSection();
		return y + cardPanel.Height + 16;
	}

	private void SetRule(int width, int height, string? device)
	{
		SupportedGame current = _selection.Current;
		if ((object)current != null)
		{
			_settings.MonitorRules = MonitorRules.Set(_settings.MonitorRules, current.Id, width, height, device ?? "");
			_store.Save(_settings);
			SetCardStatus(_ruleStatus, (width > 0) ? (current.DisplayName + " will switch to " + FormatSize(width, height) + " on launch, and back when it closes.") : (current.DisplayName + " will leave your resolution alone."), Theme.TextDim);
			RefreshRuleSection();
		}
	}

	private void RefreshRuleSection()
	{
		SupportedGame current = _selection.Current;
		bool flag = current != null;
		foreach (ChipButton ruleChip in _ruleChips)
		{
			ruleChip.Enabled = flag;
		}
		MonitorRule monitorRule = (flag ? MonitorRules.For(_settings.MonitorRules, current.Id) : null);
		if (_ruleCaption != null)
		{
			Label ruleCaption = _ruleCaption;
			string text;
			if (flag)
			{
				text = ((monitorRule == null) ? (current.DisplayName + " currently leaves your resolution alone.") : (current.DisplayName + " switches to " + FormatSize(monitorRule.Width, monitorRule.Height) + ", and back on exit."));
			}
			else
			{
				text = "Pick a game above to set a launch resolution for it.";
			}
			ruleCaption.Text = text;
		}
		if (!flag)
		{
			foreach (ChipButton ruleChip2 in _ruleChips)
			{
				ruleChip2.Active = false;
			}
		}
		else
		{
			_ruleChips[0].Active = monitorRule == null;
			for (int i = 1; i < _ruleChips.Count; i++)
			{
				_ruleChips[i].Active = monitorRule != null && _ruleChips[i].Text == FormatSize(monitorRule.Width, monitorRule.Height);
			}
		}
		SyncRuleRow(current, monitorRule);
		LayoutPvpCard();
	}

	private void SyncRuleRow(SupportedGame? game, MonitorRule? rule)
	{
		if (_ruleToggle == null || _ruleSummary == null)
		{
			return;
		}
		_syncingRuleToggle = true;
		try
		{
			_ruleToggle.Enabled = game != null;
			_ruleToggle.Checked = rule != null;
		}
		finally
		{
			_syncingRuleToggle = false;
		}
		if (game == null)
		{
			_ruleSummary.Text = "Pick a game further down this page to give it a launch resolution.";
			return;
		}
		DisplayMode? displayMode;
		if (rule == null)
		{
			Label? ruleSummary = _ruleSummary;
			displayMode = Now();
			string text;
			if (displayMode.HasValue)
			{
				DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
				text = $"Off. Turn it on and {game.DisplayName} will start in {FormatSize(valueOrDefault.Width, valueOrDefault.Height)}, the size you are in now.";
			}
			else
			{
				text = "Off. " + game.DisplayName + " leaves your resolution alone.";
			}
			ruleSummary.Text = text;
			return;
		}
		Label? ruleSummary2 = _ruleSummary;
		displayMode = Now();
		string text2;
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault2 = displayMode.GetValueOrDefault();
			text2 = $"{game.DisplayName} starts in {FormatSize(rule.Width, rule.Height)} and goes back to {FormatSize(valueOrDefault2.Width, valueOrDefault2.Height)} when it closes.";
		}
		else
		{
			text2 = game.DisplayName + " starts in " + FormatSize(rule.Width, rule.Height) + " and goes back when it closes.";
		}
		ruleSummary2.Text = text2;
	}

	private void SetStatus(string text, Color colour)
	{
		_status.ForeColor = colour;
		_status.Text = text;
	}

	private static Label AddCardAnswer(CardPanel card)
	{
		Label label = new Label
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(Ink(21, Fonts.Caption), card.Height - 21 + 8),
			Size = new Size(card.Width - 42 + GlyphPad(Fonts.Caption), LogicalTextHeight("Ag", Fonts.Caption, card.Width - 42)),
			UseMnemonic = false
		};
		card.Controls.Add(label);
		card.Height = label.Bottom + 21;
		return label;
	}

	private void SetCardStatus(Label answer, string text, Color colour)
	{
		SetStatus(text, colour);
		answer.Text = text;
		answer.ForeColor = colour;
		answer.Height = DeviceTextHeight(text, answer.Font, answer.Width);
		Control parent = answer.Parent;
		int num = answer.Bottom + AtDesignScale(21) - parent.Height;
		parent.Height += num;
		foreach (Control control in Controls)
		{
			if (control.Top > parent.Top)
			{
				control.Top += num;
			}
		}
		FitScrollToContent();
	}

	private void SetCustomStatus(string text, Color colour)
	{
		SetStatus(text, colour);
		EchoBesideTheCustomBox();
	}

	private void EchoBesideTheCustomBox()
	{
		SayBesideTheCustomBox(_status.Text, _status.ForeColor);
	}

	private static string FormatSize(int width, int height)
	{
		return $"{width} × {height}";
	}

	private static CardPanel NewCard(int x, int y, int w, int h)
	{
		return new CardPanel
		{
			Location = new Point(x, y),
			Size = new Size(w, h)
		};
	}

	private static int LogicalTextHeight(string text, Font font, int logicalWidth)
	{
		float num = (float)Tokens.Dpi / 96f;
		int val = (int)Math.Round((float)logicalWidth * num) - 2 * GlyphPad(font) - 2;
		return (int)Math.Ceiling((float)TextRenderer.MeasureText(text, font, new Size(Math.Max(20, val), int.MaxValue), TextFormatFlags.WordBreak).Height / num) + 4;
	}

	private static int DeviceTextHeight(string text, Font font, int deviceWidth)
	{
		int val = deviceWidth - 2 * GlyphPad(font) - 2;
		return TextRenderer.MeasureText(text, font, new Size(Math.Max(20, val), int.MaxValue), TextFormatFlags.WordBreak).Height + Tokens.Scale(4);
	}

	private static int GlyphPad(Font font)
	{
		return (TextRenderer.MeasureText("H", font).Width - TextRenderer.MeasureText("H", font, Size.Empty, TextFormatFlags.NoPadding).Width) / 2;
	}

	private static int Ink(int x, Font font)
	{
		return x - GlyphPad(font);
	}

	private static Label SectionHeading(string text, int x, int y)
	{
		return new Label
		{
			Text = text,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Font = Fonts.BodyBold,
			Location = new Point(Ink(x, Fonts.BodyBold), y),
			Size = new Size(TextRenderer.MeasureText(text, Fonts.BodyBold).Width + 4, 20),
			TextAlign = ContentAlignment.TopLeft,
			AutoSize = false,
			UseMnemonic = false
		};
	}

	private static Panel Hairline(int x, int y, int w)
	{
		return new Panel
		{
			Location = new Point(x, y),
			Size = new Size(w, 1),
			BackColor = Theme.Border
		};
	}

	private static int ColumnLeft(int span, int col, int count, int gap)
	{
		return col * (span + gap) / count;
	}

	private static int ColumnWidth(int span, int col, int count, int gap)
	{
		return ((col == count - 1) ? span : (ColumnLeft(span, col + 1, count, gap) - gap)) - ColumnLeft(span, col, count, gap);
	}
}
