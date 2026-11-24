using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Theming;

namespace VibranceHud.Controls;

public sealed class HueWheel : Control
{
	internal readonly record struct RingNameSlot(int Degrees, string Name, Rectangle Rect, PointF TickEnd);

	public const int MinHue = -180;

	public const int MaxHue = 180;

	private static readonly float RefR = 0.82f;

	private static readonly float RefG = 0.38f;

	private static readonly float RefB = 0.34f;

	private Bitmap? _disc;

	private int _hue;

	private bool _dragging;

	public const int MinDarkenHue = 0;

	public const int MaxDarkenHue = 359;

	public const int MinDarken = 0;

	public const int MaxDarken = 400;

	private bool _darkenMode;

	private int _darkenHue = 120;

	private int _darken;

	private bool _hasDarkenSelection;

	private const int RingBandPx = 28;

	private bool _ring;

	private Color _surface = Color.Empty;

	private static readonly (int Degrees, string Name)[] RingNames = new (int, string)[6]
	{
		(0, "Red"),
		(60, "Yellow"),
		(120, "Green"),
		(180, "Cyan"),
		(240, "Blue"),
		(300, "Magenta")
	};

	private (int Diameter, bool Ring, bool Darken) _failedDisc = (Diameter: -1, Ring: false, Darken: false);

	public bool DarkenMode
	{
		get
		{
			return _darkenMode;
		}
		set
		{
			if (_darkenMode != value)
			{
				_darkenMode = value;
				TabStop = value;
				_disc?.Dispose();
				_disc = null;
				Invalidate();
			}
		}
	}

	public int DarkenHue
	{
		get
		{
			return _darkenHue;
		}
		set
		{
			int num = (value % 360 + 360) % 360;
			if (num != _darkenHue || !_hasDarkenSelection)
			{
				_darkenHue = num;
				_hasDarkenSelection = true;
				Invalidate();
			}
		}
	}

	public bool Ring
	{
		get
		{
			return _ring;
		}
		set
		{
			if (_ring != value)
			{
				_ring = value;
				_disc?.Dispose();
				_disc = null;
				Invalidate();
			}
		}
	}

	public Color Surface
	{
		get
		{
			return _surface;
		}
		set
		{
			_surface = value;
			Invalidate();
		}
	}

	public string CentreText
	{
		get
		{
			if (!_darkenMode)
			{
				if (_hue != 0)
				{
					return "Colours turned";
				}
				return "No turn";
			}
			if (_hasDarkenSelection)
			{
				if (_darken != 0)
				{
					return HueName(_darkenHue) + " darker";
				}
				return "Not darkened";
			}
			return "Pick a colour to darken";
		}
	}

	private int RingAngle
	{
		get
		{
			if (!_darkenMode)
			{
				if (_hue < 0)
				{
					return 360 + _hue;
				}
				return _hue;
			}
			return _darkenHue;
		}
	}

	internal Action<string>? FailPointForTest { get; set; }

	public int Darken
	{
		get
		{
			return _darken;
		}
		set
		{
			int num = Math.Clamp(value, 0, 400);
			if (num != _darken)
			{
				_darken = num;
				if (num > 0)
				{
					_hasDarkenSelection = true;
				}
				Invalidate();
			}
		}
	}

	public int Hue
	{
		get
		{
			return _hue;
		}
		set
		{
			int num = Math.Clamp(value, -180, 180);
			if (num != _hue)
			{
				_hue = num;
				Invalidate();
			}
		}
	}

	private int Diameter
	{
		get
		{
			if (!_ring)
			{
				return Math.Max(1, Math.Min(Width, Height) - Tokens.Scale(6));
			}
			return Math.Max(1, Math.Min(Tokens.Scale(176), Math.Min(Width, Height) - Tokens.Scale(80)));
		}
	}

	private bool SeeThrough
	{
		get
		{
			if (!(Parent is CardPanel))
			{
				if (SlateSurface.Active)
				{
					return AppBackground.IsSet;
				}
				return false;
			}
			return true;
		}
	}

	public event EventHandler? DarkenChanged;

	public event EventHandler? HueChanged;

	internal static string NearestName(int degrees)
	{
		return HueName(degrees);
	}

