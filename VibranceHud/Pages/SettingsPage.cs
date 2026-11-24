using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VibranceHud.Audio;
using VibranceHud.Capabilities;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Hotkeys;
using VibranceHud.Licensing;
using VibranceHud.Music;
using VibranceHud.Support;

namespace VibranceHud.Pages;

public sealed class SettingsPage : GlowPage
{
	private const int ColumnLeft = 16;

	private const int LeftWidth = 576;

	private const int ColumnGap = 16;

	private const int RightLeft = 608;

	private const int RightWidth = 340;

	private const int CardTop = 0;

	private const int CardGap = 16;

	private const string EngineLineName = "DisplayEngineLine";

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly Action<bool> _onLowLatencyChanged;

	private readonly Action<bool> _onCompetitiveModeChanged;

	private Action? _refreshCompetitiveRow;

	private Control[] _competitiveControls = Array.Empty<Control>();

	private readonly Func<string?> _installerHash;

	private Timer? _audioMeterTimer;

	private Timer? _copyHashTimer;

	private DebouncedAction? _audioSave;

	private Func<Rectangle>? _verifyField;

	private (GlassButton Copy, GlassButton VirusTotal)? _verifyButtons;

	private (Label Lead, Label Tail, Control? Control)? _updateRow;

	private (CardPanel Card, Label Why, Label Result)? _recordingLayout;

	private (CardPanel Card, Label? Why, GlassButton? Fix, Label? Note)? _factsLayout;

	private bool _fitting;

	private Action? _sayWhatWindowsRefused;

	internal const string SystemDefaultBrowser = "System default";

	private Form? _meterHost;

	private bool _meterHostInFront = true;

	protected override string PageTitle => "Settings";

	protected override string PageSubtitle => "How PlexusX itself behaves.";

	protected override bool UsesHeaderPanel => true;

	internal Rectangle VerifyFieldForTest => _verifyField?.Invoke() ?? Rectangle.Empty;

	internal static Color RowCaptionInk => Theme.TextFaint;

	internal void RefreshLicenceControls()
	{
		_refreshCompetitiveRow?.Invoke();
	}

