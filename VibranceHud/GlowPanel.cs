using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Theming;

namespace VibranceHud;

public sealed class GlowPanel : Panel
{
	public IBackdrop? Field { get; set; }

	public int Scrim { get; set; }

	public GlowPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		PaintGuard.Try(this, () =>
		{
			AppBackground.Paint(g, Left, Top);
			Field?.Paint(g, Left, Top);
			if (SlateSurface.Active)
			{
				Glass.PaintChrome(g, ClientRectangle, 0f, new Point(Left, Top));
				return;
			}
			using SolidBrush brush = new SolidBrush(SignalTokens.Ash.Chrome);
			g.FillRectangle(brush, ClientRectangle);
		});
		base.OnPaint(e);
	}
}
