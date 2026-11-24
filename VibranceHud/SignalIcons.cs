using System.Drawing;
using System.Drawing.Drawing2D;

namespace VibranceHud;

internal static class SignalIcons
{
	private const float Grid = 20f;

	internal static void Draw(Graphics g, SignalIcon icon, RectangleF box, Color ink)
	{
		if (icon == SignalIcon.None || box.Width < 2f || box.Height < 2f)
		{
			return;
		}
		GraphicsState gstate = g.Save();
		try
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			g.TranslateTransform(box.X, box.Y);
			g.ScaleTransform(box.Width / 20f, box.Height / 20f);
			using Pen pen = new Pen(ink, 1.5f)
			{
				StartCap = LineCap.Round,
				EndCap = LineCap.Round,
				LineJoin = LineJoin.Round
			};
			using SolidBrush brush = new SolidBrush(ink);
			switch (icon)
			{
			case SignalIcon.Compare:
			{
				using (GraphicsPath path4 = RoundRect(3f, 3f, 14f, 14f, 2f))
				{
					g.DrawPath(pen, path4);
				}
				g.DrawLine(pen, 10f, 1.5f, 10f, 18.5f);
				using GraphicsPath graphicsPath5 = new GraphicsPath();
				using SolidBrush brush2 = new SolidBrush(Color.FromArgb((int)((float)(int)ink.A * 0.35f), ink));
				graphicsPath5.AddLine(10f, 3f, 15f, 3f);
				graphicsPath5.AddArc(13f, 3f, 4f, 4f, 270f, 90f);
				graphicsPath5.AddLine(17f, 5f, 17f, 15f);
				graphicsPath5.AddArc(13f, 13f, 4f, 4f, 0f, 90f);
				graphicsPath5.AddLine(15f, 17f, 10f, 17f);
				graphicsPath5.CloseFigure();
				g.FillPath(brush2, graphicsPath5);
				break;
			}
			case SignalIcon.Phone:
			{
				using (GraphicsPath path2 = RoundRect(5.5f, 2.5f, 9f, 15f, 2f))
				{
					g.DrawPath(pen, path2);
				}
				g.DrawLine(pen, 8.8f, 14.8f, 11.2f, 14.8f);
				break;
			}
			case SignalIcon.Reset:
				g.DrawArc(pen, 4.1260004f, 4.142f, 12f, 12f, 189.03f, 307.11f);
				g.DrawLines(pen, new PointF[3]
				{
					new PointF(3.8f, 4.6f),
					new PointF(3.8f, 9.4f),
					new PointF(8.6f, 9.4f)
				});
				break;
			case SignalIcon.Save:
				g.DrawPolygon(pen, new PointF[5]
				{
					new PointF(5.5f, 3f),
					new PointF(14.5f, 3f),
					new PointF(14.5f, 17f),
					new PointF(10f, 13.7f),
					new PointF(5.5f, 17f)
				});
				break;
			case SignalIcon.Copy:
			{
				using (GraphicsPath path3 = RoundRect(6.5f, 6.5f, 10f, 10f, 1.5f))
				{
					g.DrawPath(pen, path3);
				}
				using GraphicsPath graphicsPath4 = new GraphicsPath();
				graphicsPath4.AddLine(13.5f, 6.5f, 13.5f, 4.5f);
				graphicsPath4.AddArc(11.5f, 3.5f, 2f, 2f, 0f, -90f);
				graphicsPath4.AddLine(12.5f, 3.5f, 4.5f, 3.5f);
				graphicsPath4.AddArc(3.5f, 3.5f, 2f, 2f, 270f, -90f);
				graphicsPath4.AddLine(3.5f, 4.5f, 3.5f, 12.5f);
				graphicsPath4.AddArc(3.5f, 11.5f, 2f, 2f, 180f, -90f);
				graphicsPath4.AddLine(4.5f, 13.5f, 6.5f, 13.5f);
				g.DrawPath(pen, graphicsPath4);
				break;
			}
			case SignalIcon.Upload:
				g.DrawLine(pen, 10f, 13f, 10f, 3f);
				g.DrawLines(pen, new PointF[3]
				{
					new PointF(6f, 6.8f),
					new PointF(10f, 2.8f),
					new PointF(14f, 6.8f)
				});
				g.DrawLine(pen, 4f, 16.5f, 16f, 16.5f);
				break;
			case SignalIcon.Download:
				g.DrawLine(pen, 10f, 3f, 10f, 13f);
				g.DrawLines(pen, new PointF[3]
				{
					new PointF(6f, 9.2f),
					new PointF(10f, 13.2f),
					new PointF(14f, 9.2f)
				});
				g.DrawLine(pen, 4f, 16.5f, 16f, 16.5f);
				break;
			case SignalIcon.ExternalLink:
			{
				g.DrawLines(pen, new PointF[3]
				{
					new PointF(11.5f, 3.5f),
					new PointF(16.5f, 3.5f),
					new PointF(16.5f, 8.5f)
				});
				g.DrawLine(pen, 16.5f, 3.5f, 9.5f, 10.5f);
				using GraphicsPath graphicsPath3 = new GraphicsPath();
				graphicsPath3.AddLine(14.5f, 12.5f, 14.5f, 15f);
				graphicsPath3.AddArc(11.5f, 13.5f, 3f, 3f, 0f, 90f);
				graphicsPath3.AddLine(13f, 16.5f, 5f, 16.5f);
				graphicsPath3.AddArc(3.5f, 13.5f, 3f, 3f, 90f, 90f);
				graphicsPath3.AddLine(3.5f, 15f, 3.5f, 7f);
				graphicsPath3.AddArc(3.5f, 5.5f, 3f, 3f, 180f, 90f);
				graphicsPath3.AddLine(5f, 5.5f, 7.5f, 5.5f);
				g.DrawPath(pen, graphicsPath3);
				break;
			}
			case SignalIcon.Arrow:
				g.DrawLine(pen, 4f, 10f, 16f, 10f);
				g.DrawLines(pen, new PointF[3]
				{
					new PointF(11.5f, 5.5f),
					new PointF(16f, 10f),
					new PointF(11.5f, 14.5f)
				});
				break;
			case SignalIcon.Chevron:
				g.DrawLines(pen, new PointF[3]
				{
					new PointF(6f, 8f),
					new PointF(10f, 12f),
					new PointF(14f, 8f)
				});
				break;
			case SignalIcon.Close:
				g.DrawLine(pen, 5.5f, 5.5f, 14.5f, 14.5f);
				g.DrawLine(pen, 14.5f, 5.5f, 5.5f, 14.5f);
				break;
			case SignalIcon.Check:
				g.DrawLines(pen, new PointF[3]
				{
					new PointF(5f, 10.5f),
					new PointF(8.2f, 13.7f),
					new PointF(15f, 6.8f)
				});
				break;
			case SignalIcon.Plus:
				g.DrawLine(pen, 10f, 4f, 10f, 16f);
				g.DrawLine(pen, 4f, 10f, 16f, 10f);
				break;
			case SignalIcon.Search:
				g.DrawEllipse(pen, 3.5f, 3.5f, 10.6f, 10.6f);
				g.DrawLine(pen, 12.8f, 12.8f, 17f, 17f);
				break;
			case SignalIcon.Success:
				g.DrawEllipse(pen, 2.5f, 2.5f, 15f, 15f);
				g.DrawLines(pen, new PointF[3]
				{
					new PointF(6.6f, 10.3f),
					new PointF(8.9f, 12.6f),
					new PointF(13.5f, 7.8f)
				});
				break;
			case SignalIcon.Warning:
				g.DrawPolygon(pen, new PointF[3]
				{
					new PointF(10f, 2.8f),
					new PointF(17.8f, 16.5f),
					new PointF(2.2f, 16.5f)
				});
				g.DrawLine(pen, 10f, 8f, 10f, 11.6f);
				g.FillEllipse(brush, 9.1f, 13.1f, 1.8f, 1.8f);
				break;
			case SignalIcon.Danger:
				g.DrawPolygon(pen, new PointF[8]
				{
					new PointF(7f, 2.5f),
					new PointF(13f, 2.5f),
					new PointF(17.5f, 7f),
					new PointF(17.5f, 13f),
					new PointF(13f, 17.5f),
					new PointF(7f, 17.5f),
					new PointF(2.5f, 13f),
					new PointF(2.5f, 7f)
				});
				g.DrawLine(pen, 10f, 6.2f, 10f, 11f);
				g.FillEllipse(brush, 9.1f, 12.900001f, 1.8f, 1.8f);
				break;
			case SignalIcon.Info:
				g.DrawEllipse(pen, 2.5f, 2.5f, 15f, 15f);
				g.DrawLine(pen, 10f, 9f, 10f, 14f);
				g.FillEllipse(brush, 9.1f, 5.4f, 1.8f, 1.8f);
				break;
			case SignalIcon.Lock:
			{
				using (GraphicsPath path = RoundRect(4.5f, 8.5f, 11f, 8.5f, 1.5f))
				{
					g.DrawPath(pen, path);
				}
				using GraphicsPath graphicsPath2 = new GraphicsPath();
				graphicsPath2.AddLine(7f, 8.5f, 7f, 6.2f);
				graphicsPath2.AddArc(7f, 3.2f, 6f, 6f, 180f, 180f);
				graphicsPath2.AddLine(13f, 6.2f, 13f, 8.5f);
				g.DrawPath(pen, graphicsPath2);
				break;
			}
			case SignalIcon.Trash:
			{
				g.DrawLine(pen, 3.5f, 5.5f, 16.5f, 5.5f);
				using (GraphicsPath graphicsPath = new GraphicsPath())
				{
					graphicsPath.AddLine(8f, 5.5f, 8f, 4f);
					graphicsPath.AddLine(8f, 3.5f, 12f, 3.5f);
					graphicsPath.AddLine(12f, 4f, 12f, 5.5f);
					g.DrawPath(pen, graphicsPath);
				}
				g.DrawLines(pen, new PointF[6]
				{
					new PointF(5f, 5.5f),
					new PointF(5.8f, 16.1f),
					new PointF(7.3f, 17f),
					new PointF(12.7f, 17f),
					new PointF(14.2f, 16.1f),
					new PointF(15f, 5.5f)
				});
				g.DrawLine(pen, 8.5f, 8.5f, 8.5f, 14f);
				g.DrawLine(pen, 11.5f, 8.5f, 11.5f, 14f);
				break;
			}
			case SignalIcon.Verify:
				g.DrawLine(pen, 7.5f, 3f, 6f, 17f);
				g.DrawLine(pen, 14f, 3f, 12.5f, 17f);
				g.DrawLine(pen, 3.5f, 7.5f, 17.5f, 7.5f);
				g.DrawLine(pen, 2.5f, 12.5f, 16.5f, 12.5f);
				break;
			}
		}
		finally
		{
			g.Restore(gstate);
		}
	}

	private static GraphicsPath RoundRect(float x, float y, float w, float h, float r)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		float num = r * 2f;
		graphicsPath.AddArc(x, y, num, num, 180f, 90f);
		graphicsPath.AddArc(x + w - num, y, num, num, 270f, 90f);
		graphicsPath.AddArc(x + w - num, y + h - num, num, num, 0f, 90f);
		graphicsPath.AddArc(x, y + h - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}
}
