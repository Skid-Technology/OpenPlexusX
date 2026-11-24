using System.Drawing;

namespace VibranceHud.Design;

public sealed class SwitchableBackdrop : IBackdrop
{
	private readonly BlossomBough _bough = new BlossomBough();

	private IBackdrop? _layer;

	private SoftBackdrop? _soft;

	private bool _boughShows = true;

	private int _w;

	private int _h;

	public BackdropLook Look { get; private set; }

	public bool Moves
	{
		get
		{
			if (!_boughShows || !_bough.Moves)
			{
				return _layer?.Moves ?? false;
			}
			return true;
		}
	}

	internal SoftBackdrop? SoftForTest => _layer as SoftBackdrop;

	public void Use(BackdropLook look)
	{
		if (look != Look)
		{
			Look = look;
			_boughShows = look != BackdropLook.Off;
			SoftBackdrop layer = ((look != BackdropLook.Soft) ? null : (_soft ?? (_soft = new SoftBackdrop())));
			_layer = layer;
			if (_w > 0 && _h > 0)
			{
				_layer?.Resize(_w, _h);
			}
		}
	}

	public void Resize(int w, int h)
	{
		_w = w;
		_h = h;
		_bough.Resize(w, h);
		_soft?.Resize(w, h);
		if (_layer != _soft)
		{
			_layer?.Resize(w, h);
		}
	}

	public void Update(double dtSeconds)
	{
		_bough.Update(dtSeconds);
		_layer?.Update(dtSeconds);
	}

	public void Paint(Graphics g, int offsetX, int offsetY)
	{
		if (_boughShows && _layer is SoftBackdrop softBackdrop)
		{
			softBackdrop.PaintOver(_bough, g, offsetX, offsetY);
			return;
		}
		if (_boughShows)
		{
			_bough.Paint(g, offsetX, offsetY);
		}
		_layer?.Paint(g, offsetX, offsetY);
	}
}