	public SettingsPage(AppSettings settings, SettingsStore store, IVibranceEngine? engine = null, AudioEdgeService? audio = null, Action? onShowLegal = null, Action<bool>? onLowLatencyChanged = null, Func<string?>? installerHash = null, Action? onShowOnDiscordChanged = null, Action? onShowDisplay = null, Action<bool>? onCompetitiveModeChanged = null)
	{
		SettingsPage settingsPage = this;
		_onLowLatencyChanged = onLowLatencyChanged ?? ((Action<bool>)((bool _) =>
		{
		}));
		_onCompetitiveModeChanged = onCompetitiveModeChanged ?? ((Action<bool>)((bool _) =>
		{
		}));
		_settings = settings;
		_store = store;
		_installerHash = installerHash ?? ((Func<string>)(() => LoadedBuild.Sha256()));
		Dock = DockStyle.Fill;
		BackColor = Theme.Background;
		AutoScroll = true;
		Font = Fonts.Body;
		Padding = new Padding(40, 32, 40, 32);
		int leftY = 0;
		int rightY = 0;
		SignalCard general = LeftCard();
		general.Panel.Name = "GeneralCard";
		IStartupRegistration startup = StartupRegistration.For(FlavourPolicy.Current);
		ToggleSwitch startupToggle = SignalCard.Toggle(StartupRegistration.ShowsOn(startup.State, _settings.StartWithWindows));
		SignalCard.Placed startupRow = general.Row("Launch with Windows", "Starts in the tray and puts your look back.", startupToggle);
		bool startupSyncing = false;
		startupToggle.CheckedChanged += async (object? s, EventArgs e) =>
		{
			if (!startupSyncing)
			{
				bool wanted = startupToggle.Checked;
				StartupState startupState;
				if (wanted)
				{
					startupState = await startup.TurnOnAsync();
				}
				else
				{
					startup.TurnOff();
					startupState = startup.State;
				}
				settingsPage._settings.StartWithWindows = StartupRegistration.SavedPreferenceAfter(wanted, startupState);
				settingsPage._store.Save(settingsPage._settings);
				ShowStartupState(startupState);
			}
		};
		if (FlavourPolicy.Current.Startup == StartupMechanism.StartupTask)
		{
			RefreshStartupRowAsync();
		}
		GlassButton glassButton = FlatButton("Change", 0, 0, 79);
		glassButton.Height = 28;
		glassButton.Click += (object? _, EventArgs _) =>
		{
			onShowDisplay?.Invoke();
		};
		general.Row("Quick vibrance shortcut", "Opens the small window over any game.", glassButton, HotkeyPicker.GetSpacedDisplay(_settings.HotkeyModifierMask, _settings.HotkeyVirtualKey));
		HotkeySlot[] slots = MusicHotkey.Slots;
		foreach (HotkeySlot slot in slots)
		{
			(bool, uint, uint) tuple = MusicHotkey.Binding(_settings, slot);
			ToggleSwitch toggle = SignalCard.Toggle(tuple.Item1);
			string text = HotkeyConflicts.Describe(slot);
			toggle.AccessibleName = text;
			toggle.AccessibleDescription = "Optional music shortcut.";
			HotkeyPicker picker = new HotkeyPicker
			{
				ModifierMask = tuple.Item2,
				VirtualKey = tuple.Item3,
				Size = HotkeyPicker.PickerMinimumSize,
				Dimmed = !tuple.Item1,
				AccessibleName = text + " shortcut"
			};
			toggle.CheckedChanged += (object? _, EventArgs _) =>
			{
				picker.Dimmed = !toggle.Checked;
				(bool, uint, uint) tuple2 = MusicHotkey.Binding(settingsPage._settings, slot);
				if (toggle.Checked && MusicHotkey.Clashes(settingsPage._settings, slot, tuple2.Item2, tuple2.Item3))
				{
					toggle.Checked = false;
					picker.ReportBindingResult(succeeded: false);
				}
				else
				{
					MusicHotkey.SetBinding(settingsPage._settings, slot, toggle.Checked, tuple2.Item2, tuple2.Item3);
					picker.ReportBindingResult(MusicPlayer.RebindHotkeys(slot));
					settingsPage._store.Save(settingsPage._settings);
				}
			};
			picker.HotkeyChanged += (uint mask, uint key) =>
			{
				if (MusicHotkey.Clashes(settingsPage._settings, slot, mask, key))
				{
					picker.ReportBindingResult(succeeded: false);
				}
				else
				{
					MusicHotkey.SetBinding(settingsPage._settings, slot, enabled: true, mask, key);
					toggle.Checked = true;
					picker.ReportBindingResult(MusicPlayer.RebindHotkeys(slot));
					settingsPage._store.Save(settingsPage._settings);
				}
			};
			Panel panel = new Panel
			{
				BackColor = Color.Transparent,
				TabStop = false,
				Size = new Size(picker.Width + 12 + toggle.Width, picker.Height)
			};
			toggle.Location = new Point(picker.Width + 12, (panel.Height - toggle.Height) / 2);
			panel.Controls.Add(picker);
			panel.Controls.Add(toggle);
			general.Row(text, "Optional music shortcut.", panel);
		}
		bool flag = _settings.OverlayMode == OverlayMode.Mag;
		ColourReachLine colourReachLine = ColourReachStatus.For(VibrancePage.ColourReachFactsFromEffect(colourReachesScreen: true, engine?.ScreenEffectIsLive, _settings));
		bool flag2 = colourReachLine.Severity == ColourReachSeverity.Refused;
		string caption = (flag2 ? colourReachLine.Text : (flag ? "Shows on your monitor, but not in recordings. Same on every PC; see Recording below." : "Picked automatically for your graphics card."));
		Label engineLine = general.Row("Display engine", caption, null, flag ? "Magnification" : "DX11", (!(flag2 | flag)) ? 1 : 2, flag2 ? new Color?(Theme.Negative) : ((Color?)null)).Caption;
		engineLine.Name = "DisplayEngineLine";
		Label refusedLine = new Label
		{
			ForeColor = ControlInk.Warning,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Padding = new Padding(0, 10, 0, 0),
			Location = new Point(engineLine.Left, engineLine.Bottom),
			Size = new Size(general.ContentWidth, 0),
			MaximumSize = new Size(general.ContentWidth, 0),
			UseMnemonic = false,
			Visible = false
		};
		general.Panel.Controls.Add(refusedLine);
		SayWhatWindowsRefused(HotkeyStatus.Line());
		general.GrowToFit(refusedLine);
		PlaceLeft(general);
		_sayWhatWindowsRefused = () =>
		{
			string text4 = HotkeyStatus.Line();
			if (!(text4 == refusedLine.Text))
			{
				refusedLine.Top = engineLine.Bottom;
				int num4 = general.Panel.Height - LowestChild(general.Panel);
				int height2 = general.Panel.Height;
				SayWhatWindowsRefused(text4);
				general.Panel.Height = LowestChild(general.Panel) + num4;
				settingsPage.ReflowBelow(general.Panel, general.Panel.Height - height2);
			}
		};
		SignalCard perf = LeftCard();
		perf.Panel.Name = "PerformanceCard";
		ToggleSwitch composited = SignalCard.Toggle(_settings.KeepDesktopComposited);
		SignalCard.Placed compositedRow = perf.Row("Keep desktop composited", CompositedNote(_settings.KeepDesktopComposited), composited, null, 2);
		composited.CheckedChanged += (object? s, EventArgs e) =>
		{
			settingsPage._settings.KeepDesktopComposited = composited.Checked;
			compositedRow.Caption.Text = CompositedNote(composited.Checked);
			settingsPage._onLowLatencyChanged(!composited.Checked);
			settingsPage._store.Save(settingsPage._settings);
		};
		string text2 = "";
		if (engine != null)
		{
			OverlayMode overlayMode = _settings.OverlayMode;
			bool driverVibrance = engine.DriverAvailable;
			bool flag3 = CaptureStatus.ToggleCanHelp(overlayMode, driverVibrance);
			ToggleSwitch streaming = (flag3 ? SignalCard.Toggle(_settings.StreamingMode) : null);
			CaptureState captureState = CaptureStatus.Resolve(overlayMode, driverVibrance, streaming?.Checked ?? false);
			SignalCard.Placed placed = perf.Row("Move my colours where recording can see them", CaptureStatus.Headline(captureState), streaming, null, 1, (captureState == CaptureState.Visible) ? RowCaptionInk : ControlInk.Warning, flag3 ? ((Color?)null) : new Color?(Theme.TextDim));
			text2 = CaptureStatus.Reason(captureState, driverVibrance);
			if (streaming != null)
			{
				Label recordingCaption = placed.Caption;
				streaming.CheckedChanged += (object? s2, EventArgs e2) =>
				{
					engine.StreamingMode = streaming.Checked;
					settingsPage._settings.StreamingMode = streaming.Checked;
					settingsPage._store.Save(settingsPage._settings);
					CaptureState captureState2 = CaptureStatus.Resolve(overlayMode, driverVibrance, streaming.Checked);
					recordingCaption.Text = CaptureStatus.Headline(captureState2);
					recordingCaption.ForeColor = ((captureState2 == CaptureState.Visible) ? RowCaptionInk : ControlInk.Warning);
				};
			}
		}
		if (engine != null && engine.DriverAvailable)
		{
			HashSet<Control> before = perf.Panel.Controls.Cast<Control>().ToHashSet();
			int height = perf.Panel.Height;
			ToggleSwitch competitive = SignalCard.Toggle(_settings.CompetitiveMode);
			SignalCard.Placed competitiveRow = perf.Row("Competitive mode", CompetitiveNote(_settings.CompetitiveMode), competitive, null, 2);
			competitive.CheckedChanged += (object? s, EventArgs e) =>
			{
				if (VibranceEngine.CompetitiveModeCanWork(engine))
				{
					settingsPage._settings.CompetitiveMode = competitive.Checked;
					competitiveRow.Caption.Text = CompetitiveNote(competitive.Checked);
					settingsPage._store.Save(settingsPage._settings);
					settingsPage._onCompetitiveModeChanged(competitive.Checked);
				}
			};
			_competitiveControls = (from Control c in perf.Panel.Controls
				where !before.Contains(c)
				select c).ToArray();
			Rectangle[] bounds = _competitiveControls.Select((Control c) => c.Bounds).ToArray();
			int rowHeight = perf.Panel.Height - height;
			bool present = true;
			_refreshCompetitiveRow = () =>
			{
				bool flag4 = VibranceEngine.CompetitiveModeCanWork(engine);
				if (flag4 == present)
				{
					return;
				}
				settingsPage.SuspendLayout();
				try
				{
					for (int i = 0; i < settingsPage._competitiveControls.Length; i++)
					{
						Control control = settingsPage._competitiveControls[i];
						if (flag4)
						{
							control.Bounds = bounds[i];
							settingsPage.ScaleToCurrentDesign(control);
							perf.Panel.Controls.Add(control);
						}
						else
						{
							perf.Panel.Controls.Remove(control);
						}
					}
					int num4 = settingsPage.AtDesignScale(flag4 ? rowHeight : (-rowHeight));
					perf.Panel.Height += num4;
					present = flag4;
					settingsPage.ReflowBelow(perf.Panel, num4);
				}
				finally
				{
					settingsPage.ResumeLayout(performLayout: true);
				}
			};
			RefreshLicenceControls();
		}
		PlaceLeft(perf);
		SignalCard signalCard = LeftCard();
		signalCard.Panel.Name = "UpdatesCard";
		GlassButton checkBtn = FlatButton("Check for updates", 0, 0, 137);
		checkBtn.Height = 28;
		checkBtn.Click += async (object? s, EventArgs e) =>
		{
			checkBtn.Enabled = false;
			checkBtn.Text = "Checking…";
			try
			{
				await UpdateService.CheckManuallyAsync(settingsPage.FindForm());
			}
			finally
			{
				if (!checkBtn.IsDisposed)
				{
					checkBtn.Text = "Check for updates";
					checkBtn.Enabled = true;
				}
			}
		};
		signalCard.StatusRow("PlexusX " + UpdateService.CurrentVersion.ToString(3), FlavourPolicy.Current.UpdatesLine, checkBtn);
		_updateRow = signalCard.StatusParts;
		PlaceLeft(signalCard);
		MachineCapabilities current = Machine.Current;
		SignalCard signalCard2 = LeftCard();
		signalCard2.Panel.Name = "DiagnosticsCard";
		GlassButton glassButton2 = FlatButton("Copy diagnostics", 0, 0, 137);
		glassButton2.Height = 28;
		SignalCard.Placed diagRow = signalCard2.Row("Diagnostics", "Copies these facts for us - no personal details.", glassButton2);
		glassButton2.Click += (object? s, EventArgs e) =>
		{
			string text4 = CaptureDiagnostic.BuildReport(settingsPage._settings, engine?.DriverAvailable ?? false, CaptureProbe.Failed("not run"));
			diagRow.Caption.Text = (TheClipboard.Put(text4) ? "Copied. Paste it to us on Discord - it has no personal details in it." : "Couldn't reach the clipboard. Try again in a moment.");
		};
		PlaceLeft(signalCard2);
		int x = 18;
		CardPanel cardPanel = new CardPanel
		{
			Location = new Point(608, rightY),
			Size = new Size(340, 183),
			Name = "AboutCard"
		};
		cardPanel.Controls.Add(new LogoBox
		{
			Image = BrandAssets.HorizontalLogo(Theme.IsLight),
			Location = new Point(21, 20),
			Size = new Size(110, 21)
		});
		cardPanel.Controls.Add(new Label
		{
			Text = "Changes what your monitor shows, never touches the game.",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(x, 58),
			Size = new Size(298, 40)
		});
		cardPanel.Controls.Add(new Label
		{
			Text = "Version " + UpdateService.CurrentVersion.ToString(3) + " · Windows 10 and 11",
			ForeColor = RowCaptionInk,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(20, 105),
			Size = new Size(298, 16)
		});
		GlassButton glassButton3 = FlatButton("Send a bug report", 21, 134, 138);
		glassButton3.Height = 28;
		cardPanel.Controls.Add(glassButton3);
		if (onShowLegal != null)
		{
			GlassLink glassLink = new GlassLink
			{
				Text = "Legal",
				Location = new Point(173, 134),
				Size = new Size(60, 28),
				TextAlign = ContentAlignment.MiddleLeft
			};
			glassLink.Click += (object? s, EventArgs e) =>
			{
				onShowLegal();
			};
			cardPanel.Controls.Add(glassLink);
		}
		PlaceRight(cardPanel);
		string hash = SafelyReadInstallerHash();
		CardPanel verify = new CardPanel
		{
			Location = new Point(608, rightY),
			Size = new Size(340, (hash != null) ? 184 : 122),
			Name = "VerifyCard"
		};
		Rectangle verifyIcon = new Rectangle(21, 25, 16, 16);
		verify.Paint += (object? s, PaintEventArgs e) =>
		{
			SignalIcons.Draw(e.Graphics, SignalIcon.Verify, new RectangleF(Tokens.Scale(verifyIcon.X), Tokens.Scale(verifyIcon.Y), Tokens.Scale(verifyIcon.Width), Tokens.Scale(verifyIcon.Height)), Theme.Text);
		};
		verify.Controls.Add(new Label
		{
			Text = "Verify this copy",
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Font = Fonts.BodyBold,
			Location = new Point(42, 23),
			Size = new Size(274, 20),
			TextAlign = ContentAlignment.MiddleLeft
		});
		if (hash != null)
		{
			verify.Controls.Add(new Label
			{
				Text = FlavourWording.VerifyCaption(FlavourPolicy.Current),
				ForeColor = RowCaptionInk,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(20, 48),
				Size = new Size(298, 32)
			});
			Label hashLabel = new Label
			{
				Text = GroupedAndElidedHash(hash),
				ForeColor = Theme.Text,
				BackColor = Color.Transparent,
				Font = Fonts.Mono,
				Location = new Point(32, 97),
				Size = new Size(278, 20),
				Name = "VerifyHash"
			};
			_verifyField = () => new Rectangle(settingsPage.AtDesignScale(21), hashLabel.Top - settingsPage.AtDesignScale(6), verify.Width - 2 * settingsPage.AtDesignScale(21), settingsPage.AtDesignScale(32));
			verify.Paint += (object? s, PaintEventArgs e) =>
			{
				PaintWellAt(e.Graphics, settingsPage.VerifyFieldForTest);
			};
			verify.Controls.Add(hashLabel);
			GlassButton copyHashBtn = FlatButton("Copy", 21, 135, 79);
			copyHashBtn.Height = 28;
			copyHashBtn.Icon = SignalIcon.Copy;
			copyHashBtn.Text = "Copied";
			copyHashBtn.Width = PreferredLogicalWidth(copyHashBtn);
			copyHashBtn.Text = "Copy";
			copyHashBtn.Click += (object? s, EventArgs e) =>
			{
				copyHashBtn.Text = (TheClipboard.Put(hash) ? "Copied" : "Couldn't copy");
				settingsPage.ShowCopyHashFeedback(copyHashBtn);
			};
			verify.Controls.Add(copyHashBtn);
			GlassButton glassButton4 = FlatButton("Check on VirusTotal", 0, 135, 171);
			glassButton4.Height = 28;
			glassButton4.TrailingIcon = SignalIcon.ExternalLink;
			glassButton4.Width = PreferredLogicalWidth(glassButton4);
			glassButton4.Left = copyHashBtn.Right + 8;
			glassButton4.Click += (object? s, EventArgs e) =>
			{
				LoopbackSignIn.OpenBrowser("https://www.virustotal.com/gui/file/" + hash);
			};
			verify.Controls.Add(glassButton4);
			_verifyButtons = (copyHashBtn, glassButton4);
		}
		else
		{
			verify.Controls.Add(new Label
			{
				Text = "Not available right now. This does not mean anything is wrong - try again after a restart.",
				ForeColor = RowCaptionInk,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(x, 50),
				Size = new Size(298, 48)
			});
		}
		PlaceRight(verify);
		SignalCard signalCard4 = LeftCard();
		signalCard4.Panel.Name = "DiscordCard";
		ToggleSwitch discordToggle = SignalCard.Toggle(_settings.ShowOnDiscord);
		discordToggle.CheckedChanged += (object? _, EventArgs _) =>
		{
			settingsPage._settings.ShowOnDiscord = discordToggle.Checked;
			settingsPage._store.Save(settingsPage._settings);
			onShowOnDiscordChanged?.Invoke();
		};
		signalCard4.Row("Show PlexusX on my Discord profile", "Lets people on Discord see you are using PlexusX.", discordToggle);
		PlaceLeft(signalCard4);
		if (audio != null)
		{
			SignalCard signalCard5 = LeftCard();
			signalCard5.Panel.Name = "LoudFootstepsCard";
			ToggleSwitch audioToggle = SignalCard.Toggle(_settings.AudioEdgeEnabled);
			signalCard5.Row("Loud footsteps", "Pulls loud sounds down so quiet ones stay audible. It lowers everything you hear, voice chat too.", audioToggle, null, 2);
			Label ceilingValue = new Label
			{
				Text = $"{_settings.AudioEdgeThresholdPercent}%",
				ForeColor = Theme.TextDim,
				BackColor = Color.Transparent,
				Font = Fonts.Body,
				Size = new Size(48, 20),
				TextAlign = ContentAlignment.MiddleRight
			};
			SignalCard.Placed placed2 = signalCard5.Row("Hold sounds above", "Anything louder than this gets held down. Lower holds more. 50% is a good start.", ceilingValue);
			FlatSlider ceilingSlider = new FlatSlider
			{
				AccessibleName = "Loud footsteps limit",
				Minimum = 5,
				Maximum = 100,
				Value = Math.Clamp(_settings.AudioEdgeThresholdPercent, 5, 100)
			};
			ceilingSlider.SetTrackBounds(21, placed2.Bottom + 2, signalCard5.ContentWidth);
			signalCard5.Panel.Controls.Add(ceilingSlider);
			DebouncedAction saveAudio = new DebouncedAction(() =>
			{
				settingsPage._store.Save(settingsPage._settings);
			}, 500);
			_audioSave = saveAudio;
			Label meter = new Label
			{
				ForeColor = RowCaptionInk,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(18, ceilingSlider.Bottom + 8),
				AutoSize = false,
				Size = new Size(signalCard5.ContentWidth, 16),
				Text = "No sound reaching PlexusX right now."
			};
			signalCard5.Panel.Controls.Add(meter);
			ceilingSlider.ValueChanged += (object? s, EventArgs e) =>
			{
				settingsPage._settings.AudioEdgeThresholdPercent = ceilingSlider.Value;
				ceilingValue.Text = $"{ceilingSlider.Value}%";
				audio.Threshold = (float)ceilingSlider.Value / 100f;
				saveAudio.Trigger();
			};
			_audioMeterTimer = new Timer
			{
				Interval = 100
			};
			_audioMeterTimer.Tick += (object? s, EventArgs e) =>
			{
				if (!settingsPage.Visible || settingsPage.IsDisposed)
				{
					settingsPage.SyncAudioMeter();
				}
				else if (!audio.IsRunning)
				{
					meter.Text = $"Off - your {ceilingSlider.Value}% limit does nothing until you switch this on.";
					meter.ForeColor = RowCaptionInk;
				}
				else
				{
					AudioEdgeService.MeterState meter2 = audio.Meter;
					if (meter2 == AudioEdgeService.MeterState.NoReading)
					{
						meter.Text = "No sound reaching PlexusX right now.";
						meter.ForeColor = RowCaptionInk;
					}
					else
					{
						int value = (int)Math.Round(audio.Peak * 100f);
						bool flag4 = meter2 == AudioEdgeService.MeterState.Holding;
						meter.Text = (flag4 ? $"Now: {value}% - above your {ceilingSlider.Value}% limit, holding it down." : $"Now: {value}% - under your {ceilingSlider.Value}% limit, nothing to hold.");
						meter.ForeColor = (flag4 ? ControlInk.Warning : RowCaptionInk);
					}
				}
			};
			SyncAudioMeter();
			audioToggle.CheckedChanged += (object? s, EventArgs e) =>
			{
				settingsPage._settings.AudioEdgeEnabled = audioToggle.Checked;
				if (audioToggle.Checked)
				{
					audio.Threshold = (float)ceilingSlider.Value / 100f;
					audio.Start();
				}
				else
				{
					audio.Stop();
				}
				settingsPage._store.Save(settingsPage._settings);
			};
			signalCard5.GrowToFitAll();
			PlaceLeft(signalCard5);
		}
		if (engine != null)
		{
			bool driverVibrance2 = engine.DriverAvailable;
			SignalCard signalCard6 = LeftCard();
			signalCard6.Panel.Name = "RecordingCard";
			GlassButton testBtn = FlatButton("Test my recording setup", 0, 0, 180);
			testBtn.Height = 28;
			SignalCard.Placed placed3 = signalCard6.Row("Recording", "", testBtn);
			Label label = new Label
			{
				Text = ((text2.Length > 0) ? (text2 + " ") : "") + "In OBS use Display Capture, not Game Capture - Game Capture reads the game directly and never sees this. Gamma is never captured either way.",
				ForeColor = RowCaptionInk,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(18, placed3.Bottom + 2),
				AutoSize = true,
				MaximumSize = new Size(signalCard6.ContentWidth, 0)
			};
			signalCard6.Panel.Controls.Add(label);
			Label result = new Label
			{
				ForeColor = RowCaptionInk,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(18, label.Bottom + 8),
				AutoSize = true,
				MaximumSize = new Size(signalCard6.ContentWidth, 0),
				Text = "The test takes five seconds and flashes your screen. Nothing is sent anywhere."
			};
			signalCard6.Panel.Controls.Add(result);
			_recordingLayout = (signalCard6.Panel, label, result);
			testBtn.Click += async (object? s2, EventArgs e2) =>
			{
				if (GlassDialog.Show(settingsPage.FindForm(), "Test my recording setup", "This flashes your screen on and off for about five seconds while it measures what recording software actually receives.\r\n\r\nRun it now?", GlassDialogButtons.YesNo, GlassDialogTone.Info, "Run test") == DialogResult.Yes)
				{
					testBtn.Enabled = false;
					testBtn.Text = "Measuring…";
					result.ForeColor = RowCaptionInk;
					result.Text = "Measuring - leave the screen alone for a few seconds.";
					CaptureProbe captureProbe = await Task.Run(() => engine.RunCaptureProbe());
					string text4 = CaptureDiagnostic.BuildReport(settingsPage._settings, driverVibrance2, captureProbe);
					testBtn.Enabled = true;
					testBtn.Text = "Test my recording setup";
					result.ForeColor = (captureProbe.ReachesCapture ? Theme.Text : ControlInk.Warning);
					result.Text = (captureProbe.Ran ? ("Result: " + captureProbe.Note + ".") : (captureProbe.Note + ".")) + " " + (TheClipboard.Put(text4) ? "The full report is on your clipboard - paste it to us on Discord." : "Couldn't reach the clipboard.");
				}
			};
			signalCard6.GrowToFitAll();
			PlaceLeft(signalCard6);
		}
		SignalCard facts = LeftCard();
		facts.Panel.Name = "MachineFactsCard";
		facts.Row("This PC", "What PlexusX measured when it started.");
		int dy = facts.Bottom + 2;
		DiagRow("Screen colour changes", current.GammaRamp switch
		{
			GammaSupport.Working => "Working", 
			GammaSupport.Clamped => "Limited by Windows", 
			GammaSupport.Refused => "Blocked", 
			_ => "Not measured", 
		}, current.GammaRamp == GammaSupport.Working);
		DiagRow("Running as administrator", current.Elevated ? "Yes" : "No", current.Elevated);
		DiagRow("HDR", current.HdrActive ? "On - Windows ignores colour changes" : "Off", !current.HdrActive);
		DiagRow("Graphics", current.Gpu switch
		{
			GpuVendor.Nvidia => "NVIDIA", 
			GpuVendor.Amd => "AMD", 
			GpuVendor.Intel => "Intel", 
			GpuVendor.Other => "Other graphics card", 
			_ => "Couldn't detect", 
		}, current.Gpu != GpuVendor.Unknown);
		DiagRow("Graphics-card vibrance", current.DriverVibrance ? "Available" : "Not available", current.DriverVibrance);
		DiagRow("Monitors", current.MonitorCount.ToString(), good: true);
		string toneLimitation = current.ToneLimitation;
		Label item = null;
		GlassButton item2 = null;
		Label item3 = null;
		if (toneLimitation.Length > 0)
		{
			Label label2 = new Label
			{
				Text = toneLimitation,
				ForeColor = ControlInk.Warning,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(18, dy + 4),
				AutoSize = true,
				MaximumSize = new Size(facts.ContentWidth, 0)
			};
			facts.Panel.Controls.Add(label2);
			item = label2;
			dy = label2.Bottom + 8;
		}
		RepairKind plan = Repair.Plan(current);
		if (plan != RepairKind.None)
		{
			GlassButton glassButton5 = FlatButton(Repair.ButtonText(plan), 21, dy + 4, 190);
			glassButton5.Height = 28;
			Label fixNote = new Label
			{
				Text = Repair.Explain(plan),
				ForeColor = RowCaptionInk,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(221, dy + 10),
				AutoSize = true,
				MaximumSize = new Size(facts.ContentWidth - 200, 0)
			};
			glassButton5.Click += (object? s, EventArgs e) =>
			{
				if (!Repair.Run(plan, () =>
				{
					Application.Exit();
				}))
				{
					fixNote.Text = "Not started - Windows refused or you cancelled. Nothing has changed.";
				}
			};
			facts.Panel.Controls.Add(glassButton5);
			facts.Panel.Controls.Add(fixNote);
			item2 = glassButton5;
			item3 = fixNote;
		}
		facts.GrowToFitAll();
		facts.Panel.Height = facts.Panel.Controls.Cast<Control>().Max((Control c) => c.Bottom) + 21;
		_factsLayout = (facts.Panel, item, item2, item3);
		PlaceLeft(facts);
		SignalCard bug = LeftCard();
		bug.Panel.Name = "BugReportCard";
		bug.Row("Report a bug", "What did you do, and what happened instead? The facts from Diagnostics go with it, so you don't have to describe your PC.", null, null, 2);
		int num3 = bug.Bottom + 8;
		GlassTextBox text3 = new GlassTextBox
		{
			Multiline = true,
			PlaceholderText = "e.g. gamma slider does nothing after I turned HDR off",
			MaxLength = 2000,
			Location = new Point(21, num3),
			Size = new Size(bug.ContentWidth, 96),
			Name = "BugReportText",
			AccessibleName = "What happened",
			TabStop = false
		};
		GlassTextBox contact = new GlassTextBox
		{
			PlaceholderText = "Where can we reply? (email or Discord name - optional)",
			MaxLength = 200,
			Location = new Point(21, num3 + 106),
			Size = new Size(bug.ContentWidth, 32),
			Name = "BugReportContact",
			AccessibleName = "Where can we reply",
			TabStop = false
		};
		text3.Inner.AccessibleName = text3.AccessibleName;
		contact.Inner.AccessibleName = contact.AccessibleName;
		GlassButton send = PrimaryButton("Send", 21, num3 + 152, 120);
		send.Name = "BugReportSend";
		GlassButton glassButton6 = FlatButton("Join our Discord", 153, num3 + 152, 140);
		glassButton6.Click += (object? s, EventArgs e) =>
		{
			LoopbackSignIn.OpenBrowser("https://plexusx.lol/discord");
		};
		Label status = new Label
		{
			Text = "",
			ForeColor = RowCaptionInk,
			BackColor = Color.Transparent,
			Font = Fonts.Caption,
			Location = new Point(18, num3 + 194),
			Size = new Size(bug.ContentWidth, 32),
			Name = "BugReportStatus",
			Visible = false
		};
		send.Click += async (object? s, EventArgs e) =>
		{
			if (!BugReportClient.IsEnough(text3.Text))
			{
				Say(ControlInk.Warning, "Say a little more - what did you do, what happened?");
			}
			else
			{
				send.Enabled = false;
				Say(RowCaptionInk, "Sending…");
				string version = UpdateService.CurrentVersion.ToString(3);
				IReadOnlyDictionary<string, string> facts2 = BugReportClient.Facts(Machine.Current, settingsPage._settings.OverlayMode, (engine as VibranceEngine)?.Meter, version);
				if (await BugReportClient.SendAsync(BugReportClient.BuildBody(version, text3.Text, contact.Text, facts2)))
				{
					Say(Theme.Text, "Sent - thank you. We read every report.");
					text3.Text = "";
				}
				else
				{
					Say(Theme.Negative, "Couldn't send. Email support@plexusx.lol and paste what you wrote.");
					send.Enabled = true;
				}
			}
			void Say(Color ink, string words)
			{
				status.ForeColor = ink;
				status.Text = words;
				status.Visible = true;
				bug.GrowToFit(status);
			}
		};
		bug.Panel.Controls.Add(text3);
		bug.Panel.Controls.Add(contact);
		bug.Panel.Controls.Add(send);
		bug.Panel.Controls.Add(glassButton6);
		bug.GrowToFitAll();
		bug.Panel.Height = Math.Max(bug.Panel.Height, send.Bottom + 21);
		bug.Panel.Controls.Add(status);
		PlaceLeft(bug);
		CardPanel bugCard = bug.Panel;
		glassButton3.Click += (object? s, EventArgs e) =>
		{
			settingsPage.ScrollTo(Math.Max(0, bugCard.Top - Tokens.Scale(0)));
			text3.Focus();
		};
		FitScrollToContent();
		void DiagRow(string what, string value, bool good)
		{
			facts.Panel.Controls.Add(new Label
			{
				Text = what,
				ForeColor = RowCaptionInk,
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(20, dy),
				Size = new Size(240, 16)
			});
			facts.Panel.Controls.Add(new Label
			{
				Text = value,
				ForeColor = (good ? Theme.Text : ControlInk.Warning),
				BackColor = Color.Transparent,
				Font = Fonts.Caption,
				Location = new Point(263, dy),
				Size = new Size(289, 16)
			});
			dy += 20;
		}
		SignalCard LeftCard()
		{
			return new SignalCard(16, leftY, 576);
		}
		void PlaceLeft(SignalCard card)
		{
			settingsPage.Controls.Add(card.Panel);
			leftY = card.Panel.Bottom + 16;
		}
		void PlaceRight(CardPanel cardPanel2)
		{
			settingsPage.Controls.Add(cardPanel2);
			rightY = cardPanel2.Bottom + 16;
		}
		async Task RefreshStartupRowAsync()
		{
			StartupState state = await Task.Run(() => startup.RefreshAsync());
			if (!startupToggle.IsDisposed)
			{
				startupSyncing = true;
				try
				{
					startupToggle.Checked = StartupRegistration.ShowsOn(state, settingsPage._settings.StartWithWindows);
				}
				finally
				{
					startupSyncing = false;
				}
				if (StartupRegistration.IsRefusedByWindows(state) && settingsPage._settings.StartWithWindows)
				{
					settingsPage._settings.StartWithWindows = false;
					settingsPage._store.Save(settingsPage._settings);
				}
				ShowStartupState(state);
			}
		}
		void SayWhatWindowsRefused(string refused)
		{
			refusedLine.Text = refused;
			refusedLine.Top = engineLine.Bottom;
			if (refused.Length > 0)
			{
				refusedLine.Visible = true;
				refusedLine.AutoSize = true;
			}
			else
			{
				refusedLine.AutoSize = false;
				refusedLine.Height = 0;
				refusedLine.Visible = false;
			}
		}
		void ShowStartupState(StartupState state)
		{
			string text4 = StartupRegistration.ExplainState(state);
			if (text4 != null)
			{
				startupSyncing = true;
				try
				{
					startupToggle.Checked = false;
				}
				finally
				{
					startupSyncing = false;
				}
				Label caption2 = startupRow.Caption;
				if (caption2 != null)
				{
					caption2.Text = text4;
				}
			}
		}
	}

