using System.Drawing;
using System.Windows.Forms;

namespace VibranceHud;

public sealed class HeaderPanel : Panel
{
	private readonly CardSurface _surface = new CardSurface();

	public HeaderPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		_surface.Paint(this, e.Graphics);
		base.OnPaint(e);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_surface.Dispose();
		}
		base.Dispose(disposing);
	}
}
