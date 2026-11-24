using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibranceHud;

public sealed class LogoBox : Control
{
	private Image? _image;

	private Bitmap? _scaled;

	internal const int MaxSide = 4096;

	private int _failedHeight;

	private bool _failureLogged;

	public Image? Image
	{
		get
		{
			return _image;
		}
		set
		{
			if (_image != value)
			{
				_image = value;
				DropCache();
				_failedHeight = 0;
				Invalidate();
			}
		}
	}

	internal Action? BeforeRenderForTest { get; set; }

	public LogoBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		SetStyle(ControlStyles.Selectable, value: false);
		TabStop = false;
	}

	private void DropCache()
	{
		_scaled?.Dispose();
		_scaled = null;
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		DropCache();
		_failedHeight = 0;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			DropCache();
		}
		base.Dispose(disposing);
	}

	private static bool IsGdiFailure(Exception ex)
	{
		if (!(ex is OutOfMemoryException) && !(ex is ExternalException) && !(ex is ArgumentException))
		{
			return ex is InvalidOperationException;
		}
		return true;
	}

	private void LogOnce(Exception ex)
	{
		if (!_failureLogged)
		{
			_failureLogged = true;
			CrashLog.Write(ex);
		}
	}

	private Bitmap? RenderScaled(Image source, int height)
	{
		if (height > 4096)
		{
			return null;
		}
		Bitmap bitmap = null;
		try
		{
			int num;
			lock (BrandAssets.DrawGate)
			{
				if (source.Width < 1 || source.Height < 1)
				{
					return null;
				}
				num = Math.Max(1, (int)((float)height * ((float)source.Width / (float)source.Height)));
			}
			if (num > 4096)
			{
				return null;
			}
			BeforeRenderForTest?.Invoke();
			bitmap = new Bitmap(num, height, PixelFormat.Format32bppArgb);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.PixelOffsetMode = PixelOffsetMode.Half;
				lock (BrandAssets.DrawGate)
				{
					graphics.DrawImage(source, new Rectangle(0, 0, num, height));
				}
			}
			return bitmap;
		}
		catch (Exception ex) when (IsGdiFailure(ex))
		{
			bitmap?.Dispose();
			LogOnce(ex);
			return null;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (_image == null || Height < 1)
		{
			return;
		}
		if (_scaled == null || _scaled.Height != Height)
		{
			if (Height == _failedHeight)
			{
				return;
			}
			DropCache();
			_scaled = RenderScaled(_image, Height);
			if (_scaled == null)
			{
				_failedHeight = Height;
				return;
			}
		}
		try
		{
			e.Graphics.DrawImageUnscaled(_scaled, 0, 0);
		}
		catch (Exception ex) when (IsGdiFailure(ex))
		{
			LogOnce(ex);
		}
	}
}