	internal static string CompetitiveNote(bool on)
	{
		if (!on)
		{
			return "Full colour everywhere. In borderless games Windows draws it over the game, which can add input delay.";
		}
		return "In full-screen games your colour runs on the NVIDIA driver, with nothing drawn over the game.";
	}

	private static string CompositedNote(bool on)
	{
		if (!on)
		{
			return "Your colours may be missing from OBS, Discord share and clip recordings. What you see on your own screen is unaffected.";
		}
		return "Costs about one extra frame of input delay, roughly 7 ms at 144 Hz.";
	}

	internal static void PaintWell(Graphics g, Rectangle box)
	{
		PaintWellAt(g, new Rectangle(Tokens.Scale(box.X), Tokens.Scale(box.Y), Tokens.Scale(box.Width), Tokens.Scale(box.Height)));
	}

	internal static void PaintWellAt(Graphics g, Rectangle device)
	{
		if (device.Width > 1 && device.Height > 1)
		{
			GraphicsState gstate = g.Save();
			g.TranslateTransform(device.X, device.Y);
			int dpi = Tokens.Dpi;
			ControlInk.PaintBox(g, device.Size, ControlInk.PxF(dpi, 4f), ControlInk.Hairline(dpi), ControlInk.Surface2, ControlInk.Border);
			g.Restore(gstate);
		}
	}

