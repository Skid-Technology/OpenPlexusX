using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Monitors;

namespace VibranceHud.Pages;

public sealed class MonitorHardwarePage : GlowPage
{
	private sealed class PanelState
	{
		public int PendingBrightness = -1;

		public int PendingContrast = -1;

		public int PendingBlue = -1;

		public int PendingRed = -1;

		public int PendingGreen = -1;

		public string Key = "";

		public bool BlueUntrusted;

		public GlassButton? Revert;

		public FlatSlider? BrightnessSlider;

		public FlatSlider? ContrastSlider;

		public FlatSlider? BlueSlider;

		public FlatSlider? RedSlider;

		public FlatSlider? GreenSlider;

		public Label? BrightnessReadout;

		public Label? ContrastReadout;

		public Label? BlueReadout;

		public Label? RedReadout;

		public Label? GreenReadout;

		public bool Reverting;

		public required MonitorCapability Cap { get; init; }

		public required DebouncedAction Write { get; init; }
	}

	private const int PageLeft = 16;

	private const int CardW = 932;

	private new const int Gutter = 21;

	private const int ContentW = 890;

	private const int ColGap = 40;

	private const int ColW = 425;

	private const int RightColX = 486;

	private const int RowPitch = 54;

	private const int TrackDrop = 20;

	private const int FirstCardY = 116;

	private const int CardGap = 16;

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly IReadOnlyList<MonitorCapability> _caps;

	private readonly List<PanelState> _panels = new List<PanelState>();

	private readonly List<(int MonitorIndex, GlassDropdown Picker, IReadOnlyList<byte> Values)> _inputPickers = new List<(int, GlassDropdown, IReadOnlyList<byte>)>();

	private readonly IMonitorWriter _writer;

	private readonly SignalToolTip _tips = new SignalToolTip
	{
		ShowAlways = true
	};

	private readonly Action? _onColourChanged;

	private readonly Func<int, string?> _deviceNameFor;

	private IReadOnlyList<MonitorCapability> _liveCaps = Array.Empty<MonitorCapability>();

	private IReadOnlyList<string> _connectedKeys = Array.Empty<string>();

	private readonly Func<IReadOnlyList<MonitorCapability>> _probe;

	internal const int MaxLowBlueStrength = 60;

	private bool _probeStarted;

	private readonly bool _probeOnShow;

	private readonly List<DebouncedAction> _colourSaves = new List<DebouncedAction>();

	internal const string NothingToUndo = "Nothing to undo";

	internal const string NothingToUndoTip = "PlexusX has nothing saved to put back on this monitor. If it still looks wrong, use the monitor's own menu.";

	internal Func<string, bool>? ConfirmForTest { get; set; }

	internal IReadOnlyList<MonitorCapability> Capabilities => _caps;

