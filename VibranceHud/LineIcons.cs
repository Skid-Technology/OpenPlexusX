using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace VibranceHud;

internal static class LineIcons
{
	private static void Stroke(Graphics g, Rectangle box, Color ink, Action<Graphics, Pen> draw)
	{
		if (box.Width > 0 && box.Height > 0)
		{
			GraphicsState gstate = g.Save();
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			g.TranslateTransform(box.X, box.Y);
			g.ScaleTransform((float)box.Width / 20f, (float)box.Height / 20f);
			using (Pen arg = new Pen(ink, 1.5f)
			{
				StartCap = LineCap.Round,
				EndCap = LineCap.Round,
				LineJoin = LineJoin.Round
			})
			{
				draw(g, arg);
			}
			g.Restore(gstate);
		}
	}

	internal static void Check(Graphics g, Rectangle box, Color ink)
	{
		Stroke(g, box, ink, (Graphics gr, Pen pen) =>
		{
			gr.DrawLines(pen, new PointF[3]
			{
				new PointF(5f, 10.5f),
				new PointF(8.2f, 13.7f),
				new PointF(15f, 6.8f)
			});
		});
	}

	internal static void Success(Graphics g, Rectangle box, Color ink)
	{
		Stroke(g, box, ink, (Graphics gr, Pen pen) =>
		{
			gr.DrawEllipse(pen, 2.5f, 2.5f, 15f, 15f);
			gr.DrawLines(pen, new PointF[3]
			{
				new PointF(6.6f, 10.3f),
				new PointF(8.9f, 12.6f),
				new PointF(13.5f, 7.8f)
			});
		});
	}

	internal static void Close(Graphics g, Rectangle box, Color ink)
	{
		Stroke(g, box, ink, (Graphics gr, Pen pen) =>
		{
			gr.DrawLine(pen, 5.5f, 5.5f, 14.5f, 14.5f);
			gr.DrawLine(pen, 14.5f, 5.5f, 5.5f, 14.5f);
		});
	}

	internal static void Info(Graphics g, Rectangle box, Color ink)
	{
		Stroke(g, box, ink, (Graphics gr, Pen pen) =>
		{
			gr.DrawEllipse(pen, 2.5f, 2.5f, 15f, 15f);
			gr.DrawLine(pen, 10f, 9f, 10f, 14f);
			using SolidBrush brush = new SolidBrush(ink);
			gr.FillEllipse(brush, 9.1f, 5.4f, 1.8f, 1.8f);
		});
	}

	internal static void Plus(Graphics g, Rectangle box, Color ink)
	{
		Stroke(g, box, ink, (Graphics gr, Pen pen) =>
		{
			gr.DrawLine(pen, 10f, 4f, 10f, 16f);
			gr.DrawLine(pen, 4f, 10f, 16f, 10f);
		});
	}
}
