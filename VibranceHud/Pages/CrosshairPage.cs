using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Crosshair;
using VibranceHud.Design;
using VibranceHud.Licensing;

namespace VibranceHud.Pages;

public sealed class CrosshairPage : GlowPage
{
	private sealed class PreviewBox : Panel
	{
		public CrosshairConfig Config { get; set; } = new CrosshairConfig();

		public PreviewBox()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, Width, Height), Tokens.Scale(4));
			using Region clip = graphics.Clip;
			graphics.SetClip(path, CombineMode.Intersect);
			CrosshairRender.DrawCheckerboard(graphics, new Rectangle(0, 0, Width, Height), Tokens.Scale(16));
			GraphicsState gstate = graphics.Save();
			graphics.TranslateTransform((float)Width / 2f, (float)Height / 2f);
			CrosshairRender.Draw(graphics, Config);
			graphics.Restore(gstate);
			graphics.Clip = clip;
			base.OnPaint(e);
		}
	}

	private class SwatchDot : Control
	{
		private readonly Color _colour;

		private readonly OutsideFocusRing _ring;

		private bool _active;

		private bool _hover;

		public Color Colour => _colour;

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
				}
			}
		}

		public SwatchDot(Color colour)
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			_colour = colour;
			Size = new Size(28, 28);
			Cursor = Cursors.Hand;
			BackColor = Color.Transparent;
			AccessibleRole = AccessibleRole.RadioButton;
			AccessibleName = ColourWheel.ToHex(colour);
			_ring = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 4f));
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			_hover = true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_hover = false;
			Invalidate();
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			Invalidate();
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			Invalidate();
		}

		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			_ring.Rehook();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			Keys keyCode = e.KeyCode;
			if ((keyCode == Keys.Return || keyCode == Keys.Space) ? true : false)
			{
				e.Handled = true;
				OnClick(EventArgs.Empty);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_ring.Release();
			}
			base.Dispose(disposing);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			float num = (float)Width / 28f;
			RectangleF rect = new RectangleF(0f, 0f, Width, Height);
			RectangleF rect2 = RectangleF.Inflate(rect, -2f * num, -2f * num);
			if (_active)
			{
				using SolidBrush brush = new SolidBrush(ControlInk.Accent);
				using GraphicsPath path = Glass.RoundedPath(rect, 4f * num);
				graphics.FillPath(brush, path);
			}
			using (GraphicsPath path2 = Glass.RoundedPath(rect2, 2f * num))
			{
				using SolidBrush brush2 = new SolidBrush(Enabled ? _colour : UiHelpers.Muted(_colour));
				graphics.FillPath(brush2, path2);
			}
			float num2 = Math.Max(1f, (float)Math.Floor(num));
			using (Pen pen = new Pen((_hover && !_active) ? ControlInk.Text2 : ControlInk.Border, num2))
			{
				using GraphicsPath path3 = Glass.RoundedPath(RectangleF.Inflate(rect2, (0f - num2) / 2f, (0f - num2) / 2f), Math.Max(0.5f, 2f * num - num2 / 2f));
				graphics.DrawPath(pen, path3);
			}
			_ring.Show(Focused && ShowFocusCues && Enabled);
		}
	}

	private sealed class OutlineSwatch : SwatchDot
	{
		public OutlineSwatch(Color colour)
			: base(colour)
		{
		}
	}

	private sealed class AddSwatch : Control
	{
		private readonly OutsideFocusRing _ring;

		private bool _hover;

		public AddSwatch()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Size = new Size(28, 28);
			Cursor = Cursors.Hand;
			TabStop = true;
			AccessibleRole = AccessibleRole.PushButton;
			AccessibleName = "Any colour";
			_ring = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 4f));
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			_hover = true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_hover = false;
			Invalidate();
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			Invalidate();
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			Invalidate();
		}

		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			_ring.Rehook();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			Keys keyCode = e.KeyCode;
			if ((keyCode == Keys.Return || keyCode == Keys.Space) ? true : false)
			{
				e.Handled = true;
				OnClick(EventArgs.Empty);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_ring.Release();
			}
			base.Dispose(disposing);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			float num = (float)Width / 28f;
			RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
			using (Pen pen = new Pen(_hover ? ControlInk.Text1 : ControlInk.Edge, 1f)
			{
				DashStyle = DashStyle.Dash
			})
			{
				using GraphicsPath path = Glass.RoundedPath(rect, 4f * num);
				graphics.DrawPath(pen, path);
			}
			float num2 = 16f * num;
			SignalIcons.Draw(graphics, SignalIcon.Plus, new RectangleF(((float)Width - num2) / 2f, ((float)Height - num2) / 2f, num2, num2), ControlInk.Text2);
			_ring.Show(Focused && ShowFocusCues && Enabled);
		}
	}

	private sealed class GuidanceNote : Control
	{
		private readonly (string Text, bool Code)[] _runs;

		private static Font? _codeFont;

		private static int _codeFontDpi;

		private static float _codeFontPoints;

		private static int Pad => S(12);

		private static int Icon => S(16);

		private static int IconGap => S(8);

		private static int Line => S(16);

		private static Font CodeFont
		{
			get
			{
				float sizeInPoints = Fonts.Caption.SizeInPoints;
				if (_codeFont == null || _codeFontDpi != Tokens.Dpi || _codeFontPoints != sizeInPoints)
				{
					_codeFont?.Dispose();
					_codeFont = Fonts.Of(FontRole.Caption, Fonts.MonoFamily);
					_codeFontDpi = Tokens.Dpi;
					_codeFontPoints = sizeInPoints;
				}
				return _codeFont;
			}
		}

		public GuidanceNote(string before, string code, string after)
		{
			_runs = new (string, bool)[3]
			{
				(before, false),
				(code, true),
				(after, false)
			};
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Text = before + code + after;
			SetStyle(ControlStyles.Selectable, value: false);
			TabStop = false;
		}

		private static int S(int v)
		{
			return Tokens.Scale(v);
		}

		private static Font FontOf(bool code)
		{
			if (!code)
			{
				return Fonts.Caption;
			}
			return CodeFont;
		}

		private List<List<(string Word, bool Code)>> Wrap(int width)
		{
			List<List<(string, bool)>> list = new List<List<(string, bool)>>
			{
				new List<(string, bool)>()
			};
			int num = 0;
			int width2 = TextRenderer.MeasureText(" ", Fonts.Caption, Size.Empty, TextFormatFlags.NoPadding).Width;
			(string, bool)[] runs = _runs;
			for (int i = 0; i < runs.Length; i++)
			{
				(string, bool) tuple = runs[i];
				string item = tuple.Item1;
				bool item2 = tuple.Item2;
				string[] array = item.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				foreach (string text in array)
				{
					int num2 = Advance(text, FontOf(item2));
					int num3 = ((list[list.Count - 1].Count != 0) ? width2 : 0) + num2;
					if (list[list.Count - 1].Count > 0 && num + num3 > width)
					{
						list.Add(new List<(string, bool)>());
						num = 0;
						num3 = num2;
					}
					list[list.Count - 1].Add((text, item2));
					num += num3;
				}
			}
			return list;
		}

		internal int HeightFor(int width)
		{
			return S(1) + Pad + Wrap(Math.Max(1, width - Icon - IconGap)).Count * Line;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (SolidBrush brush = new SolidBrush(ControlInk.Border))
			{
				graphics.FillRectangle(brush, 0, 0, Width, ControlInk.Hairline(Tokens.Dpi));
			}
			int num = S(1) + Pad;
			SignalIcons.Draw(graphics, SignalIcon.Info, new RectangleF(0f, num, Icon, Icon), ControlInk.Info);
			int num2 = Icon + IconGap;
			int width = TextRenderer.MeasureText(" ", Fonts.Caption, Size.Empty, TextFormatFlags.NoPadding).Width;
			List<List<(string, bool)>> list = Wrap(Math.Max(1, Width - num2));
			for (int i = 0; i < list.Count; i++)
			{
				int num3 = num2;
				foreach (var item2 in list[i])
				{
					string item = item2.Item1;
					Font font = FontOf(item2.Item2);
					int num4 = Advance(item, font);
					TextRenderer.DrawText(graphics, item, font, new Rectangle(num3, num + i * Line, num4 + S(2), Line), Text3, TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
					num3 += num4 + width;
				}
			}
		}
	}

	private sealed class SavedChip : Control
	{
		private const int PreviewSize = 24;

		private const int DeleteWidth = 24;

		internal const int LogicalWidth = 190;

		internal const int LogicalHeight = 36;

		private readonly CrosshairConfig _config;

		private bool _hover;

		private bool _active;

		private float Px => (float)Height / 36f;

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
				}
			}
		}

		private Rectangle DeleteRect => new Rectangle(Width - (int)(24f * Px), 0, (int)(24f * Px), Height);

		public event EventHandler? LoadRequested;

		public event EventHandler? DeleteRequested;

		public SavedChip(CrosshairConfig config)
		{
			_config = config;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
			BackColor = Color.Transparent;
			Cursor = Cursors.Hand;
			Font = Fonts.Label;
			Size = new Size(190, 36);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			_hover = true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_hover = false;
			Invalidate();
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (e.Button == MouseButtons.Left)
			{
				if (DeleteRect.Contains(e.Location))
				{
					DeleteRequested?.Invoke(this, EventArgs.Empty);
				}
				else
				{
					LoadRequested?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = Glass.RoundedPath(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), Tokens.Scale(4)))
			{
				using (SolidBrush brush = new SolidBrush(_hover ? Theme.SurfaceHover : Theme.Surface))
				{
					graphics.FillPath(brush, path);
				}
				using Pen pen = new Pen(_active ? Theme.Accent : Theme.GlassEdge, 1f);
				graphics.DrawPath(pen, path);
			}
			int num = (int)(24f * Px);
			Rectangle target = new Rectangle((int)(7f * Px), (Height - num) / 2, num, num);
			CrosshairRender.Draw(graphics, _config, target);
			int num2 = target.Right + (int)(10f * Px);
			Rectangle bounds = new Rectangle(num2, 0, Width - num2 - DeleteRect.Width, Height);
			TextRenderer.DrawText(graphics, _config.Name, Font, bounds, Theme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
			int num3 = Tokens.Scale(12);
			LineIcons.Close(graphics, new Rectangle(DeleteRect.X + (DeleteRect.Width - num3) / 2, (Height - num3) / 2, num3, num3), Theme.TextDim);
		}
	}

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly CrosshairService _service;

	private readonly PreviewBox _preview;

	private readonly CardPanel _card;

	private readonly FlowLayoutPanel _savedFlow;

	private readonly Label _savedEmpty;

	private readonly Label _shareCaption;

	private readonly Label _shareHint;

	private readonly Label _shareStatus;

	private readonly GlassTextBox _codeBox;

	private readonly GlassButton _copyCode;

	private readonly GlassButton _applyCode;

	private readonly GuidanceNote _note;

	private GlassButton _useImageBtn;

	private GlassButton _clearImageBtn;

	private Label _imageNote;

	private readonly ToggleSwitch _enabled;

	private readonly Label _status;

	private readonly Label _onLabel;

	private readonly QuietScrollPanel _galleryHost;

	private readonly List<CrosshairCell> _cells = new List<CrosshairCell>();

	private readonly SegmentStrip _families;

	private CrosshairGallery.Family _family;

	private readonly CardPanel _right;

	private readonly QuietScrollPanel _leftScroll;

	private readonly QuietScrollPanel _rightScroll;

	private readonly Panel _moreOptionsFade;

	private readonly GlassButton _moreOptions;

	private FlatSlider _sizeSlider;

	private FlatSlider _thicknessSlider;

	private FlatSlider _gapSlider;

	private FlatSlider _dotSlider;

	private FlatSlider _circleSlider;

	private FlatSlider _opacitySlider;

	private FlatSlider _offsetXSlider;

	private FlatSlider _offsetYSlider;

	private FlatSlider _outlineSlider;

	private FlatSlider _imageScaleSlider;

	private FlatSlider _rotationSlider;

	private FlatSlider _dotOpacitySlider;

	private readonly List<ChipButton> _partChips = new List<ChipButton>();

	private readonly List<ChipButton> _shapeChips = new List<ChipButton>();

	private readonly List<OutlineSwatch> _outlineDots = new List<OutlineSwatch>();

	private bool _applyingPreset;

	private readonly List<SwatchDot> _colourDots = new List<SwatchDot>();

	private readonly ColourWheel _wheel;

	private readonly GlassTextBox _hexBox;

	private bool _syncingColour;

	private readonly List<(Label Label, FontRole Role)> _ownLabels = new List<(Label, FontRole)>();

	private const int PanelH = 508;

	private const int LeftW = 568;

	private const int RightW = 348;

	private const int PanelGap = 16;

	private const int Inset = 17;

	private const int ContentW = 534;

	private const int RightContentW = 314;

	private const int ColGap = 32;

	private const int ColW = 251;

	private const int Col1 = 17;

	private const int Col2 = 300;

	private const int PreviewH = 326;

	private const int LabelH = 20;

	private const int TrackGap = 2;

	private const int TrackH = 16;

	private const int RowH = 38;

	private const int RowPitch = 50;

	private const int SlidersY = 359;

	private const int ColourY = 463;

	private const int LabelBleed = 4;

	private const int SwatchBox = 28;

	private const int SwatchGap = 8;

	private const int WordGap = 8;

	private static readonly Color[] SwatchColours = new Color[6]
	{
		Color.White,
		Color.FromArgb(255, 0, 255, 102),
		Color.FromArgb(255, 0, 255, 255),
		Color.FromArgb(255, 255, 220, 0),
		Color.FromArgb(255, 255, 60, 60),
		Color.FromArgb(255, 255, 0, 170)
	};

	private const int FoldY = 516;

	private const int StripH = 32;

	private const int CellH = 72;

	private const int CellGap = 8;

	private const int GalleryRows = 3;

	private const int GalleryTop = 44;

	private CrosshairConfig _current;

	private readonly Label _opacityValue;

	private readonly IReadOnlyList<CrosshairGallery.Family> _familyOrder;

	private readonly Label _designsOnTheirWay;

	private bool _showDesignsOnTheirWay;

	private int _designsGeneration;

	private readonly List<(Label Name, Label Value, FlatSlider Slider, bool Second)> _rows = new List<(Label, Label, FlatSlider, bool)>();

	private readonly Label _opacityWord;

	private readonly Label _outlineWord;

	private readonly Label _shapeWord;

	private readonly Label _partsWord;

	private readonly Label _hexNote;

	private readonly Label _colourWord;

	private readonly Label _outlineColourWord;

	private readonly AddSwatch _anyColour;

	private readonly ToggleSwitch _outlineSwitch;

	private readonly GlassButton _saveBtn;

	private int _opacityRight;

	private const int MinLeftW = 430;

	private const int RightBleed = 4;

	private readonly int _wheelTop;

	private const int ReadoutW = 80;

	private const int ChipH = 32;

	private const int PartChipGap = 8;

	private const int PartChipW = 82;

	private const int ShapeChipW = 100;

	private bool _fittingPage;

	private bool _scaledByTheBase;

	private int _followedDpi = 96;

	private Font? _shareCodeFont;

	[ThreadStatic]
	internal static Action? RebuildingTheGalleryForTest;

	internal const int MaxSavedCrosshairs = 10;

	protected override string PageTitle => "Crosshair";

	protected override string PageSubtitle => "An overlay drawn on your screen, not in the game.";

	protected override bool UsesHeaderPanel => true;

	protected override Control[] HeaderActions => new Control[2] { _onLabel, _enabled };

	private static Color Text3
	{
		get
		{
			if (!(Theme.CurrentName == "Slate"))
			{
				return SignalTokens.Ash.Text3;
			}
			return SignalTokens.Slate.Text3;
		}
	}

	internal string? TestDesignsOnTheirWay
	{
		get
		{
			if (!_showDesignsOnTheirWay)
			{
				return null;
			}
			return _designsOnTheirWay.Text;
		}
	}

	internal Control TestDesignsOnTheirWayLine => _designsOnTheirWay;

	internal Control TestShareHint => _shareHint;

	internal IReadOnlyList<CrosshairGallery.Family> TestFamilies => _familyOrder;

	internal CrosshairGallery.Family TestFamily => _family;

	internal IReadOnlyList<CrosshairCell> TestCells => _cells;

	internal string TestOnWord => _onLabel.Text;

	internal CrosshairConfig CurrentCrosshair => _current;

	internal IReadOnlyList<CrosshairConfig> TestSavedCrosshairs => _settings.SavedCrosshairs;

	internal Control TestLeftPanel => _card;

	internal ScrollableControl TestLeftScroll => _leftScroll;

	internal Control TestRightPanel => _right;

	internal ScrollableControl TestRightScroll => _rightScroll;

	internal Control TestNote => _note;

	internal Control TestEnabledSwitch => _enabled;

	internal Control TestWheel => _wheel;

	internal int TestSliderCount => CountOf<FlatSlider>();

	internal int TestGalleryCellCount => _cells.Count;

	private static string ImageStoreDir => Path.Combine(AppFolders.Local, "crosshairs");

	public CrosshairPage(AppSettings settings, SettingsStore store, CrosshairService service)
	{
		_settings = settings;
		_store = store;
		_service = service;
		_current = service.Config.Clone();
		Font = Fonts.Body;
		AutoScroll = true;
		ContentWidth = 0;
		int y = 0;
		_enabled = new ToggleSwitch
		{
			AccessibleName = "Crosshair",
			Size = new Size(40, 20),
			Checked = _service.IsVisible
		};
		_enabled.CheckedChanged += (object? s, EventArgs e2) =>
		{
			if (_enabled.Checked)
			{
				_service.Show();
			}
			else
			{
				_service.Hide();
			}
			_settings.CrosshairEnabled = _enabled.Checked;
			_store.Save(_settings);
			UpdateStatus();
		};
		_onLabel = Own(new Label
		{
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleRight,
			Size = new Size(40, 20)
		}, FontRole.BodyBold);
		_card = new CardPanel
		{
			Location = new Point(16, y),
			Size = new Size(568, 508)
		};
		_leftScroll = new QuietScrollPanel
		{
			Location = new Point(0, 0),
			Size = new Size(568, 508),
			BackColor = Color.Transparent
		};
		_card.Controls.Add(_leftScroll);
		_moreOptionsFade = new Panel
		{
			BackColor = Color.Transparent
		};
		_moreOptionsFade.Paint += (object? _, PaintEventArgs e2) =>
		{
			if (_moreOptionsFade.Width <= 0 || _moreOptionsFade.Height <= 0)
			{
				return;
			}
			using LinearGradientBrush brush = new LinearGradientBrush(_moreOptionsFade.ClientRectangle, Color.FromArgb(0, Theme.Surface), Theme.Surface, LinearGradientMode.Vertical);
			e2.Graphics.FillRectangle(brush, _moreOptionsFade.ClientRectangle);
		};
		_moreOptions = new GlassButton
		{
			Name = "moreOptions",
			Text = "More options",
			Kind = GlassButtonKind.Quiet,
			TrailingIcon = SignalIcon.Chevron,
			Font = Fonts.Caption
		};
		_moreOptions.Click += (object? _, EventArgs _) =>
		{
			_leftScroll.AutoScrollPosition = new Point(0, Math.Max(0, S(516) - S(17)));
			UpdateMoreOptions();
			_leftScroll.Invalidate(invalidateChildren: true);
		};
		_moreOptionsFade.Controls.Add(_moreOptions);
		_card.Controls.Add(_moreOptionsFade);
		_leftScroll.Scroll += (object? _, ScrollEventArgs _) =>
		{
			UpdateMoreOptions();
		};
		_leftScroll.Layout += (object? _, LayoutEventArgs _) =>
		{
			UpdateMoreOptions();
		};
		_leftScroll.Invalidated += (object? _, InvalidateEventArgs _) =>
		{
			UpdateMoreOptions();
		};
		_leftScroll.Paint += (object? _, PaintEventArgs _) =>
		{
			UpdateMoreOptions();
		};
		Controls.Add(_card);
		_right = new CardPanel
		{
			Location = new Point(600, y),
			Size = new Size(348, 508)
		};
		_rightScroll = new QuietScrollPanel
		{
			Location = new Point(17, 17),
			Size = new Size(314, 474),
			BackColor = Color.Transparent
		};
		_right.Controls.Add(_rightScroll);
		Controls.Add(_right);
		QuietScrollPanel leftScroll = _leftScroll;
		_preview = new PreviewBox
		{
			Location = new Point(17, 17),
			Size = new Size(534, 326)
		};
		leftScroll.Controls.Add(_preview);
		_sizeSlider = AddRow(leftScroll, "Size", 17, 359, 5, 300, (int)Math.Round(_current.ResolvedSize * 10f), Tenths, (int v) =>
		{
			_current.SetSizeTenths(v);
			OnSliderMoved();
		});
		_thicknessSlider = AddRow(leftScroll, "Thickness", 300, 359, 5, 100, (int)Math.Round(_current.ResolvedThickness * 10f), Tenths, (int v) =>
		{
			_current.SetThicknessTenths(v);
			OnSliderMoved();
		});
		_gapSlider = AddRow(leftScroll, "Gap", 17, 409, 0, 300, (int)Math.Round(_current.ResolvedGap * 10f), Tenths, (int v) =>
		{
			_current.SetGapTenths(v);
			OnSliderMoved();
		});
		_outlineSlider = AddRow(leftScroll, "Outline thickness", 300, 409, 0, 40, _current.OutlineThicknessTenths ?? 10, Tenths, (int v) =>
		{
			_current.OutlineThicknessTenths = v;
			OnSliderMoved();
		});
		int y2 = 467;
		_colourWord = Own(new Label
		{
			Text = "Colour",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(13, y2, 60, 20)
		}, FontRole.Body);
		leftScroll.Controls.Add(_colourWord);
		Color[] swatchColours = SwatchColours;
		for (int num = 0; num < swatchColours.Length; num++)
		{
			Color color = swatchColours[num];
			SwatchDot swatchDot = new SwatchDot(color)
			{
				Location = new Point(17, 463),
				Active = ((color.ToArgb() & 0xFFFFFF) == (_current.ColourArgb & 0xFFFFFF))
			};
			Color captured = color;
			swatchDot.Click += (object? s, EventArgs e2) =>
			{
				ApplyColour(captured, null);
			};
			_colourDots.Add(swatchDot);
			leftScroll.Controls.Add(swatchDot);
		}
		_anyColour = new AddSwatch
		{
			Location = new Point(17, 463)
		};
		_anyColour.Click += (object? _, EventArgs _) =>
		{
			ShowTheColourPicker();
		};
		leftScroll.Controls.Add(_anyColour);
		_opacityValue = Own(new Label
		{
			Text = $"{_current.Opacity}%",
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(17, y2, 60, 20)
		}, FontRole.BodyBold);
		leftScroll.Controls.Add(_opacityValue);
		_opacityWord = Own(new Label
		{
			Text = "Opacity",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(17, y2, 70, 20)
		}, FontRole.Body);
		leftScroll.Controls.Add(_opacityWord);
		int num2 = 516;
		_opacitySlider = AddRow(leftScroll, "Opacity", 300, num2, 10, 100, Math.Clamp(_current.Opacity, 10, 100), (int v) => $"{v}%", (int v) =>
		{
			_opacityValue.Text = $"{v}%";
			LayOutTheOpacityReadout();
			_current.Opacity = v;
			OnSliderMoved();
			RefreshGalleryPreviews();
		});
		_opacitySlider.Name = "opacitySlider";
		num2 += 50;
		_shapeWord = RowLabel("Shape", 17, num2, 534);
		leftScroll.Controls.Add(_shapeWord);
		int x = 17;
		foreach (CrosshairShapeKind item2 in CrosshairShapeKinds.All)
		{
			AddShape(num2 + 20 + 8, ref x, item2);
		}
		num2 += 76;
		_partsWord = RowLabel("Parts", 17, num2, 534);
		leftScroll.Controls.Add(_partsWord);
		int x2 = 17;
		int y3 = num2 + 20 + 8;
		AddPart(y3, ref x2, "Up", () => _current.ResolvedArmTop, (bool v) =>
		{
			_current.ArmTop = v;
		});
		AddPart(y3, ref x2, "Down", () => _current.ResolvedArmBottom, (bool v) =>
		{
			_current.ArmBottom = v;
		});
		AddPart(y3, ref x2, "Left", () => _current.ResolvedArmLeft, (bool v) =>
		{
			_current.ArmLeft = v;
		});
		AddPart(y3, ref x2, "Right", () => _current.ResolvedArmRight, (bool v) =>
		{
			_current.ArmRight = v;
		});
		AddPart(y3, ref x2, "Dot", () => _current.ResolvedCentreDot, (bool v) =>
		{
			_current.CentreDot = v;
		});
		AddPart(y3, ref x2, "Ring", () => _current.ResolvedShowCircle, (bool v) =>
		{
			_current.ShowCircle = v;
		});
		num2 += 76;
		_rotationSlider = AddRow(leftScroll, "Rotation", 17, num2, 0, 90, _current.ResolvedRotationDegrees, Degrees, (int v) =>
		{
			_current.SetRotationDegrees(v);
			OnSliderMoved();
		});
		_rotationSlider.Name = "rotationSlider";
		_dotSlider = AddRow(leftScroll, "Dot size", 300, num2, 5, 100, (int)Math.Round(_current.ResolvedDotSize * 10f), Tenths, (int v) =>
		{
			_current.DotSizeTenths = v;
			OnSliderMoved();
		});
		num2 += 50;
		_dotOpacitySlider = AddRow(leftScroll, "Dot opacity", 17, num2, 10, 100, _current.ResolvedDotOpacity, (int v) => $"{v}%", (int v) =>
		{
			_current.DotOpacity = v;
			OnSliderMoved();
		});
		_dotOpacitySlider.Name = "dotOpacitySlider";
		_circleSlider = AddRow(leftScroll, "Ring size", 300, num2, 10, 400, (int)Math.Round(_current.ResolvedCircleRadius * 10f), Tenths, (int v) =>
		{
			_current.CircleRadiusTenths = v;
			OnSliderMoved();
		});
		num2 = (_wheelTop = num2 + 58);
		_wheel = new ColourWheel
		{
			Location = new Point(17, num2),
			Size = new Size(196, 130),
			Colour = Color.FromArgb(_current.ColourArgb)
		};
		_wheel.ColourChanged += (object? _, EventArgs _) =>
		{
			ApplyColour(_wheel.Colour, _wheel);
		};
		leftScroll.Controls.Add(_wheel);
		int num3 = 249;
		leftScroll.Controls.Add(RowLabel("Hex", num3, num2 + 4, 120));
		_hexBox = new GlassTextBox
		{
			AccessibleName = "Hex",
			Name = "hexBox",
			Location = new Point(num3, num2 + 28),
			Size = new Size(110, 32)
		};
		_hexBox.Inner.Font = Fonts.Mono;
		_hexBox.Inner.AccessibleName = "Hex";
		_hexBox.Inner.CharacterCasing = CharacterCasing.Upper;
		_hexBox.Inner.MaxLength = 7;
		_hexBox.Text = ColourWheel.ToHex(Color.FromArgb(_current.ColourArgb));
		_hexBox.TextChanged += (object? s, EventArgs e2) =>
		{
			if (ColourWheel.TryParseHex(_hexBox.Text, out var colour))
			{
				ApplyColour(colour, _hexBox);
			}
		};
		leftScroll.Controls.Add(_hexBox);
		_hexNote = Own(new Label
		{
			Text = "Paste a colour to match a friend's crosshair.",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Location = new Point(num3, num2 + 68),
			Size = new Size(551 - num3, 34)
		}, FontRole.Caption);
		leftScroll.Controls.Add(_hexNote);
		num2 += 150;
		_outlineWord = Own(new Label
		{
			Text = "Outline",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(13, num2, 488, 20)
		}, FontRole.Body);
		leftScroll.Controls.Add(_outlineWord);
		ToggleSwitch outline = new ToggleSwitch
		{
			AccessibleName = "Outline",
			Name = "outlineSwitch",
			Size = new Size(40, 20),
			Location = new Point(511, num2),
			Checked = _current.Outline
		};
		outline.CheckedChanged += (object? s, EventArgs e2) =>
		{
			_current.Outline = outline.Checked;
			if (!_applyingPreset)
			{
				Push();
			}
		};
		leftScroll.Controls.Add(outline);
		_outlineSwitch = outline;
		num2 += 50;
		_outlineColourWord = Own(new Label
		{
			Text = "Outline colour",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(13, num2 + 4, 110, 20)
		}, FontRole.Body);
		leftScroll.Controls.Add(_outlineColourWord);
		foreach (Color item3 in new Color[1] { Color.Black }.Concat(SwatchColours))
		{
			OutlineSwatch outlineSwatch = new OutlineSwatch(item3)
			{
				Location = new Point(17, num2)
			};
			Color captured2 = item3;
			outlineSwatch.Click += (object? _, EventArgs _) =>
			{
				ApplyOutlineColour(captured2);
			};
			_outlineDots.Add(outlineSwatch);
			leftScroll.Controls.Add(outlineSwatch);
		}
		SyncOutlineDots();
		num2 += 50;
		_offsetXSlider = AddRow(leftScroll, "Offset X", 17, num2, -200, 200, _current.ResolvedOffsetX, (int v) => $"{v} px", (int v) =>
		{
			_current.OffsetXPx = v;
			OnSliderMoved();
		});
		_offsetYSlider = AddRow(leftScroll, "Offset Y", 300, num2, -200, 200, _current.ResolvedOffsetY, (int v) => $"{v} px", (int v) =>
		{
			_current.OffsetYPx = v;
			OnSliderMoved();
		});
		num2 += 50;
		_imageScaleSlider = AddRow(leftScroll, "Image size", 17, num2, 25, 400, _current.ImageScalePercent, (int v) => $"{v}%", (int v) =>
		{
			_current.ImageScalePercent = v;
			OnSliderMoved();
		});
		_imageScaleSlider.Name = "imageScaleSlider";
		num2 += 58;
		_useImageBtn = Button("Use an image…", 17, num2, 140);
		_useImageBtn.Click += (object? _, EventArgs _) =>
		{
			ImportImage();
		};
		leftScroll.Controls.Add(_useImageBtn);
		_clearImageBtn = Button("Back to shapes", 167, num2, 130);
		_clearImageBtn.Click += (object? _, EventArgs _) =>
		{
			_current.ImagePath = null;
			Push();
			SyncImageControls();
		};
		_clearImageBtn.Visible = false;
		leftScroll.Controls.Add(_clearImageBtn);
		_imageNote = Own(new Label
		{
			Text = "",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Location = new Point(17, num2 + 40),
			AutoSize = true,
			MaximumSize = new Size(534, 0)
		}, FontRole.Caption);
		leftScroll.Controls.Add(_imageNote);
		num2 += 76;
		_status = Own(new Label
		{
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Location = new Point(17, num2),
			Size = new Size(534, 34)
		}, FontRole.Caption);
		leftScroll.Controls.Add(_status);
		num2 += 46;
		leftScroll.Controls.Add(RowLabel("Saved", 17, num2, 200));
		GlassButton glassButton = Button("Save as…", 421, num2 + 20 + 4, 130);
		glassButton.Click += (object? s, EventArgs e2) =>
		{
			SaveCurrent();
		};
		leftScroll.Controls.Add(glassButton);
		_saveBtn = glassButton;
		_savedFlow = new FlowLayoutPanel
		{
			Location = new Point(17, num2 + 20 + 4),
			Size = new Size(394, 36),
			MaximumSize = new Size(394, 0),
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			WrapContents = true,
			BackColor = Color.Transparent
		};
		leftScroll.Controls.Add(_savedFlow);
		_savedEmpty = Own(new Label
		{
			Text = "No saved crosshairs yet — tweak, then Save as…",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Location = new Point(17, num2 + 20 + 4),
			Size = new Size(394, 32),
			TextAlign = ContentAlignment.MiddleLeft
		}, FontRole.Caption);
		leftScroll.Controls.Add(_savedEmpty);
		List<CrosshairGallery.Family> list = new List<CrosshairGallery.Family>
		{
			CrosshairGallery.Family.Dot,
			CrosshairGallery.Family.Cross,
			CrosshairGallery.Family.X,
			CrosshairGallery.Family.Ring
		};
		CrosshairGallery.Family[] values = Enum.GetValues<CrosshairGallery.Family>();
		foreach (CrosshairGallery.Family item in values)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		_designsGeneration = ServedCrosshairs.Generation;
		IReadOnlyList<CrosshairGallery.GalleryItem> designsAtOpening = CrosshairGallery.All;
		if (designsAtOpening.Count > 0)
		{
			list.RemoveAll((CrosshairGallery.Family family) => !designsAtOpening.Any((CrosshairGallery.GalleryItem i) => i.Group == family));
		}
		_familyOrder = list;
		_family = OpeningFamily();
		_families = new SegmentStrip(list.Select(FamilyWord).ToArray())
		{
			AccessibleName = "Crosshair family",
			Location = new Point(0, 0),
			Size = new Size(314, 32),
			Active = FamilyWord(_family),
			ArrowsChoose = true
		};
		_families.Chosen += (object? _, string word) =>
		{
			_family = _familyOrder.First((CrosshairGallery.Family family) => FamilyWord(family) == word);
			_families.Active = word;
			LayOutGallery();
		};
		_rightScroll.Controls.Add(_families);
		_galleryHost = new QuietScrollPanel
		{
			Location = new Point(0, 44),
			Size = new Size(314, 232)
		};
		_rightScroll.Controls.Add(_galleryHost);
		_designsOnTheirWay = Own(new Label
		{
			Text = "",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			Visible = false
		}, FontRole.Caption);
		_rightScroll.Controls.Add(_designsOnTheirWay);
		_shareCaption = Own(new Label
		{
			Text = "Share code",
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft
		}, FontRole.BodyBold);
		_rightScroll.Controls.Add(_shareCaption);
		_shareHint = Own(new Label
		{
			Text = "Paste one from a friend",
			ForeColor = Text3,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft
		}, FontRole.Caption);
		_rightScroll.Controls.Add(_shareHint);
		_codeBox = new GlassTextBox
		{
			Name = "shareCodeBox",
			CharacterCasing = CharacterCasing.Upper,
			PlaceholderText = "Paste a code, e.g. PXC-…"
		};
		UseShareCodeFont();
		_rightScroll.Controls.Add(_codeBox);
		_copyCode = new GlassButton
		{
			Text = "Copy",
			Icon = SignalIcon.Copy,
			Kind = GlassButtonKind.Secondary,
			Height = 32
		};
		_copyCode.Click += (object? _, EventArgs _) =>
		{
			CopyMyCode();
		};
		_rightScroll.Controls.Add(_copyCode);
		_applyCode = new GlassButton
		{
			Text = "Apply",
			Kind = GlassButtonKind.Primary,
			Height = 32
		};
		_applyCode.Click += (object? _, EventArgs _) =>
		{
			ApplySharedCode();
		};
		_rightScroll.Controls.Add(_applyCode);
		_shareStatus = Own(new Label
		{
			Text = "",
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft
		}, FontRole.Caption);
		_rightScroll.Controls.Add(_shareStatus);
		_note = new GuidanceNote("CS2 hides overlays unless you add ", "-allow_third_party_software", " to its launch options.");
		_rightScroll.Controls.Add(_note);
		BuildGallery();
		LayOutTheRightPanel();
		LayOutAcrossTheWidth();
		Resize += (object? _, EventArgs _) =>
		{
			LayOutAcrossTheWidth();
		};
		RefreshSavedList();
		SyncImageControls();
		RovingFocus.Attach(_colourDots.Cast<Control>().Append(_anyColour), (Control control) => control is SwatchDot swatchDot2 && swatchDot2.Active, (Control control) => control is SwatchDot);
		RovingFocus.Attach(_outlineDots, (Control control) => control is SwatchDot swatchDot2 && swatchDot2.Active);
		RovingFocus.Attach(_shapeChips, (Control control) => control is ChipButton chipButton && chipButton.Active);
		Push();
	}

	private T Own<T>(T label, FontRole role) where T : Label
	{
		label.Font = Fonts.For(role);
		label.UseMnemonic = false;
		_ownLabels.Add((label, role));
		return label;
	}

	private Label RowLabel(string text, int x, int y, int w)
	{
		return Own(new Label
		{
			Text = text,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(x - 4, y, w + 4, 20)
		}, FontRole.Body);
	}

	private static int Advance(string text, Font font)
	{
		return TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
	}

	private static int InkInsetOf(Label l)
	{
		if (!string.IsNullOrEmpty(l.Text))
		{
			return Math.Max(0, (TextRenderer.MeasureText(l.Text, l.Font).Width - Advance(l.Text, l.Font)) / 2);
		}
		return 0;
	}

	private static void PlaceInk(Label l, int inkLeft)
	{
		l.SetBounds(inkLeft - InkInsetOf(l), l.Top, TextRenderer.MeasureText(l.Text, l.Font).Width, l.Height);
	}

	private static string FamilyWord(CrosshairGallery.Family family)
	{
		if (family != CrosshairGallery.Family.Ring)
		{
			return family.ToString();
		}
		return "Circle";
	}

	private static CrosshairGallery.Family? FamilyForShape(CrosshairConfig c)
	{
		switch (CrosshairShapeKinds.Of(c))
		{
		case CrosshairShapeKind.Cross:
		case CrosshairShapeKind.T:
			if (c.ResolvedRotationDegrees == 45)
			{
				return CrosshairGallery.Family.X;
			}
			return CrosshairGallery.Family.Cross;
		case CrosshairShapeKind.Circle:
			return CrosshairGallery.Family.Ring;
		case CrosshairShapeKind.Dot:
			return CrosshairGallery.Family.Dot;
		default:
			return null;
		}
	}

	private static string Tenths(int tenths)
	{
		return ((float)tenths / 10f).ToString("0.#", CultureInfo.InvariantCulture) + " px";
	}

	private static string Degrees(int degrees)
	{
		return degrees.ToString(CultureInfo.InvariantCulture) + "°";
	}

	private FlatSlider AddRow(Control parent, string label, int x, int y, int min, int max, int value, Func<int, string> format, Action<int> onChange)
	{
		Label label2 = Own(new Label
		{
			Text = label,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft,
			Bounds = new Rectangle(x - 4, y, 175, 20)
		}, FontRole.Body);
		parent.Controls.Add(label2);
		Label readout = Own(new Label
		{
			Text = format(value),
			ForeColor = Theme.Text,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleRight,
			Bounds = new Rectangle(x + 251 - 80, y, 84, 20)
		}, FontRole.BodyBold);
		parent.Controls.Add(readout);
		FlatSlider slider = new FlatSlider
		{
			AccessibleName = label,
			Minimum = min,
			Maximum = max,
			Value = Math.Clamp(value, min, max)
		};
		slider.SetTrackBounds(x, y + 20 + 2, 251, 16);
		slider.ValueChanged += (object? s, EventArgs e2) =>
		{
			readout.Text = format(slider.Value);
			onChange(slider.Value);
		};
		parent.Controls.Add(slider);
		_rows.Add((label2, readout, slider, x == 300));
		return slider;
	}

	private void AddPart(int y, ref int x, string label, Func<bool> get, Action<bool> set)
	{
		ChipButton chip = new ChipButton
		{
			Text = label,
			Location = new Point(x, y),
			Size = new Size(82, 32),
			Font = Fonts.Caption,
			Active = get()
		};
		chip.Click += (object? _, EventArgs _) =>
		{
			set(!chip.Active);
			chip.Active = get();
			Push();
			HighlightActivePreset();
		};
		_partChips.Add(chip);
		_leftScroll.Controls.Add(chip);
		x += 90;
	}

	private void AddShape(int y, ref int x, CrosshairShapeKind kind)
	{
		ChipButton chipButton = new ChipButton
		{
			Text = ShapeLabel(kind),
			Location = new Point(x, y),
			Size = new Size(100, 32),
			Font = Fonts.Caption,
			AccessibleRole = AccessibleRole.RadioButton,
			Active = (CrosshairShapeKinds.Of(_current) == kind)
		};
		chipButton.Click += (object? _, EventArgs _) =>
		{
			CrosshairShapeKinds.Apply(_current, kind);
			Push();
			HighlightActivePreset();
		};
		_shapeChips.Add(chipButton);
		_leftScroll.Controls.Add(chipButton);
		x += 108;
	}

	private static string ShapeLabel(CrosshairShapeKind kind)
	{
		return kind switch
		{
			CrosshairShapeKind.Cross => "Cross", 
			CrosshairShapeKind.T => "T", 
			CrosshairShapeKind.Circle => "Circle", 
			CrosshairShapeKind.Dot => "Dot only", 
			CrosshairShapeKind.Chevron => "Chevron", 
			_ => kind.ToString(), 
		};
	}

	private void RefreshPartChips()
	{
		if (_partChips.Count >= 6)
		{
			_partChips[0].Active = _current.ResolvedArmTop;
			_partChips[1].Active = _current.ResolvedArmBottom;
			_partChips[2].Active = _current.ResolvedArmLeft;
			_partChips[3].Active = _current.ResolvedArmRight;
			_partChips[4].Active = _current.ResolvedCentreDot;
			_partChips[5].Active = _current.ResolvedShowCircle;
		}
	}

	private void RefreshShapeChips()
	{
		CrosshairShapeKind? crosshairShapeKind = CrosshairShapeKinds.Of(_current);
		for (int i = 0; i < _shapeChips.Count; i++)
		{
			_shapeChips[i].Active = crosshairShapeKind == CrosshairShapeKinds.All[i];
		}
	}

	private void ShowTheColourPicker()
	{
		int y = Math.Max(0, S(_wheelTop) - S(17));
		_leftScroll.AutoScrollPosition = new Point(0, y);
		UpdateMoreOptions();
		_leftScroll.Invalidate(invalidateChildren: true);
	}

	internal void TestShowTheColourPicker()
	{
		ShowTheColourPicker();
	}

	private void UpdateMoreOptions()
	{
		if (_moreOptionsFade != null)
		{
			int num = _leftScroll.DisplayRectangle.Height - _leftScroll.ClientSize.Height;
			_moreOptionsFade.Visible = num > 0 && -_leftScroll.AutoScrollPosition.Y < num;
		}
	}

	private void FitPageToCards()
	{
		if (!_scaledByTheBase || !Visible || _fittingPage)
		{
			return;
		}
		_fittingPage = true;
		try
		{
			FitScrollToContent(0);
			if (AutoScrollMinSize.Height <= ClientSize.Height && DisplayRectangle.Height > ClientSize.Height)
			{
				AutoScroll = false;
				AutoScroll = true;
			}
		}
		finally
		{
			_fittingPage = false;
		}
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		base.OnVisibleChanged(e);
		if (Visible && _scaledByTheBase)
		{
			LayOutAcrossTheWidth();
		}
	}

	private int S(int logical)
	{
		if (!_scaledByTheBase)
		{
			return logical;
		}
		return (int)Math.Round((float)logical * ((float)Tokens.Dpi / 96f));
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		_scaledByTheBase = true;
		_followedDpi = Tokens.Dpi;
		LayOutTheRightPanel();
		LayOutAcrossTheWidth();
		RefreshSavedList();
	}

	private void FollowTheMonitor()
	{
		if (!_scaledByTheBase || IsDisposed)
		{
			return;
		}
		int dpi = Tokens.Dpi;
		if (dpi == _followedDpi)
		{
			return;
		}
		ApplyDpi(dpi);
		_followedDpi = dpi;
		foreach (var ownLabel in _ownLabels)
		{
			Label item = ownLabel.Label;
			FontRole item2 = ownLabel.Role;
			item.Font = Fonts.For(item2);
		}
		UseShareCodeFont();
		_hexBox.Inner.Font = Fonts.Mono;
		LayOutTheRightPanel();
		RefreshSavedList();
	}

	private void UseShareCodeFont()
	{
		Font font = Fonts.Of(FontRole.Caption, Fonts.MonoFamily);
		_codeBox.Inner.Font = font;
		if (_codeBox.Inner.Font != font)
		{
			font.Dispose();
			return;
		}
		_shareCodeFont?.Dispose();
		_shareCodeFont = font;
		_codeBox.PerformLayout();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing)
		{
			_shareCodeFont?.Dispose();
			_shareCodeFont = null;
		}
	}

	private void LayOutAcrossTheWidth()
	{
		FollowTheMonitor();
		int num = S(16);
		int num2 = Width + 1 - num;
		int num3 = S(348);
		int num4 = Math.Clamp(num2 - num - S(16) - num3, S(430), S(568));
		int y = ((_scaledByTheBase && HasHeaderPanel) ? (ContentTop + AutoScrollPosition.Y) : _card.Top);
		_card.SetBounds(num, y, num4, S(508));
		_leftScroll.SetBounds(0, 0, num4, S(492));
		_moreOptionsFade.SetBounds(S(1), S(492), num4 - S(2), S(15));
		_moreOptions.SetBounds((_moreOptionsFade.Width - S(136)) / 2, 0, S(136), S(15));
		_moreOptions.Font = Fonts.Caption;
		LayOutTheLeftPanel(num4);
		_right.SetBounds(num + num4 + S(16), y, num3, S(508));
		int num5 = S(4);
		_rightScroll.SetBounds(S(17) - num5, S(17), num3 - 2 * S(17) + 2 * num5, S(508) - 2 * S(17));
		LayOutTheRightPanel();
		FitPageToCards();
		UpdateMoreOptions();
	}

	private void LayOutTheRightPanel()
	{
		int num = S(4);
		int num2 = _rightScroll.Width - 2 * num;
		if (num2 <= 0)
		{
			return;
		}
		int y = _rightScroll.AutoScrollPosition.Y;
		_families.SetBounds(num, y, Math.Min(num2, _families.NaturalWidth), S(32));
		IReadOnlyList<CrosshairGallery.GalleryItem> designs = CrosshairGallery.All;
		int num3 = Math.Clamp((_familyOrder.Max((CrosshairGallery.Family family) => Enumerable.Count(designs, (CrosshairGallery.GalleryItem i) => i.Group == family)) + 3) / 4, 1, 3);
		int num4 = num3 * 72 + (num3 - 1) * 8;
		_galleryHost.SetBounds(num, S(44) + y, num2, S(num4));
		int num5 = S(44 + num4 + 16);
		int num6 = S(4);
		if (_showDesignsOnTheirWay)
		{
			int num7 = Math.Max(S(20), TextRenderer.MeasureText(_designsOnTheirWay.Text, _designsOnTheirWay.Font, new Size(num2, int.MaxValue), TextFormatFlags.WordBreak).Height);
			_designsOnTheirWay.SetBounds(num - num6, y, num2 + num6, num7);
			num5 = num7 + S(16);
		}
		else
		{
			_designsOnTheirWay.SetBounds(num - num6, y, num2 + num6, 0);
		}
		int num8 = S(24);
		_shareHint.Top = num5 + y;
		_shareHint.Height = num8;
		PlaceInk(_shareHint, num + num2 - Advance(_shareHint.Text, _shareHint.Font));
		_shareCaption.Top = num5 + y;
		_shareCaption.Height = num8;
		PlaceInk(_shareCaption, num);
		_shareCaption.Width = Math.Min(_shareCaption.Width, Math.Max(S(40), _shareHint.Left - _shareCaption.Left));
		num5 += num8 + S(12);
		_codeBox.SetBounds(num, num5 + y, num2, S(32));
		num5 += S(32) + S(8);
		int width = _copyCode.GetPreferredSize(Size.Empty).Width;
		int width2 = _applyCode.GetPreferredSize(Size.Empty).Width;
		_copyCode.SetBounds(num, num5 + y, width, S(32));
		_applyCode.SetBounds(num + width + S(8), num5 + y, width2, S(32));
		num5 += S(32) + S(4);
		int num9 = S(4);
		_shareStatus.SetBounds(num - num9, num5 + y, num2 + num9, S(32));
		num5 += S(32) + S(12);
		int num10 = _note.HeightFor(num2);
		int height = _rightScroll.ClientSize.Height;
		int num11 = Math.Max(num5, height - num10);
		_note.SetBounds(num, num11 + y, num2, num10);
		_rightScroll.AutoScrollMinSize = new Size(0, num11 + num10);
		LayOutGallery();
	}

	private void LayOutTheLeftPanel(int panelW)
	{
		int num = Math.Max(S(200), panelW - 2 * S(17));
		int num2 = (num - S(32)) / 2;
		int num3 = S(17);
		int num4 = num3 + num2 + S(32);
		int num5 = S(4);
		int num6 = S(80);
		_preview.SetBounds(num3, _preview.Top, num, _preview.Height);
		foreach (var row in _rows)
		{
			Label item = row.Name;
			Label item2 = row.Value;
			FlatSlider item3 = row.Slider;
			int num7 = (row.Second ? num4 : num3);
			int num8 = InkInsetOf(item);
			int num9 = InkInsetOf(item2);
			item.SetBounds(num7 - num8, item.Top, num2 - num6 + num8, item.Height);
			item2.SetBounds(num7 + num2 - num6, item2.Top, num6 + num9, item2.Height);
			item3.SetTrackBounds(num7, item3.Top, num2, item3.Height);
		}
		LayOutASwatchRow(_colourWord, _colourDots.Cast<Control>().Append(_anyColour), num3);
		LayOutASwatchRow(_outlineColourWord, _outlineDots, num3);
		_opacityRight = num3 + num;
		LayOutTheOpacityReadout();
		_outlineWord.SetBounds(num3 - num5, _outlineWord.Top, num - S(50) + num5, _outlineWord.Height);
		_outlineSwitch.Left = num3 + num - _outlineSwitch.Width;
		_shapeWord.Width = num + num5;
		_partsWord.Width = num + num5;
		int num10 = S(8);
		int num11 = (num - 4 * num10) / 5;
		for (int i = 0; i < _shapeChips.Count; i++)
		{
			_shapeChips[i].SetBounds(num3 + i * (num11 + num10), _shapeChips[i].Top, num11, _shapeChips[i].Height);
		}
		for (int j = 0; j < _partChips.Count; j++)
		{
			int num12 = (num + num10) * j / 6;
			int num13 = (num + num10) * (j + 1) / 6 - num10;
			_partChips[j].SetBounds(num3 + num12, _partChips[j].Top, num13 - num12, _partChips[j].Height);
		}
		_hexNote.Width = Math.Max(S(40), num3 + num - _hexNote.Left);
		_imageNote.MaximumSize = new Size(num, 0);
		_status.Width = num;
		_saveBtn.Left = num3 + num - _saveBtn.Width;
		_savedFlow.MaximumSize = new Size(Math.Max(S(40), num - S(140)), 0);
		_savedEmpty.Width = Math.Max(S(40), num - S(140));
	}

	private void LayOutASwatchRow(Label word, IEnumerable<Control> boxes, int c1)
	{
		PlaceInk(word, c1);
		int num = c1 + Advance(word.Text, word.Font) + S(8);
		int num2 = S(28);
		int num3 = num2 + S(8);
		foreach (Control box in boxes)
		{
			box.SetBounds(num, box.Top, num2, num2);
			num += num3;
		}
	}

	private void LayOutTheOpacityReadout()
	{
		if (_opacityRight > 0)
		{
			int num = _opacityRight - Advance(_opacityValue.Text, _opacityValue.Font);
			PlaceInk(_opacityValue, num);
			PlaceInk(_opacityWord, num - S(8) - Advance(_opacityWord.Text, _opacityWord.Font));
		}
	}

	private CrosshairGallery.Family OpeningFamily()
	{
		CrosshairGallery.GalleryItem? galleryItem = CrosshairGallery.Matching(_current);
		CrosshairGallery.Family? family = (((object)galleryItem != null) ? new CrosshairGallery.Family?(galleryItem.Group) : FamilyForShape(_current));
		if (family.HasValue)
		{
			CrosshairGallery.Family valueOrDefault = family.GetValueOrDefault();
			if (_familyOrder.Contains(valueOrDefault))
			{
				return valueOrDefault;
			}
		}
		return _familyOrder[0];
	}

	private void DecideWhereTheDesignsAre(bool noDesigns)
	{
		_showDesignsOnTheirWay = noDesigns && ServedCrosshairs.OnTheirWay;
		_families.Visible = !_showDesignsOnTheirWay;
		_galleryHost.Visible = !_showDesignsOnTheirWay;
		_designsOnTheirWay.Visible = _showDesignsOnTheirWay;
		_designsOnTheirWay.Text = (_showDesignsOnTheirWay ? DesignsWaitingWords() : "");
	}

	private static string DesignsWaitingWords()
	{
		return PcClock.WaitingWords("Crosshair designs are on the way. They load when PlexusX reaches our server.");
	}

	internal void ReloadServedDesigns()
	{
		int generation = ServedCrosshairs.Generation;
		if (generation == _designsGeneration)
		{
			if (_showDesignsOnTheirWay && _designsOnTheirWay.Text != DesignsWaitingWords())
			{
				_designsOnTheirWay.Text = DesignsWaitingWords();
				LayOutTheRightPanel();
				Invalidate(invalidateChildren: true);
			}
			return;
		}
		if (_cells.Count == 0 && CrosshairGallery.All.Count > 0)
		{
			_family = OpeningFamily();
			_families.Active = FamilyWord(_family);
		}
		RebuildingTheGalleryForTest?.Invoke();
		BuildGallery();
		LayOutTheRightPanel();
		_designsGeneration = generation;
		Invalidate(invalidateChildren: true);
	}

	internal void TestChooseFamily(CrosshairGallery.Family family)
	{
		_families.TestChoose(FamilyWord(family));
	}

	private void BuildGallery()
	{
		foreach (CrosshairCell cell in _cells)
		{
			cell.Dispose();
		}
		_galleryHost.Controls.Clear();
		_cells.Clear();
		List<string> list = _settings.FavouriteCrosshairs ?? new List<string>();
		IReadOnlyList<CrosshairGallery.GalleryItem> all = CrosshairGallery.All;
		DecideWhereTheDesignsAre(all.Count == 0);
		if (all.Count > 0)
		{
			HashSet<string> galleryIds = new HashSet<string>(all.Select((CrosshairGallery.GalleryItem g) => g.Id), StringComparer.Ordinal);
			if (list.RemoveAll((string id) => !galleryIds.Contains(id)) > 0)
			{
				_store.Save(_settings);
			}
		}
		HashSet<string> favouriteSet = new HashSet<string>(list, StringComparer.Ordinal);
		foreach (CrosshairGallery.GalleryItem item in all.Where((CrosshairGallery.GalleryItem i) => favouriteSet.Contains(i.Id)).Concat(all.Where((CrosshairGallery.GalleryItem i) => !favouriteSet.Contains(i.Id))))
		{
			CrosshairCell crosshairCell = new CrosshairCell(item)
			{
				PreviewStyle = _current,
				Favourite = favouriteSet.Contains(item.Id)
			};
			CrosshairGallery.GalleryItem captured = item;
			crosshairCell.Click += (object? _, EventArgs _) =>
			{
				ApplyGalleryItem(captured);
			};
			crosshairCell.FavouriteToggled += (object? _, EventArgs _) =>
			{
				ToggleFavourite(captured);
			};
			_cells.Add(crosshairCell);
			_galleryHost.Controls.Add(crosshairCell);
		}
		LayOutGallery();
		HighlightActivePreset();
	}

	private void LayOutGallery()
	{
		int width = _galleryHost.Width;
		if (width <= 0)
		{
			return;
		}
		int num = S(8);
		int num2 = S(72);
		double num3 = (double)(width + num) / 4.0;
		_galleryHost.SuspendLayout();
		int num4 = 0;
		foreach (CrosshairCell cell in _cells)
		{
			if (cell.Visible = cell.Item.Group == _family)
			{
				int num5 = num4 % 4;
				int num6 = num4 / 4;
				int num7 = (int)Math.Round((double)num5 * num3);
				int num8 = (int)Math.Round((double)(num5 + 1) * num3 - (double)num);
				cell.SetBounds(num7, num6 * (num2 + num) + _galleryHost.AutoScrollPosition.Y, Math.Max(1, num8 - num7), num2);
				num4++;
			}
		}
		_galleryHost.ResumeLayout(performLayout: true);
	}

	private void ApplyGalleryItem(CrosshairGallery.GalleryItem item)
	{
		_current.ImagePath = null;
		CrosshairGallery.Apply(_current, item);
		_applyingPreset = true;
		try
		{
			_sizeSlider.Value = Math.Clamp((int)Math.Round(_current.ResolvedSize * 10f), _sizeSlider.Minimum, _sizeSlider.Maximum);
			_thicknessSlider.Value = Math.Clamp((int)Math.Round(_current.ResolvedThickness * 10f), _thicknessSlider.Minimum, _thicknessSlider.Maximum);
			_gapSlider.Value = Math.Clamp((int)Math.Round(_current.ResolvedGap * 10f), _gapSlider.Minimum, _gapSlider.Maximum);
			_rotationSlider.Value = Math.Clamp(_current.ResolvedRotationDegrees, _rotationSlider.Minimum, _rotationSlider.Maximum);
		}
		finally
		{
			_applyingPreset = false;
		}
		CrosshairGallery.Apply(_current, item);
		Push();
		SyncImageControls();
		HighlightActivePreset();
	}

	private void ApplyColour(Color colour, Control? source)
	{
		if (_syncingColour)
		{
			return;
		}
		_syncingColour = true;
		try
		{
			_current.ColourArgb = (_current.ColourArgb & -16777216) | (colour.ToArgb() & 0xFFFFFF);
			foreach (SwatchDot colourDot in _colourDots)
			{
				colourDot.Active = (colourDot.Colour.ToArgb() & 0xFFFFFF) == (_current.ColourArgb & 0xFFFFFF);
			}
			if (source != _wheel)
			{
				_wheel.Colour = colour;
			}
			if (source != _hexBox)
			{
				_hexBox.Text = ColourWheel.ToHex(colour);
			}
			Push();
			RefreshGalleryPreviews();
		}
		finally
		{
			_syncingColour = false;
		}
	}

	private void RefreshGalleryPreviews()
	{
		foreach (CrosshairCell cell in _cells)
		{
			cell.PreviewStyle = _current;
			cell.Invalidate();
		}
	}

	private void ToggleFavourite(CrosshairGallery.GalleryItem item)
	{
		AppSettings settings = _settings;
		if (settings.FavouriteCrosshairs == null)
		{
			List<string> list = (settings.FavouriteCrosshairs = new List<string>());
		}
		if (!_settings.FavouriteCrosshairs.Remove(item.Id))
		{
			_settings.FavouriteCrosshairs.Add(item.Id);
		}
		_store.Save(_settings);
		BuildGallery();
	}

	private void OnSliderMoved()
	{
		if (!_applyingPreset)
		{
			Push();
			HighlightActivePreset();
		}
	}

	private void HighlightActivePreset()
	{
		CrosshairGallery.GalleryItem galleryItem = CrosshairGallery.Matching(_current);
		foreach (CrosshairCell cell in _cells)
		{
			cell.Active = galleryItem != null && cell.Item.Id == galleryItem.Id;
			cell.PreviewStyle = _current;
			cell.Invalidate();
		}
		RefreshPartChips();
		RefreshShapeChips();
	}

	private void Push()
	{
		_preview.Config = _current;
		_preview.Invalidate();
		_service.Apply(_current.Clone());
		_settings.ActiveCrosshair = _current.Clone();
		_store.Save(_settings);
		UpdateStatus();
	}

	private void UpdateStatus()
	{
		_onLabel.Text = (_enabled.Checked ? "On" : "Off");
		LayoutHeaderPanel();
		if (!_enabled.Checked)
		{
			_status.Text = "Crosshair is off.";
			_status.ForeColor = Theme.TextDim;
		}
		else if (CrosshairService.IsExclusiveFullscreen())
		{
			_status.Text = "A game is in exclusive full-screen - no overlay can draw there. Switch it to borderless.";
			_status.ForeColor = Theme.Accent;
		}
		else
		{
			_status.Text = "Crosshair is showing, centred on your screen.";
			_status.ForeColor = Theme.TextDim;
		}
	}

	internal void TestSetEnabled(bool on)
	{
		_enabled.Checked = on;
	}

	private void RefreshSavedList()
	{
		_savedFlow.SuspendLayout();
		_savedFlow.Controls.Clear();
		foreach (CrosshairConfig saved in _settings.SavedCrosshairs)
		{
			SavedChip savedChip = new SavedChip(saved)
			{
				Active = (saved.Name == _current.Name),
				Size = new Size(AtDesignScale(190), AtDesignScale(36)),
				Margin = new Padding(0, 0, AtDesignScale(8), AtDesignScale(8))
			};
			savedChip.LoadRequested += (object? s, EventArgs e2) =>
			{
				LoadSaved(saved.Name);
			};
			savedChip.DeleteRequested += (object? s, EventArgs e2) =>
			{
				DeleteSaved(saved.Name);
			};
			_savedFlow.Controls.Add(savedChip);
		}
		_savedFlow.ResumeLayout(performLayout: true);
		bool flag = _settings.SavedCrosshairs.Count > 0;
		_savedFlow.Visible = flag;
		_savedEmpty.Visible = !flag;
		int num = (flag ? _savedFlow.Bottom : _savedEmpty.Bottom) - _leftScroll.AutoScrollPosition.Y;
		_leftScroll.AutoScrollMinSize = new Size(0, num + S(17) + S(8));
		FitScrollToContent(0);
	}

	private void CopyMyCode()
	{
		string text = CrosshairCode.Encode(_current);
		_codeBox.Text = text;
		bool flag = TheClipboard.Put(text);
		SetShareStatus(flag ? "Copied. Paste it to a friend." : "Couldn't reach the clipboard. Copy the code from the box above.", flag ? Theme.Accent : Theme.TextDim);
	}

	internal void ApplyLibraryCrosshair(CrosshairConfig incoming)
	{
		if (incoming != null)
		{
			incoming.Name = _current.Name;
			_current = incoming;
			SyncControlsToCurrent();
			Push();
			RefreshGalleryPreviews();
		}
	}

	private void ApplySharedCode()
	{
		if (!CrosshairCode.TryDecode(_codeBox.Text, out CrosshairConfig crosshair))
		{
			SetShareStatus("That code isn't valid. Nothing changed.", Theme.Negative);
			return;
		}
		crosshair.Name = _current.Name;
		_current = crosshair;
		SyncControlsToCurrent();
		Push();
		RefreshGalleryPreviews();
		SetShareStatus("Applied.", Theme.Accent);
	}

	private void SetShareStatus(string text, Color colour)
	{
		_shareStatus.Text = text;
		_shareStatus.ForeColor = colour;
	}

	private void LoadSaved(string name)
	{
		CrosshairConfig crosshairConfig = _settings.SavedCrosshairs.FirstOrDefault((CrosshairConfig crosshairConfig2) => string.Equals(crosshairConfig2.Name, name, StringComparison.OrdinalIgnoreCase));
		if (crosshairConfig != null)
		{
			_current = crosshairConfig.Clone();
			SyncControlsToCurrent();
			Push();
			RefreshSavedList();
		}
	}

	private void SyncControlsToCurrent()
	{
		_applyingPreset = true;
		try
		{
			_current.SetSizeTenths(Set(_sizeSlider, (int)Math.Round(_current.ResolvedSize * 10f)));
			_current.SetThicknessTenths(Set(_thicknessSlider, (int)Math.Round(_current.ResolvedThickness * 10f)));
			_current.SetGapTenths(Set(_gapSlider, (int)Math.Round(_current.ResolvedGap * 10f)));
			_current.DotSizeTenths = Set(_dotSlider, (int)Math.Round(_current.ResolvedDotSize * 10f));
			_current.CircleRadiusTenths = Set(_circleSlider, (int)Math.Round(_current.ResolvedCircleRadius * 10f));
			_current.Opacity = Set(_opacitySlider, _current.Opacity);
			_current.OffsetXPx = Set(_offsetXSlider, _current.ResolvedOffsetX);
			_current.OffsetYPx = Set(_offsetYSlider, _current.ResolvedOffsetY);
			_current.OutlineThicknessTenths = Set(_outlineSlider, _current.OutlineThicknessTenths ?? 10);
			_current.ImageScalePercent = Set(_imageScaleSlider, _current.ImageScalePercent);
			_current.SetRotationDegrees(Set(_rotationSlider, _current.ResolvedRotationDegrees));
			_current.DotOpacity = Set(_dotOpacitySlider, _current.ResolvedDotOpacity);
			_outlineSwitch.Checked = _current.Outline;
			_wheel.Colour = Color.FromArgb(_current.ColourArgb);
			_hexBox.Text = ColourWheel.ToHex(Color.FromArgb(_current.ColourArgb));
		}
		finally
		{
			_applyingPreset = false;
		}
		_opacityValue.Text = $"{_opacitySlider.Value}%";
		HighlightActivePreset();
		SyncColourDots();
		SyncOutlineDots();
		SyncImageControls();
		static int Set(FlatSlider slider, int value)
		{
			return slider.Value = Math.Clamp(value, slider.Minimum, slider.Maximum);
		}
	}

	private void SaveCurrent()
	{
		string text = Prompt.Ask("Save crosshair as", _current.Name);
		if (!string.IsNullOrWhiteSpace(text))
		{
			SaveCurrentAs(text);
		}
	}

	private void SaveCurrentAs(string name)
	{
		name = name.Trim();
		if (WouldExceedCap(name))
		{
			GlassDialog.Show(FindForm(), "Presets full", $"You already have {10} saved crosshairs - the most this " + "page keeps. Delete one to save a new one.");
			return;
		}
		_current.Name = name;
		_settings.SavedCrosshairs.RemoveAll((CrosshairConfig crosshairConfig) => string.Equals(crosshairConfig.Name, _current.Name, StringComparison.OrdinalIgnoreCase));
		_settings.SavedCrosshairs.Add(_current.Clone());
		_store.Save(_settings);
		RefreshSavedList();
	}

	private bool WouldExceedCap(string trimmedName)
	{
		if (!_settings.SavedCrosshairs.Any((CrosshairConfig crosshairConfig) => string.Equals(crosshairConfig.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
		{
			return _settings.SavedCrosshairs.Count >= 10;
		}
		return false;
	}

	internal void TestSaveCurrentAs(string name)
	{
		SaveCurrentAs(name);
	}

	internal bool TestWouldRefuseSave(string name)
	{
		return WouldExceedCap(name.Trim());
	}

	internal void TestDeleteSaved(string name)
	{
		DeleteSaved(name);
	}

	internal void TestLoadSaved(string name)
	{
		LoadSaved(name);
	}

	private int CountOf<T>() where T : Control
	{
		int n = 0;
		Walk(this);
		return n;
		void Walk(Control parent)
		{
			foreach (Control control in parent.Controls)
			{
				if (control is T)
				{
					n++;
				}
				Walk(control);
			}
		}
	}

	private void DeleteSaved(string name)
	{
		_settings.SavedCrosshairs.RemoveAll((CrosshairConfig crosshairConfig) => string.Equals(crosshairConfig.Name, name, StringComparison.OrdinalIgnoreCase));
		_store.Save(_settings);
		RefreshSavedList();
	}

	private void SyncColourDots()
	{
		foreach (SwatchDot colourDot in _colourDots)
		{
			colourDot.Active = colourDot.Colour.ToArgb() == _current.ColourArgb;
		}
	}

	private void ApplyOutlineColour(Color colour)
	{
		_current.OutlineColourRgb = colour.ToArgb() & 0xFFFFFF;
		SyncOutlineDots();
		Push();
		HighlightActivePreset();
	}

	private void SyncOutlineDots()
	{
		int num = _current.ResolvedOutlineColourArgb & 0xFFFFFF;
		foreach (OutlineSwatch outlineDot in _outlineDots)
		{
			outlineDot.Active = (outlineDot.Colour.ToArgb() & 0xFFFFFF) == num;
		}
	}

	private void SyncImageControls()
	{
		bool usesImage = _current.UsesImage;
		_clearImageBtn.Visible = usesImage;
		_imageNote.Text = (usesImage ? "Using your image. The shape, size and colour controls below do not apply to it - opacity, image size and position still do." : "");
		foreach (ChipButton partChip in _partChips)
		{
			partChip.Enabled = !usesImage;
		}
		_sizeSlider.Enabled = !usesImage;
		_thicknessSlider.Enabled = !usesImage;
		_gapSlider.Enabled = !usesImage;
		_dotSlider.Enabled = !usesImage;
		_circleSlider.Enabled = !usesImage;
		_offsetXSlider.Enabled = true;
		_offsetYSlider.Enabled = true;
		_outlineSlider.Enabled = !usesImage;
		_imageScaleSlider.Enabled = usesImage;
		_wheel.Enabled = !usesImage;
		_hexBox.Enabled = !usesImage;
		foreach (SwatchDot colourDot in _colourDots)
		{
			colourDot.Enabled = !usesImage;
		}
		foreach (ChipButton shapeChip in _shapeChips)
		{
			shapeChip.Enabled = !usesImage;
		}
		_rotationSlider.Enabled = !usesImage;
		_dotOpacitySlider.Enabled = !usesImage;
		foreach (OutlineSwatch outlineDot in _outlineDots)
		{
			outlineDot.Enabled = !usesImage;
		}
	}

	private void ImportImage()
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Pick a crosshair image",
			Filter = "Images (*.png;*.bmp;*.gif;*.jpg)|*.png;*.bmp;*.gif;*.jpg|All files (*.*)|*.*",
			CheckFileExists = true
		};
		if (openFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			if (!ImageCrosshair.TryImport(openFileDialog.FileName, ImageStoreDir, out string stored, out string error))
			{
				_imageNote.ForeColor = Theme.TextDim;
				_imageNote.Text = error;
				return;
			}
			_current.ImagePath = stored;
			_imageNote.ForeColor = Theme.TextDim;
			Push();
			SyncImageControls();
		}
	}

	private static GlassButton Button(string text, int x, int y, int w)
	{
		GlassButton glassButton = SettingsPage.FlatButton(text, x, y, w);
		glassButton.Height = 32;
		return glassButton;
	}
}