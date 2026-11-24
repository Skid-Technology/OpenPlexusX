using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;
using VibranceHud.Licensing;
using VibranceHud.Music;
using VibranceHud.Pages;
using VibranceHud.Theming;

namespace VibranceHud;

public sealed class VibrancePopup : Form
{
	internal sealed class PresetTile : Control
	{
		private sealed class TileAccessible : ControlAccessibleObject
		{
			private readonly PresetTile _tile;

			public override AccessibleRole Role => AccessibleRole.RadioButton;

			public override AccessibleStates State => (AccessibleStates)((int)base.State | (_tile.Active ? 16 : 0));

			public override string DefaultAction => "Select";

			internal TileAccessible(PresetTile tile)
				: base(tile)
			{
				_tile = tile;
			}

			public override void DoDefaultAction()
			{
				_tile.InvokeOnClick(_tile, EventArgs.Empty);
			}
		}

		private readonly int _dpi;

		private readonly Image? _photo;

		private Bitmap? _thumb;

		private Size _thumbFor;

		private bool _active;

		private bool _hover;

		internal DisplayPreset Preset { get; }

		internal bool HasPicture => _photo != null;

		internal Action<int>? MoveBy { get; set; }

		public bool Active
		{
			get
			{
				return _active;
			}
			set
			{
				if (_active != value)
				{
					_active = value;
					Invalidate();
					if (IsHandleCreated)
					{
						AccessibilityNotifyClients(AccessibleEvents.StateChange, -1);
					}
				}
			}
		}

		private Rectangle Box => new Rectangle(Px(4), Px(4), Math.Max(1, Width - 2 * Px(4)), Px(44));

		internal static Action? ThumbFailPointForTest { get; set; }

		private int Px(int logical)
		{
			return Tokens.ScaleAt(_dpi, logical);
		}

