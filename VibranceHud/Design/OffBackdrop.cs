using System.Drawing;

namespace VibranceHud.Design;

public sealed class OffBackdrop : IBackdrop
{
	public bool Moves => false;

	public void Resize(int w, int h)
	{
	}

	public void Update(double dtSeconds)
	{
	}

	public void Paint(Graphics g, int offsetX, int offsetY)
	{
	}
}
