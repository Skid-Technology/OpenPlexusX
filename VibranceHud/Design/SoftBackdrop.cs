using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using VibranceHud.Theming;

namespace VibranceHud.Design;

public sealed class SoftBackdrop : IBackdrop
{
	private int _w;

	private int _h;

	private const int MostLayers = 4;

	private readonly List<(string Key, Bitmap Layer)> _layers = new List<(string, Bitmap)>();

	private readonly HashSet<string> _failedKeys = new HashSet<string>();

	internal int LayerBuilds { get; private set; }

	internal int PoolDrawsForTest { get; private set; }

	internal int LayersHeldForTest => _layers.Count;

	public bool Moves => false;

	public void Resize(int w, int h)
	{
		if (w != _w || h != _h)
		{
			foreach (var layer in _layers)
			{
				layer.Layer.Dispose();
			}
			_layers.Clear();
			_failedKeys.Clear();
		}
		_w = w;
		_h = h;
	}

	public void Update(double dtSeconds)
	{
	}

	public void Paint(Graphics g, int offsetX, int offsetY)
	{
		if (_w > 0 && _h > 0)
		{
			Bitmap bitmap = LayerFor(offsetX, offsetY, null);
			if (bitmap != null)
			{
				g.DrawImageUnscaled(bitmap, 0, 0);
			}
		}
	}

	internal void PaintOver(BlossomBough under, Graphics g, int offsetX, int offsetY)
	{
		if (_w <= 0 || _h <= 0)
		{
			under.Paint(g, offsetX, offsetY);
			return;
		}
		Bitmap bitmap = LayerFor(offsetX, offsetY, under);
		if (bitmap != null)
		{
			g.DrawImageUnscaled(bitmap, 0, 0);
		}
	}

	private Bitmap? LayerFor(int offsetX, int offsetY, BlossomBough? under)
	{
		string text = Theme.CurrentName + "|" + _w + "x" + _h + "|" + offsetX + "," + offsetY + ((under == null) ? "" : (AppBackground.IsSet ? "|tree|img" : "|tree"));
		for (int i = 0; i < _layers.Count; i++)
		{
			if (_layers[i].Key == text)
			{
				return _layers[i].Layer;
			}
		}
		if (_failedKeys.Contains(text))
		{
			return null;
		}
		if (_layers.Count >= 4)
		{
			_layers[0].Layer.Dispose();
			_layers.RemoveAt(0);
		}
		Bitmap bitmap = null;
		try
		{
			BackdropLooks.FailPointForTest?.Invoke("build");
			bitmap = new Bitmap(_w, _h, PixelFormat.Format32bppPArgb);
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.Clear(Color.Transparent);
			under?.Paint(graphics, offsetX, offsetY);
			DrawPools(graphics, offsetX, offsetY);
		}
		catch (Exception ex) when (PaintGuard.IsGdiFailure(ex))
		{
			bitmap?.Dispose();
			if (_failedKeys.Count >= 16)
			{
				_failedKeys.Clear();
			}
			_failedKeys.Add(text);
			PaintGuard.LogOnce(this, ex);
			return null;
		}
		LayerBuilds++;
		_layers.Add((text, bitmap));
		return bitmap;
	}

	private void DrawPools(Graphics g, int offsetX, int offsetY)
	{
		PoolDrawsForTest++;
		Pool(g, Theme.PlexusNodeA, offsetX + (int)((float)_w * 0.22f), offsetY + (int)((float)_h * 0.18f), (int)((float)Math.Max(_w, _h) * 0.55f));
		Pool(g, Theme.PlexusNodeB, offsetX + (int)((float)_w * 0.8f), offsetY + (int)((float)_h * 0.72f), (int)((float)Math.Max(_w, _h) * 0.45f));
	}

	private static void Pool(Graphics g, Color colour, int cx, int cy, int radius)
	{
		if (radius <= 0)
		{
			return;
		}
		Rectangle rect = new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2);
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddEllipse(rect);
		PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
		pathGradientBrush.CenterColor = Color.FromArgb(46, colour);
		pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(0, colour) };
		using PathGradientBrush brush = pathGradientBrush;
		g.FillEllipse(brush, rect);
	}
}
