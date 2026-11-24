using System;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class SliderRow
{
	private sealed class ReadingLabel : Label
	{
		private const TextFormatFlags Flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

		public Font? UnitFont { get; set; }

		public Color UnitInk { get; set; }

		private static int Gap => Tokens.Scale(2);

		private static Size MaxSize => new Size(int.MaxValue, int.MaxValue);

		protected override void OnPaint(PaintEventArgs e)
		{
			if (UnitFont == null)
			{
				base.OnPaint(e);
				return;
			}
			(string Number, string Unit) tuple = Split(Text);
			string item = tuple.Number;
			string item2 = tuple.Unit;
			Graphics graphics = e.Graphics;
			graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			float num = ((float)Height - Font.GetHeight(graphics)) / 2f + Ascent(Font, graphics);
			int width = TextRenderer.MeasureText(graphics, item, Font, MaxSize, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
			TextRenderer.DrawText(graphics, item, Font, new Point(0, (int)Math.Round(num - Ascent(Font, graphics))), ForeColor, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
			if (item2.Length > 0)
			{
				TextRenderer.DrawText(graphics, item2, UnitFont, new Point(width + Gap, (int)Math.Round(num - Ascent(UnitFont, graphics))), UnitInk, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
			}
		}

		private static float Ascent(Font font, Graphics g)
		{
			FontFamily fontFamily = font.FontFamily;
			FontStyle style = font.Style;
			return font.GetHeight(g) * (float)fontFamily.GetCellAscent(style) / (float)fontFamily.GetLineSpacing(style);
		}

		internal static (string Number, string Unit) Split(string text)
		{
			if (text == null)
			{
				text = "";
			}
			int num = -1;
			for (int i = 0; i < text.Length; i++)
			{
				if (char.IsDigit(text[i]))
				{
					num = i;
				}
			}
			if (num >= 0)
			{
				return (Number: text.Substring(0, num + 1), Unit: text.Substring(num + 1));
			}
			return (Number: text, Unit: "");
		}
	}

	public const int Tuck = 2;

	private readonly Label _caption;

	private readonly Label _value;

	private readonly SliderRowShape _shape;

	private readonly Func<int, string>? _format;

	private readonly decimal _valuePerStep;

	private TextBox? _editor;

	private bool _editing;

	private Label? _availabilityHint;

	private bool _shown = true;

	public static int RowHeight => CaptionH + CaptionGap + TwoColorSlider.MinHeight + Air;

	public static int LargeRowHeight => LargeCaptionH + CaptionGap + TwoColorSlider.MinHeight + LargeAir;

	public static int CompactRowHeight => CaptionH + CompactTrackBox;

	public static int CompactRowPitch => Tokens.Scale(50);

	public static int HeadlineRowHeight => HeadlineTrackTop + HeadlineTrackBox;

	public static int HeadlineTrackTop => HeadingBox - 2 + HeadlineValueBox - 2;

	private static int HeadingBox => Math.Max(HeadingH, TextRenderer.MeasureText("Wg", HeadlineCaptionFont).Height);

	private static int HeadlineValueBox => Math.Max(ValueH, TextRenderer.MeasureText("0", Fonts.For(FontRole.Value)).Height);

	private static int HeadlineTrackBox => Tokens.Scale(22);

	private static int CompactTrackBox => Tokens.Scale(22);

	private static int CaptionH => Fonts.LineHeight(FontRole.Label);

	private static int LargeCaptionH => Math.Max(Fonts.LineHeight(FontRole.Title), Fonts.LineHeight(FontRole.HeadingRegular));

	private static int HeadingH => Fonts.LineHeight(FontRole.Heading);

	private static int ValueH => Fonts.LineHeight(FontRole.Value);

	private static int CaptionGap => Tokens.Scale(8);

	private static int Air => Tokens.Scale(2);

	private static int LargeAir => Tokens.Scale(8);

	private static int ValueW => Tokens.Scale(120);

	private static int LargeValueW => Tokens.Scale(160);

	private static int CompactValueW => Tokens.Scale(72);

	private static Font CaptionFont => Fonts.Label;

	private static Font ValueFont => Fonts.BodyBold;

	private static Font LargeCaptionFont => Fonts.HeadingRegular;

	private static Font LargeValueFont => Fonts.Title;

	private static Font HeadlineCaptionFont => Fonts.Heading;

	private static Font HeadlineValueFont => Fonts.Value;

	private static Font HeadlineUnitFont => Fonts.Title;

	public int AvailabilityHintHeight
	{
		get
		{
			Label availabilityHint = _availabilityHint;
			if (availabilityHint != null)
			{
				string text = availabilityHint.Text;
				if (text != null && text.Length > 0)
				{
					return Math.Max(Fonts.LineHeight(FontRole.Caption), _availabilityHint.Font.Height) + Tokens.Scale(4);
				}
			}
			return 0;
		}
	}

	public TwoColorSlider Slider { get; }

	public bool Visible
	{
		get
		{
			return Slider.Visible;
		}
		set
		{
			_shown = value;
			if (!value)
			{
				EndValueEdit(apply: false);
			}
			_caption.Visible = value;
			_value.Visible = value;
			Slider.Visible = value;
			if (_availabilityHint != null)
			{
				_availabilityHint.Visible = value && _availabilityHint.Text.Length > 0;
			}
		}
	}

	public string ValueText
	{
		get
		{
			return _value.Text;
		}
		set
		{
			if (_value.Text != value)
			{
				_value.Text = value;
			}
		}
	}

	public Rectangle Bounds => Rectangle.Union(Rectangle.Union(_caption.Bounds, _value.Bounds), Slider.Bounds);

	internal (Font Number, Font? Unit) ReadingFontsForTest => (Number: _value.Font, Unit: (_value as ReadingLabel)?.UnitFont);

	public void SetAvailabilityHint(string text)
	{
		if (_availabilityHint != null || text.Length != 0)
		{
			if (_availabilityHint == null)
			{
				_availabilityHint = new Label
				{
					Font = Fonts.Caption,
					ForeColor = Theme.TextDim,
					BackColor = Color.Transparent,
					AutoSize = false,
					TabStop = false
				};
				Slider.Parent.Controls.Add(_availabilityHint);
			}
			_availabilityHint.Text = text;
			_availabilityHint.Visible = _shown && text.Length > 0;
		}
	}

	private void PlaceAvailabilityHint(int x, int width)
	{
		if (_availabilityHint != null)
		{
			_availabilityHint.SetBounds(x, Slider.Bottom + Tokens.Scale(4), width, Math.Max(0, AvailabilityHintHeight - Tokens.Scale(4)));
		}
	}

	public SliderRow(Control parent, string caption, int minimum, int maximum, int? notch, int value, SliderRowShape shape = SliderRowShape.Standard, SliderPalette? palette = null, Func<int, string>? format = null, decimal valuePerStep = 1m)
	{
		if (valuePerStep <= 0m)
		{
			throw new ArgumentOutOfRangeException("valuePerStep");
		}
		_shape = shape;
		_format = format;
		_valuePerStep = valuePerStep;
		_caption = new Label
		{
			Text = caption,
			Font = CaptionFontFor(shape),
			ForeColor = CaptionInkFor(shape),
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft
		};
		_value = ((shape == SliderRowShape.Headline) ? new ReadingLabel
		{
			UnitFont = HeadlineUnitFont,
			UnitInk = Theme.TextDim
		} : new Label());
		_value.Font = ValueFontFor(shape);
		_value.ForeColor = Theme.Text;
		_value.BackColor = Color.Transparent;
		_value.AutoSize = false;
		_value.TextAlign = ((shape == SliderRowShape.Headline) ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight);
		Slider = new TwoColorSlider
		{
			Minimum = minimum,
			Maximum = maximum,
			Notch = notch,
			Value = value,
			Palette = (palette ?? SliderPalette.Accent()),
			Hero = (shape == SliderRowShape.Headline)
		};
		parent.Controls.Add(_caption);
		parent.Controls.Add(_value);
		parent.Controls.Add(Slider);
		_value.Cursor = Cursors.Hand;
		_value.Click += (object? _, EventArgs _) =>
		{
			BeginValueEdit();
		};
		_value.LocationChanged += (object? _, EventArgs _) =>
		{
			PlaceEditor();
		};
		_value.SizeChanged += (object? _, EventArgs _) =>
		{
			PlaceEditor();
		};
		Slider.EnabledChanged += (object? _, EventArgs _) =>
		{
			if (!Slider.Enabled)
			{
				EndValueEdit(apply: false);
			}
		};
		Slider.VisibleChanged += (object? _, EventArgs _) =>
		{
			if (!Slider.Visible)
			{
				EndValueEdit(apply: false);
			}
		};
		Slider.ValueChanged += (object? _, EventArgs _) =>
		{
			SyncValueText();
		};
		SyncValueText();
	}

	private void BeginValueEdit()
	{
		if (_editing || !Slider.Enabled || !Slider.Visible)
		{
			return;
		}
		if (_editor == null)
		{
			_editor = new TextBox
			{
				Visible = false,
				Multiline = true,
				BorderStyle = BorderStyle.None,
				AcceptsReturn = false,
				TextAlign = ((_shape != SliderRowShape.Headline) ? HorizontalAlignment.Right : HorizontalAlignment.Left),
				AccessibleName = _caption.Text
			};
			_editor.KeyDown += (object? _, KeyEventArgs e) =>
			{
				if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Escape)
				{
					e.SuppressKeyPress = true;
					EndValueEdit(e.KeyCode == Keys.Return);
					Slider.Focus();
				}
			};
			_editor.LostFocus += (object? _, EventArgs _) =>
			{
				EndValueEdit(apply: true);
			};
			_value.Parent.Controls.Add(_editor);
		}
		_editor.Font = _value.Font;
		_editor.ForeColor = Theme.Text;
		_editor.BackColor = Theme.Surface;
		_editor.Text = ((decimal)Slider.Value * _valuePerStep).ToString((_valuePerStep < 1m) ? "0.00" : "0", CultureInfo.InvariantCulture);
		PlaceEditor();
		_editing = true;
		_value.Visible = false;
		_editor.Visible = true;
		_editor.BringToFront();
		_editor.Focus();
		_editor.SelectAll();
	}

	private void PlaceEditor()
	{
		if (_editor != null)
		{
			_editor.Bounds = _value.Bounds;
		}
	}

	private void EndValueEdit(bool apply)
	{
		if (_editing)
		{
			_editing = false;
			string s = _editor.Text.Trim().Replace(',', '.');
			_editor.Visible = false;
			_value.Visible = Slider.Visible;
			if (apply && Slider.Enabled && Slider.Visible && decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
			{
				result = Math.Clamp(result, (decimal)Slider.Minimum * _valuePerStep, (decimal)Slider.Maximum * _valuePerStep);
				Slider.Value = (int)Math.Round(result / _valuePerStep, MidpointRounding.AwayFromZero);
			}
			SyncValueText();
		}
	}

	private static Font CaptionFontFor(SliderRowShape shape)
	{
		return shape switch
		{
			SliderRowShape.Headline => HeadlineCaptionFont, 
			SliderRowShape.Large => LargeCaptionFont, 
			_ => CaptionFont, 
		};
	}

	private static Font ValueFontFor(SliderRowShape shape)
	{
		return shape switch
		{
			SliderRowShape.Headline => HeadlineValueFont, 
			SliderRowShape.Large => LargeValueFont, 
			_ => ValueFont, 
		};
	}

	private static Color CaptionInkFor(SliderRowShape shape)
	{
		if (shape != SliderRowShape.Headline)
		{
			return Theme.TextDim;
		}
		return Theme.Text;
	}

	public void SyncValueText()
	{
		ValueText = _format?.Invoke(Slider.Value) ?? Slider.Value.ToString(CultureInfo.InvariantCulture);
	}

	internal static (string Number, string Unit) SplitReadingForTest(string text)
	{
		return ReadingLabel.Split(text);
	}

	public void Place(int x, int y, int width)
	{
		if (_shape == SliderRowShape.Headline)
		{
			PlaceHeadline(x, y, width, width);
			return;
		}
		bool flag = _shape == SliderRowShape.Large;
		int num = (flag ? LargeCaptionH : CaptionH);
		int num2 = (flag ? LargeValueW : ValueW);
		int width2 = Math.Max(Tokens.Scale(40), width - num2 - Tokens.Scale(8));
		_caption.SetBounds(x, y, width2, num);
		_value.SetBounds(x + width - num2, y, num2, num);
		Slider.SetTrackBounds(x, y + num + CaptionGap, width);
		PlaceAvailabilityHint(x, width);
	}

	public void PlaceCompact(int x, int y, int width)
	{
		int num = Math.Min(CompactValueW, Math.Max(Tokens.Scale(40), width / 2));
		int width2 = Math.Max(Tokens.Scale(40), width - num - Tokens.Scale(8));
		_caption.SetBounds(x, y, width2, CaptionH);
		_value.SetBounds(x + width - num, y, num, CaptionH);
		SetTrackBox(x, y + CaptionH, width, CompactTrackBox);
		PlaceAvailabilityHint(x, width);
	}

	public void PlaceHeadline(int x, int y, int width, int captionWidth)
	{
		int num = Math.Max(HeadingH, TextRenderer.MeasureText(_caption.Text, _caption.Font).Height);
		_caption.SetBounds(x, y, Math.Max(Tokens.Scale(40), captionWidth), num);
		int num2 = y + num - 2;
		int num3 = Math.Max(HeadlineValueBox, TextRenderer.MeasureText(_value.Text, _value.Font).Height);
		_value.SetBounds(x, num2, width, num3);
		SetTrackBox(x, num2 + num3 - 2, width, HeadlineTrackBox);
		PlaceAvailabilityHint(x, width);
	}

	private void SetTrackBox(int x, int y, int width, int height)
	{
		Slider.SetBounds(x - TwoColorSlider.EdgeInset, y, width + 2 * TwoColorSlider.EdgeInset, height);
	}

	public void SetToolTip(ToolTip tips, string text)
	{
		tips.SetToolTip(_caption, text);
		tips.SetToolTip(_value, text);
	}

	public void Restyle()
	{
		_caption.ForeColor = CaptionInkFor(_shape);
		_value.ForeColor = Theme.Text;
		_caption.Font = CaptionFontFor(_shape);
		_value.Font = ValueFontFor(_shape);
		if (_availabilityHint != null)
		{
			_availabilityHint.Font = Fonts.Caption;
			_availabilityHint.ForeColor = Theme.TextDim;
		}
		if (_editor != null)
		{
			_editor.Font = _value.Font;
			_editor.ForeColor = Theme.Text;
			_editor.BackColor = Theme.Surface;
		}
		if (_value is ReadingLabel readingLabel)
		{
			readingLabel.UnitFont = HeadlineUnitFont;
			readingLabel.UnitInk = Theme.TextDim;
		}
	}
}