	internal bool OffersAnyHardwareControl => _caps.Any((MonitorCapability monitorCapability) => monitorCapability.SupportsBrightness || monitorCapability.SupportsContrast || monitorCapability.SupportsRgbGain);

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
			Size = new Size(TextRenderer.MeasureText(text, Fonts.BodyBold).Width + 8, 20),
			TextAlign = ContentAlignment.TopLeft,
			AutoSize = false,
			UseMnemonic = false
		};
	}

	private static Label Paragraph(string text, int x, int y, int width)
	{
		float num = (float)Tokens.Dpi / 96f;
		Font body = Fonts.Body;
		int val = (int)Math.Round((float)width * num) - 2 * GlyphPad(body) - 2;
		int height = (int)Math.Ceiling((float)TextRenderer.MeasureText(text, body, new Size(Math.Max(20, val), int.MaxValue), TextFormatFlags.WordBreak).Height / num) + 4;
		return new Label
		{
			Text = text,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(Ink(x, Fonts.Body), y),
			Size = new Size(width, height),
			UseMnemonic = false
		};
	}

	private static string InputName(byte value)
	{
		return value switch
		{
			1 => "VGA 1", 
			3 => "DVI 1", 
			15 => "DisplayPort 1", 
			16 => "DisplayPort 2", 
			17 => "HDMI 1", 
			18 => "HDMI 2", 
			27 => "USB-C", 
			_ => $"Input 0x{value:X2}", 
		};
	}

	private bool MayWriteToThePanel()
	{
		return true;
	}

	public MonitorHardwarePage(AppSettings settings, SettingsStore store, IReadOnlyList<MonitorCapability>? capabilities = null, IMonitorWriter? writer = null, Action? onColourChanged = null, Func<int, string?>? deviceNameResolver = null, Func<IReadOnlyList<MonitorCapability>>? probe = null)
	{
		_probe = probe ?? ((capabilities != null) ? ((Func<IReadOnlyList<MonitorCapability>>)(() => capabilities)) : new Func<IReadOnlyList<MonitorCapability>>(MonitorProbe.Probe));
		_settings = settings;
		_store = store;
		_caps = capabilities ?? Array.Empty<MonitorCapability>();
		_probeOnShow = capabilities == null;
		_writer = writer ?? new Dxva2MonitorWriter();
		_onColourChanged = onColourChanged;
		_deviceNameFor = deviceNameResolver ?? new Func<int, string>(MonitorDevices.DeviceNameForIndex);
		AutoScroll = true;
		Label title = new Label
		{
			Text = "Monitor",
			Font = Fonts.Display,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			AutoSize = false,
			UseMnemonic = false
		};
		Label subtitle = new Label
		{
			Text = "Your monitor's own settings, without reaching round the back.",
			Font = Fonts.Label,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.TopLeft,
			AutoSize = false,
			UseMnemonic = false
		};
		UseHeaderPanel(title, subtitle);
		_liveCaps = _caps;
		_connectedKeys = MonitorPanicRestore.KeysFor(_caps ?? Array.Empty<MonitorCapability>(), _deviceNameFor);
		foreach (Control item in BuildCards(_caps, "Asking your monitor what it supports…"))
		{
			Controls.Add(item);
		}
		FitScrollToContent();
	}

	internal static bool StackColumns(int clientWidthDevice, float designScale)
	{
		if (clientWidthDevice > 0)
		{
			return clientWidthDevice < (int)Math.Round(948f * designScale);
		}
		return false;
	}

	private List<Control> BuildCards(IReadOnlyList<MonitorCapability> caps, string emptyMessage)
	{
		List<Control> list = new List<Control>();
		bool stack = StackColumns(Width, DesignScale);
		int num = 116;
		foreach (MonitorCapability cap in caps)
		{
			Control control = BuildCardSafe(cap, num, stack);
			num += control.Height + 16;
			ScaleToCurrentDesign(control);
			list.Add(control);
		}
		if (caps.Count == 0)
		{
			CardPanel cardPanel = new CardPanel
			{
				Location = new Point(16, 116),
				Size = new Size(932, 84)
			};
			cardPanel.Controls.Add(Paragraph(emptyMessage, 21, 27, 890));
			ScaleToCurrentDesign(cardPanel);
			list.Add(cardPanel);
		}
		return list;
	}

	private Control BuildCardSafe(MonitorCapability cap, int y, bool stack)
	{
		int count = _panels.Count;
		int count2 = _colourSaves.Count;
		int count3 = _inputPickers.Count;
		try
		{
			return BuildMonitorCard(cap, y, stack);
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
			while (_panels.Count > count)
			{
				List<PanelState> panels = _panels;
				panels[panels.Count - 1].Write.Dispose();
				_panels.RemoveAt(_panels.Count - 1);
			}
			while (_colourSaves.Count > count2)
			{
				List<DebouncedAction> colourSaves = _colourSaves;
				colourSaves[colourSaves.Count - 1].Dispose();
				_colourSaves.RemoveAt(_colourSaves.Count - 1);
			}
			while (_inputPickers.Count > count3)
			{
				_inputPickers.RemoveAt(_inputPickers.Count - 1);
			}
			string text = "This monitor";
			int index = 0;
			try
			{
				index = cap.Index;
			}
			catch
			{
			}
			try
			{
				if (!string.IsNullOrWhiteSpace(cap.Description))
				{
					text = cap.Description;
				}
			}
			catch
			{
			}
			CardPanel cardPanel = new CardPanel
			{
				Location = new Point(16, y),
				Size = new Size(932, 84),
				Controls = { (Control?)Paragraph(text + " could not be shown.", 21, 27, 700) }
			};
			GlassButton glassButton = new GlassButton
			{
				Text = "Restore my monitor",
				Kind = GlassButtonKind.Secondary,
				Location = new Point(743, 16),
				Size = new Size(168, 28)
			};
			glassButton.Click += (object? _, EventArgs _) =>
			{
				RestoreOneMonitor(index, null, useBackgroundThread: true);
			};
			cardPanel.Controls.Add(glassButton);
			return cardPanel;
		}
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		if (!_probeStarted && _probeOnShow && _caps.Count <= 0)
		{
			_probeStarted = true;
			BeginProbe();
		}
	}

	private IReadOnlyList<MonitorCapability> ProbeSafely()
	{
		try
		{
			return _probe() ?? Array.Empty<MonitorCapability>();
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
			return Array.Empty<MonitorCapability>();
		}
	}

	internal void ProbeAndRebuildForTest()
	{
		Rebuild(ProbeSafely());
	}

	public void BeginProbe()
	{
		Task.Run(() =>
		{
			IReadOnlyList<MonitorCapability> caps = ProbeSafely();
			try
			{
				if (!IsDisposed && IsHandleCreated)
				{
					BeginInvoke(() =>
					{
						Rebuild(caps);
					});
				}
			}
			catch (ObjectDisposedException)
			{
			}
			catch (InvalidOperationException)
			{
			}
		});
	}

	private void Rebuild(IReadOnlyList<MonitorCapability> caps)
	{
		if (IsDisposed)
		{
			return;
		}
		foreach (PanelState panel in _panels)
		{
			panel.Write.Dispose();
		}
		_panels.Clear();
		foreach (DebouncedAction colourSafe in _colourSaves)
		{
			colourSafe.Dispose();
		}
		_colourSaves.Clear();
		_inputPickers.Clear();
		_tips.RemoveAll();
		_liveCaps = caps;
		_connectedKeys = MonitorPanicRestore.KeysFor(caps, _deviceNameFor);
		List<Control> list = BuildCards(caps, "No monitors answered - your panel may not support DDC/CI, or the cable doesn't carry it.");
		SuspendLayout();
		try
		{
			foreach (Control item in (from Control control in Controls
				where !(control is HeaderPanel)
				select control).ToList())
			{
				Controls.Remove(item);
				item.Dispose();
			}
			foreach (Control item2 in list)
			{
				Controls.Add(item2);
			}
		}
		finally
		{
			ResumeLayout(performLayout: true);
		}
		FitScrollToContent();
	}

	private Control BuildMonitorCard(MonitorCapability cap, int y, bool stack)
	{
		bool flag = cap.SupportsBrightness || cap.SupportsContrast || cap.SupportsRgbGain || cap.SupportsRedGain || cap.SupportsGreenGain || cap.SupportsInputSource;
		CardPanel cardPanel = new CardPanel
		{
			Location = new Point(16, y),
			Size = new Size(932, flag ? 300 : 132)
		};
		Label label = new Label
		{
			Text = cap.Description,
			Font = Fonts.BodyBold,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Location = new Point(Ink(21, Fonts.BodyBold), 21),
			Size = new Size(flag ? 700 : 750, 20),
			UseMnemonic = false,
			AutoEllipsis = true
		};
		_tips.SetToolTip(label, cap.Description ?? "");
		cardPanel.Controls.Add(label);
		if (!flag)
		{
			Label label2 = Paragraph(cap.Refusal, 21, 51, 425);
			cardPanel.Controls.Add(label2);
			int colX = (stack ? 21 : 486);
			int rowY = (stack ? (51 + label2.Height + 21) : 51);
			int val = AddSoftwareColourRows(cardPanel, cap.Index, colX, rowY, 425);
			cardPanel.Height = Math.Max(val, 51 + label2.Height) + 21;
			return cardPanel;
		}
		string key = MonitorPanicRestore.KeyFor(cap, _deviceNameFor);
		PanelState state = new PanelState
		{
			Cap = cap,
			Key = key,
			Write = null
		};
		DebouncedAction write = new DebouncedAction(() =>
		{
			Flush(state);
		}, 120);
		state = new PanelState
		{
			Cap = cap,
			Key = key,
			Write = write
		};
		_panels.Add(state);
		AppSettings.PanelSettings panelSettings = _settings.PanelFor(key, cap.Index, _connectedKeys);
		state.Revert = new GlassButton
		{
			Kind = GlassButtonKind.Secondary,
			Location = new Point(743, 16),
			Size = new Size(168, 28)
		};
		RefreshRevertButton(state, panelSettings);
		state.Revert.Click += (object? _, EventArgs _) =>
		{
			OnRevertClicked(state, useBackgroundThread: true);
		};
		cardPanel.Controls.Add(state.Revert);
		int num = 51;
		if (cap.SupportsBrightness)
		{
			int num3;
			if (panelSettings.Brightness < 0)
			{
				int? num2 = TrustedBrightness(cap);
				if (num2.HasValue)
				{
					int valueOrDefault = num2.GetValueOrDefault();
					num3 = cap.Brightness.ToPercent(valueOrDefault);
				}
				else
				{
					num3 = 50;
				}
			}
			else
			{
				num3 = panelSettings.Brightness;
			}
			int value = num3;
			num = AddRow(cardPanel, "Brightness", 21, num, 425, value, (int v) =>
			{
				if (!state.Reverting)
				{
					Interlocked.Exchange(ref state.PendingBrightness, v);
					write.Trigger();
				}
			}, out state.BrightnessSlider, out state.BrightnessReadout);
		}
		if (cap.SupportsContrast)
		{
			int value2 = ((panelSettings.Contrast >= 0) ? panelSettings.Contrast : cap.Contrast.ToPercent(cap.Contrast.Current));
			num = AddRow(cardPanel, "Contrast", 21, num, 425, value2, (int v) =>
			{
				if (!state.Reverting)
				{
					Interlocked.Exchange(ref state.PendingContrast, v);
					write.Trigger();
				}
			}, out state.ContrastSlider, out state.ContrastReadout);
		}
		if (cap.SupportsRgbGain)
		{
			num = AddRow(cardPanel, "Low blue light", 21, num, 425, Math.Clamp(panelSettings.LowBlue, 0, 60), (int v) =>
			{
				if (!state.Reverting)
				{
					Interlocked.Exchange(ref state.PendingBlue, v);
					write.Trigger();
				}
			}, out state.BlueSlider, out state.BlueReadout);
			state.BlueSlider.Maximum = 60;
			state.BlueUntrusted = ((!panelSettings.HasOriginals) ? (!panelSettings.PanelTouched && MonitorPanicRestore.BlueLooksAlreadyCut(cap)) : (panelSettings.OriginalBlueGain < 0 && MonitorPanicRestore.BlueLooksAlreadyCut(cap)));
			RefreshRevertButton(state, panelSettings);
			Label label3 = Paragraph(state.BlueUntrusted ? "Your monitor's blue already looks reduced - press Restore my monitor first." : "Warmer for late sessions. Cuts blue light on any monitor.", 21, num, 425);
			cardPanel.Controls.Add(label3);
			num += label3.Height + 16;
		}
		if (cap.SupportsRedGain)
		{
			int value3 = ((panelSettings.RedGain >= 0) ? panelSettings.RedGain : cap.RedGain.ToPercent(cap.RedGain.Current));
			num = AddRow(cardPanel, "Red gain", 21, num, 425, value3, (int v) =>
			{
				if (!state.Reverting)
				{
					Interlocked.Exchange(ref state.PendingRed, v);
					write.Trigger();
				}
			}, out state.RedSlider, out state.RedReadout);
		}
		if (cap.SupportsGreenGain)
		{
			int value4 = ((panelSettings.GreenGain >= 0) ? panelSettings.GreenGain : cap.GreenGain.ToPercent(cap.GreenGain.Current));
			num = AddRow(cardPanel, "Green gain", 21, num, 425, value4, (int v) =>
			{
				if (!state.Reverting)
				{
					Interlocked.Exchange(ref state.PendingGreen, v);
					write.Trigger();
				}
			}, out state.GreenSlider, out state.GreenReadout);
		}
		if (cap.SupportsInputSource)
		{
			cardPanel.Controls.Add(SectionHeading("Input source", 21, num));
			GlassDropdown picker = new GlassDropdown
			{
				Location = new Point(21, num + 26),
				Size = new Size(220, 32)
			};
			IReadOnlyList<byte> values = cap.InputSourceValues;
			picker.SetItems(values.Select((byte v) => (!InputName(v).StartsWith("Input ", StringComparison.Ordinal)) ? $"{InputName(v)} (0x{v:X2})" : InputName(v)));
			byte? currentInputSource = cap.CurrentInputSource;
			int num4;
			if (currentInputSource.HasValue)
			{
				byte valueOrDefault2 = currentInputSource.GetValueOrDefault();
				num4 = values.ToList().IndexOf(valueOrDefault2);
			}
			else
			{
				num4 = -1;
			}
			int num5 = num4;
			if (num5 >= 0)
			{
				picker.SelectedIndex = num5;
			}
			int monitorIndex = cap.Index;
			int previousIndex = picker.SelectedIndex;
			picker.SelectedIndexChanged += (object? _, EventArgs _) =>
			{
				if (picker.SelectedIndex >= 0 && picker.SelectedIndex < values.Count && picker.SelectedIndex != previousIndex)
				{
					string text = InputName(values[picker.SelectedIndex]);
					string text2 = "Your screen will switch to " + text + ". Use your monitor's own buttons to come back.";
					if (!(ConfirmForTest?.Invoke(text2) ?? (GlassDialog.Show(FindForm(), "Switch input source", text2, GlassDialogButtons.ConfirmSafeEnter, GlassDialogTone.Warning, "Switch input") == DialogResult.Yes)))
					{
						picker.SelectedIndex = previousIndex;
					}
					else
					{
						previousIndex = picker.SelectedIndex;
						WriteInputSource(monitorIndex, picker, values[picker.SelectedIndex], useBackgroundThread: true);
					}
				}
			};
			cardPanel.Controls.Add(picker);
			_inputPickers.Add((monitorIndex, picker, values));
			Label label4 = Paragraph("These are the usual names. If one looks wrong, check your monitor's own menu.", 21, num + 70, 425);
			cardPanel.Controls.Add(label4);
			num += 70 + label4.Height + 16;
		}
		int val2 = (stack ? AddSoftwareColourRows(cardPanel, cap.Index, 21, num, 425) : AddSoftwareColourRows(cardPanel, cap.Index, 486, 51, 425));
		cardPanel.Height = Math.Max(num, val2) + 21;
		return cardPanel;
	}

	private int AddSoftwareColourRows(CardPanel card, int monitorIndex, int colX, int rowY, int colW)
	{
		string device = _deviceNameFor(monitorIndex);
		if (device == null)
		{
			return rowY;
		}
		DebouncedAction save = new DebouncedAction(() =>
		{
			_store.Save(_settings);
		}, 500);
		_colourSaves.Add(save);
		card.Controls.Add(SectionHeading("This monitor's colour", colX, rowY));
		GlassButton glassButton = new GlassButton
		{
			Text = "Reset",
			Kind = GlassButtonKind.Secondary,
			Location = new Point(colX + colW - 66, rowY + 22 - 28),
			Size = new Size(66, 28)
		};
		card.Controls.Add(glassButton);
		Label label = Paragraph("PlexusX adjusts this screen itself. Works on every monitor.", colX, rowY + 22, colW);
		card.Controls.Add(label);
		rowY += 22 + label.Height + 16;
		MonitorColourSetting monitorColourSetting = Row();
		card.Controls.Add(RowLabel("Gamma", colX, rowY));
		Label gammaReadout = new Label
		{
			Text = $"{monitorColourSetting.GammaPercent}%",
			Font = Fonts.BodyBold,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Location = new Point(colX + colW - 60, rowY),
			Size = new Size(60 + GlyphPad(Fonts.BodyBold), 20),
			TextAlign = ContentAlignment.TopRight,
			UseMnemonic = false
		};
		card.Controls.Add(gammaReadout);
		FlatSlider gamma = new FlatSlider
		{
			AccessibleName = "Gamma",
			Minimum = 50,
			Maximum = 150,
			Value = Math.Clamp(monitorColourSetting.GammaPercent, 50, 150)
		};
		gamma.SetTrackBounds(colX, rowY + 20, colW);
		gamma.ValueChanged += (object? _, EventArgs _) =>
		{
			gammaReadout.Text = $"{gamma.Value}%";
			Row().GammaPercent = gamma.Value;
			_onColourChanged?.Invoke();
			save.Trigger();
		};
		card.Controls.Add(gamma);
		rowY += 54;
		rowY = AddRow(card, "Warmth", colX, rowY, colW, Math.Clamp(monitorColourSetting.WarmthPercent, 0, 100), (int v) =>
		{
			Row().WarmthPercent = v;
			_onColourChanged?.Invoke();
			save.Trigger();
		}, out FlatSlider warmth, out Label _);
		glassButton.Click += (object? _, EventArgs _) =>
		{
			gamma.Value = 100;
			warmth.Value = 0;
		};
		return rowY;
		MonitorColourSetting Row()
		{
			MonitorColourSetting monitorColourSetting2 = _settings.MonitorColours.Find((MonitorColourSetting m) => m.DeviceName == device);
			if (monitorColourSetting2 == null)
			{
				monitorColourSetting2 = new MonitorColourSetting
				{
					DeviceName = device
				};
				_settings.MonitorColours.Add(monitorColourSetting2);
			}
			return monitorColourSetting2;
		}
	}

	private int AddRow(Control parent, string caption, int x, int y, int width, int value, Action<int> apply, out FlatSlider slider, out Label readout)
	{
		parent.Controls.Add(RowLabel(caption, x, y));
		readout = new Label
		{
			Text = $"{value}%",
			Font = Fonts.BodyBold,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Location = new Point(x + width - 60, y),
			Size = new Size(60 + GlyphPad(Fonts.BodyBold), 20),
			TextAlign = ContentAlignment.TopRight,
			UseMnemonic = false
		};
		parent.Controls.Add(readout);
		FlatSlider flatSlider = new FlatSlider
		{
			AccessibleName = caption,
			Minimum = 0,
			Maximum = 100,
			Value = Math.Clamp(value, 0, 100)
		};
		FlatSlider flatSlider2 = flatSlider;
		slider = flatSlider;
		FlatSlider s = flatSlider2;
		Label r = readout;
		slider.SetTrackBounds(x, y + 20, width);
		slider.ValueChanged += (object? _, EventArgs _) =>
		{
			r.Text = $"{s.Value}%";
			apply(s.Value);
		};
		parent.Controls.Add(slider);
		return y + 54;
	}

	private static Label RowLabel(string text, int x, int y)
	{
		return new Label
		{
			Text = text,
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			Font = Fonts.Body,
			Location = new Point(Ink(x, Fonts.Body), y),
			Size = new Size(TextRenderer.MeasureText(text, Fonts.Body).Width + 8, 20),
			TextAlign = ContentAlignment.TopLeft,
			AutoSize = false,
			UseMnemonic = false
		};
	}

	private void Flush(PanelState state)
	{
		try
		{
			MonitorCapability cap = state.Cap;
			List<Action<AppSettings.PanelSettings>> accepted = new List<Action<AppSettings.PanelSettings>>();
			bool flag = MayWriteToThePanel();
			int brightness = Interlocked.Exchange(ref state.PendingBrightness, -1);
			if ((brightness >= 0) & flag)
			{
				CaptureOriginals(state);
				int wroteRaw = cap.Brightness.FromPercent(brightness);
				if (_writer.SetBrightnessPercent(cap.Index, cap.Brightness, brightness))
				{
					accepted.Add((AppSettings.PanelSettings p) =>
					{
						p.Brightness = brightness;
						p.WroteBrightness = wroteRaw;
					});
				}
			}
			int contrast = Interlocked.Exchange(ref state.PendingContrast, -1);
			if ((contrast >= 0) & flag)
			{
				CaptureOriginals(state);
				int wroteRaw2 = cap.Contrast.FromPercent(contrast);
				if (_writer.SetContrastPercent(cap.Index, cap.Contrast, contrast))
				{
					accepted.Add((AppSettings.PanelSettings p) =>
					{
						p.Contrast = contrast;
						p.WroteContrast = wroteRaw2;
					});
				}
			}
			int blue = Interlocked.Exchange(ref state.PendingBlue, -1);
			if (((blue >= 0) & flag) && !state.BlueUntrusted)
			{
				CaptureOriginals(state);
				AppSettings.PanelSettings panelSettings = PanelOf(state);
				PanelRange gain = LowBlueBase(cap.BlueGain, panelSettings.HasOriginals, panelSettings.OriginalBlueGain);
				int wroteRaw3 = Math.Clamp(MonitorControl.LowBlueTarget(gain, blue), gain.Min, gain.Max);
				if (_writer.SetLowBlueLight(cap.Index, gain, blue))
				{
					accepted.Add((AppSettings.PanelSettings p) =>
					{
						p.LowBlue = blue;
						p.WroteBlueGain = wroteRaw3;
					});
				}
			}
			int red = Interlocked.Exchange(ref state.PendingRed, -1);
			if ((red >= 0) & flag)
			{
				CaptureOriginals(state);
				int wroteRaw4 = cap.RedGain.FromPercent(red);
				if (_writer.SetRedGainPercent(cap.Index, cap.RedGain, red))
				{
					accepted.Add((AppSettings.PanelSettings p) =>
					{
						p.RedGain = red;
						p.WroteRedGain = wroteRaw4;
					});
				}
			}
			int green = Interlocked.Exchange(ref state.PendingGreen, -1);
			if ((green >= 0) & flag)
			{
				CaptureOriginals(state);
				int wroteRaw5 = cap.GreenGain.FromPercent(green);
				if (_writer.SetGreenGainPercent(cap.Index, cap.GreenGain, green))
				{
					accepted.Add((AppSettings.PanelSettings p) =>
					{
						p.GreenGain = green;
						p.WroteGreenGain = wroteRaw5;
					});
				}
			}
			if (accepted.Count == 0)
			{
				return;
			}
			Marshal(() =>
			{
				AppSettings.PanelSettings panelSettings2 = PanelOf(state);
				foreach (Action<AppSettings.PanelSettings> item in accepted)
				{
					item(panelSettings2);
				}
				if (panelSettings2.HasOriginals)
				{
					panelSettings2.PanelTouched = true;
				}
				_store.Save(_settings);
				if (state.Revert != null)
				{
					RefreshRevertButton(state, panelSettings2);
				}
			});
		}
		catch
		{
		}
	}

	private static int? TrustedBrightness(MonitorCapability cap)
	{
		if (cap.BrightnessCurrent <= cap.Brightness.Min)
		{
			return null;
		}
		return cap.BrightnessCurrent;
	}

	internal static PanelRange LowBlueBase(PanelRange probed, bool hasOriginals, int original)
	{
		if (!hasOriginals || original < 0)
		{
			return probed;
		}
		return probed with
		{
			Current = Math.Clamp(original, probed.Min, probed.Max)
		};
	}

	private void CaptureOriginals(PanelState state)
	{
		MonitorCapability cap = state.Cap;
		AppSettings.PanelSettings panelSettings = PanelOf(state);
		if (panelSettings.HasOriginals)
		{
			if (panelSettings.OriginalBlueGain >= 0 || state.BlueUntrusted)
			{
				return;
			}
			Marshal(() =>
			{
				AppSettings.PanelSettings panelSettings2 = PanelOf(state);
				if (panelSettings2.HasOriginals && panelSettings2.OriginalBlueGain < 0)
				{
					panelSettings2.OriginalBlueGain = cap.BlueGain.Current;
				}
			});
			return;
		}
		int? brightness = (cap.SupportsBrightness ? _writer.ReadTrustedBrightness(cap.Index) : ((int?)null));
		Marshal(() =>
		{
			AppSettings.PanelSettings panelSettings2 = PanelOf(state);
			if (!panelSettings2.HasOriginals)
			{
				panelSettings2.OriginalBrightness = brightness ?? TrustedBrightness(cap) ?? (-1);
				panelSettings2.OriginalContrast = cap.Contrast.Current;
				panelSettings2.OriginalBlueGain = (state.BlueUntrusted ? (-1) : cap.BlueGain.Current);
				panelSettings2.OriginalRedGain = cap.RedGain.Current;
				panelSettings2.OriginalGreenGain = cap.GreenGain.Current;
				panelSettings2.HasOriginals = true;
			}
		});
	}

	private void WriteInputSource(int monitorIndex, GlassDropdown picker, byte value, bool useBackgroundThread)
	{
		picker.Enabled = false;
		if (useBackgroundThread)
		{
			Task.Run((Action)Write);
		}
		else
		{
			Write();
		}
		void Write()
		{
			try
			{
				_writer.SetInputSource(monitorIndex, value);
			}
			catch
			{
			}
			finally
			{
				Marshal(() =>
				{
					picker.Enabled = true;
				});
			}
		}
	}

	internal void SetInputSourceForTest(int monitorIndex, byte value)
	{
		(int, GlassDropdown, IReadOnlyList<byte>) tuple = _inputPickers.FirstOrDefault(((int MonitorIndex, GlassDropdown Picker, IReadOnlyList<byte> Values) p) => p.MonitorIndex == monitorIndex);
		if (tuple.Item2 != null)
		{
			WriteInputSource(monitorIndex, tuple.Item2, value, useBackgroundThread: false);
		}
	}

	private void Revert(PanelState state)
	{
		Revert(state, useBackgroundThread: true);
	}

	internal void RevertForTest(int panelIndex = 0)
	{
		PanelState panelState = _panels.ElementAtOrDefault(panelIndex);
		if (panelState != null)
		{
			Revert(panelState, useBackgroundThread: false);
		}
	}

	private void Revert(PanelState state, bool useBackgroundThread)
	{
		if (state.Reverting)
		{
			return;
		}
		MonitorCapability cap = state.Cap;
		AppSettings.PanelSettings panelSettings = PanelOf(state);
		int? brightness;
		int? contrast;
		int? blueGain;
		int? redGain;
		int? greenGain;
		if (panelSettings.HasOriginals)
		{
			state.Reverting = true;
			if (state.Revert != null)
			{
				state.Revert.Enabled = false;
				state.Revert.Text = "Restoring…";
			}
			brightness = ((cap.SupportsBrightness && panelSettings.OriginalBrightness > cap.Brightness.Min) ? new int?(panelSettings.OriginalBrightness) : ((int?)null));
			contrast = (cap.SupportsContrast ? new int?(panelSettings.OriginalContrast) : ((int?)null));
			blueGain = ((cap.SupportsRgbGain && panelSettings.OriginalBlueGain >= 0) ? new int?(panelSettings.OriginalBlueGain) : ((int?)null));
			redGain = (cap.SupportsRedGain ? new int?(panelSettings.OriginalRedGain) : ((int?)null));
			greenGain = (cap.SupportsGreenGain ? new int?(panelSettings.OriginalGreenGain) : ((int?)null));
			if (useBackgroundThread)
			{
				Task.Run((Action)Write);
			}
			else
			{
				Write();
			}
		}
		void Write()
		{
			try
			{
				(bool Brightness, bool Contrast, bool BlueGain, bool RedGain, bool GreenGain) result = _writer.RestoreAll(cap.Index, brightness, contrast, blueGain, redGain, greenGain);
				Marshal(() =>
				{
					ApplyRevertResult(state, cap, brightness, contrast, blueGain, redGain, greenGain, result);
				});
			}
			catch
			{
				Marshal(() =>
				{
					state.Reverting = false;
					if (state.Revert != null)
					{
						RefreshRevertButton(state, PanelOf(state));
					}
				});
			}
		}
	}

	private void ApplyRevertResult(PanelState state, MonitorCapability cap, int? brightness, int? contrast, int? blueGain, int? redGain, int? greenGain, (bool Brightness, bool Contrast, bool BlueGain, bool RedGain, bool GreenGain) result)
	{
		AppSettings.PanelSettings panelSettings = PanelOf(state);
		if (brightness.HasValue && result.Brightness)
		{
			panelSettings.Brightness = -1;
			if (state.BrightnessSlider != null && state.BrightnessReadout != null)
			{
				state.BrightnessSlider.Value = Math.Clamp(cap.Brightness.ToPercent(brightness.Value), 0, 100);
				state.BrightnessReadout.Text = $"{state.BrightnessSlider.Value}%";
			}
		}
		if (contrast.HasValue && result.Contrast)
		{
			panelSettings.Contrast = -1;
			if (state.ContrastSlider != null && state.ContrastReadout != null)
			{
				state.ContrastSlider.Value = Math.Clamp(cap.Contrast.ToPercent(contrast.Value), 0, 100);
				state.ContrastReadout.Text = $"{state.ContrastSlider.Value}%";
			}
		}
		if (blueGain.HasValue && result.BlueGain)
		{
			panelSettings.LowBlue = -1;
			if (state.BlueSlider != null && state.BlueReadout != null)
			{
				state.BlueSlider.Value = 0;
				state.BlueReadout.Text = "0%";
			}
		}
		if (redGain.HasValue && result.RedGain)
		{
			panelSettings.RedGain = -1;
			if (state.RedSlider != null && state.RedReadout != null)
			{
				state.RedSlider.Value = Math.Clamp(cap.RedGain.ToPercent(redGain.Value), 0, 100);
				state.RedReadout.Text = $"{state.RedSlider.Value}%";
			}
		}
		if (greenGain.HasValue && result.GreenGain)
		{
			panelSettings.GreenGain = -1;
			if (state.GreenSlider != null && state.GreenReadout != null)
			{
				state.GreenSlider.Value = Math.Clamp(cap.GreenGain.ToPercent(greenGain.Value), 0, 100);
				state.GreenReadout.Text = $"{state.GreenSlider.Value}%";
			}
		}
		if ((!brightness.HasValue || result.Brightness) && (!contrast.HasValue || result.Contrast) && (!blueGain.HasValue || result.BlueGain) && (!redGain.HasValue || result.RedGain) && (!greenGain.HasValue || result.GreenGain))
		{
			panelSettings.PanelTouched = false;
		}
		_store.Save(_settings);
		state.Reverting = false;
		if (state.Revert != null)
		{
			RefreshRevertButton(state, panelSettings);
		}
	}

	private AppSettings.PanelSettings PanelOf(PanelState state)
	{
		return _settings.PanelFor(state.Key, state.Cap.Index, _connectedKeys);
	}

	private static bool HasSomethingToPutBack(AppSettings.PanelSettings p)
	{
		if (!p.PanelTouched && p.Brightness < 0 && p.Contrast < 0 && p.LowBlue < 0 && p.RedGain < 0)
		{
			return p.GreenGain >= 0;
		}
		return true;
	}

	private static bool ShowsPutItBack(PanelState state, AppSettings.PanelSettings panel)
	{
		if (panel.HasOriginals)
		{
			return !state.BlueUntrusted;
		}
		return false;
	}

	private void OnRevertClicked(PanelState state, bool useBackgroundThread)
	{
		if (ShowsPutItBack(state, PanelOf(state)))
		{
			Revert(state, useBackgroundThread);
		}
		else
		{
			RestoreOneMonitor(state.Cap.Index, state, useBackgroundThread);
		}
	}

	private void RefreshRevertButton(PanelState state, AppSettings.PanelSettings panel)
	{
		if (state.Revert != null)
		{
			if (ShowsPutItBack(state, panel))
			{
				bool flag = HasSomethingToPutBack(panel);
				state.Revert.Text = "Put it back";
				state.Revert.Enabled = flag;
				_tips.SetToolTip(state.Revert, flag ? "" : "Nothing changed yet");
			}
			else
			{
				state.Revert.Text = "Restore my monitor";
				state.Revert.Enabled = true;
				_tips.SetToolTip(state.Revert, "Asks this monitor to go back to its own colour settings.");
			}
		}
	}

	private void RestoreOneMonitor(int monitorIndex, PanelState? state, bool useBackgroundThread)
	{
		(int Brightness, int Contrast, int Red, int Green) before = ((state == null) ? default((int, int, int, int)) : SavedFields(PanelOf(state)));
		if (state != null)
		{
			if (state.Reverting)
			{
				return;
			}
			state.Reverting = true;
			if (state.Revert != null)
			{
				state.Revert.Enabled = false;
				state.Revert.Text = "Restoring…";
			}
		}
		if (useBackgroundThread)
		{
			Task.Run((Action)Work);
		}
		else
		{
			Work();
		}
		void Work()
		{
			PanicResult result = null;
			try
			{
				result = MonitorPanicRestore.Run(_settings, _store, _writer, () => ProbeSafely(), () =>
				{
				}, () =>
				{
				}, _deviceNameFor, monitorIndex, OnUiThread, userInitiated: true);
			}
			catch
			{
			}
			Marshal(() =>
			{
				if (state != null)
				{
					if (state.BlueSlider != null && state.BlueReadout != null)
					{
						state.BlueSlider.Value = 0;
						state.BlueReadout.Text = "0%";
					}
					ReseedRestoredSliders(state, PanelOf(state), before);
					state.Reverting = false;
					RefreshRevertButton(state, PanelOf(state));
					if (result != null && MonitorPanicRestore.DidNothingToThePanels(result) && state.Revert != null)
					{
						state.Revert.Text = "Nothing to undo";
						_tips.SetToolTip(state.Revert, "PlexusX has nothing saved to put back on this monitor. If it still looks wrong, use the monitor's own menu.");
					}
				}
			});
		}
	}

	private static (int Brightness, int Contrast, int Red, int Green) SavedFields(AppSettings.PanelSettings p)
	{
		return (Brightness: p.Brightness, Contrast: p.Contrast, Red: p.RedGain, Green: p.GreenGain);
	}

	private static void ReseedRestoredSliders(PanelState state, AppSettings.PanelSettings p, (int Brightness, int Contrast, int Red, int Green) before)
	{
		if (p.HasOriginals)
		{
			MonitorCapability cap = state.Cap;
			if (before.Brightness >= 0 && p.Brightness < 0 && p.OriginalBrightness > cap.Brightness.Min && state.BrightnessSlider != null && state.BrightnessReadout != null)
			{
				state.BrightnessSlider.Value = Math.Clamp(cap.Brightness.ToPercent(p.OriginalBrightness), 0, 100);
				state.BrightnessReadout.Text = $"{state.BrightnessSlider.Value}%";
			}
			if (before.Contrast >= 0 && p.Contrast < 0 && p.OriginalContrast >= 0 && state.ContrastSlider != null && state.ContrastReadout != null)
			{
				state.ContrastSlider.Value = Math.Clamp(cap.Contrast.ToPercent(p.OriginalContrast), 0, 100);
				state.ContrastReadout.Text = $"{state.ContrastSlider.Value}%";
			}
			if (before.Red >= 0 && p.RedGain < 0 && p.OriginalRedGain >= 0 && state.RedSlider != null && state.RedReadout != null)
			{
				state.RedSlider.Value = Math.Clamp(cap.RedGain.ToPercent(p.OriginalRedGain), 0, 100);
				state.RedReadout.Text = $"{state.RedSlider.Value}%";
			}
			if (before.Green >= 0 && p.GreenGain < 0 && p.OriginalGreenGain >= 0 && state.GreenSlider != null && state.GreenReadout != null)
			{
				state.GreenSlider.Value = Math.Clamp(cap.GreenGain.ToPercent(p.OriginalGreenGain), 0, 100);
				state.GreenReadout.Text = $"{state.GreenSlider.Value}%";
			}
		}
	}

	internal void RestoreMyMonitorForTest(int panelIndex = 0)
	{
		PanelState panelState = _panels.ElementAtOrDefault(panelIndex);
		if (panelState != null)
		{
			RestoreOneMonitor(panelState.Cap.Index, panelState, useBackgroundThread: false);
		}
	}

	internal void ClickRevertButtonForTest(int panelIndex = 0)
	{
		PanelState panelState = _panels.ElementAtOrDefault(panelIndex);
		if (panelState != null)
		{
			OnRevertClicked(panelState, useBackgroundThread: false);
		}
	}

	internal string? RevertButtonTextForTest(int panelIndex = 0)
	{
		return _panels.ElementAtOrDefault(panelIndex)?.Revert?.Text;
	}

	internal bool? RevertButtonEnabledForTest(int panelIndex = 0)
	{
		return _panels.ElementAtOrDefault(panelIndex)?.Revert?.Enabled;
	}

	internal FlatSlider? BlueSliderForTest(int panelIndex = 0)
	{
		return _panels.ElementAtOrDefault(panelIndex)?.BlueSlider;
	}

	internal FlatSlider? BrightnessSliderForTest(int panelIndex = 0)
	{
		return _panels.ElementAtOrDefault(panelIndex)?.BrightnessSlider;
	}

	internal IReadOnlyList<string> LayoutProblemsForTest()
	{
		List<string> list = new List<string>();
		foreach (CardPanel item in Controls.OfType<CardPanel>())
		{
			List<Control> list2 = (from Control control2 in item.Controls
				where control2.Visible && control2.Width > 0 && control2.Height > 0
				select control2).ToList();
			for (int num = 0; num < list2.Count; num++)
			{
				Control control = list2[num];
				if (control.Right > item.ClientSize.Width + 2 || control.Bottom > item.ClientSize.Height + 2 || control.Left < -2 || control.Top < -2)
				{
					list.Add($"{Describe(control)} runs outside its card {item.Size}");
				}
				for (int num2 = num + 1; num2 < list2.Count; num2++)
				{
					Rectangle rectangle = Rectangle.Intersect(control.Bounds, list2[num2].Bounds);
					if (rectangle.Width > 2 && rectangle.Height > 2)
					{
						list.Add($"{Describe(control)} overlaps {Describe(list2[num2])} by {rectangle.Width}x{rectangle.Height}");
					}
				}
			}
		}
		return list;
	}

	private static string Describe(Control c)
	{
		return $"{c.GetType().Name} '{(c.Text ?? "").Replace("\r\n", " ")}' {c.Bounds}";
	}

	private void OnUiThread(Action action)
	{
		try
		{
			if (!IsDisposed)
			{
				if (IsHandleCreated && InvokeRequired)
				{
					Invoke(action);
				}
				else
				{
					action();
				}
			}
		}
		catch (ObjectDisposedException)
		{
		}
		catch (InvalidOperationException)
		{
		}
	}

	private void Marshal(Action action)
	{
		try
		{
			if (!IsDisposed)
			{
				if (IsHandleCreated && InvokeRequired)
				{
					BeginInvoke(action);
				}
				else
				{
					action();
				}
			}
		}
		catch (ObjectDisposedException)
		{
		}
		catch (InvalidOperationException)
		{
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			foreach (PanelState panel in _panels)
			{
				panel.Write.Dispose();
			}
			foreach (DebouncedAction colourSafe in _colourSaves)
			{
				colourSafe.Dispose();
			}
			_tips.Dispose();
		}
		base.Dispose(disposing);
	}

	internal void FlushPendingForTest()
	{
		foreach (PanelState panel in _panels)
		{
			Flush(panel);
		}
	}

	internal void RebuildForTest(IReadOnlyList<MonitorCapability> caps)
	{
		Rebuild(caps);
	}

	internal void SetSliderForTest(string caption, int value)
	{
		CardPanel cardPanel = Controls.OfType<CardPanel>().FirstOrDefault();
		if (cardPanel == null)
		{
			return;
		}
		List<Label> labels = cardPanel.Controls.OfType<Label>().ToList();
		List<FlatSlider> source = cardPanel.Controls.OfType<FlatSlider>().ToList();
		int i;
		for (i = 0; i < labels.Count; i++)
		{
			if ((labels[i].Text ?? "").Replace(" ", "").Equals(caption.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
			{
				FlatSlider flatSlider = source.FirstOrDefault((FlatSlider s) => s.Top > labels[i].Top && s.Top < labels[i].Top + 40);
				if (flatSlider != null)
				{
					flatSlider.Value = value;
				}
				break;
			}
		}
	}
}