	internal static int PreferredLogicalWidth(GlassButton b)
	{
		int num = ControlInk.Dpi(b);
		return (int)Math.Ceiling((double)b.GetPreferredSize(Size.Empty).Width * 96.0 / (double)num);
	}

	private static int LowestChild(Control card)
	{
		int num = 0;
		foreach (Control control in card.Controls)
		{
			if (control.Height > 0)
			{
				num = Math.Max(num, control.Bottom);
			}
		}
		return num;
	}

	private void ReflowBelow(CardPanel grown, int delta, bool refitScroll = true)
	{
		if (delta == 0)
		{
			return;
		}
		foreach (CardPanel item in Controls.OfType<CardPanel>())
		{
			if (item != grown && item.Top > grown.Top && item.Left < grown.Right && item.Right > grown.Left)
			{
				item.Top += delta;
			}
		}
		if (refitScroll)
		{
			FitScrollToContent();
		}
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		base.OnLayout(e);
		if (_fitting)
		{
			return;
		}
		_fitting = true;
		try
		{
			FitWordsToTheirBoxes();
		}
		finally
		{
			_fitting = false;
		}
	}

	private void FitWordsToTheirBoxes()
	{
		if (Controls.Count == 0 || !WordsFit.TypeAndBoxesAgree(AtDesignScale(16), AtDesignScale(96)))
		{
			return;
		}
		int num = 0;
		SuspendLayout();
		try
		{
			foreach (CardPanel item in (from c in Controls.OfType<CardPanel>()
				orderby c.Top
				select c).ToList())
			{
				int num2 = WordsFit.GrowWrappedLabels(item, (Label label) =>
				{
					(Label, Label, Control)? updateRow = _updateRow;
					if (updateRow.HasValue)
					{
						(Label, Label, Control) valueOrDefault = updateRow.GetValueOrDefault();
						if (label != valueOrDefault.Item1)
						{
							return label == valueOrDefault.Item2;
						}
						return true;
					}
					return false;
				});
				WordsFit.GrowNarrowButtons(item, AtDesignScale(21));
				num2 += RestackMeasuredLabels(item);
				if (num2 != 0)
				{
					ReflowBelow(item, num2, refitScroll: false);
				}
				num += num2;
			}
			FitTheVerifyButtons();
			FitTheUpdateRow();
			if (num != 0)
			{
				FitScrollToContent(AtDesignScale(40));
			}
		}
		finally
		{
			ResumeLayout(performLayout: false);
		}
	}

