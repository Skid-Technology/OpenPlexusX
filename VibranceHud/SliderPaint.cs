using System.Drawing;

namespace VibranceHud;

internal static class SliderPaint
{
	internal const int TrackPx = 2;

	internal const int KnobRingPx = 2;

	internal static int FillPx(bool lg)
	{
		if (!lg)
		{
			return 4;
		}
		return 6;
	}

	internal static int TickPx(bool lg)
	{
		if (!lg)
		{
			return 14;
		}
		return 16;
	}

	internal static int KnobPx(bool lg)
	{
		if (!lg)
		{
			return 7;
		}
		return 8;
	}

	internal static int FocusReachPx(bool lg)
	{
		return KnobPx(lg) + 1 + 2 + 2;
	}

	internal static void Track(Graphics g, int dpi, int left, int right, int cy)
	{
		int num = ControlInk.Px(dpi, 2);
		using SolidBrush brush = new SolidBrush(ControlInk.Track);
		g.FillRectangle(brush, left, cy - num / 2, right - left, num);
	}

	internal static Color FillInk(bool enabled)
	{
		if (!enabled)
		{
			return ControlInk.TextDisabled;
		}
		return ControlInk.Accent;
	}

	internal static void Fill(Graphics g, int from, int to, int cy, int fillH, Color ink)
	{
		if (to > from)
		{
			using (SolidBrush brush = new SolidBrush(ink))
			{
				g.FillRectangle(brush, from, cy - fillH / 2, to - from, fillH);
			}
		}
	}

	internal static void Tick(Graphics g, int dpi, int nx, int cy, int tickH, bool enabled)
	{
		int num = ControlInk.Hairline(dpi);
		using SolidBrush brush = new SolidBrush(enabled ? ControlInk.Text3 : ControlInk.TextDisabled);
		g.FillRectangle(brush, nx - num / 2, cy - tickH / 2, num, tickH);
	}

	internal static void Knob(Graphics g, int dpi, float radius, int x, int cy, Color ringInk, bool enabled)
	{
		float num = ControlInk.PxF(dpi, 2f);
		using (SolidBrush brush = new SolidBrush(ringInk))
		{
			g.FillEllipse(brush, (float)x - (radius + num / 2f), (float)cy - (radius + num / 2f), 2f * (radius + num / 2f), 2f * (radius + num / 2f));
		}
		using SolidBrush brush2 = new SolidBrush(enabled ? ControlInk.Accent : ControlInk.TextDisabledInk);
		g.FillEllipse(brush2, (float)x - (radius - num / 2f), (float)cy - (radius - num / 2f), 2f * (radius - num / 2f), 2f * (radius - num / 2f));
	}

	internal static void FocusRing(Graphics g, int dpi, bool lg, int x, int cy)
	{
		float num = ControlInk.PxF(dpi, KnobPx(lg) + 1);
		float num2 = ControlInk.PxF(dpi, 2f);
		float num3 = num + ControlInk.PxF(dpi, 2f) + num2 / 2f;
		using Pen pen = new Pen(ControlInk.Focus, num2);
		g.DrawEllipse(pen, (float)x - num3, (float)cy - num3, 2f * num3, 2f * num3);
	}
}