		internal PresetTile(DisplayPreset preset, Image? photo, int dpi)
		{
			Preset = preset;
			_photo = photo;
			_dpi = dpi;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Cursor = Cursors.Hand;
			TabStop = false;
			Text = preset.Name;
			AccessibleRole = AccessibleRole.RadioButton;
			AccessibleName = string.Format(CultureInfo.InvariantCulture, "{0}, {1} percent", preset.Name, preset.Saturation);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			_hover = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			_hover = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			Invalidate();
			base.OnGotFocus(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			Invalidate();
			base.OnLostFocus(e);
		}

		protected override bool IsInputKey(Keys keyData)
		{
			Keys keys = keyData & Keys.KeyCode;
			if ((uint)(keys - 35) > 5u)
			{
				return base.IsInputKey(keyData);
			}
			return true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			switch (e.KeyCode)
			{
			case Keys.Left:
			case Keys.Up:
				MoveBy?.Invoke(-1);
				e.Handled = true;
				break;
			case Keys.Right:
			case Keys.Down:
				MoveBy?.Invoke(1);
				e.Handled = true;
				break;
			case Keys.Home:
				MoveBy?.Invoke(-99);
				e.Handled = true;
				break;
			case Keys.End:
				MoveBy?.Invoke(99);
				e.Handled = true;
				break;
			case Keys.Return:
			case Keys.Space:
				OnClick(EventArgs.Empty);
				e.Handled = true;
				break;
			}
		}

		protected override AccessibleObject CreateAccessibilityInstance()
		{
			return new TileAccessible(this);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Rectangle box = Box;
			int num = Px(4);
			using (GraphicsPath path = Glass.RoundedPath(box, num))
			{
				using SolidBrush brush = new SolidBrush(SignalInk.PreviewWell);
				SmoothingMode smoothingMode = g.SmoothingMode;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.FillPath(brush, path);
				g.SmoothingMode = smoothingMode;
			}
			Rectangle picture = Rectangle.Inflate(box, -Px(2), -Px(2));
			if (_photo != null && picture.Width > 1 && picture.Height > 1)
			{
				PaintGuard.Try(this, () =>
				{
					g.DrawImageUnscaled(Thumb(picture.Size), picture.Location);
				});
			}
			Color? color;
			if (_active)
			{
				color = ControlInk.Accent;
			}
			else
			{
				color = (_hover ? new Color?(ControlInk.Edge) : ((Color?)null));
			}
			if (color.HasValue)
			{
				Color valueOrDefault = color.GetValueOrDefault();
				float num2 = Px(2);
				RectangleF rect = new RectangleF((float)box.X + num2 / 2f, (float)box.Y + num2 / 2f, (float)box.Width - num2, (float)box.Height - num2);
				GraphicsState gstate = g.Save();
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.PixelOffsetMode = PixelOffsetMode.Half;
				using (GraphicsPath path2 = Glass.RoundedPath(rect, Math.Max(0f, (float)num - num2 / 2f)))
				{
					using Pen pen = new Pen(valueOrDefault, num2);
					g.DrawPath(pen, path2);
				}
				g.Restore(gstate);
			}
			Rectangle bounds = new Rectangle(0, box.Bottom + Px(4), Width, Px(16));
			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			TextRenderer.DrawText(g, Text, _active ? Fonts.CaptionBold : Fonts.Caption, bounds, _active ? Theme.Text : Theme.TextDim, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			if (Focused && ShowFocusCues)
			{
				ControlInk.PaintFocusRing(g, box, num, _dpi);
			}
		}

		private Bitmap Thumb(Size size)
		{
			if (_thumb != null && _thumbFor == size)
			{
				return _thumb;
			}
			ThumbFailPointForTest?.Invoke();
			using Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				Image photo = _photo;
				float num = Math.Max((float)size.Width / (float)photo.Width, (float)size.Height / (float)photo.Height);
				float num2 = (float)size.Width / num;
				float num3 = (float)size.Height / num;
				RectangleF rectangleF = new RectangleF(((float)photo.Width - num2) / 2f, ((float)photo.Height - num3) / 2f, num2, num3);
				using ImageAttributes imageAttributes = new ImageAttributes();
				imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
				lock (BrandAssets.DrawGate)
				{
					graphics.DrawImage(photo, new Rectangle(Point.Empty, size), rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height, GraphicsUnit.Pixel, imageAttributes);
				}
			}
			Bitmap bitmap2 = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
			try
			{
				using Graphics graphics2 = Graphics.FromImage(bitmap2);
				using TextureBrush brush = new TextureBrush(bitmap);
				using GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, size.Width, size.Height), Px(2));
				graphics2.SmoothingMode = SmoothingMode.AntiAlias;
				graphics2.FillPath(brush, path);
			}
			catch
			{
				bitmap2.Dispose();
				throw;
			}
			Bitmap? thumb = _thumb;
			_thumb = bitmap2;
			_thumbFor = size;
			thumb?.Dispose();
			return bitmap2;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_thumb?.Dispose();
				_thumb = null;
			}
			base.Dispose(disposing);
		}
	}

	private const int PanelW = 420;

	private const int HeaderH = 40;

	private const int HeaderPadL = 16;

	private const int MarkSize = 16;

	private const int MarkGap = 8;

	private const int CloseCell = 46;

	private const int CloseGlyph = 16;

	private const int Pad = 24;

	private const int BodyTop = 8;

	private const int BlockGap = 12;

	private const int LabelLine = 20;

	private const int LiveIcon = 16;

	private const int LiveGap = 8;

	private const int LabelDy = 2;

	private const int ValueDy = 20;

	private const int SatValueLine = 48;

	private const int SatTrack = 80;

	private const int SatTicks = 91;

	private const int SatBlock = 117;

	private const int VibValueLine = 36;

	private const int VibTrack = 68;

	private const int VibTicks = 79;

	private const int VibBlock = 107;

	private const int TickLine = 16;

	private const int TilesDy = 25;

	private const int TileH = 44;

	private const int TileGap = 8;

	private const int TileWordGap = 4;

	private const int TileWordLine = 16;

	private const int TileRing = 4;

	private const int FineTop = 12;

	private const int FooterH = 64;

	private const int FooterPadX = 24;

	private const int ButtonH = 32;

	private const int ButtonGap = 8;

	private const int AutosaveDebounceMs = 250;

	private readonly int _dpi;

	private readonly int _radius;

	private readonly IVibranceEngine _engine;

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly DebouncedAction _autosaveDebounce;

	private readonly Action? _onEngineChangedByUser;

	private readonly Timer _timer;

	private readonly bool _built;

	private bool _readoutDirty = true;

	private readonly List<PresetTile> _presetTiles = new List<PresetTile>();

	private readonly List<DisplayPreset> _presets = new List<DisplayPreset>();

	private bool _applyingPreset;

	private int _looksRowX;

	private int _looksRowTop;

	private int _looksRowW;

	private int _looksGeneration;

	private Label? _looksOnTheirWay;

	private Label? _shortcutLabel;

	private bool _showLooksOnTheirWay;

	private bool _syncingFromEngine;

	private readonly List<(string Caption, Rectangle Row, Func<string> Value)> _rows = new List<(string, Rectangle, Func<string>)>();

	private readonly List<(string Caption, string Hint, Rectangle LabelRow, Rectangle ValueRow, Font ValueFace, Font UnitFace, TwoColorSlider Slider, Func<string> Value, string Unit, string? NotchLabel)> _headlines = new List<(string, string, Rectangle, Rectangle, Font, Font, TwoColorSlider, Func<string>, string, string)>();

	private readonly Dictionary<TwoColorSlider, Rectangle> _tickRows = new Dictionary<TwoColorSlider, Rectangle>();

	private readonly int _headerH;

	private readonly int _liveY;

	private readonly int _liveH;

	private readonly int _presetsHeadingY;

	private readonly int _footerRuleY;

	private bool _draggingASlider;

	private bool _pageOwedTheDrag;

	private int ColGap => TwoColorSlider.EdgeInsetAt(_dpi) * 2 + 2;

	internal bool ListeningForIdle { get; private set; }

	internal nint ReturnFocusTo { get; set; }

	internal Func<nint, bool> SetForeground { get; set; } = (nint window) => IsWindow(window) && SetForegroundWindow(window);

	internal Func<nint, bool> IsOurs { get; set; } = (nint window) =>
	{
		GetWindowThreadProcessId(window, out var processId);
		return processId == (uint)Environment.ProcessId;
	};

	private static Font TitleFont => Fonts.Heading;

	private static Font LiveFont => Fonts.BodyBold;

	private static Font LiveDimFont => Fonts.Body;

	private static Font HeadlineLabelFont => Fonts.Heading;

	private static Font HeadlineHintFont => Fonts.Caption;

	private static Font SatValueFont => Fonts.Value;

	private static Font VibValueFont => Fonts.Display;

	private static Font SatUnitFont => Fonts.Title;

	private static Font VibUnitFont => Fonts.Heading;

	private static Font TickFont => Fonts.Caption;

	private static Font CaptionFont => Fonts.Label;

	private static Font ValueFont => Fonts.LabelBold;

	private static Font FooterFont => Fonts.Caption;

	internal TwoColorSlider VibranceSlider { get; }

	internal TwoColorSlider SaturationSlider { get; }

	internal TwoColorSlider BrightnessSlider { get; }

	internal TwoColorSlider GammaSlider { get; }

	internal TwoColorSlider ContrastSlider { get; }

	internal TwoColorSlider TemperatureSlider { get; }

	internal TwoColorSlider HighlightsSlider { get; }

	internal TwoColorSlider ShadowsSlider { get; }

	private static SliderPalette PlainWhite => new SliderPalette(Theme.Accent, Theme.Accent, Theme.Accent);

	public bool FreeTier { get; }

	public Action? OpenMainWindow { get; set; }

	private int FineRowH => S(20) + S(2) + TwoColorSlider.MinHeightAt(_dpi) + S(10);

	internal IReadOnlyList<string> TestTileNames => _presets.Select((DisplayPreset p) => p.Name).ToList();

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

	internal static Action? PaintFailPointForTest { get; set; }

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWindow(nint window);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetForegroundWindow(nint window);

	private static string Signed(int v)
	{
		if (v <= 0)
		{
			return v.ToString(CultureInfo.InvariantCulture);
		}
		return "+" + v;
	}

	private int S(int logical)
	{
		return Tokens.ScaleAt(_dpi, logical);
	}

	public VibrancePopup(IVibranceEngine engine, AppSettings settings, SettingsStore store, bool freeTier = false, Action? onEngineChangedByUser = null)
	{
		_engine = engine;
		_onEngineChangedByUser = onEngineChangedByUser;
		_settings = settings;
		_store = store;
		FreeTier = freeTier;
		_autosaveDebounce = new DebouncedAction(Save, 250);
		_dpi = SignalInk.DpiFor(this);
		_radius = S(8);
		FormBorderStyle = FormBorderStyle.None;
		StartPosition = FormStartPosition.CenterScreen;
		ShowInTaskbar = false;
		TopMost = true;
		DoubleBuffered = true;
		Icon = AppIcon.Value;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		int num = S(420);
		int num2 = S(24);
		int num3 = num - 2 * num2;
		int num4 = (num3 - ColGap) / 2;
		int x = num2 + num4 + ColGap;
		_headerH = S(40);
		_liveH = S(20);
		_liveY = _headerH + S(8);
		int num5 = _liveY + _liveH + S(12);
		bool flag = !freeTier;
		if (flag)
		{
			SaturationSlider = AddHeadline("Saturation", "100 is the game as it ships", num5, num2, num3, 48, SatValueFont, SatUnitFont, 80, 91, 0, 300, 100, _engine.Saturation, (int v) =>
			{
				if (!_applyingPreset)
				{
					_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Saturation;
				}
				_engine.Saturation = v;
			}, "%", null);
			num5 += S(117);
		}
		else
		{
			SaturationSlider = AddRow("Saturation", num2, num5, num3, 0, 300, 100, _engine.Saturation, (int v) =>
			{
				if (!_applyingPreset)
				{
					_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Saturation;
				}
				_engine.Saturation = v;
			}, () => $"{SaturationSlider.Value}%");
			num5 += FineRowH;
		}
		SaturationSlider.Fill = SliderFill.Signal;
		SaturationSlider.Hero = true;
		if (flag)
		{
			VibranceSlider = AddHeadline("Vibrance", "Past 100 it continues in software", num5, num2, num3, 36, VibValueFont, VibUnitFont, 68, 79, 0, 350, 100, _engine.Vibrance, (int v) =>
			{
				if (!_applyingPreset)
				{
					_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Vibrance;
				}
				_engine.Vibrance = v;
			}, "%", $"{100} · driver");
			num5 += S(107);
		}
		else
		{
			VibranceSlider = new TwoColorSlider
			{
				Minimum = 0,
				Maximum = 350,
				Notch = 100,
				Value = _engine.Vibrance,
				Palette = PlainWhite,
				Visible = false,
				TabStop = false,
				AccessibleName = "Vibrance"
			};
			Controls.Add(VibranceSlider);
		}
		_presetsHeadingY = (flag ? num5 : (-1));
		if (flag)
		{
			int num6 = num5 + S(25);
			_looksRowX = num2;
			_looksRowTop = num6;
			_looksRowW = num3;
			_looksOnTheirWay = new Label
			{
				Text = "Your looks are on the way. They load when PlexusX reaches our server.",
				Font = Fonts.Caption,
				ForeColor = SignalInk.Faint,
				BackColor = Color.Transparent,
				AutoSize = false,
				TextAlign = ContentAlignment.MiddleLeft,
				Bounds = new Rectangle(num2 - S(3), num6, num3 + S(3), S(44) + S(4) + S(16)),
				Visible = false
			};
			Controls.Add(_looksOnTheirWay);
			BuildLooksRow();
			num5 = num6 + S(44) + S(4) + S(16);
			num5 += S(12);
			BrightnessSlider = AddRow("Brightness", num2, num5, num4, 50, 170, 100, _engine.Brightness, (int v) =>
			{
				_engine.Brightness = v;
			}, () => $"{BrightnessSlider.Value}%");
			GammaSlider = AddRow("Gamma", x, num5, num4, 50, 150, 100, _engine.Gamma, (int v) =>
			{
				_engine.Gamma = v;
			}, () => ((float)GammaSlider.Value / 100f).ToString("0.00", CultureInfo.InvariantCulture));
			num5 += FineRowH;
			ContrastSlider = AddRow("Contrast", num2, num5, num4, 50, 150, 100, _engine.Contrast, (int v) =>
			{
				_engine.Contrast = v;
			}, () => $"{ContrastSlider.Value}%");
			TemperatureSlider = AddRow("Temperature", x, num5, num4, -100, 100, 0, _engine.Temperature, (int v) =>
			{
				_engine.Temperature = v;
			}, () => VibrancePage.TemperatureText(TemperatureSlider.Value));
			num5 += FineRowH;
			HighlightsSlider = AddRow("Highlights", num2, num5, num4, -100, 100, 0, _engine.Tone.Highlights, (int v) =>
			{
				_engine.Tone = _engine.Tone with
				{
					Highlights = v
				};
			}, () => Signed(HighlightsSlider.Value));
			ShadowsSlider = AddRow("Shadows", x, num5, num4, -100, 100, 0, _engine.Tone.Shadows, (int v) =>
			{
				_engine.Tone = _engine.Tone with
				{
					Shadows = v
				};
			}, () => Signed(ShadowsSlider.Value));
			num5 += FineRowH;
		}
		else
		{
			BrightnessSlider = AddRow("Brightness", num2, num5, num4, 50, 170, 100, _engine.Brightness, (int v) =>
			{
				_engine.Brightness = v;
			}, () => "", shown: false);
			GammaSlider = AddRow("Gamma", x, num5, num4, 50, 150, 100, _engine.Gamma, (int v) =>
			{
				_engine.Gamma = v;
			}, () => "", shown: false);
			ContrastSlider = AddRow("Contrast", num2, num5, num4, 50, 150, 100, _engine.Contrast, (int v) =>
			{
				_engine.Contrast = v;
			}, () => "", shown: false);
			TemperatureSlider = AddRow("Temperature", x, num5, num4, -100, 100, 0, _engine.Temperature, (int v) =>
			{
				_engine.Temperature = v;
			}, () => "", shown: false);
			HighlightsSlider = AddRow("Highlights", num2, num5, num4, -100, 100, 0, _engine.Tone.Highlights, (int v) =>
			{
				_engine.Tone = _engine.Tone with
				{
					Highlights = v
				};
			}, () => "", shown: false);
			ShadowsSlider = AddRow("Shadows", x, num5, num4, -100, 100, 0, _engine.Tone.Shadows, (int v) =>
			{
				_engine.Tone = _engine.Tone with
				{
					Shadows = v
				};
			}, () => "", shown: false);
			Controls.Add(new Label
			{
				Text = "The rest comes with a plan.",
				Font = Fonts.Caption,
				ForeColor = SignalInk.Faint,
				BackColor = Color.Transparent,
				AutoSize = false,
				TextAlign = ContentAlignment.MiddleLeft,
				Bounds = new Rectangle(num2 - S(3), num5, num3 + S(3), Math.Max(S(20), Fonts.Caption.Height))
			});
			num5 += Math.Max(S(20), Fonts.Caption.Height) + S(10);
		}
		_footerRuleY = num5;
		int num7 = S(64);
		int num8 = Math.Max(S(32), Fonts.BodyBold.Height + S(6));
		int y = _footerRuleY + S(1) + (num7 - S(1) - num8 + 1) / 2;
		ClientSize = new Size(num, _footerRuleY + num7);
		BackColor = Theme.Background;
		int num9 = Math.Max(S(16), S(32));
		int num10 = num - S(46) + (S(46) - S(16)) / 2 + S(16) / 2;
		int num11 = (_headerH - S(16)) / 2 + S(16) / 2;
		VibranceHud.Controls.CaptionButton captionButton = new VibranceHud.Controls.CaptionButton(VibranceHud.Controls.CaptionButton.Kind.Close)
		{
			Location = new Point(num10 - num9 / 2, num11 - num9 / 2),
			Size = new Size(num9, num9)
		};
		captionButton.Click += (object? s, EventArgs e) =>
		{
			Save();
			Hide();
			GiveFocusBack();
		};
		Controls.Add(captionButton);
		GlassButton glassButton = new GlassButton
		{
			Text = "Reset",
			Kind = GlassButtonKind.Quiet,
			Icon = SignalIcon.Reset,
			Height = num8
		};
		GlassButton glassButton2 = new GlassButton
		{
			Text = "Open PlexusX",
			Kind = GlassButtonKind.Secondary,
			TrailingIcon = SignalIcon.Arrow,
			Height = num8
		};
		int width = glassButton2.GetPreferredSize(Size.Empty).Width;
		int width2 = glassButton.GetPreferredSize(Size.Empty).Width;
		int num12 = num - S(24) - width;
		int num13 = num12 - S(8) - width2;
		glassButton2.Bounds = new Rectangle(num12, y, width, num8);
		glassButton.Bounds = new Rectangle(num13, y, width2, num8);
		glassButton.Click += (object? s, EventArgs e) =>
		{
			ResetToDefaults();
		};
		glassButton2.Click += (object? s, EventArgs e) =>
		{
			OpenMainWindow?.Invoke();
		};
		_shortcutLabel = new Label
		{
			Text = ShortcutWords(),
			AutoEllipsis = true,
			Font = FooterFont,
			ForeColor = SignalInk.Faint,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(S(24) - S(3), y, Math.Max(S(40), num13 - S(24) - S(8)), num8)
		};
		Controls.Add(_shortcutLabel);
		Controls.Add(glassButton);
		Controls.Add(glassButton2);
		MusicPlayer.AttachToPopup(this, _shortcutLabel);
		ApplyRoundedRegion();
		WindowDrag.Enable(this, this);
		RestorePosition();
		UpdateActiveChip();
		_timer = new Timer
		{
			Interval = 60000
		};
		_timer.Start();
		_built = true;
	}

	private void ResetToDefaults()
	{
		_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Stronger;
		_applyingPreset = true;
		try
		{
			SaturationSlider.Value = 100;
			BrightnessSlider.Value = 100;
			GammaSlider.Value = 100;
			ContrastSlider.Value = 100;
			TemperatureSlider.Value = 0;
			HighlightsSlider.Value = 0;
			ShadowsSlider.Value = 0;
		}
		finally
		{
			_applyingPreset = false;
		}
		_engine.Tone = _engine.Tone with
		{
			Blacks = 0,
			Whites = 0,
			ShadowTint = 0,
			HighlightTint = 0
		};
		if (_engine is VibranceEngine vibranceEngine)
		{
			vibranceEngine.Mix = ChannelMix.Identity;
			_settings.Mix = null;
			vibranceEngine.Hue = 0;
			int num = (vibranceEngine.BlueGain = 100);
			int redGain = (vibranceEngine.GreenGain = num);
			vibranceEngine.RedGain = redGain;
			_settings.Hue = 0;
			AppSettings settings = _settings;
			AppSettings settings2 = _settings;
			num = (_settings.BlueGain = 100);
			redGain = (settings2.GreenGain = num);
			settings.RedGain = redGain;
		}
		_onEngineChangedByUser?.Invoke();
		UpdateActiveChip();
		Save();
		Invalidate();
	}

	private void ChooseNeighbour(int from, int delta)
	{
		if (_presetTiles.Count != 0)
		{
			int num = Math.Clamp(from + delta, 0, _presetTiles.Count - 1);
			if (num != from)
			{
				ApplyPreset(_presets[num]);
				_presetTiles[num].Focus();
			}
		}
	}

	private TwoColorSlider AddHeadline(string caption, string hint, int top, int x, int width, int valueLine, Font valueFace, Font unitFace, int trackCentre, int tickTop, int min, int max, int? notch, int value, Action<int> apply, string unit, string? notchLabel)
	{
		Rectangle item = new Rectangle(x, top + S(2), width, S(20));
		Rectangle valueRow = new Rectangle(x, top + S(20), width, S(valueLine));
		TwoColorSlider slider = new TwoColorSlider
		{
			Minimum = min,
			Maximum = max,
			Notch = notch,
			Value = value,
			Palette = PlainWhite,
			Hero = true,
			AccessibleName = caption,
			KnobRing = Theme.Background,
			Snap = SliderSnapMode.Gentle
		};
		slider.SetTrackBounds(x, top + S(trackCentre) - TwoColorSlider.MinHeightAt(_dpi) / 2, width);
		Rectangle value2 = new Rectangle(x, top + S(tickTop), width, Math.Max(S(16), TickFont.Height));
		slider.ValueChanged += (object? s, EventArgs e) =>
		{
			_readoutDirty = true;
			if (!_syncingFromEngine)
			{
				apply(slider.Value);
				Invalidate(valueRow);
				if (!_applyingPreset)
				{
					UpdateActiveChip();
				}
				_autosaveDebounce.Trigger();
				_settings.ManualOverrideActive = true;
				TellTheDisplayPage();
			}
		};
		slider.DragBegin += (object? s, EventArgs e) =>
		{
			BeginSliderDrag();
		};
		slider.DragEnd += (object? s, EventArgs e) =>
		{
			EndSliderDrag();
		};
		Controls.Add(slider);
		Func<string> item2 = () => slider.Value.ToString(CultureInfo.InvariantCulture);
		_headlines.Add((caption, hint, item, valueRow, valueFace, unitFace, slider, item2, unit, notchLabel));
		_tickRows[slider] = value2;
		return slider;
	}

	private TwoColorSlider AddRow(string caption, int x, int y, int width, int min, int max, int? notch, int value, Action<int> apply, Func<string> readout, bool shown = true)
	{
		int labelLine = Math.Max(S(20), CaptionFont.Height);
		TwoColorSlider slider = new TwoColorSlider
		{
			Minimum = min,
			Maximum = max,
			Notch = notch,
			Value = value,
			Palette = PlainWhite,
			Visible = shown,
			TabStop = shown,
			AccessibleName = caption,
			KnobRing = Theme.Background,
			Snap = SliderSnapMode.Gentle
		};
		slider.SetTrackBounds(x, y + labelLine + S(2), width);
		slider.ValueChanged += (object? s, EventArgs e) =>
		{
			_readoutDirty = true;
			if (!_syncingFromEngine)
			{
				apply(slider.Value);
				Invalidate(new Rectangle(x, y, width, labelLine));
				if (!_applyingPreset)
				{
					UpdateActiveChip();
				}
				_autosaveDebounce.Trigger();
				_settings.ManualOverrideActive = true;
				TellTheDisplayPage();
			}
		};
		slider.DragBegin += (object? s, EventArgs e) =>
		{
			BeginSliderDrag();
		};
		slider.DragEnd += (object? s, EventArgs e) =>
		{
			EndSliderDrag();
		};
		Controls.Add(slider);
		if (shown)
		{
			_rows.Add((caption, new Rectangle(x, y, width, labelLine), readout));
		}
		return slider;
	}

	private void TellTheDisplayPage()
	{
		if (_draggingASlider)
		{
			_pageOwedTheDrag = true;
		}
		else
		{
			_onEngineChangedByUser?.Invoke();
		}
	}

	private void BeginSliderDrag()
	{
		_draggingASlider = true;
		_engine.BeginDrag();
	}

	private void EndSliderDrag()
	{
		_engine.EndDrag();
		_draggingASlider = false;
		if (_pageOwedTheDrag)
		{
			_pageOwedTheDrag = false;
			_onEngineChangedByUser?.Invoke();
		}
	}

	private void ApplyPreset(DisplayPreset preset)
	{
		_applyingPreset = true;
		try
		{
			_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Stronger;
			SaturationSlider.Value = Math.Clamp(preset.Saturation, SaturationSlider.Minimum, SaturationSlider.Maximum);
			VibranceSlider.Value = Math.Clamp(preset.Vibrance, VibranceSlider.Minimum, VibranceSlider.Maximum);
			BrightnessSlider.Value = preset.Brightness;
			GammaSlider.Value = preset.Gamma;
			ContrastSlider.Value = preset.Contrast;
			TemperatureSlider.Value = preset.Temperature;
			HighlightsSlider.Value = Math.Clamp(preset.Highlights, HighlightsSlider.Minimum, HighlightsSlider.Maximum);
			ShadowsSlider.Value = Math.Clamp(preset.Shadows, ShadowsSlider.Minimum, ShadowsSlider.Maximum);
			if (_engine is VibranceEngine vibranceEngine)
			{
				vibranceEngine.Hue = preset.Hue;
				vibranceEngine.RedGain = preset.RedGain;
				vibranceEngine.GreenGain = preset.GreenGain;
				vibranceEngine.BlueGain = preset.BlueGain;
				_settings.Hue = vibranceEngine.Hue;
				_settings.RedGain = vibranceEngine.RedGain;
				_settings.GreenGain = vibranceEngine.GreenGain;
				_settings.BlueGain = vibranceEngine.BlueGain;
				vibranceEngine.Depth = preset.Depth().Clamped();
				_settings.Depth = vibranceEngine.Depth;
			}
		}
		finally
		{
			_applyingPreset = false;
		}
		ColourVisionMode? colourVisionMode = LookCorrection.GiveTheirsBack(_settings);
		if (colourVisionMode.HasValue)
		{
			ColourVisionMode valueOrDefault = colourVisionMode.GetValueOrDefault();
			PutCorrectionOnScreen(valueOrDefault);
			_onEngineChangedByUser?.Invoke();
		}
		UpdateActiveChip();
		Save();
		Invalidate();
	}

	private void PutCorrectionOnScreen(ColourVisionMode mode)
	{
		if (_engine is VibranceEngine vibranceEngine)
		{
			vibranceEngine.ColourVision = mode;
		}
	}

	private DisplayPreset? CurrentPreset()
	{
		ToneSettings tone = _engine.Tone;
		if (_settings.ColourVisionFromPreset)
		{
			return null;
		}
		for (int i = 0; i < _presets.Count; i++)
		{
			if (_presets[i].MatchesWithGrade(SaturationSlider.Value, VibranceSlider.Value, BrightnessSlider.Value, GammaSlider.Value, ContrastSlider.Value, TemperatureSlider.Value, tone.Highlights, tone.Shadows, tone.NightVision, tone.Blacks, tone.Whites, tone.ShadowTint, tone.HighlightTint, _engine.Mix, (_engine is VibranceEngine vibranceEngine) ? vibranceEngine.Hue : 0, (_engine is VibranceEngine vibranceEngine2) ? vibranceEngine2.RedGain : 100, (_engine is VibranceEngine vibranceEngine3) ? vibranceEngine3.GreenGain : 100, (_engine is VibranceEngine vibranceEngine4) ? vibranceEngine4.BlueGain : 100, (_engine is VibranceEngine vibranceEngine5) ? vibranceEngine5.Depth : ColourDepth.Neutral))
			{
				return _presets[i];
			}
		}
		return null;
	}

	private void UpdateActiveChip()
	{
		DisplayPreset displayPreset = CurrentPreset();
		int val = -1;
		for (int i = 0; i < _presetTiles.Count; i++)
		{
			bool flag = displayPreset != null && (object)_presets[i] == displayPreset;
			_presetTiles[i].Active = flag;
			if (flag)
			{
				val = i;
			}
		}
		for (int j = 0; j < _presetTiles.Count; j++)
		{
			_presetTiles[j].TabStop = j == Math.Max(0, val);
		}
		Invalidate(new Rectangle(0, _liveY, ClientSize.Width, _liveH));
	}

	private void ApplyRoundedRegion()
	{
		using GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, ClientSize.Width, ClientSize.Height), _radius);
		Region = new Region(path);
	}

	private void RestorePosition()
	{
		if (_settings.PopupX == int.MinValue || _settings.PopupY == int.MinValue)
		{
			return;
		}
		Rectangle b = new Rectangle(_settings.PopupX, _settings.PopupY, Width, Height);
		bool flag = false;
		Screen[] allScreens = Screen.AllScreens;
		for (int i = 0; i < allScreens.Length; i++)
		{
			Rectangle rectangle = Rectangle.Intersect(allScreens[i].WorkingArea, b);
			if (rectangle.Width >= 120 && rectangle.Height >= 60)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			StartPosition = FormStartPosition.Manual;
			Location = b.Location;
		}
	}

	protected override void OnMove(EventArgs e)
	{
		base.OnMove(e);
		if (_built && WindowState == FormWindowState.Normal)
		{
			_settings.PopupX = Left;
			_settings.PopupY = Top;
			_autosaveDebounce.Trigger();
		}
	}

	private void OnIdleRepaint(object? sender, EventArgs e)
	{
		if (_readoutDirty)
		{
			_readoutDirty = false;
			Invalidate();
		}
	}

	private void StopListeningForIdle()
	{
		if (ListeningForIdle)
		{
			Application.Idle -= OnIdleRepaint;
			ListeningForIdle = false;
		}
	}

	protected override void SetVisibleCore(bool value)
	{
		if (value && !Visible)
		{
			nint foregroundWindow = GetForegroundWindow();
			ReturnFocusTo = ((foregroundWindow != IntPtr.Zero && !IsOurs(foregroundWindow)) ? foregroundWindow : IntPtr.Zero);
		}
		base.SetVisibleCore(value);
	}

	private void GiveFocusBack()
	{
		if (ReturnFocusTo != IntPtr.Zero && !IsOurs(ReturnFocusTo))
		{
			SetForeground(ReturnFocusTo);
		}
	}

	protected override void OnDeactivate(EventArgs e)
	{
		base.OnDeactivate(e);
		Save();
		Hide();
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == Keys.Escape)
		{
			Save();
			Hide();
			GiveFocusBack();
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	private void BuildLooksRow()
	{
		foreach (PresetTile presetTile2 in _presetTiles)
		{
			Controls.Remove(presetTile2);
			presetTile2.Dispose();
		}
		_presetTiles.Clear();
		_presets.Clear();
		_looksGeneration = ServedLooks.Generation;
		List<DisplayPreset> list = new List<DisplayPreset>(LookCorrection.OfferableWithoutANote(ServedLooks.ForGame(_settings.ColourPresetGame).Presets));
		int count = list.Count;
		int num = S(8);
		int num2 = Math.Max(5, count);
		int num3 = ((count > 0) ? ((_looksRowW - (num2 - 1) * num) / num2) : 0);
		int num4 = S(4);
		for (int i = 0; i < count; i++)
		{
			DisplayPreset preset = list[i];
			int index = i;
			PresetTile presetTile = new PresetTile(preset, BrandAssets.PresetChip(preset.Art), _dpi)
			{
				Bounds = new Rectangle(_looksRowX + i * (num3 + num) - num4, _looksRowTop - num4, num3 + 2 * num4, num4 + S(44) + S(4) + S(16))
			};
			presetTile.Click += (object? s, EventArgs e) =>
			{
				ApplyPreset(preset);
			};
			presetTile.MoveBy = (int delta) =>
			{
				ChooseNeighbour(index, delta);
			};
			_presets.Add(preset);
			_presetTiles.Add(presetTile);
			Controls.Add(presetTile);
		}
		_showLooksOnTheirWay = _looksOnTheirWay != null && count == 0 && ServedLooks.OnTheirWay;
		if (_looksOnTheirWay != null && _showLooksOnTheirWay)
		{
			_looksOnTheirWay.Text = PcClock.WaitingWords();
		}
		if (_looksOnTheirWay != null)
		{
			_looksOnTheirWay.Visible = _showLooksOnTheirWay;
		}
	}

	internal void ReloadServedLooks()
	{
		if (FreeTier || IsDisposed || _looksOnTheirWay == null)
		{
			return;
		}
		if (ServedLooks.Generation == _looksGeneration)
		{
			if (_showLooksOnTheirWay && _looksOnTheirWay.Text != PcClock.WaitingWords())
			{
				_looksOnTheirWay.Text = PcClock.WaitingWords();
				Invalidate(invalidateChildren: true);
			}
		}
		else
		{
			BuildLooksRow();
			UpdateActiveChip();
			Invalidate(invalidateChildren: true);
		}
	}

	private string ShortcutWords()
	{
		string text = HotkeyPicker.GetDisplay(_settings.HotkeyModifierMask, _settings.HotkeyVirtualKey);
		string text2 = "";
		if (text.EndsWith("Num+", StringComparison.Ordinal))
		{
			text = text.Substring(0, text.Length - "Num+".Length);
			text2 = "Num+";
		}
		return "Shortcut: " + text.Replace("+", " + ") + text2;
	}

	private void UpdateShortcutLabel()
	{
		if (_shortcutLabel != null && !_shortcutLabel.IsDisposed)
		{
			string text = ShortcutWords();
			if (_shortcutLabel.Text != text)
			{
				_shortcutLabel.Text = text;
			}
		}
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		if (Visible)
		{
			UpdateShortcutLabel();
			if (!ListeningForIdle)
			{
				Application.Idle += OnIdleRepaint;
				ListeningForIdle = true;
			}
		}
		else
		{
			StopListeningForIdle();
		}
		base.OnVisibleChanged(e);
	}

	internal void SyncFromEngine()
	{
		_readoutDirty = true;
		UpdateShortcutLabel();
		_syncingFromEngine = true;
		try
		{
			VibranceSlider.Value = _engine.Vibrance;
			SaturationSlider.Value = _engine.Saturation;
			BrightnessSlider.Value = _engine.Brightness;
			GammaSlider.Value = _engine.Gamma;
			ContrastSlider.Value = _engine.Contrast;
			TemperatureSlider.Value = _engine.Temperature;
			HighlightsSlider.Value = _engine.Tone.Highlights;
			ShadowsSlider.Value = _engine.Tone.Shadows;
		}
		finally
		{
			_syncingFromEngine = false;
		}
		UpdateActiveChip();
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		using (SolidBrush brush = new SolidBrush(Theme.Background))
		{
			g.FillRectangle(brush, ClientRectangle);
		}
		PaintGuard.Try(this, () =>
		{
			PaintFailPointForTest?.Invoke();
			AppBackground.PaintInto(g, Width, Height);
		});
		using (GraphicsPath path = Glass.RoundedPath(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), _radius))
		{
			using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(210, Theme.Background)))
			{
				g.FillPath(brush2, path);
			}
			PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			using (Pen pen = new Pen(SignalInk.WindowEdge, 1f))
			{
				g.DrawPath(pen, path);
			}
			g.PixelOffsetMode = pixelOffsetMode;
		}
		g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		int num = S(24);
		int num2 = ClientSize.Width - 2 * num;
		int num3 = S(16);
		Rectangle r = new Rectangle(S(16), (_headerH - num3) / 2, num3, num3);
		using (SolidBrush brush3 = new SolidBrush(Theme.Accent))
		{
			g.FillPolygon(brush3, StarPoints(r));
		}
		int num4 = S(16) + num3 + S(8);
		TextRenderer.DrawText(g, "Quick vibrance", TitleFont, new Rectangle(num4, 0, ClientSize.Width - num4 - S(46), _headerH), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		string text = CurrentPreset()?.Name ?? "Custom";
		int num5 = S(16);
		SignalIcons.Draw(g, SignalIcon.Success, new RectangleF(num, (float)_liveY + (float)(_liveH - num5) / 2f, num5, num5), ControlInk.Success);
		int num6 = num + num5 + S(8);
		int width = TextRenderer.MeasureText(g, "Live", LiveFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
		TextRenderer.DrawText(g, "Live", LiveFont, new Rectangle(num6, _liveY, width, _liveH), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		TextRenderer.DrawText(g, text, LiveDimFont, new Rectangle(num6 + width + S(8), _liveY, Math.Max(1, num + num2 - num6 - width - S(8)), _liveH), Theme.TextDim, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		foreach (var headline in _headlines)
		{
			TextRenderer.DrawText(g, headline.Caption, HeadlineLabelFont, headline.LabelRow, Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			TextRenderer.DrawText(g, headline.Hint, HeadlineHintFont, headline.LabelRow, SignalInk.Faint, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			string text2 = headline.Value();
			Size size = TextRenderer.MeasureText(g, text2, headline.ValueFace, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
			TextRenderer.DrawText(g, text2, headline.ValueFace, new Rectangle(headline.ValueRow.X, headline.ValueRow.Y, size.Width, headline.ValueRow.Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			TextRenderer.DrawText(g, headline.Unit, headline.UnitFace, new Rectangle(headline.ValueRow.X + size.Width, headline.ValueRow.Y, Math.Max(1, headline.ValueRow.Width - size.Width), headline.ValueRow.Height), Theme.TextDim, TextFormatFlags.Bottom | TextFormatFlags.NoPadding);
			if (_tickRows.TryGetValue(headline.Slider, out var value))
			{
				DrawHeadlineTicks(g, value, headline.Slider, headline.NotchLabel);
			}
		}
		foreach (var (text3, bounds, func) in _rows)
		{
			TextRenderer.DrawText(g, text3, CaptionFont, bounds, Theme.TextDim, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			TextRenderer.DrawText(g, func(), ValueFont, bounds, Theme.Text, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
		if (_presetsHeadingY >= 0)
		{
			TextRenderer.DrawText(g, PresetsHeading(), TitleFont, new Rectangle(num, _presetsHeadingY, num2, S(20)), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
		PixelOffsetMode pixelOffsetMode2 = g.PixelOffsetMode;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		using (SolidBrush brush4 = new SolidBrush(Theme.Border))
		{
			g.FillRectangle(brush4, 0, _footerRuleY, ClientSize.Width, 1);
		}
		g.PixelOffsetMode = pixelOffsetMode2;
		base.OnPaint(e);
	}

	private string PresetsHeading()
	{
		string text = _settings.ColourPresetGame ?? "";
		if (text.Length != 0)
		{
			return text + " presets";
		}
		return "Presets";
	}

	private void DrawHeadlineTicks(Graphics g, Rectangle row, TwoColorSlider slider, string? notchLabel)
	{
		int x = row.X;
		int right = row.Right;
		int num = Math.Max(1, right - x);
		int num2 = S(60);
		TextFormatFlags textFormatFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
		TextRenderer.DrawText(g, slider.Minimum.ToString(CultureInfo.InvariantCulture), TickFont, new Rectangle(x, row.Y, num2, row.Height), SignalInk.Faint, textFormatFlags | TextFormatFlags.Default);
		TextRenderer.DrawText(g, slider.Maximum.ToString(CultureInfo.InvariantCulture), TickFont, new Rectangle(right - num2, row.Y, num2, row.Height), SignalInk.Faint, textFormatFlags | TextFormatFlags.Right);
		if (notchLabel != null)
		{
			int? notch = slider.Notch;
			if (notch.HasValue)
			{
				int valueOrDefault = notch.GetValueOrDefault();
				float num3 = (float)(valueOrDefault - slider.Minimum) / (float)(slider.Maximum - slider.Minimum);
				int num4 = x + (int)(num3 * (float)num);
				TextRenderer.DrawText(g, notchLabel, TickFont, new Rectangle(num4 - num2, row.Y, num2 * 2, row.Height), SignalInk.Faint, textFormatFlags | TextFormatFlags.HorizontalCenter);
				return;
			}
		}
		for (int i = 1; i <= 2; i++)
		{
			float num5 = (float)i / 3f;
			int num6 = x + (int)(num5 * (float)num);
			TextRenderer.DrawText(g, (slider.Minimum + (int)Math.Round((float)(slider.Maximum - slider.Minimum) * num5)).ToString(CultureInfo.InvariantCulture), TickFont, new Rectangle(num6 - num2 / 2, row.Y, num2, row.Height), SignalInk.Faint, textFormatFlags | TextFormatFlags.HorizontalCenter);
		}
	}

	private static Point[] StarPoints(Rectangle r)
	{
		float num = (float)r.X + (float)r.Width / 2f;
		float num2 = (float)r.Y + (float)r.Height / 2f;
		float num3 = (float)r.Width / 2f;
		float num4 = num3 * 0.4f;
		Point[] array = new Point[24];
		for (int i = 0; i < 24; i++)
		{
			double num5 = Math.PI / 12.0 * (double)i - Math.PI / 2.0;
			float num6 = ((i % 2 == 0) ? num3 : num4);
			array[i] = new Point((int)((double)num + (double)num6 * Math.Cos(num5)), (int)((double)num2 + (double)num6 * Math.Sin(num5)));
		}
		return array;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	internal void Save()
	{
		_settings.VibrancePercent = VibranceSlider.Value;
		if (_engine.VibranceChosenByUser)
		{
			_settings.VibranceChosenByUser = true;
		}
		_settings.SaturationPercent = SaturationSlider.Value;
		_settings.BrightnessPercent = BrightnessSlider.Value;
		_settings.GammaPercent = GammaSlider.Value;
		_settings.ContrastPercent = ContrastSlider.Value;
		_settings.Temperature = TemperatureSlider.Value;
		_settings.Tone = _engine.Tone;
		_store.Save(_settings);
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		Save();
		base.OnFormClosing(e);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			StopListeningForIdle();
			_timer?.Stop();
			_timer?.Dispose();
			_autosaveDebounce.Dispose();
		}
		base.Dispose(disposing);
	}
}