	private int RestackMeasuredLabels(CardPanel card)
	{
		int height = card.Height;
		(CardPanel, Label, Label)? recordingLayout = _recordingLayout;
		if (recordingLayout.HasValue)
		{
			(CardPanel, Label, Label) valueOrDefault = recordingLayout.GetValueOrDefault();
			if (card == valueOrDefault.Item1)
			{
				valueOrDefault.Item3.Top = valueOrDefault.Item2.Bottom + AtDesignScale(8);
				card.Height = card.Controls.Cast<Control>().Max((Control c) => c.Bottom) + AtDesignScale(5);
			}
		}
		(CardPanel, Label, GlassButton, Label)? factsLayout = _factsLayout;
		if (factsLayout.HasValue)
		{
			(CardPanel, Label, GlassButton, Label) valueOrDefault2 = factsLayout.GetValueOrDefault();
			if (card == valueOrDefault2.Item1)
			{
				Label item = valueOrDefault2.Item2;
				if (item != null)
				{
					GlassButton item2 = valueOrDefault2.Item3;
					if (item2 != null)
					{
						Label item3 = valueOrDefault2.Item4;
						if (item3 != null)
						{
							item2.Top = item.Bottom + AtDesignScale(12);
							item3.Top = item2.Top + AtDesignScale(6);
						}
					}
				}
				card.Height = card.Controls.Cast<Control>().Max((Control c) => c.Bottom) + AtDesignScale(21);
			}
		}
		return card.Height - height;
	}

