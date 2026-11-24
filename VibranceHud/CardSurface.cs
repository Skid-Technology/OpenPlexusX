using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Theming;

namespace VibranceHud;

internal sealed class CardSurface : IDisposable
{
	private Bitmap? _glass;

	private Size _glassSize;

	private string _glassTheme = "";

	private Size _failedSize;

	private string _failedKey = "";

	private bool _failureLogged;

	private static int Radius => Tokens.Scale(8);

	internal Action<string>? FailPointForTest { get; set; }

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

	internal void Paint(Control owner, Graphics g)
	{
		try
		{
			if (SlateSurface.Active && AppBackground.IsSet)
			{
				Glass.PaintPanel(g, new RectangleF(0.5f, 0.5f, owner.Width - 1, owner.Height - 1), Radius, Glass.BackdropOffsetOf(owner));
				return;
			}
			Bitmap bitmap = EnsureGlass(owner.Size);
			if (bitmap != null)
			{
				FailPointForTest?.Invoke("draw");
				g.DrawImageUnscaled(bitmap, 0, 0);
			}
		}
		catch (Exception ex) when (IsGdiFailure(ex))
		{
			LogOnce(ex);
		}
	}

	private static void PaintSignalCard(Graphics g, Size size)
	{
		RectangleF rectangleF = new RectangleF(0.5f, 0.5f, size.Width - 1, size.Height - 1);
		float num = Radius;
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddArc(rectangleF.X, rectangleF.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(rectangleF.Right - num, rectangleF.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(rectangleF.Right - num, rectangleF.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(rectangleF.X, rectangleF.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		using (SolidBrush brush = new SolidBrush(SignalTokens.Ash.Surface1))
		{
			g.FillPath(brush, graphicsPath);
		}
		using Pen pen = new Pen(SignalTokens.Ash.Border, 1f);
		g.DrawPath(pen, graphicsPath);
	}

	private Bitmap? EnsureGlass(Size size)
	{
		if (size.Width < 2 || size.Height < 2)
		{
			return null;
		}
		string text = Theme.CurrentName + "|" + Glass.ActiveChrome;
		if (_glass != null && _glassSize == size && _glassTheme == text)
		{
			return _glass;
		}
		if (_failedSize == size && _failedKey == text)
		{
			return null;
		}
		try
		{
			FailPointForTest?.Invoke("build");
			_glass?.Dispose();
			_glass = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
			_glassSize = size;
			_glassTheme = text;
			using Graphics graphics = Graphics.FromImage(_glass);
			graphics.Clear(Color.Transparent);
			if (SlateSurface.Active)
			{
				Glass.PaintPanel(graphics, new RectangleF(0.5f, 0.5f, size.Width - 1, size.Height - 1), Radius);
			}
			else
			{
				PaintSignalCard(graphics, size);
			}
		}
		catch
		{
			_glass?.Dispose();
			_glass = null;
			_glassSize = Size.Empty;
			_glassTheme = "";
			_failedSize = size;
			_failedKey = text;
			throw;
		}
		_failedSize = Size.Empty;
		_failedKey = "";
		return _glass;
	}

	public void Dispose()
	{
		_glass?.Dispose();
		_glass = null;
	}
}