	private static string HueName(int degrees)
	{
		int d = (degrees % 360 + 360) % 360;
		return RingNames.OrderBy(((int Degrees, string Name) n) => Math.Min(Math.Abs(n.Degrees - d), 360 - Math.Abs(n.Degrees - d))).First().Name;
	}

	private void PaintRing(Graphics g)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		int diameter = Diameter;
		float num = (float)Width / 2f;
		float num2 = (float)Height / 2f;
		float num3 = (float)diameter / 2f;
		float num4 = S(28f);
		float num5 = num3 - num4;
		Bitmap bitmap = Disc();
		if (bitmap != null)
		{
			g.DrawImageUnscaled(bitmap, (int)(num - num3), (int)(num2 - num3));
		}
		using (Pen pen = new Pen(ControlInk.Border, Math.Max(1f, S(1f))))
		{
			g.DrawEllipse(pen, num - num3 - S(0.5f), num2 - num3 - S(0.5f), 2f * num3 + S(1f), 2f * num3 + S(1f));
			g.DrawEllipse(pen, num - num5 + S(0.5f), num2 - num5 + S(0.5f), 2f * num5 - S(1f), 2f * num5 - S(1f));
		}
		if (_darkenMode)
		{
			float num6 = num5 - S(8f);
			using Pen pen2 = new Pen(ControlInk.Track, S(3f));
			g.DrawEllipse(pen2, num - num6, num2 - num6, 2f * num6, 2f * num6);
			if (_darken > 0)
			{
				float num7 = (float)_darken / 400f * 50f;
				using Pen pen3 = new Pen(ControlInk.Text1, S(3f))
				{
					StartCap = LineCap.Round,
					EndCap = LineCap.Round
				};
				g.DrawArc(pen3, num - num6, num2 - num6, 2f * num6, 2f * num6, (float)_darkenHue - num7, 2f * num7);
			}
		}
		int ringAngle = RingAngle;
		using (Pen pen4 = new Pen(ControlInk.Text2, S(1.5f)))
		{
			using Pen pen5 = new Pen(ControlInk.TextDisabled, Math.Max(1f, S(1f)));
			for (int i = 0; i < 360; i += 30)
			{
				bool flag = i % 60 == 0;
				double num8 = (double)i * Math.PI / 180.0;
				float num9 = (float)Math.Cos(num8);
				float num10 = (float)Math.Sin(num8);
				float num11 = num3 + S(3f);
				float num12 = num3 + S(flag ? 9 : 6);
				g.DrawLine(flag ? pen4 : pen5, num + num9 * num11, num2 + num10 * num11, num + num9 * num12, num2 + num10 * num12);
			}
		}
		Font captionBold = Fonts.CaptionBold;
		RingNameSlot[] array = RingNameSlots(g, num, num2, num3);
		for (int j = 0; j < array.Length; j++)
		{
			RingNameSlot ringNameSlot = array[j];
			bool flag2 = (!_darkenMode || _hasDarkenSelection) && ringAngle % 60 == 0 && ringNameSlot.Degrees == ringAngle;
			TextRenderer.DrawText(g, ringNameSlot.Name, captionBold, ringNameSlot.Rect.Location, flag2 ? ControlInk.Text1 : ControlInk.Text3, TextFormatFlags.NoPadding);
		}
		HueAt(ringAngle, out var r, out var g2, out var b);
		Color color = Hsl(r, g2, b);
		TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
		int num13 = (int)S(100f);
		int num14 = (int)Math.Round(num - (float)num13 / 2f);
		int num15 = Fonts.LineHeight(FontRole.Caption);
		int num16 = Math.Max(num15, (int)Math.Ceiling((float)TextRenderer.MeasureText(g, CentreText, Fonts.Caption, new Size(num13, int.MaxValue), flags).Height / (float)num15) * num15);
		bool flag3 = !_darkenMode || _hasDarkenSelection;
		float num17 = (flag3 ? S(22f) : 0f);
		float num18 = S(46f) + S(5f) + S(1f) + num17 + S(1f) + (float)num16;
		float num19 = num2 - num18 / 2f;
		float num20 = S(46f);
		if (flag3)
		{
			using (SolidBrush brush = new SolidBrush(ControlInk.Edge))
			{
				g.FillEllipse(brush, num - num20 / 2f - S(1f), num19 - S(1f), num20 + S(2f), num20 + S(2f));
			}
			using SolidBrush brush2 = new SolidBrush(_darkenMode ? DarkenedSwatch(color) : color);
			g.FillEllipse(brush2, num - num20 / 2f, num19, num20, num20);
		}
		float num21 = num19 + num20 + S(5f) + S(1f);
		if (num17 > 0f)
		{
			string text = (_darkenMode ? (_darken + "%") : (_hue + "°"));
			TextRenderer.DrawText(g, text, Fonts.For(FontRole.Title), new Rectangle(num14 - (int)S(20f), (int)Math.Round(num21 + num17 / 2f) - (int)S(14f), num13 + (int)S(40f), (int)S(28f)), ControlInk.Text1, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
			num21 += num17;
		}
		num21 += S(1f);
		TextRenderer.DrawText(g, CentreText, Fonts.Caption, new Rectangle(num14, (int)Math.Round(num21), num13, num16), ControlInk.Text2, flags);
		if (flag3)
		{
			double num22 = (double)ringAngle * Math.PI / 180.0;
			float num23 = num3 - num4 / 2f;
			float num24 = num + (float)Math.Cos(num22) * num23;
			float num25 = num2 + (float)Math.Sin(num22) * num23;
			float num26 = S(15f);
			using (SolidBrush brush3 = new SolidBrush(ControlInk.KnobRing))
			{
				g.FillEllipse(brush3, num24 - num26 - S(2f), num25 - num26 - S(2f), 2f * num26 + S(4f), 2f * num26 + S(4f));
			}
			using (SolidBrush brush4 = new SolidBrush(ControlInk.Focus))
			{
				g.FillEllipse(brush4, num24 - num26, num25 - num26, 2f * num26, 2f * num26);
			}
			float num27 = num26 - S(3f);
			using (SolidBrush brush5 = new SolidBrush(color))
			{
				g.FillEllipse(brush5, num24 - num27, num25 - num27, 2f * num27, 2f * num27);
			}
			g.PixelOffsetMode = PixelOffsetMode.Default;
			if (_darkenMode && Focused && ShowFocusCues)
			{
				ControlInk.PaintFocusRing(g, new Rectangle((int)(num - num3), (int)(num2 - num3), diameter, diameter), num3, ControlInk.Dpi(this));
			}
		}
		static float S(float px)
		{
			return px * (float)Tokens.Scale(100) / 100f;
		}
	}