	private void ShowCopyHashFeedback(GlassButton button)
	{
		if (_copyHashTimer == null)
		{
			_copyHashTimer = new Timer
			{
				Interval = 2000
			};
			_copyHashTimer.Tick += (object? _, EventArgs _) =>
			{
				_copyHashTimer.Stop();
				_copyHashTimer.Dispose();
				_copyHashTimer = null;
				button.Text = "Copy";
			};
		}
		_copyHashTimer.Stop();
		_copyHashTimer.Start();
	}

	private void FitTheVerifyButtons()
	{
		(GlassButton, GlassButton)? verifyButtons = _verifyButtons;
		if (verifyButtons.HasValue)
		{
			(GlassButton, GlassButton) valueOrDefault = verifyButtons.GetValueOrDefault();
			(GlassButton, GlassButton) tuple = valueOrDefault;
			GlassButton item = tuple.Item1;
			GlassButton item2 = tuple.Item2;
			string text = item.Text;
			item.Text = "Copied";
			int width = item.GetPreferredSize(Size.Empty).Width;
			item.Text = "Couldn't copy";
			int width2 = item.GetPreferredSize(Size.Empty).Width;
			item.Text = text;
			item2.Width = item2.GetPreferredSize(Size.Empty).Width;
			int val = item.Parent.Width - item.Left - AtDesignScale(21) - AtDesignScale(8) - item2.Width;
			item.Width = Math.Max(width, Math.Min(width2, val));
			item2.Left = item.Right + AtDesignScale(8);
		}
	}

