using System;

namespace VibranceHud.Zoom;

internal sealed class ZoomKeyBinder
{
	private readonly Func<bool> _allowed;

	private readonly Func<(uint Mask, uint Vk)> _key;

	private readonly Func<uint, uint, bool> _conflict;

	private readonly Func<uint, uint, bool> _bind;

	private readonly Action _unbind;

	private readonly Action _reset;

	private bool _bound;

	private uint _mask;

	private uint _vk;

	public bool IsBound => _bound;

	public ZoomKeyBinder(Func<bool> allowed, Func<(uint Mask, uint Vk)> key, Func<uint, uint, bool> conflict, Func<uint, uint, bool> bind, Action unbind, Action reset)
	{
		_allowed = allowed;
		_key = key;
		_conflict = conflict;
		_bind = bind;
		_unbind = unbind;
		_reset = reset;
	}

	public void Apply(bool force = false)
	{
		(uint, uint) tuple = _key();
		uint item = tuple.Item1;
		uint item2 = tuple.Item2;
		bool flag = _allowed() && item2 != 0 && !_conflict(item, item2);
		if (flag)
		{
			if (_bound && !force && item == _mask && item2 == _vk)
			{
				return;
			}
			if (_bind(item, item2))
			{
				_bound = true;
				_mask = item;
				_vk = item2;
				return;
			}
		}
		bool bound = _bound;
		_bound = false;
		if (bound | force | flag)
		{
			_unbind();
			_reset();
		}
	}
}