	private RingNameSlot[] RingNameSlots(Graphics g, float cx, float cy, float outer)
	{
		Font captionBold = Fonts.CaptionBold;
		RingNameSlot[] array = new RingNameSlot[RingNames.Length];
		for (int i = 0; i < RingNames.Length; i++)
		{
			(int Degrees, string Name) tuple = RingNames[i];
			int item = tuple.Degrees;
			string item2 = tuple.Name;
			double num = (double)item * Math.PI / 180.0;
			float num2 = (float)Math.Cos(num);
			float num3 = (float)Math.Sin(num);
			float num4 = cx + num2 * (outer + S(12f));
			float num5 = cy + num3 * (outer + S(12f));
			Size size = TextRenderer.MeasureText(g, item2, captionBold, Size.Empty, TextFormatFlags.NoPadding);
			float num6;
			if (num2 > 0.3f)
			{
				num6 = num4;
			}
			else
			{
				num6 = ((num2 < -0.3f) ? (num4 - (float)size.Width) : (num4 - (float)size.Width / 2f));
			}
			if (Math.Abs(num3) > 0.3f)
			{
				num5 += (float)(Math.Sign(num3) * size.Height) / 2f;
			}
			Rectangle rect = new Rectangle(new Point((int)Math.Round(num6), (int)Math.Round(num5 - (float)size.Height / 2f)), size);
			PointF tickEnd = new PointF(cx + num2 * (outer + S(9f)), cy + num3 * (outer + S(9f)));
			array[i] = new RingNameSlot(item, item2, rect, tickEnd);
		}
		return array;
		static float S(float px)
		{
			return px * (float)Tokens.Scale(100) / 100f;
		}
	}