	private void FitTheUpdateRow()
	{
		(Label, Label, Control)? updateRow = _updateRow;
		if (updateRow.HasValue)
		{
			(Label, Label, Control) valueOrDefault = updateRow.GetValueOrDefault();
			(Label, Label, Control) tuple = valueOrDefault;
			Label item = tuple.Item1;
			Label item2 = tuple.Item2;
			Control item3 = tuple.Item3;
			item.MinimumSize = new Size(0, item2.Height);
			item.AutoSize = true;
			Size preferredSize = item.GetPreferredSize(Size.Empty);
			item.Size = new Size(preferredSize.Width, Math.Max(preferredSize.Height, item2.Height));
			item.Top = item2.Top + (item2.Height - item.Height) / 2;
			int num = item.Right + AtDesignScale(4);
			int num2 = ((item3 != null) ? (item3.Left - AtDesignScale(12)) : (item2.Parent.Width - AtDesignScale(21)));
			item2.SetBounds(num, item2.Top, Math.Max(AtDesignScale(40), num2 - num), item2.Height);
		}
	}

	private string? SafelyReadInstallerHash()
	{
		try
		{
			return _installerHash();
		}
		catch
		{
			return null;
		}
	}

	internal static string GroupedAndElidedHash(string hash)
	{
		List<string> list = new List<string>();
		for (int i = 0; i + 4 <= hash.Length; i += 4)
		{
			list.Add(hash.Substring(i, 4));
		}
		if (hash.Length % 4 != 0)
		{
			list.Add(hash.Substring(list.Count * 4));
		}
		if (list.Count <= 6)
		{
			return string.Join(" ", list);
		}
		string text = "…";
		return string.Join(" ", list.Take(4)) + " " + text + " " + string.Join(" ", list.Skip(list.Count - 2));
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		base.OnVisibleChanged(e);
		if (Visible)
		{
			_sayWhatWindowsRefused?.Invoke();
		}
		SyncAudioMeter();
	}

