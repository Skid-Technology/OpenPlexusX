using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Design;

namespace VibranceHud.Onboarding;

internal static class WizardChrome
{
	internal const int Radius = 8;

	internal const int Border = 1;

	internal static void Shape(Form form)
	{
		using GraphicsPath path = Glass.RoundedPath(new RectangleF(0f, 0f, form.Width + 1, form.Height + 1), Tokens.Scale(8));
		form.Region = new Region(path);
	}

	internal static void PaintRim(Graphics g, Form form)
	{
		SmoothingMode smoothingMode = g.SmoothingMode;
		PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.Half;
		float num = 0.5f;
		using GraphicsPath path = Glass.RoundedPath(new RectangleF(num, num, (float)form.Width - num * 2f, (float)form.Height - num * 2f), Math.Max(1f, (float)Tokens.Scale(8) - num));
		using Pen pen = new Pen(SignalDialogWindow.EdgeInk, 1f);
		g.DrawPath(pen, path);
		g.SmoothingMode = smoothingMode;
		g.PixelOffsetMode = pixelOffsetMode;
	}
}
