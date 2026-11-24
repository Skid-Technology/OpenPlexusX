using System.Drawing;

namespace VibranceHud.Design;

public interface IBackdrop
{
	bool Moves { get; }

	void Resize(int w, int h);

	void Update(double dtSeconds);

	void Paint(Graphics g, int offsetX, int offsetY);
}