	private void SyncAudioMeter()
	{
		if (_audioMeterTimer == null)
		{
			return;
		}
		FollowTheMeterHost();
		if (!IsDisposed && Parent != null && Visible)
		{
			Form meterHost = _meterHost;
			if (meterHost != null && meterHost.Visible && meterHost.WindowState != FormWindowState.Minimized && _meterHostInFront)
			{
				_audioMeterTimer.Start();
				return;
			}
		}
		_audioMeterTimer.Stop();
	}

	private void FollowTheMeterHost()
	{
		Form form = (IsDisposed ? null : FindForm());
		if (form != _meterHost)
		{
			LetGoOfTheMeterHost();
			_meterHost = form;
			_meterHostInFront = true;
			if (form != null)
			{
				form.Activated += OnMeterHostActivated;
				form.Deactivate += OnMeterHostDeactivated;
				form.VisibleChanged += OnMeterHostChanged;
				form.Resize += OnMeterHostChanged;
			}
		}
	}

	private void LetGoOfTheMeterHost()
	{
		if (_meterHost != null)
		{
			_meterHost.Activated -= OnMeterHostActivated;
			_meterHost.Deactivate -= OnMeterHostDeactivated;
			_meterHost.VisibleChanged -= OnMeterHostChanged;
			_meterHost.Resize -= OnMeterHostChanged;
			_meterHost = null;
		}
	}

	private void OnMeterHostActivated(object? sender, EventArgs e)
	{
		_meterHostInFront = true;
		SyncAudioMeter();
	}

	private void OnMeterHostDeactivated(object? sender, EventArgs e)
	{
		_meterHostInFront = false;
		SyncAudioMeter();
	}

	private void OnMeterHostChanged(object? sender, EventArgs e)
	{
		SyncAudioMeter();
	}

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		if (Parent != null)
		{
			_sayWhatWindowsRefused?.Invoke();
		}
		SyncAudioMeter();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Control[] competitiveControls = _competitiveControls;
			for (int i = 0; i < competitiveControls.Length; i++)
			{
				competitiveControls[i].Dispose();
			}
		}
		if (disposing)
		{
			LetGoOfTheMeterHost();
			_audioMeterTimer?.Stop();
			_audioMeterTimer?.Dispose();
			_audioMeterTimer = null;
			_copyHashTimer?.Stop();
			_copyHashTimer?.Dispose();
			_copyHashTimer = null;
			_audioSave?.Dispose();
			_audioSave = null;
		}
		base.Dispose(disposing);
	}

	internal static GlassButton FlatButton(string text, int x, int y, int width)
	{
		return new GlassButton
		{
			Text = text,
			Kind = GlassButtonKind.Secondary,
			Size = new Size(width, 32),
			Location = new Point(x, y)
		};
	}

	internal static GlassButton PrimaryButton(string text, int x, int y, int width, int height = 32)
	{
		return new GlassButton
		{
			Text = text,
			Kind = GlassButtonKind.Primary,
			Size = new Size(width, height),
			Location = new Point(x, y)
		};
	}

	internal static void RestylePrimary(GlassButton b)
	{
		b.Invalidate();
	}
}
