using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class ColourWheel : Control
{
	private const int BarWidth = 18;

	private const int BarGap = 12;

	private Bitmap? _wheel;

	private int _wheelDiameter;

	private float _hue;

	private float _saturation;

	private float _value = 1f;

	private bool _draggingWheel;

	private bool _draggingBar;

	private readonly OutsideFocusRing _ring;

	private int _failedDiameter;

	internal Action<string>? FailPointForTest { get; set; }

	public Color Colour
	{
		get
		{
			return FromHsv(_hue, _saturation, _value);
		}
		set
		{
			ToHsv(value, out _hue, out _saturation, out _value);
			Invalidate();
		}
	}

	private int Diameter => Math.Max(8, Math.Min(Width - 18 - 12, Height));

	private Rectangle WheelRect => new Rectangle(0, (Height - Diameter) / 2, Diameter, Diameter);

	private Rectangle BarRect => new Rectangle(Width - 18, 4, 17, Math.Max(8, Height - 9));

	public event EventHandler? ColourChanged;

	public ColourWheel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		TabStop = true;
		AccessibleRole = AccessibleRole.Slider;
		AccessibleName = "Colour picker";
		Size = new Size(180, 160);
		_ring = new OutsideFocusRing(this, () => ControlInk.PxF(ControlInk.Dpi(this), 4f));
	}

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		_ring.Rehook();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		Focus();
		if (BarRect.Contains(e.Location))
		{
			_draggingBar = true;
			DragTracker.Begin();
			TakeBar(e.Y);
		}
		else if (WheelRect.Contains(e.Location))
		{
			_draggingWheel = true;
			DragTracker.Begin();
			TakeWheel(e.Location);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_draggingBar)
		{
			TakeBar(e.Y);
		}
		else if (_draggingWheel)
		{
			TakeWheel(e.Location);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		if (_draggingWheel || _draggingBar)
		{
			DragTracker.End();
		}
		_draggingWheel = false;
		_draggingBar = false;
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

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		switch (e.KeyCode)
		{
		default:
			return;
		case Keys.Left:
			_hue = (_hue + 355f) % 360f;
			break;
		case Keys.Right:
			_hue = (_hue + 5f) % 360f;
			break;
		case Keys.Up:
			_value = Math.Min(1f, _value + 0.05f);
			break;
		case Keys.Down:
			_value = Math.Max(0f, _value - 0.05f);
			break;
		case Keys.Prior:
			_saturation = Math.Min(1f, _saturation + 0.1f);
			break;
		case Keys.Next:
			_saturation = Math.Max(0f, _saturation - 0.1f);
			break;
		case Keys.End:
		case Keys.Home:
			return;
		}
		e.Handled = true;
		Invalidate();
		ColourChanged?.Invoke(this, EventArgs.Empty);
	}

	internal void TestPressKey(Keys key)
	{
		OnKeyDown(new KeyEventArgs(key));
	}

	internal bool TestClaimsKey(Keys key)
	{
		return IsInputKey(key);
	}

	protected override bool IsInputKey(Keys keyData)
	{
		if ((uint)(keyData - 33) <= 1u || (uint)(keyData - 37) <= 3u)
		{
			return true;
		}
		return base.IsInputKey(keyData);
	}

	private void TakeWheel(Point p)
	{
		Rectangle wheelRect = WheelRect;
		float num = (float)wheelRect.X + (float)wheelRect.Width / 2f;
		float num2 = (float)wheelRect.Y + (float)wheelRect.Height / 2f;
		float num3 = (float)p.X - num;
		float num4 = (float)p.Y - num2;
		float num5 = (float)wheelRect.Width / 2f;
		float num6 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
		_saturation = Math.Clamp(num6 / num5, 0f, 1f);
		_hue = (float)((Math.Atan2(num4, num3) * 180.0 / Math.PI + 360.0) % 360.0);
		Invalidate();
		ColourChanged?.Invoke(this, EventArgs.Empty);
	}

	private void TakeBar(int y)
	{
		Rectangle barRect = BarRect;
		_value = 1f - Math.Clamp((float)(y - barRect.Y) / (float)barRect.Height, 0f, 1f);
		Invalidate();
		ColourChanged?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		PaintGuard.Try(this, () =>
		{
			PaintBody(g);
		});
	}

	private void PaintBody(Graphics g)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle wheelRect = WheelRect;
		if (wheelRect.Width > 8)
		{
			EnsureWheel(wheelRect.Width);
			using Region clip = g.Clip;
			using (GraphicsPath graphicsPath = new GraphicsPath())
			{
				graphicsPath.AddEllipse(wheelRect);
				g.SetClip(graphicsPath);
				if (_wheel != null)
				{
					g.DrawImage(_wheel, wheelRect);
				}
				if (_value < 1f)
				{
					using SolidBrush brush = new SolidBrush(Color.FromArgb((int)(255f * (1f - _value)), 0, 0, 0));
					g.FillRectangle(brush, wheelRect);
				}
			}
			g.Clip = clip;
			DrawWheelMarker(g, wheelRect);
		}
		DrawBar(g);
		if (Focused && ShowFocusCues)
		{
			_ring.Show(Enabled);
		}
		else
		{
			_ring.Show(show: false);
		}
		if (!Enabled)
		{
			using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(158, Theme.Background)))
			{
				g.FillRectangle(brush2, ClientRectangle);
			}
		}
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = (Enabled ? Cursors.Hand : Cursors.Default);
		Invalidate();
	}

	private void DrawWheelMarker(Graphics g, Rectangle wheel)
	{
		float num = (float)wheel.Width / 2f;
		float num2 = (float)((double)_hue * Math.PI / 180.0);
		float num3 = (float)wheel.X + num + (float)Math.Cos(num2) * num * _saturation;
		float num4 = (float)wheel.Y + num + (float)Math.Sin(num2) * num * _saturation;
		RectangleF rect = new RectangleF(num3 - 6f, num4 - 6f, 12f, 12f);
		using (Pen pen = new Pen(Color.FromArgb(200, 0, 0, 0), 3f))
		{
			g.DrawEllipse(pen, rect);
		}
		using Pen pen2 = new Pen(Color.White, 1.6f);
		g.DrawEllipse(pen2, rect);
	}

	private void DrawBar(Graphics g)
	{
		Rectangle barRect = BarRect;
		if (barRect.Height <= 8)
		{
			return;
		}
		Color color = FromHsv(_hue, _saturation, 1f);
		using (LinearGradientBrush brush = new LinearGradientBrush(barRect, color, Color.Black, 90f))
		{
			g.FillRectangle(brush, barRect);
		}
		using (Pen pen = new Pen(Color.FromArgb(90, Theme.GlassEdge), 1f))
		{
			g.DrawRectangle(pen, barRect);
		}
		int num = barRect.Y + (int)((1f - _value) * (float)barRect.Height);
		using (Pen pen2 = new Pen(Color.FromArgb(200, 0, 0, 0), 3f))
		{
			g.DrawLine(pen2, barRect.X, num, barRect.Right, num);
		}
		using Pen pen3 = new Pen(Color.White, 1.4f);
		g.DrawLine(pen3, barRect.X, num, barRect.Right, num);
	}

	private void EnsureWheel(int diameter)
	{
		if ((_wheel == null || _wheelDiameter != diameter) && _failedDiameter != diameter)
		{
			Bitmap wheel;
			try
			{
				FailPointForTest?.Invoke("build");
				wheel = BuildWheel(diameter);
			}
			catch (Exception ex) when (PaintGuard.IsGdiFailure(ex))
			{
				_wheel?.Dispose();
				_wheel = null;
				_wheelDiameter = 0;
				_failedDiameter = diameter;
				throw;
			}
			Bitmap? wheel2 = _wheel;
			_wheel = wheel;
			_wheelDiameter = diameter;
			_failedDiameter = 0;
			wheel2?.Dispose();
		}
	}

	private static Bitmap BuildWheel(int diameter)
	{
		Bitmap bitmap = new Bitmap(diameter, diameter, PixelFormat.Format32bppArgb);
		float num = (float)diameter / 2f;
		BitmapData bitmapData;
		try
		{
			bitmapData = bitmap.LockBits(new Rectangle(0, 0, diameter, diameter), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
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
			for (int i = 0; i < diameter; i++)
			{
				Array.Clear(array, 0, array.Length);
				for (int j = 0; j < diameter; j++)
				{
					float num2 = (float)j - num + 0.5f;
					float num3 = (float)i - num + 0.5f;
					float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
					if (!(num4 > num))
					{
						Color color = FromHsv((float)((Math.Atan2(num3, num2) * 180.0 / Math.PI + 360.0) % 360.0), Math.Min(1f, num4 / num), 1f);
						byte b = ((num4 > num - 1.2f) ? ((byte)(255f * Math.Clamp(num - num4, 0f, 1.2f) / 1.2f)) : byte.MaxValue);
						int num5 = j * 4;
						array[num5] = color.B;
						array[num5 + 1] = color.G;
						array[num5 + 2] = color.R;
						array[num5 + 3] = b;
					}
				}
				Marshal.Copy(array, 0, bitmapData.Scan0 + i * bitmapData.Stride, bitmapData.Stride);
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

	public static Color FromHsv(float hue, float saturation, float value)
	{
		hue = (hue % 360f + 360f) % 360f;
		saturation = Math.Clamp(saturation, 0f, 1f);
		value = Math.Clamp(value, 0f, 1f);
		float num = value * saturation;
		float num2 = num * (1f - Math.Abs(hue / 60f % 2f - 1f));
		float num3 = value - num;
		var (num4, num5, num6) = ((hue < 60f) ? (num, num2, 0f) : ((hue < 120f) ? (num2, num, 0f) : ((hue < 180f) ? (0f, num, num2) : ((hue < 240f) ? (0f, num2, num) : ((!(hue < 300f)) ? (num, 0f, num2) : (num2, 0f, num))))));
		return Color.FromArgb(255, (int)Math.Round((num4 + num3) * 255f), (int)Math.Round((num5 + num3) * 255f), (int)Math.Round((num6 + num3) * 255f));
	}

	public static string ToHex(Color colour)
	{
		return $"{colour.R:X2}{colour.G:X2}{colour.B:X2}";
	}

	public static bool TryParseHex(string? text, out Color colour)
	{
		colour = Color.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.Trim().TrimStart('#');
		int length = text2.Length;
		if ((length != 3 && length != 6) || 1 == 0)
		{
			return false;
		}
		string text3 = text2;
		for (length = 0; length < text3.Length; length++)
		{
			if (!Uri.IsHexDigit(text3[length]))
			{
				return false;
			}
		}
		if (text2.Length == 3)
		{
			text2 = string.Concat(text2[0], text2[0], text2[1], text2[1], text2[2], text2[2]);
		}
		colour = Color.FromArgb(255, Convert.ToInt32(text2.Substring(0, 2), 16), Convert.ToInt32(text2.Substring(2, 2), 16), Convert.ToInt32(text2.Substring(4, 2), 16));
		return true;
	}

	public static void ToHsv(Color colour, out float hue, out float saturation, out float value)
	{
		float num = (float)(int)colour.R / 255f;
		float num2 = (float)(int)colour.G / 255f;
		float num3 = (float)(int)colour.B / 255f;
		float num4 = Math.Max(num, Math.Max(num2, num3));
		float num5 = Math.Min(num, Math.Min(num2, num3));
		float num6 = num4 - num5;
		value = num4;
		saturation = ((num4 <= 0f) ? 0f : (num6 / num4));
		if (num6 <= 0f)
		{
			hue = 0f;
			return;
		}
		float num7;
		if (num4 == num)
		{
			num7 = 60f * ((num2 - num3) / num6 % 6f);
		}
		else
		{
			num7 = ((num4 == num2) ? (60f * ((num3 - num) / num6 + 2f)) : (60f * ((num - num2) / num6 + 4f)));
		}
		hue = num7;
		hue = (hue % 360f + 360f) % 360f;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_wheel?.Dispose();
			_ring.Release();
		}
		base.Dispose(disposing);
	}
}