	internal RingNameSlot[] RingNameSlotsForTest()
	{
		using Bitmap image = new Bitmap(1, 1);
		using Graphics g = Graphics.FromImage(image);
		float outer = (float)Diameter / 2f;
		return RingNameSlots(g, (float)Width / 2f, (float)Height / 2f, outer);
	}

	private static Color Hsl(float r, float g, float b)
	{
		float num = (1f - Math.Abs(0.100000024f)) * 0.88f;
		float num2 = 0.55f - num / 2f;
		return Color.FromArgb(Byte(num2 + r * num), Byte(num2 + g * num), Byte(num2 + b * num));
	}

	private Color DarkenedSwatch(Color colour)
	{
		if (_darken == 0)
		{
			return colour;
		}
		var (num, num2, num3) = SaturationMetrics.Transform(SaturationMetrics.Build(SaturationMetric.Rec709, 1f, 1f, new ColourDepth(SaturationMetric.Rec709, _darken, _darkenHue)), (double)(int)colour.R / 255.0, (double)(int)colour.G / 255.0, (double)(int)colour.B / 255.0);
		return Color.FromArgb(Byte((float)num), Byte((float)num2), Byte((float)num3));
	}

	private void SetDarken(int hue, int amount)
	{
		int num = (hue % 360 + 360) % 360;
		int num2 = Math.Clamp(amount, 0, 400);
		if (num != _darkenHue || num2 != _darken || !_hasDarkenSelection)
		{
			_darkenHue = num;
			_darken = num2;
			_hasDarkenSelection = true;
			Invalidate();
			DarkenChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	internal void TestPressAt(Point p)
	{
		OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0));
		OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0));
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	internal Point PointFor(int angleDegrees, double strength)
	{
		int diameter = Diameter;
		double num = (double)(Width - diameter) / 2.0 + (double)diameter / 2.0;
		double num2 = (double)(Height - diameter) / 2.0 + (double)diameter / 2.0;
		double num3 = (double)angleDegrees * Math.PI / 180.0;
		double num4 = (double)diameter / 2.0 * Math.Clamp(strength, 0.0, 1.0);
		return new Point((int)Math.Round(num + Math.Cos(num3) * num4), (int)Math.Round(num2 + Math.Sin(num3) * num4));
	}

	protected override bool IsInputKey(Keys keyData)
	{
		bool flag = _darkenMode;
		if (flag)
		{
			Keys keys = keyData & Keys.KeyCode;
			bool flag2 = (((uint)(keys - 36) <= 4u || keys == Keys.Delete) ? true : false);
			flag = flag2;
		}
		if (!flag)
		{
			return base.IsInputKey(keyData);
		}
		return true;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (_darkenMode)
		{
			bool shift = e.Shift;
			switch (e.KeyCode)
			{
			default:
				return;
			case Keys.Right:
				SetDarken(_darkenHue + ((!shift) ? 1 : 15), _darken);
				break;
			case Keys.Left:
				SetDarken(_darkenHue - ((!shift) ? 1 : 15), _darken);
				break;
			case Keys.Up:
				SetDarken(_darkenHue, _darken + (shift ? 50 : 5));
				break;
			case Keys.Down:
				SetDarken(_darkenHue, _darken - (shift ? 50 : 5));
				break;
			case Keys.Home:
			case Keys.Delete:
				SetDarken(120, 0);
				break;
			case Keys.Select:
			case Keys.Print:
			case Keys.Execute:
			case Keys.Snapshot:
			case Keys.Insert:
				return;
			}
			e.Handled = true;
		}
	}

	protected override void OnDoubleClick(EventArgs e)
	{
		base.OnDoubleClick(e);
		if (_darkenMode)
		{
			SetDarken(120, 0);
		}
		else if (_hue != 0)
		{
			_hue = 0;
			Invalidate();
			HueChanged?.Invoke(this, EventArgs.Empty);
		}
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

	public HueWheel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		TabStop = false;
		Cursor = Cursors.Hand;
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		_disc?.Dispose();
		_disc = null;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		if (SeeThrough)
		{
			base.OnPaintBackground(e);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		if (!SeeThrough)
		{
			g.Clear((!_surface.IsEmpty) ? _surface : (Parent?.BackColor ?? Theme.Background));
		}
		PaintGuard.Try(this, () =>
		{
			PaintBody(g);
		});
	}

	private void PaintBody(Graphics g)
	{
		if (_ring)
		{
			PaintRing(g);
			return;
		}
		Bitmap bitmap = Disc();
		int diameter = Diameter;
		int num = (Width - diameter) / 2;
		int num2 = (Height - diameter) / 2;
		if (bitmap != null)
		{
			g.DrawImageUnscaled(bitmap, num, num2);
		}
		double num3 = (_darkenMode ? ((double)_darken / 400.0) : ((double)Math.Abs(_hue) / 180.0));
		double num4 = (double)(_darkenMode ? _darkenHue : ((_hue >= 0) ? _hue : (360 + _hue))) * Math.PI / 180.0;
		int num5 = num + diameter / 2;
		int num6 = num2 + diameter / 2;
		int num7 = (int)Math.Round(((double)diameter / 2.0 - (double)Tokens.Scale(7)) * num3);
		Point point = new Point(num5 + (int)Math.Round(Math.Cos(num4) * (double)num7), num6 + (int)Math.Round(Math.Sin(num4) * (double)num7));
		int num8 = Tokens.Scale(11);
		using (SolidBrush brush = new SolidBrush(Theme.Accent))
		{
			g.FillEllipse(brush, point.X - num8 / 2, point.Y - num8 / 2, num8, num8);
		}
		using (Pen pen = new Pen(Theme.Background, Tokens.Scale(2)))
		{
			g.DrawEllipse(pen, point.X - num8 / 2, point.Y - num8 / 2, num8, num8);
		}
		if (_darkenMode && Focused && ShowFocusCues)
		{
			ControlInk.PaintFocusRing(g, new Rectangle(num, num2, diameter, diameter), (float)diameter / 2f, ControlInk.Dpi(this));
		}
	}

	private Bitmap? Disc()
	{
		if (_disc != null)
		{
			return _disc;
		}
		(int, bool, bool) tuple = (Diameter, _ring, _darkenMode);
		(int, bool, bool) failedDisc = _failedDisc;
		(int, bool, bool) tuple2 = tuple;
		if (failedDisc.Item1 == tuple2.Item1 && failedDisc.Item2 == tuple2.Item2 && failedDisc.Item3 == tuple2.Item3)
		{
			return null;
		}
		try
		{
			FailPointForTest?.Invoke("build");
			return _disc = BuildDisc(Diameter);
		}
		catch (Exception ex) when (PaintGuard.IsGdiFailure(ex))
		{
			_failedDisc = tuple;
			throw;
		}
	}

	private Bitmap BuildDisc(int d)
	{
		Bitmap bitmap = new Bitmap(d, d, PixelFormat.Format32bppArgb);
		BitmapData bitmapData;
		try
		{
			bitmapData = bitmap.LockBits(new Rectangle(0, 0, d, d), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		}
		catch
		{
			bitmap.Dispose();
			throw;
		}
		bool flag = false;
		try
		{
			byte[] array = new byte[bitmapData.Stride];
			float num = (float)d / 2f;
			float num2 = num;
			for (int i = 0; i < d; i++)
			{
				Array.Clear(array, 0, array.Length);
				for (int j = 0; j < d; j++)
				{
					float num3 = (float)j - num + 0.5f;
					float num4 = (float)i - num + 0.5f;
					float num5 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
					if (num5 > num2)
					{
						continue;
					}
					int num6 = (int)Math.Round(Math.Atan2(num4, num3) * 180.0 / Math.PI);
					if (num6 < 0)
					{
						num6 += 360;
					}
					int degrees = ((num6 > 180) ? (num6 - 360) : num6);
					float num7 = (_ring ? 1f : (num5 / num2));
					float channel;
					float channel2;
					float channel3;
					if (_darkenMode || _ring)
					{
						HueAt(num6, out var r, out var g, out var b);
						channel = 0.5f + (r - 0.5f) * num7;
						channel2 = 0.5f + (g - 0.5f) * num7;
						channel3 = 0.5f + (b - 0.5f) * num7;
						if (_ring)
						{
							float num8 = Tokens.Scale(28);
							float num9 = Math.Clamp((num5 - (num2 - num8)) / num8, 0f, 1f);
							float num10 = (180f + 20f * num9) / 255f;
							channel = 37f / 255f + r * num10;
							channel2 = 37f / 255f + g * num10;
							channel3 = 37f / 255f + b * num10;
						}
					}
					else
					{
						float[] array2 = ColourBalance.HueMatrix(degrees);
						channel = RefR + (array2[0] * RefR + array2[5] * RefG + array2[10] * RefB - RefR) * num7;
						channel2 = RefG + (array2[1] * RefR + array2[6] * RefG + array2[11] * RefB - RefG) * num7;
						channel3 = RefB + (array2[2] * RefR + array2[7] * RefG + array2[12] * RefB - RefB) * num7;
					}
					byte b2 = ((num5 > num2 - 1.2f) ? ((byte)(255f * Math.Clamp(num2 - num5, 0f, 1.2f) / 1.2f)) : byte.MaxValue);
					if (_ring)
					{
						float num11 = num2 - (float)Tokens.Scale(28);
						float num12 = Math.Clamp(num2 - num5 + 0.5f, 0f, 1f) * Math.Clamp(num5 - num11 + 0.5f, 0f, 1f);
						if (num12 <= 0f)
						{
							continue;
						}
						b2 = (byte)Math.Round(255f * num12);
					}
					int num13 = j * 4;
					array[num13] = Byte(channel3);
					array[num13 + 1] = Byte(channel2);
					array[num13 + 2] = Byte(channel);
					array[num13 + 3] = b2;
				}
				Marshal.Copy(array, 0, bitmapData.Scan0 + i * bitmapData.Stride, array.Length);
			}
			flag = true;
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
			if (!flag)
			{
				bitmap.Dispose();
			}
		}
		return bitmap;
	}

	private static byte Byte(float channel)
	{
		return (byte)Math.Round(Math.Clamp(channel, 0f, 1f) * 255f);
	}

	private static void HueAt(int degrees, out float r, out float g, out float b)
	{
		float num = (float)((degrees % 360 + 360) % 360) / 60f;
		float num2 = 1f - Math.Abs(num % 2f - 1f);
		(r, g, b) = (int)num switch
		{
			0 => (1f, num2, 0f), 
			1 => (num2, 1f, 0f), 
			2 => (0f, 1f, num2), 
			3 => (0f, num2, 1f), 
			4 => (num2, 0f, 1f), 
			_ => (1f, 0f, num2), 
		};
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (_darkenMode && TabStop)
		{
			Focus();
		}
		_dragging = true;
		FromPoint(e.Location);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		if (_darkenMode)
		{
			SetDarken(_darkenHue + ((e.Delta < 0) ? 5 : (-5)), _darken);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (_dragging)
		{
			FromPoint(e.Location);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		_dragging = false;
	}

	private void FromPoint(Point p)
	{
		int diameter = Diameter;
		double num = (double)(Width - diameter) / 2.0 + (double)diameter / 2.0;
		double num2 = (double)(Height - diameter) / 2.0 + (double)diameter / 2.0;
		double num3 = (double)p.X - num;
		double num4 = (double)p.Y - num2;
		double num5 = Math.Min(1.0, Math.Sqrt(num3 * num3 + num4 * num4) / ((double)diameter / 2.0));
		int num6 = (int)Math.Round(Math.Atan2(num4, num3) * 180.0 / Math.PI);
		if (num6 < 0)
		{
			num6 += 360;
		}
		if (_darkenMode)
		{
			SetDarken(num6, _ring ? _darken : ((int)Math.Round(num5 * 400.0)));
			return;
		}
		int num7 = ((num6 > 180) ? (num6 - 360) : num6);
		int num8 = (_ring ? num7 : ((int)Math.Round((double)num7 * num5)));
		if (num8 != _hue)
		{
			_hue = Math.Clamp(num8, -180, 180);
			Invalidate();
			HueChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
